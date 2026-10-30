using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;

namespace Stillwake.Game.Features.Research;

/// <summary>Where a node stands on the tree's map: its column, how far along what it needs, and its row, counted from the top.</summary>
public readonly record struct TechSpot(int Column, int Row);

/// <summary>A category's band across the map: the rows it spans.</summary>
public readonly record struct TechBand(TechCategory Category, int FirstRow, int Rows);

/// <summary>
/// The tree laid out by itself (2026-10-30-tech-graph): a node's column is the longest way to it through what it needs, so a line
/// always runs left to right, and each category is a band of rows holding its nodes, so a new node needs no coordinates.
/// </summary>
public sealed class TechMap
{
    /// <summary>Where each node stands, by id.</summary>
    public IReadOnlyDictionary<string, TechSpot> Spots { get; }

    /// <summary>The bands, top to bottom.</summary>
    public IReadOnlyList<TechBand> Bands { get; }

    /// <summary>How many columns the map is wide.</summary>
    public int Columns { get; }

    /// <summary>How many rows the map is tall.</summary>
    public int Rows { get; }

    public TechMap(IReadOnlyList<TechCategory> categories)
    {
        var nodes = categories.SelectMany(c => c.Nodes).ToDictionary(n => n.Id);
        var columns = new Dictionary<string, int>();
        int ColumnOf(TechNode node)
        {
            if (columns.TryGetValue(node.Id, out int known)) return known;
            int column = node.Needs.Length == 0 ? 0 : node.Needs.Max(n => ColumnOf(nodes[n])) + 1;
            columns[node.Id] = column;
            return column;
        }

        var spots = new Dictionary<string, TechSpot>();
        var bands = new List<TechBand>();
        int first = 0;
        foreach (var category in categories)
        {
            // Each node takes the first row of its band free at its column, in the order the category lists them.
            var taken = new HashSet<(int Column, int Row)>();
            int rows = 1;
            foreach (var node in category.Nodes)
            {
                int column = ColumnOf(node), row = 0;
                while (!taken.Add((column, row))) row++;
                rows = System.Math.Max(rows, row + 1);
                spots[node.Id] = new TechSpot(column, first + row);
            }
            bands.Add(new TechBand(category, first, rows));
            first += rows;
        }
        (Spots, Bands, Columns, Rows) = (spots, bands, columns.Values.Max() + 1, first);
    }
}
