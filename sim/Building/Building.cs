using Stillwake.Sim.Crafting;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;
using Stillwake.Sim.Research;
using Cost = System.Collections.Generic.IReadOnlyList<(string Item, int Count)>;

namespace Stillwake.Sim.Building;

/// <summary>A blueprint for <c>Placement</c> at <c>Cell</c>, a wall's with its shape (0069): it costs nothing until delivered to.</summary>
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
/// E on plan <c>Plan</c>: one tick of a hero's work, sent every tick the player holds E on it within reach.
/// A tick that begins a part of a blueprint's work first takes that part's item from the hero (ADR 0063).
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
    private readonly ICrewQueries _crew;
    private readonly ICrewOps _crewOps;

    public override string Id => "building";

    public BuildingModule(
        IGridQueries grid, IGridOps gridOps, IItemsQueries items, IItemsOps itemsOps,
        IPowerQueries power, IResearchQueries research, ICraftingQueries crafting, ICrewQueries crew, ICrewOps crewOps)
    {
        _grid = grid;
        _gridOps = gridOps;
        _items = items;
        _itemsOps = itemsOps;
        _power = power;
        _research = research;
        _crafting = crafting;
        _crew = crew;
        _crewOps = crewOps;
        Handles<PlacePlan>(c => PlaceProblem(c.Cell, c.Placement), ApplyPlacePlan);
        Handles<Deconstruct>(c => MarkProblem(c.Cell, c.Layer), ApplyDeconstruct);
        Handles<CancelPlan>(c => State.Plans.Exists(p => p.Id == c.Id) ? null : "building.noPlan", ApplyCancelPlan);
        Handles<Work>(c => WorkProblemOf(c.Plan, c.From), ApplyWork);
        Handles<Dig>(c => DigProblemOf(c.From, c.Cell, c.Tool, running: true), ApplyDig);
        // A mark whose thing a meteor broke has nothing left to take apart, nor a crack its wall. In derivation, after
        // every command and construction system, so a hit made before is seen on its tick.
        System(Phase.Derivation, () =>
        {
            State.Plans.RemoveAll(p => p.Clear is { } layer && MarkedCell(p.Cell, layer) == null);
            SweepCracks();
            SweepPlants();
        });
        // A ruin's resonator zaps who stands right by it, a breath at a time (0090): before crew breathes, in its phase.
        System(Phase.Simulation, Zap);
    }

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

    /// <summary>What blueprint <paramref name="id"/> still lacks of its cost, paid an item a part as it is worked: none for a mark or no plan, nor in creative mode.</summary>
    public Cost Lacking(int id) => State.Plans.Find(p => p.Id == id) is { } plan ? LackOf(plan) : [];

    /// <summary>
    /// How many parts of blueprint <paramref name="id"/> are put in, of all its parts, one an item of its cost: those
    /// paid for, or in creative mode those its work has begun. Null for a mark or no plan.
    /// </summary>
    public (int In, int Of)? Parts(int id)
    {
        if (State.Plans.Find(p => p.Id == id) is not { Place: { } place } plan) return null;
        // The parts the work so far has begun: the last tick worked is the one before Progress.
        var (begun, parts) = plan.Progress > 0 ? PartsAt(place, plan.Progress - 1) : (0, PartsAt(place, 0).Parts);
        return (Math.Min(parts, Math.Max(begun, PaidInto(plan))), parts);
    }

    /// <summary>Ticks of work plan <paramref name="id"/> takes in all, how far a blueprint is drawn built by its work done; null for no plan.</summary>
    public int? TicksFor(int id) => State.Plans.Find(p => p.Id == id) is { } plan ? Costs.TicksFor(_grid, plan) : null;

    /// <summary>What placing <paramref name="placement"/> costs: a build menu card's price.</summary>
    public Cost CostFor(Placement placement) => Costs.BuildOf(placement).Cost;

    /// <summary>What blueprint <paramref name="id"/> costs in all, for its tip; null for a mark or no plan.</summary>
    public Cost? CostOf(int id) =>
        State.Plans.Find(p => p.Id == id)?.Place is { } place ? Costs.BuildOf(place).Cost : null;

    /// <summary>
    /// What taking apart what mark <paramref name="id"/> names gives back, as its last tick of work refunds it: for its
    /// tip. Null for a blueprint or no plan; empty once what it names is gone.
    /// </summary>
    public Cost? RefundOf(int id) =>
        State.Plans.Find(p => p.Id == id) is { Clear: { } layer } plan ? Costs.BuiltAt(_grid, plan.Cell, layer)?.Cost ?? [] : null;

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
    /// One tick of a hero's work: one that begins a part of a blueprint not paid for yet first takes an item
    /// from the sender for it (<see cref="PaymentOf"/>). The last tick builds the blueprint or takes apart what
    /// the mark names.
    /// </summary>
    private void ApplyWork(Work c)
    {
        int at = State.Plans.FindIndex(p => p.Id == c.Plan);
        var plan = State.Plans[at];
        if (Owes(plan))
        {
            // Validation found something to pay with.
            string item = PaymentOf(plan, c.From)!;
            _itemsOps.Take(c.From, [(item, 1)]);
            var delivered = plan.Delivered.ToList();
            int had = delivered.FindIndex(s => s.Item == item);
            if (had < 0) delivered.Add(new Stack(item, 1));
            else delivered[had] = new Stack(item, delivered[had].Count + 1);
            plan = plan with { Delivered = delivered };
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
            // What a mark names may be gone already, to a meteor: then there is nothing to refund. RefundOf tells the same.
            _gridOps.Clear(plan.Cell, plan.Clear!);
            _itemsOps.GiveOrDrop(from, built.Cost, plan.Cell);
        }
        State.Plans.RemoveAll(p => p.Id == plan.Id);
    }
}
