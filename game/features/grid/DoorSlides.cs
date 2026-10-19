using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// The door cells sliding open or shut, each taking <see cref="Seconds"/> from one end to the other; the cells of a
/// doorway move together, so its panel slides as one. Only the look: the sim moved the door at once. Plain C#, so it is
/// tested.
/// </summary>
public sealed class DoorSlides
{
    /// <summary>How long a panel takes to slide all the way, whatever the doorway's length.</summary>
    public const double Seconds = 0.2;

    /// <summary>The cells still sliding: how far open each is now, from 0 to 1, and which way it goes.</summary>
    private readonly Dictionary<int, (double Open, bool Opening)> _moving = new();
    private readonly List<int> _cells = [];

    /// <summary>Whether any cell is sliding: with none, <see cref="Advance"/> has nothing to do.</summary>
    public bool Any => _moving.Count > 0;

    /// <summary>
    /// Door cell <paramref name="cell"/> was seen to open, or shut: it slides that way from the other end, or, turned
    /// back part way, from where it is.
    /// </summary>
    public void Moved(int cell, bool open) =>
        _moving[cell] = (_moving.TryGetValue(cell, out var slide) ? slide.Open : open ? 0 : 1, open);

    /// <summary>How far open <paramref name="cell"/> is drawn, 0 shut to 1 open; null at rest.</summary>
    public double? OpenOf(int cell) => _moving.TryGetValue(cell, out var slide) ? slide.Open : null;

    /// <summary>
    /// Moves every slide on by <paramref name="seconds"/>; answers the cells that were sliding, those that came to rest
    /// among them. Good until the next call.
    /// </summary>
    public IReadOnlyList<int> Advance(double seconds)
    {
        _cells.Clear();
        _cells.AddRange(_moving.Keys);
        double by = seconds / Seconds;
        foreach (int cell in _cells)
        {
            var (open, opening) = _moving[cell];
            double now = opening ? Math.Min(1, open + by) : Math.Max(0, open - by);
            if (now == (opening ? 1 : 0)) _moving.Remove(cell);
            else _moving[cell] = (now, opening);
        }
        return _cells;
    }
}
