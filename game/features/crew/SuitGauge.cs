using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using CrewSpec = Stillwake.Content.Crew;
using Look = Stillwake.Game.WristLook;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The suit without a word (SuitGauge.tsx, 0081): a segmented gauge under the cuff, the canister for its label, that
/// turns red and pulses when the suit runs low out of room air, red at the screen's edges then, and the screen going
/// dark on passing out; a soft beep every <see cref="S.AlarmEveryS"/> while it runs low, silent once passed out. Reads
/// the sim four times a second, never every frame; the gauge moves smoothly between reads (<see cref="GaugeMotion"/>).
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
    private readonly WristGauge _gauge = new(
        ItemIcons.Icon(CrewSpec.Canister), new Rect2(8, 7, 16, 16), Look.SuitMeterLeft, Look.SuitCells, 0,
        AsciiArt.Colour(Look.Suit), AsciiArt.Colour(Look.SuitLow), motes: true);
    private readonly ColorRect _vignette = new(), _dark = new() { Color = new Color(0, 0, 0, 0) };
    private readonly ShaderMaterial _edges = new() { Shader = new Shader { Code = VignetteShader } };
    private double _sinceRead = RefreshS, _vignetteShown, _darkShown;
    private SuitDanger _danger;
    private readonly AudioStreamPlayer _alarm = Cues.Player(S.Alarm, S.Helmet);
    // Due at once: the first beep comes as the suit turns low.
    private double _sinceAlarm = S.AlarmEveryS;

    /// <param name="read">This player's suit and its danger, as <see cref="Danger.Read"/> gives them.</param>
    /// <param name="ui">The slots' size, which the hotbar's size and so the gauge's place and width follow (0058).</param>
    public SuitGauge(Func<(int Suit, SuitDanger Danger)> read, Ui ui)
    {
        _read = read;
        void Place()
        {
            var row = Slots.GaugeRowOf(ui.Slot);
            _gauge.Place(new Vector2(row.SuitX, Slots.GaugeTop(ui.Slot)), row.SuitWidth);
        }
        Place();
        ui.SlotScale.Subscribe(Place);
        Layer = ScreenLayers.Hud;
        // The gauge first: the red edges and the dark cover it too.
        AddChild(_gauge);
        foreach (var screen in new[] { _vignette, _dark })
        {
            screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            screen.MouseFilter = Control.MouseFilterEnum.Ignore;
            AddChild(screen);
        }
        _vignette.Material = _edges;
        AddChild(_alarm);
    }

    /// <summary>Where the gauge lies on the screen: what the markers keep out of.</summary>
    public Rect2 Covers => _gauge.GetGlobalRect();

    public override void _Process(double delta)
    {
        _sinceRead += delta;
        if (_sinceRead >= RefreshS)
        {
            _sinceRead = 0;
            (int suit, _danger) = _read();
            _gauge.Motion.Read((double)suit / CrewSpec.Suit);
            _gauge.Low = _danger != SuitDanger.None;
        }
        Beep(delta);
        _vignetteShown = Toward(_vignetteShown, _danger == SuitDanger.Low ? 1 : 0, delta / VignetteS);
        _darkShown = Toward(_darkShown, _danger == SuitDanger.Out ? 1 : 0, delta / DarkS);
        _edges.SetShaderParameter(Strength, (float)_vignetteShown);
        _dark.Color = new Color(0, 0, 0, (float)_darkShown * Dark);
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
}
