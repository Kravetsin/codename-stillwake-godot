using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A hero's step across a planet's fog (fog-edges): crossing a seam outward, moved by the period to the other band and set
/// <see cref="FogEdges.Drop"/> rows over its ground line beside the zone, its speed across kept and none up or down. A hero
/// seated in a drop pod is never moved.
/// </summary>
public static class FogMove
{
    /// <summary>The hero after the step from <paramref name="before"/> to <paramref name="after"/>, moved; null when it is not.</summary>
    public static Hero? Of(FogEdges? fog, Hero before, Hero after, bool seated)
    {
        if (seated || fog == null) return null;
        int shift = fog.Shift(before.X, after.X);
        return shift == 0 ? null : after with { X = after.X + shift, Y = fog.Arrive(shift), Vy = 0 };
    }
}
