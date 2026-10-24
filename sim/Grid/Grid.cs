using System.Text.Json.Serialization;
using Stillwake.Content;

namespace Stillwake.Sim.Grid;

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
    /// <summary>Where anything may be built (0048): the map's zone, or the whole grid.</summary>
    [JsonInclude] internal Box Zone { get; set; }
    [JsonInclude, JsonConverter(typeof(Runs))] internal int[] Structure { get; set; } = [];
    [JsonInclude, JsonConverter(typeof(Runs))] internal int[] Material { get; set; } = [];
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
    /// <summary>Derived: every write to a layer or a machine's cells since the last load, as far back as it holds (0051).</summary>
    [JsonIgnore] internal ChangeLog Log { get; set; } = null!;
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
    /// The cells whose layers differ now from what they held at <paramref name="version"/>, ascending, each
    /// with the layers that do; a cell changed and changed back is not among them, and empty while
    /// <paramref name="version"/> is current. Null when the grid no longer knows: the log has let go of
    /// writes since then, or the version is not one this grid has had. A reader then looks at the whole
    /// grid again. Versions start over with a load, which brings a new grid: a reader keeps the grid it read too.
    /// </summary>
    IReadOnlyList<CellChange>? ChangedSince(int version);
    /// <summary>
    /// The smallest box holding every cell with anything in any layer or under a machine, <see cref="Box.None"/>
    /// when nothing is: every cell outside it is empty space. Kept tight by every change, never saved.
    /// </summary>
    Box Built { get; }
    /// <summary>The build zone (0048): nothing is placed outside it. The whole grid for a map that sets none.</summary>
    Box Zone { get; }
    ReadOnlySpan<int> StructureLayer { get; }
    /// <summary>Each cell's wall material code (<c>Materials</c>), 0 where no wall stands.</summary>
    ReadOnlySpan<int> MaterialLayer { get; }
    ReadOnlySpan<int> BackLayer { get; }
    /// <summary>Each cell's back wall material code, 0 where there is no back wall.</summary>
    ReadOnlySpan<int> BackMaterialLayer { get; }
    int StructureAt(int cell);
    /// <summary>Whether a body stops at <paramref name="cell"/> from every side (<see cref="Trait.Blocks"/>); past the grid, it does.</summary>
    bool Blocks(int cell);
    /// <summary>Whether <paramref name="cell"/> holds what stands on it from above (<see cref="Trait.Supports"/>).</summary>
    bool Supports(int cell);
    /// <summary>Whether a hero climbs <paramref name="cell"/> (<see cref="Trait.Climbable"/>).</summary>
    bool Climbable(int cell);
    /// <summary>Whether <paramref name="cell"/> is airtight (<see cref="Trait.Seals"/>).</summary>
    bool Seals(int cell);
    /// <summary>Whether <paramref name="cell"/> stops light (<see cref="Trait.BlocksLight"/>).</summary>
    bool BlocksLight(int cell);
    /// <summary>Whether <paramref name="cell"/> is stood on or stopped at: it blocks or supports; past the grid, it is.</summary>
    bool Solid(int cell);
    /// <summary>What the wall at <paramref name="cell"/> is made of; null for no wall, or off the grid.</summary>
    Material? WallAt(int cell);
    /// <summary>What the back wall behind <paramref name="cell"/> is made of; null for none, or off the grid.</summary>
    Material? BackAt(int cell);
    /// <summary>
    /// Whether the back wall behind <paramref name="cell"/> is the ground's own, as it grew from the seed (0077): false for
    /// none, for one a hero built, and on a surface that did not grow, the ship or a drawn map.
    /// </summary>
    bool GrownBack(int cell);
    Machine? MachineAt(int cell);
    /// <summary>Machine <paramref name="id"/>; null for none, or one taken apart.</summary>
    Machine? MachineById(int id);
    /// <summary>Every machine, in id order.</summary>
    IReadOnlyList<Machine> Machines { get; }
    /// <summary>The machines of <paramref name="kind"/>, in id order: what a system walks that works one kind.</summary>
    IReadOnlyList<Machine> MachinesOf(string kind);
    /// <summary>The cells where pipes meet machine <paramref name="m"/>, in the order its kind lists its ports.</summary>
    IReadOnlyList<int> PortsOf(Machine m);
    /// <summary>
    /// The cells a machine or a piece (a ladder, a door segment, 0104) of <paramref name="kind"/> covers with its top-left at
    /// <paramref name="cell"/>, ascending; null when it is neither or does not fit on the grid.
    /// </summary>
    IReadOnlyList<int>? Footprint(string kind, int cell);
    /// <summary>
    /// The cells of the piece the structure at <paramref name="cell"/> is part of, ascending, taken apart as one (0104): a ladder
    /// cell's or a door cell's two by two, its columns paired from the left end of its row's run of the same and its rows
    /// from the top of its column's; any other structure its own cell; none for none.
    /// </summary>
    IReadOnlyList<int> PieceOf(int cell);
    string? PlacementProblem(int cell, Placement placement);
    /// <summary>
    /// The structure codes of the cells the <see cref="SetDoor"/>s among <paramref name="toggles"/> would change, in order, each as its command would apply it and skipped
    /// where it would be refused: what a client shows of its own toggles before their tick runs (0014). Every
    /// other cell is as the world has it; a cell set back is listed at its own code. The world is untouched.
    /// </summary>
    IReadOnlyDictionary<int, int> AfterToggles(IReadOnlyList<Command> toggles);
}

