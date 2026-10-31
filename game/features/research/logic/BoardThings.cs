using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Features.Research;

/// <summary>A blueprint or craft as the board sees it (docs/plans/board.md).</summary>
/// <param name="Parts">Each thing it costs, and how many of it are enough.</param>
/// <param name="Stations">The machines that make it, for a craft; null for a blueprint.</param>
/// <param name="Market">Whether only the market opens it: no slot, thread or silhouette till bought.</param>
/// <param name="Start">Whether the wreck knows it from the start: written down when understood, however early it can be built.</param>
public sealed record BoardThing(string Id, IReadOnlyList<(string Item, int Enough)> Parts, IReadOnlyList<string>? Stations, bool Market, bool Start);

/// <summary>Every blueprint and craft the board may show. Upgrades are the market page's rows, never notes.</summary>
public static class BoardThings
{
    /// <summary>Every blueprint, then every craft, in content order: what breaks a tie between threads.</summary>
    public static readonly IReadOnlyList<BoardThing> All =
    [
        .. Blueprints.All.Select(b => Of(b.Id, b.Cost, null)),
        .. Crafts.All.Select(c => Of(c.Item, c.Cost, Stations.All.Where(s => s.Rows.Any(r => r.Contains(c.Item))).Select(s => s.Machine).ToList())),
    ];

    private static BoardThing Of(string id, IReadOnlyList<(string Item, int Count)> cost, IReadOnlyList<string>? stations) => new(
        id,
        cost.Select(c => (c.Item, Understanding.Enough(c.Item))).ToList(),
        stations,
        Content.Research.Node(id) is { Tab: Content.Research.Market },
        Content.Research.StartKnown.Contains(id, StringComparer.Ordinal));
}
