using Stillwake.Sim.Crafting;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;
using Stillwake.Sim.Research;
using Cost = System.Collections.Generic.IReadOnlyList<(string Item, int Count)>;

namespace Stillwake.Sim.Building;

/// <summary>A blueprint for <c>Placement</c> at <c>Cell</c>: it costs nothing until delivered to.</summary>
public sealed record PlacePlan(int Cell, Placement Placement) : Command;

/// <summary>A mark on what <c>Layer</c> of <c>Cell</c> holds, for heroes' work to take apart.</summary>
/// <remarks>Not positional: a positional record has a <c>Deconstruct</c> method, which this name forbids.</remarks>
public sealed record Deconstruct : Command
{
    public int Cell { get; init; }
    public string Layer { get; init; }

    public Deconstruct(int cell, string layer)
    {
        Cell = cell;
        Layer = layer;
    }
}

/// <summary>Cancels plan <c>Id</c>, a blueprint or a mark; what was delivered to a blueprint falls at it.</summary>
public sealed record CancelPlan(int Id) : Command;

/// <summary>
/// E on plan <c>Plan</c>: a delivery to a blueprint still lacking something, otherwise one tick of a
/// hero's work, sent every tick the player holds E on it within reach.
/// </summary>
public sealed record Work(int Plan) : Command;

/// <summary>
/// Plans, and the work that turns them into the ship (packages/sim/src/modules/building): blueprints
/// built and marks taken apart through the grid's operations, paid from and refunded to the items.
/// </summary>
public sealed partial class BuildingModule : Module<BuildingState>
{
    private readonly IGridQueries _grid;
    private readonly IGridOps _gridOps;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IPowerQueries _power;
    private readonly IResearchQueries _research;
    private readonly ICraftingQueries _crafting;

    public override string Id => "building";

    public BuildingModule(
        IGridQueries grid, IGridOps gridOps, IItemsQueries items, IItemsOps itemsOps,
        IPowerQueries power, IResearchQueries research, ICraftingQueries crafting)
    {
        _grid = grid;
        _gridOps = gridOps;
        _items = items;
        _itemsOps = itemsOps;
        _power = power;
        _research = research;
        _crafting = crafting;
        Handles<PlacePlan>(c => PlaceProblem(c.Cell, c.Placement), ApplyPlacePlan);
        Handles<Deconstruct>(c => MarkProblem(c.Cell, c.Layer), ApplyDeconstruct);
        Handles<CancelPlan>(c => State.Plans.Exists(p => p.Id == c.Id) ? null : "building.noPlan", ApplyCancelPlan);
        Handles<Work>(c => WorkProblemOf(c.Plan, c.From), ApplyWork);
        // A mark whose thing a meteor broke has nothing left to take apart. In derivation, after every
        // command and construction system, so a hit made before is seen on its tick.
        System(Phase.Derivation, () => State.Plans.RemoveAll(p => p.Clear is { } layer && MarkedCell(p.Cell, layer) == null));
    }

    protected override BuildingState Init(Layout layout) => new();

    // Queries.

    /// <summary>Every plan, in the order they were made; live and read-only.</summary>
    public IReadOnlyList<Plan> Plans => State.Plans;

    /// <summary>Every plan covering <paramref name="cell"/>, in the order they were made.</summary>
    public IReadOnlyList<Plan> PlansAt(int cell) => State.Plans.Where(p => Covers(p).Contains(cell)).ToList();

    /// <summary>
    /// What a removal takes from <paramref name="cell"/> next, of <paramref name="layers"/> or all of
    /// them, and the cell its mark is kept at; null when none of those layers holds anything there.
    /// </summary>
    public (string Layer, int Cell)? ClearAt(int cell, IReadOnlyList<string>? layers = null) => NextToClear(cell, layers);

    /// <summary>Why a blueprint for <paramref name="placement"/> at <paramref name="cell"/> would be refused: what a ghost shows before the command is sent.</summary>
    public string? Problem(int cell, Placement placement) => PlaceProblem(cell, placement);

    /// <summary>Why a mark on <paramref name="layer"/> of <paramref name="cell"/> would be refused: what a removal ghost shows.</summary>
    public string? Problem(int cell, string layer) => MarkProblem(cell, layer);

