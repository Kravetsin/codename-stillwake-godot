using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Stillwake.Game.Render;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Shared;

/// <summary>
/// The air row of the vitals (hud-vitals): the oxygen sign in its circle under the charge dial and a piece of light per part of the
/// air, each as long as it holds, lit as far as <see cref="AirMotion"/> shows, drawn smooth as the air-motion pick E has it: the
/// strip laid whole, cut by a soft mask whose ends are rounded, its glow one smooth falloff. Low, it turns red and pulses, its last
/// units shimmering; a hit's lost air stays lit white, a hurt's colour a moment, then drains back; a breath's refill runs a light
/// along it and motes rise; a canister breathed sweeps it white; air breathed again sends a wave across; the edge glows while it
/// moves. Every change eases in and out. The crew's suit gauge reads the sim on its own timer and hands the air here.
/// </summary>
public partial class AirRow : Control
{
    private static readonly Color Sky = new("38bdf8");
    /// <summary>The last units that shimmer while low and dim with the fog.</summary>
    private const double LastUnits = 5;
    /// <summary>How long the sign takes to grey as a canister cools, and to come back.</summary>
    private const double GreyInS = 0.25, GreyOutS = 0.4;
    /// <summary>The most parts the bar draws: lungs, a helmet and food's leave room.</summary>
    private const int Most = 4;
    private const float Top = Vitals.AirY - Vitals.PieceHeight / 2, Height = Vitals.PieceHeight;
    private static readonly Rect2 SignAt = new(Vitals.SignX - Vitals.Icon / 2, Vitals.AirY - Vitals.Icon / 2, Vitals.Icon, Vitals.Icon);
    /// <summary>Where the bar may end, in block units, until the edge's keys are first laid: the pick's reach.</summary>
    private float _end = Vitals.AirReach;

    private readonly Texture2D _icon;
    private readonly Texture2D? _shortIcon;
    private readonly AirMotion _motion = new();
    private readonly Bar _bar;
    private readonly Over _light;
    private readonly List<Control> _spots = [];
    private double _clock, _hurt = -1, _grey;
    private Color _hurtTint = Colors.White;

