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


/// <summary>What the rule grants by: a thing, how many of each part are enough, and for a craft the machines that make it; null for a blueprint.</summary>
internal sealed record Walkable(string Id, (string Item, int Enough)[] Parts, string[]? Stations);

/// <summary>
/// The research slice: empty, as what is known, earned and sold is the world's <c>Knowledge</c>. Public so modules can be
/// typed by it.
/// </summary>
public sealed class ResearchState;

/// <summary>What any module may read of the research.</summary>
public interface IResearchQueries
{
    /// <summary>Whether <paramref name="placement"/>'s blueprint is known: every one in creative mode.</summary>
    bool Knows(Placement placement);
    /// <summary>Whether the blueprint or craft <paramref name="id"/> is known: every one in creative mode.</summary>
    bool Knows(string id);
    /// <summary>Every blueprint, then every craft, as the world knows it now; creative mode aside.</summary>
    IReadOnlyList<Recipe> Recipes();
    /// <summary>The ship's credits.</summary>
    int Credits { get; }
    /// <summary>Whether <paramref name="node"/> is done: a block bought, all it opens known; a gate's order sold and its ring reached; every one in creative mode.</summary>
    bool Done(TechNode node);
    /// <summary>Whether all <paramref name="node"/> needs are done.</summary>
    bool Reached(TechNode node);

    /// <summary>Why block <paramref name="block"/> cannot be bought now, leaving the machine aside; null when it can.</summary>
    string? BuyProblem(TechNode block);
}

/// <summary>
/// Knowing blueprints and crafts (docs/plans/board.md): what the world knows, what the rule grants it, and the market's rings it
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
            [.. e.Cost.Select(c => (c.Item, Understanding.Enough(c.Item)))],
            e.Placement != null ? null : [.. Content.Stations.All.Where(s => s.Rows.Any(r => r.Contains(e.Id))).Select(s => s.Machine)])),
    ];

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
    /// Grants what the rule allows (docs/plans/board.md), when a count or the grid has moved. One pass is enough: the rule reads
    /// counts and the grid, never what is granted, and a loaded world walks on its first tick, so it matches one that kept running.
    /// </summary>
    internal void Walk()
    {
        if (_knowledge.Version == _knowledgeSeen && _grid.Version == _gridSeen) return;
        for (int i = 0; i < Walked.Length; i++)
            if (!_knowledge.Granted(Walked[i].Id) && Understood(Walked[i])) _knowledgeOps.Grant(Walked[i].Id);
        _knowledgeSeen = _knowledge.Version;
        _gridSeen = _grid.Version;
    }

    /// <summary>Whether each part of <paramref name="thing"/> is enough and, for a craft, a machine that makes it stands on this surface.</summary>
    private bool Understood(Walkable thing)
    {
        for (int i = 0; i < thing.Parts.Length; i++)
            if (_knowledge.ObtainedOf(thing.Parts[i].Item) < thing.Parts[i].Enough) return false;
        if (thing.Stations is not { } stations) return true;
        for (int i = 0; i < stations.Length; i++)
            if (_grid.MachinesOf(stations[i]).Count > 0) return true;
        return false;
    }

    // Queries.

    public bool Knows(Placement placement) => Knows(Research.Blueprints.Of(placement));

    public bool Knows(string id) => _items.Creative || _knowledge.Granted(id);

    public IReadOnlyList<Recipe> Recipes() => Every.Select(e => new Recipe(e.Id, e.Placement, e.Cost, _knowledge.Granted(e.Id))).ToList();

    public int Credits => _knowledge.Credits;

    public bool Done(TechNode node)
    {
        if (_items.Creative) return true;
        if (!node.Gate) return _knowledge.Granted(node.Id);
        // Sales count from the world's start, so without its ring reached an order met early would open a ring past a shut one.
        if (!Reached(node)) return false;
        foreach (var (item, count) in node.Order)
            if (_knowledge.SoldOf(item) < count) return false;
        return true;
    }

    public bool Reached(TechNode node)
    {
        foreach (string need in node.Needs)
            if (!Done(Content.Research.Node(need)!)) return false;
        return true;
    }

    public string? BuyProblem(TechNode block)
    {
        if (Done(block)) return "research.bought";
        if (block.Gate || !Reached(block)) return "research.locked";
        return Prices.Of(block) > _knowledge.Credits ? "research.noCredits" : null;
    }

    // Commands.

    private string? BuyProblem(BuyBlock c) => Content.Research.Node(c.Block) is { } block ? BuyProblem(block) : "research.locked";

    private void Apply(BuyBlock c)
    {
        var block = Content.Research.Node(c.Block)!;
        _knowledgeOps.Spend(Prices.Of(block));
        _knowledgeOps.Grant(block.Id);
    }

    /// <summary>Buys by hand, refused ones too, at any node.</summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(8) != 0) return null;
        var nodes = Content.Research.Nodes;
        return new BuyBlock(nodes[(int)rng.Below((uint)nodes.Count)].Id) { From = from };
    }
}
