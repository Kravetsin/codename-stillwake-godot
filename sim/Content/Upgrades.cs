namespace Stillwake.Content;

/// <summary>An upgrade bought on the tree (2026-10-30-room-upgrades): one more slot of its <c>Kind</c> for the whole crew, for <c>Credits</c>.</summary>
public sealed record Upgrade(string Id, string Kind, int Credits);

/// <summary>What the credits can buy besides blueprints: more room. Each is a node of <see cref="Research.Tabs"/>, granted as a blueprint is.</summary>
public static class Upgrades
{
    public const string Bag = "bag", Belt = "belt", TradeSlots = "tradeSlots";

    /// <summary>The slots of a trade station open from the start; <see cref="MachineSpec.Slots"/> is how many it can come to.</summary>
    public const int TradeStart = 2;

    public static readonly IReadOnlyList<Upgrade> All =
    [
        new("bag1", Bag, 200),
        new("bag2", Bag, 300),
        new("bag3", Bag, 500),
        new("bag4", Bag, 800),
        new("bag5", Bag, 1200),
        new("bag6", Bag, 1800),
        new("bag7", Bag, 2500),
        new("bag8", Bag, 4000),
        new("bag9", Bag, 5000),
        new("bag10", Bag, 8000),
        new("bag11", Bag, 15000),
        new("belt1", Belt, 2000),
        new("belt2", Belt, 8000),
        new("tradeSlots1", TradeSlots, 300),
        new("tradeSlots2", TradeSlots, 500),
        new("tradeSlots3", TradeSlots, 1200),
        new("tradeSlots4", TradeSlots, 1800),
        new("tradeSlots5", TradeSlots, 4000),
        new("tradeSlots6", TradeSlots, 5000),
    ];

    private static readonly Dictionary<string, Upgrade> ById = All.ToDictionary(u => u.Id, StringComparer.Ordinal);

    /// <summary>The upgrade named <paramref name="id"/>; null for none.</summary>
    public static Upgrade? Of(string id) => ById.GetValueOrDefault(id);

    /// <summary>The slots of <paramref name="kind"/> that the upgrades <paramref name="granted"/> add.</summary>
    public static int Added(string kind, Func<string, bool> granted)
    {
        int added = 0;
        foreach (var upgrade in All)
            if (upgrade.Kind == kind && granted(upgrade.Id)) added++;
        return added;
    }
}
