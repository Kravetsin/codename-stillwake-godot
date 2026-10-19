using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using CrewSpec = Stillwake.Content.Crew;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The suit without a word (SuitGauge.tsx): a bar at the top left that turns red and pulses when the
/// suit runs low out of room air, red at the screen's edges then, and the screen going dark on passing
/// out; a soft beep every <see cref="S.AlarmEveryS"/> while it runs low, silent once passed out. Reads
/// the sim four times a second, never every frame.
/// </summary>
public partial class SuitGauge : CanvasLayer
{
    private const double RefreshS = 0.25;
    /// <summary>How long the red edges and the dark take to come and go, as the CSS transitions did.</summary>
    private const double VignetteS = 0.7, DarkS = 1.0;
    private const float Dark = 0.85f;
    // Made once: a string passed each frame would make a StringName each frame for the GC to finalize.
    private static readonly StringName Strength = "strength";

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
    private readonly AudioStreamPlayer _alarm = Cues.Player(S.Alarm, S.Helmet);
    // Due at once: the first beep comes as the suit turns low.
    private double _sinceAlarm = S.AlarmEveryS;

    /// <param name="read">This player's suit and its danger, as <see cref="Danger.Read"/> gives them.</param>
    /// <param name="ui">The slots' size, which the hotbar's height and so the gauge's place follow (0058).</param>
    public SuitGauge(Func<(int Suit, SuitDanger Danger)> read, Ui ui)
    {
        _read = read;
        void Place() => _bar.Position = _bar.Position with { Y = Slots.GaugeTop(ui.Slot) };
        Place();
        ui.SlotScale.Subscribe(Place);
        Layer = ScreenLayers.Hud;
        // The bar first: the red edges and the dark cover it too.
        AddChild(_bar);
        foreach (var screen in new[] { _vignette, _dark })
        {
            screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            screen.MouseFilter = Control.MouseFilterEnum.Ignore;
            AddChild(screen);
        }
        _vignette.Material = _edges;
        AddChild(_alarm);
    }

    /// <summary>Where the bar lies on the screen: what the markers keep out of.</summary>
    public Rect2 Covers => _bar.GetGlobalRect();

    public override void _Process(double delta)
    {
        _sinceRead += delta;
        if (_sinceRead >= RefreshS)
        {
            _sinceRead = 0;
            (int suit, _danger) = _read();
            _bar.Share = (float)suit / CrewSpec.Suit;
            _bar.InDanger = _danger != SuitDanger.None;
        }
        Beep(delta);
        _vignetteShown = Toward(_vignetteShown, _danger == SuitDanger.Low ? 1 : 0, delta / VignetteS);
        _darkShown = Toward(_darkShown, _danger == SuitDanger.Out ? 1 : 0, delta / DarkS);
        _edges.SetShaderParameter(Strength, (float)_vignetteShown);
        _dark.Color = new Color(0, 0, 0, (float)_darkShown * Dark);
        _bar.QueueRedraw();
    }

    private void Beep(double delta)
    {
        if (_danger != SuitDanger.Low)
        {
            _sinceAlarm = S.AlarmEveryS;
            if (_danger == SuitDanger.Out && _alarm.Playing) _alarm.Stop();
            return;
        }
        if ((_sinceAlarm += delta) < S.AlarmEveryS) return;
        _sinceAlarm = 0;
        _alarm.Play();
    }

    private static double Toward(double from, double to, double step) =>
        from < to ? Math.Min(to, from + step) : Math.Max(to, from - step);

    /// <summary>
    /// The bar under its label, drawn by hand on a screen of the wrist computer, in its tab under the cuff (0074): Tailwind's
    /// slate and sky, red pulsing in danger.
    /// </summary>
    private sealed partial class Bar : Control
    {
        private static readonly Color Text = new("94a3b8"), Track = new("334155"), Fill = new("38bdf8"), Alarm = new("ef4444");

        public float Share { get; set; } = 1;
        public bool InDanger { get; set; }
        private double _clock;

        public Bar()
        {
            Position = new Vector2(Slots.Margin, 0);
            Size = new Vector2(224, 30);
            MouseFilter = MouseFilterEnum.Ignore;
            var tab = WristArt.Frame(WristPieces.Gauge, MouseFilterEnum.Ignore);
            AddChild(tab);
            WristArt.Fit(tab, WristPieces.Gauge, Size);
        }

        public override void _Process(double delta) => _clock += delta;

        public override void _Draw()
        {
            WristArt.DrawScreen(this, Size, WristArt.Screen, glare: false);
            var font = Style.Mono;
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