/// <summary>How a module that depends on the grid may change it.</summary>
public interface IGridOps
{
    void Place(int cell, Placement placement);
    void Clear(int cell, string layer);
}

/// <summary>
/// Opens (<c>Open</c>) or shuts the doorway <c>Cell</c> is in: what its sender saw it should become, so two
/// peers opening one door at once leave it open, where a toggle would shut it again.
/// </summary>
public sealed record SetDoor(int Cell, bool Open) : Command;

/// <summary>The ship's cells in layers (packages/sim/src/modules/grid).</summary>
public sealed partial class GridModule : Module<GridState>, IGridQueries, IGridOps
{
    public override string Id => "grid";

    /// <summary>How many writes the change log holds; small in tests, so they reach past it.</summary>
    internal int LogCapacity { get; init; } = ChangeLog.DefaultCapacity;

    public GridModule()
    {
        _structureAt = c => State.Structure[c];
        _now = ValueAt;
        // Already as asked, a door is left alone, the version too: nothing derived from the grid is redone.
        Handles<SetDoor>(c => IsDoorway(State.Structure, c.Cell) ? null : "grid.noDoor", c =>
        {
            int to = DoorCode(c.Open, State.Structure[c.Cell]);
            var doorway = DoorwayOf(c.Cell, _structureAt);
            if (!Differs(State.Structure, doorway, to)) return;
            State.Version++;
            foreach (int cell in doorway) Set(State.Structure, GridLayers.Structure, cell, to);
        });
    }

    /// <summary>Every write to a layer goes through here, so the log misses none: it holds what the cell held before.</summary>
    private void Set(int[] layer, GridLayers which, int cell, int value)
    {
        int old = layer[cell];
        if (old == value) return;
        if (_grown && which != GridLayers.Machine) Touch(cell);
        State.Log.Add(State.Version, cell, which, old);
        layer[cell] = value;
    }

    /// <summary>What <paramref name="cell"/> holds in <paramref name="layer"/> now: the codes as saved, a machine by its id.</summary>
    private int ValueAt(int cell, GridLayers layer) => layer switch
    {
        GridLayers.Structure => State.Structure[cell],
        GridLayers.Material => State.Material[cell],
        GridLayers.Back => State.Back[cell],
        GridLayers.BackMaterial => State.BackMaterial[cell],
        _ => State.Occupancy[cell],
    };

    private readonly Func<int, GridLayers, int> _now;
    /// <summary>The last answer of <see cref="ChangedSince"/>, as the version asked and the version it was given at: the sim's readers all ask alike.</summary>
    private (int Since, int At, IReadOnlyList<CellChange>? Changes) _asked = (-1, -1, null);
    private static readonly CellChange[] NoChanges = [];

    public IReadOnlyList<CellChange>? ChangedSince(int version)
    {
        if (version == State.Version) return NoChanges;
        if (version > State.Version || version < 0) return null;
        if (_asked.Since == version && _asked.At == State.Version && _asked.Changes != null) return _asked.Changes;
        var changes = State.Log.Since(version, _now);
        _asked = (version, State.Version, changes);
        return changes;
    }

