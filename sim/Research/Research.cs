using System.Text.Json.Serialization;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Knowledge;
using Stillwake.Sim.Power;

namespace Stillwake.Sim.Research;

/// <summary>An analyzer part-way through what its slot holds, in thousandths of a tick at full power.</summary>
public sealed record Analysis(int Analyzer, int Progress);

/// <summary>
/// An analyzer as its panel shows it: what its slot holds, how far it is, out of how much work, and why
/// it stands still if it does: <c>research.unpowered</c>; <c>research.nothingNew</c> once what it
/// holds is analysed; <c>research.nothingToLearn</c> for an item with nothing to learn (<see cref="Analysable"/>).
/// </summary>
public sealed record Analyzing(string Item, int Progress, int Work, string? Problem);

/// <summary>
/// A blueprint or a craft as the world knows it: what it costs, whether it was granted, and the items
/// of its cost that need analysis (<see cref="Analysable"/>) and are not analysed yet, in the cost's order.
/// </summary>
/// <param name="Placement">What a blueprint places; null for a craft, named by the item it makes.</param>
public sealed record Recipe(
    string Id, Placement? Placement, IReadOnlyList<(string Item, int Count)> Cost, bool Granted,
    IReadOnlyList<string> Unanalysed)
{
    /// <summary>Granted, or opened: every item of its cost that needs analysis analysed. Creative mode aside.</summary>
    public bool Known => Granted || Unanalysed.Count == 0;
}

/// <summary>
/// The research slice. Public so modules can be typed by it, its members internal: others go through
/// <see cref="IResearchQueries"/>.
/// </summary>
public sealed class ResearchState
{
    /// <summary>The analyzers at work, by machine id, in id order.</summary>
    [JsonInclude] internal List<Analysis> Analyses { get; set; } = [];
}

/// <summary>What any module may read of the research. Lists are live: a reader never writes them.</summary>
public interface IResearchQueries
{
    /// <summary>Whether <paramref name="placement"/>'s blueprint is known: every one in creative mode.</summary>
    bool Knows(Placement placement);
    /// <summary>Whether the blueprint or craft <paramref name="id"/> is known: every one in creative mode.</summary>
    bool Knows(string id);
    /// <summary>The items analysed, by id, sorted ordinally.</summary>
    IReadOnlyList<string> Analysed { get; }
    /// <summary>Every blueprint, then every craft, as the world knows it now; creative mode aside.</summary>
    IReadOnlyList<Recipe> Recipes();
    /// <summary>Analyzer <paramref name="id"/> as its panel shows it; null for an empty slot.</summary>
    Analyzing? At(int id);
}

/// <summary>
/// Knowing blueprints and crafts (ADRs 0024, 0027): what the world was granted, the items analyzers have
/// analysed, and what that opens (packages/sim/src/modules/research).
/// </summary>
public sealed class ResearchModule : Module<ResearchState>, IResearchQueries
{
    private const int Work = Content.Research.Ticks * 1000;

    /// <summary>Every blueprint, then every craft: its id, what it places, its cost.</summary>
    private static readonly IReadOnlyList<(string Id, Placement? Placement, IReadOnlyList<(string Item, int Count)> Cost)> Every =
    [
        .. Research.Blueprints.All.Select(b => (b.Id, (Placement?)b.Placement, b.Cost)),
        .. Content.Crafts.All.Select(c => (c.Item, (Placement?)null, c.Cost)),
    ];

    private static readonly Dictionary<string, IReadOnlyList<(string Item, int Count)>> CostOf =
        Every.ToDictionary(e => e.Id, e => e.Cost, StringComparer.Ordinal);

    /// <summary>The id of every blueprint, then every craft: what a map with no <c>Layout.Known</c> is granted (<c>Knowledge</c>).</summary>
    public static readonly IReadOnlyList<string> Ids = Every.Select(e => e.Id).ToList();

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IPowerQueries _power;
    private readonly IPowerOps _powerOps;
    private readonly IKnowledgeQueries _knowledge;
    private readonly IKnowledgeOps _knowledgeOps;

