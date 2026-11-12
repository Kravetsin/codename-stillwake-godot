using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Knowledge;

namespace Stillwake.Sim.Research;

/// <summary>A blueprint or a craft as the world knows it: what it costs, and whether it is known.</summary>
/// <param name="Placement">What a blueprint places; null for a craft, named by the item it makes.</param>
public sealed record Recipe(string Id, Placement? Placement, IReadOnlyList<(string Item, int Count)> Cost, bool Known);

/// <summary>Buys the tree's block <paramref name="Block"/>, with the ship's credits, from anywhere.</summary>
public sealed record BuyBlock(string Block) : Command;

/// <summary>A good the next level wants: how many are sold since the level opened, and how many it asks.</summary>
public sealed record Want(string Item, int Sold, int Count);

/// <summary>
/// Where the mafia's reputation stands (2026-11-08-mafia-reputation): the gate of the next level, null past the last; what it wants; its points and
/// target, in sevenths of a credit's worth sold.
/// </summary>
public sealed record Standing(TechNode? Next, IReadOnlyList<Want> Wanted, long Points, long Target);


/// <summary>What the rule grants by: a thing, its parts, and for a craft the machines that make it; null for a blueprint.</summary>
internal sealed record Walkable(string Id, string[] Parts, string[]? Stations);

/// <summary>
/// The research slice: empty, as what is known, earned and sold is the world's <c>Knowledge</c>. Public so modules can be
/// typed by it.
/// </summary>
public sealed class ResearchState;

/// <summary>What any module may read of the research.</summary>
public interface IResearchQueries
{
    /// <summary>Whether the world knows <paramref name="placement"/>'s blueprint, whoever is in creative mode.</summary>
    bool Knows(Placement placement);
    /// <summary>Whether the world knows the blueprint or craft <paramref name="id"/>, whoever is in creative mode.</summary>
    bool Knows(string id);
    /// <summary>Whether <paramref name="peer"/> may use <paramref name="placement"/>'s blueprint: known, or every one in its creative mode.</summary>
    bool Knows(int peer, Placement placement);
    /// <summary>Whether <paramref name="peer"/> may use the blueprint or craft <paramref name="id"/>: known, or every one in its creative mode.</summary>
    bool Knows(int peer, string id);
    /// <summary>Every blueprint, then every craft, as the world knows it now; creative mode aside.</summary>
    IReadOnlyList<Recipe> Recipes();
    /// <summary>The ship's credits.</summary>
    int Credits { get; }
    /// <summary>
    /// Whether <paramref name="node"/> is done: a block bought, all it opens known or on its way (2026-11-08-mafia-deliveries); a gate its level
    /// reached. Never gear or the confiscated container, sold again and again.
    /// </summary>
    bool Done(TechNode node);
    /// <summary>Whether <paramref name="node"/> is done as <paramref name="peer"/> sees it: every one in its creative mode.</summary>
    bool Done(int peer, TechNode node);
    /// <summary>Whether all <paramref name="node"/> needs are done.</summary>
    bool Reached(TechNode node);
    /// <summary>Where the mafia's reputation stands: the next level, what it wants and how far it is.</summary>
    Standing Standing();

    /// <summary>Why block <paramref name="block"/> cannot be bought now, leaving the machine aside; null when it can.</summary>
    string? BuyProblem(TechNode block);
}

/// <summary>
/// Knowing blueprints and crafts (docs/decisions/2026-11-05-the-board.md): what the world knows, what the rule grants it, and the market's rings it
/// buys more from. The state is the world's <c>Knowledge</c>; research holds the rules.
/// </summary>
public sealed class ResearchModule : Module<ResearchState>, IResearchQueries
{
    /// <summary>Every blueprint, then every craft: its id, what it places, its cost.</summary>
    private static readonly IReadOnlyList<(string Id, Placement? Placement, IReadOnlyList<(string Item, int Count)> Cost)> Every =
    [
        .. Research.Blueprints.All.Select(b => (b.Id, (Placement?)b.Placement, b.Cost)),
        .. Content.Crafts.All.Select(c => (c.Item, (Placement?)null, c.Cost)),
    ];

