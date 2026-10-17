using Stillwake.Content;
using Stillwake.Sim.Grid;
using Cost = System.Collections.Generic.IReadOnlyList<(string Item, int Count)>;

namespace Stillwake.Sim.Building;

/// <summary>Which plans may be made and worked (rules.ts).</summary>
public sealed partial class BuildingModule
{
    /// <summary>The layers deconstruction looks in, in the order it takes them.</summary>
    private static readonly string[] ClearOrder = ["machine", "conduit", "structure", "back"];

    /// <summary>
    /// Structure and machines exclude each other in a cell; a conduit runs through either, and a back
    /// wall stands behind all three.
    /// </summary>
    private static string SlotOf(string layer) => layer is "conduit" or "back" ? layer : "solid";

    /// <summary>The cells a blueprint for <paramref name="place"/> at <paramref name="cell"/> covers: a machine's whole footprint, otherwise its one cell.</summary>
    private IReadOnlyList<int> Covers(int cell, Placement place) =>
        place.Layer == "machine" ? _grid.Footprint(place.Kind, cell) ?? [cell] : [cell];

    /// <summary>The cells a plan covers: a blueprint's, or the whole machine a mark names.</summary>
    private IReadOnlyList<int> Covers(Plan plan)
    {
        if (plan.Place is { } place) return Covers(plan.Cell, place);
        var machine = plan.Clear == "machine" ? _grid.MachineAt(plan.Cell) : null;
        return (machine != null ? _grid.Footprint(machine.Kind, machine.Cell) : null) ?? [plan.Cell];
    }

    /// <summary>Where a mark to clear <paramref name="layer"/> at <paramref name="cell"/> is kept, or null when that layer is empty there.</summary>
    private int? MarkedCell(int cell, string layer) => layer switch
    {
        "structure" => _grid.StructureAt(cell) == Structure.Empty ? null : cell,
        "conduit" => _grid.ConduitAt(cell) == Conduit.None ? null : cell,
        "machine" => _grid.MachineAt(cell)?.Cell,
        "back" => _grid.BackAt(cell) == null ? null : cell,
        _ => null,
    };

    private bool IsMarked(string layer, int cell) => State.Plans.Exists(p => p.Clear == layer && p.Cell == cell);

    /// <summary>
    /// What deconstruction takes from a cell next, of <paramref name="layers"/>: a machine, then a
    /// conduit, then the structure, then the back wall, passing over what is marked already, so a second
    /// pass over a pipe in a wall marks the wall. When everything there is marked, the top one, which a
    /// mark would refuse.
    /// </summary>
    private (string Layer, int Cell)? NextToClear(int cell, IReadOnlyList<string>? layers)
    {
        (string Layer, int Cell)? top = null;
        foreach (var layer in ClearOrder)
        {
            if (layers != null && !layers.Contains(layer)) continue;
            if (MarkedCell(cell, layer) is not int at) continue;
            if (!IsMarked(layer, at)) return (layer, at);
            top ??= (layer, at);
        }
        return top;
    }

    /// <summary>
    /// Why taking <paramref name="layer"/> out of <paramref name="cell"/> would leave something behind:
    /// <c>building.cabled</c> for a machine a cable ends on, <c>building.notEmpty</c> for one holding
    /// items (a crate, a furnace) or a station with crafts queued.
    /// </summary>
    private string? InUse(int cell, string layer)
    {
        var machine = layer == "machine" ? _grid.MachineAt(cell) : null;
        if (machine == null) return null;
        if (_power.Cabled(machine.Cell)) return "building.cabled";
        bool holds = _items.Crate(machine.Id).Any(s => s != null) || _crafting.Queue(machine.Id).Count > 0;
        return holds ? "building.notEmpty" : null;
    }

    /// <summary>Whether <paramref name="kind"/> is a machine built into the ship from its start (the lifepod): never planned, never taken apart.</summary>
    private static bool IsFixed(string kind) => Machines.TryGet(kind, out var spec) && spec.Fixed;

    /// <summary>Why <paramref name="layer"/> of <paramref name="cell"/> cannot be marked, as a refusal key; null when it can.</summary>
    private string? MarkProblem(int cell, string layer)
    {
        if (MarkedCell(cell, layer) is not int at) return "building.nothingThere";
        if (layer == "machine" && _grid.MachineAt(at) is { } machine && IsFixed(machine.Kind)) return "building.fixed";
        if (IsMarked(layer, at)) return "building.planned";
        return InUse(at, layer);
    }

    /// <summary>
    /// Why <paramref name="place"/> cannot be planned at <paramref name="cell"/>: a machine built into the ship, a blueprint not known
    /// (ADR 0024), what the grid says, then a plan already claiming its slot in a cell it covers.
    /// </summary>
    private string? PlaceProblem(int cell, Placement place)
    {
        if (place.Layer == "machine" && IsFixed(place.Kind)) return "building.fixed";
        if (!_research.Knows(place)) return "building.unknown";
        var gridProblem = _grid.PlacementProblem(cell, place);
        if (gridProblem != null) return gridProblem;
        string slot = SlotOf(place.Layer);
        var wanted = Covers(cell, place);
        bool taken = State.Plans.Exists(p =>
            p.Place is { } other && SlotOf(other.Layer) == slot && Covers(p).Any(wanted.Contains));
        return taken ? "building.planned" : null;
    }

    /// <summary>
    /// What blueprint <paramref name="plan"/> still lacks before work on it may begin; nothing in
    /// creative mode, where it is built as it is, nor for a mark.
    /// </summary>
    private Cost LackOf(Plan plan) =>
        plan.Place is { } place && !_items.Creative ? Costs.Lacking(Costs.BuildOf(place).Cost, plan.Delivered) : [];

    /// <summary>
    /// Why E on plan <paramref name="id"/> by peer <paramref name="peer"/> is refused: the plan is gone;
    /// it is a blueprint still lacking something and the hero carries none of it; or it is a mark on a
    /// crate that holds something or a machine a cable ends on.
    /// </summary>
    private string? WorkProblemOf(int id, int peer)
    {
        var plan = State.Plans.Find(p => p.Id == id);
        if (plan == null) return "building.noPlan";
        if (plan.Clear is { } layer) return InUse(plan.Cell, layer);
        var due = LackOf(plan);
        if (due.Count == 0) return null;
        var carried = _items.Totals(peer);
        return due.Any(d => carried.Any(c => c.Item == d.Item && c.Count > 0)) ? null : "items.notEnough";
    }
}
