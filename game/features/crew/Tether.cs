using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A tether as drawn (apps/game/src/features/crew/tether.ts): a rope between where it is tied and the
/// hero's clip, in thousandths of a cell, paid out like a reel's line a little longer than the way it
/// spans, so it neither tangles near the ship nor bends at the limit, where it runs straight.
/// </summary>
public sealed class Tether
{
    /// <summary>
    /// A line out in space: no gravity, only a slow drift, a wave along the line. Thousandths of a cell. The
    /// tether's, and the harpoon's (<see cref="HarpoonLine"/>).
    /// </summary>
    public static readonly RopeStyle Space = new(Links: 20, Keep: 0.95, Gravity: 0, Drift: 0.5, DriftSpeed: 0.01, Wave: 0.35);
    public static int Links => Space.Links;
    /// <summary>How much longer than the way it spans the line is let out, and the least it is.</summary>
    private const double Slack = 1.25, Shortest = 1000;
    /// <summary>The share of the difference the reel takes in each step, so the line pays out smoothly.</summary>
    private const double Reel = 0.05;

    private readonly Rope _rope;
    private double _length;

    public Tether(Point anchor, (double X, double Y) clip, int seed = 0)
    {
        Anchor = anchor;
        _rope = new Rope(Space, (anchor.X, anchor.Y), clip, seed);
        _length = Math.Max(Shortest, Math.Sqrt((clip.X - anchor.X) * (clip.X - anchor.X) + (clip.Y - anchor.Y) * (clip.Y - anchor.Y)));
    }

    /// <summary>Where a tether meets the hero: the air pack, a little above the middle of the body.</summary>
    public static (double X, double Y) ClipOf(double x, double y) => (x, y - Spec.Height * 0.6);

    /// <summary>The anchor it was tied at: a tether tied elsewhere is a new line.</summary>
    public Point Anchor { get; }

    /// <summary>How long the line is let out now.</summary>
    public double Length => _length;

    public IReadOnlyList<Rope.Knot> Points => _rope.Points;

    /// <summary>Adds the line's points as drawn this frame, in thousandths of a cell, to <paramref name="into"/>.</summary>
    public void DrawnInto(List<(double X, double Y)> into) => _rope.DrawnInto(into);

    /// <summary>Moves the line on by <paramref name="elapsedMs"/>, its ends at <paramref name="from"/> and <paramref name="to"/>.</summary>
    public void Update((double X, double Y) from, (double X, double Y) to, double elapsedMs) =>
        _rope.Update(from, to, elapsedMs, span =>
        {
            double wanted = Math.Min(Spec.Tether, Math.Max(Shortest, span * Slack));
            _length = Math.Max(_length + (Math.Max(wanted, span) - _length) * Reel, span);
            return _length;
        });
}
