using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The vitals' thermometer, a row under the charge (body-temperature, the user's pick «Ртуть» G4 K1 N3): a small upright
/// thermometer in the signs' column, its bulb a test tube's that flames lick in the heat and a crust of frost climbs in the cold, and
/// a glass tube from the cold's death to the heat's with the body's mercury in it, frost-blue below its normal and ember above.
/// Glints run along the glass toward where the place draws the body, red and quicker when that is past a death; the frost fogs
/// the glass, the heat makes it waver; through the last breath the thread breaks into beads that pulse with the heart. No number:
/// the hover names the body, the degrees where it stands and both limits. Hidden with no hero or passed out. Reads four times a second.
/// </summary>
public partial class Thermometer : CanvasLayer
{
    private const double RefreshS = 0.25;
    private static readonly Color Calm = new("d3dee9"), FrostLight = new("9ed8ff"), FrostDeep = new("3a86e0"), Rime = new("e6f7ff");
    private static readonly Color HeatLight = new("ffc46e"), HeatDeep = new("ff5a26"), Danger = new("ff4a3a"), Ink = new("0c0e14");
    private static readonly Color Silver = new("c8d2de"), Glass = new("aac3e1");

    // The face's own units are the block's; the row's centre line sits Mid down it.
    // The tube as tall as the other rows' pieces.
    private const float Mid = 20, Tube = 112, TubeR = Vitals.PieceHeight / 2, Left = Vitals.RowLeft;
    // The sign was laid out at an icon of 22 and the tube's details at a radius of 3.4: each grows from there.
    private const float G = Vitals.Icon / 22f, K = TubeR / 3.4f;
    // The sign: its stem's half width and its cap's centre, the bulb's radius and centre.
    private const float StemR = 2.3f * G, StemTop = Mid - 11 * G, BulbR = 4 * G, BulbY = Mid + 4.4f * G, Cx = Vitals.SignX;
    private static readonly float StemFoot = BulbY - MathF.Sqrt(BulbR * BulbR - StemR * StemR);
    private static readonly Vector2[] Outline = MakeOutline(), Capsule = MakeCapsule(), BulbEdge = Circle(new Vector2(Cx, BulbY), BulbR + 0.7f * G, 28);

    private readonly Func<ThermometerReading?> _read;
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Face _face;
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
        _face = new Face(this)
        {
            Position = new Vector2(0, Vitals.ColdY - Mid),
            Size = new Vector2(Left + Tube + TubeR + 2, 2 * Mid),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _root.AddChild(_face);
        AddChild(_root);
        _tips.Add(_face, () => Thermometers.Tip(_sim));
    }

    public override void _Process(double delta)
    {
        var (x, y) = Vitals.Origin(GetViewport().GetVisibleRect().Size.Y, _ui.Slot);
        float scale = Vitals.ScaleOf(_ui.Slot);
        (_root.Position, _root.Scale) = (new Vector2(x, y), new Vector2(scale, scale));
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
        // Flames, fog, haze, glints and the heart move: drawn each frame unless the body is calm and going nowhere.
        bool still = Thermometers.Shown(glided) == Thermometers.Shown(Thermometers.Normal) && !Ghosted(glided, target) && _sim.Gasp == 0;
        if (!still || glided != _glided || target != _target) _face.QueueRedraw();
        (_glided, _target) = (glided, target);
        _tips.Frame(!_ui.MenuOpen.Get() ? GetViewport().GetMousePosition() : null);
    }

    /// <summary>Where <paramref name="degrees"/> stand along the tube, in the face's units.</summary>
    private static float X(float degrees) => Left + Thermometers.OnScale(degrees).At * Tube;

    /// <summary>Whether the glints run: the place draws the body somewhere it is not yet, or past a death.</summary>
    private static bool Ghosted(float body, float target) => Thermometers.OnScale(target).Past || Math.Abs(X(target) - X(body)) > 1.2f;

    /// <summary>The mercury's colour at <paramref name="degrees"/>: calm at the normal, frost-blue below, ember above, deeper toward either death.</summary>
    private static Color ColourAt(float degrees)
    {
        int tenths = Thermometers.Shown(degrees), normal = Thermometers.Shown(Thermometers.Normal);
        return tenths < normal ? FrostLight.Lerp(FrostDeep, Thermometers.Frost(degrees))
            : tenths > normal ? HeatLight.Lerp(HeatDeep, Thermometers.Hot(degrees)) : Calm;
    }

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

