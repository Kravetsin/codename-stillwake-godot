using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// How a block of the tree ties to the others (2026-10-30-tech-rings), worked out from the recipes: what it takes, where it is made,
/// and what takes it, known or not, so a player sees the way ahead before buying.
/// </summary>
/// <param name="Takes">What one of it takes: what is bought or raw alike.</param>
/// <param name="From">The blocks that open what it takes, and the stations that smelt, crush or grow what it takes.</param>
/// <param name="MadeAt">The blocks of the stations it is made at; none for what is built where it stands.</param>
/// <param name="UsedIn">The blocks that take what it makes, or are made at it.</param>
public sealed record TechLink(IReadOnlyList<(string Item, int Count)> Takes, IReadOnlyList<string> From, IReadOnlyList<string> MadeAt, IReadOnlyList<string> UsedIn);

/// <summary>Every block's <see cref="TechLink"/>.</summary>
public static class TechLinks
{
    private static readonly Dictionary<string, Blueprint> Prints = Blueprints.All.ToDictionary(b => b.Id, StringComparer.Ordinal);

    private static readonly Dictionary<string, TechLink> ById = Content.Research.Nodes.Where(n => !n.Gate).ToDictionary(n => n.Id, Worked, StringComparer.Ordinal);

    /// <summary>How block <paramref name="node"/> ties to the others.</summary>
    public static TechLink Of(TechNode node) => ById[node.Id];

    /// <summary>What <paramref name="id"/> costs to make or build.</summary>
    public static IReadOnlyList<(string Item, int Count)> CostOf(string id) => Upgrades.Of(id) != null ? [] : Crafts.TryGet(id, out var craft) ? craft.Cost : Prints.TryGetValue(id, out var print) ? print.Cost : [];

    private static TechLink Worked(TechNode node)
    {
        var takes = CostOf(node.Id);
        var from = takes.SelectMany(t => Sources(t.Item)).Where(id => id != node.Id).Distinct().ToList();
        var madeAt = StationsOf(node.Id).Where(id => id != node.Id).Distinct().ToList();
        var used = Content.Research.Nodes.Where(n => !n.Gate && n != node && (CostOf(n.Id).Any(c => Makes(node, c.Item)) || StationsOf(n.Id).Contains(node.Id)))
            .Select(n => n.Id).ToList();
        return new TechLink(takes, from, madeAt, used);
    }

    private static bool Makes(TechNode node, string item) => node.Id == item || Sources(item).Contains(node.Id);

    private static bool IsBlock(string id) => Content.Research.Node(id) is { Gate: false };

    /// <summary>The blocks <paramref name="item"/> comes from: the one opening its craft, and the stations that smelt, crush or grow it out of something.</summary>
    private static IEnumerable<string> Sources(string item)
    {
        if (IsBlock(item) && Crafts.TryGet(item, out _)) yield return item;
        foreach (var source in Content.Items.All.Where(i => i.Yields?.Any(y => y.Item == item) == true))
            foreach (var station in Stations.All.Where(s => s.Fed && s.Process == source.Process && s.Heat >= source.Heat))
                if (IsBlock(station.Machine)) yield return station.Machine;
    }

    /// <summary>The blocks of the stations that make the craft <paramref name="id"/>.</summary>
    private static IEnumerable<string> StationsOf(string id) =>
        Stations.All.Where(s => s.Rows.Any(r => r.Contains(id)) && IsBlock(s.Machine)).Select(s => s.Machine);
}
