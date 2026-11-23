using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// When a walking guard is heard stepping (derelict-guards): a foot down each time its way walked passes another <c>every</c>, but never
/// two closer than <c>leastS</c> seconds, as a hero's are heard each half stride. Plain C#, tested.
/// </summary>
public sealed class Footfalls(double every, double leastS)
{
    private long? _was;
    private double _sinceS = double.PositiveInfinity;

    /// <summary>
    /// Whether a foot comes down this frame, <paramref name="way"/> walked in all, <paramref name="dt"/> seconds after the last; none
    /// while it <paramref name="walks"/> not, nor on the first frame it walks.
    /// </summary>
    public bool Step(double way, bool walks, double dt)
    {
        _sinceS += dt;
        long? now = walks ? (long)Math.Floor(way / every) : null;
        bool down = now != null && _was != null && now != _was && _sinceS >= leastS;
        if (down) _sinceS = 0;
        _was = now;
        return down;
    }
}
