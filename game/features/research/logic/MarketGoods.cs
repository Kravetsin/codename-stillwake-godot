using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Features.Research;

/// <summary>Which tab of the market a good sits under.</summary>
public enum MarketShelf
{
    Upgrades,
    Blueprints,
}

/// <summary>What a good's tile says, from what the sim says of its steps.</summary>
public enum MarketTile
{
    /// <summary>Its next step can be bought.</summary>
    Affordable,

    /// <summary>Its next step is open, and the credits are short.</summary>
    Dear,

    /// <summary>Every step opened is bought, and the next waits for the mafia's reputation: its price shows as garbage.</summary>
    Locked,

    /// <summary>Every step is bought: a blueprint owned, or a kind of upgrade bought out.</summary>
    Owned,
}

/// <summary>What deciphering does to a tile: nothing, or its price and count, for every good is shown from the start (2026-11-08-mafia-reputation).</summary>
public enum MarketFresh
{
    None,
    More,
}

/// <summary>
/// A good on the market, one tile ([market-grid](../../../../docs/decisions/2026-11-03-market-grid.md)): a kind of upgrade, its blocks as the steps of one ladder, or a
/// block of its own. Its id is the upgrade's kind or the block's id.
/// </summary>
public sealed record MarketGood(string Id, MarketShelf Shelf, IReadOnlyList<TechNode> Steps);

/// <summary>A good as the sim has it now: what its tile says, the step its button buys, how many of its opened steps are bought.</summary>
public readonly record struct MarketState(MarketTile Tile, TechNode? Next, int Bought, int Opened);

/// <summary>The market's goods, worked out from <c>Content.Research</c>, and what each one's tile says.</summary>
public static class MarketGoods
{
    /// <summary>
    /// The goods of <paramref name="tab"/>, each kind of upgrade once with its blocks in the order the content lists them, which is the order their prices
    /// rise in (<c>MarketGoodsTests</c>), then the other blocks.
    /// </summary>
    public static IReadOnlyList<MarketGood> Of(TechTab tab)
    {
        var steps = new Dictionary<string, List<TechNode>>(StringComparer.Ordinal);
        var kinds = new List<string>();
        var others = new List<MarketGood>();
        foreach (var ring in tab.Rings)
        {
            foreach (var (id, _) in ring.Blocks)
            {
                var node = Content.Research.Node(id)!;
                if (Upgrades.Of(id) is not { } upgrade)
                {
                    others.Add(new MarketGood(id, MarketShelf.Blueprints, [node]));
                    continue;
                }
                if (!steps.TryGetValue(upgrade.Kind, out var ladder))
                {
                    steps[upgrade.Kind] = ladder = [];
                    kinds.Add(upgrade.Kind);
                }
                ladder.Add(node);
            }
        }
        return [.. kinds.Select(kind => new MarketGood(kind, MarketShelf.Upgrades, steps[kind])), .. others];
    }

    /// <summary>What <paramref name="good"/>'s tile says, from whether each step is bought, reached, and why it cannot be bought.</summary>
    public static MarketState StateOf(MarketGood good, Func<TechNode, bool> done, Func<TechNode, bool> reached, Func<TechNode, string?> problem)
    {
        int bought = 0, opened = 0;
        TechNode? next = null;
        foreach (var step in good.Steps)
        {
            if (!reached(step)) continue;
            opened++;
            if (done(step)) bought++;
            else next ??= step;
        }
        if (next != null) return new MarketState(problem(next) == null ? MarketTile.Affordable : MarketTile.Dear, next, bought, opened);
        var locked = good.Steps.FirstOrDefault(s => !reached(s));
        return new MarketState(locked != null ? MarketTile.Locked : MarketTile.Owned, locked, bought, opened);
    }

    /// <summary>
    /// The goods under <paramref name="shelf"/> (all with null), every one shown (2026-11-08-mafia-reputation): upgrades first, then blueprints open,
    /// newest first, then those the mafia does not sell yet, nearest first; the cheaper first among those opened together.
    /// </summary>
    public static IReadOnlyList<MarketGood> Sorted(IEnumerable<MarketGood> goods, Func<TechNode, bool> reached, MarketShelf? shelf) =>
        [.. goods.Where(g => shelf == null || g.Shelf == shelf)
            .OrderBy(g => g.Shelf)
            .ThenBy(g => g.Shelf == MarketShelf.Upgrades ? 0 : reached(g.Steps[0]) ? -g.Steps[0].Ring : 100 + g.Steps[0].Ring)
            .ThenBy(g => g.Shelf == MarketShelf.Upgrades ? 0 : Prices.Of(g.Steps[0]))];

    /// <summary>
    /// What deciphering does to <paramref name="good"/> while the rows <paramref name="playing"/> says decipher: its price and count, if a step opened
    /// is in them.
    /// </summary>
    public static MarketFresh FreshOf(MarketGood good, Func<TechNode, bool> reached, Func<int, bool> playing)
    {
        foreach (var step in good.Steps)
            if (reached(step) && playing(step.Ring)) return MarketFresh.More;
        return MarketFresh.None;
    }
}
