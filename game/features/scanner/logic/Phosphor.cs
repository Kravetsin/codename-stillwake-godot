using System;

namespace Stillwake.Game.Features.Scanner;

/// <summary>
/// The picked readout, phosphor (2026-11-07-the-scanner): green 3 by 5 digits on dark glass, as the tablet's edge keys glow, its
/// box in world pixels over the point it reads. Its numbers are the page's.
/// </summary>
public static class Phosphor
{
    /// <summary>The box's height, the digits' inset in it, and its gap over the point, in world pixels.</summary>
    public const int Height = 11, Inset = 3, Above = 4;

    private static readonly string[] Digits =
    [
        "111101101101111", "010110010010111", "111001111100111", "111001011001111", "101101111001001",
        "111100111001111", "111100111101111", "111001010010010", "111101111101111", "111101111001111",
    ];
    private const string Slash = "001001010100100";

    /// <summary>How wide <paramref name="text"/> is in world pixels: 3 a character, one between.</summary>
    public static int Width(string text) => text.Length * 4 - 1;

    /// <summary>The room the value left takes beside <paramref name="max"/> (with its slash): the maximum's digits, so a falling value never narrows the box.</summary>
    public static int LeftWidth(string max) => Width(max) - 4;

    /// <summary>
    /// Screen pixels a readout pixel takes at <paramref name="zoom"/>: whole, at least one, so every digit keeps its shape as the
    /// camera eases; a world pixel at a fractional zoom grows and shrinks by a screen pixel as it moves.
    /// </summary>
    public static int Scale(double zoom) => Math.Max(1, (int)Math.Round(zoom, MidpointRounding.AwayFromZero));

    /// <summary>The box reading up to <paramref name="max"/> (with its slash) over (<paramref name="x"/>, <paramref name="y"/>); the value left lies right-aligned in <see cref="LeftWidth"/>.</summary>
    public static (int X, int Y, int W, int H) Box(int x, int y, string max)
    {
        int w = LeftWidth(max) + Width(max) + 1 + 2 * Inset;
        return (x - (int)Math.Round(w / 2.0, MidpointRounding.AwayFromZero), y - Above - Height, w, Height);
    }

    /// <summary>Whether <paramref name="c"/>'s pixel at (<paramref name="col"/>, <paramref name="row"/>) of its 3 by 5 is lit; anything but a digit is the slash.</summary>
    public static bool Lit(char c, int col, int row) => (c is >= '0' and <= '9' ? Digits[c - '0'] : Slash)[row * 3 + col] == '1';
}
