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
/// <param name="Takes">What one of it takes, first thing it opens first: what is bought or raw alike.</param>
/// <param name="From">The blocks that open what it takes, and the stations that smelt, crush or grow what it takes.</param>
/// <param name="MadeAt">The blocks of the stations it is made at; none for what is built where it stands.</param>
/// <param name="UsedIn">The blocks that take what it makes, or are made at it.</param>
public sealed record TechLink(IReadOnlyList<(string Item, int Count)> Takes, IReadOnlyList<string> From, IReadOnlyList<string> MadeAt, IReadOnlyList<string> UsedIn);

/// <summary>Every block's <see cref="TechLink"/>.</summary>
public static class TechLinks
{
    private static readonly Dictionary<string, Blueprint> Prints = Blueprints.All.ToDictionary(b => b.Id, StringComparer.Ordinal);

    /// <summary>Which block opens each id.</summary>
    private static readonly Dictionary<string, string> Opener = Content.Research.Nodes
        .SelectMany(n => n.Opens.Select(id => (id, n.Id))).ToDictionary(p => p.id, p => p.Item2, StringComparer.Ordinal);

    private static readonly Dictionary<string, TechLink> ById = Content.Research.Nodes.Where(n => !n.Gate).ToDictionary(n => n.Id, Worked, StringComparer.Ordinal);

    /// <summary>How block <paramref name="node"/> ties to the others.</summary>
    public static TechLink Of(TechNode node) => ById[node.Id];

    /// <summary>What <paramref name="id"/> costs to make or build.</summary>
    public static IReadOnlyList<(string Item, int Count)> CostOf(string id) => Crafts.TryGet(id, out var craft) ? craft.Cost : Prints.TryGetValue(id, out var print) ? print.Cost : [];

    private static TechLink Worked(TechNode node)
    {
        var takes = node.Opens.SelectMany(CostOf).GroupBy(c => c.Item, StringComparer.Ordinal).Select(g => (g.Key, g.Sum(c => c.Count))).ToList();
        var from = takes.SelectMany(t => Sources(t.Item1)).Where(id => id != node.Id).Distinct().ToList();
        var madeAt = node.Opens.SelectMany(StationsOf).Where(id => id != node.Id).Distinct().ToList();
        var used = Content.Research.Nodes.Where(n => !n.Gate && n != node && (n.Opens.SelectMany(CostOf).Any(c => Makes(node, c.Item)) || n.Opens.SelectMany(StationsOf).Contains(node.Id)))
            .Select(n => n.Id).ToList();
        return new TechLink(takes, from, madeAt, used);
    }

    private static bool Makes(TechNode node, string item) => node.Opens.Contains(item) || Sources(item).Contains(node.Id);

    /// <summary>The blocks <paramref name="item"/> comes from: the one opening its craft, and the stations that smelt, crush or grow it out of something.</summary>
    private static IEnumerable<string> Sources(string item)
    {
        if (Opener.TryGetValue(item, out string? opener) && Crafts.TryGet(item, out _)) yield return opener;
        foreach (var source in Content.Items.All.Where(i => i.Yields?.Any(y => y.Item == item) == true))
        {
            foreach (var station in Stations.All.Where(s => s.Fed && s.Process == source.Process && s.Heat >= source.Heat))
                if (Opener.TryGetValue(station.Machine, out string? smelter)) yield return smelter;
        }
    }

    /// <summary>The blocks of the stations that make the craft <paramref name="id"/>.</summary>
    private static IEnumerable<string> StationsOf(string id)
    {
        foreach (var station in Stations.All.Where(s => s.Rows.Any(r => r.Contains(id))))
            if (Opener.TryGetValue(station.Machine, out string? block)) yield return block;
    }
}
