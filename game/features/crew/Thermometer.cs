using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The vitals' temperature dial, the second circle (body-temperature, the user's picks «Ртуть» G4 K1 N3 and «dials» A1): a small
/// upright thermometer in its centre, its bulb a test tube's that flames lick in the heat and a crust of frost climbs in the cold; round
/// it a two-sided ring, the normal at the top, the heat filling red clockwise and the cold blue counter-clockwise, both deaths meeting
/// at the bottom (<see cref="Thermometers.RingAngle"/>). Glints run round the ring toward where the place draws the body, red and
/// quicker when that is past a death; through the last breath the glass beats red with the heart. Under it the body to a tenth; the
/// hover names the body, the degrees where it stands and both limits. Hidden with no hero or passed out. Reads four times a second.
/// </summary>
public partial class Thermometer : CanvasLayer
{
    private const double RefreshS = 0.25;
    private static readonly Color Calm = new("d3dee9"), FrostLight = new("9ed8ff"), FrostDeep = new("3a86e0"), Rime = new("e6f7ff");
    private static readonly Color HeatLight = new("ffc46e"), HeatDeep = new("ff5a26"), Danger = new("ff4a3a"), Ink = new("0c0e14");
    private static readonly Color Silver = new("c8d2de");

    // The dial's centre and ring, in the block's units.
    private const float R = Vitals.RingRadius, Light = Vitals.RingLight;
    private static readonly Vector2 Dial = new(Vitals.Column(Vitals.TemperatureDial), Vitals.RowY);
    // The sign was laid out at an icon of 22: it grows from there, centred a little low in the circle as its bulb is heavy.
    private const float G = Vitals.Icon / 22f, Mid = Vitals.RowY + 1.3f * G;
    // The sign: its stem's half width and its cap's centre, the bulb's radius and centre.
    private const float StemR = 2.3f * G, StemTop = Mid - 11 * G, BulbR = 4 * G, BulbY = Mid + 4.4f * G;
    private static readonly float Cx = Dial.X, StemFoot = BulbY - MathF.Sqrt(BulbR * BulbR - StemR * StemR);
    private static readonly Vector2[] Outline = MakeOutline(), BulbEdge = Circle(new Vector2(Cx, BulbY), BulbR + 0.7f * G, 28);

    private readonly Func<ThermometerReading?> _read;
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Face _face;
    private readonly AddedLayer _light;
    private readonly TipSpots _tips;
    private readonly Ui _ui;
    private double _sinceRead = RefreshS;
    private ThermometerReading _sim;
    private float _glided;
    private float _target = Thermometers.Normal;
    private bool _shown;
    private double _clock;

    /// <param name="read">What this player's hero's body reads (<see cref="Thermometers.Read"/>); null without a hero or passed out, which hides it.</param>
    /// <param name="ui">The slots' size the block follows, and the one tooltip.</param>
    public Thermometer(Func<ThermometerReading?> read, Ui ui)
    {
        (_read, _ui) = (read, ui);
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.Hud;
        _face = new Face(this) { MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(_face);
        _root.AddChild(_light = new AddedLayer(on => _face.DrawLight(on)));
        var spot = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = Dial - new Vector2(Vitals.Dial / 2, Vitals.Dial / 2),
            Size = new Vector2(Vitals.Dial, Vitals.Dial + Vitals.CircleGap),
        };
        _root.AddChild(spot);
        AddChild(_root);
        _tips.Add(spot, () => Thermometers.Tip(_sim));
    }

