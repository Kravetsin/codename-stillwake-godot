using System;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The part of the board the camera keeps in bounds, the first note at the origin: at least a view round that note, which so stands in
/// the view's middle, and all that was shown with a margin. It only grows, so nothing jumps as shapes come and go, and no further than the board's edges
/// ([board-edges](../../../../docs/decisions/2026-11-02-board-edges.md)).
/// </summary>
public readonly record struct BoardBox(double Left, double Top, double Right, double Bottom)
{
    /// <summary>What is left round what is shown.</summary>
    public const double Margin = BoardSlots.NoteSize;

    /// <summary>How far from the first note a thing may stand, either way across and up or down: the board fills the view at its smallest, with its notes still pictures.</summary>
    public const double ReachWide = 1200, ReachTall = 600;

    /// <summary>Whether a point lies on the board.</summary>
    public static bool Inside(double x, double y) => Math.Abs(x) <= ReachWide && Math.Abs(y) <= ReachTall;

    /// <summary>The point of the board closest to <paramref name="spot"/>.</summary>
    public static BoardSpot Within(BoardSpot spot) => new(Math.Clamp(spot.X, -ReachWide, ReachWide), Math.Clamp(spot.Y, -ReachTall, ReachTall));

    public double Wide => Right - Left;

    public double Tall => Bottom - Top;

    /// <summary>The box of a view, centred on the first note.</summary>
    public static BoardBox Round(double viewWide, double viewTall) => new(-viewWide / 2, -viewTall / 2, viewWide / 2, viewTall / 2);

    /// <summary>This box grown to hold a thing shown at <paramref name="x"/>, <paramref name="y"/> and its margin.</summary>
    public BoardBox Holding(double x, double y) =>
        new(Math.Min(Left, x - Margin), Math.Min(Top, y - Margin), Math.Max(Right, x + Margin), Math.Max(Bottom, y + Margin));

    /// <summary>This box grown to hold <paramref name="other"/>.</summary>
    public BoardBox Holding(BoardBox other) =>
        new(Math.Min(Left, other.Left), Math.Min(Top, other.Top), Math.Max(Right, other.Right), Math.Max(Bottom, other.Bottom));
}
