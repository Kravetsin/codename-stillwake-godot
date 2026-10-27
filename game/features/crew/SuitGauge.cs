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
/// Air coming back is felt (<see cref="AirCues"/>): a canister breathed surges the gauge; room air breathed again sends a
/// wave across it and a soft inhale through the helmet; a low suit out of danger lets the red edges go in a pale flash.
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
        uniform vec4 tint = vec4(0.8627, 0.149, 0.149, 0.45);
        // radial-gradient(ellipse at center, transparent 55%, rgba(220, 38, 38, 0.45) 100%): 1 at the corners.
        void fragment() {
            float r = length((UV - 0.5) * 2.0) / sqrt(2.0);
            COLOR = vec4(tint.rgb, clamp((r - 0.55) / 0.45, 0.0, 1.0) * tint.a * strength);
        }
        """;

    private static readonly StringName Tint = "tint";

    private readonly Func<(int Air, int Capacity, SuitDanger Danger, bool InAir)> _read;
    private readonly WristGauge _gauge = new(
        ItemIcons.Icon(CrewSpec.Canister), new Rect2(8, 7, 16, 16), Look.SuitMeterLeft, Look.SuitCells,
        AsciiArt.Colour(Look.Suit), AsciiArt.Colour(Look.SuitLow));
    private readonly ColorRect _vignette = new(), _letGo = new(), _dark = new() { Color = new Color(0, 0, 0, 0) };
    private static readonly Shader Edges = new() { Code = VignetteShader };
    private readonly ShaderMaterial _edges = new() { Shader = Edges }, _relief = new() { Shader = Edges };
    private double _sinceRead = RefreshS, _vignetteShown, _darkShown;
    /// <summary>How long since the danger let go, while its pale edge shows; below 0 otherwise.</summary>
    private double _letGoS = -1;
    private SuitDanger _danger;
    private readonly AirCues _cues = new();
    private readonly AudioStreamPlayer _alarm = Cues.Player(S.Alarm, S.Helmet), _inhale = Cues.Player(S.Inhale, S.Helmet);
    // Due at once: the first beep comes as the suit turns low.
    private double _sinceAlarm = S.AlarmEveryS;

    /// <param name="read">This player's suit and its danger, as <see cref="Danger.Read"/> gives them.</param>
    /// <param name="ui">The slots' size, which the hotbar's size and so the gauge's place and width follow (0058).</param>
    public SuitGauge(Func<(int Air, int Capacity, SuitDanger Danger, bool InAir)> read, Ui ui)
    {
        _read = read;
        ui.Refilled += _gauge.Surge;
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
        foreach (var screen in new[] { _vignette, _letGo, _dark })
        {
            screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            screen.MouseFilter = Control.MouseFilterEnum.Ignore;
            AddChild(screen);
        }
        _vignette.Material = _edges;
        _letGo.Material = _relief;
        var pale = AsciiArt.Colour(Look.LetGo);
        _relief.SetShaderParameter(Tint, new Color(pale, Look.LetGoAlpha));
        _relief.SetShaderParameter(Strength, 0f);
        AddChild(_alarm);
        AddChild(_inhale);
    }

    /// <summary>This player's hero breathes room air again, out of thin air or vacuum: the hero's visor puffs.</summary>
    public event Action? BreathesAgain;

    /// <summary>Where the gauge lies on the screen: what the markers keep out of.</summary>
    public Rect2 Covers => _gauge.GetGlobalRect();

    public override void _Process(double delta)
    {
        _sinceRead += delta;
        if (_sinceRead >= RefreshS)
        {
            (int suit, int capacity, _danger, bool inAir) = _read();
            var back = _cues.Read(suit, capacity, _danger, inAir, _sinceRead);
            _sinceRead = 0;
            // A hit is more than the dearest breath, running bare in the cold: bare lungs are small enough for a breath to look like one.
            _gauge.Motion.Read((double)suit / capacity, Math.Max(WristLook.BigDrop, (CrewSpec.Exerted + CrewSpec.BareCold + 0.5) / capacity));
            _gauge.Low = _danger != SuitDanger.None;
            if (back.BreathesAgain)
            {
                _gauge.Wave();
                _inhale.Play();
                BreathesAgain?.Invoke();
            }
            if (back.LetsGo) _letGoS = 0;
        }
        Beep(delta);
        LetGo(delta);
        // Letting go, the red gives way at once to the pale edge rather than fading under it.
        double redS = _letGoS >= 0 ? Look.LetGoInS : VignetteS;
        _vignetteShown = Toward(_vignetteShown, _danger == SuitDanger.Low ? 1 : 0, delta / redS);
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

    /// <summary>The pale edge as the danger lets go: up over <see cref="Look.LetGoInS"/>, then fading out, eased, over <see cref="Look.LetGoOutS"/>.</summary>
    private void LetGo(double delta)
    {
        if (_letGoS < 0) return;
        _letGoS += delta;
        double t = _letGoS, shown = t < Look.LetGoInS ? t / Look.LetGoInS : 1 - (t - Look.LetGoInS) / Look.LetGoOutS;
        if (shown <= 0 || _danger != SuitDanger.None) (_letGoS, shown) = (-1, 0);
        _relief.SetShaderParameter(Strength, (float)(t < Look.LetGoInS ? shown : shown * shown));
    }

    private static double Toward(double from, double to, double step) =>
        from < to ? Math.Min(to, from + step) : Math.Max(to, from - step);
}
