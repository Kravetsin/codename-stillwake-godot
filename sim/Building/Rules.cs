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
        // Where tools break what stands, nothing is marked: a planet's things go to tools alone (the fan of rays).
        if (Rules.ToolsBreak) return "building.notHere";
        if (layer == "machine" && _grid.MachineAt(at) is { } machine)
        {
            if (IsFixed(machine.Kind)) return "building.fixed";
            if (Machines.Get(machine.Kind).Wreck) return "building.needsAxe";
        }
        if (IsMarked(layer, at)) return "building.planned";
        return InUse(at, layer) ?? HoldsMachine(at, layer);
    }

    /// <summary>
    /// Why taking the structure out of <paramref name="cell"/> would leave a machine standing on nothing (2026-11-07-machines-stand):
    /// <c>building.holdsMachine</c> when a standing machine stands on any cell of its piece.
    /// </summary>
    private string? HoldsMachine(int cell, string layer)
    {
        if (layer != "structure") return null;
        int width = _grid.Width;
        foreach (int c in _grid.PieceOf(cell) is { Count: > 0 } piece ? piece : [cell])
            if (c >= width && _grid.MachineAt(c - width) is { } machine && Machines.Stands(machine.Kind)) return "building.holdsMachine";
        return null;
    }

    /// <summary>
    /// Why a standing machine <paramref name="place"/> at <paramref name="cell"/> has nothing to stand on (2026-11-07-machines-stand):
    /// <c>building.noFloor</c> unless every cell of the row under its footprint is footing, built or, when <paramref name="planned"/>
    /// counts, a blueprint of one. Null for anything else.
    /// </summary>
    private string? FootingProblem(int cell, Placement place, bool planned)
    {
        if (place.Layer != "machine" || !Machines.Stands(place.Kind)) return null;
        var spec = Machines.Get(place.Kind);
        int below = cell + spec.Height * _grid.Width;
        for (int dx = 0; dx < spec.Width; dx++)
            if (!_grid.Footing(below + dx) && !(planned && FootingPlanned(below + dx))) return "building.noFloor";
        return null;
    }

    /// <summary>Whether a blueprint of a footing structure (a wall, a floor) covers <paramref name="cell"/>.</summary>
    private bool FootingPlanned(int cell) => State.Plans.Exists(p =>
        p.Place is { Layer: "structure" } s && Structures.Placed(s.Kind) is int code && Structures.Footing(code) && Covers(p).Contains(cell));

    /// <summary>
    /// Why <paramref name="place"/> may not be planned at <paramref name="cell"/> for the shafts planned platforms keep, as the grid
    /// keeps built ones': under one (<c>grid.underPlatform</c>), or a platform over a solid plan (<c>grid.shaftBlocked</c>). Back walls
    /// are behind the fall.
    /// </summary>
    private string? PlannedShaftProblem(int cell, Placement place)
    {
        if (place.Layer == "back") return null;
        static bool Platform(Placement p) => p.Layer == "structure" && Structures.Placed(p.Kind) is int code && Doorways.IsPlatform(code);
        int width = _grid.Width;
        var wanted = Covers(cell, place);
        foreach (var plan in State.Plans)
        {
            if (plan.Place is not { } other || other.Layer == "back") continue;
            var theirs = Covers(plan);
            if (Platform(other) && wanted.Any(c => theirs.Any(t => t % width == c % width && t < c))) return "grid.underPlatform";
            if (Platform(place) && theirs.Any(t => wanted.Any(c => c % width == t % width && t > c))) return "grid.shaftBlocked";
        }
        return null;
    }

    /// <summary>
    /// Why <paramref name="place"/> cannot be planned at <paramref name="cell"/>: a machine built into the ship, one its surface's kind
    /// does not allow (0076), a blueprint <paramref name="peer"/> does not know (ADR 0024), what the grid says, the shafts, a standing
    /// machine with no footing under it built or planned (built only, building instantly), a plan already claiming its slot in a cell
    /// it covers, its ports, then, building instantly, a wall on a hero.
    /// </summary>
    private string? PlaceProblem(int cell, Placement place, int peer)
    {
        if (place.Layer == "machine" && IsFixed(place.Kind)) return "building.fixed";
        if (!Rules.Allows(place.Kind)) return "building.notHere";
        if (Unbuilt(place) || !_research.Knows(peer, place)) return "building.unknown";
        var gridProblem = _grid.PlacementProblem(cell, place);
        if (gridProblem != null) return gridProblem;
        if (PlannedShaftProblem(cell, place) is { } shaft) return shaft;
        if (FootingProblem(cell, place, planned: !State.Instant) is { } footing) return footing;
        string slot = SlotOf(place.Layer);
        var wanted = Covers(cell, place);
        bool taken = State.Plans.Exists(p =>
            p.Place is { } other && SlotOf(other.Layer) == slot && Covers(p).Any(wanted.Contains));
        if (taken) return "building.planned";
        // A machine stands only where its ports fit the pipes there (2026-10-29-typed-ports).
        if (place.Layer == "machine" && _pipes.SeatProblem(place.Kind, cell) is { } seat) return seat;
        // Built at once, a wall waits for no step out: it is refused on a hero, as its last tick of work would be (0077).
        return State.Instant && place.Kind == "wall" && HeroIn(cell) ? "building.heroInside" : null;
    }

    /// <summary>A wall or back wall of a material no blueprint builds (0077), of bedrock, or a machine none does, a pile; creative mode too.</summary>
    private static bool Unbuilt(Placement place) =>
        ((place.Kind is "wall" or "backWall" && Materials.TryGet(place.Material ?? "", out _)) || (place.Layer == "machine" && Machines.TryGet(place.Kind, out _))) && !Research.Blueprints.Has(place);

    /// <summary>
    /// What blueprint <paramref name="plan"/> still lacks of its cost, in the cost's order; nothing for a mark. A creative
    /// worker pays its parts as anyone does, from nothing, so whoever works on after it owes only the parts after.
    /// </summary>
    private Cost LackOf(Plan plan) =>
        plan.Place is { } place ? Costs.Lacking(Costs.BuildOf(place).Cost, plan.Delivered) : [];

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
    /// pays an item first; never for a mark.
    /// </summary>
    private bool Owes(Plan plan) =>
        plan.Place is { } place && PaidInto(plan) < PartsAt(place, plan.Progress).Begun;

    /// <summary>
    /// What peer <paramref name="peer"/> pays for the part of <paramref name="plan"/> that begins next: the first
    /// item in the cost's order that the plan still lacks and its stock holds (build-from-crates), or a creative peer's first
    /// lacking from nothing; null with none.
    /// </summary>
    private string? PaymentOf(Plan plan, int peer)
    {
        if (_items.Creative(peer)) return LackOf(plan) is [var (first, _), ..] ? first : null;
        var carried = _items.StockTotals(peer);
        foreach (var (item, _) in LackOf(plan))
            if (carried.Any(c => c.Item == item && c.Count > 0)) return item;
        return null;
    }

    /// <summary>
    /// Thousandths of a cell from a standing body's middle up to the top of its cell: its feet on a cell's edge, the middle half
    /// the height above them (<c>Hero.Middle</c> in the client).
    /// </summary>
    private const int MiddleInCell = (1000 - Hero.Height / 2 % 1000) % 1000;

    /// <summary>The rows a standing body reaches above and below its middle's, and the columns either side, under two cells wide.</summary>
    private const int RowsUp = (Hero.Height / 2 - MiddleInCell + 999) / 1000, RowsDown = (MiddleInCell + Hero.Height / 2 - 1) / 1000,
        ColumnsBeside = (Hero.Width / 2 + 999) / 1000;

    /// <summary>
    /// Whether a hero's body is in <paramref name="cell"/>: within the box a standing body fills round the cell its client last
    /// reported (<c>Hero.Cell</c>, the body's middle), whichever side of it the middle is on. A body off its feet may reach a
    /// row further down, not asked: a wall would land under its feet.
    /// </summary>
    private bool HeroIn(int cell)
    {
        int w = _grid.Width, x = cell % w, y = cell / w;
        foreach (var hero in _crew.Heroes)
        {
            int across = x - hero.Cell % w, down = y - hero.Cell / w;
            if (Math.Abs(across) <= ColumnsBeside && down >= -RowsUp && down <= RowsDown) return true;
        }
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
        if (plan.Clear is { } layer) return Rules.ToolsBreak ? "building.notHere" : InUse(plan.Cell, layer) ?? HoldsMachine(plan.Cell, layer);
        bool last = plan.Progress + 1 >= Costs.TicksFor(_grid, plan);
        // A wall's last tick waits while a hero stands in its cell, so nobody walls themselves in (0077); it finishes once they step out.
        if (plan.Place is { Kind: "wall" } && last && HeroIn(plan.Cell)) return "building.heroInside";
        // So does any last tick over what stands there now: a drop pod docks or lands where it finds room, plans or not.
        if (plan.Place is { } place && last && _grid.PlacementProblem(plan.Cell, place) is { } blocked)
            return blocked;
        // A standing machine finishes only on footing built by then, a floor planned under it not yet laid (2026-11-07-machines-stand).
        if (plan.Place is { } standing && last && FootingProblem(plan.Cell, standing, planned: false) is { } floor) return floor;
        // Nor over pipes its ports no longer fit, piped since it was planned (2026-10-29-typed-ports).
        if (plan.Place is { Layer: "machine" } machine && last && _pipes.SeatProblem(machine.Kind, plan.Cell) is { } seat)
            return seat;
        return Owes(plan) && PaymentOf(plan, peer) == null ? "items.notEnough" : null;
    }
}
