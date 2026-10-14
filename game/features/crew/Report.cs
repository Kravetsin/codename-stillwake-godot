using System;
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
    /// puts it in another room than the body is in, or on the other side of the air's edge: a hero
    /// breathes room air only in a cell holding it. Null otherwise. Rooms and air are compared where
    /// they are now, never remembered: a door that merges or splits rooms renames them. Until the host
    /// releases it, the same <c>Enter</c> goes out again each frame — a few, and only on crossing an edge.
    /// </summary>
    public static Command? Of(ICrewQueries crew, IRoomsQueries rooms, IAtmosphereQueries air, int width, int id, Hero hero)
    {
        int cell = hero.BodyCell(width);
        var known = crew.HeroOf(id);
        if (known != null && rooms.RoomAt(known.Cell) == rooms.RoomAt(cell) && air.AiredAt(known.Cell) == air.AiredAt(cell))
            return null;
        return new Enter(cell);
    }

    /// <summary>
    /// Whether <paramref name="hero"/> is out of breath: running aboard, or swimming fast with the body in
    /// space (<paramref name="inSpace"/>). Shift held standing still costs nothing.
    /// </summary>
    public static bool Exerting(Hero hero, bool inSpace) => inSpace
        ? Math.Abs(hero.Vx) > Stillwake.Content.Hero.Swim || Math.Abs(hero.Vy) > Stillwake.Content.Hero.Swim
        : !hero.Climbing && Math.Abs(hero.Vx) > Stillwake.Content.Hero.Walk;

    /// <summary>
    /// <c>Exert</c> when the sim's hero <paramref name="id"/> breathes otherwise than <paramref name="exerting"/>
    /// says; null while they agree or the sim has no hero. Sent again each frame until the host releases it.
    /// </summary>
    public static Command? ExertOf(ICrewQueries crew, int id, bool exerting) =>
        crew.HeroOf(id) is { } known && known.Exerting != exerting ? new Exert(exerting) : null;
}
