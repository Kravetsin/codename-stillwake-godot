using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// What this hero just gained, counted where it came from (2026-11-06-gain-spots): a column of <see cref="GainLines"/> by each place
/// that gave, a dug cell, a caught piece, a stack walked over. A gain near a place still showing goes onto its column, so a drill
/// working along a seam keeps one count, and the pieces of one catch each keep their own. Places in cells.
/// </summary>
public sealed class GainSpots
{
    /// <summary>
    /// How near a place still showing a gain must be to go onto its column: within <see cref="Near"/> cells, or, zoomed out, within
    /// <see cref="WidePx"/> across and <see cref="TallPx"/> up or down on the screen, where the two columns would lie over each other:
    /// a line is about as wide as an icon and "+00", and a row tall.
    /// </summary>
    public const double Near = 4, WidePx = 80, TallPx = 36;
    /// <summary>How many places show at most: a new one past them pushes the oldest off.</summary>
    public const int Most = 8;

    /// <summary>One place's column: where it stands, in cells, and its lines.</summary>
    public sealed class Spot(double x, double y)
    {
        public double X { get; } = x;
        public double Y { get; } = y;
        public GainLines Lines { get; } = new();
    }

    private readonly List<Spot> _spots = [];

    /// <summary>The places showing, oldest first.</summary>
    public IReadOnlyList<Spot> Spots => _spots;

    /// <summary>
    /// Adds <paramref name="count"/> of <paramref name="key"/> gained at (<paramref name="x"/>, <paramref name="y"/>), in cells, a cell
    /// <paramref name="cellPx"/> px on the screen now.
    /// </summary>
    public void Add(string key, int count, double x, double y, double cellPx)
    {
        Spot? nearest = null;
        double best = 1;
        foreach (var spot in _spots)
        {
            double dx = spot.X - x, dy = spot.Y - y;
            // How far, as a share of reach: 1 at its edge, by cells or on the screen, whichever reaches farther.
            double cells = (dx * dx + dy * dy) / (Near * Near);
            double screen = Math.Pow(dx * cellPx / WidePx, 2) + Math.Pow(dy * cellPx / TallPx, 2);
            double d = Math.Min(cells, screen);
            if (d <= best) (nearest, best) = (spot, d);
        }
        if (nearest == null)
        {
            _spots.Add(nearest = new Spot(x, y));
            if (_spots.Count > Most) _spots.RemoveAt(0);
        }
        nearest.Lines.Add(key, count);
    }

    /// <summary>Moves <paramref name="elapsedMs"/> on: lines age and go, and places with none left.</summary>
    public void Update(double elapsedMs)
    {
        foreach (var spot in _spots) spot.Lines.Update(elapsedMs);
        _spots.RemoveAll(s => s.Lines.Lines.Count == 0);
    }
}