    /// <param name="icon">The oxygen sign.</param>
    /// <param name="shortIcon">Its picture while <see cref="Short"/>: the red oxygen sign.</param>
    public AirRow(Texture2D icon, Texture2D shortIcon)
    {
        (_icon, _shortIcon) = (icon, shortIcon);
        MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_bar = new Bar());
        // The light added over all: behind the sign, a hurt's or a canister's; the ring's.
        AddChild(_light = new Over(this));
    }

    /// <summary>
    /// The rectangle of the <paramref name="part"/>-th part of the air, for its tip: made on first asking, hidden while the air
    /// has fewer parts.
    /// </summary>
    public Control SpotOf(int part)
    {
        while (_spots.Count <= part)
        {
            var spot = new Control { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
            _spots.Add(spot);
            AddChild(spot);
            MoveChild(spot, 0);
        }
        return _spots[part];
    }

    /// <summary>Whether the suit runs short: the bar and its sign turn red, pulsing.</summary>
    public bool Short
    {
        get => _motion.Short;
        set => _motion.Short = value;
    }

    /// <summary>How far its last units dim, 0 to 1: a low suit's breath (<see cref="Features.Crew.VisorFog"/>).</summary>
    public float LastDim { get; set; }

    /// <summary>How bright its sign and bar are.</summary>
    public float Bright { get; set; } = 1;

    /// <summary>Whether its sign shows grey: a canister cooling (canister-mark).</summary>
    public bool Greyed { get; set; }

    /// <summary>Where its sign is drawn: what a mark round it centres on.</summary>
    public Rect2 IconAt => SignAt;

    /// <summary>Drawn over its sign by the feature it measures for: the canister's ring.</summary>
    public Action<CanvasItem>? DrawMark { get; set; }

    /// <summary>Added over all with the row's own light: the ring's glow.</summary>
    public Action<CanvasItem>? LightMark { get; set; }

    /// <summary>
    /// A read of the hero's air (<see cref="AirMotion.Read"/>): <paramref name="air"/> of <paramref name="capacity"/> units in its
    /// <paramref name="parts"/> (air-segments), the lungs first, each its piece on one scale past a gap; a drop of
    /// <paramref name="hit"/> units a hit, a rise of <paramref name="fill"/> a breath's refill.
    /// </summary>
    public void Read(int air, int capacity, IReadOnlyList<(string Key, int Size)> parts, double hit, int fill) =>
        _motion.Read(air, capacity, parts, hit, fill);

    /// <summary>The row's place, the block's origin on the screen, its size, and where its bar may end (<see cref="Vitals.AirEnd"/>).</summary>
    public void Place(Vector2 origin, float scale, float end)
    {
        Position = origin;
        Scale = new Vector2(scale, scale);
        _end = end;
    }

    /// <summary>A canister breathed: its air sweeps the bar white, motes rise thick, the sign glows sky.</summary>
    public void Surge() => _motion.Surge();

    /// <summary>Room air breathed again: one wave of pale light across the bar, lit or not.</summary>
    public void Wave() => _motion.Wave();

    /// <summary>A hurt (2026-11-08-hurts-are-shown): the air it took stays lit in <paramref name="tint"/>, the kind's colour, lit behind the sign.</summary>
    public void Hurt(Color tint)
    {
        (_hurtTint, _hurt) = (tint, 0);
        _motion.Hurt();
    }

    public override void _Process(double delta)
    {
        _clock += delta;
        if (_hurt >= 0 && (_hurt += delta) > 1) _hurt = -1;
        _grey = Greyed ? Math.Min(1, _grey + delta / GreyInS) : Math.Max(0, _grey - delta / GreyOutS);
        _motion.Step(delta, _end);
        // Its own box as far as its bar reaches, the sign's circle and all.
        Size = new Vector2(_motion.End + StripCuts.Margin, Vitals.Foot);
        int k = 0;
        foreach (var p in _motion.Parts)
        {
            if (!p.Present) continue;
            if (k < _spots.Count) (_spots[k].Position, _spots[k].Size, _spots[k].Visible) = (new Vector2(p.X, Top), new Vector2(Math.Max(4, p.Length), Height), true);
            k++;
        }
        for (; k < _spots.Count; k++) _spots[k].Visible = false;
        _bar.Show(_motion, (float)_clock, LastDim, Bright, _hurtTint);
        QueueRedraw();
        _light.QueueRedraw();
    }

    public override void _Draw()
    {
        float grey = (float)AirMotion.Smooth(_grey), red = (float)_motion.Red;
        if (grey > 0.004f) DrawTextureRect(Render.Greyed.Of(_icon), SignAt, false, Colors.White with { A = 0.8f * grey * Bright });
        float shortAlpha = _shortIcon != null ? red : 0;
        DrawTextureRect(_icon, SignAt, false, Colors.White with { A = (1 - grey) * (1 - shortAlpha) * Bright });
        if (_shortIcon != null && shortAlpha > 0.004f)
            DrawTextureRect(_shortIcon, SignAt, false, Colors.White with { A = (1 - grey) * shortAlpha * Vitals.Pulse(_clock * 2) * Bright });
        DrawMark?.Invoke(this);
    }

    /// <summary>Added over all: light behind the sign for a hurt or a canister, and the ring's.</summary>
    private void DrawLight(CanvasItem on)
    {
        var sign = SignAt.GetCenter();
        if (_hurt >= 0) Glow(on, sign, 24, _hurtTint with { A = 0.45f * (float)AirMotion.Env(_hurt, 0.06, 0, 0.55) * Bright });
        if (_motion.Canister is { } c) Glow(on, sign, 26, Sky with { A = 0.55f * (float)AirMotion.Env(c.Age, 0.08, 0.1, 1.1) * Bright });
        LightMark?.Invoke(on);
    }

    private static void Glow(CanvasItem on, Vector2 at, float radius, Color colour) =>
        on.DrawTextureRect(PixelArt.Glow, new Rect2(at - new Vector2(radius, radius), new Vector2(2 * radius, 2 * radius)), false, colour);

    /// <summary>The light added over all: <see cref="DrawLight"/>.</summary>
    private sealed partial class Over : Control
    {
        private readonly AirRow _row;

        public Over(AirRow row)
        {
            _row = row;
            MouseFilter = MouseFilterEnum.Ignore;
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        }

        public override void _Draw() => _row.DrawLight(this);
    }

    /// <summary>
    /// The bar: one rectangle whose shader draws it all, pixel by pixel. Each piece's dark track; its lit run, the strip laid whole
    /// (<see cref="StripCuts.Column"/>) in sky crossing to red, the events' light inside it, cut by a soft mask that ends in a rounded
    /// tip; a hit's lost air the same, rounded at both ends; the glow of both a blur's smooth falloff, worked out rather than stacked;
    /// the edge's highlight, the wave and the motes added soft. What changes each frame comes in as uniforms, set without allocating.
    /// </summary>
    private sealed partial class Bar : Control
    {
        private static string F(double x) => x.ToString("0.0#####", CultureInfo.InvariantCulture);

        private static readonly string Code = $$"""
            shader_type canvas_item;
            render_mode blend_premul_alpha, unshaded;

            uniform sampler2D sky_strip : filter_linear_mipmap, repeat_disable;
            uniform sampler2D red_strip : filter_linear_mipmap, repeat_disable;
            uniform sampler2D dark_sky : filter_linear_mipmap, repeat_disable;
            uniform sampler2D dark_red : filter_linear_mipmap, repeat_disable;
            uniform sampler2D white_strip : filter_linear_mipmap, repeat_disable;
            // The strip's size in block units; its light's mean colour, premultiplied, for the glow.
            uniform vec2 strip_size;
            uniform vec4 sky_mean;
            uniform vec4 red_mean;
            uniform vec4 white_mean;
            // Each part: its left, its length, how far it is lit; the stretch of a hit's lost air along it.
            uniform vec4 part0; uniform vec4 part1; uniform vec4 part2; uniform vec4 part3;
            uniform vec4 lost0; uniform vec4 lost1; uniform vec4 lost2; uniform vec4 lost3;
            // red, the lit run's alpha (the pulse), brightness, the clock
            uniform vec4 look;
            // the last units' dimming: from, to, how much, as far as
            uniform vec4 dim;
            // a hit's flash, the refill's light (where, how bright), a canister's band (how bright)
            uniform vec4 inner0;
            // a canister's sweep (where, how bright), its band (from, to)
            uniform vec4 inner1;
            // the lost air: how bright, how much its hurt's colour; that colour
            uniform vec4 lost_look;
            uniform vec3 lost_tint;
            // the edge (where, its highlight), the wave (where, how bright)
            uniform vec4 edge;
            // the motes: from, to, how many, how high; how bright
            uniform vec4 motes;
            uniform float mote_alpha;

            const float AIR_Y = {{F(Vitals.AirY)}};
            const float PIECE = {{F(Vitals.PieceHeight)}};
            const float TIP_Y = 8.0;
            const float LONG = {{F(StripCuts.Long)}};
            const float MARGIN = {{F(StripCuts.Margin)}};
            const float CAP = {{F(StripCuts.Cap)}};
            const vec3 SKY200 = vec3(0.729412, 0.901961, 0.992157);

            varying vec2 at;

            void vertex() {
                at = VERTEX;
            }

            // The mask's one profile, across the bar and round its tips alike: full to 0.6, then a smooth fall.
            float tip(float s) {
                if (s <= 0.6) return 1.0;
                if (s >= 1.0) return 0.0;
                if (s < 0.7) return mix(1.0, 0.88, (s - 0.6) * 10.0);
                if (s < 0.8) return mix(0.88, 0.6, (s - 0.7) * 10.0);
                if (s < 0.9) return mix(0.6, 0.25, (s - 0.8) * 10.0);
                return mix(0.25, 0.0, (s - 0.9) * 10.0);
            }

            // Soft light's falloff from its middle to its rim.
            float gauss(float s) {
                if (s >= 1.0) return 0.0;
                if (s < 0.2) return mix(1.0, 0.85, s * 5.0);
                if (s < 0.45) return mix(0.85, 0.45, (s - 0.2) * 4.0);
                if (s < 0.7) return mix(0.45, 0.15, (s - 0.45) * 4.0);
                return mix(0.15, 0.0, (s - 0.7) / 0.3);
            }

            // A soft ellipse of light at c, radii r, at most a bright, at the point q.
            float haze(vec2 q, vec2 c, vec2 r, float a) {
                if (a <= 0.003) return 0.0;
                return clamp(a * gauss(length((q - c) / r)), 0.0, 1.0);
            }

            // StripCuts.Column: the strip's column a point along a piece reads.
            float column(float a, float len) {
                if (len <= LONG) {
                    float cut = max(0.0, len - min(CAP, len * 0.5));
                    return a < cut ? a : a + LONG - len;
                }
                if (a < LONG - CAP) return a;
                if (a >= len - CAP) return a + LONG - len;
                float body = LONG - 2.0 * CAP;
                return CAP + mod(a - (LONG - CAP), body);
            }

            // The strip over a whole piece at the point q, premultiplied; mipmapped by the smooth coordinate's gradients g (x's, then
            // y's), so the body's repeats leave no seam.
            vec4 strip(sampler2D t, vec2 q, vec4 g, float x, float len) {
                float a = q.x - x;
                if (len <= 0.01 || a < -MARGIN || a > len + MARGIN) return vec4(0.0);
                float v = (q.y - (AIR_Y - PIECE * 0.5 - MARGIN)) / strip_size.y;
                if (v < 0.0 || v > 1.0) return vec4(0.0);
                vec4 c = textureGrad(t, vec2((MARGIN + column(a, len)) / strip_size.x, v), g.xy, g.zw);
                return vec4(c.rgb * c.a, c.a);
            }

            // The soft mask at q of a run from `from` to `to` along a piece: the profile across, a rounded tip at its end that slides
            // out past the piece's own end as it nears it; at its start too if soft0, else open to the strip's own start.
            float run_mask(vec2 q, float x, float len, float from, float to, float r, bool soft0) {
                float c1 = x + to - 0.83 * r * (1.0 - smoothstep(0.0, 1.0, (to - (len - r)) / r));
                float c0 = x - 3.0;
                if (soft0) {
                    c0 = x + from + 0.83 * r * (1.0 - smoothstep(0.0, 1.0, (r - from) / r));
                }
                float dy = abs(q.y - AIR_Y) / TIP_Y;
                float m = 0.0;
                if (c1 > c0 && q.x >= c0 && q.x <= c1) {
                    m = tip(dy);
                }
                float e = tip(length(vec2((q.x - c1) / r, dy)));
                m += e * (1.0 - m);
                if (soft0) {
                    e = tip(length(vec2((q.x - c0) / r, dy)));
                    m += e * (1.0 - m);
                }
                return m;
            }

            float phi(float z) {
                return 0.5 + 0.5 * tanh(0.7978845608 * (z + 0.044715 * z * z * z));
            }

            // A band of light at q from a to b along the bar, as tall as a piece, blurred: the wide blur and the near one added.
            float blurred(vec2 q, float a, float b) {
                float y0 = AIR_Y - PIECE * 0.5;
                float y1 = AIR_Y + PIECE * 0.5;
                float wide = (phi((q.x - a) / 3.2) - phi((q.x - b) / 3.2)) * (phi((q.y - y0) / 3.2) - phi((q.y - y1) / 3.2));
                float near = (phi((q.x - a) / 1.2) - phi((q.x - b) / 1.2)) * (phi((q.y - y0) / 1.2) - phi((q.y - y1) / 1.2));
                return 0.5 * wide + 0.32 * near;
            }

            vec4 over(vec4 top, vec4 under) {
                return top + under * (1.0 - top.a);
            }

            vec4 track(vec4 d, vec2 q, vec4 g, vec4 p, float red) {
                d = over(strip(dark_sky, q, g, p.x, p.y) * 0.9, d);
                if (red > 0.004) {
                    d = over(strip(dark_red, q, g, p.x, p.y) * 0.9 * red, d);
                }
                return d;
            }

            // A part's lit run onto d, its glow added to glow.
            void lit(inout vec4 d, inout vec3 glow, vec2 q, vec4 g, vec4 p, float red) {
                if (p.z <= 0.02) return;
                vec4 l = over(strip(red_strip, q, g, p.x, p.y) * red, strip(sky_strip, q, g, p.x, p.y));
                // The events' light inside the lit run, added, cut by its mask with it.
                vec4 add = vec4(0.0);
                bool band = abs(q.y - AIR_Y) <= 9.0;
                if (inner0.x > 0.0 && band && q.x >= p.x - 3.0 && q.x <= p.x + p.y + 3.0) {
                    add += vec4(0.35 * inner0.x);
                }
                add += vec4(SKY200, 1.0) * haze(q, vec2(inner0.y, AIR_Y), vec2(16.0, 7.0), inner0.z);
                add += vec4(1.0) * haze(q, vec2(inner1.x, AIR_Y), vec2(22.0, 8.0), inner1.y);
                if (inner0.w > 0.003 && band) {
                    float ramp = min(6.0, (inner1.w - inner1.z + 12.0) * 0.5);
                    add += vec4(SKY200, 1.0) * inner0.w * clamp(min(q.x - inner1.z + 6.0, inner1.w + 6.0 - q.x) / ramp, 0.0, 1.0);
                }
                l = min(l + add, vec4(1.0));
                if (dim.z > 0.004 && q.x >= dim.x && q.x <= dim.w) {
                    l *= 1.0 - dim.z * clamp((q.x - dim.x) / max(0.001, dim.y - dim.x), 0.0, 1.0);
                }
                l *= run_mask(q, p.x, p.y, 0.0, p.z, 7.0, false) * look.y;
                d = over(l, d);
                vec3 tone = mix(sky_mean.rgb, red_mean.rgb, red) + vec3(0.35 * inner0.x);
                glow += tone * look.y * look.y * blurred(q, p.x, p.x + p.z);
            }

            // A part's stretch of a hit's lost air onto d, its glow added to glow.
            void lost(inout vec4 d, inout vec3 glow, vec2 q, vec4 g, vec4 p, vec4 s) {
                if (s.y - s.x <= 0.0001 || lost_look.x <= 0.004) return;
                vec4 w = strip(white_strip, q, g, p.x, p.y);
                vec4 l = over(vec4(w.rgb * lost_tint, w.a) * lost_look.y, w);
                l *= run_mask(q, p.x, p.y, s.x, s.y, 5.0, true) * lost_look.x;
                d = over(l, d);
                glow += mix(white_mean.rgb, white_mean.rgb * lost_tint, lost_look.y) * 0.4 * lost_look.x * blurred(q, p.x + s.x, p.x + s.y);
            }

            void fragment() {
                vec2 q = at;
                vec4 g = vec4(dFdx(q.x) / strip_size.x, dFdx(q.y) / strip_size.y, dFdy(q.x) / strip_size.x, dFdy(q.y) / strip_size.y);
                float red = look.x;
                vec4 d = vec4(0.0);
                vec3 glow = vec3(0.0);
                d = track(d, q, g, part0, red);
                d = track(d, q, g, part1, red);
                d = track(d, q, g, part2, red);
                d = track(d, q, g, part3, red);
                lit(d, glow, q, g, part0, red);
                lit(d, glow, q, g, part1, red);
                lit(d, glow, q, g, part2, red);
                lit(d, glow, q, g, part3, red);
                lost(d, glow, q, g, part0, lost0);
                lost(d, glow, q, g, part1, lost1);
                lost(d, glow, q, g, part2, lost2);
                lost(d, glow, q, g, part3, lost3);
                vec3 add = glow;
                add += vec3(1.0) * haze(q, vec2(edge.x - 3.0, AIR_Y), vec2(6.0, 3.5), edge.y);
                add += SKY200 * haze(q, vec2(edge.z, AIR_Y), vec2(14.0, 6.0), edge.w);
                if (mote_alpha > 0.004) {
                    for (int k = 0; k < 12; k++) {
                        if (float(k) >= motes.z) {
                            break;
                        }
                        float ph = look.w * 1.2 + float(k) * 0.27;
                        float p = fract(ph);
                        float x = motes.x + fract(float(k) * 0.37 + floor(ph) * 0.618) * (motes.y - motes.x - 2.0);
                        float y = AIR_Y - PIECE * 0.5 - 1.0 - p * motes.w;
                        add += SKY200 * haze(q, vec2(x, y), vec2(1.6), mote_alpha * smoothstep(0.0, 1.0, p / 0.2) * (1.0 - p));
                    }
                }
                COLOR = vec4(d.rgb + add, d.a) * look.z;
            }
            """;

        private static readonly StringName[] PartNames = ["part0", "part1", "part2", "part3"], LostNames = ["lost0", "lost1", "lost2", "lost3"];
        private static readonly StringName LookName = "look", Dim = "dim", Inner0 = "inner0", Inner1 = "inner1", LostLook = "lost_look",
            LostTint = "lost_tint", Edge = "edge", MotesName = "motes", MoteAlpha = "mote_alpha";

        private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Code } };
        private float _end;

        public Bar()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            Material = _material;
            var sky = LightStrip.Of("sky", 0);
            _material.SetShaderParameter("strip_size", sky.GetSize());
            foreach (var (name, tone) in new[] { ("sky_strip", "sky"), ("red_strip", "red"), ("dark_sky", "darkSky"), ("dark_red", "darkRed"), ("white_strip", "white") })
                _material.SetShaderParameter(name, Pixels(LightStrip.Of(tone, 0)));
            foreach (var (name, tone) in new[] { ("sky_mean", "sky"), ("red_mean", "red"), ("white_mean", "white") })
                _material.SetShaderParameter(name, LightStrip.Mean(tone));
        }

        /// <summary>The picture a shader samples: a hi-res picture's own pixels, past the canvas texture that sizes it.</summary>
        private static Texture2D Pixels(Texture2D texture) => texture is CanvasTexture { DiffuseTexture: { } pixels } ? pixels : texture;

        /// <summary>What the bar shows this frame, as pick E draws it, handed to the shader.</summary>
        public void Show(AirMotion m, float clock, float lastDim, float bright, Color hurtTint)
        {
            var parts = m.Parts;
            Visible = parts.Count > 0;
            if (!Visible) return;
            double red = m.Red, t = clock;
            float pulse = 1 + (Vitals.Pulse(t) - 1) * (float)red, start = Vitals.RowLeft, edge = m.EdgeX;
            var can = m.Canister;
            var hit = m.Hit;
            for (int i = 0; i < Most; i++)
            {
                var p = i < parts.Count ? parts[i] : default;
                _material.SetShaderParameter(PartNames[i], new Vector4(p.X, p.Length, p.Lit, 0));
            }
            // What a hit took stays lit, white with the hurt's colour a moment, then drains back to the fill.
            double lostAlpha = 0, tint = 0;
            if (hit is { } h && m.Lost is { } lost && lost > m.Shown + 0.05)
            {
                lostAlpha = 0.9 * AirMotion.Env(h.Age, 0.05, 1.1, 0.4);
                tint = h.Hurt ? AirMotion.Env(h.Age, 0.08, 0.3, 0.35) : 0;
                for (int i = 0; i < Most; i++)
                {
                    var along = i < parts.Count ? m.Along(i, m.Shown, lost) : null;
                    _material.SetShaderParameter(LostNames[i], along is { } ab ? new Vector4(ab.A, ab.B, 0, 0) : Vector4.Zero);
                }
            }
            else
                for (int i = 0; i < Most; i++) _material.SetShaderParameter(LostNames[i], Vector4.Zero);
            _material.SetShaderParameter(LostLook, new Vector4((float)lostAlpha, (float)tint, 0, 0));
            _material.SetShaderParameter(LostTint, new Vector3(hurtTint.R, hurtTint.G, hurtTint.B));
            _material.SetShaderParameter(LookName, new Vector4((float)red, pulse, bright, (float)(t % 1000)));
            // Low, the last units shimmer; the fogged breath dims them too.
            double shimmer = red * 0.6 * (0.5 - 0.5 * Math.Cos(t * Math.PI * 3)), dimmed = 1 - (1 - shimmer) * (1 - lastDim);
            float lastX = m.XOf(m.Shown - LastUnits);
            _material.SetShaderParameter(Dim, new Vector4(lastX - 6, lastX, (float)dimmed, edge + 6));
            float flash = hit is { } struck ? (float)AirMotion.Env(struck.Age, 0.05, 0.05, 0.35) : 0;
            float gain = m.Gain > 0.01 && can is null ? 0.6f * (float)m.Gain : 0;
            float gainX = start - 30 + (float)(t * 0.45 % 1) * (edge - start + 60);
            if (can is { } c)
            {
                float to = m.XOf(c.To), q = (float)Math.Clamp(c.Age / 0.6, 0, 1);
                _material.SetShaderParameter(Inner0, new Vector4(flash, gainX, gain, 0.4f * (float)AirMotion.Env(c.Age, 0.15, 0.3, 1.0)));
                _material.SetShaderParameter(Inner1, new Vector4(start + (to - start) * q * (2 - q), 0.7f * (float)AirMotion.Env(c.Age, 0.08, 0.3, 0.4), m.XOf(c.From), to));
                Motes(start, to, Look.SurgeMotes, Look.SurgeRise, 0.85 * AirMotion.Env(c.Age, 0.15, 0.6, 0.7));
            }
            else
            {
                _material.SetShaderParameter(Inner0, new Vector4(flash, gainX, gain, 0));
                _material.SetShaderParameter(Inner1, Vector4.Zero);
                Motes(start, edge, Look.Motes, Look.MoteRise, 0.7 * m.Gain);
            }
            float wave = m.Again is { } again ? (float)(again / Look.WaveS) : -1;
            _material.SetShaderParameter(Edge, new Vector4(edge, edge > start + 0.5f ? 0.35f * (float)m.Moving : 0,
                start - 20 + (m.End - start + 40) * wave, wave >= 0 ? 0.4f * Mathf.Sin(Mathf.Pi * wave) : 0));
            if (m.End != _end) QueueRedraw();
            _end = m.End;
        }

        private void Motes(float from, float to, int count, int rise, double alpha)
        {
            _material.SetShaderParameter(MotesName, new Vector4(from, to, count, rise));
            _material.SetShaderParameter(MoteAlpha, to - from < 4 ? 0f : (float)alpha);
        }

        /// <summary>One rectangle over all the bar may light: the wave's reach either side, the motes' rise above, the glow's below.</summary>
        public override void _Draw() => DrawRect(new Rect2(Vitals.RowLeft - 36, Vitals.AirY - 23, _end - Vitals.RowLeft + 72, 40), Colors.White);
    }
}
