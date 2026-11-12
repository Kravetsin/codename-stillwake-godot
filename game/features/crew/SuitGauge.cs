using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using CrewSpec = Stillwake.Content.Crew;
using Look = Stillwake.Game.WristLook;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The suit without a word (SuitGauge.tsx, 0081): a segmented gauge under the cuff, the oxygen sign for its label, red and
/// pulsing while the suit runs low as the beep sounds (<see cref="Danger.Low"/>). A suit
/// running low out of room air fogs the visor, its last lamp dimming with each breath (<see cref="VisorFog"/>; the last
/// breath and passing out darken it in <see cref="TunnelView"/>), and beeps softly every <see cref="S.AlarmEveryS"/>,
/// silent once passed out; out of danger the fog evaporates. Reads the sim four times a second, never every frame; the
/// gauge moves smoothly between reads (<see cref="GaugeMotion"/>). Air coming back is felt (<see cref="AirCues"/>): a
/// canister breathed surges the gauge; room air breathed again sends a wave across it and a soft inhale through the helmet.
/// While the canister cools the sign greys and a ring sweeps round it (<see cref="DrawRing"/>).
/// </summary>
public partial class SuitGauge : CanvasLayer
{
    private const double RefreshS = 0.25;

    private readonly Func<(int Air, int Capacity, SuitDanger Danger, bool InAir)> _read;
    private static readonly Texture2D RedSign = Signs.Of(Sign.Oxygen, bad: true);
    private readonly WristGauge _gauge = new(
        // The oxygen sign the rooms' air is read by (tip-signs), as tall as the energy tab's bolt beside it; red while low.
        Signs.Of(Sign.Oxygen), new Rect2(4, 4, 22, 22), Look.SuitMeterLeft, Look.SuitCells, AsciiArt.Colour(Look.Suit), RedSign);
    private readonly Func<int> _canisterIn;
    private readonly CanisterCooling _cooling = new();
    private readonly FlatBlink _refused = new();
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
    /// <param name="canisterIn">This player's hero's ticks left before another canister, <c>CanisterIn</c>; none without a hero.</param>
    /// <param name="ui">The slots' size, which the hotbar's size and so the gauge's place and width follow (0058).</param>
    public SuitGauge(Func<(int Air, int Capacity, SuitDanger Danger, bool InAir)> read, Func<int> canisterIn, Ui ui)
    {
        (_read, _canisterIn) = (read, canisterIn);
        ui.Refilled += _gauge.Surge;
        ui.PressedCooling += _refused.Press;
        _gauge.DrawMark = DrawRing;
        _gauge.LightMark = LightRing;
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

    /// <summary>
    /// A hurt of this player's hero (2026-11-08-hurts-are-shown): the sim is read at once, so the loss shows with the hurt, and
    /// blinks in <paramref name="tint"/>, the kind's colour.
    /// </summary>
    public void Hurt(Color tint)
    {
        _sinceRead = RefreshS;
        _gauge.Hurt(tint);
    }

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
            _cooling.Read(_canisterIn());
            _gauge.Short = _danger != SuitDanger.None;
            if (again)
            {
                _gauge.Wave();
                _inhale.Play();
                BreathesAgain?.Invoke();
            }
        }
        _cooling.Step(delta);
        _refused.Step(delta);
        _gauge.Greyed = _cooling.Cooling || _refused.Red > 0;
        Beep(delta);
        var fog = _fog.Advance(delta);
        _fogView.Show(fog);
        _gauge.LastDim = (float)fog.LastDim;
    }

    /// <summary>The ring's radius round the oxygen sign, in tab pixels; how wide it is drawn.</summary>
    private const float RingRadius = 12.5f, RingWidth = 1.5f;
    private static readonly Color Refused = new(1, 70 / 255f, 60 / 255f);

    /// <summary>
    /// The canister's ring round the oxygen sign (canister-mark, as picked): while it cools a sky arc sweeps clockwise from the
    /// top over a dark ring, a bright dot at its head; asked for too soon, the sign and a ring blink red and jolt; ready, a pale
    /// ring flashes and fades.
    /// </summary>
    private void DrawRing(CanvasItem on)
    {
        var centre = _gauge.IconAt.GetCenter();
        float bright = _gauge.Bright;
        if (_refused.Red > 0)
        {
            float red = (float)_refused.Red;
            var jolt = new Vector2(_refused.Jolt.X, _refused.Jolt.Y);
            on.DrawTextureRect(RedSign, new Rect2(_gauge.IconAt.Position + jolt, _gauge.IconAt.Size), false, Colors.White with { A = red * bright });
            Ring(on, centre + jolt, 0, Mathf.Tau, Refused with { A = 0.9f * red * bright });
            return;
        }
        if (_cooling.Cooling)
        {
            float end = HandAngle;
            Ring(on, centre, 0, Mathf.Tau, Style.Slate700 with { A = 0.95f * bright });
            Ring(on, centre, -Mathf.Pi / 2, end, Style.Sky400 with { A = 0.85f * bright });
            on.DrawRect(new Rect2(centre + RingAt(end) - Vector2.One, new Vector2(2, 2)), Style.Sky200 with { A = bright });
            return;
        }
        if (_cooling.Ready > 0) Ring(on, centre, 0, Mathf.Tau, Style.Sky200 with { A = (float)_cooling.Ready * bright });
    }

    /// <summary>The ring's light, added: red round a refused sign, sky as it turns ready, a spark at the sweep's head.</summary>
    private void LightRing(CanvasItem on)
    {
        var centre = _gauge.IconAt.GetCenter();
        float bright = _gauge.Bright;
        if (_refused.Red > 0) Glow(on, centre + new Vector2(_refused.Jolt.X, 0), 20, Refused with { A = 0.45f * (float)_refused.Red * bright });
        else if (_cooling.Cooling) Glow(on, centre + RingAt(HandAngle), 4, Style.Sky400 with { A = 0.5f * bright });
        if (_cooling.Ready > 0) Glow(on, centre, 20, Style.Sky400 with { A = 0.55f * (float)_cooling.Ready * bright });
    }

    /// <summary>Where the sweep has reached, clockwise from the top as the screen's y runs down.</summary>
    private float HandAngle => -Mathf.Pi / 2 + Mathf.Tau * (float)_cooling.Passed;

    private static Vector2 RingAt(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * RingRadius;

    private static void Ring(CanvasItem on, Vector2 centre, float start, float end, Color colour)
    {
        if (end <= start) return;
        on.DrawArc(centre, RingRadius, start, end, Math.Max(3, (int)((end - start) * RingRadius / 1.5f)), colour, RingWidth, true);
    }

    private static void Glow(CanvasItem on, Vector2 at, float radius, Color colour) =>
        on.DrawTextureRect(PixelArt.Glow, new Rect2(at - new Vector2(radius, radius), new Vector2(2 * radius, 2 * radius)), false, colour);

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