    protected override GridState Init(Layout layout)
    {
        (_grown, _touched) = (layout.Grown, []);
        _base.Clear();
        int size = layout.Width * layout.Height;
        var s = new GridState
        {
            Width = layout.Width,
            Height = layout.Height,
            Zone = layout.Zone ?? new Box(0, 0, layout.Width - 1, layout.Height - 1),
            Structure = new int[size],
            Material = new int[size],
            Back = new int[size],
            BackMaterial = new int[size],
        };
        int hull = Materials.Get(Materials.DefaultHull).Code;
        var drawn = layout.Materials;
        for (int cell = 0, y = 0; y < layout.Height; y++)
            for (int x = 0; x < layout.Width; x++, cell++)
            {
                char c = layout.Rows[y][x];
                // A wall and its back wall of the material the map draws there (0077), else of the default hull.
                int material = drawn != null && drawn[cell] != 0 ? drawn[cell] : hull;
                // Every cell but space has a back wall of the default hull, hull walls included; a solar panel,
                // making power with no fire, is drawn under the stars, which it needs; a planet's sky has none (0077).
                if (c != ' ' && (drawn == null || drawn[cell] > 0) && !(Legend.MachineOf(c) is { } kind && Content.Machines.Get(kind) is { Makes: not null, FuelSlot: null })) { s.Back[cell] = Back.Wall; s.BackMaterial[cell] = material; }
                s.Structure[cell] = c switch
                {
                    '#' => Structure.Wall,
                    '=' => Structure.Floor,
                    'H' => Structure.Ladder,
                    'D' => Structure.DoorClosed,
                    'd' => Structure.DoorOpen,
                    'h' => Structure.HatchClosed,
                    'o' => Structure.HatchOpen,
                    _ => Structure.Empty,
                };
                if (s.Structure[cell] == Structure.Wall) s.Material[cell] = material;
            }
        foreach (var (kind, cell) in layout.Machines()) s.Machines.Add(new Machine(s.NextMachineId++, kind, cell));
        return s;
    }

