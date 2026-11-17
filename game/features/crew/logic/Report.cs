using System;
using System.Collections.Generic;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Players;
using Stillwake.Sim.Rooms;

namespace Stillwake.Game.Features.Crew;

/// <summary>Telling the sim where this machine's hero is (apps/game/src/features/crew/report.ts).</summary>
public static class Report
{
    /// <summary>
    /// <c>Enter</c> with the body's cell, when the sim does not know hero <paramref name="id"/> yet, or
    /// puts it in another room than the body is in, on the other side of the air's edge, or of the
    /// lifepod's reach: a hero breathes room air only in a cell holding it, the pod's only by it. Rooms and
    /// air are compared where they are now, never remembered: a door that merges or splits rooms renames
    /// them. Until the host releases it, the same <c>Enter</c> goes out again each frame — a few, and only
    /// on crossing an edge. Else, as the hero's report, while the sim holds it in another cell than the
    /// body's and <paramref name="every"/> ticks have gone since the last one sent (<paramref name="reported"/>,
    /// at world tick <paramref name="tick"/>): what the degrees and the cold read (0050), every tick it moves while a
    /// creature threatens it (<see cref="Every"/>). Null otherwise.
    /// </summary>
    public static Command? Of(ICrewQueries crew, IRoomsQueries rooms, IAtmosphereQueries air, int width, int id, Hero hero, int tick, int reported,
        int every = Stillwake.Content.Crew.ReportEvery)
    {
        int cell = hero.BodyCell(width);
        var known = crew.HeroOf(id);
        if (known != null && rooms.RoomAt(known.Cell) == rooms.RoomAt(cell) && air.AiredAt(known.Cell) == air.AiredAt(cell)
            && crew.ByPod(known.Cell) == crew.ByPod(cell))
            return known.Cell != cell && tick - reported >= every ? new Enter(cell) : null;
        return new Enter(cell);
    }

    /// <summary>
    /// What goes before the <c>Enter</c> that has the pod print hero <paramref name="id"/> (2026-10-28-looks-are-printed):
    /// <c>ChooseLook</c> with the player's <paramref name="look"/>, once a join (<paramref name="sent"/> already or not), so the
    /// pod prints it in that look; <c>Hold</c> while the player has none yet, its look editor open on a first run: no
    /// <c>Enter</c> goes till it closes. Nothing for a hero the sim knows.
    /// </summary>
    public static (Command? Choose, bool Hold) LookFirst(ICrewQueries crew, int id, Look? look, bool sent) =>
        crew.HeroOf(id) != null ? (null, false)
        : look == null ? (null, true)
        : (sent ? null : new ChooseLook(look), false);

    /// <summary>
    /// How often the hero reports where it is: every tick it moves while a creature <paramref name="threatened"/> it, so what charges
    /// it aims a round trip behind (Terraria's way, 2026-10-26-creatures-are-composed); else once a second (0050).
    /// </summary>
    public static int Every(bool threatened) => threatened ? Stillwake.Content.Crew.ReportThreatened : Stillwake.Content.Crew.ReportEvery;

    /// <summary>
    /// How cold hero <paramref name="peer"/>'s body is, 0 to 1 (body-temperature): <see cref="ColdOf"/> its <c>BodyOf</c>. What the
    /// frost on the screen is drawn at.
    /// </summary>
    public static float Cold(ICrewQueries crew, int peer) => ColdOf(crew.BodyOf(peer) / 100f);

    /// <summary>How hot hero <paramref name="peer"/>'s body is, 0 to 1: <see cref="HeatOf"/> its <c>BodyOf</c>. What the haze on the screen is drawn at.</summary>
    public static float Heat(ICrewQueries crew, int peer) => HeatOf(crew.BodyOf(peer) / 100f);

    /// <summary>
    /// The cold's share at a <paramref name="body"/> of so many degrees, 0 to 1: how far under <c>Temperature.Normal</c> it is of the
    /// way to <c>ColdDeath</c>, eased out (<see cref="Eased"/>) so a little cold shows plainly.
    /// </summary>
    public static float ColdOf(float body)
    {
        float normal = Stillwake.Content.Temperature.Normal / 100f, death = Stillwake.Content.Temperature.ColdDeath / 100f;
        return Eased((normal - body) / (normal - death));
    }

