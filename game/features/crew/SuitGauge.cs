using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using CrewSpec = Stillwake.Content.Crew;
using Look = Stillwake.Game.WristLook;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The suit without a word (SuitGauge.tsx, 0081): a segmented gauge under the cuff, the canister for its label. A suit
/// running low out of room air fogs the visor, its last lamp dimming with each breath (<see cref="VisorFog"/>; the last
/// breath and passing out darken it in <see cref="TunnelView"/>), and beeps softly every <see cref="S.AlarmEveryS"/>,
/// silent once passed out; out of danger the fog evaporates. Reads the sim four times a second, never every frame; the
/// gauge moves smoothly between reads (<see cref="GaugeMotion"/>). Air coming back is felt (<see cref="AirCues"/>): a
/// canister breathed surges the gauge; room air breathed again sends a wave across it and a soft inhale through the helmet.
/// </summary>
public partial class SuitGauge : CanvasLayer
{
    private const double RefreshS = 0.25;

    private readonly Func<(int Air, int Capacity, SuitDanger Danger, bool InAir)> _read;
    private readonly WristGauge _gauge = new(
        // The oxygen sign the rooms' air is read by (tip-signs), as tall as the energy tab's bolt beside it.
        Signs.Of(Sign.Oxygen), new Rect2(6, 6, 18, 18), Look.SuitMeterLeft, Look.SuitCells,
        AsciiArt.Colour(Look.Suit));
    private readonly VisorFog _fog = new();
    private readonly VisorFogView _fogView = new();
    private double _sinceRead = RefreshS;
    private int _capacity;
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
        AddChild(_gauge);
        AddChild(_fogView);
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
            bool again = _cues.Read(suit, capacity, _danger, inAir, _sinceRead);
            _sinceRead = 0;
            // A helmet put on or off changes the share with nothing lost: shown at once, no white.
            if (_capacity != 0 && capacity != _capacity) _gauge.Motion.Show((double)suit / capacity);
            // A hit is more than the dearest breath, running bare in the cold: bare lungs are small enough for a breath to look like one.
            else _gauge.Motion.Read((double)suit / capacity, Math.Max(WristLook.BigDrop, (CrewSpec.Exerted + CrewSpec.BareCold + 0.5) / capacity));
            _capacity = capacity;
            _fog.Read(_danger, (double)suit / capacity);
            if (again)
            {
                _gauge.Wave();
                _inhale.Play();
                BreathesAgain?.Invoke();
            }
        }
        Beep(delta);
        var fog = _fog.Advance(delta);
        _fogView.Show(fog);
        _gauge.LastDim = (float)fog.LastDim;
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
}
