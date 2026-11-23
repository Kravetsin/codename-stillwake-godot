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

    /// <summary>Where the point (<paramref name="lx"/>, <paramref name="ly"/>) of the piece's own frame, from its middle, is drawn, as <see cref="TurnedBox.At"/> turns it.</summary>
    public (double X, double Y) At(double lx, double ly)
    {
        double cos = Math.Cos(Radians), sin = Math.Sin(Radians);
        return (X + lx * cos - ly * sin, Y + lx * sin + ly * cos);
    }

    /// <summary>The way (<paramref name="lx"/>, <paramref name="ly"/>) of the piece's own frame as drawn: turned by its angle, not moved.</summary>
    public (double X, double Y) Turned(double lx, double ly)
    {
        var (x, y) = At(lx, ly);
        return (x - X, y - Y);
    }

    /// <summary>The drawn way (<paramref name="x"/>, <paramref name="y"/>) in the piece's own frame: <see cref="Turned"/> undone.</summary>
    public (double X, double Y) Unturned(double x, double y) => Local(X + x, Y + y);

    /// <summary>Where the drawn point (<paramref name="x"/>, <paramref name="y"/>) lies in the piece's own frame, from its middle: <see cref="At"/> undone.</summary>
    public (double X, double Y) Local(double x, double y)
    {
        double cos = Math.Cos(Radians), sin = Math.Sin(Radians), dx = x - X, dy = y - Y;
        return (dx * cos + dy * sin, -dx * sin + dy * cos);
    }
}

/// <summary>
/// Where a piece of debris is drawn between ticks, in thousandths of a cell: <paramref name="progress"/> ticks
/// on from its last (<c>Session.Drift</c>, past 1 while the next tick is late), carried on by its motion and turned by
/// its angle and its spin on (debris-physics); the host player's feed setting it anew snaps it there. The debris feature
/// draws it there, and the crew's harpoon sticks its rod there.
/// </summary>
public static class PieceDrawn
{
    public static (double X, double Y) At(Piece piece, double progress) =>
        LiveDebris.Pose(piece.Id) is { } live ? (live.X, live.Y) : (piece.X + piece.Vx * progress, piece.Y + piece.Vy * progress);

    /// <summary>
    /// Where <paramref name="piece"/> is drawn and how it is turned, <paramref name="progress"/> ticks on (<see cref="At"/>); on the host
    /// player's client, where its body is (<see cref="LiveDebris"/>).
    /// </summary>
    public static PiecePose Pose(Piece piece, double progress) =>
        LiveDebris.Pose(piece.Id) ?? new PiecePose(piece.X + piece.Vx * progress, piece.Y + piece.Vy * progress, piece.Angle + piece.Spin * progress);

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
