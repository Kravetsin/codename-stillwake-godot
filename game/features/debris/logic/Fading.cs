using System;

namespace Stillwake.Game.Features.Debris;

/// <summary>Whether a piece of debris gone was broken rather than drifted off (features/debris).</summary>
public static class Fading
{
    /// <summary>
    /// Whether a piece gone, last seen at <paramref name="x"/> <paramref name="ticks"/> ticks before, was broken
    /// (0034): it had hits in it and would still have been on its path; one that drifted past its end only faded.
    /// </summary>
    public static bool Broken(int damage, int x, int speed, int end, int ticks) =>
        damage > 0 && x - (long)speed * ticks >= end;
}
