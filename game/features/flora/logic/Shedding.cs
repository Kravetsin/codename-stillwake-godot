using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Flora;

/// <summary>
/// When a runner shakes leaves off a plant it brushes (plant-stirs): faster than <see cref="FloraLooks.ScatterSpeed"/>, and
/// not that plant again for <see cref="FloraLooks.ScatterEvery"/> seconds, so running back and forth by a bush sheds a few
/// at a time and never strips it in a stream. Plain C#.
/// </summary>
public sealed class Shedding
{
    private readonly Dictionary<int, double> _last = new();

    /// <summary>
    /// Whether plant <paramref name="key"/>, met by a body going <paramref name="vx"/> pixels a second at
    /// <paramref name="seconds"/>, sheds now; if so it is marked shed then.
    /// </summary>
    public bool Sheds(int key, float vx, double seconds)
    {
        if (Math.Abs(vx) < FloraLooks.ScatterSpeed) return false;
        if (_last.TryGetValue(key, out double last) && seconds - last < FloraLooks.ScatterEvery) return false;
        _last[key] = seconds;
        return true;
    }

    /// <summary>Forgets plant <paramref name="key"/>, felled.</summary>
    public void Forget(int key) => _last.Remove(key);
}