    public override void _Process(double delta)
    {
        var (x, y) = Vitals.Origin(GetViewport().GetVisibleRect().Size.Y, _ui.Slot);
        float scale = Vitals.ScaleOf(_ui.Slot);
        var place = (new Vector2(x, y), new Vector2(scale, scale));
        bool moved = place != (_root.Position, _root.Scale);
        (_root.Position, _root.Scale) = place;
        _sinceRead += delta;
        if (_sinceRead >= RefreshS)
        {
            _sinceRead = 0;
            var read = _read();
            Visible = read != null;
            if (read is { } now)
            {
                // Shown again (a first read, waking up): the reading as it is, nothing to glide from.
                if (!_shown) (_glided, _target) = (now.Body / 100f, now.Target / 100f);
                _sim = now;
            }
            _shown = read != null;
        }
        if (!Visible)
        {
            _tips.Frame(null);
            return;
        }
        _clock += delta;
        float glided = Thermometers.Glide(_glided, _sim.Body / 100f, delta), target = Thermometers.Glide(_target, _sim.Target / 100f, delta);
        // Flames, frost, glints and the heart move: drawn each frame unless the body is calm and going nowhere.
        bool still = Thermometers.Shown(glided) == Thermometers.Shown(Thermometers.Normal) && !Ghosted(glided, target) && _sim.Gasp == 0;
        if (!still || moved || glided != _glided || target != _target)
        {
            _face.QueueRedraw();
            _light.QueueRedraw();
        }
        (_glided, _target) = (glided, target);
        _tips.Frame(!_ui.MenuOpen.Get() ? GetViewport().GetMousePosition() : null);
    }

    /// <summary>Whether the glints run: the place draws the body somewhere it is not yet, or past a death.</summary>
    private static bool Ghosted(float body, float target) =>
        Thermometers.Past(target) || Math.Abs(Thermometers.RingAngle(target) - Thermometers.RingAngle(body)) * R > 1.2f;

    /// <summary>The colour at <paramref name="degrees"/>: calm at the normal, frost-blue below, ember above, deeper toward either death.</summary>
    private static Color ColourAt(float degrees)
    {
        int tenths = Thermometers.Shown(degrees), normal = Thermometers.Shown(Thermometers.Normal);
        return tenths < normal ? FrostLight.Lerp(FrostDeep, Thermometers.Frost(degrees))
            : tenths > normal ? HeatLight.Lerp(HeatDeep, Thermometers.Hot(degrees)) : Calm;
    }

    private static Vector2 OnRing(float angle, float radius) => Dial + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

    private static Vector2[] MakeOutline()
    {
        var points = new List<Vector2> { new(Cx - StemR, StemFoot) };
        for (int i = 0; i <= 8; i++)
        {
            float a = MathF.PI + MathF.PI * i / 8;
            points.Add(new Vector2(Cx + StemR * MathF.Cos(a), StemTop + StemR * MathF.Sin(a)));
        }
        points.Add(new Vector2(Cx + StemR, StemFoot));
        float side = MathF.Asin(StemR / BulbR), from = -MathF.PI / 2 + side, to = -MathF.PI / 2 - side + Mathf.Tau;
        for (int i = 1; i < 24; i++)
        {
            float a = from + (to - from) * i / 24;
            points.Add(new Vector2(Cx + BulbR * MathF.Cos(a), BulbY + BulbR * MathF.Sin(a)));
        }
        return [.. points];
    }

    private static Vector2[] Circle(Vector2 centre, float radius, int n)
    {
        var points = new Vector2[n];
        for (int i = 0; i < n; i++) points[i] = centre + radius * Vector2.FromAngle(Mathf.Tau * i / n);
        return points;
    }

    private static float Hash(float n)
    {
        double h = Math.Sin(n * 127.1 + 311.7) * 43758.5453;
        return (float)(h - Math.Floor(h));
    }

    private sealed partial class Face(Thermometer owner) : Control
    {
        private float _t, _hot;
        private bool _warm;

