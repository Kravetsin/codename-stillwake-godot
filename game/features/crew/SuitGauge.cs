using System;
using Godot;
using Suits = Stillwake.Content.Crew;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The suit without a word (SuitGauge.tsx): a bar at the top left that turns red and pulses when the
/// suit runs low out of room air, red at the screen's edges then, and the screen going dark on passing
/// out. Reads the sim four times a second, never every frame.
/// </summary>
public partial class SuitGauge : CanvasLayer
{
    private const double RefreshS = 0.25;
    /// <summary>How long the red edges and the dark take to come and go, as the CSS transitions did.</summary>
    private const double VignetteS = 0.7, DarkS = 1.0;
    private const float Dark = 0.85f;

    private const string VignetteShader = """
        shader_type canvas_item;
        uniform float strength = 0.0;
        // radial-gradient(ellipse at center, transparent 55%, rgba(220, 38, 38, 0.45) 100%): 1 at the corners.
        void fragment() {
            float r = length((UV - 0.5) * 2.0) / sqrt(2.0);
            COLOR = vec4(220.0 / 255.0, 38.0 / 255.0, 38.0 / 255.0, clamp((r - 0.55) / 0.45, 0.0, 1.0) * 0.45 * strength);
        }
        """;

    private readonly Func<(int Suit, SuitDanger Danger)> _read;
    private readonly Bar _bar = new();
    private readonly ColorRect _vignette = new(), _dark = new() { Color = new Color(0, 0, 0, 0) };
    private readonly ShaderMaterial _edges = new() { Shader = new Shader { Code = VignetteShader } };
    private double _sinceRead = RefreshS, _vignetteShown, _darkShown;
    private SuitDanger _danger;

    /// <param name="read">This player's suit and its danger, as <see cref="Danger.Read"/> gives them.</param>
    public SuitGauge(Func<(int Suit, SuitDanger Danger)> read)
    {
        _read = read;
        // The bar first: the red edges and the dark cover it too.
        AddChild(_bar);
        foreach (var screen in new[] { _vignette, _dark })
        {
            screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            screen.MouseFilter = Control.MouseFilterEnum.Ignore;
            AddChild(screen);
        }
        _vignette.Material = _edges;
    }

    public override void _Process(double delta)
    {
        _sinceRead += delta;
        if (_sinceRead >= RefreshS)
        {
            _sinceRead = 0;
            (int suit, _danger) = _read();
            _bar.Share = (float)suit / Suits.Suit;
            _bar.InDanger = _danger != SuitDanger.None;
        }
        _vignetteShown = Toward(_vignetteShown, _danger == SuitDanger.Low ? 1 : 0, delta / VignetteS);
        _darkShown = Toward(_darkShown, _danger == SuitDanger.Out ? 1 : 0, delta / DarkS);
        _edges.SetShaderParameter("strength", (float)_vignetteShown);
        _dark.Color = new Color(0, 0, 0, (float)_darkShown * Dark);
        _bar.QueueRedraw();
    }

    private static double Toward(double from, double to, double step) =>
        from < to ? Math.Min(to, from + step) : Math.Max(to, from - step);

    /// <summary>The bar under its label, drawn by hand: Tailwind's slate and sky, red pulsing in danger.</summary>
    private sealed partial class Bar : Control
    {
        private static readonly Color Back = new(new Color("0f172a"), 0.9f), Edge = new("334155"),
            Text = new("94a3b8"), Track = new("334155"), Fill = new("38bdf8"), Alarm = new("ef4444");

        public float Share { get; set; } = 1;
        public bool InDanger { get; set; }
        private double _clock;

        public Bar()
        {
            Position = new Vector2(12, 64);
            Size = new Vector2(224, 30);
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Process(double delta) => _clock += delta;

        public override void _Draw()
        {
            var box = new Rect2(Vector2.Zero, Size);
            DrawRect(box, Back);
            DrawRect(box, Edge, false, 1);
            var font = ThemeDB.FallbackFont;
            DrawString(font, new Vector2(12, 20), "O₂", HorizontalAlignment.Left, -1, 12, Text);
            var track = new Rect2(40, 12, Size.X - 52, 6);
            DrawRect(track, Track);
            // animate-pulse: down to half and back every two seconds.
            float pulse = InDanger ? 0.75f + 0.25f * Mathf.Cos((float)(_clock * Math.PI)) : 1;
            var color = InDanger ? Alarm : Fill;
            DrawRect(track with { Size = new Vector2(track.Size.X * Math.Clamp(Share, 0, 1), track.Size.Y) }, color with { A = pulse });
        }
    }
}
