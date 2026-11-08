using System;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where a piece of debris is drawn between ticks, in thousandths of a cell: <paramref name="progress"/> ticks
/// on from its last (<c>Session.Drift</c>, past 1 while the next tick is late), drifting left by its speed or,
/// reeled in by a harpoon, its share of the way still to go to its reel's point (0044), never past it. The
/// debris feature draws it there, and the crew's harpoon sticks its rod there.
/// </summary>
public static class PieceDrawn
{
    public static (double X, double Y) At(Piece piece, double progress)
    {
        if (piece.Reel is not { } reel) return (piece.X - piece.Speed * progress, piece.Y);
        double share = Math.Min(progress, reel.Left) / reel.Left;
        return (piece.X + (reel.ToX - piece.X) * share, piece.Y + (reel.ToY - piece.Y) * share);
    }
}