    /// <summary>The id of every blueprint, then every craft, then every upgrade: what a map with no <c>Layout.Known</c> is granted (<c>Knowledge</c>).</summary>
    public static readonly IReadOnlyList<string> Ids = [.. Every.Select(e => e.Id), .. Content.Upgrades.All.Select(u => u.Id)];

    /// <summary>What the rule walks: every blueprint and craft in <see cref="Every"/>'s order, but what only the market opens. Upgrades are no recipes.</summary>
    internal static readonly Walkable[] Walked =
    [
        .. Every.Where(e => Content.Research.Node(e.Id) is not { Tab: Content.Research.Market }).Select(e => new Walkable(
            e.Id,
            [.. e.Cost.Select(c => c.Item)],
            e.Placement != null ? null : [.. Content.Stations.All.Where(s => s.Rows.Any(r => r.Contains(e.Id))).Select(s => s.Machine)])),
    ];

    /// <summary>The market's gates in ring order: level <c>n</c>'s is the <c>n - 1</c>th.</summary>
    private static readonly TechNode[] Gates = [.. Content.Research.Nodes.Where(n => n is { Gate: true, Tab: Content.Research.Market }).OrderBy(n => n.Ring)];

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IKnowledgeQueries _knowledge;
    private readonly IKnowledgeOps _knowledgeOps;
    private int _knowledgeSeen = -1, _gridSeen = -1;

    public override string Id => "research";

    public ResearchModule(IGridQueries grid, IItemsQueries items, IKnowledgeQueries knowledge, IKnowledgeOps knowledgeOps)
    {
        _grid = grid;
        _items = items;
        _knowledge = knowledge;
        _knowledgeOps = knowledgeOps;
        Handles<BuyBlock>(c => BuyProblem(c), Apply);
        System(Phase.Derivation, Walk);
    }

    protected override ResearchState Init(Layout layout) => new();

    public override void Rebuild()
    {
        _knowledgeSeen = -1;
        _gridSeen = -1;
    }

    /// <summary>
    /// Raises the mafia's level when its target is met, every tick, then grants what the rule allows (docs/decisions/2026-11-05-the-board.md), when a
    /// count or the grid has moved. One pass is enough: the rule reads
    /// counts and the grid, never what is granted, and a loaded world walks on its first tick, so it matches one that kept running.
    /// </summary>
    internal void Walk()
    {
        Rise();
        if (_knowledge.Version == _knowledgeSeen && _grid.Version == _gridSeen) return;
        for (int i = 0; i < Walked.Length; i++)
            if (!_knowledge.Granted(Walked[i].Id) && Understood(Walked[i])) _knowledgeOps.Grant(Walked[i].Id);
        _knowledgeSeen = _knowledge.Version;
        _gridSeen = _grid.Version;
    }

    /// <summary>Opens the next level once its points reach its target, counting the one after from what is sold now: one level a tick at most.</summary>
    private void Rise()
    {
        if (Next() is not { } next || Points(next) < Target(next)) return;
        var after = GateOf(next.Ring + 1);
        var start = after == null ? [] : after.Order.Select(d => new Tally(d.Item, _knowledge.SoldOf(d.Item))).ToList();
        _knowledgeOps.Open(next.Ring, start, _knowledge.SoldWorth);
    }

    private static TechNode? GateOf(int ring) => ring >= 1 && ring <= Gates.Length ? Gates[ring - 1] : null;

    private TechNode? Next() => GateOf(_knowledge.Level + 1);

    /// <summary>What <paramref name="gate"/>'s level asks, in sevenths: everything it wants at full worth.</summary>
    private static long Target(TechNode gate)
    {
        long target = 0;
        foreach (var (item, count) in gate.Order) target += (long)count * Market.WorthOf(item) * Content.Research.OffWanted;
        return target;
    }

