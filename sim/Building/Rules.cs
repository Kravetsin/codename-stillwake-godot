using Stillwake.Content;
using Stillwake.Sim.Grid;
using Cost = System.Collections.Generic.IReadOnlyList<(string Item, int Count)>;

namespace Stillwake.Sim.Building;

/// <summary>Which plans may be made and worked (rules.ts).</summary>
public sealed partial class BuildingModule
{
    /// <summary>The layers deconstruction looks in, in the order it takes them.</summary>
    private static readonly string[] ClearOrder = ["machine", "structure", "back"];

    /// <summary>
    /// Structure and machines exclude each other in a cell, and a back wall stands behind both.
    /// </summary>
    private static string SlotOf(string layer) => layer is "back" ? layer : "solid";

    /// <summary>
    /// The cells a blueprint for <paramref name="place"/> at <paramref name="cell"/> covers: a machine's or a piece's whole footprint
    /// (a ladder's, a door's, 0104), otherwise its one cell.
    /// </summary>
    private IReadOnlyList<int> Covers(int cell, Placement place) =>
        place.Layer == "machine" || Builds.PieceOf(place.Kind) != null ? _grid.Footprint(place.Kind, cell) ?? [cell] : [cell];

    /// <summary>The cells a plan covers: a blueprint's, or the whole machine or piece a mark names.</summary>
    private IReadOnlyList<int> Covers(Plan plan)
    {
        if (plan.Place is { } place) return Covers(plan.Cell, place);
        if (plan.Clear == "structure") return _grid.PieceOf(plan.Cell) is { Count: > 0 } piece ? piece : [plan.Cell];
        var machine = plan.Clear == "machine" ? _grid.MachineAt(plan.Cell) : null;
        return (machine != null ? _grid.Footprint(machine.Kind, machine.Cell) : null) ?? [plan.Cell];
    }

    /// <summary>
    /// Where a mark to clear <paramref name="layer"/> at <paramref name="cell"/> is kept, or null when that layer is empty there: a
    /// machine's at its top-left, a piece's at its first cell (0104), whichever of their cells was marked.
    /// </summary>
    private int? MarkedCell(int cell, string layer) => layer switch
    {
        "structure" => _grid.PieceOf(cell) is { Count: > 0 } piece ? piece[0] : null,
        "machine" => _grid.MachineAt(cell)?.Cell,
        "back" => _grid.BackAt(cell) == null ? null : cell,
        _ => null,
    };

    private bool IsMarked(string layer, int cell) => State.Plans.Exists(p => p.Clear == layer && p.Cell == cell);

    /// <summary>
    /// What deconstruction takes from a cell next, of <paramref name="layers"/> in their order, else a machine, then
    /// the structure, then the back wall; passing over what is marked already, so a second pass over a
    /// machine marks what stands behind. When everything there is marked, the top one, which a mark would refuse.
    /// </summary>
    private (string Layer, int Cell)? NextToClear(int cell, IReadOnlyList<string>? layers)
    {
        (string Layer, int Cell)? top = null;
        foreach (var layer in layers ?? ClearOrder)
        {
            if (MarkedCell(cell, layer) is not int at) continue;
            if (!IsMarked(layer, at)) return (layer, at);
            top ??= (layer, at);
        }
        return top;
    }

    /// <summary>
    /// Why taking <paramref name="layer"/> out of <paramref name="cell"/> would leave something behind:
    /// <c>building.notEmpty</c> for a machine holding items (a crate, a furnace), a module fitted (0086) or a station with
    /// crafts queued. A machine cables end on comes apart, its cables kept (0110).
    /// </summary>
    private string? InUse(int cell, string layer)
    {
        var machine = layer == "machine" ? _grid.MachineAt(cell) : null;
        if (machine == null) return null;
        bool holds = _items.Crate(machine.Id).Any(s => s != null) || _items.FittedTo(machine.Id).Count > 0
            || _crafting.Queue(machine.Id).Count > 0;
        return holds ? "building.notEmpty" : null;
    }

    /// <summary>Whether <paramref name="kind"/> is a machine built into the ship from its start (the lifepod): never planned, never taken apart.</summary>
    private static bool IsFixed(string kind) => Machines.TryGet(kind, out var spec) && spec.Fixed;

    /// <summary>Why <paramref name="layer"/> of <paramref name="cell"/> cannot be marked, as a refusal key; null when it can.</summary>
    private string? MarkProblem(int cell, string layer)
    {
        if (MarkedCell(cell, layer) is not int at) return "building.nothingThere";
        if (IsNatural(at, layer)) return "building.natural";
        if (layer == "machine" && _grid.MachineAt(at) is { } machine && IsFixed(machine.Kind)) return "building.fixed";
        if (IsMarked(layer, at)) return "building.planned";
        return InUse(at, layer);
    }

