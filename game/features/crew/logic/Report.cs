using System;
using System.Collections.Generic;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crew;
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
    /// at world tick <paramref name="tick"/>): what the cold past the field reads (0050), every tick it moves while a
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
    /// How often the hero reports where it is: every tick it moves while a creature <paramref name="threatened"/> it, so what charges
    /// it aims a round trip behind (Terraria's way, 2026-10-26-creatures-are-composed); else once a second (0050).
    /// </summary>
    public static int Every(bool threatened) => threatened ? Stillwake.Content.Crew.ReportThreatened : Stillwake.Content.Crew.ReportEvery;

    /// <summary>
    /// How hard the cold bites at <paramref name="cell"/>, the body's own, from 0 inside the field to 1 at
    /// the world's edge: the crew's <c>EdgeCost</c> over <c>Field.EdgeCost</c>, eased out (2s − s²) so the
    /// frost shows a little past the line and is heavy well before the edge (0050). What the frost on the
    /// screen is drawn at.
    /// </summary>
    public static float Cold(ICrewQueries crew, int cell)
    {
        float share = Math.Clamp((float)crew.EdgeCost(cell) / Stillwake.Content.Field.EdgeCost, 0f, 1f);
        return share * (2 - share);
    }

    /// <summary>
    /// Whether <paramref name="hero"/> is out of breath: running aboard. In space (<paramref name="inSpace"/>)
    /// never: a swim is slow, and a jetpack's flight is paid in thrust (0047). Shift held standing still
    /// costs nothing.
    /// </summary>
    public static bool Exerting(Hero hero, bool inSpace) =>
        !inSpace && !hero.Climbing && Math.Abs(hero.Vx) > Stillwake.Content.Hero.Walk;

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
}
