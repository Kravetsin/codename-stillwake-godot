using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>Where a thing's frame stands: its middle, in board pixels from the board's top-left, and how many steps it is from raw.</summary>
public sealed record BoardFrame(string Id, string District, int Depth, double X, double Y);

/// <summary>A district's block: its top-left and size in board pixels.</summary>
public sealed record BoardBlock(string District, double X, double Y, double Wide, double Tall);

/// <summary>
/// Where everything on the board stands (board-islands), a pure function of Content, built once a run: every thing a fixed frame in its
/// district's block from the start, so a find never moves anything. In a block, a column for each depth it holds, shallow to deep, rows in the
/// table's order, at most <see cref="Rows"/> a column; blocks side by side along their lane, lanes one under another.
/// </summary>
public sealed class BoardLayout
{
    /// <summary>
    /// The board's pattern (the background grid draws it): a frame 3 by 3 small cells and a gutter of one, so every frame stands on a big cell of one pitch.
    /// A block's caption, its margin and the gaps between blocks and lanes keep every block on that pitch too.
    /// </summary>
    public const double Grid = 30, FrameWide = 3 * Grid, FrameTall = 3 * Grid, Cell = 4 * Grid, Row = 4 * Grid, Head = 3 * Grid, Pad = Grid, Apart = 3 * Grid, Lanes = 5 * Grid;

    /// <summary>The most frames a column holds before the depth takes another column.</summary>
    public const int Rows = 6;

    private static readonly Lazy<BoardLayout> Built = new(() => Of(BoardDistricts.All, Depths(BoardThings.Ids, id => BoardThings.Of(id)?.Parts, BoardThings.SourcesOf).GetValueOrDefault));

    /// <summary>The board's layout, worked out once from Content.</summary>
    public static BoardLayout Main => Built.Value;

    public IReadOnlyDictionary<string, BoardFrame> Frames { get; }

    public IReadOnlyList<BoardBlock> Blocks { get; }

    public double Wide { get; }

    public double Tall { get; }

    private BoardLayout(IReadOnlyDictionary<string, BoardFrame> frames, IReadOnlyList<BoardBlock> blocks, double wide, double tall) =>
        (Frames, Blocks, Wide, Tall) = (frames, blocks, wide, tall);

    /// <summary>How many steps from raw <paramref name="id"/> is; 0 for anything off the board.</summary>
    public static int DepthOf(string id) => Main.Frames.TryGetValue(id, out var frame) ? frame.Depth : 0;

    /// <summary>The layout of <paramref name="districts"/>, each thing at its <paramref name="depth"/>.</summary>
    public static BoardLayout Of(IReadOnlyList<BoardDistrict> districts, Func<string, int> depth)
    {
        var frames = new Dictionary<string, BoardFrame>(StringComparer.Ordinal);
        var blocks = new List<BoardBlock>();
        double top = 0, wide = 0;
        foreach (var lane in districts.Select(d => d.Lane).Distinct().Order())
        {
            double left = 0, tall = 0;
            foreach (var district in districts.Where(d => d.Lane == lane))
            {
                var columns = district.Things.GroupBy(depth).OrderBy(g => g.Key).ToList();
                double x = left + Pad;
                int rows = 1;
                foreach (var column in columns)
                {
                    var things = column.ToList();
                    int across = (things.Count + Rows - 1) / Rows;
                    for (int i = 0; i < things.Count; i++)
                        frames[things[i]] = new BoardFrame(things[i], district.Id, column.Key, x + i / Rows * Cell + FrameWide / 2, top + Head + i % Rows * Row + FrameTall / 2);
                    rows = Math.Max(rows, Math.Min(Rows, things.Count));
                    x += across * Cell;
                }
                double blockWide = x - (Cell - FrameWide) + Pad - left, blockTall = Head + rows * Row - (Row - FrameTall) + Pad;
                blocks.Add(new BoardBlock(district.Id, left, top, blockWide, blockTall));
                left += blockWide + Apart;
                tall = Math.Max(tall, blockTall);
            }
            wide = Math.Max(wide, left - Apart);
            top += tall + Lanes;
        }
        return new BoardLayout(frames, blocks, wide, Math.Max(0, top - Lanes));
    }

    /// <summary>
    /// How many steps from raw each of <paramref name="ids"/> is: an item only found 0, a recipe 1 + the most of its parts' (1 with none), a smelted item 1 +
    /// the least of its sources'. A cycle meets a guard: what is still being worked out counts 0.
    /// </summary>
    /// <param name="parts">A recipe's parts; null for no recipe.</param>
    /// <param name="sources">What an item is smelted, crushed, grown or fermented out of.</param>
    public static Dictionary<string, int> Depths(IEnumerable<string> ids, Func<string, IReadOnlyList<string>?> parts, Func<string, IReadOnlyList<string>> sources)
    {
        var depths = new Dictionary<string, int>(StringComparer.Ordinal);
        var working = new HashSet<string>(StringComparer.Ordinal);
        int Of(string id)
        {
            if (depths.TryGetValue(id, out int known)) return known;
            if (!working.Add(id)) return 0;
            int depth = parts(id) is { } recipe ? 1 + recipe.Select(Of).DefaultIfEmpty(0).Max()
                : sources(id) is { Count: > 0 } from ? 1 + from.Min(Of)
                : 0;
            working.Remove(id);
            return depths[id] = depth;
        }
        foreach (string id in ids) Of(id);
        return depths;
    }
}
