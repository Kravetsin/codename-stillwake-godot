using System.Text.Json.Serialization;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;

namespace Stillwake.Sim.Research;

/// <summary>An analyzer part-way through what its slot holds, in thousandths of a tick at full power.</summary>
public sealed record Analysis(int Analyzer, int Progress);

/// <summary>
/// An analyzer as its panel shows it: what its slot holds, how far it is, out of how much work, and why
/// it stands still if it does: <c>research.unpowered</c> or <c>research.nothingNew</c>.
/// </summary>
public sealed record Analyzing(string Item, int Progress, int Work, string? Problem);

/// <summary>
/// The research slice. Public so modules can be typed by it, its members internal: others go through
/// <see cref="IResearchQueries"/>.
/// </summary>
public sealed class ResearchState
{
    /// <summary>The blueprints known, by id, sorted ordinally.</summary>
    [JsonInclude] internal List<string> Known { get; set; } = [];
    /// <summary>The analyzers at work, by machine id, in id order.</summary>
    [JsonInclude] internal List<Analysis> Analyses { get; set; } = [];
}

/// <summary>What any module may read of the research. Lists are live: a reader never writes them.</summary>
public interface IResearchQueries
{
    /// <summary>Whether <paramref name="placement"/>'s blueprint is known: every one in creative mode.</summary>
    bool Knows(Placement placement);
    /// <summary>The blueprints known, by id, sorted ordinally.</summary>
    IReadOnlyList<string> Known { get; }
    /// <summary>Every blueprint there is, with what it places.</summary>
    IReadOnlyList<Blueprint> Blueprints { get; }
    /// <summary>Analyzer <paramref name="id"/> as its panel shows it; null for an empty slot.</summary>
    Analyzing? At(int id);
}

/// <summary>
/// Knowing blueprints (ADR 0024): which blueprints the world knows, and analyzers learning more from
/// what is put in them (packages/sim/src/modules/research).
/// </summary>
public sealed class ResearchModule : Module<ResearchState>, IResearchQueries
{
    private const int Work = Content.Research.Ticks * 1000;

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IPowerQueries _power;
    private readonly IPowerOps _powerOps;

    public override string Id => "research";

    public ResearchModule(IGridQueries grid, IItemsQueries items, IItemsOps itemsOps, IPowerQueries power, IPowerOps powerOps)
    {
        _grid = grid;
        _items = items;
        _itemsOps = itemsOps;
        _power = power;
        _powerOps = powerOps;
        // After power's derivation: an analyzer works at its satisfaction of this tick.
        System(Phase.Simulation, Analyse);
    }

    protected override ResearchState Init(Layout layout)
    {
        var known = (layout.Known ?? Research.Blueprints.All.Select(b => b.Id).ToList()).ToList();
        known.Sort(StringComparer.Ordinal);
        return new ResearchState { Known = known };
    }

    // Queries.

    public bool Knows(Placement placement) => _items.Creative || State.Known.Contains(Research.Blueprints.Of(placement));
    public IReadOnlyList<string> Known => State.Known;
    public IReadOnlyList<Blueprint> Blueprints => Research.Blueprints.All;

    public Analyzing? At(int id)
    {
        var item = ItemIn(id);
        if (item == null) return null;
        int progress = State.Analyses.Find(a => a.Analyzer == id)?.Progress ?? 0;
        string? problem = Teaches(State.Known, item).Count == 0 ? "research.nothingNew"
            : _power.Satisfaction(id) == 0 ? "research.unpowered"
            : null;
        return new Analyzing(item, progress, Work, problem);
    }

    /// <summary>What machine <paramref name="id"/>'s first slot holds; null for none, or no such machine.</summary>
    private string? ItemIn(int id)
    {
        var slots = _items.Crate(id);
        return slots.Count == 0 ? null : slots[0]?.Item;
    }

    /// <summary>
    /// What analysing <paramref name="item"/> would teach, given <paramref name="known"/>: a schematic the
    /// first machine of <c>Schematics</c> not yet known; anything else the blueprints it unlocks but
    /// those known; nothing for nothing.
    /// </summary>
    private static List<string> Teaches(List<string> known, string? item)
    {
        if (item == null) return [];
        if (item == "schematic")
        {
            var next = Content.Research.Schematics.FirstOrDefault(id => !known.Contains(id));
            return next == null ? [] : [next];
        }
        foreach (var (unlocking, blueprints) in Content.Research.Unlocks)
            if (unlocking == item) return blueprints.Where(id => !known.Contains(id)).ToList();
        return [];
    }

    private void Analyse()
    {
        var kept = new List<Analysis>();
        foreach (var m in _grid.Machines)
        {
            if (m.Kind != "analyzer") continue;
            var item = ItemIn(m.Id);
            var learns = Teaches(State.Known, item);
            // An empty slot, or nothing new in it: what was done is dropped, and no power drawn.
            _powerOps.Rest(m.Id, learns.Count == 0);
            if (learns.Count == 0) continue;
            int was = State.Analyses.Find(a => a.Analyzer == m.Id)?.Progress ?? 0;
            int progress = was + _power.Satisfaction(m.Id);
            if (progress < Work)
            {
                kept.Add(new Analysis(m.Id, progress));
                continue;
            }
            State.Known.AddRange(learns);
            State.Known.Sort(StringComparer.Ordinal);
            _powerOps.Rest(m.Id, true);
            // A schematic is used up; a material stays to be taken back.
            if (item == "schematic") _itemsOps.Consume(m.Id, 0);
        }
        State.Analyses = kept;
    }
}