    /// <summary>The heat's share at a <paramref name="body"/> of so many degrees, 0 to 1: how far over its normal it is of the way to <c>HeatDeath</c>, eased as the cold's.</summary>
    public static float HeatOf(float body)
    {
        float normal = Stillwake.Content.Temperature.Normal / 100f, death = Stillwake.Content.Temperature.HeatDeath / 100f;
        return Eased((body - normal) / (death - normal));
    }

    /// <summary>A share of the cold or the heat, clamped to 0 to 1 and eased out (2s − s²): what the frost, the haze and the thermometer draw.</summary>
    public static float Eased(float share)
    {
        share = Math.Clamp(share, 0f, 1f);
        return share * (2 - share);
    }

    /// <summary>
    /// Whether <paramref name="hero"/> is out of breath: running aboard. In space (<paramref name="inSpace"/>)
    /// never: a swim is slow, and a jetpack's flight is paid in thrust (0047). Shift held standing still
    /// costs nothing. Seated, <paramref name="runs"/> says: true in a treadmill wheel with a move key held, which is what
    /// turns it (power-chain), wherever it stands.
    /// </summary>
    public static bool Exerting(Hero hero, bool inSpace, bool? runs = null) =>
        runs ?? (!inSpace && !hero.Climbing && Math.Abs(hero.Vx) > Stillwake.Content.Hero.Walk);

    /// <summary>
    /// <c>Exert</c> when the sim's hero <paramref name="id"/> breathes otherwise than <paramref name="exerting"/>
    /// says; null while they agree or the sim has no hero. Sent again each frame until the host releases it.
    /// </summary>
    public static Command? ExertOf(ICrewQueries crew, int id, bool exerting) =>
        crew.HeroOf(id) is { } known && known.Exerting != exerting ? new Exert(exerting) : null;

    /// <summary>
    /// <c>Thrust</c> when the sim's hero <paramref name="id"/> thrusts otherwise than <paramref name="thrusting"/>
    /// says; null while they agree or the sim has no hero. Sent again each frame until the host releases it (0047).
    /// </summary>
    public static Command? ThrustOf(ICrewQueries crew, int id, bool thrusting) =>
        crew.HeroOf(id) is { } known && known.Thrusting != thrusting ? new Thrust(thrusting) : null;

    /// <summary>
    /// <c>Refill</c> when the player asks to breathe a canister and the sim would take it now (<paramref name="problem"/>,
    /// <c>RefillProblem</c>'s, is null) with none in <paramref name="inFlight"/>; null otherwise. What is sent is what is
    /// shown and heard, so a second click before the first lands breathes nothing and shows nothing (0047).
    /// </summary>
    public static Refill? RefillOf(string? problem, IReadOnlyList<Command> inFlight)
    {
        foreach (var c in inFlight)
            if (c is Refill) return null;
        return problem == null ? new Refill() : null;
    }

    /// <summary>
    /// <c>Eat</c> of <paramref name="food"/> when the player asks and the sim would take it now (<paramref name="problem"/>,
    /// <c>EatProblem</c>'s, is null) with no meal in <paramref name="inFlight"/>; null otherwise: a second click before the
    /// first lands eats nothing (food-and-the-body).
    /// </summary>
    public static Eat? EatOf(string food, string? problem, IReadOnlyList<Command> inFlight)
    {
        foreach (var c in inFlight)
            if (c is Eat) return null;
        return problem == null ? new Eat(food) : null;
    }

    /// <summary>
    /// <c>SuitMode</c> flipping the suit's mode on a press (V, its edge key; 2026-11-03-the-regen-suit): from the last one in
    /// <paramref name="inFlight"/>, sent and not yet applied, else from the sim's <paramref name="on"/>; switching on only
    /// where <paramref name="problem"/>, <c>SuitModeProblem</c>'s, is null. Off is always sent.
    /// </summary>
    public static SuitMode? SuitModeOf(bool on, string? problem, IReadOnlyList<Command> inFlight)
    {
        for (int i = inFlight.Count - 1; i >= 0; i--)
            if (inFlight[i] is SuitMode sent)
            {
                on = sent.On;
                break;
            }
        return on ? new SuitMode(false) : problem == null ? new SuitMode(true) : null;
    }
}
