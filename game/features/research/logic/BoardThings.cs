using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Features.Research;

/// <summary>A blueprint or craft as the board sees it (docs/decisions/2026-11-05-the-board.md).</summary>
/// <param name="Parts">Each thing it costs: one of each ever had is enough.</param>
/// <param name="Stations">The machines that make it, for a craft; null for a blueprint.</param>
/// <param name="Market">Whether only the market opens it: its parts never bring it nearer.</param>
public sealed record BoardThing(string Id, IReadOnlyList<string> Parts, IReadOnlyList<string>? Stations, bool Market);

/// <summary>Every thing the board holds a frame for: every item one carries, every blueprint and craft. Upgrades are the market page's rows, never frames.</summary>
public static class BoardThings
{
    /// <summary>Every blueprint, then every craft, in content order.</summary>
    public static readonly IReadOnlyList<BoardThing> All =
    [
        .. Blueprints.All.Select(b => Of(b.Id, b.Cost, null)),
        .. Crafts.All.Select(c => Of(c.Item, c.Cost, Stations.All.Where(s => s.Rows.Any(r => r.Contains(c.Item))).Select(s => s.Machine).ToList())),
    ];

    private static readonly Dictionary<string, BoardThing> ById = All.GroupBy(t => t.Id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

    /// <summary>Every id with a frame: items but containers, wreckage and the like, then blueprints and crafts, then the drone's forms (drone-lessons).</summary>
    public static readonly IReadOnlyList<string> Ids =
        [.. Content.Items.All.Where(i => i.Group != ItemGroup.Other).Select(i => i.Id).Concat(All.Select(t => t.Id)).Concat(Lessons.Forms).Distinct(StringComparer.Ordinal)];

    /// <summary>Whether <paramref name="id"/> is a form the drone learns (drone-lessons): drawn as its thread icon, never pencilled in.</summary>
    public static bool Form(string id) => Lessons.Forms.Contains(id, StringComparer.Ordinal);

    /// <summary>The machine kinds that make or work something.</summary>
    public static readonly IReadOnlyList<string> Machines = [.. Stations.All.Select(s => s.Machine).Distinct(StringComparer.Ordinal)];

    /// <summary>The blueprint or craft <paramref name="id"/>; null for an item no recipe makes.</summary>
    public static BoardThing? Of(string id) => ById.GetValueOrDefault(id);

    /// <summary>Whether only the black market opens <paramref name="id"/>.</summary>
    public static bool Market(string id) => Content.Research.Node(id) is { Tab: Content.Research.Market };

    /// <summary>Whether <paramref name="id"/> is a machine that makes or works something.</summary>
    public static bool Machine(string id) => Machines.Contains(id, StringComparer.Ordinal);

    /// <summary>What <paramref name="item"/> is smelted, crushed, grown or fermented out of as a yield, never a byproduct: none for anything else.</summary>
    public static IReadOnlyList<string> SourcesOf(string item) => Sources.GetValueOrDefault(item) ?? [];

    private static readonly Dictionary<string, List<string>> Sources = Content.Items.All
        .SelectMany(i => (i.Yields ?? []).Select(y => (Out: y.Item, From: i.Id)))
        .GroupBy(p => p.Out, StringComparer.Ordinal)
        .ToDictionary(g => g.Key, g => g.Select(p => p.From).Distinct(StringComparer.Ordinal).ToList(), StringComparer.Ordinal);

    private static BoardThing Of(string id, IReadOnlyList<(string Item, int Count)> cost, IReadOnlyList<string>? stations) => new(
        id,
        cost.Select(c => c.Item).Distinct(StringComparer.Ordinal).ToList(),
        stations,
        Market(id));
}
