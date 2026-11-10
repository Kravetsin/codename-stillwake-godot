using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Flora;

/// <summary>
/// When a body brushing a plant moves it again (plant-stirs): going faster than a speed, and not that plant again for a while,
/// so walking by a bush sways it once rather than every frame its body overlaps it. Plain C#.
/// </summary>
public sealed class Brushes(float speed, double every)
{
    private readonly Dictionary<int, double> _last = new();

    /// <summary>
    /// Whether plant <paramref name="key"/>, met by a body going <paramref name="vx"/> pixels a second at
    /// <paramref name="seconds"/>, answers now; if so it is marked then.
    /// </summary>
    public bool Answers(int key, float vx, double seconds)
    {
        if (Math.Abs(vx) < speed) return false;
        if (_last.TryGetValue(key, out double last) && seconds - last < every) return false;
        _last[key] = seconds;
        return true;
    }

    /// <summary>Forgets plant <paramref name="key"/>, felled.</summary>
    public void Forget(int key) => _last.Remove(key);
}