    /// <summary>Why peer <paramref name="peer"/>'s work on plan <paramref name="id"/> would be refused: what the client shows first.</summary>
    public string? WorkProblem(int id, int peer) => WorkProblemOf(id, peer);

    /// <summary>What blueprint <paramref name="id"/> still lacks before work on it may begin: none for a mark or no plan.</summary>
    public Cost Lacking(int id) => State.Plans.Find(p => p.Id == id) is { } plan ? LackOf(plan) : [];

    /// <summary>Ticks of work plan <paramref name="id"/> takes in all, for its bar; null for no plan.</summary>
    public int? TicksFor(int id) => State.Plans.Find(p => p.Id == id) is { } plan ? Costs.TicksFor(_grid, plan) : null;

    /// <summary>What placing <paramref name="placement"/> costs: a build menu card's price.</summary>
    public Cost CostFor(Placement placement) => Costs.BuildOf(placement).Cost;

    /// <summary>What blueprint <paramref name="id"/> costs in all, for its tray and bar; null for a mark or no plan.</summary>
    public Cost? CostOf(int id) =>
        State.Plans.Find(p => p.Id == id)?.Place is { } place ? Costs.BuildOf(place).Cost : null;

    // Commands.

    private void ApplyPlacePlan(PlacePlan c)
    {
        State.Plans.Add(Plan.Blueprint(State.NextPlanId, c.Cell, c.Placement));
        State.NextPlanId += 1;
    }

    private void ApplyDeconstruct(Deconstruct c)
    {
        State.Plans.Add(Plan.Mark(State.NextPlanId, MarkedCell(c.Cell, c.Layer) ?? c.Cell, c.Layer));
        State.NextPlanId += 1;
    }

    private void ApplyCancelPlan(CancelPlan c)
    {
        var plan = State.Plans.Find(p => p.Id == c.Id)!;
        // What was delivered falls where the blueprint was, for whoever comes to pick it up.
        if (plan.Place != null) _itemsOps.DropAt(c.From, plan.Delivered.Select(s => (s.Item, s.Count)).ToList(), plan.Cell);
        State.Plans.Remove(plan);
    }

    /// <summary>
    /// A blueprint still lacking something takes from the sender, item by item, the lesser of what it
    /// lacks and what the sender carries. Otherwise it is one tick of a hero's work: the last builds the
    /// blueprint or takes apart what the mark names.
    /// </summary>
    private void ApplyWork(Work c)
    {
        int at = State.Plans.FindIndex(p => p.Id == c.Plan);
        var plan = State.Plans[at];
        var due = LackOf(plan);
        if (plan.Place != null && due.Count > 0)
        {
            var carried = _items.Totals(c.From);
            var delivered = plan.Delivered.ToList();
            foreach (var (item, n) in due)
            {
                int given = Math.Min(n, carried.FirstOrDefault(s => s.Item == item).Count);
                if (given == 0) continue;
                _itemsOps.Take(c.From, [(item, given)]);
                int had = delivered.FindIndex(s => s.Item == item);
                if (had < 0) delivered.Add(new Stack(item, given));
                else delivered[had] = new Stack(item, delivered[had].Count + given);
            }
            State.Plans[at] = plan with { Delivered = delivered };
            return;
        }
        var worked = plan with { Progress = plan.Progress + 1 };
        State.Plans[at] = worked;
        if (worked.Progress >= Costs.TicksFor(_grid, worked)) Complete(worked, c.From);
    }

    /// <summary>
    /// Builds what a blueprint asks for, or takes apart what a mark names, refunding it to the hero of
    /// peer <paramref name="from"/>, what does not fit falling at the mark; drops the plan.
    /// </summary>
    private void Complete(Plan plan, int from)
    {
        if (plan.Place is { } place)
        {
            // Plans claim their slots, so nothing can fill one first: a refusal here is our bug.
            _gridOps.Place(plan.Cell, place);
        }
        else if (Costs.BuiltAt(_grid, plan.Cell, plan.Clear!) is { } built)
        {
            // What a mark names may be gone already, to a meteor: then there is nothing to refund.
            _gridOps.Clear(plan.Cell, plan.Clear!);
            _itemsOps.GiveOrDrop(from, built.Cost, plan.Cell);
        }
        State.Plans.RemoveAll(p => p.Id == plan.Id);
    }
}