        public override void _Draw()
        {
            float body = owner._glided, target = owner._target, breath = Thermometers.Breath(owner._sim.Gasp);
            int tenths = Thermometers.Shown(body), normal = Thermometers.Shown(Thermometers.Normal);
            bool cold = tenths < normal, gasping = breath >= 0, coldSide = body < Thermometers.Normal;
            _t = (float)owner._clock;
            (_warm, _hot) = (tenths > normal, Thermometers.Hot(body));
            float frost = Thermometers.Frost(body), beat = gasping ? Thermometers.Beat(owner._clock, coldSide, breath) : 0;
            var colour = ColourAt(body);
            var still = cold || _warm ? colour : Silver;
            var mercury = gasping ? still.Lerp(Danger, 0.3f + 0.5f * beat) : still;
            float coldK = gasping && coldSide ? 1 : cold ? frost : 0, hotK = gasping && !coldSide ? 1 : _warm ? _hot : 0;
            DrawRing(body, target, cold);
            DrawSign(body, mercury, gasping, beat, coldK, hotK);
            DialNumber.Draw(this, [Thermometers.Body(tenths)], new Vector2(Dial.X, Vitals.NumberY), owner._root.Scale.X, colour);
        }

        private void Arc(CanvasItem on, string tone, float from, float to, float alpha) =>
            RingLight.Arc(on, LightStrip.Of(tone, 0), Dial, R, Light, from, to, Colors.White with { A = alpha });

        /// <summary>
        /// The ring: the cold's half dark blue and the heat's dark red, the body's side lit from the top to where it stands; a notch a
        /// degree, a pointer at the normal, a red mark where both deaths meet; the glints toward the target and a tick where they stop.
        /// </summary>
        private void DrawRing(float body, float target, bool cold)
        {
            const float top = -MathF.PI / 2, half = MathF.PI, gap = 1.2f / R;
            Arc(this, "darkSky", top - half + gap, top - gap, 0.85f);
            Arc(this, "darkRed", top + gap, top + half - gap, 0.85f);
            float b = Thermometers.RingAngle(body);
            if (_warm) Arc(this, "red", top, b, 0.95f);
            else if (cold) Arc(this, "sky", b, top, 0.95f);
            float r0 = R + Light * 0.7f + 0.5f;
            foreach (var (angle, isLong) in Thermometers.RingNotches())
                DrawLine(OnRing(angle, r0), OnRing(angle, r0 + (isLong ? 2.4f : 1.1f)), Calm with { A = isLong ? 0.55f : 0.3f }, isLong ? 0.8f : 0.6f, true);
            var tip = OnRing(top, r0 - 0.2f);
            DrawColoredPolygon([tip, tip + new Vector2(-1.5f, -2.6f), tip + new Vector2(1.5f, -2.6f)], Calm with { A = 0.7f });
            DrawLine(OnRing(top + half, r0), OnRing(top + half, r0 + 2.6f), Danger with { A = 0.75f }, 1, true);
            if (!Ghosted(body, target)) return;
            bool past = Thermometers.Past(target);
            float tA = Thermometers.RingAngle(target), speed = past ? 1.3f : 0.85f;
            var colour = past ? Danger : ColourAt(target);
            for (int i = 0; i < 3; i++)
            {
                float p = (_t * speed + i / 3f) % 1, s = MathF.Sin(MathF.PI * p);
                DrawCircle(OnRing(b + (tA - b) * p, R), 0.7f * s, colour.Lerp(Colors.White, 0.4f) with { A = 0.85f * s });
            }
            DrawLine(OnRing(tA, R - Light * 0.75f), OnRing(tA, R + Light * 0.75f), Ink with { A = 0.6f }, 1.6f, true);
            DrawLine(OnRing(tA, R - Light * 0.75f), OnRing(tA, R + Light * 0.75f), colour with { A = past ? 0.55f + 0.45f * MathF.Cos(_t * MathF.PI * 2.2f) : 0.9f }, 0.7f, true);
        }