    private static Vector2[] MakeCapsule()
    {
        var points = new List<Vector2>();
        for (int i = 0; i <= 10; i++)
        {
            float a = -MathF.PI / 2 + MathF.PI * i / 10;
            points.Add(new Vector2(Left + Tube + TubeR * MathF.Cos(a), Mid + TubeR * MathF.Sin(a)));
        }
        for (int i = 0; i <= 10; i++)
        {
            float a = MathF.PI / 2 + MathF.PI * i / 10;
            points.Add(new Vector2(Left + TubeR * MathF.Cos(a), Mid + TubeR * MathF.Sin(a)));
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
            DrawSign(body, mercury, gasping, beat, coldK, hotK);
            DrawTube(body, target, colour, mercury, cold, frost, breath);
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
                DrawColoredPolygon(piece, Rime with { A = 0.78f });
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

        /// <summary>How far the heat makes the glass waver at <paramref name="x"/>: more toward its hot end.</summary>
        private float Wob(float x) => _warm ? MathF.Sin(x * 0.45f - _t * 7) * 0.9f * K * _hot * ((x - Left) / Tube) : 0;

        /// <summary>The tube: its glass and notches, the mercury's thread, the frost's fog, the glints toward the target.</summary>
        private void DrawTube(float body, float target, Color colour, Color mercury, bool cold, float frost, float breath)
        {
            float bx = X(body), tx = X(target), top = Mid - TubeR, foot = Mid + TubeR;
            bool gasping = breath >= 0;
            DrawColoredPolygon(Capsule, Glass with { A = 0.06f });
            var glass = _warm ? Calm.Lerp(HeatLight, _hot) : Calm;
            TubeLine(top, glass with { A = 0.4f }, 0.8f);
            TubeLine(foot, glass with { A = 0.22f }, 0.8f);
            TubeLine(top + 1, Colors.White with { A = 0.1f }, 0.5f);
            DrawArc(new Vector2(Left, Mid), TubeR, MathF.PI / 2, 3 * MathF.PI / 2, 10, glass with { A = 0.3f }, 0.8f, true);
            DrawArc(new Vector2(Left + Tube, Mid + Wob(Left + Tube)), TubeR, -MathF.PI / 2, MathF.PI / 2, 10, glass with { A = 0.3f }, 0.8f, true);
            if (_warm)
            {
                Glow(new Vector2(Left + Tube, Mid), 10 + 10 * _hot, HeatDeep with { A = 0.3f * _hot });
                for (float x = bx; x < Left + Tube; x += 8) Glow(new Vector2(x, Mid + Wob(x)), 6, HeatLight with { A = 0.08f * _hot });
            }
            float normalAt = Thermometers.OnScale(Thermometers.Normal).At;
            foreach (var (at, isLong) in Thermometers.Notches())
            {
                if (at <= 0 || at >= 1) continue;
                bool isNormal = Math.Abs(at - normalAt) < 1e-4f;
                float x = Left + at * Tube, length = isNormal ? 4 : isLong ? 3 : 1.3f;
                DrawLine(new Vector2(x, foot + 1.6f), new Vector2(x, foot + 1.6f + length),
                    Calm with { A = isNormal ? 0.7f : isLong ? 0.55f : 0.28f }, isNormal ? 1.1f : 0.9f, true);
            }
            foreach (float x in new[] { Left, Left + Tube })
                DrawLine(new Vector2(x, top - 1.6f), new Vector2(x, foot + 1.6f), Danger with { A = 0.6f }, 1.1f, true);
            float nx = Left + normalAt * Tube;
            DrawColoredPolygon([new(nx, top - 1.1f), new(nx - 1.7f, top - 3.7f), new(nx + 1.7f, top - 3.7f)], Calm with { A = 0.6f });
            // The thread: whole, or breaking into beads through the last breath.
            int pieces = gasping ? 7 : 1;
            float gap = gasping ? breath * 3 * K : 0, alpha = gasping ? 1 - 0.55f * breath : 1;
            for (int i = 0; i < pieces; i++)
            {
                float x0 = Left + (bx - Left) * i / pieces + (i > 0 ? gap / 2 : 0), x1 = Left + (bx - Left) * (i + 1) / pieces - (i < pieces - 1 ? gap / 2 : 0);
                Thread(x0, x1, mercury with { A = alpha }, (2.4f - (gasping ? breath : 0)) * K);
                Thread(x0, x1, Colors.White with { A = 0.35f * alpha }, 0.5f * K, -0.6f * K);
            }
            DrawCircle(new Vector2(bx, Mid + Wob(bx)), 1.5f * K, mercury with { A = alpha });
            if (cold || _warm) Glow(new Vector2(bx, Mid), 5 * K, colour with { A = 0.3f });
            if (cold) DrawFog(frost, top, foot);
            if (Ghosted(body, target)) DrawGlints(bx, tx, target, top, foot);
        }

        private void TubeLine(float y, Color colour, float width)
        {
            var points = new Vector2[(int)(Tube / 2) + 1];
            for (int i = 0; i < points.Length; i++)
            {
                float x = Left + i * 2;
                points[i] = new Vector2(x, y + Wob(x));
            }
            DrawPolyline(points, colour, width, true);
        }

        private void Thread(float x0, float x1, Color colour, float width, float dy = 0)
        {
            if (x1 - x0 < 0.2f) return;
            var points = new Vector2[13];
            for (int i = 0; i < points.Length; i++)
            {
                float x = x0 + (x1 - x0) * i / 12;
                points[i] = new Vector2(x, Mid + dy + Wob(x));
            }
            DrawPolyline(points, colour, width, true);
        }

        /// <summary>The glass fogging with frost, thickest at its cold end; rime spiking off it.</summary>
        private void DrawFog(float frost, float top, float foot)
        {
            for (int i = 0; i < 150; i++)
            {
                float u = Hash(i * 1.37f), k = Math.Clamp((frost * (1.25f - 0.7f * u) - Hash(i * 3.3f) * 0.6f) * 3, 0, 1);
                if (k > 0)
                    DrawCircle(new Vector2(Left - 2 + u * (Tube + 4), Mid + (Hash(i * 2.11f) - 0.5f) * (2 * TubeR + 0.6f)), 0.35f + Hash(i * 4.7f) * 0.8f, Rime with { A = 0.45f * k });
            }
            for (int i = 0; i < 26; i++)
            {
                float u = Hash(i * 7.9f), k = Math.Clamp((frost * (1.2f - 0.8f * u) - 0.15f) * 3, 0, 1);
                if (k <= 0) continue;
                float x = Left + u * Tube, length = (1 + Hash(i * 8.3f) * 2.6f) * k, lean = (Hash(i * 9.1f) - 0.5f) * 1.6f;
                DrawLine(new Vector2(x, top), new Vector2(x + lean, top - length), Rime with { A = 0.7f * k }, 0.5f, true);
                DrawLine(new Vector2(x, foot), new Vector2(x - lean, foot + length * 0.7f), Rime with { A = 0.5f * k }, 0.5f, true);
            }
        }

        /// <summary>Glints running from the body toward where the place draws it, a tick where they stop; red and quicker past a death.</summary>
        private void DrawGlints(float bx, float tx, float target, float top, float foot)
        {
            bool past = Thermometers.OnScale(target).Past;
            var colour = past ? Danger : ColourAt(target);
            float alpha = past ? 0.55f + 0.45f * MathF.Cos(_t * MathF.PI * 2.2f) : 0.9f, dir = tx >= bx ? 1 : -1, speed = past ? 1.3f : 0.85f;
            var streak = colour.Lerp(Colors.White, 0.4f);
            for (int i = 0; i < 3; i++)
            {
                float p = (_t * speed + i / 3f) % 1, x = bx + (tx - bx) * p, s = MathF.Sin(MathF.PI * p);
                Glow(new Vector2(x, Mid), 4.5f * K, colour with { A = 0.55f * s });
                if (s > 0.01f) DrawLine(new Vector2(x - dir * 5 * K * s, Mid), new Vector2(x, Mid), streak with { A = 0.85f * s }, 1.4f * K, true);
            }
            DrawLine(new Vector2(tx, top - 1.8f), new Vector2(tx, foot + 1.8f), Ink with { A = 0.6f }, 2.4f, true);
            DrawLine(new Vector2(tx, top - 1.6f), new Vector2(tx, foot + 1.6f), colour with { A = alpha }, 1, true);
            if (past) Glow(new Vector2(tx, Mid), 10, Danger with { A = 0.45f * alpha });
        }

        private void Glow(Vector2 at, float radius, Color colour) =>
            DrawTextureRect(PixelArt.Glow, new Rect2(at - new Vector2(radius, radius), new Vector2(2 * radius, 2 * radius)), false, colour);
    }
}
