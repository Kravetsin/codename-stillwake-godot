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
/// <param name="From">What it is smelted, crushed or grown out of, as a yield or a byproduct, any of them: iron of hull plates or iron ore.</param>
public sealed record TechLink(IReadOnlyList<(string Item, int Count)> Takes, IReadOnlyList<string> MadeAt, IReadOnlyList<string> UsedIn, IReadOnlyList<string> From);

/// <summary>How a thing comes to be, the first line of its card.</summary>
public enum Origin
{
    /// <summary>A recipe takes parts: the card lists them.</summary>
    Made,
    /// <summary>A fed station smelts, crushes or grows it out of something: the card names what.</summary>
    Worked,
    /// <summary>A craft of no parts, the pod's: made from nothing.</summary>
    Free,
    /// <summary>Nothing makes it: only found, caught, dug or hunted.</summary>
    Found,
    /// <summary>A blueprint or upgrade of no parts: the card says nothing of where it comes from.</summary>
    None,
}

/// <summary>How one thing ties to a picked one, in the colour of its thread.</summary>
public enum Tie
{
    /// <summary>The picked thing is made, smelted or grown at it.</summary>
    MadeAt,
    /// <summary>The picked thing takes it or comes out of it.</summary>
    From,
    /// <summary>It takes the picked thing, is made at it or comes out of it.</summary>
    UsedIn,
}

/// <summary>Every thing's <see cref="TechLink"/>: an item, a blueprint, a craft or a block of the tree.</summary>
public static class TechLinks
{
    private static readonly Dictionary<string, Blueprint> Prints = Blueprints.All.ToDictionary(b => b.Id, StringComparer.Ordinal);

    /// <summary>Everything that may take something or be made at something.</summary>
    private static readonly string[] Users = [.. Blueprints.All.Select(b => b.Id).Concat(Crafts.All.Select(c => c.Item)).Concat(Content.Research.Nodes.Where(n => !n.Gate).Select(n => n.Id)).Distinct(StringComparer.Ordinal)];

    private static readonly ConcurrentDictionary<string, TechLink> ById = new(StringComparer.Ordinal);

    /// <summary>How <paramref name="id"/> ties to the others, worked out the first time it is asked.</summary>
    public static TechLink Of(string id) => ById.GetOrAdd(id, Worked);

    /// <summary>How <paramref name="id"/> ties to <paramref name="picked"/>, as the board lights it and the search lists it; none if not directly.</summary>
    public static Tie? TieOf(string picked, string id)
    {
        var link = Of(picked);
        return link.MadeAt.Contains(id) ? Tie.MadeAt : link.Takes.Any(t => t.Item == id) || link.From.Contains(id) ? Tie.From : link.UsedIn.Contains(id) ? Tie.UsedIn : null;
    }

    /// <summary>What <paramref name="id"/> costs to make or build.</summary>
    public static IReadOnlyList<(string Item, int Count)> CostOf(string id) => Upgrades.Of(id) != null ? [] : Crafts.TryGet(id, out var craft) ? craft.Cost : Prints.TryGetValue(id, out var print) ? print.Cost : [];

    /// <summary>How <paramref name="id"/> comes to be: "only found" is said of what no recipe, blueprint, upgrade or station makes.</summary>
    public static Origin OriginOf(string id)
    {
        var link = Of(id);
        if (link.Takes.Count > 0) return Origin.Made;
        if (link.From.Count > 0) return Origin.Worked;
        if (Crafts.TryGet(id, out _)) return Origin.Free;
        return Prints.ContainsKey(id) || Upgrades.Of(id) != null || Content.Research.Node(id) != null ? Origin.None : Origin.Found;
    }

    private static TechLink Worked(string thing)
    {
        var madeAt = StationsOf(thing).Concat(WorkedAt(thing)).Where(id => id != thing).Distinct().ToList();
        var outs = Content.Items.TryGet(thing, out var spec) && spec.Yields != null ? Out(spec) : [];
        var used = Users.Where(id => id != thing && (CostOf(id).Any(c => c.Item == thing) || StationsOf(id).Contains(thing))).Concat(ComesOutOf(thing)).Concat(outs).Distinct().ToList();
        var from = Content.Items.All.Where(i => Out(i).Contains(thing)).Select(i => i.Id).ToList();
        return new TechLink(CostOf(thing), madeAt, used, from);
    }

    /// <summary>The build menu's tab (<see cref="MenuTabs"/>) the blueprint <paramref name="id"/> is built from; null for anything else.</summary>
    public static string? MenuTabOf(string id) => !Prints.TryGetValue(id, out var print) ? null : print.Placement switch
    {
        { Layer: "machine", Kind: var kind } => Machines.All.FirstOrDefault(m => m.Kind == kind)?.Menu?.Tab,
        { Kind: "wall", Material: var material } => Materials.All.FirstOrDefault(m => m.Id == material)?.Menu?.Tab,
        { Kind: "backWall", Material: var material } => Materials.All.FirstOrDefault(m => m.Id == material)?.BackMenu?.Tab,
        { Kind: var kind } => Builds.All.FirstOrDefault(b => b.Id == kind)?.Menu?.Tab,
    };

    private static bool Built(string id) => Prints.ContainsKey(id);

    /// <summary>What a fed station makes of <paramref name="source"/>: its yields and its byproducts.</summary>
    private static IEnumerable<string> Out(ItemSpec source) =>
        (source.Yields ?? []).Select(y => y.Item).Concat((source.Byproducts ?? []).Select(b => b.Item));

    /// <summary>The stations a player builds that smelt, crush or grow <paramref name="item"/> out of something.</summary>
    private static IEnumerable<string> WorkedAt(string item) =>
        Content.Items.All.Where(i => i.Yields != null && Out(i).Contains(item)).SelectMany(ComingFrom);

    /// <summary>What comes out of what station <paramref name="machine"/> works.</summary>
    private static IEnumerable<string> ComesOutOf(string machine) =>
        Content.Items.All.Where(i => i.Yields != null && ComingFrom(i).Contains(machine)).SelectMany(Out);

    private static IEnumerable<string> ComingFrom(ItemSpec source) =>
        Stations.All.Where(s => s.Fed && s.Process == source.Process && s.Heat >= source.Heat && Built(s.Machine)).Select(s => s.Machine);

    /// <summary>The stations a player builds that make the craft <paramref name="id"/>.</summary>
    private static IEnumerable<string> StationsOf(string id) =>
        Stations.All.Where(s => s.Rows.Any(r => r.Contains(id)) && Built(s.Machine)).Select(s => s.Machine);
}