    public override string Id => "research";

    public ResearchModule(
        IGridQueries grid, IItemsQueries items, IPowerQueries power, IPowerOps powerOps, IKnowledgeQueries knowledge, IKnowledgeOps knowledgeOps)
    {
        _knowledge = knowledge;
        _knowledgeOps = knowledgeOps;
        _grid = grid;
        _items = items;
        _power = power;
        _powerOps = powerOps;
        // After power's derivation: an analyzer works at its satisfaction of this tick.
        System(Phase.Simulation, Analyse);
    }

    protected override ResearchState Init(Layout layout) => new();

    // Queries.

    public bool Knows(Placement placement) => Knows(Research.Blueprints.Of(placement));

    public bool Knows(string id) =>
        _items.Creative || (CostOf.TryGetValue(id, out var cost) && Known(id, cost));

    public IReadOnlyList<string> Analysed => _knowledge.Analysed;

    public IReadOnlyList<Recipe> Recipes() =>
        Every.Select(e => new Recipe(
            e.Id, e.Placement, e.Cost, Granted(e.Id),
            e.Cost.Select(c => c.Item).Where(item => Analysable.NeedsAnalysis(item) && !IsAnalysed(item)).Distinct().ToList())).ToList();

    public Analyzing? At(int id)
    {
        var item = ItemIn(id);
        if (item == null) return null;
        int progress = ProgressOf(id);
        string? problem = !Analysable.NeedsAnalysis(item) ? "research.nothingToLearn"
            : IsAnalysed(item) ? "research.nothingNew"
            : _power.Satisfaction(id) == 0 ? "research.unpowered"
            : null;
        return new Analyzing(item, progress, Work, problem);
    }

    private bool Granted(string id) => _knowledge.Granted(id);

    private bool IsAnalysed(string item) => _knowledge.IsAnalysed(item);

    /// <summary>The opening rule: granted, or every item of its cost analysed that needs analysis.</summary>
    private bool Known(string id, IReadOnlyList<(string Item, int Count)> cost) =>
        Granted(id) || cost.All(c => !Analysable.NeedsAnalysis(c.Item) || IsAnalysed(c.Item));

    /// <summary>What machine <paramref name="id"/>'s first slot holds; null for none, or no such machine.</summary>
    private string? ItemIn(int id)
    {
        var slots = _items.Crate(id);
        return slots.Count == 0 ? null : slots[0]?.Item;
    }

    /// <summary>Analyzer <paramref name="id"/>'s work so far; none for one not at work.</summary>
    private int ProgressOf(int id)
    {
        var analyses = State.Analyses;
        int lo = 0, hi = analyses.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            if (analyses[mid].Analyzer == id) return analyses[mid].Progress;
            if (analyses[mid].Analyzer < id) lo = mid + 1;
            else hi = mid - 1;
        }
        return 0;
    }

    private void Analyse()
    {
        var kept = new List<Analysis>();
        var analyses = State.Analyses;
        int next = 0;
        foreach (var m in _grid.MachinesOf("analyzer"))
        {
            var item = ItemIn(m.Id);
            bool works = item != null && Analysable.NeedsAnalysis(item) && !IsAnalysed(item);
            // Empty, holding what is in no cost or what is analysed: what was done is dropped, no power drawn.
            _powerOps.Rest(m.Id, !works);
            if (!works) continue;
            // Analyses and analyzers are both in id order: one pass finds each analyzer's.
            while (next < analyses.Count && analyses[next].Analyzer < m.Id) next++;
            int was = next < analyses.Count && analyses[next].Analyzer == m.Id ? analyses[next].Progress : 0;
            int progress = was + _power.Satisfaction(m.Id);
            if (progress < Work)
            {
                kept.Add(new Analysis(m.Id, progress));
                continue;
            }
            // Two analyzers finishing one item on one tick: the second finds it analysed above.
            _knowledgeOps.Learn(item!);
            _powerOps.Rest(m.Id, true);
        }
        State.Analyses = kept;
    }
}
