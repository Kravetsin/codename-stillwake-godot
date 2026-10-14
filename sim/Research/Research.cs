using System.Text.Json.Serialization;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;

namespace Stillwake.Sim.Research;

/// <summary>An analyzer part-way through what its slot holds, in thousandths of a tick at full power.</summary>
public sealed record Analysis(int Analyzer, int Progress);

/// <summary>
/// An analyzer as its panel shows it: what its slot holds, how far it is, out of how much work, and why
/// it stands still if it does: <c>research.unpowered</c>, or <c>research.nothingNew</c> once what it
/// holds is analysed.
/// </summary>
public sealed record Analyzing(string Item, int Progress, int Work, string? Problem);

/// <summary>
/// A blueprint or a craft as the world knows it: what it costs, whether it was granted, whether
/// analysis cannot open it yet, and the items of its cost not analysed yet, in the cost's order.
/// </summary>
/// <param name="Placement">What a blueprint places; null for a craft, named by the item it makes.</param>
public sealed record Recipe(
    string Id, Placement? Placement, IReadOnlyList<(string Item, int Count)> Cost, bool Granted, bool Locked,
    IReadOnlyList<string> Unanalysed)
{
    /// <summary>Granted, or opened: every item of its cost analysed and it not locked. Creative mode aside.</summary>
    public bool Known => Granted || (!Locked && Unanalysed.Count == 0);
}

/// <summary>
/// The research slice. Public so modules can be typed by it, its members internal: others go through
/// <see cref="IResearchQueries"/>.
/// </summary>
public sealed class ResearchState
{
    /// <summary>The blueprints and crafts granted from the start, by id, sorted ordinally.</summary>
    [JsonInclude] internal List<string> Granted { get; set; } = [];
    /// <summary>The items analysed, by id, each once, sorted ordinally.</summary>
    [JsonInclude] internal List<string> Analysed { get; set; } = [];
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

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IPowerQueries _power;
    private readonly IPowerOps _powerOps;

    public override string Id => "research";

    public ResearchModule(IGridQueries grid, IItemsQueries items, IPowerQueries power, IPowerOps powerOps)
    {
        _grid = grid;
        _items = items;
        _power = power;
        _powerOps = powerOps;
        // After power's derivation: an analyzer works at its satisfaction of this tick.
        System(Phase.Simulation, Analyse);
    }

    protected override ResearchState Init(Layout layout)
    {
        var granted = (layout.Known ?? Every.Select(e => e.Id).ToList()).Distinct().ToList();
        granted.Sort(StringComparer.Ordinal);
        return new ResearchState { Granted = granted };
    }

    // Queries.

    public bool Knows(Placement placement) => Knows(Research.Blueprints.Of(placement));

    public bool Knows(string id) =>
        _items.Creative || (CostOf.TryGetValue(id, out var cost) && Known(id, cost));

    public IReadOnlyList<string> Analysed => State.Analysed;

    public IReadOnlyList<Recipe> Recipes() =>
        Every.Select(e => new Recipe(
            e.Id, e.Placement, e.Cost, Granted(e.Id), Content.Research.Locked.Contains(e.Id),
            e.Cost.Select(c => c.Item).Where(item => !IsAnalysed(item)).Distinct().ToList())).ToList();

    public Analyzing? At(int id)
    {
        var item = ItemIn(id);
        if (item == null) return null;
        int progress = State.Analyses.Find(a => a.Analyzer == id)?.Progress ?? 0;
        string? problem = IsAnalysed(item) ? "research.nothingNew"
            : _power.Satisfaction(id) == 0 ? "research.unpowered"
            : null;
        return new Analyzing(item, progress, Work, problem);
    }

    private bool Granted(string id) => State.Granted.BinarySearch(id, StringComparer.Ordinal) >= 0;

    private bool IsAnalysed(string item) => State.Analysed.BinarySearch(item, StringComparer.Ordinal) >= 0;

    /// <summary>The opening rule: granted, or every item of its cost analysed and it not locked.</summary>
    private bool Known(string id, IReadOnlyList<(string Item, int Count)> cost) =>
        Granted(id) || (!Content.Research.Locked.Contains(id) && cost.All(c => IsAnalysed(c.Item)));

    /// <summary>What machine <paramref name="id"/>'s first slot holds; null for none, or no such machine.</summary>
    private string? ItemIn(int id)
    {
        var slots = _items.Crate(id);
        return slots.Count == 0 ? null : slots[0]?.Item;
    }

    private void Analyse()
    {
        var kept = new List<Analysis>();
        foreach (var m in _grid.Machines)
        {
            if (m.Kind != "analyzer") continue;
            var item = ItemIn(m.Id);
            bool works = item != null && !IsAnalysed(item);
            // Empty, or what it holds analysed: what was done is dropped, and no power drawn.
            _powerOps.Rest(m.Id, !works);
            if (!works) continue;
            int was = State.Analyses.Find(a => a.Analyzer == m.Id)?.Progress ?? 0;
            int progress = was + _power.Satisfaction(m.Id);
            if (progress < Work)
            {
                kept.Add(new Analysis(m.Id, progress));
                continue;
            }
            // Two analyzers finishing one item on one tick: the second finds it analysed above.
            int at = State.Analysed.BinarySearch(item!, StringComparer.Ordinal);
            State.Analysed.Insert(~at, item!);
            _powerOps.Rest(m.Id, true);
        }
        State.Analyses = kept;
    }
}