    /// <summary>
    /// How far toward <paramref name="gate"/>'s level, in sevenths: a wanted good up to its count at full worth, past it at a seventh, and everything
    /// else sold since the level opened at a seventh, never below none, as a later build may lower a worth.
    /// </summary>
    private long Points(TechNode gate)
    {
        long points = 0, wanted = 0;
        foreach (var (item, count) in gate.Order)
        {
            long sold = _knowledge.SoldOf(item) - _knowledge.BaseOf(item), worth = Market.WorthOf(item);
            points += Math.Min(sold, count) * worth * Content.Research.OffWanted + Math.Max(sold - count, 0) * worth;
            wanted += sold * worth;
        }
        return points + Math.Max(0, _knowledge.SoldWorth - _knowledge.BaseWorth - wanted);
    }

    /// <summary>Whether each part of <paramref name="thing"/> was ever obtained and, for a craft, a machine that makes it stands on this surface.</summary>
    private bool Understood(Walkable thing)
    {
        for (int i = 0; i < thing.Parts.Length; i++)
            if (_knowledge.ObtainedOf(thing.Parts[i]) == 0) return false;
        if (thing.Stations is not { } stations) return true;
        for (int i = 0; i < stations.Length; i++)
            if (_grid.MachinesOf(stations[i]).Count > 0) return true;
        return false;
    }

    // Queries.

    public bool Knows(Placement placement) => Knows(Research.Blueprints.Of(placement));

    public bool Knows(string id) => _knowledge.Granted(id);

    public bool Knows(int peer, Placement placement) => _items.Creative(peer) || Knows(placement);

    public bool Knows(int peer, string id) => _items.Creative(peer) || Knows(id);

    public IReadOnlyList<Recipe> Recipes() => Every.Select(e => new Recipe(e.Id, e.Placement, e.Cost, _knowledge.Granted(e.Id))).ToList();

    public int Credits => _knowledge.Credits;

    public bool Done(int peer, TechNode node) => _items.Creative(peer) || Done(node);

    public bool Done(TechNode node) =>
        node.Gate ? node.Ring <= _knowledge.Level : !Repeats(node.Id) && (_knowledge.Granted(node.Id) || _knowledge.InFlight(node.Id));

    /// <summary>Whether <paramref name="good"/> is sold again and again: gear, one to a hero, and the confiscated container.</summary>
    public static bool Repeats(string good) => good == Content.Research.Confiscated || Content.Research.GearSold.Contains(good, StringComparer.Ordinal);

    public bool Reached(TechNode node)
    {
        foreach (string need in node.Needs)
            if (!Done(Content.Research.Node(need)!)) return false;
        return true;
    }

    public Standing Standing()
    {
        if (Next() is not { } next) return new Standing(null, [], 0, 0);
        var wanted = next.Order.Select(d => new Want(d.Item, _knowledge.SoldOf(d.Item) - _knowledge.BaseOf(d.Item), d.Count)).ToList();
        return new Standing(next, wanted, Points(next), Target(next));
    }

    public string? BuyProblem(TechNode block)
    {
        if (Done(block)) return "research.bought";
        if (block.Gate) return "research.locked";
        if (!Reached(block)) return "research.reputation";
        return Prices.Of(block) > _knowledge.Credits ? "research.noCredits" : null;
    }

    // Commands.

    private string? BuyProblem(BuyBlock c) => Content.Research.Node(c.Block) is { } block ? BuyProblem(block) : "research.locked";

    private void Apply(BuyBlock c)
    {
        var block = Content.Research.Node(c.Block)!;
        _knowledgeOps.Spend(Prices.Of(block));
        // Granted only when its piece is caught or broken: it comes as debris along a trade station's row (2026-11-08-mafia-deliveries).
        _knowledgeOps.Deliver(block.Id);
    }

    /// <summary>Buys by hand, refused ones too, at any node.</summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(8) != 0) return null;
        var nodes = Content.Research.Nodes;
        return new BuyBlock(nodes[(int)rng.Below((uint)nodes.Count)].Id) { From = from };
    }
}
