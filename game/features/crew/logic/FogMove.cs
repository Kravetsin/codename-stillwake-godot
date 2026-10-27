namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A hero's step across a planet's fog (fog-edges): crossing a seam outward, moved by the period to the other band and set
/// <c>FogEdges.Drop</c> rows over the highest ground it will glide on over, its speed across kept and none up or down, still
/// gliding. A hero seated in a drop pod is never moved.
/// </summary>
public static class FogMove
{
    /// <summary>The hero after the step from <paramref name="before"/> to <paramref name="after"/> on <paramref name="ship"/>, moved; null when it is not.</summary>
    public static Hero? Of(Ship ship, Hero before, Hero after, bool seated)
    {
        if (seated || ship.Fog is not { } fog) return null;
        int shift = fog.Shift(before.X, after.X);
        return shift == 0 ? null : after with { X = after.X + shift, Y = fog.Arrive(shift, column => Hero.TopOf(ship, column)), Vy = 0, Gliding = true };
    }
}
