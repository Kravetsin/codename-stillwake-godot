using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Knowledge;

namespace Stillwake.Sim.Research;

/// <summary>A blueprint or a craft as the world knows it: what it costs, and whether it is known.</summary>
/// <param name="Placement">What a blueprint places; null for a craft, named by the item it makes.</param>
public sealed record Recipe(string Id, Placement? Placement, IReadOnlyList<(string Item, int Count)> Cost, bool Known);

/// <summary>Buys the tree's block <paramref name="Block"/> at tech station <paramref name="Machine"/>, with the ship's credits.</summary>
public sealed record BuyBlock(int Machine, string Block) : Command;

/// <summary>Puts what the sender carries of tier <paramref name="Tier"/>'s deposit into it, at tech station <paramref name="Machine"/>.</summary>
public sealed record Deposit(int Machine, string Tier) : Command;

/// <summary>
/// The research slice: empty, as what is known, earned and deposited is the world's <c>Knowledge</c>. Public so modules can be
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
    /// <summary>Whether <paramref name="block"/> is bought: all it opens known.</summary>
    bool Bought(TechBlock block);
    /// <summary>Whether <paramref name="tier"/> is open: it and every tier before it have their deposits full; every one in creative mode.</summary>
    bool Open(TechTier tier);
    /// <summary>How much of <paramref name="item"/> <paramref name="tier"/>'s deposit holds.</summary>
    int Deposited(TechTier tier, string item);
    /// <summary>Why <paramref name="block"/> cannot be bought now, leaving the machine aside; null when it can.</summary>
    string? BuyProblem(TechBlock block);
}

/// <summary>
/// Knowing blueprints and crafts (2026-10-30-trade-and-tech): what the world knows, the tree it buys more of at tech stations, and
/// the tiers' deposits. The state is the world's <c>Knowledge</c>; research holds the rules.
/// </summary>
public sealed class ResearchModule : Module<ResearchState>, IResearchQueries
{
    /// <summary>Every blueprint, then every craft: its id, what it places, its cost.</summary>
    private static readonly IReadOnlyList<(string Id, Placement? Placement, IReadOnlyList<(string Item, int Count)> Cost)> Every =
    [
        .. Research.Blueprints.All.Select(b => (b.Id, (Placement?)b.Placement, b.Cost)),
        .. Content.Crafts.All.Select(c => (c.Item, (Placement?)null, c.Cost)),
    ];

    /// <summary>The id of every blueprint, then every craft: what a map with no <c>Layout.Known</c> is granted (<c>Knowledge</c>).</summary>
    public static readonly IReadOnlyList<string> Ids = Every.Select(e => e.Id).ToList();

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IKnowledgeQueries _knowledge;
    private readonly IKnowledgeOps _knowledgeOps;

    public override string Id => "research";

    public ResearchModule(IGridQueries grid, IItemsQueries items, IItemsOps itemsOps, IKnowledgeQueries knowledge, IKnowledgeOps knowledgeOps)
    {
        _grid = grid;
        _items = items;
        _itemsOps = itemsOps;
        _knowledge = knowledge;
        _knowledgeOps = knowledgeOps;
        Handles<BuyBlock>(c => BuyProblem(c), Apply);
        Handles<Deposit>(c => DepositProblem(c), Apply);
    }

    protected override ResearchState Init(Layout layout) => new();

    // Queries.

    public bool Knows(Placement placement) => Knows(Research.Blueprints.Of(placement));

    public bool Knows(string id) => _items.Creative || _knowledge.Granted(id);

    public IReadOnlyList<Recipe> Recipes() => Every.Select(e => new Recipe(e.Id, e.Placement, e.Cost, _knowledge.Granted(e.Id))).ToList();

    public int Credits => _knowledge.Credits;

    public bool Bought(TechBlock block) => block.Opens.All(Knows);

    public bool Open(TechTier tier)
    {
        if (_items.Creative) return true;
        foreach (var t in Content.Research.Tiers)
        {
            foreach (var (item, count) in t.Deposit)
                if (_knowledge.DepositOf(t.Id, item) < count) return false;
            if (t == tier) return true;
        }
        throw new InvalidOperationException($"A tier not in the tree: {tier.Id}.");
    }

    public int Deposited(TechTier tier, string item) => _knowledge.DepositOf(tier.Id, item);

    public string? BuyProblem(TechBlock block)
    {
        if (Bought(block)) return "research.bought";
        if (Content.Research.Block(block.Id) is not { } found || !Open(found.Tier) || (found.Before is { } before && !Bought(before)))
            return "research.locked";
        return block.Price > _knowledge.Credits ? "research.noCredits" : null;
    }

    // Commands.

    private bool IsTechStation(int machine) => _grid.MachineById(machine) is { } m && Machines.Is(m.Kind, MachineTrait.Tech);

    private string? BuyProblem(BuyBlock c)
    {
        if (!IsTechStation(c.Machine)) return "research.noMachine";
        return Content.Research.Block(c.Block) is { } found ? BuyProblem(found.Block) : "research.locked";
    }

    private void Apply(BuyBlock c)
    {
        var block = Content.Research.Block(c.Block)!.Value.Block;
        _knowledgeOps.Spend(block.Price);
        foreach (string id in block.Opens) _knowledgeOps.Grant(id);
    }

    private string? DepositProblem(Deposit c)
    {
        if (!IsTechStation(c.Machine)) return "research.noMachine";
        if (!Content.Research.TryTier(c.Tier, out var tier)) return "research.locked";
        if (Open(tier)) return "research.tierOpen";
        var tiers = Content.Research.Tiers;
        for (int i = 1; i < tiers.Count; i++)
            if (tiers[i] == tier && !Open(tiers[i - 1])) return "research.locked";
        return Taken(c.From, tier).Count == 0 ? "research.nothingToDeposit" : null;
    }

    private void Apply(Deposit c)
    {
        Content.Research.TryTier(c.Tier, out var tier);
        foreach (var (item, count) in _itemsOps.Take(c.From, Taken(c.From, tier!))) _knowledgeOps.AddDeposit(c.Tier, item, count);
    }

    /// <summary>What <paramref name="peer"/> would put into <paramref name="tier"/>'s deposit now: of each item, what it lacks or what is carried, the less.</summary>
    private List<(string Item, int Count)> Taken(int peer, TechTier tier)
    {
        var taken = new List<(string Item, int Count)>();
        var totals = _items.Totals(peer);
        foreach (var (item, count) in tier.Deposit)
        {
            int carried = 0;
            foreach (var t in totals)
                if (t.Item == item) carried += t.Count;
            int put = Math.Min(count - _knowledge.DepositOf(tier.Id, item), carried);
            if (put > 0) taken.Add((item, put));
        }
        return taken;
    }

    /// <summary>Buys and deposits by hand, refused ones too: at a tech station when one stands, at any block and tier.</summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(8) != 0) return null;
        var stations = _grid.MachinesWith(MachineTrait.Tech);
        int machine = stations.Count > 0 && rng.Below(8) != 0 ? stations[(int)rng.Below((uint)stations.Count)].Id : (int)rng.Below(64);
        var tiers = Content.Research.Tiers;
        var tier = tiers[(int)rng.Below((uint)tiers.Count)];
        if (rng.Below(2) == 0) return new Deposit(machine, tier.Id) { From = from };
        var branch = tier.Branches[(int)rng.Below((uint)tier.Branches.Length)];
        return new BuyBlock(machine, branch.Blocks[(int)rng.Below((uint)branch.Blocks.Length)].Id) { From = from };
    }
}
