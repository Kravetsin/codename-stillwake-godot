using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The held spear's own effects, one a figure (spear-effects, <see cref="SpearFx"/>): plain, the lens and eddies its thrust bends
/// the air with, drawn on this node over the world at the shimmers' layer; with the arc fitted, the arc's light on the held
/// stage: run down the cable on a thrust, charges rolling down it all the while, lightning off the moving blade.
/// </summary>
public partial class SpearLight : Node2D
{
    // The world seen through the haze, moved by its field (pixels of the world along the spear, from the point) and a breath paler.
    private static readonly Shader HazeShader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded;
            uniform sampler2D screen : hint_screen_texture, filter_linear, repeat_disable;
            uniform float lens; uniform float lens_x; uniform float lens_r;
            uniform float eddy; uniform float eddy_x; uniform float eddy_apart; uniform float eddy_r;
            uniform float line_y;
            varying vec2 local;
            void vertex() { local = VERTEX; }
            void fragment() {
                vec2 f = vec2(0.0);
                float pale = 0.0;
                vec2 v = local - vec2(lens_x, 0.0);
                float m = lens * exp(-dot(v, v) / (lens_r * lens_r));
                f -= v * 0.5 * m;
                pale += 0.1 * m;
                for (int i = 0; i < 2; i++) {
                    float side = i == 0 ? -1.0 : 1.0;
                    v = local - vec2(eddy_x, line_y + side * eddy_apart);
                    m = eddy * exp(-dot(v, v) / (eddy_r * eddy_r));
                    f += vec2(-v.y, v.x) * 0.95 * m * -side;
                    pale += 0.08 * m;
                }
                vec2 shift = inverse(mat2(dFdx(local), dFdy(local))) * f;
                vec4 seen = texture(screen, SCREEN_UV - shift * SCREEN_PIXEL_SIZE);
                COLOR = vec4(mix(seen.rgb, vec3(0.871, 0.898, 0.941), pale), clamp(pale * 40.0, 0.0, 1.0) * seen.a);
            }
            """,
    };

    /// <summary>The arc's violet and its white core (the pick pages').</summary>
    private static readonly (int, int, int) Tint = (180, 154, 255), Core = (239, 233, 218);
    /// <summary>How often an arc's shape is rolled anew, ms: each lives as long.</summary>
    private const double RollMs = 33;

    private readonly ShaderMaterial _material = new() { Shader = HazeShader };
    private readonly SpearWake _wake = new();
    private readonly Random _random = new();
    private readonly List<(double X, double Y)> _slice = [];
    private readonly (double X, double Y)[] _points = new (double, double)[64];
    private Rect2 _box;
    private double _rollMs, _passes;
    private Func<(double X, double Y), Vector2>? _held;
    private readonly Func<(double X, double Y), (double X, double Y)> _inWorld;

    public SpearLight()
    {
        (Material, TopLevel, Visible) = (_material, true, false);
        (ZIndex, ZAsRelative) = (Layers.Shimmer, false);
        _inWorld = p => { var w = _held!(p); return (w.X, w.Y); };
    }

    /// <summary>
    /// <paramref name="elapsedMs"/> on: the spear held where <paramref name="held"/> takes its picture into the world, none while
    /// it is not in hand; <paramref name="jabMs"/> into a thrust, none between; the arc fitted or not; the hero running by
    /// <paramref name="run"/> (0 to 1); <paramref name="stage"/> the held stage its arcs are drawn on.
    /// </summary>
    public void Step(double elapsedMs, Func<(double X, double Y), Vector2>? held, double? jabMs, bool arc, double run, Stage stage)
    {
        _rollMs += elapsedMs;
        bool roll = _rollMs >= RollMs;
        if (roll) _rollMs %= RollMs;
        Haze(held, arc ? null : jabMs is { } ms ? SpearFx.HazeAt(ms) : null);
        _held = held;
        _wake.Step(elapsedMs, held != null && arc ? _inWorld : null);
        foreach (var m in _wake.Motes)
        {
            stage.Flares.Bloom(m.X, m.Y, 1.8, Tint, m.Alpha * Fx.Glow);
            stage.Flares.Bloom(m.X, m.Y, 0.5, Core, 1.4 * m.Alpha * Fx.Glow);
        }
        if (held == null || !arc) return;
        _passes += SpearFx.PassesIn(elapsedMs, run);
        if (jabMs is { } jab && SpearFx.JabAt(jab) is { } j) Jab(j, held, stage, roll);
        Charges(held, stage, roll);
        if (roll) Wake(held, stage.Bolts);
    }

    /// <summary>
    /// The charges rolling down the cable all the while: an arc over the stretch each lights, a glow at its head, and off the
    /// point, as it leaves, a spark of an arc into the air.
    /// </summary>
    private void Charges(Func<(double X, double Y), Vector2> held, Stage stage, bool roll)
    {
        for (int lane = 0; lane < SpearFx.ChargeLanes; lane++)
        {
            if (SpearFx.ChargeAt(lane, _passes) is not var (head, alpha)) continue;
            double a = alpha * Fx.Glow;
            if (head <= 1)
            {
                var at = held(SpearFx.Along(head));
                stage.Flares.Bloom(at.X, at.Y, 5, Tint, 0.45 * a);
                stage.Flares.Bloom(at.X, at.Y, 1.6, Core, 0.8 * a);
                if (roll) stage.Bolts.Add(Along(held, Math.Max(0, head - SpearFx.ChargeLength), head), 2, a, 0.9, tint: BoltTint.Violet);
            }
            else if (roll) Spit(held, stage.Bolts, a, 0.6);
        }
    }

    /// <summary>
    /// Lightning off the moving blade to where its points were a moment ago, branching; and now and then an arc leaping off its
    /// edge into the air. Nothing off a blade at rest.
    /// </summary>
    private void Wake(Func<(double X, double Y), Vector2> held, Bolts bolts)
    {
        double s = _wake.Strength;
        if (s <= 0.05) return;
        for (int i = 0; i < SpearWake.Points.Length; i++)
            foreach (var (lagMs, share) in Lags)
                if (_wake.Behind(i, lagMs) is { } was)
                {
                    var from = SpearWake.Points[i].At;
                    bolts.Add(() => { var a = held(from); return (a.X, a.Y, was.X, was.Y, 0.2); }, 2, s * share * SpearWake.Points[i].Weight * Fx.Glow, 0.8, branchy: true, tint: BoltTint.Violet);
                }
        if (_random.NextDouble() < 0.5 * s) Spit(held, bolts, s, 1);
    }

    /// <summary>How far back the wake's lightning reaches, ms, and how bright each reach is.</summary>
    private static readonly (double Ms, double Share)[] Lags = [(55, 1), (110, 0.6)];

    /// <summary>An arc leaping off the blade's last stretch into the air, out of either edge, <paramref name="far"/> times a few pixels long.</summary>
    private void Spit(Func<(double X, double Y), Vector2> held, Bolts bolts, double intensity, double far)
    {
        var from = SpearFx.Along(0.8 + 0.2 * _random.NextDouble());
        double a = (_random.NextDouble() < 0.5 ? -1 : 1) * Math.PI * (0.25 + 0.4 * _random.NextDouble()), l = far * (2 + 3 * _random.NextDouble()) / HeldLook.Spear.Size;
        bolts.Add(Ends(held, from, (from.X + Math.Cos(a) * l, from.Y + Math.Sin(a) * l), 0.3), 2, intensity, 0.6, tint: BoltTint.Violet);
    }

    /// <summary>The thrust's lens and eddies, laid along the spear from its point; hidden while there are none.</summary>
    private void Haze(Func<(double X, double Y), Vector2>? held, SpearFx.Haze? haze)
    {
        Visible = held != null && haze != null;
        if (!Visible) return;
        var h = haze!.Value;
        var look = HeldLook.Spear;
        Vector2 o = held!((0, 0)), x = (held((1, 0)) - o).Normalized(), y = (held((0, 1)) - o).Normalized();
        GlobalTransform = new Transform2D(x, y, held(look.EdgeTo));
        float line = (float)((look.Grip.Y - look.EdgeTo.Y) * look.Size), reach = (float)(h.EddyRadius * 2.6);
        float left = (float)Math.Min(h.EddyX - h.EddyRadius * 2.6, h.LensX - h.LensRadius * 2.6), right = (float)(h.LensX + h.LensRadius * 2.6);
        float top = (float)Math.Min(-h.LensRadius * 2.6, line - h.EddyApart - reach), bottom = (float)Math.Max(h.LensRadius * 2.6, line + h.EddyApart + reach);
        _box = new Rect2(left, top, right - left, bottom - top);
        foreach (var (name, value) in new (string, double)[]
        {
            ("lens", h.Lens), ("lens_x", h.LensX), ("lens_r", h.LensRadius), ("eddy", h.Eddy), ("eddy_x", h.EddyX),
            ("eddy_apart", h.EddyApart), ("eddy_r", h.EddyRadius), ("line_y", line),
        })
            _material.SetShaderParameter(name, (float)value);
        QueueRedraw();
    }

    public override void _Draw() => DrawRect(_box, Colors.White);

    /// <summary>The arc on a thrust: the coil's glow and flick, the arc running down the cable lit behind it, arcs spat off the point.</summary>
    private void Jab(SpearFx.Jab j, Func<(double X, double Y), Vector2> held, Stage stage, bool roll)
    {
        if (j.Charge > 0)
        {
            var box = held(SpearFx.Box);
            var tube = held(((SpearFx.TubeFrom.X + SpearFx.TubeTo.X) / 2, (SpearFx.TubeFrom.Y + SpearFx.TubeTo.Y) / 2));
            stage.Flares.Bloom(box.X, box.Y, 9, Tint, 0.28 * j.Charge * Fx.Glow);
            stage.Flares.Bloom(tube.X, tube.Y, 4.4, Core, 0.45 * j.Charge * Fx.Glow);
        }
        if (roll && j.Flicks) stage.Bolts.Add(Ends(held, SpearFx.TubeFrom, SpearFx.TubeTo, 0.3), 2, 0.7 * j.Charge, 0.4, tint: BoltTint.Violet);
        if (j.Lit > 0)
        {
            SpearFx.Slice(0, j.Head, 0.7, _slice);
            int n = Math.Min(_slice.Count, _points.Length);
            // Head first: the trail is brightest where the arc is and dies back toward the coil.
            for (int i = 0; i < n; i++)
            {
                var p = held(_slice[_slice.Count - 1 - i]);
                _points[i] = (p.X, p.Y);
            }
            stage.Flares.Trail(_points.AsSpan(0, n), Tint, 0.6, 0.6 * j.Lit * Fx.Glow, 0.4);
        }
        if (roll && j.Running) stage.Bolts.Add(Along(held, Math.Max(0, j.Head - SpearFx.RunLength), j.Head), 2, 1, 0.8, branchy: true, tint: BoltTint.Violet);
        if (!roll || j.Spits <= 0) return;
        var tip = HeldLook.Spear.EdgeTo;
        for (int i = 0; i < 3; i++)
        {
            // Two off the point ahead, one back over the blade; picture pixels are three quarters of the world's.
            double a = Math.PI * (_random.NextDouble() * 1.4 - 0.7) + (i == 2 ? Math.PI : 0), l = (1.5 + _random.NextDouble() * 2) / HeldLook.Spear.Size;
            stage.Bolts.Add(Ends(held, tip, (tip.X + Math.Cos(a) * l, tip.Y + Math.Sin(a) * l), 0.25), 2, j.Spits, 0.4, tint: BoltTint.Violet);
        }
    }

    /// <summary>An arc along the cable from <paramref name="from"/> to <paramref name="to"/> of it, wherever the spear is drawn when asked.</summary>
    private static BoltEnds Along(Func<(double X, double Y), Vector2> held, double from, double to) =>
        Ends(held, SpearFx.Along(from), SpearFx.Along(to), 0.15);

    /// <summary>An arc's ends at two points of the spear's picture, wherever it is drawn when asked.</summary>
    private static BoltEnds Ends(Func<(double X, double Y), Vector2> held, (double X, double Y) from, (double X, double Y) to, double bulge) => () =>
    {
        var (a, b) = (held(from), held(to));
        return (a.X, a.Y, b.X, b.Y, bulge);
    };
}