        /// <summary>The ring's light, added: the glow under the lit side, deeper toward the death; the glints' glow.</summary>
        public void DrawLight(CanvasItem on)
        {
            float body = owner._glided, target = owner._target;
            int tenths = Thermometers.Shown(body), normal = Thermometers.Shown(Thermometers.Normal);
            float b = Thermometers.RingAngle(body);
            const float top = -MathF.PI / 2;
            if (tenths > normal) RingLight.Glow(on, LightStrip.Of("red", 0), Dial, R, Light, top, b, 0.5f + 0.4f * Thermometers.Hot(body));
            else if (tenths < normal) RingLight.Glow(on, LightStrip.Of("sky", 0), Dial, R, Light, b, top, 0.5f + 0.4f * Thermometers.Frost(body));
            if (!Ghosted(body, target)) return;
            bool past = Thermometers.Past(target);
            float tA = Thermometers.RingAngle(target), speed = past ? 1.3f : 0.85f, t = (float)owner._clock;
            var colour = past ? Danger : ColourAt(target);
            for (int i = 0; i < 3; i++)
            {
                float p = (t * speed + i / 3f) % 1;
                RingLight.Spot(on, OnRing(b + (tA - b) * p, R), 3.2f, colour with { A = 0.55f * MathF.Sin(MathF.PI * p) });
            }
        }

        /// <summary>The sign: the glass with its mercury, flames licking the bulb in the heat, a crust of frost climbing it in the cold.</summary>
        private void DrawSign(float body, Color mercury, bool gasping, float beat, float coldK, float hotK)
        {
            var bulb = new Vector2(Cx, BulbY);
            var edge = gasping ? Calm.Lerp(Danger, 0.5f + 0.5f * beat) : Calm;
            DrawColoredPolygon(Outline, Silver with { A = 0.08f });
            if (gasping) Glow(bulb, (6 + 2 * beat) * G, Danger with { A = 0.3f + 0.5f * beat });
            DrawCircle(bulb, 2.5f * G, mercury with { A = 0.95f });
            float share = Math.Clamp((body - Thermometers.Coldest) / (Thermometers.Hottest - Thermometers.Coldest), 0, 1);
            float top = StemFoot + 0.6f * G - (StemFoot + 0.6f * G - StemTop) * share;
            DrawRect(new Rect2(Cx - 0.95f * G, top, 1.9f * G, BulbY - top), mercury with { A = 0.95f });
            DrawPolyline([.. Outline, Outline[0]], edge with { A = 0.85f }, 1.1f * G, true);
            foreach (float y in new[] { StemTop + 2.5f * G, StemTop + 5.5f * G, StemTop + 8.5f * G })
                DrawLine(new Vector2(Cx + StemR + 0.9f * G, y), new Vector2(Cx + StemR + 2.3f * G, y), edge with { A = 0.5f }, 0.6f * G, true);
            if (hotK > 0)
            {
                float k = Math.Clamp(hotK * 2.5f, 0, 1);
                int n = 3 + (int)MathF.Round(2 * hotK);
                for (int i = 0; i < n; i++)
                {
                    float x = Cx + (i - (n - 1) / 2f) * (5.2f * G / Math.Max(1, n - 1)) * 1.4f, flicker = 0.7f + 0.3f * MathF.Sin(_t * 13 + i * 2.1f);
                    float height = (2.6f + 4.5f * hotK) * G * flicker * (i == 0 || i == n - 1 ? 0.75f : 1);
                    Tongue(new Vector2(x, BulbY + BulbR + 1.4f * G), height, 1.1f * G, MathF.Sin(_t * 9 + i) * 0.9f * G, k);
                }
                Glow(bulb + new Vector2(0, 2 * G), 7 * G, HeatDeep with { A = 0.3f * k });
            }
            if (coldK > 0) DrawCrust(Math.Clamp(coldK * 1.3f, 0, 1));
        }

