using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>A number of its host a module trades (0100): a station's speed, what a machine or tool draws, a tool's damage, a fed station's heat.</summary>
public enum Stat { Speed, Draw, Damage, Heat }

/// <summary>
/// What a module does in a host (0100), one kind of effect a record: a <see cref="Trade"/> today; a residual effect, a hit's
/// shock that lingers, is the next kind.
/// </summary>
public abstract record ModuleEffect;

/// <summary>
/// A trade (0100): <paramref name="Add"/> added to its host's <paramref name="Stat"/>, in thousandths added to 1000 for speed,
/// draw and damage, so +1000 is twice and +4000 five times; in whole levels for heat. Trades on one stat sum, a host's modules'
/// too, before they are applied once: gains round down, costs up.
/// </summary>
public sealed record Trade(Stat Stat, int Add) : ModuleEffect;

/// <summary>
/// What a module does in some of its hosts (0100): <paramref name="Hosts"/> by kind, electric machines by their machine's kind and
/// electric tools by their item (0098), and its <paramref name="Effects"/> in each of them.
/// </summary>
public sealed record HostEffects(IReadOnlyList<string> Hosts, IReadOnlyList<ModuleEffect> Effects);

/// <summary>
/// A module (0086): an item fitted into a machine by hand, Valheim's way: one of each kind a machine at most, taken out again by
/// hand, never moved by drones, never loot; or onto a tool's stack at an arc bench (0099). What it does is its own in each host
/// (0100): a station runs faster, the charger fills faster, the drill hits harder, each for more power.
/// </summary>
/// <param name="Item">The item it is, by its id.</param>
/// <param name="Tech">
/// Its technology, by id: the client's icon for it, shown on the sockets that take it, on the slots of modules that fit
/// the machine open and on its card, in place of any list of where it goes (0086).
/// </param>
/// <param name="On">What it fits into and does there, a host in one entry at most (<c>ContentTests</c>).</param>
public sealed record ModuleSpec(string Item, string Tech, IReadOnlyList<HostEffects> On)
{
    /// <summary>Every host it fits, in <see cref="On"/>'s order.</summary>
    public IEnumerable<string> Hosts => On.SelectMany(o => o.Hosts);

    /// <summary>What it does in a host of <paramref name="host"/>; none where it fits not. Never a new list.</summary>
    public IReadOnlyList<ModuleEffect> In(string host)
    {
        for (int i = 0; i < On.Count; i++)
            for (int j = 0; j < On[i].Hosts.Count; j++)
                if (On[i].Hosts[j] == host) return On[i].Effects;
        return [];
    }

    /// <summary>Whether it fits into a host of <paramref name="host"/>.</summary>
    public bool Fits(string host)
    {
        for (int i = 0; i < On.Count; i++)
            for (int j = 0; j < On[i].Hosts.Count; j++)
                if (On[i].Hosts[j] == host) return true;
        return false;
    }

    /// <summary>What its trades add to <paramref name="stat"/> in a host of <paramref name="host"/>, summed; 0 where it fits not.</summary>
    public int Adds(string host, Stat stat)
    {
        var effects = In(host);
        int sum = 0;
        for (int i = 0; i < effects.Count; i++)
            if (effects[i] is Trade trade && trade.Stat == stat) sum += trade.Add;
        return sum;
    }
}

/// <summary>Every module, by its item (0086).</summary>
public static class Modules
{
    /// <summary>
    /// The arc module, of a sky shard (0096), the arc branch's first (0100): twice the speed for twice the power in an electric
    /// station, twice the damage for twice the draw in the drill (0098), five times the charging for five times the draw in the
    /// charger, whose draw is what it charges (the user's call: a fair trade; an accumulator still charges without it). The
    /// furnace and the pod take none: one burns planks, the other is paid by hand; nor the drone bay, yet.
    /// </summary>
    public static readonly ModuleSpec Arc = new("arcModule", "arc",
    [
        new(["smelter", "former", "assembler"], [new Trade(Stat.Speed, 1000), new Trade(Stat.Draw, 1000)]),
        new(["drill"], [new Trade(Stat.Damage, 1000), new Trade(Stat.Draw, 1000)]),
        new(["charger"], [new Trade(Stat.Draw, 4000)]),
    ]);

    public static readonly IReadOnlyList<ModuleSpec> All = [Arc];

    private static readonly Dictionary<string, ModuleSpec> ByItem = All.ToDictionary(m => m.Item, StringComparer.Ordinal);

    /// <summary>The module <paramref name="item"/> is; throws for an item that is none.</summary>
    public static ModuleSpec Get(string item) => ByItem[item];

    public static bool TryGet(string? item, [MaybeNullWhen(false)] out ModuleSpec module)
    {
        module = null;
        return item != null && ByItem.TryGetValue(item, out module);
    }

    /// <summary>Whether module <paramref name="item"/> fits into a host of <paramref name="kind"/>.</summary>
    public static bool Fits(string? item, string kind) => TryGet(item, out var module) && module.Fits(kind);

    /// <summary>The modules a host of <paramref name="kind"/> takes, a socket each, in <see cref="All"/>'s order; none for most.</summary>
    public static IEnumerable<ModuleSpec> For(string kind) => All.Where(m => m.Fits(kind));

    /// <summary>Whether some module fits a host of <paramref name="kind"/>: a machine, or a tool an arc bench takes (0099).</summary>
    public static bool Hosted(string? kind) => kind != null && All.Any(m => m.Fits(kind));
}
