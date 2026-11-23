using System;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Shared;

/// <summary>
/// A piece of debris as drawn between ticks: its middle (<paramref name="X"/>, <paramref name="Y"/>) in thousandths of a cell and its
/// <paramref name="Angle"/> in 65536ths of a turn, clockwise, not wrapped.
/// </summary>
public readonly record struct PiecePose(double X, double Y, double Angle)
{
    /// <summary>The angle in radians, clockwise as the screen shows it: what a picture is turned by.</summary>
    public double Radians => Angle / 65536 * 2 * Math.PI;
}

/// <summary>
/// Where a piece of debris is drawn between ticks, in thousandths of a cell: <paramref name="progress"/> ticks
/// on from its last (<c>Session.Drift</c>, past 1 while the next tick is late), drifting left by its speed or,
/// reeled in by a harpoon, its share of the way still to go to its reel's point (0044), never past it; turned by its
/// angle and its spin on (debris-physics). The debris feature draws it there, and the crew's harpoon sticks its rod there.
/// </summary>
public static class PieceDrawn
{
    public static (double X, double Y) At(Piece piece, double progress)
    {
        if (piece.Reel is not { } reel) return (piece.X - piece.Speed * progress, piece.Y);
        double share = Math.Min(progress, reel.Left) / reel.Left;
        return (piece.X + (reel.ToX - piece.X) * share, piece.Y + (reel.ToY - piece.Y) * share);
    }

    /// <summary>Where <paramref name="piece"/> is drawn and how it is turned, <paramref name="progress"/> ticks on (<see cref="At"/>).</summary>
    public static PiecePose Pose(Piece piece, double progress)
    {
        var (x, y) = At(piece, progress);
        return new PiecePose(x, y, piece.Angle + piece.Spin * progress);
    }

    /// <summary>
    /// The box <paramref name="halfW"/> by <paramref name="halfH"/> (thousandths of a cell) of <paramref name="piece"/> as drawn
    /// <paramref name="progress"/> ticks on: the sim's own turned box (<see cref="TurnedBox"/>), at the drawn pose rounded to its units,
    /// so the client tests a hit with the sim's maths.
    /// </summary>
    public static TurnedBox Shape(Piece piece, double progress, int halfW, int halfH)
    {
        var pose = Pose(piece, progress);
        return TurnedBox.Of(piece.Id, (int)Math.Round(pose.X), (int)Math.Round(pose.Y), halfW, halfH, (int)Math.Round(pose.Angle) & 0xFFFF);
    }

    /// <summary>Where the point (<paramref name="x"/>, <paramref name="y"/>), in thousandths of a cell, lies in <paramref name="box"/>'s own frame.</summary>
    public static (int X, int Y) Local(TurnedBox box, double x, double y) => box.Local((int)Math.Round(x), (int)Math.Round(y));
}
