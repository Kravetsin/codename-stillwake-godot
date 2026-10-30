namespace Stillwake.Content;

/// <summary>An upgrade bought on the tree (2026-10-30-room-upgrades): <c>Slots</c> more of its <c>Kind</c> for the whole crew, for <c>Credits</c>.</summary>
public sealed record Upgrade(string Id, string Kind, int Slots, int Credits);

/// <summary>What the credits can buy besides blueprints: more room. Each is a node of <see cref="Research.Tabs"/>, granted as a blueprint is.</summary>
public static class Upgrades
{
    public const string Bag = "bag", Belt = "belt", TradeSlots = "tradeSlots";

    /// <summary>The slots of a trade station open from the start; <see cref="MachineSpec.Slots"/> is how many it can come to.</summary>
    public const int TradeStart = 2;

    public static readonly IReadOnlyList<Upgrade> All =
    [
        new("bag1", Bag, 3, 200),
        new("bag2", Bag, 3, 300),
        new("bag3", Bag, 3, 500),
        new("bag4", Bag, 3, 800),
        new("bag5", Bag, 3, 1200),
        new("bag6", Bag, 3, 1800),
        new("bag7", Bag, 3, 2500),
        new("bag8", Bag, 3, 4000),
        new("bag9", Bag, 3, 5000),
        new("bag10", Bag, 3, 8000),
        new("bag11", Bag, 6, 15000),
        new("belt1", Belt, 1, 2000),
        new("belt2", Belt, 1, 3500),
        new("belt3", Belt, 1, 4500),
        new("belt4", Belt, 1, 8000),
        new("tradeSlots1", TradeSlots, 1, 300),
        new("tradeSlots2", TradeSlots, 1, 500),
        new("tradeSlots3", TradeSlots, 1, 1200),
        new("tradeSlots4", TradeSlots, 1, 1800),
        new("tradeSlots5", TradeSlots, 1, 4000),
        new("tradeSlots6", TradeSlots, 1, 5000),
    ];

    private static readonly Dictionary<string, Upgrade> ById = All.ToDictionary(u => u.Id, StringComparer.Ordinal);

    /// <summary>The upgrade named <paramref name="id"/>; null for none.</summary>
    public static Upgrade? Of(string id) => ById.GetValueOrDefault(id);

    /// <summary>The slots of <paramref name="kind"/> that the upgrades <paramref name="granted"/> add.</summary>
    public static int Added(string kind, Func<string, bool> granted)
    {
        int added = 0;
        foreach (var upgrade in All)
            if (upgrade.Kind == kind && granted(upgrade.Id)) added += upgrade.Slots;
        return added;
    }
}
