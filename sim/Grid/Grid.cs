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
    [JsonInclude, JsonConverter(typeof(Runs))] internal int[] Structure { get; set; } = [];
    [JsonInclude, JsonConverter(typeof(Runs))] internal int[] Material { get; set; } = [];
    [JsonInclude, JsonConverter(typeof(Runs))] internal int[] Conduit { get; set; } = [];
    [JsonInclude, JsonConverter(typeof(Runs))] internal int[] Back { get; set; } = [];
    [JsonInclude, JsonConverter(typeof(Runs))] internal int[] BackMaterial { get; set; } = [];
    [JsonInclude] internal List<Machine> Machines { get; set; } = [];
    [JsonInclude] internal int NextMachineId { get; set; } = 1;
    /// <summary>Derived: the id of the machine covering each cell, 0 for none.</summary>
    [JsonIgnore] internal int[] Occupancy { get; set; } = [];
    /// <summary>Derived: bumped on every change to a layer or a machine; 0 again after a load.</summary>
    [JsonIgnore] internal int Version { get; set; }
}

/// <summary>What any module may read of the grid. Arrays are live: a reader never writes them.</summary>
public interface IGridQueries
{
    int Width { get; }
    int Height { get; }
    /// <summary>
    /// Bumped on every change to any layer or machine, never saved: a module that derives from the
    /// grid looks again only when it moved. After a load it starts over, as do the modules.
    /// </summary>
    int Version { get; }
    ReadOnlySpan<int> StructureLayer { get; }
    /// <summary>Each cell's wall material code (<c>Materials</c>), 0 where no wall stands.</summary>
    ReadOnlySpan<int> MaterialLayer { get; }
    ReadOnlySpan<int> ConduitLayer { get; }
    ReadOnlySpan<int> BackLayer { get; }
    /// <summary>Each cell's back wall material code, 0 where there is no back wall.</summary>
    ReadOnlySpan<int> BackMaterialLayer { get; }
    int StructureAt(int cell);
    /// <summary>What the wall at <paramref name="cell"/> is made of; null for no wall, or off the grid.</summary>
    Material? WallAt(int cell);
    /// <summary>What the back wall behind <paramref name="cell"/> is made of; null for none, or off the grid.</summary>
    Material? BackAt(int cell);
    /// <summary>The conduit code at <paramref name="cell"/>: <see cref="Conduit.None"/> off the grid.</summary>
    int ConduitAt(int cell);
    Machine? MachineAt(int cell);
    IReadOnlyList<Machine> Machines { get; }
    /// <summary>The cells where conduits meet machine <paramref name="m"/>, in the order its kind lists its ports.</summary>
    IReadOnlyList<int> PortsOf(Machine m);
    /// <summary>The cells a machine of <paramref name="kind"/> covers with its top-left at <paramref name="cell"/>; null when it is no machine or does not fit on the grid.</summary>
    IReadOnlyList<int>? Footprint(string kind, int cell);
    string? PlacementProblem(int cell, Placement placement);
    /// <summary>
    /// The structure and conduit layers as they would be after the <see cref="ToggleDoor"/>s and
    /// <see cref="ToggleValve"/>s among <paramref name="toggles"/>, in order, each as its command would
    /// apply it and skipped where it would be refused: what a client shows of its own toggles before
    /// their tick runs (0014). Copies; the world is untouched.
    /// </summary>
    (int[] Structure, int[] Conduit) AfterToggles(IReadOnlyList<Command> toggles);
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
        Handles<ToggleDoor>(c => IsDoor(State.Structure, c.Cell) ? null : "grid.noDoor", c =>
        {
            State.Version++;
            ToggleDoorAt(State.Structure, c.Cell);
        });
        Handles<ToggleValve>(c => IsValve(State.Conduit, c.Cell) ? null : "grid.noValve", c =>
        {
            State.Version++;
            ToggleValveAt(State.Conduit, c.Cell);
        });
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
        int hull = Materials.Get(Materials.DefaultHull).Code;
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

