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

/// <summary>Puts what the sender carries of what gate <paramref name="Gate"/> asks for into it, at tech station <paramref name="Machine"/>.</summary>
public sealed record Deposit(int Machine, string Gate) : Command;

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
    /// <summary>Whether <paramref name="node"/> is done: a block bought, all it opens known; a gate's deposit full; every one in creative mode.</summary>
    bool Done(TechNode node);
    /// <summary>Whether all <paramref name="node"/> needs are done.</summary>
    bool Reached(TechNode node);
    /// <summary>How much of <paramref name="item"/> gate <paramref name="gate"/> holds.</summary>
    int Deposited(TechNode gate, string item);
    /// <summary>Why block <paramref name="block"/> cannot be bought now, leaving the machine aside; null when it can.</summary>
    string? BuyProblem(TechNode block);
}

/// <summary>
/// Knowing blueprints and crafts (2026-10-30-trade-and-tech): what the world knows, the tree it buys more of at tech stations, and
/// its gates' deposits. The state is the world's <c>Knowledge</c>; research holds the rules.
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

    public bool Done(TechNode node)
    {
        if (_items.Creative) return true;
        if (!node.Gate) return node.Opens.All(_knowledge.Granted);
        foreach (var (item, count) in node.Deposit)
            if (_knowledge.DepositOf(node.Id, item) < count) return false;
        return true;
    }

    public bool Reached(TechNode node)
    {
        foreach (string need in node.Needs)
            if (!Done(Content.Research.Node(need)!)) return false;
        return true;
    }

    public int Deposited(TechNode gate, string item) => _knowledge.DepositOf(gate.Id, item);

    public string? BuyProblem(TechNode block)
    {
        if (Done(block)) return "research.bought";
        if (block.Gate || !Reached(block)) return "research.locked";
        return Prices.Of(block) > _knowledge.Credits ? "research.noCredits" : null;
    }

    // Commands.

    private bool IsTechStation(int machine) => _grid.MachineById(machine) is { } m && Machines.Is(m.Kind, MachineTrait.Tech);

    private string? BuyProblem(BuyBlock c)
    {
        if (!IsTechStation(c.Machine)) return "research.noMachine";
        return Content.Research.Node(c.Block) is { } block ? BuyProblem(block) : "research.locked";
    }

    private void Apply(BuyBlock c)
    {
        var block = Content.Research.Node(c.Block)!;
        _knowledgeOps.Spend(Prices.Of(block));
        foreach (string id in block.Opens) _knowledgeOps.Grant(id);
    }

    private string? DepositProblem(Deposit c)
    {
        if (!IsTechStation(c.Machine)) return "research.noMachine";
        if (Content.Research.Node(c.Gate) is not { Gate: true } gate) return "research.locked";
        if (Done(gate)) return "research.gateOpen";
        if (!Reached(gate)) return "research.locked";
        return Taken(c.From, gate).Count == 0 ? "research.nothingToDeposit" : null;
    }

    private void Apply(Deposit c)
    {
        foreach (var (item, count) in _itemsOps.Take(c.From, Taken(c.From, Content.Research.Node(c.Gate)!))) _knowledgeOps.AddDeposit(c.Gate, item, count);
    }

    /// <summary>What <paramref name="peer"/> would put into <paramref name="gate"/> now: of each item, what it lacks or what is carried, the less.</summary>
    private List<(string Item, int Count)> Taken(int peer, TechNode gate)
    {
        var taken = new List<(string Item, int Count)>();
        var totals = _items.Totals(peer);
        foreach (var (item, count) in gate.Deposit)
        {
            int carried = 0;
            foreach (var t in totals)
                if (t.Item == item) carried += t.Count;
            int put = Math.Min(count - _knowledge.DepositOf(gate.Id, item), carried);
            if (put > 0) taken.Add((item, put));
        }
        return taken;
    }

    /// <summary>Buys and deposits by hand, refused ones too: at a tech station when one stands, at any node.</summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(8) != 0) return null;
        var stations = _grid.MachinesWith(MachineTrait.Tech);
        int machine = stations.Count > 0 && rng.Below(8) != 0 ? stations[(int)rng.Below((uint)stations.Count)].Id : (int)rng.Below(64);
        var nodes = Content.Research.Nodes;
        var node = nodes[(int)rng.Below((uint)nodes.Count)];
        return node.Gate ? new Deposit(machine, node.Id) { From = from } : new BuyBlock(machine, node.Id) { From = from };
    }
}
