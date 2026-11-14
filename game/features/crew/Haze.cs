using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The heat past a body's high, without a word (2026-11-14-heat-limits, the user's pick C): the world at the screen's edges
/// shimmers, its rows pushed sideways by a rising wave, faintly warmed, and sparks drift up from the bottom and the sides; the
/// shimmer reaches in and the sparks thicken as the heat deepens. A full-screen quad over the world for the shimmer and the sparks
/// drawn over it, under the HUD; hidden while there is none. Reads the heat (<see cref="Report.Heat"/>) four times a second and
/// eases toward it, as the frost does.
/// </summary>
public partial class Haze : CanvasLayer
{
    private const double RefreshS = 0.25;
    /// <summary>How long the haze takes to grow or fade by the whole of its range.</summary>
    private const double EaseS = 1.0;
    private static readonly StringName Strength = "strength";

    private const string Shader = """
        shader_type canvas_item;
        uniform float strength = 0.0;
        uniform sampler2D screen : hint_screen_texture, filter_linear, repeat_disable;

        void fragment() {
            vec2 size = 1.0 / SCREEN_PIXEL_SIZE;
            float e = strength, y = FRAGCOORD.y * 600.0 / size.y;
            // The rows pushed sideways by a wave rising with time, kept at the edges: reaching in as the heat deepens.
            float off = 4.0 * e * (sin(y * 0.045 + TIME * 5.2) + 0.55 * sin(y * 0.13 - TIME * 3.3)) * size.y / 600.0;
            float r = length((SCREEN_UV - 0.5) * 2.0), inner = mix(1.05, 0.5, e);
            vec3 hot = mix(texture(screen, SCREEN_UV + vec2(off * SCREEN_PIXEL_SIZE.x, 0.0)).rgb, vec3(1.0, 0.6, 0.29), 0.04 * e);
            COLOR = vec4(hot, smoothstep(inner, inner + 0.35, r));
        }
        """;

    private readonly Func<float> _read;
    private readonly ColorRect _screen = new();
    private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Shader } };
    private readonly Sparks _sparks = new();
    private double _sinceRead = RefreshS, _shown;
    private float _heat;

    /// <param name="read">How hot this player's hero is, 0 to 1: <see cref="Report.Heat"/> at its body's cell.</param>
    public Haze(Func<float> read)
    {
        _read = read;
        Layer = ScreenLayers.Haze;
        foreach (var c in new Control[] { _screen, _sparks })
        {
            c.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            c.MouseFilter = Control.MouseFilterEnum.Ignore;
            c.Visible = false;
            AddChild(c);
        }
        _screen.Material = _material;
    }

    public override void _Process(double delta)
    {
        _sinceRead += delta;
        if (_sinceRead >= RefreshS)
        {
            _sinceRead = 0;
            _heat = _read();
        }
        _shown = _shown < _heat ? Math.Min(_heat, _shown + delta / EaseS) : Math.Max(_heat, _shown - delta / EaseS);
        bool on = _shown > 0;
        if (on && !_sparks.Visible) _sparks.Wake((float)_shown);
        _screen.Visible = _sparks.Visible = on;
        if (!on) return;
        _material.SetShaderParameter(Strength, (float)_shown);
        _sparks.Step(delta, (float)_shown);
    }

    /// <summary>
    /// The sparks, drawn added: each rises from its own place along the bottom or a side, again and again. How many fly follows the
    /// heat, but a spark joins or leaves only as it starts a new rise, never mid-flight.
    /// </summary>
    private sealed partial class Sparks : Control
    {
        private const int Most = 90;
        /// <summary>The screen height the sparks' sizes are given for; they scale with the screen.</summary>
        private const float Tall = 600;
        private static readonly Color Pale = new("ffe0a0"), Ember = new("ff6020");
        private static readonly Spark[] All = Make();
        private readonly bool[] _flying = new bool[Most];
        private readonly float[] _phase = new float[Most];
        private double _clock;
        private float _heat;

        private readonly record struct Spark(int Side, float At, float Life, float Seed, float Hue, float Rise, float Size);

        public Sparks() => Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };

        private static int Count(float heat) => (int)(Most * Math.Min(1, 0.15f + heat));

        /// <summary>Shown again: as many flying as the heat calls for, each where its rise has got to.</summary>
        public void Wake(float heat)
        {
            int n = Count(heat);
            for (int i = 0; i < Most; i++) (_flying[i], _phase[i]) = (i < n, PhaseOf(i));
        }

        public void Step(double delta, float heat)
        {
            (_clock, _heat) = (_clock + delta, heat);
            int n = Count(heat);
            for (int i = 0; i < Most; i++)
            {
                float phase = PhaseOf(i);
                if (phase < _phase[i]) _flying[i] = i < n;
                _phase[i] = phase;
            }
            QueueRedraw();
        }

        private float PhaseOf(int i) => (float)((_clock / All[i].Life + All[i].Seed) % 1);

        public override void _Draw()
        {
            var size = Size;
            float k = size.Y / Tall, t = (float)_clock;
            for (int i = 0; i < Most; i++)
            {
                if (!_flying[i]) continue;
                var s = All[i];
                float ph = _phase[i], up = ph * size.Y * s.Rise, sway = MathF.Sin(t * 1.6f + s.Seed * 20);
                var at = s.Side == 0
                    ? new Vector2((s.At < 0.5f ? s.At * 0.5f : 1 - (1 - s.At) * 0.5f) * size.X, size.Y + 6 * k - up)
                    : new Vector2(s.Side == 1 ? s.At * size.X * 0.08f : size.X - s.At * size.X * 0.08f, size.Y * (0.55f + 0.45f * s.Seed) - up * 0.8f);
                at.X += sway * 10 * ph * k;
                float alpha = (1 - ph) * Math.Min(1, ph * 8) * (0.45f + 0.55f * _heat) * (0.7f + 0.3f * MathF.Sin(t * 20 + s.Seed * 40));
                if (alpha <= 0.01f) continue;
                var colour = Pale.Lerp(Ember, s.Hue);
                float width = s.Size * k, glow = width * 4;
                DrawTextureRect(PixelArt.Glow, new Rect2(at - new Vector2(glow, glow), new Vector2(2 * glow, 2 * glow)), false, colour with { A = alpha * 0.35f });
                DrawLine(at, at + new Vector2(-sway * 1.5f * k, 5 * k + width * 2), colour with { A = alpha }, width, true);
            }
        }

        private static float Hash(float n)
        {
            double h = Math.Sin(n * 127.1 + 311.7) * 43758.5453;
            return (float)(h - Math.Floor(h));
        }

        private static Spark[] Make()
        {
            var all = new Spark[Most];
            for (int i = 0; i < Most; i++)
                all[i] = new(Hash(i * 1.7f) < 0.6f ? 0 : Hash(i * 2.9f) < 0.5f ? 1 : 2, Hash(i * 3.3f), 2.2f + Hash(i * 5.1f) * 2.6f, Hash(i * 7.7f),
                    Hash(i * 9.3f), 0.35f + Hash(i * 4.4f) * 0.35f, 1.4f + Hash(i * 6.6f) * 1.8f);
            return all;
        }
    }
}
