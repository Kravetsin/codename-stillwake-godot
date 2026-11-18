using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Crew;
using static Stillwake.Game.I18n.En;
using CrewSpec = Stillwake.Content.Crew;
using Foods = Stillwake.Content.Foods;
using Look = Stillwake.Game.WristLook;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The suit without a word (hud-vitals): the air row of the vitals at the screen's bottom-left, the oxygen sign and a piece of
/// light as long as the suit holds, its source named on hover; red and pulsing while the suit runs low as the beep sounds
/// (<see cref="Danger.Low"/>). A suit running low out of room air fogs the visor, its last units dimming with each breath (<see cref="VisorFog"/>; the last
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
    // The oxygen sign the rooms' air is read by (tip-signs); red while low.
    private readonly AirRow _gauge = new(Signs.Of(Sign.Oxygen), RedSign);
    private readonly Func<int> _canisterIn;
    private readonly Func<IReadOnlyList<AirPart>> _parts;
    private IReadOnlyList<AirPart> _shown = [];
    private int _tipped;
    private readonly Ui _ui;
    private readonly TipSpots _tips;
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
    /// <param name="parts">This player's hero's air in its parts (air-segments): the piece's segments, each named on hover.</param>
    /// <param name="ui">The slots' size, which the block's size follows (0058), and the one tooltip.</param>
    public SuitGauge(Func<(int Air, int Capacity, SuitDanger Danger, bool InAir)> read, Func<int> canisterIn, Func<IReadOnlyList<AirPart>> parts, Ui ui)
    {
        (_read, _canisterIn, _parts, _ui) = (read, canisterIn, parts, ui);
        _tips = new TipSpots(ui.Tip);
        ui.Refilled += _gauge.Surge;
        ui.PressedCooling += _refused.Press;
        _gauge.DrawMark = DrawRing;
        _gauge.LightMark = LightRing;
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

    /// <summary>Where the vitals lie on the screen, the whole block: what the markers keep out of.</summary>
    public Rect2 Covers
    {
        get
        {
            var (x, y, w, h) = Vitals.Covers(GetViewport().GetVisibleRect().Size.Y, _ui.Slot, KeysLeft);
            return new Rect2(x, y, w, h);
        }
    }

    /// <summary>Where the edge's full row of keys starts, which the air's bar stops short of; till it is first laid, as at the pick's reach.</summary>
    private float KeysLeft => _ui.EdgeKeysLeft ?? Slots.Margin + (Vitals.AirReach * Vitals.ScaleOf(_ui.Slot)) + Vitals.KeysGap;

    public override void _Process(double delta)
    {
        var (x, y) = Vitals.Origin(GetViewport().GetVisibleRect().Size.Y, _ui.Slot);
        _gauge.Place(new Vector2(x, y), Vitals.ScaleOf(_ui.Slot), Vitals.AirEnd(KeysLeft, _ui.Slot));
        _tips.Frame(!_ui.MenuOpen.Get() ? GetViewport().GetMousePosition() : null);
        _sinceRead += delta;
        if (_sinceRead >= RefreshS)
        {
            (int suit, int capacity, _danger, bool inAir) = _read();
            bool again = _cues.Read(suit, capacity, _danger, inAir, _sinceRead);
            _sinceRead = 0;
            // A helmet put on or off changes the share with nothing lost: shown at once, no white.
            if (_capacity != 0 && capacity != _capacity) _gauge.Motion.Show((double)suit / capacity);
            // A hit is more than the dearest breath, running bare in the cold (a unit of it): bare lungs are small enough for a
            // breath to look like one.
            else _gauge.Motion.Read((double)suit / capacity, Math.Max(WristLook.BigDrop, (CrewSpec.Exerted + 1 + 0.5) / capacity));
            _capacity = _gauge.Units = capacity;
            // Each part of the air its own segment of the same scale, past a gap, named on hover (air-segments).
            _gauge.Parts = [.. (_shown = _parts()).Select(p => p.Size)];
            for (; _tipped < _shown.Count; _tipped++)
            {
                int part = _tipped;
                _tips.Add(_gauge.SpotOf(part), () => TipOf(part));
            }
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

    /// <summary>The <paramref name="part"/>-th part's tip: what holds it, the lungs, the helmet worn or the stimulant, and its units.</summary>
    private Tip? TipOf(int part)
    {
        if (part >= _shown.Count) return null;
        var (key, size) = (_shown[part].Key, _shown[part].Size);
        string name = key == AirPart.Lungs ? T("hud.lungs") : key == AirPart.Food ? T($"item.{Foods.Stim}") : T($"item.{key}");
        return new Tip(T("hud.holds", ("name", name), ("n", size)), []);
    }

    /// <summary>The ring's radius round the oxygen sign, in block units, its circle the dials' (hud-vitals).</summary>
    private const float RingRadius = Vitals.RingRadius;
    private static readonly Color Refused = new(1, 70 / 255f, 60 / 255f);

    /// <summary>
    /// The canister's ring round the oxygen sign (canister-mark, as picked): while it cools a sky arc sweeps clockwise from the
    /// top over a dark ring, a bright dot at its head; asked for too soon, the sign and the arc left to cool blink red and jolt,
    /// the swept arc kept sky; ready, a pale ring flashes and fades.
    /// </summary>
    private void DrawRing(CanvasItem on)
    {
        float bright = _gauge.Bright, red = (float)_refused.Red;
        var jolt = red > 0 ? new Vector2(_refused.Jolt.X, _refused.Jolt.Y) : Vector2.Zero;
        var centre = _gauge.IconAt.GetCenter() + jolt;
        if (_cooling.Cooling)
        {
            var (swept, left) = CanisterCooling.Arcs(_cooling.Passed);
            Ring(on, centre, -Mathf.Pi / 2, 3 * Mathf.Pi / 2, Style.Slate700 with { A = 0.95f * bright });
            Ring(on, centre, swept.From, swept.To, Style.Sky400 with { A = 0.85f * bright });
            if (red > 0) Ring(on, centre, left.From, left.To, Refused with { A = 0.9f * red * bright });
            on.DrawRect(new Rect2(centre + RingAt(swept.To) - Vector2.One, new Vector2(2, 2)), Style.Sky200 with { A = bright });
        }
        else if (_cooling.Ready > 0)
        {
            Ring(on, centre, -Mathf.Pi / 2, 3 * Mathf.Pi / 2, Style.Sky200 with { A = (float)_cooling.Ready * bright });
            if (red > 0) Ring(on, centre, -Mathf.Pi / 2, 3 * Mathf.Pi / 2, Refused with { A = 0.9f * red * bright });
        }
        if (red > 0) on.DrawTextureRect(RedSign, new Rect2(_gauge.IconAt.Position + jolt, _gauge.IconAt.Size), false, Colors.White with { A = red * bright });
    }

    /// <summary>The ring's light, added: red on a refused sign, inside the ring so the sweep keeps its sky; sky as it turns ready; a spark at the sweep's head.</summary>
    private void LightRing(CanvasItem on)
    {
        float bright = _gauge.Bright, red = (float)_refused.Red;
        var centre = _gauge.IconAt.GetCenter() + (red > 0 ? new Vector2(_refused.Jolt.X, _refused.Jolt.Y) : Vector2.Zero);
        if (red > 0) Glow(on, centre, Vitals.Icon / 2, Refused with { A = 0.45f * red * bright });
        if (_cooling.Cooling) Glow(on, centre + RingAt(HandAngle), 4, Style.Sky400 with { A = 0.5f * bright });
        if (_cooling.Ready > 0) Glow(on, centre, 20, Style.Sky400 with { A = 0.55f * (float)_cooling.Ready * bright });
    }

    /// <summary>Where the sweep has reached, clockwise from the top as the screen's y runs down.</summary>
    private float HandAngle => -Mathf.Pi / 2 + Mathf.Tau * (float)_cooling.Passed;

    private static Vector2 RingAt(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * RingRadius;

    /// <summary>An arc of the ring, as thick as the dials' light, in its own colour.</summary>
    private static void Ring(CanvasItem on, Vector2 centre, float start, float end, Color colour) => RingLight.Tinted(on, centre, start, end, colour);

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
