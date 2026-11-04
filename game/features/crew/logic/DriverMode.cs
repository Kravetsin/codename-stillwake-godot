using System.Linq;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Crew;

/// <summary>A dot of the mark by the pointer: lit, waiting for charge, or off.</summary>
public enum ModeDot { Off, Lit, Waiting }

/// <summary>
/// The mark by the pointer that says the power driver's mode (mode-glyph): a dot a cell of its square, the bottom row first,
/// left to right, as the square is worked. All lit, the square; the first alone, one cell; the square wanted with no charge,
/// the first lit and the rest waiting. None for a builder of one cell, which has no mode.
/// </summary>
public static class DriverMode
{
    public static ModeDot[]? DotsOf(Spec.Screwdriver? driver, bool square, bool charged)
    {
        if (driver is not { Area: > 1 } d) return null;
        var rest = !square ? ModeDot.Off : charged ? ModeDot.Lit : ModeDot.Waiting;
        return [ModeDot.Lit, .. Enumerable.Repeat(rest, d.Area * d.Area - 1)];
    }
}