        /// <summary>A tongue of flame from <paramref name="foot"/> up: an ember outside, a light within.</summary>
        private void Tongue(Vector2 foot, float height, float width, float sway, float alpha)
        {
            if (alpha <= 0.01f || height <= 0.2f) return;
            var tip = foot + new Vector2(sway, -height);
            var outer = new List<Vector2>();
            Curve(outer, foot + new Vector2(-width, 0), foot + new Vector2(-width * 1.1f, -height * 0.45f), tip);
            Curve(outer, tip, foot + new Vector2(width * 1.1f, -height * 0.45f), foot + new Vector2(width, 0));
            Curve(outer, foot + new Vector2(width, 0), foot + new Vector2(0, width * 0.8f), foot + new Vector2(-width, 0));
            DrawColoredPolygon([.. outer], HeatDeep with { A = 0.8f * alpha });
            var inner = new List<Vector2>();
            var core = foot + new Vector2(sway * 0.6f, -height * 0.6f);
            Curve(inner, foot + new Vector2(-width * 0.45f, 0), foot + new Vector2(-width * 0.5f, -height * 0.3f), core);
            Curve(inner, core, foot + new Vector2(width * 0.5f, -height * 0.3f), foot + new Vector2(width * 0.45f, 0));
            DrawColoredPolygon([.. inner], HeatLight with { A = 0.85f * alpha });
        }

        /// <summary>Adds a quadratic curve's points to <paramref name="into"/>, its last left for the next.</summary>
        private static void Curve(List<Vector2> into, Vector2 from, Vector2 control, Vector2 to)
        {
            for (int i = 0; i < 6; i++)
            {
                float s = i / 6f;
                into.Add(from.Lerp(control, s).Lerp(control.Lerp(to, s), s));
            }
        }

        /// <summary>Frost climbing the bulb from its foot, <paramref name="cover"/> of the way up, a ring of rime about it.</summary>
        private void DrawCrust(float cover)
        {
            float level = BulbY + BulbR - 2 * BulbR * cover;
            var crust = new List<Vector2> { new(Cx - BulbR - G, BulbY + BulbR + G) };
            for (float u = -BulbR / G - 1; u <= BulbR / G + 1.01f; u += 0.7f)
                crust.Add(new Vector2(Cx + u * G, level + (MathF.Sin(u * 4.1f) * 0.6f + (Hash(u * 3.3f) - 0.5f) * 0.9f) * G));
            crust.Add(new Vector2(Cx + BulbR + G, BulbY + BulbR + G));
            foreach (var piece in Geometry2D.IntersectPolygons(crust.ToArray(), BulbEdge))
            {
                var (corners, indices) = (piece, Geometry2D.TriangulatePolygon(piece));
                // DrawColoredPolygon would fail on a crest just inside the rim or touching it: those are filled in strips.
                if (indices.Length == 0)
                {
                    var (points, triangles) = CrustFill.Upright(Array.ConvertAll(piece, p => (p.X, p.Y)));
                    (corners, indices) = (Array.ConvertAll(points, p => new Vector2(p.X, p.Y)), triangles);
                }
                if (indices.Length > 0) RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), indices, corners, [Rime with { A = 0.78f }]);
                DrawPolyline([.. piece, piece[0]], Colors.White with { A = 0.9f }, 0.4f * G, true);
            }
            var bulb = new Vector2(Cx, BulbY);
            for (int i = 0; i < 10; i++)
            {
                var at = new Vector2(Cx - BulbR + Hash(i * 2.7f) * 2 * BulbR, level + 0.6f * G + Hash(i * 5.9f) * 2 * BulbR * cover);
                if (at.DistanceTo(bulb) <= BulbR + 0.3f * G) DrawCircle(at, 0.35f * G, FrostDeep with { A = 0.45f });
            }
            DrawArc(bulb, BulbR + 0.5f * G, 0, Mathf.Tau, 28, Rime with { A = 0.55f * cover }, 0.6f * G, true);
        }

        private void Glow(Vector2 at, float radius, Color colour) =>
            DrawTextureRect(PixelArt.Glow, new Rect2(at - new Vector2(radius, radius), new Vector2(2 * radius, 2 * radius)), false, colour);
    }
}
