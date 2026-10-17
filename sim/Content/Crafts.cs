using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>A part a crafting machine makes, one unit at a time: what one unit costs and how long it takes.</summary>
/// <param name="Item">The item made, one a unit; the craft is named by it.</param>
/// <param name="Cost">Whole units of items one unit takes, all of a queued craft's taken when it is queued.</param>
/// <param name="Ticks">Ticks one unit takes at full speed, 20 to a second.</param>
public sealed record Craft(string Item, IReadOnlyList<(string Item, int Count)> Cost, int Ticks);

/// <summary>What the lifepod's Fabricator makes, by the item made (the crafting module).</summary>
public static class Crafts
{
    public static readonly IReadOnlyList<Craft> All =
    [
        new("wire", [("scrap", 2)], Ticks: 20),
        new("plate", [("iron", 2)], Ticks: 40),
        new("gear", [("iron", 1)], Ticks: 40),
        new("tube", [("iron", 1)], Ticks: 40),
        new("rotor", [("gear", 1), ("wire", 2)], Ticks: 60),
        new("frame", [("plate", 2)], Ticks: 80),
        new("coil", [("wire", 3)], Ticks: 60),
        new("fireAxe", [("iron", 2), ("planks", 1)], Ticks: 60),
        new("harpoon", [("planks", 1), ("frame", 1), ("tube", 1), ("coil", 1), ("wire", 4)], Ticks: 120),
        new("jetpack", [("frame", 1), ("tube", 2), ("canister", 1), ("wire", 2)], Ticks: 120),
    ];

    /// <summary>
    /// Where each craft sits in the Fabricator's panel, a family a row, simple to complex left to right:
    /// every craft once, known or not, so nothing moves when one opens.
    /// </summary>
    public static readonly IReadOnlyList<IReadOnlyList<string>> Rows = [["plate", "gear", "tube", "frame"], ["wire", "coil", "rotor"], ["fireAxe", "harpoon", "jetpack"]];

    /// <summary>The most crafts a row holds: the panel's columns.</summary>
    public const int RowMost = 4;

    /// <summary>
    /// The lifepod's speed, in thousandths of full speed: work it adds a tick, a unit taking its
    /// <see cref="Craft.Ticks"/> × 1000. A fifth: the pod is the most basic bench, always there, not the
    /// factory; machines for parts, faster and with longer queues, come later.
    /// </summary>
    public const int PodSpeed = 200;

    private static readonly Dictionary<string, Craft> ByItem = All.ToDictionary(c => c.Item, StringComparer.Ordinal);

    /// <summary>The craft making <paramref name="item"/>; throws for an item no craft makes.</summary>
    public static Craft Get(string item) => ByItem[item];

    public static bool TryGet(string item, [MaybeNullWhen(false)] out Craft craft) => ByItem.TryGetValue(item, out craft);
}

/// <summary>A crafting machine's queue, shared by the crew.</summary>
public static class Crafting
{
    /// <summary>The most crafts one machine's queue holds; each may make many units.</summary>
    public const int QueueMost = 4;
}
