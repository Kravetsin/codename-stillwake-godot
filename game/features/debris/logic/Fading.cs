using System;

namespace Stillwake.Game.Features.Debris;

/// <summary>Whether a piece of debris gone was broken rather than drifted off (features/debris).</summary>
public static class Fading
{
    /// <summary>
    /// Whether a piece gone, last seen at <paramref name="x"/> <paramref name="ticks"/> ticks before, was broken (0034): a
    /// wreck, the only piece a blow breaks, not <paramref name="caught"/> by hand, and still on its path, so broken by its first
    /// blow as by its last; one that drifted past its end only faded.
    /// </summary>
    public static bool Broken(bool wreck, bool caught, int x, int speed, int end, int ticks) =>
        wreck && !caught && x - (long)speed * ticks >= end;
}
