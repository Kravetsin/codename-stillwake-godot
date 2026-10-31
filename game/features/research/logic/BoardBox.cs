using System;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The part of the board the camera keeps in bounds, the first note at the origin: at least a view round that note, which so stands in
/// the view's middle, and all that was shown with a margin. It only grows, so nothing jumps as a click shows a shape and hides it again.
/// </summary>
public readonly record struct BoardBox(double Left, double Top, double Right, double Bottom)
{
    /// <summary>What is left round what is shown.</summary>
    public const double Margin = BoardSlots.NoteSize;

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
