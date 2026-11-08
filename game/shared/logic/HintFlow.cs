using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>A piece of a hint line as it is laid: a press, a "+" joining two, or one word.</summary>
public readonly record struct HintPiece(Press? Press, string? Word, float Width)
{
    /// <summary>The "+" between two presses held together, Ctrl and a drag.</summary>
    public bool Plus => Press == null && Word == "+";
}

/// <summary>A piece placed: its row from 0 and its left edge from the line's.</summary>
public readonly record struct HintPlaced(HintPiece Piece, int Row, float X);

/// <summary>
/// A hint line laid out (build-hints): its presses, then its words one by one, flowed into rows of a width, the rows after
/// the first indented. Godot-free; the widths come from the view.
/// </summary>
public static class HintFlow
{
    /// <summary>The space between pieces, and round a "+".</summary>
    public const float Gap = 3, PlusGap = 1;

    /// <summary>
    /// The pieces of <paramref name="line"/> saying <paramref name="words"/>: its presses, a "+" between two held together
    /// (neither an item: an item is what to take, not to hold), then each word.
    /// </summary>
    public static List<HintPiece> PiecesOf(KeyHint line, string words, Func<Press, float> pressWidth, Func<string, float> wordWidth)
    {
        var pieces = new List<HintPiece>();
        for (int i = 0; i < line.Presses.Count; i++)
        {
            var press = line.Presses[i];
            if (i > 0 && press is not ItemPress && line.Presses[i - 1] is not ItemPress) pieces.Add(new HintPiece(null, "+", wordWidth("+")));
            pieces.Add(new HintPiece(press, null, pressWidth(press)));
        }
        foreach (string word in words.Split(' ', StringSplitOptions.RemoveEmptyEntries)) pieces.Add(new HintPiece(null, word, wordWidth(word)));
        return pieces;
    }

    /// <summary>
    /// <paramref name="pieces"/> placed in rows of <paramref name="width"/>, a piece that does not fit starting the next row at
    /// <paramref name="indent"/>; a "+" never ends a row, and a row's first piece is placed even if too wide.
    /// </summary>
    public static List<HintPlaced> Flow(IReadOnlyList<HintPiece> pieces, float width, float indent)
    {
        var placed = new List<HintPlaced>();
        int row = 0;
        float x = 0;
        bool first = true;
        for (int i = 0; i < pieces.Count; i++)
        {
            var piece = pieces[i];
            bool afterPlus = i > 0 && pieces[i - 1].Plus;
            float gap = first ? 0 : piece.Plus || afterPlus ? PlusGap : Gap;
            // A "+" is kept with the press after it: it goes on only if both fit, and that press never moves alone.
            float need = piece.Width + (piece.Plus && i + 1 < pieces.Count ? PlusGap + pieces[i + 1].Width : 0);
            if (!first && !afterPlus && x + gap + need > width) (row, x, gap) = (row + 1, indent, 0);
            placed.Add(new HintPlaced(piece, row, x + gap));
            x += gap + piece.Width;
            first = false;
        }
        return placed;
    }
}