    /// <summary>
    /// Why <paramref name="place"/> cannot be planned at <paramref name="cell"/>: a machine built into the ship, one its surface's kind
    /// does not allow (0076), a blueprint not known (ADR 0024), what the grid says, then a plan already claiming its slot in a cell it covers.
    /// </summary>
    private string? PlaceProblem(int cell, Placement place)
    {
        if (place.Layer == "machine" && IsFixed(place.Kind)) return "building.fixed";
        if (!Rules.Allows(Builds.BuiltAs(place.Kind))) return "building.notHere";
        if (Unbuilt(place) || !_research.Knows(place)) return "building.unknown";
        var gridProblem = _grid.PlacementProblem(cell, place);
        if (gridProblem != null) return gridProblem;
        string slot = SlotOf(place.Layer);
        var wanted = Covers(cell, place);
        bool taken = State.Plans.Exists(p =>
            p.Place is { } other && SlotOf(other.Layer) == slot && Covers(p).Any(wanted.Contains));
        return taken ? "building.planned" : null;
    }

    /// <summary>A wall or back wall of a material no blueprint builds (0077): of bedrock; creative mode too.</summary>
    private static bool Unbuilt(Placement place) =>
        place.Kind is "wall" or "backWall" && Materials.TryGet(place.Material ?? "", out _) && !Research.Blueprints.Has(place);

    /// <summary>
    /// What blueprint <paramref name="plan"/> still lacks of its cost, in the cost's order; nothing in creative
    /// mode, where it is built as it is, nor for a mark.
    /// </summary>
    private Cost LackOf(Plan plan) =>
        plan.Place is { } place && !_items.Creative ? Costs.Lacking(Costs.BuildOf(place).Cost, plan.Delivered) : [];

    /// <summary>
    /// The parts of the work on a blueprint for <paramref name="place"/> begun by the tick at <paramref name="progress"/>,
    /// that tick counted, and all its parts: one an item of its cost (ADR 0063).
    /// </summary>
    private static (int Begun, int Parts) PartsAt(Placement place, int progress)
    {
        var build = Costs.BuildOf(place);
        int parts = Costs.PartsOf(build.Cost);
        return (Costs.PartsBegun(progress, build.BuildTicks, parts), parts);
    }

    /// <summary>The items paid into blueprint <paramref name="plan"/>: one a part begun.</summary>
    private static int PaidInto(Plan plan) => plan.Delivered.Sum(s => s.Count);

    /// <summary>
    /// Whether the next tick of work on <paramref name="plan"/> begins a part not paid for yet, so its worker
    /// pays an item first; never in creative mode, nor for a mark.
    /// </summary>
    private bool Owes(Plan plan) =>
        plan.Place is { } place && !_items.Creative && PaidInto(plan) < PartsAt(place, plan.Progress).Begun;

    /// <summary>
    /// What peer <paramref name="peer"/> pays for the part of <paramref name="plan"/> that begins next: the first
    /// item in the cost's order that the plan still lacks and the hero carries; null when it carries none.
    /// </summary>
    private string? PaymentOf(Plan plan, int peer)
    {
        var carried = _items.Totals(peer);
        foreach (var (item, _) in LackOf(plan))
            if (carried.Any(c => c.Item == item && c.Count > 0)) return item;
        return null;
    }

    /// <summary>
    /// Whether a hero's body is in <paramref name="cell"/>: the cell its client last reported (<c>Hero.Cell</c>, the body's middle),
    /// or the one above or under it, a body being under three cells tall. Its other column, where it straddles two, is not asked.
    /// </summary>
    private bool HeroIn(int cell)
    {
        int w = _grid.Width;
        foreach (var hero in _crew.Heroes)
            if (cell == hero.Cell || cell == hero.Cell - w || cell == hero.Cell + w) return true;
        return false;
    }

    /// <summary>
    /// Why E on plan <paramref name="id"/> by peer <paramref name="peer"/> is refused: the plan is gone; it is a
    /// blueprint whose next part is not paid for and the hero carries nothing it still lacks; or it is a mark on
    /// a crate that holds something or a machine a cable ends on; or it is a wall whose last tick would finish it on a hero.
    /// </summary>
    private string? WorkProblemOf(int id, int peer)
    {
        var plan = State.Plans.Find(p => p.Id == id);
        if (plan == null) return "building.noPlan";
        if (plan.Clear is { } layer) return InUse(plan.Cell, layer);
        // A wall's last tick waits while a hero stands in its cell, so nobody walls themselves in (0077); it finishes once they step out.
        if (plan.Place is { Kind: "wall" } && plan.Progress + 1 >= Costs.TicksFor(_grid, plan) && HeroIn(plan.Cell)) return "building.heroInside";
        return Owes(plan) && PaymentOf(plan, peer) == null ? "items.notEnough" : null;
    }
}
