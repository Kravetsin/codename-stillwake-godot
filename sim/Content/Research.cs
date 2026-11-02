namespace Stillwake.Content;

/// <summary>
/// A node of the black market: a block opening one blueprint or upgrade, bought with credits, or the gate, an order, that opens a row of
/// blocks by what has been sold (docs/plans/board.md).
/// </summary>
/// <param name="Id">Its name: a block's the one blueprint or craft it opens, named as <see cref="Research"/> says; a gate's own.</param>
/// <param name="Needs">The nodes that must be done first: their ids. A block's is its ring's gate, a gate's the gate before it.</param>
/// <param name="Order">What a gate asks to have been sold. None for a block.</param>
/// <param name="Times">How many times its worth a block costs.</param>
/// <param name="Tab">The id of the tab it is drawn on.</param>
/// <param name="Ring">Its row on that tab, from the top; a gate's is the row it opens.</param>
public sealed record TechNode(string Id, string[] Needs, IReadOnlyList<(string Item, int Count)> Order, int Times, string Tab, int Ring)
{
    /// <summary>Whether it is opened by what is sold rather than bought.</summary>
    public bool Gate => Order.Count > 0;
}

/// <summary>What a tab's row is written as: what its gate orders, none for a row open from the start, and its blocks, each with how many times its worth it costs.</summary>
public sealed record TechRing(string? Gate, IReadOnlyList<(string Item, int Count)> Order, params (string Id, int Times)[] Blocks);

/// <summary>A tab of rows, from the top; the market page holds them to what fits (a Content test).</summary>
public sealed record TechTab(string Id, params TechRing[] Rings);

/// <summary>
/// Knowing blueprints and crafts (docs/plans/board.md). Blueprints are named as the build menu names their cards: a wall
/// or a back wall by its kind and material (<c>wallIron</c>, <c>backWallScrap</c>), anything else by the grid's name for it
/// (<c>floor</c>, <c>furnace</c>); a craft by the item it makes (<c>wire</c>). Each is understood by the rule, or bought at a
/// node of <see cref="Tabs"/>. Cables are no blueprint and always known.
/// </summary>
public static class Research
{
    /// <summary>
    /// What the wreck knows from its start: nothing (docs/plans/board.md). A castaway writes down what they find, the first walls at
    /// ten chunks, and what the pod makes for nothing is understood on the first tick, having no part to wait for.
    /// </summary>
    public static readonly IReadOnlyList<string> StartKnown = [];

    /// <summary>The tab of what only credits open: what is on it stays outside the rule that grants the rest (docs/plans/board.md).</summary>
    public const string Market = "blackMarket";

    /// <summary>What a part understood must have been obtained to, in credits of its worth: a rare part takes few, a common one many.</summary>
    public const int EnoughWorth = 200;

    /// <summary>However cheap a part, no more than this many are needed to understand it.</summary>
    public const int EnoughMost = 10;

    /// <summary>An upgrade: its price its own.</summary>
    private static (string Id, int Times) Up(string id) => (id, 1);

    /// <summary>What credits buy, by tab: the market alone.</summary>
    public static readonly IReadOnlyList<TechTab> Tabs =
    [
        // Open from the start, at a price rather than a stage (black-market), its later rows by what is sold (docs/plans/board.md).
        new(Market,
            new TechRing(null, [],
                ("solarPanel", 5),
                Up("bag1"),
                Up("bag2"),
                Up("tradeSlots1")),
            new("order1", [("scrapChunk", 40), ("iron", 20), ("planks", 20)],
                Up("bag3"),
                Up("bag4"),
                Up("tradeSlots2")),
            new("order2", [("plate", 30), ("gear", 20)],
                Up("bag5"),
                Up("bag6"),
                Up("belt1"),
                Up("tradeSlots3"),
                Up("tradeSlots4")),
            new("order3", [("glass", 20), ("silicon", 20), ("frame", 10)],
                Up("bag7"),
                Up("bag8"),
                Up("tradeSlots5")),
            new("order4", [("circuit", 20)],
                Up("bag9"),
                Up("tradeSlots6")),
            new("order5", [("copper", 20), ("mica", 10), ("hide", 5)],
                Up("bag10"),
                Up("bag11"),
                Up("belt2"))),
    ];

    /// <summary>Every node: tab by tab, row by row, a row's gate before its blocks.</summary>
    public static readonly IReadOnlyList<TechNode> Nodes = [.. Tabs.SelectMany(NodesOf)];

    private static readonly Dictionary<string, TechNode> ById = Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);

    /// <summary>The node named <paramref name="id"/>; null for none.</summary>
    public static TechNode? Node(string id) => ById.GetValueOrDefault(id);

    private static IEnumerable<TechNode> NodesOf(TechTab tab)
    {
        string? gate = null;
        for (int ring = 0; ring < tab.Rings.Length; ring++)
        {
            var each = tab.Rings[ring];
            if (each.Gate is { } id)
            {
                yield return new TechNode(id, gate == null ? [] : [gate], each.Order, 1, tab.Id, ring);
                gate = id;
            }
            foreach (var (block, times) in each.Blocks)
                yield return new TechNode(block, gate == null ? [] : [gate], [], times, tab.Id, ring);
        }
    }
}
