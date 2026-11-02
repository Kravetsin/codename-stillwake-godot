using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// How a thing ties to the others (2026-10-30-tech-rings), worked out from the recipes: what it takes, where it is made,
/// and what takes it, known or not, so a player sees the way ahead. Only direct ties: a furnace ties to iron and not to all that iron goes into, for the player follows a chain themselves ([board-thoughts](../../../../docs/decisions/2026-11-02-board-thoughts.md)).
/// </summary>
/// <param name="Takes">What one of it takes: what is bought or raw alike.</param>
/// <param name="MadeAt">The stations it is made at, smelted, crushed or grown in, those a player builds; none for what is built where it stands.</param>
/// <param name="UsedIn">The things that take it, are made at it, or come out of it.</param>
public sealed record TechLink(IReadOnlyList<(string Item, int Count)> Takes, IReadOnlyList<string> MadeAt, IReadOnlyList<string> UsedIn);

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
        var madeAt = StationsOf(thing).Concat(WorkedAt(thing)).Where(id => id != thing).Distinct().ToList();
        var used = Users.Where(id => id != thing && (CostOf(id).Any(c => c.Item == thing) || StationsOf(id).Contains(thing))).Concat(ComesOutOf(thing)).Distinct().ToList();
        return new TechLink(CostOf(thing), madeAt, used);
    }

    private static bool Built(string id) => Prints.ContainsKey(id);

    /// <summary>The stations a player builds that smelt, crush or grow <paramref name="item"/> out of something.</summary>
    private static IEnumerable<string> WorkedAt(string item) =>
        Content.Items.All.Where(i => i.Yields?.Any(y => y.Item == item) == true).SelectMany(ComingFrom);

    /// <summary>What comes out of what station <paramref name="machine"/> works.</summary>
    private static IEnumerable<string> ComesOutOf(string machine) =>
        Content.Items.All.Where(i => i.Yields != null && ComingFrom(i).Contains(machine)).SelectMany(i => i.Yields!.Select(y => y.Item));

    private static IEnumerable<string> ComingFrom(ItemSpec source) =>
        Stations.All.Where(s => s.Fed && s.Process == source.Process && s.Heat >= source.Heat && Built(s.Machine)).Select(s => s.Machine);

    /// <summary>The stations a player builds that make the craft <paramref name="id"/>.</summary>
    private static IEnumerable<string> StationsOf(string id) =>
        Stations.All.Where(s => s.Rows.Any(r => r.Contains(id)) && Built(s.Machine)).Select(s => s.Machine);
}
