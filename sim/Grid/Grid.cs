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

/// <summary>A rectangle of cells, its edges included; <see cref="None"/> holds none.</summary>
public readonly record struct Box(int Left, int Top, int Right, int Bottom)
{
    public static readonly Box None = new(0, 0, -1, -1);
    public bool IsEmpty => Right < Left || Bottom < Top;
    public int Width => IsEmpty ? 0 : Right - Left + 1;
    public int Height => IsEmpty ? 0 : Bottom - Top + 1;
    public bool Contains(int x, int y) => x >= Left && x <= Right && y >= Top && y <= Bottom;

    /// <summary>The smallest box holding both.</summary>
    public Box Union(Box other) =>
        IsEmpty ? other
        : other.IsEmpty ? this
        : new(Math.Min(Left, other.Left), Math.Min(Top, other.Top), Math.Max(Right, other.Right), Math.Max(Bottom, other.Bottom));
}

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
    /// <summary>Every machine, in id order: ids only grow, and a new one goes last.</summary>
    [JsonInclude] internal List<Machine> Machines { get; set; } = [];
    [JsonInclude] internal int NextMachineId { get; set; } = 1;
    /// <summary>Derived: the id of the machine covering each cell, 0 for none.</summary>
    [JsonIgnore] internal int[] Occupancy { get; set; } = [];
    /// <summary>Derived: every machine by its id; only looked up.</summary>
    [JsonIgnore] internal Dictionary<int, Machine> ById { get; set; } = new();
    /// <summary>Derived: the machines of each kind, in id order; only looked up.</summary>
    [JsonIgnore] internal Dictionary<string, List<Machine>> ByKind { get; set; } = new(StringComparer.Ordinal);
    /// <summary>Derived: bumped on every change to a layer or a machine; 0 again after a load.</summary>
    [JsonIgnore] internal int Version { get; set; }
    /// <summary>Derived: the smallest box holding every cell with anything in it, kept tight by every change.</summary>
    [JsonIgnore] internal Box Built { get; set; } = Box.None;
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
    /// <summary>
    /// The smallest box holding every cell with anything in any layer or under a machine, <see cref="Box.None"/>
    /// when nothing is: every cell outside it is empty space. Kept tight by every change, never saved.
    /// </summary>
    Box Built { get; }
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
    /// <summary>Machine <paramref name="id"/>; null for none, or one taken apart.</summary>
    Machine? MachineById(int id);
    /// <summary>Every machine, in id order.</summary>
    IReadOnlyList<Machine> Machines { get; }
    /// <summary>The machines of <paramref name="kind"/>, in id order: what a system walks that works one kind.</summary>
    IReadOnlyList<Machine> MachinesOf(string kind);
    /// <summary>The cells where conduits meet machine <paramref name="m"/>, in the order its kind lists its ports.</summary>
    IReadOnlyList<int> PortsOf(Machine m);
    /// <summary>The cells a machine of <paramref name="kind"/> covers with its top-left at <paramref name="cell"/>; null when it is no machine or does not fit on the grid.</summary>
    IReadOnlyList<int>? Footprint(string kind, int cell);
    string? PlacementProblem(int cell, Placement placement);
    /// <summary>
    /// The structure and conduit codes of the cells the <see cref="ToggleDoor"/>s and <see cref="ToggleValve"/>s
    /// among <paramref name="toggles"/> would change, in order, each as its command would apply it and skipped
    /// where it would be refused: what a client shows of its own toggles before their tick runs (0014). Every
    /// other cell is as the world has it; a cell toggled back is listed at its own code. The world is untouched.
    /// </summary>
    (IReadOnlyDictionary<int, int> Structure, IReadOnlyDictionary<int, int> Conduit) AfterToggles(IReadOnlyList<Command> toggles);
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
        _structureAt = c => State.Structure[c];
        _setStructure = (c, code) => State.Structure[c] = code;
        Handles<ToggleDoor>(c => IsDoor(State.Structure, c.Cell) ? null : "grid.noDoor", c =>
        {
            State.Version++;
            ToggleDoorAt(c.Cell, _structureAt, _setStructure);
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
            // Every cell but space has a back wall of the default hull, hull walls included; a solar panel,
            // making power with no fire, is drawn under the stars, which it needs.
            if (c != ' ' && !(Legend.MachineOf(c) is { } kind && Content.Machines.Get(kind) is { Makes: not null, FuelSlot: null })) { s.Back[cell] = Back.Wall; s.BackMaterial[cell] = hull; }
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
        State.ById = new Dictionary<int, Machine>(State.Machines.Count);
        State.ByKind = new Dictionary<string, List<Machine>>(StringComparer.Ordinal);
        foreach (var m in State.Machines)
        {
            foreach (int c in Footprint(m.Kind, m.Cell) ?? [])
                State.Occupancy[c] = m.Id;
            Listed(m);
        }
        State.Built = Box.None;
        for (int c = 0; c < State.Occupancy.Length; c++)
            if (IsBuilt(c)) Grow(c);
    }

    private bool IsBuilt(int c) =>
        State.Structure[c] != Structure.Empty || State.Conduit[c] != Conduit.None || State.Back[c] != Back.None || State.Occupancy[c] != 0;

    private void Grow(int c)
    {
        int x = c % State.Width, y = c / State.Width;
        State.Built = State.Built.Union(new Box(x, y, x, y));
    }

    /// <summary>After <paramref name="cleared"/> were emptied, the built box moves in past each edge line left empty.</summary>
    private void Shrink(IReadOnlyList<int> cleared)
    {
        var b = State.Built;
        bool onEdge = false;
        foreach (int c in cleared)
        {
            int x = c % State.Width, y = c / State.Width;
            onEdge |= x == b.Left || x == b.Right || y == b.Top || y == b.Bottom;
        }
        if (!onEdge) return;
        while (!b.IsEmpty && RowEmpty(b.Top, b)) b = b with { Top = b.Top + 1 };
        while (!b.IsEmpty && RowEmpty(b.Bottom, b)) b = b with { Bottom = b.Bottom - 1 };
        while (!b.IsEmpty && ColumnEmpty(b.Left, b)) b = b with { Left = b.Left + 1 };
        while (!b.IsEmpty && ColumnEmpty(b.Right, b)) b = b with { Right = b.Right - 1 };
        State.Built = b.IsEmpty ? Box.None : b;
    }

    private bool RowEmpty(int y, Box b)
    {
        for (int x = b.Left; x <= b.Right; x++) if (IsBuilt(y * State.Width + x)) return false;
        return true;
    }

    private bool ColumnEmpty(int x, Box b)
    {
        for (int y = b.Top; y <= b.Bottom; y++) if (IsBuilt(y * State.Width + x)) return false;
        return true;
    }

    /// <summary>Puts <paramref name="m"/> into the derived lookups; it is the highest id yet, so its kind's list stays in id order.</summary>
    private void Listed(Machine m)
    {
        State.ById.Add(m.Id, m);
        if (State.ByKind.TryGetValue(m.Kind, out var kind)) kind.Add(m);
        else State.ByKind[m.Kind] = [m];
    }

    /// <summary>Where machine <paramref name="id"/> is in <paramref name="machines"/>, a list in id order; the complement of where it would go when absent.</summary>
    private static int IndexOf(List<Machine> machines, int id)
    {
        int lo = 0, hi = machines.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int at = machines[mid].Id;
            if (at == id) return mid;
            if (at < id) lo = mid + 1;
            else hi = mid - 1;
        }
        return ~lo;
    }

    public int Width => State.Width;
    public int Height => State.Height;
    public int Version => State.Version;
    public Box Built => State.Built;
    public ReadOnlySpan<int> StructureLayer => State.Structure;
    public ReadOnlySpan<int> MaterialLayer => State.Material;
    public ReadOnlySpan<int> ConduitLayer => State.Conduit;
    public ReadOnlySpan<int> BackLayer => State.Back;
    public ReadOnlySpan<int> BackMaterialLayer => State.BackMaterial;
    public IReadOnlyList<Machine> Machines => State.Machines;
    public Machine? MachineById(int id) => State.ById.GetValueOrDefault(id);

    public IReadOnlyList<Machine> MachinesOf(string kind) =>
        State.ByKind.TryGetValue(kind, out var machines) ? machines : NoMachines;

    private static readonly Machine[] NoMachines = [];

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
        return id == 0 ? null : State.ById[id];
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

    public (IReadOnlyDictionary<int, int> Structure, IReadOnlyDictionary<int, int> Conduit) AfterToggles(IReadOnlyList<Command> toggles)
    {
        // Only the toggled cells: copying the layers cost a big world 16 MB a tick while a toggle was in flight.
        var structure = new Dictionary<int, int>();
        var conduit = new Dictionary<int, int>();
        int StructureAt(int c) => structure.TryGetValue(c, out int code) ? code : State.Structure[c];
        foreach (var t in toggles)
        {
            if (t is ToggleDoor door && InBounds(door.Cell) && StructureAt(door.Cell) is Structure.DoorClosed or Structure.DoorOpen)
                ToggleDoorAt(door.Cell, StructureAt, (c, code) => structure[c] = code);
            if (t is ToggleValve valve && InBounds(valve.Cell))
            {
                int code = conduit.TryGetValue(valve.Cell, out int shown) ? shown : State.Conduit[valve.Cell];
                if (code is Conduit.ValveOpen or Conduit.ValveClosed) conduit[valve.Cell] = Toggled(code);
            }
        }
        return (structure, conduit);
    }

    private static void ToggleValveAt(int[] conduit, int cell) => conduit[cell] = Toggled(conduit[cell]);

    private static int Toggled(int valve) => valve == Conduit.ValveOpen ? Conduit.ValveClosed : Conduit.ValveOpen;

    /// <summary>
    /// A doorway is every door cell joined to the clicked one, as <paramref name="codeAt"/> reads them: opened or
    /// closed as one, each cell given its new code through <paramref name="set"/>.
    /// </summary>
    private void ToggleDoorAt(int cell, Func<int, int> codeAt, Action<int, int> set)
    {
        int to = codeAt(cell) == Structure.DoorOpen ? Structure.DoorClosed : Structure.DoorOpen;
        if (_inDoorway.Length != State.Structure.Length) _inDoorway = new bool[State.Structure.Length];
        Visit(codeAt, cell);
        while (_todo.Count > 0)
        {
            int at = _todo.Pop(), x = at % State.Width;
            set(at, to);
            Visit(codeAt, at - State.Width);
            Visit(codeAt, at + State.Width);
            if (x > 0) Visit(codeAt, at - 1);
            if (x < State.Width - 1) Visit(codeAt, at + 1);
        }
        foreach (int c in _visited) _inDoorway[c] = false;
        _visited.Clear();
    }

    /// <summary>Scratch for <see cref="ToggleDoorAt"/>, a flag a cell, all false between calls; and its cells to visit and visited.</summary>
    private bool[] _inDoorway = [];
    private readonly Stack<int> _todo = new();
    private readonly List<int> _visited = [];
    /// <summary>The live structure layer read and written, made once so a door toggle allocates nothing.</summary>
    private readonly Func<int, int> _structureAt;
    private readonly Action<int, int> _setStructure;

    private void Visit(Func<int, int> codeAt, int cell)
    {
        if (!InBounds(cell) || _inDoorway[cell] || codeAt(cell) is not (Structure.DoorClosed or Structure.DoorOpen)) return;
        _inDoorway[cell] = true;
        _visited.Add(cell);
        _todo.Push(cell);
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
        Grow(cell);
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
                var added = new Machine(State.NextMachineId++, p.Kind, cell);
                State.Machines.Add(added);
                Listed(added);
                var footprint = Footprint(p.Kind, cell)!;
                foreach (int c in footprint) State.Occupancy[c] = added.Id;
                Grow(footprint[^1]);
                return;
        }
    }

    public void Clear(int cell, string layer)
    {
        if (!InBounds(cell)) throw new InvalidOperationException($"grid.clear at {cell}: out of bounds");
        State.Version++;
        switch (layer)
        {
            case "structure": State.Structure[cell] = Structure.Empty; State.Material[cell] = 0; break;
            case "conduit": State.Conduit[cell] = Conduit.None; break;
            case "back": State.Back[cell] = Back.None; State.BackMaterial[cell] = 0; break;
            case "machine":
                if (!State.ById.Remove(State.Occupancy[cell], out var removed)) return;
                State.Machines.RemoveAt(IndexOf(State.Machines, removed.Id));
                var kind = State.ByKind[removed.Kind];
                kind.RemoveAt(IndexOf(kind, removed.Id));
                var footprint = Footprint(removed.Kind, removed.Cell) ?? [];
                foreach (int c in footprint) State.Occupancy[c] = 0;
                Shrink(footprint);
                return;
            default: return;
        }
        Shrink([cell]);
    }
}
