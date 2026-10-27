using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crew;

namespace Stillwake.Game.Features.Crew;

/// <summary>This player's hero in danger, as the screen shows it.</summary>
public enum SuitDanger { None, Low, Out }

/// <summary>The suit's danger (apps/game/src/features/crew/danger.ts) and what the suit gauge reads.</summary>
public static class Danger
{
    /// <summary>Below this share of a full suit, out of room air, the hero is in danger.</summary>
    public const double Low = 0.25;

    /// <summary>
    /// <see cref="SuitDanger.Out"/> while passed out, <see cref="SuitDanger.Low"/> while the hero's air is low, by its
    /// capacity, with no air to refill it. In an aired room or by the lifepod low air fills again, so it is no danger.
    /// </summary>
    public static SuitDanger Of((int Air, int Capacity, int Out)? hero, bool inRoomAir)
    {
        if (hero is not var (air, capacity, @out)) return SuitDanger.None;
        if (@out > 0) return SuitDanger.Out;
        return (double)air / capacity < Low && !inRoomAir ? SuitDanger.Low : SuitDanger.None;
    }

    /// <summary>
    /// Hero <paramref name="id"/>'s air and capacity (the suit's tank, or its lungs bare), full while the sim does not know
    /// it, its danger, and whether it breathes room air or the pod's: what the gauge shows.
    /// </summary>
    public static (int Air, int Capacity, SuitDanger Danger, bool InAir) Read(ICrewQueries crew, IAtmosphereQueries air, int id)
    {
        var hero = crew.HeroOf(id);
        int capacity = crew.CapacityOf(id);
        // The pod's air refills a hero as room air does.
        bool inRoomAir = hero != null && (air.AiredAt(hero.Cell) || crew.ByPod(hero.Cell));
        return (hero?.Air ?? capacity, capacity, Of(hero == null ? null : (hero.Air, capacity, hero.Out), inRoomAir), inRoomAir);
    }
}
