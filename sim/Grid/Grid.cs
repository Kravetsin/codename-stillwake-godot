using System.Text.Json.Serialization;
using Stillwake.Content;

namespace Stillwake.Sim.Grid;

/// <summary>Structure codes as stored in saves: never reuse or renumber one.</summary>
public static class Structure
{
    public const int Empty = 0, Wall = 1, Floor = 2, Ladder = 3, DoorClosed = 4, DoorOpen = 5;
}

/// <summary>Conduit codes as stored in saves.</summary>
public static class Conduit
{
    public const int None = 0, Pipe = 1, ValveOpen = 2, ValveClosed = 3;
}

/// <summary>Back wall codes as stored in saves.</summary>
public static class Back
{
    public const int None = 0, Wall = 1;
}

public sealed record Machine(int Id, string Kind, int Cell);

/// <summary>What may be put in a cell: a layer, a kind, and a material for walls and back walls.</summary>
public sealed record Placement(string Layer, string Kind, string? Material = null)
{
    public static Placement Wall(string material = "scrap") => new("structure", "wall", material);
    public static Placement BackWall(string material = "scrap") => new("back", "backWall", material);
}

/// <summary>
/// The grid's slice. Public so modules can be typed by it, its members internal, so no other module
/// can read or write them: they go through <see cref="IGridQueries"/> and <see cref="IGridOps"/>.
/// </summary>
public sealed class GridState
{
    [JsonInclude] internal int Width { get; set; }
    [JsonInclude] internal int Height { get; set; }
    [JsonInclude] internal int[] Structure { get; set; } = [];
    [JsonInclude] internal int[] Material { get; set; } = [];
    [JsonInclude] internal int[] Conduit { get; set; } = [];
    [JsonInclude] internal int[] Back { get; set; } = [];
    [JsonInclude] internal int[] BackMaterial { get; set; } = [];
    [JsonInclude] internal List<Machine> Machines { get; set; } = [];
    [JsonInclude] internal int NextMachineId { get; set; } = 1;
    /// <summary>Derived: the id of the machine covering each cell, 0 for none.</summary>
    [JsonIgnore] internal int[] Occupancy { get; set; } = [];
}

/// <summary>What any module may read of the grid. Arrays are live: a reader never writes them.</summary>
public interface IGridQueries
{
    int Width { get; }
    int Height { get; }
    ReadOnlySpan<int> StructureLayer { get; }
    ReadOnlySpan<int> BackLayer { get; }
    int StructureAt(int cell);
    Machine? MachineAt(int cell);
    IReadOnlyList<Machine> Machines { get; }
    string? PlacementProblem(int cell, Placement placement);
}

/// <summary>How a module that depends on the grid may change it.</summary>
public interface IGridOps
{
    void Place(int cell, Placement placement);
    void Clear(int cell, string layer);
}

public sealed record ToggleDoor(int Cell) : Command;
public sealed record ToggleValve(int Cell) : Command;

/// <summary>The ship's cells in layers (packages/sim/src/modules/grid).</summary>
public sealed class GridModule : Module<GridState>, IGridQueries, IGridOps
{
    public override string Id => "grid";

    public GridModule()
    {
        Handles<ToggleDoor>(c => IsDoor(c.Cell) ? null : "grid.noDoor", c => ToggleDoorAt(c.Cell));
        Handles<ToggleValve>(c => IsValve(c.Cell) ? null : "grid.noValve", c =>
            State.Conduit[c.Cell] = State.Conduit[c.Cell] == Conduit.ValveOpen ? Conduit.ValveClosed : Conduit.ValveOpen);
    }

    protected override GridState Init(Layout layout)
    {
        int size = layout.Width * layout.Height;
        var s = new GridState
        {
            Width = layout.Width,
            Height = layout.Height,
            Structure = new int[size],
            Material = new int[size],
            Conduit = new int[size],
            Back = new int[size],
            BackMaterial = new int[size],
        };
        int hull = Data.MaterialById(Data.DefaultHull)!.Code;
        for (int cell = 0; cell < size; cell++)
        {
            char c = layout.At(cell);
            // Every cell but space has a back wall of the default hull, hull walls included.
            if (c != ' ') { s.Back[cell] = Back.Wall; s.BackMaterial[cell] = hull; }
            s.Structure[cell] = c switch
            {
                '#' => Structure.Wall,
                '=' => Structure.Floor,
                'H' => Structure.Ladder,
                'D' => Structure.DoorClosed,
                'd' => Structure.DoorOpen,
                _ => Structure.Empty,
            };
            if (s.Structure[cell] == Structure.Wall) s.Material[cell] = hull;
            s.Conduit[cell] = c switch
            {
                '-' => Conduit.Pipe,
                'v' => Conduit.ValveOpen,
                'x' => Conduit.ValveClosed,
                _ => Conduit.None,
            };
        }
        foreach (int cell in layout.CellsOf('-', under: true)) s.Conduit[cell] = Conduit.Pipe;
        foreach (var (kind, cell) in layout.Machines()) s.Machines.Add(new Machine(s.NextMachineId++, kind, cell));
        return s;
    }

    public override void Rebuild()
    {
        State.Occupancy = new int[State.Width * State.Height];
        foreach (var m in State.Machines)
            foreach (int c in Footprint(m.Kind, m.Cell) ?? [])
                State.Occupancy[c] = m.Id;
    }