    /// <summary>A real door or valve most of the time, any cell (off the grid too) otherwise, so refusals are drawn.</summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        int size = State.Width * State.Height;
        int Pick(List<int> cells) =>
            cells.Count > 0 && rng.Below(4) != 0 ? cells[(int)rng.Below((uint)cells.Count)] : (int)rng.Below((uint)size + 2) - 1;
        if (rng.Below(2) == 0)
            return new ToggleDoor(Pick(CellsWith(State.Structure, Structure.DoorClosed, Structure.DoorOpen))) { From = from };
        return new ToggleValve(Pick(CellsWith(State.Conduit, Conduit.ValveOpen, Conduit.ValveClosed))) { From = from };
    }

    private static List<int> CellsWith(int[] codes, int a, int b)
    {
        var cells = new List<int>();
        for (int c = 0; c < codes.Length; c++) if (codes[c] == a || codes[c] == b) cells.Add(c);
        return cells;
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
    public int Version => State.Version;
    public ReadOnlySpan<int> StructureLayer => State.Structure;
    public ReadOnlySpan<int> MaterialLayer => State.Material;
    public ReadOnlySpan<int> ConduitLayer => State.Conduit;
    public ReadOnlySpan<int> BackLayer => State.Back;
    public ReadOnlySpan<int> BackMaterialLayer => State.BackMaterial;
    public IReadOnlyList<Machine> Machines => State.Machines;
    public int StructureAt(int cell) => InBounds(cell) ? State.Structure[cell] : Structure.Empty;
    public int ConduitAt(int cell) => InBounds(cell) ? State.Conduit[cell] : Conduit.None;

    public Material? WallAt(int cell) =>
        InBounds(cell) && State.Structure[cell] == Structure.Wall ? MaterialOf(State.Material[cell]) : null;

    public Material? BackAt(int cell) =>
        InBounds(cell) && State.Back[cell] == Back.Wall ? MaterialOf(State.BackMaterial[cell]) : null;

    private static readonly Material?[] ByCode = Enumerable.Range(0, Materials.All.Max(m => m.Code) + 1)
        .Select(code => Materials.All.FirstOrDefault(m => m.Code == code)).ToArray();

    /// <summary>The material stored as <paramref name="code"/>; the default hull for a code none has, as TypeScript reads it.</summary>
    private static Material MaterialOf(int code) =>
        (code >= 0 && code < ByCode.Length ? ByCode[code] : null) ?? Materials.Get(Materials.DefaultHull);

    public Machine? MachineAt(int cell)
    {
        int id = InBounds(cell) ? State.Occupancy[cell] : 0;
        return id == 0 ? null : State.Machines.Find(m => m.Id == id);
    }

    public IReadOnlyList<int> PortsOf(Machine m)
    {
        int x = m.Cell % State.Width, y = m.Cell / State.Width;
        return Content.Machines.Get(m.Kind).Ports.Select(p => (y + p.Y) * State.Width + x + p.X).ToArray();
    }

    private bool InBounds(int cell) => cell >= 0 && cell < State.Width * State.Height;
    private bool IsDoor(int[] structure, int cell) => InBounds(cell) && structure[cell] is Structure.DoorClosed or Structure.DoorOpen;
    private bool IsValve(int[] conduit, int cell) => InBounds(cell) && conduit[cell] is Conduit.ValveOpen or Conduit.ValveClosed;

    public IReadOnlyList<int>? Footprint(string kind, int cell)
    {
        if (!Content.Machines.TryGet(kind, out var spec) || !InBounds(cell)) return null;
        int x = cell % State.Width, y = cell / State.Width;
        if (x + spec.Width > State.Width || y + spec.Height > State.Height) return null;
        var cells = new List<int>();
        for (int dy = 0; dy < spec.Height; dy++)
            for (int dx = 0; dx < spec.Width; dx++)
                cells.Add((y + dy) * State.Width + x + dx);
        return cells;
    }

    public (int[] Structure, int[] Conduit) AfterToggles(IReadOnlyList<Command> toggles)
    {
        int[] structure = [.. State.Structure], conduit = [.. State.Conduit];
        foreach (var t in toggles)
        {
            if (t is ToggleDoor door && IsDoor(structure, door.Cell)) ToggleDoorAt(structure, door.Cell);
            if (t is ToggleValve valve && IsValve(conduit, valve.Cell)) ToggleValveAt(conduit, valve.Cell);
        }
        return (structure, conduit);
    }

    private static void ToggleValveAt(int[] conduit, int cell) =>
        conduit[cell] = conduit[cell] == Conduit.ValveOpen ? Conduit.ValveClosed : Conduit.ValveOpen;

    /// <summary>A doorway is every door cell joined to the clicked one in <paramref name="structure"/>: opened or closed as one.</summary>
    private void ToggleDoorAt(int[] structure, int cell)
    {
        int to = structure[cell] == Structure.DoorOpen ? Structure.DoorClosed : Structure.DoorOpen;
        var todo = new Stack<int>([cell]);
        var found = new bool[structure.Length];
        found[cell] = true;
        while (todo.Count > 0)
        {
            int at = todo.Pop(), x = at % State.Width;
            structure[at] = to;
            foreach (int n in (int[])[at - State.Width, at + State.Width, x > 0 ? at - 1 : -1, x < State.Width - 1 ? at + 1 : -1])
            {
                if (!InBounds(n) || found[n] || !IsDoor(structure, n)) continue;
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
                if (p.Kind == "wall" && !Materials.TryGet(p.Material ?? "", out _)) return "grid.noSuchMaterial";
                return null;
            case "conduit":
                return State.Conduit[cell] == Conduit.None ? null : "grid.occupied";
            case "back":
                if (State.Back[cell] != Back.None) return "grid.occupied";
                return Materials.TryGet(p.Material ?? "", out _) ? null : "grid.noSuchMaterial";
            case "machine":
                if (!Content.Machines.TryGet(p.Kind, out _)) return "grid.noSuchMachine";
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
        State.Version++;
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
                if (p.Kind == "wall") State.Material[cell] = Materials.Get(p.Material!).Code;
                return;
            case "conduit":
                State.Conduit[cell] = p.Kind == "pipe" ? Conduit.Pipe : Conduit.ValveOpen;
                return;
            case "back":
                State.Back[cell] = Back.Wall;
                State.BackMaterial[cell] = Materials.Get(p.Material!).Code;
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
        State.Version++;
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
