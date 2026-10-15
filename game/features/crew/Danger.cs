using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crew;
using CrewSpec = Stillwake.Content.Crew;

namespace Stillwake.Game.Features.Crew;

/// <summary>This player's hero in danger, as the screen shows it.</summary>
public enum SuitDanger { None, Low, Out }

/// <summary>The suit's danger (apps/game/src/features/crew/danger.ts) and what the suit gauge reads.</summary>
public static class Danger
{
    /// <summary>Below this share of a full suit, out of room air, the hero is in danger.</summary>
    public const double Low = 0.25;

    /// <summary>
    /// <see cref="SuitDanger.Out"/> while passed out, <see cref="SuitDanger.Low"/> while the suit is low
    /// with no air to refill it. In an aired room or by the lifepod a low suit fills again, so it is no danger.
    /// </summary>
    public static SuitDanger Of((int Suit, int Out)? hero, bool inRoomAir)
    {
        if (hero is not var (suit, @out)) return SuitDanger.None;
        if (@out > 0) return SuitDanger.Out;
        return (double)suit / CrewSpec.Suit < Low && !inRoomAir ? SuitDanger.Low : SuitDanger.None;
    }

    /// <summary>Hero <paramref name="id"/>'s suit, full while the sim does not know it, and its danger: what the gauge shows.</summary>
    public static (int Suit, SuitDanger Danger) Read(ICrewQueries crew, IAtmosphereQueries air, int id)
    {
        var hero = crew.HeroOf(id);
        // The pod's air refills a suit as room air does.
        bool inRoomAir = hero != null && (air.AiredAt(hero.Cell) || crew.ByPod(hero.Cell));
        return (hero?.Suit ?? CrewSpec.Suit, Of(hero == null ? null : (hero.Suit, hero.Out), inRoomAir));
    }
}
