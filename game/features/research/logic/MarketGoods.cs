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

    /// <summary>Every step opened is bought, and a later order opens more.</summary>
    Waiting,

    /// <summary>Every step is bought: a blueprint owned, or a kind of upgrade bought out.</summary>
    Owned,
}

/// <summary>What deciphering does to a tile: nothing, all of it, or only its count, for it was there before with fewer steps.</summary>
public enum MarketFresh
{
    None,
    New,
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
        return new MarketState(opened < good.Steps.Count ? MarketTile.Waiting : MarketTile.Owned, null, bought, opened);
    }

    /// <summary>Whether any step of <paramref name="good"/> is open: what is not shows only as garbage under its order.</summary>
    public static bool Shown(MarketGood good, Func<TechNode, bool> reached) => good.Steps.Any(reached);

    /// <summary>
    /// The goods shown under <paramref name="shelf"/> (all with null), upgrades first, then blueprints newest first and the cheaper first among those opened
    /// together.
    /// </summary>
    public static IReadOnlyList<MarketGood> Sorted(IEnumerable<MarketGood> goods, Func<TechNode, bool> reached, MarketShelf? shelf) =>
        [.. goods.Where(g => Shown(g, reached) && (shelf == null || g.Shelf == shelf))
            .OrderBy(g => g.Shelf)
            .ThenByDescending(g => g.Shelf == MarketShelf.Upgrades ? 0 : g.Steps[0].Ring)
            .ThenBy(g => g.Shelf == MarketShelf.Upgrades ? 0 : Prices.Of(g.Steps[0]))];

    /// <summary>
    /// What deciphering does to <paramref name="good"/> while the rows <paramref name="playing"/> says decipher: new if every step opened is in them,
    /// more if some were open before.
    /// </summary>
    public static MarketFresh FreshOf(MarketGood good, Func<TechNode, bool> reached, Func<int, bool> playing)
    {
        bool any = false, before = false;
        foreach (var step in good.Steps)
        {
            if (!reached(step)) continue;
            if (playing(step.Ring)) any = true;
            else before = true;
        }
        return !any ? MarketFresh.None : before ? MarketFresh.More : MarketFresh.New;
    }

    /// <summary>The goods row <paramref name="row"/> opens, each kind of upgrade once: what its order shows as garbage.</summary>
    public static IReadOnlyList<MarketGood> Unlocks(IEnumerable<MarketGood> goods, int row) => [.. goods.Where(g => g.Steps.Any(s => s.Ring == row))];
}
