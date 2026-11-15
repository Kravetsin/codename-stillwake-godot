using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Flora;

/// <summary>
/// How many leaves a bush or a tree has left to lose (plant-stirs): <see cref="FloraLooks.Swing.Leafage"/> when whole, fewer
/// for each shaken off, grown back <see cref="FloraLooks.Swing.Regrow"/> a second. So a runner going back and forth through a
/// bush drags a long trail the first time and thinner ones after, and an axe never strips a tree in an endless stream. Plain C#.
/// </summary>
public sealed class Leafage
{
    private readonly Dictionary<int, (float Left, double At)> _plants = new();

    /// <summary>
    /// Takes up to <paramref name="wanted"/> leaves off plant <paramref name="key"/> of <paramref name="swing"/> at
    /// <paramref name="seconds"/>; how many it gave.
    /// </summary>
    public int Take(int key, FloraLooks.Swing swing, float wanted, double seconds)
    {
        float left = _plants.TryGetValue(key, out var was)
            ? Math.Min(swing.Leafage, was.Left + (float)(seconds - was.At) * swing.Regrow)
            : swing.Leafage;
        int given = (int)Math.Min(MathF.Floor(left), MathF.Max(0, wanted));
        _plants[key] = (left - given, seconds);
        return given;
    }

    /// <summary>Forgets plant <paramref name="key"/>, felled.</summary>
    public void Forget(int key) => _plants.Remove(key);
}
