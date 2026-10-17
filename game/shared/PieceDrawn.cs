using Stillwake.Sim.Debris;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where a piece of debris is drawn between ticks, in thousandths of a cell: <paramref name="progress"/>
/// of the way to where its next tick puts it, drifting left by its speed or, reeled in by a harpoon, its
/// share of the way still to go to its reel's point (0044). The debris feature draws it there, and the
/// crew's harpoon sticks its rod there.
/// </summary>
public static class PieceDrawn
{
    public static (double X, double Y) At(Piece piece, double progress) => piece.Reel is { } reel
        ? (piece.X + (double)(reel.ToX - piece.X) / reel.Left * progress, piece.Y + (double)(reel.ToY - piece.Y) / reel.Left * progress)
        : (piece.X - piece.Speed * progress, piece.Y);
}
