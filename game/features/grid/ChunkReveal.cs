using System;
using System.Collections.Generic;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// Which of the grid's cells <see cref="GridView"/> has drawn, by chunks <see cref="Chunk"/> cells square drawn a row at a time,
/// and which to draw next as the view moves (0079: a planet is built in nearly every cell). Every chunk the view touches is drawn
/// whole at once, so nothing pops in on screen; the chunks within <see cref="Margin"/> cells round it are drawn ahead, nearest the
/// view first, at most <see cref="Budget"/> cells a frame, so new ground coming into view while running costs no frame more than
/// that. Plain C#, so it is tested.
/// </summary>
public sealed class ChunkReveal
{
    /// <summary>How many cells square the grid is drawn in.</summary>
    public const int Chunk = 32;
    /// <summary>
    /// How far round the view the chunks are drawn ahead, in cells: over two seconds of a sprint, so the chunks it runs into are
    /// drawn long before they show.
    /// </summary>
    public const int Margin = 80;
    /// <summary>
    /// The cells drawn ahead a frame, at most: a row past it is finished. About half a millisecond with the tile layers' own update,
    /// what a 240 Hz frame can spare.
    /// </summary>
    public const int Budget = 256;

    private readonly int _width, _height, _across, _down;
    /// <summary>The rows drawn of each chunk, row by row of chunks, from its top: all of them once it is drawn whole.</summary>
    private readonly int[] _rows;
    /// <summary>Scratch: the chunks round the view not drawn whole yet, and how far each lies from the view.</summary>
    private readonly List<(int Chunk, int Distance)> _ahead = [];
    /// <summary>The chunks round the view at the last look, while all of them were drawn: nothing to do till the view leaves them.</summary>
    private Box _done = Box.None;

    public ChunkReveal(int width, int height)
    {
        (_width, _height) = (width, height);
        _across = (width + Chunk - 1) / Chunk;
        _down = (height + Chunk - 1) / Chunk;
        _rows = new int[_across * _down];
    }

    /// <summary>Whether <paramref name="cell"/> is drawn: a change to it is drawn at once, else when its row is.</summary>
    public bool Drawn(int cell)
    {
        int x = cell % _width, y = cell / _width;
        return y % Chunk < _rows[y / Chunk * _across + x / Chunk];
    }

    /// <summary>
    /// Draws what the view needs this frame through <paramref name="drawRow"/> (a row: y, its first x and past its last, answering the
    /// cells it drew): the rest of every chunk in <paramref name="view"/> (in cells, its edges included), then rows of the chunks round
    /// it, nearest first, until <see cref="Budget"/> cells are drawn. Answers the cells drawn.
    /// </summary>
    public int Reveal(Box view, Func<int, int, int, int> drawRow)
    {
        var near = Chunks(view, 0);
        var round = Chunks(view, Margin);
        if (round == _done) return 0;
        int drawn = 0;
        for (int cy = near.Top; cy <= near.Bottom; cy++)
            for (int cx = near.Left; cx <= near.Right; cx++)
                drawn += Finish(cy * _across + cx, int.MaxValue, drawRow);
        _ahead.Clear();
        for (int cy = round.Top; cy <= round.Bottom; cy++)
            for (int cx = round.Left; cx <= round.Right; cx++)
            {
                int chunk = cy * _across + cx;
                if (_rows[chunk] < RowsIn(cy)) _ahead.Add((chunk, Math.Max(Gap(cx, near.Left, near.Right), Gap(cy, near.Top, near.Bottom))));
            }
        _ahead.Sort((a, b) => a.Distance != b.Distance ? a.Distance.CompareTo(b.Distance) : a.Chunk.CompareTo(b.Chunk));
        int spent = 0;
        foreach (var (chunk, _) in _ahead)
        {
            if (spent >= Budget) break;
            spent += Finish(chunk, Budget - spent, drawRow);
        }
        drawn += spent;
        if (spent < Budget) _done = round;
        return drawn;
    }

    /// <summary>Draws the rows of <paramref name="chunk"/> not drawn yet till <paramref name="budget"/> cells are; answers how many were.</summary>
    private int Finish(int chunk, int budget, Func<int, int, int, int> drawRow)
    {
        int cx = chunk % _across, cy = chunk / _across, rows = RowsIn(cy), drawn = 0;
        int x0 = cx * Chunk, x1 = Math.Min(_width, x0 + Chunk);
        while (_rows[chunk] < rows && drawn < budget)
        {
            drawn += drawRow(cy * Chunk + _rows[chunk], x0, x1);
            _rows[chunk]++;
        }
        return drawn;
    }

    private int RowsIn(int cy) => Math.Min(Chunk, _height - cy * Chunk);

    /// <summary>The chunks a box of cells grown by <paramref name="grow"/> cells touches, within the grid.</summary>
    private Box Chunks(Box view, int grow) => new(
        Math.Clamp(view.Left - grow, 0, _width - 1) / Chunk,
        Math.Clamp(view.Top - grow, 0, _height - 1) / Chunk,
        Math.Clamp(view.Right + grow, 0, _width - 1) / Chunk,
        Math.Clamp(view.Bottom + grow, 0, _height - 1) / Chunk);

    /// <summary>How many chunks <paramref name="c"/> lies outside <paramref name="from"/>..<paramref name="to"/>.</summary>
    private static int Gap(int c, int from, int to) => c < from ? from - c : c > to ? c - to : 0;
}
