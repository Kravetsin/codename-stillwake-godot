using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>Where a thing's frame stands: its middle, in board pixels from the board's top-left, and how many steps it is from raw.</summary>
public sealed record BoardFrame(string Id, string District, int Depth, double X, double Y);

/// <summary>A district's block: its top-left and size in board pixels.</summary>
public sealed record BoardBlock(string District, double X, double Y, double Wide, double Tall);

/// <summary>The head over a column of a block: the steps from raw its frames stand at, and where it is written.</summary>
public sealed record BoardHead(string District, int Depth, double X, double Y, double Wide);

/// <summary>
/// Where everything on the board stands (docs/plans/board-islands.md), a pure function of Content, built once a run: every thing a fixed frame in its
/// district's block from the start, so a find never moves anything. In a block, a column for each depth it holds, shallow to deep, rows in the
/// table's order, at most <see cref="Rows"/> a column; blocks side by side along their lane, lanes one under another.
/// </summary>
public sealed class BoardLayout
{
    /// <summary>A frame's cell and the frame drawn in it, the gap between columns of other depths, a block's caption and its margin, and the gap between blocks.</summary>
    public const double Cell = 112, Row = 100, FrameWide = 92, FrameTall = 88, Gap = 30, Head = 82, Pad = 22, Apart = 90;

    /// <summary>The most frames a column holds before the depth takes another column.</summary>
    public const int Rows = 6;

    private static readonly Lazy<BoardLayout> Built = new(() => Of(BoardDistricts.All, Depths(BoardThings.Ids, id => BoardThings.Of(id)?.Parts, BoardThings.SourcesOf).GetValueOrDefault));

    /// <summary>The board's layout, worked out once from Content.</summary>
    public static BoardLayout Main => Built.Value;

    public IReadOnlyDictionary<string, BoardFrame> Frames { get; }

    public IReadOnlyList<BoardBlock> Blocks { get; }

    public IReadOnlyList<BoardHead> Heads { get; }

    public double Wide { get; }

    public double Tall { get; }

    private BoardLayout(IReadOnlyDictionary<string, BoardFrame> frames, IReadOnlyList<BoardBlock> blocks, IReadOnlyList<BoardHead> heads, double wide, double tall) =>
        (Frames, Blocks, Heads, Wide, Tall) = (frames, blocks, heads, wide, tall);

    /// <summary>How many steps from raw <paramref name="id"/> is; 0 for anything off the board.</summary>
    public static int DepthOf(string id) => Main.Frames.TryGetValue(id, out var frame) ? frame.Depth : 0;

    /// <summary>The layout of <paramref name="districts"/>, each thing at its <paramref name="depth"/>.</summary>
    public static BoardLayout Of(IReadOnlyList<BoardDistrict> districts, Func<string, int> depth)
    {
        var frames = new Dictionary<string, BoardFrame>(StringComparer.Ordinal);
        var blocks = new List<BoardBlock>();
        var heads = new List<BoardHead>();
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
                    heads.Add(new BoardHead(district.Id, column.Key, x, top + Head - 26, across * Cell));
                    for (int i = 0; i < things.Count; i++)
                        frames[things[i]] = new BoardFrame(things[i], district.Id, column.Key, x + i / Rows * Cell + Cell / 2, top + Head + i % Rows * Row + Row / 2);
                    rows = Math.Max(rows, Math.Min(Rows, things.Count));
                    x += across * Cell + Gap;
                }
                double blockWide = x - Gap + Pad - left, blockTall = Head + rows * Row + Pad;
                blocks.Add(new BoardBlock(district.Id, left, top, blockWide, blockTall));
                left += blockWide + Apart;
                tall = Math.Max(tall, blockTall);
            }
            wide = Math.Max(wide, left - Apart);
            top += tall + Apart;
        }
        return new BoardLayout(frames, blocks, heads, wide, Math.Max(0, top - Apart));
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
