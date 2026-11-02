using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;

namespace Stillwake.Game.Features.Research;

/// <summary>Where a block stands on a tab's map, in pixels from the map's top-left: its middle.</summary>
public readonly record struct RingSpot(double X, double Y);

/// <summary>
/// A tab of the tree laid out by itself (2026-10-30-tech-rings): its gate's hub in the middle, a block's ring and branch saying how
/// far out and which way it stands, so a new block needs no coordinates. A ring whose branch holds more than its arc takes grows
/// another row outwards, and the rings beyond move out to make room.
/// </summary>
public sealed class TechRings
{
    /// <summary>How far from the middle the hub reaches, and the first ring's first row stands.</summary>
    public const double HubRadius = 96, FirstRow = 200;
    /// <summary>A node's room: along an arc, and from one row to the next; how much further the next ring starts than a next row.</summary>
    public const double Along = 116, Across = 112, RingGap = 26;

    private readonly double _middle;

    /// <summary>Where each block stands, by id.</summary>
    public IReadOnlyDictionary<string, RingSpot> Spots { get; }

    /// <summary>How far out each ring reaches, from the middle: where its edge is drawn.</summary>
    public IReadOnlyList<double> Edges { get; }

    /// <summary>The map's side: it is square, its hub in the middle.</summary>
    public double Side => 2 * _middle;

    /// <summary>The hub's middle, on both axes.</summary>
    public double Middle => _middle;

    public TechRings(TechTab tab)
    {
        var blocks = Content.Research.Nodes.Where(n => n.Tab == tab.Id && !n.Gate).ToList();
        double wedge = 2 * Math.PI / tab.Branches.Length, radius = FirstRow;
        var polar = new List<(string Id, double Radius, double Angle)>();
        var edges = new List<double>();
        for (int ring = 0; ring < tab.Rings.Length; ring++)
        {
            int rows = 1;
            for (int b = 0; b < tab.Branches.Length; b++)
            {
                var here = blocks.Where(n => n.Ring == ring && n.Branch == tab.Branches[b]).ToList();
                double centre = -Math.PI / 2 + b * wedge, at = radius;
                for (int row = 0, placed = 0; placed < here.Count; row++, at += Across)
                {
                    int fits = Math.Max(1, (int)(wedge * at * 0.92 / Along));
                    var part = here.Skip(placed).Take(fits).ToList();
                    for (int i = 0; i < part.Count; i++)
                        polar.Add((part[i].Id, at, centre + (i - (part.Count - 1) / 2.0) * Along / at));
                    placed += part.Count;
                    rows = Math.Max(rows, row + 1);
                }
            }
            radius += rows * Across + RingGap;
            edges.Add(radius - RingGap / 2 - Across / 2);
        }
        _middle = edges[^1] + Along;
        Spots = polar.ToDictionary(p => p.Id, p => new RingSpot(_middle + p.Radius * Math.Cos(p.Angle), _middle + p.Radius * Math.Sin(p.Angle)));
        Edges = edges;
    }
}
