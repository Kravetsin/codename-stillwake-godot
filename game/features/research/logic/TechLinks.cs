using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// How a thing ties to the others (2026-10-30-tech-rings), worked out from the recipes: what it takes, where it is made,
/// and what takes it, known or not, so a player sees the way ahead.
/// </summary>
/// <param name="Takes">What one of it takes: what is bought or raw alike.</param>
/// <param name="From">The crafts of what it takes, and the stations that smelt, crush or grow what it takes.</param>
/// <param name="MadeAt">The stations it is made at, those a player builds; none for what is built where it stands.</param>
/// <param name="UsedIn">The things that take what it makes, or are made at it.</param>
public sealed record TechLink(IReadOnlyList<(string Item, int Count)> Takes, IReadOnlyList<string> From, IReadOnlyList<string> MadeAt, IReadOnlyList<string> UsedIn);

/// <summary>Every thing's <see cref="TechLink"/>: an item, a blueprint, a craft or a block of the tree.</summary>
public static class TechLinks
{
    private static readonly Dictionary<string, Blueprint> Prints = Blueprints.All.ToDictionary(b => b.Id, StringComparer.Ordinal);

    /// <summary>Everything that may take something or be made at something.</summary>
    private static readonly string[] Users = [.. Blueprints.All.Select(b => b.Id).Concat(Crafts.All.Select(c => c.Item)).Concat(Content.Research.Nodes.Where(n => !n.Gate).Select(n => n.Id)).Distinct(StringComparer.Ordinal)];

    private static readonly ConcurrentDictionary<string, TechLink> ById = new(StringComparer.Ordinal);

    /// <summary>How <paramref name="id"/> ties to the others, worked out the first time it is asked.</summary>
    public static TechLink Of(string id) => ById.GetOrAdd(id, Worked);

    /// <summary>What <paramref name="id"/> costs to make or build.</summary>
    public static IReadOnlyList<(string Item, int Count)> CostOf(string id) => Upgrades.Of(id) != null ? [] : Crafts.TryGet(id, out var craft) ? craft.Cost : Prints.TryGetValue(id, out var print) ? print.Cost : [];

    private static TechLink Worked(string thing)
    {
        var takes = CostOf(thing);
        var from = takes.SelectMany(t => Sources(t.Item)).Where(id => id != thing).Distinct().ToList();
        var madeAt = StationsOf(thing).Where(id => id != thing).Distinct().ToList();
        var used = Users.Where(id => id != thing && (CostOf(id).Any(c => Makes(thing, c.Item)) || StationsOf(id).Contains(thing))).ToList();
        return new TechLink(takes, from, madeAt, used);
    }

    private static bool Makes(string thing, string item) => thing == item || Sources(item).Contains(thing);

    private static bool Built(string id) => Prints.ContainsKey(id);

    /// <summary>What <paramref name="item"/> comes from: its craft, and the stations that smelt, crush or grow it out of something.</summary>
    private static IEnumerable<string> Sources(string item)
    {
        if (Crafts.TryGet(item, out _)) yield return item;
        foreach (var source in Content.Items.All.Where(i => i.Yields?.Any(y => y.Item == item) == true))
            foreach (var station in Stations.All.Where(s => s.Fed && s.Process == source.Process && s.Heat >= source.Heat))
                if (Built(station.Machine)) yield return station.Machine;
    }

    /// <summary>The stations a player builds that make the craft <paramref name="id"/>.</summary>
    private static IEnumerable<string> StationsOf(string id) =>
        Stations.All.Where(s => s.Rows.Any(r => r.Contains(id)) && Built(s.Machine)).Select(s => s.Machine);
}