    public int Width => State.Width;
    public int Height => State.Height;
    public ReadOnlySpan<int> StructureLayer => State.Structure;
    public ReadOnlySpan<int> BackLayer => State.Back;
    public IReadOnlyList<Machine> Machines => State.Machines;
    public int StructureAt(int cell) => InBounds(cell) ? State.Structure[cell] : Structure.Empty;

    public Machine? MachineAt(int cell)
    {
        int id = InBounds(cell) ? State.Occupancy[cell] : 0;
        return id == 0 ? null : State.Machines.Find(m => m.Id == id);
    }

    private bool InBounds(int cell) => cell >= 0 && cell < State.Width * State.Height;
    private bool IsDoor(int cell) => StructureAt(cell) is Structure.DoorClosed or Structure.DoorOpen;
    private bool IsValve(int cell) => InBounds(cell) && State.Conduit[cell] is Conduit.ValveOpen or Conduit.ValveClosed;

    /// <summary>The cells a machine of <paramref name="kind"/> covers with its top-left at <paramref name="cell"/>.</summary>
    private List<int>? Footprint(string kind, int cell)
    {
        var spec = Data.Machine(kind);
        if (spec == null || !InBounds(cell)) return null;
        int x = cell % State.Width, y = cell / State.Width;
        if (x + spec.Width > State.Width || y + spec.Height > State.Height) return null;
        var cells = new List<int>();
        for (int dy = 0; dy < spec.Height; dy++)
            for (int dx = 0; dx < spec.Width; dx++)
                cells.Add((y + dy) * State.Width + x + dx);
        return cells;
    }

    /// <summary>A doorway is every door cell joined to the clicked one: opened or closed as one.</summary>
    private void ToggleDoorAt(int cell)
    {
        int to = State.Structure[cell] == Structure.DoorOpen ? Structure.DoorClosed : Structure.DoorOpen;
        var todo = new Stack<int>([cell]);
        var found = new bool[State.Structure.Length];
        found[cell] = true;
        while (todo.Count > 0)
        {
            int at = todo.Pop(), x = at % State.Width;
            State.Structure[at] = to;
            foreach (int n in (int[])[at - State.Width, at + State.Width, x > 0 ? at - 1 : -1, x < State.Width - 1 ? at + 1 : -1])
            {
                if (!InBounds(n) || found[n] || !IsDoor(n)) continue;
                found[n] = true;
                todo.Push(n);
            }
        }
    }

    public string? PlacementProblem(int cell, Placement p)
    {
        if (!InBounds(cell)) return "grid.outOfBounds";
        switch (p.Layer)
        {
            case "structure":
                if (State.Structure[cell] != Structure.Empty) return "grid.occupied";
                if (State.Occupancy[cell] != 0) return "grid.machineInTheWay";
                if (p.Kind == "wall" && Data.MaterialById(p.Material ?? "") == null) return "grid.noSuchMaterial";
                return null;
            case "conduit":
                return State.Conduit[cell] == Conduit.None ? null : "grid.occupied";
            case "back":
                if (State.Back[cell] != Back.None) return "grid.occupied";
                return Data.MaterialById(p.Material ?? "") == null ? "grid.noSuchMaterial" : null;
            case "machine":
                if (Data.Machine(p.Kind) == null) return "grid.noSuchMachine";
                var cells = Footprint(p.Kind, cell);
                if (cells == null) return "grid.outOfBounds";
                if (cells.Any(c => State.Structure[c] != Structure.Empty)) return "grid.blocked";
                if (cells.Any(c => State.Occupancy[c] != 0)) return "grid.machineInTheWay";
                return null;
            default:
                return "grid.noSuchLayer";
        }
    }

    public void Place(int cell, Placement p)
    {
        var problem = PlacementProblem(cell, p);
        if (problem != null) throw new InvalidOperationException($"grid.place at {cell}: {problem}");
        switch (p.Layer)
        {
            case "structure":
                State.Structure[cell] = p.Kind switch
                {
                    "wall" => Structure.Wall,
                    "floor" => Structure.Floor,
                    "ladder" => Structure.Ladder,
                    _ => Structure.DoorClosed,
                };
                if (p.Kind == "wall") State.Material[cell] = Data.MaterialById(p.Material!)!.Code;
                return;
            case "conduit":
                State.Conduit[cell] = p.Kind == "pipe" ? Conduit.Pipe : Conduit.ValveOpen;
                return;
            case "back":
                State.Back[cell] = Back.Wall;
                State.BackMaterial[cell] = Data.MaterialById(p.Material!)!.Code;
                return;
            case "machine":
                int id = State.NextMachineId++;
                State.Machines.Add(new Machine(id, p.Kind, cell));
                foreach (int c in Footprint(p.Kind, cell)!) State.Occupancy[c] = id;
                return;
        }
    }

    public void Clear(int cell, string layer)
    {
        if (!InBounds(cell)) throw new InvalidOperationException($"grid.clear at {cell}: out of bounds");
        switch (layer)
        {
            case "structure": State.Structure[cell] = Structure.Empty; State.Material[cell] = 0; return;
            case "conduit": State.Conduit[cell] = Conduit.None; return;
            case "back": State.Back[cell] = Back.None; State.BackMaterial[cell] = 0; return;
            case "machine":
                int at = State.Machines.FindIndex(m => m.Id == State.Occupancy[cell]);
                if (at < 0) return;
                var removed = State.Machines[at];
                State.Machines.RemoveAt(at);
                foreach (int c in Footprint(removed.Kind, removed.Cell) ?? []) State.Occupancy[c] = 0;
                return;
        }
    }
}
