using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>A part a hero makes by hand, one unit at a time: what one unit costs and how long it takes.</summary>
/// <param name="Item">The item made, one a unit; the craft is named by it.</param>
/// <param name="Cost">Whole units of items one unit takes, all of a queued craft's taken when it is queued.</param>
/// <param name="Ticks">Ticks one unit takes, 20 to a second.</param>
public sealed record Craft(string Item, IReadOnlyList<(string Item, int Count)> Cost, int Ticks);

/// <summary>A tab of the crafts window, where every craft has a place that never moves.</summary>
/// <param name="Id">Its name's key in the client, <c>crafts.tab.&lt;Id&gt;</c>.</param>
/// <param name="Icon">The item its button shows.</param>
/// <param name="Rows">Its rows from the top, each a family of crafts by the item made, simple to complex left to right.</param>
public sealed record CraftTab(string Id, string Icon, IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>What a hero crafts by hand, by the item made (the crafting module).</summary>
public static class Crafts
{
    public static readonly IReadOnlyList<Craft> All =
    [
        new("wire", [("scrap", 2)], Ticks: 20),
        new("plate", [("iron", 2)], Ticks: 40),
        new("gear", [("iron", 1)], Ticks: 40),
        new("rotor", [("gear", 1), ("wire", 2)], Ticks: 60),
    ];

    /// <summary>Where each craft sits in the crafts window: every craft once, known or not.</summary>
    public static readonly IReadOnlyList<CraftTab> Tabs =
    [
        new("parts", Icon: "gear", Rows: [["plate", "gear", "rotor"], ["wire"]]),
    ];

    /// <summary>The most crafts a row holds: the window's columns.</summary>
    public const int RowMost = 9;

    private static readonly Dictionary<string, Craft> ByItem = All.ToDictionary(c => c.Item, StringComparer.Ordinal);

    /// <summary>The craft making <paramref name="item"/>; throws for an item no craft makes.</summary>
    public static Craft Get(string item) => ByItem[item];

    public static bool TryGet(string item, [MaybeNullWhen(false)] out Craft craft) => ByItem.TryGetValue(item, out craft);
}

/// <summary>A hero's crafting queue.</summary>
public static class Crafting
{
    /// <summary>The most crafts one hero's queue holds; each may make many units.</summary>
    public const int QueueMost = 9;
}
