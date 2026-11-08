using System;
using System.Collections.Generic;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Shared;

/// <summary>
/// The box a wreck is pointed at by (0034): its whole picture and a little more, as it rocks. The crew aims its hands by it,
/// the scanner reads by it (2026-11-07-the-scanner).
/// </summary>
public static class WreckAim
{
    /// <summary>How far past its picture's edge a wreck is still pointed at, in thousandths of a cell.</summary>
    public const int Margin = 500;

    /// <summary>Half a wreck's picture each way, in thousandths of a cell, with <see cref="Margin"/>; none for a piece caught by hand.</summary>
    public static (int HalfW, int HalfH)? HalfOf(string item) =>
        DebrisLooks.Wrecks.TryGetValue(item, out var size)
            ? (size.W * DebrisModule.Unit / (2 * Camera.Cell) + Margin, size.H * DebrisModule.Unit / (2 * Camera.Cell) + Margin)
            : null;

    /// <summary>The first wreck of <paramref name="pieces"/>, in spawn order, with <paramref name="point"/> (in cells) in its box; none over none.</summary>
    public static Piece? Under(IReadOnlyList<Piece> pieces, (double X, double Y) point)
    {
        double px = point.X * DebrisModule.Unit, py = point.Y * DebrisModule.Unit;
        for (int i = 0; i < pieces.Count; i++)
            if (HalfOf(pieces[i].Item) is var (halfW, halfH) && Math.Abs(pieces[i].X - px) <= halfW && Math.Abs(pieces[i].Y - py) <= halfH) return pieces[i];
        return null;
    }
}