    /// <summary>A real door or hatch most of the time, any cell (off the grid too) otherwise, so refusals are drawn; opened or shut at random, so one already as asked is too.</summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        int size = State.Width * State.Height;
        int Pick(List<int> cells) =>
            cells.Count > 0 && rng.Below(4) != 0 ? cells[(int)rng.Below((uint)cells.Count)] : (int)rng.Below((uint)size + 2) - 1;
        return new SetDoor(Pick(CellsWith(State.Structure, Doorways.IsDoorway)), rng.Below(2) == 0) { From = from };
    }

    /// <summary>The cells holding a structure <paramref name="holds"/>, in reading order: within <see cref="Built"/>, where anything is.</summary>
    private List<int> CellsWith(int[] codes, Func<int, bool> holds)
    {
        var cells = new List<int>();
        var box = State.Built;
        for (int y = box.Top; y <= box.Bottom; y++)
            for (int c = y * State.Width + box.Left; c <= y * State.Width + box.Right; c++)
                if (holds(codes[c])) cells.Add(c);
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
        // Found in one sweep, row by row: a grown planet is built in nearly every cell.
        int left = State.Width, right = -1, top = State.Height, bottom = -1;
        for (int y = 0, c = 0; y < State.Height; y++)
            for (int x = 0; x < State.Width; x++, c++)
                if (IsBuilt(c))
                    (left, right, top, bottom) = (Math.Min(left, x), Math.Max(right, x), Math.Min(top, y), y);
        State.Built = right < 0 ? Box.None : new Box(left, top, right, bottom);
        State.Log = new ChangeLog(LogCapacity, State.Version);
        _asked = (-1, -1, null);
    }

    private bool IsBuilt(int c) =>
        State.Structure[c] != Structure.Empty || State.Back[c] != Back.None || State.Occupancy[c] != 0;

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
    public Box Zone => State.Zone;
    public ReadOnlySpan<int> StructureLayer => State.Structure;
    public ReadOnlySpan<int> MaterialLayer => State.Material;
    public ReadOnlySpan<int> BackLayer => State.Back;
    public ReadOnlySpan<int> BackMaterialLayer => State.BackMaterial;
    public IReadOnlyList<Machine> Machines => State.Machines;
    public Machine? MachineById(int id) => State.ById.GetValueOrDefault(id);

    public IReadOnlyList<Machine> MachinesOf(string kind) =>
        State.ByKind.TryGetValue(kind, out var machines) ? machines : NoMachines;

    private static readonly Machine[] NoMachines = [];

    public int StructureAt(int cell) => InBounds(cell) ? State.Structure[cell] : Structure.Empty;

    public bool Blocks(int cell) => !InBounds(cell) || Structures.Is(State.Structure[cell], Trait.Blocks);
    public bool Supports(int cell) => Has(cell, Trait.Supports);
    public bool Climbable(int cell) => Has(cell, Trait.Climbable);
    public bool Seals(int cell) => Has(cell, Trait.Seals);
    public bool BlocksLight(int cell) => Has(cell, Trait.BlocksLight);
    public bool Solid(int cell) => !InBounds(cell) || Structures.Solid(State.Structure[cell]);

    private bool Has(int cell, Trait trait) => InBounds(cell) && Structures.Is(State.Structure[cell], trait);

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
    private bool InZone(int cell) => State.Zone.Contains(cell % State.Width, cell / State.Width);
    private bool IsDoorway(int[] structure, int cell) => InBounds(cell) && Doorways.IsDoorway(structure[cell]);

    public IReadOnlyList<int>? Footprint(string kind, int cell)
    {
        var size = Content.Machines.TryGet(kind, out var spec) ? (spec.Width, spec.Height) : Builds.PieceOf(kind);
        if (size is not var (across, tall) || !InBounds(cell)) return null;
        int x = cell % State.Width, y = cell / State.Width;
        if (x + across > State.Width || y + tall > State.Height) return null;
        var cells = new List<int>();
        for (int dy = 0; dy < tall; dy++)
            for (int dx = 0; dx < across; dx++)
                cells.Add((y + dy) * State.Width + x + dx);
        return cells;
    }

    public IReadOnlyList<int> PieceOf(int cell)
    {
        if (!InBounds(cell)) return [];
        int s = State.Structure[cell];
        if (s == Structure.Empty) return [];
        bool door = Doorways.IsDoor(s), hatch = Doorways.IsHatch(s);
        if (s != Structure.Ladder && !door && !hatch) return [cell];
        // A ladder's piece, a door segment or a hatch piece (0104, 0111): columns paired from the left end of its row's run of the
        // same, rows from the top of its column's; a hatch is one row.
        int w = State.Width, x = cell % w, y = cell / w;
        bool Same(int cx, int cy) => cx >= 0 && cy >= 0 && cx < w && cy < State.Height
            && (door ? Doorways.IsDoor(State.Structure[cy * w + cx])
                : hatch ? Doorways.IsHatch(State.Structure[cy * w + cx])
                : State.Structure[cy * w + cx] == Structure.Ladder);
        int left = 0, up = 0;
        while (Same(x - left - 1, y)) left++;
        if (!hatch) while (Same(x, y - up - 1)) up++;
        var (across, tall) = Builds.PieceOf(door ? "door" : hatch ? Builds.Hatch : "ladder") ?? (1, 1);
        int x0 = x - left % across, y0 = y - up % tall;
        var cells = new List<int>(across * tall);
        for (int cy = y0; cy < y0 + tall; cy++)
            for (int cx = x0; cx < x0 + across; cx++)
                if (Same(cx, cy)) cells.Add(cy * w + cx);
        return cells;
    }

    public IReadOnlyDictionary<int, int> AfterToggles(IReadOnlyList<Command> toggles)
    {
        // Only the toggled cells: copying the layers cost a big world 16 MB a tick while a toggle was in flight.
        var structure = new Dictionary<int, int>();
        int StructureAt(int c) => structure.TryGetValue(c, out int code) ? code : State.Structure[c];
        foreach (var t in toggles)
        {
            if (t is SetDoor door && InBounds(door.Cell) && Doorways.IsDoorway(StructureAt(door.Cell)))
            {
                int to = DoorCode(door.Open, StructureAt(door.Cell));
                foreach (int cell in DoorwayOf(door.Cell, StructureAt)) structure[cell] = to;
            }
        }
        return structure;
    }

    /// <summary>The code <paramref name="structure"/> takes open or shut, a hatch staying a hatch and a door a door.</summary>
    private static int DoorCode(bool open, int structure) => Doorways.IsHatch(structure)
        ? open ? Structure.HatchOpen : Structure.HatchClosed
        : open ? Structure.DoorOpen : Structure.DoorClosed;

    private static bool Differs(int[] layer, List<int> cells, int code)
    {
        foreach (int cell in cells)
            if (layer[cell] != code) return true;
        return false;
    }

    /// <summary>
    /// The doorway <paramref name="cell"/> is in, the door or hatch it opens and shuts with (<see cref="Doorways"/>), as
    /// <paramref name="codeAt"/> reads it. Scratch: good until the next call.
    /// </summary>
    private List<int> DoorwayOf(int cell, Func<int, int> codeAt)
    {
        int w = State.Width, size = State.Structure.Length;
        if (_inDoorway.Length != size) _inDoorway = new bool[size];
        _doorway.Clear();
        Visit(codeAt, cell);
        while (_todo.Count > 0)
        {
            int at = _todo.Pop(), links = Doorways.LinksOf(at, w, size, codeAt);
            if ((links & Doorways.Up) != 0) Visit(codeAt, at - w);
            if ((links & Doorways.Down) != 0) Visit(codeAt, at + w);
            if ((links & Doorways.Left) != 0) Visit(codeAt, at - 1);
            if ((links & Doorways.Right) != 0) Visit(codeAt, at + 1);
        }
        foreach (int c in _doorway) _inDoorway[c] = false;
        return _doorway;
    }

    /// <summary>Scratch for <see cref="DoorwayOf"/>, a flag a cell, all false between calls; its cells to visit and the doorway found.</summary>
    private bool[] _inDoorway = [];
    private readonly Stack<int> _todo = new();
    private readonly List<int> _doorway = [];
    /// <summary>The live structure layer, made once so a door set allocates nothing.</summary>
    private readonly Func<int, int> _structureAt;

    private void Visit(Func<int, int> codeAt, int cell)
    {
        if (!InBounds(cell) || _inDoorway[cell] || !Doorways.IsDoorway(codeAt(cell))) return;
        _inDoorway[cell] = true;
        _doorway.Add(cell);
        _todo.Push(cell);
    }

    public string? PlacementProblem(int cell, Placement p)
    {
        if (!InBounds(cell)) return "grid.outOfBounds";
        if (!InZone(cell)) return "grid.outsideZone";
        switch (p.Layer)
        {
            case "structure":
                // A piece (a ladder, a door segment, 0104) needs every cell it covers as one cell needs its own.
                IReadOnlyList<int>? covers = Builds.PieceOf(p.Kind) == null ? [cell] : Footprint(p.Kind, cell);
                if (covers == null) return "grid.outOfBounds";
                if (!covers.All(InZone)) return "grid.outsideZone";
                if (covers.Any(c => State.Structure[c] != Structure.Empty)) return "grid.occupied";
                if (covers.Any(c => State.Occupancy[c] != 0)) return "grid.machineInTheWay";
                if (p.Kind == "wall" && !Materials.TryGet(p.Material ?? "", out _)) return "grid.noSuchMaterial";
                return null;
            case "back":
                if (State.Back[cell] != Back.None) return "grid.occupied";
                return Materials.TryGet(p.Material ?? "", out _) ? null : "grid.noSuchMaterial";
            case "machine":
                if (!Content.Machines.TryGet(p.Kind, out _)) return "grid.noSuchMachine";
                var cells = Footprint(p.Kind, cell);
                if (cells == null) return "grid.outOfBounds";
                if (!cells.All(InZone)) return "grid.outsideZone";
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
                int code = p.Kind switch
                {
                    "wall" => Structure.Wall,
                    "floor" => Structure.Floor,
                    "ladder" => Structure.Ladder,
                    Builds.Hatch => Structure.HatchClosed,
                    _ => Structure.DoorClosed,
                };
                if (Builds.PieceOf(p.Kind) != null)
                {
                    var piece = Footprint(p.Kind, cell)!;
                    foreach (int c in piece) Set(State.Structure, GridLayers.Structure, c, code);
                    Grow(piece[^1]);
                    return;
                }
                Set(State.Structure, GridLayers.Structure, cell, code);
                if (p.Kind == "wall") Set(State.Material, GridLayers.Material, cell, Materials.Get(p.Material!).Code);
                return;
            case "back":
                Set(State.Back, GridLayers.Back, cell, Back.Wall);
                Set(State.BackMaterial, GridLayers.BackMaterial, cell, Materials.Get(p.Material!).Code);
                return;
            case "machine":
                var added = new Machine(State.NextMachineId++, p.Kind, cell);
                State.Machines.Add(added);
                Listed(added);
                var footprint = Footprint(p.Kind, cell)!;
                foreach (int c in footprint) Set(State.Occupancy, GridLayers.Machine, c, added.Id);
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
            case "structure":
                Set(State.Structure, GridLayers.Structure, cell, Structure.Empty);
                Set(State.Material, GridLayers.Material, cell, 0);
                break;
            case "back":
                Set(State.Back, GridLayers.Back, cell, Back.None);
                Set(State.BackMaterial, GridLayers.BackMaterial, cell, 0);
                break;
            case "machine":
                if (!State.ById.Remove(State.Occupancy[cell], out var removed)) return;
                State.Machines.RemoveAt(IndexOf(State.Machines, removed.Id));
                var kind = State.ByKind[removed.Kind];
                kind.RemoveAt(IndexOf(kind, removed.Id));
                var footprint = Footprint(removed.Kind, removed.Cell) ?? [];
                foreach (int c in footprint) Set(State.Occupancy, GridLayers.Machine, c, 0);
                Shrink(footprint);
                return;
            default: return;
        }
        Shrink([cell]);
    }
}
