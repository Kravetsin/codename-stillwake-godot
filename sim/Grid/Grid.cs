using System.Numerics;
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
    /// <summary>The box a vessel was written into (generated-vessels): no one builds in it; null for none.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] internal Box? Reserved { get; set; }
    /// <summary>Derived: the id of the machine covering each cell, 0 for none.</summary>
    [JsonIgnore] internal int[] Occupancy { get; set; } = [];
    /// <summary>Derived: every machine by its id; only looked up.</summary>
    [JsonIgnore] internal Dictionary<int, Machine> ById { get; set; } = new();
    /// <summary>Derived: the machines of each kind, in id order; only looked up.</summary>
    [JsonIgnore] internal Dictionary<string, List<Machine>> ByKind { get; set; } = new(StringComparer.Ordinal);
    /// <summary>Derived: the machines with each trait, by the trait's bit, in id order; only looked up.</summary>
    [JsonIgnore] internal List<Machine>[] ByTrait { get; set; } = GridModule.NoneByTrait();
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
    /// <summary>The build zone (0048): nothing is placed outside it but by <see cref="IGridOps.Lay"/>. The whole grid for a map that sets none.</summary>
    Box Zone { get; }
    /// <summary>The box a vessel was written into (generated-vessels), where nothing is built; null for none.</summary>
    Box? Reserved { get; }
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
    /// <summary>The machines of <paramref name="kind"/>, in id order: what a system walks that works one kind's own rule.</summary>
    IReadOnlyList<Machine> MachinesOf(string kind);
    /// <summary>The machines with <paramref name="trait"/>, one trait, in id order: what a system walks that works a trait (0113).</summary>
    IReadOnlyList<Machine> MachinesWith(MachineTrait trait);
    /// <summary>The cells where pipes meet machine <paramref name="m"/>, in the order its kind lists its ports.</summary>
    IReadOnlyList<int> PortsOf(Machine m);
    /// <summary>The port at <paramref name="cell"/>, its way and fluid, if a machine has one there; null otherwise.</summary>
    Port? PortAt(int cell);
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
    /// <summary>Whether a machine of <paramref name="kind"/> from <paramref name="cell"/> stands over a launch platform's run, every cell under it a platform's, shut or open.</summary>
    bool OnRun(string kind, int cell);
    /// <summary>Whether something solid stands at <paramref name="cell"/>, a structure or a machine: what a platform's shaft must be empty of.</summary>
    bool Taken(int cell);
    /// <summary>Whether every cell under <paramref name="cell"/> in its column, to the grid's edge, is empty of anything solid: a platform's shaft.</summary>
    bool ClearBelow(int cell);
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
    /// <summary>Sets the box a vessel is written into, or none (generated-vessels). Only Places calls it.</summary>
    void Reserve(Box? box);
    /// <summary>
    /// <see cref="Place"/> inside the reserved box, the zone not asked: how a vessel is written. A cell or footprint outside the
    /// box, or any other problem, throws. Only Places calls it.
    /// </summary>
    void Lay(int cell, Placement placement);
    /// <summary>
    /// Opens or shuts the doorway <paramref name="cell"/> is in as <see cref="SetDoor"/> does: a launch platform's, which only its
    /// module opens, with power (Landing). The caller checks it is a doorway.
    /// </summary>
    void SetDoorway(int cell, bool open);
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
        // A launch platform is no door to open by hand: it opens with power, by its own module's command (Landing).
        Handles<SetDoor>(c => IsDoorway(State.Structure, c.Cell) && !Doorways.IsPlatform(State.Structure[c.Cell]) ? null : "grid.noDoor",
            c => SetDoorway(c.Cell, c.Open));
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

    /// <summary>
    /// What each map character means, found once rather than per cell, for a planet's map is millions of cells: its structure,
    /// and whether it is drawn bare of a back wall — a solar panel, making power with no fire, under the stars it needs.
    /// </summary>
    private static readonly (int Structure, bool Bare)[] Looks = Enumerable.Range(0, char.MaxValue + 1)
        .Select(c => (Structures.OfChar((char)c) ?? Structure.Empty, Legend.MachineOf((char)c) is { } kind && Content.Machines.Get(kind) is { Makes: not null, FuelSlot: null }))
        .ToArray();

    /// <summary>Each material's back wall, by code: a vein's is the rock it runs through, so digging it out leaves no second lump of ore.</summary>
    private static readonly int[] BackOf = Enumerable.Range(0, Materials.All.Max(m => m.Code) + 1)
        .Select(code => Materials.ByCode(code)?.In is { } rock ? Materials.Get(rock).Code : code)
        .ToArray();

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
        {
            string row = layout.Rows[y];
            for (int x = 0; x < layout.Width; x++, cell++)
            {
                char c = row[x];
                var look = Looks[c];
                int d = drawn == null ? 0 : drawn[cell];
                // A wall and its back wall of the material the map draws there (0077), else of the default hull.
                int material = d != 0 ? d : hull;
                int back = (uint)material < (uint)BackOf.Length ? BackOf[material] : material;
                // Every cell but space has a back wall of the default hull, hull walls included; a planet's sky has none (0077).
                if (c != ' ' && (drawn == null || d > 0) && !look.Bare) { s.Back[cell] = Back.Wall; s.BackMaterial[cell] = back; }
                s.Structure[cell] = look.Structure;
                if (Structures.Is(look.Structure, Trait.Terrain)) s.Material[cell] = material;
            }
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
        State.ByTrait = NoneByTrait();
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
        var traits = Content.Machines.TryGet(m.Kind, out var spec) ? spec.Traits : MachineTrait.None;
        for (int bit = 0; bit < TraitBits; bit++)
            if (((int)traits & (1 << bit)) != 0) State.ByTrait[bit].Add(m);
    }

    /// <summary>How many traits there are: <see cref="MachineTrait"/>'s flags are its first bits.</summary>
    private static readonly int TraitBits = BitOperations.Log2((uint)Enum.GetValues<MachineTrait>().Max()) + 1;

    /// <summary>A list for each trait, all empty: the machines by trait before any is listed.</summary>
    internal static List<Machine>[] NoneByTrait() => [.. Enumerable.Range(0, TraitBits).Select(_ => new List<Machine>())];

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
    public Box? Reserved => State.Reserved;
    public ReadOnlySpan<int> StructureLayer => State.Structure;
    public ReadOnlySpan<int> MaterialLayer => State.Material;
    public ReadOnlySpan<int> BackLayer => State.Back;
    public ReadOnlySpan<int> BackMaterialLayer => State.BackMaterial;
    public IReadOnlyList<Machine> Machines => State.Machines;
    public Machine? MachineById(int id) => State.ById.GetValueOrDefault(id);

    public IReadOnlyList<Machine> MachinesOf(string kind) =>
        State.ByKind.TryGetValue(kind, out var machines) ? machines : NoMachines;

    public IReadOnlyList<Machine> MachinesWith(MachineTrait trait)
    {
        if (!BitOperations.IsPow2((uint)trait)) throw new ArgumentException($"grid.machinesWith: {trait} is not one trait");
        return State.ByTrait[BitOperations.Log2((uint)trait)];
    }

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
        Has(cell, Trait.Terrain) ? MaterialOf(State.Material[cell]) : null;

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

    public Port? PortAt(int cell)
    {
        if (MachineAt(cell) is not { } m) return null;
        int dx = cell % State.Width - m.Cell % State.Width, dy = cell / State.Width - m.Cell / State.Width;
        foreach (var p in Content.Machines.Get(m.Kind).Ports)
            if (p.X == dx && p.Y == dy) return p;
        return null;
    }

    private bool InBounds(int cell) => cell >= 0 && cell < State.Width * State.Height;
    private bool InZone(int cell) => State.Zone.Contains(cell % State.Width, cell / State.Width);
    private bool InReserved(int cell) => State.Reserved is { } box && box.Contains(cell % State.Width, cell / State.Width);

    /// <summary>
    /// Why <paramref name="cells"/> may not be built on: <c>grid.reserved</c> for one in the reserved box, before
    /// <c>grid.outsideZone</c> for one outside the zone; written by <paramref name="laid"/>, any outside the box.
    /// </summary>
    private string? Outside(IReadOnlyList<int> cells, bool laid)
    {
        if (!laid && State.Reserved != null)
            foreach (int c in cells)
                if (InReserved(c)) return "grid.reserved";
        foreach (int c in cells)
            if (Outside(c, laid) is { } why) return why;
        return null;
    }

    private string? Outside(int cell, bool laid) =>
        laid ? (InReserved(cell) ? null : "grid.outsideReserved")
        : InReserved(cell) ? "grid.reserved"
        : InZone(cell) ? null : "grid.outsideZone";
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
        var spec = Structures.Of(s);
        if (spec.Build == null || Builds.PieceOf(spec.Build) is not var (across, tall)) return [cell];
        // A piece (a ladder's, a door segment, a hatch, 0104, 0111): columns paired from the left end of its row's run of the same,
        // rows from the top of its column's. A doorway's cells are the same open or shut.
        int w = State.Width, x = cell % w, y = cell / w;
        bool Same(int cx, int cy) => cx >= 0 && cy >= 0 && cx < w && cy < State.Height
            && (spec.Group != StructureGroup.None
                ? Structures.Of(State.Structure[cy * w + cx]).Group == spec.Group
                : State.Structure[cy * w + cx] == s);
        int left = 0, up = 0;
        while (Same(x - left - 1, y)) left++;
        if (tall > 1) while (Same(x, y - up - 1)) up++;
        int x0 = x - left % across, y0 = y - up % tall;
        var cells = new List<int>(across * tall);
        for (int cy = y0; cy < y0 + tall; cy++)
            for (int cx = x0; cx < x0 + across; cx++)
                if (Same(cx, cy)) cells.Add(cy * w + cx);
        return cells;
    }

    public void SetDoorway(int cell, bool open)
    {
        // Already as asked, a doorway is left alone, the version too: nothing derived from the grid is redone.
        int to = Doorways.Toggled(State.Structure[cell], open);
        var doorway = DoorwayOf(cell, _structureAt);
        if (!Differs(State.Structure, doorway, to)) return;
        State.Version++;
        foreach (int c in doorway) Set(State.Structure, GridLayers.Structure, c, to);
    }

    public IReadOnlyDictionary<int, int> AfterToggles(IReadOnlyList<Command> toggles)
    {
        // Only the toggled cells: copying the layers cost a big world 16 MB a tick while a toggle was in flight.
        var structure = new Dictionary<int, int>();
        int StructureAt(int c) => structure.TryGetValue(c, out int code) ? code : State.Structure[c];
        foreach (var t in toggles)
        {
            if (t is SetDoor door && InBounds(door.Cell) && Doorways.IsDoorway(StructureAt(door.Cell)) && !Doorways.IsPlatform(StructureAt(door.Cell)))
            {
                int to = Doorways.Toggled(StructureAt(door.Cell), door.Open);
                foreach (int cell in DoorwayOf(door.Cell, StructureAt)) structure[cell] = to;
            }
        }
        return structure;
    }

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

    public string? PlacementProblem(int cell, Placement p) => Problem(cell, p, laid: false);

    /// <summary>Why <paramref name="p"/> may not go at <paramref name="cell"/>: built inside the zone and out of the reserved box, or <paramref name="laid"/> inside the box.</summary>
    private string? Problem(int cell, Placement p, bool laid)
    {
        if (!InBounds(cell)) return "grid.outOfBounds";
        if (Outside(cell, laid) is { } outside) return outside;
        switch (p.Layer)
        {
            case "structure":
                // A piece (a ladder, a door segment, 0104) needs every cell it covers as one cell needs its own.
                IReadOnlyList<int>? covers = Builds.PieceOf(p.Kind) == null ? [cell] : Footprint(p.Kind, cell);
                if (covers == null) return "grid.outOfBounds";
                if (Outside(covers, laid) is { } away) return away;
                if (covers.Any(c => State.Structure[c] != Structure.Empty)) return "grid.occupied";
                if (covers.Any(c => State.Occupancy[c] != 0)) return "grid.machineInTheWay";
                if (Structures.ByBuild(p.Kind)?.Has(Trait.Terrain) == true && !Materials.TryGet(p.Material ?? "", out _)) return "grid.noSuchMaterial";
                return ShaftProblem(p.Kind, covers);
            case "back":
                if (State.Back[cell] != Back.None) return "grid.occupied";
                return Materials.TryGet(p.Material ?? "", out _) ? null : "grid.noSuchMaterial";
            case "machine":
                if (!Content.Machines.TryGet(p.Kind, out _)) return "grid.noSuchMachine";
                var cells = Footprint(p.Kind, cell);
                if (cells == null) return "grid.outOfBounds";
                if (Outside(cells, laid) is { } off) return off;
                if (cells.Any(c => State.Structure[c] != Structure.Empty)) return "grid.blocked";
                if (cells.Any(c => State.Occupancy[c] != 0)) return "grid.machineInTheWay";
                if (cells.Any(UnderPlatform)) return "grid.underPlatform";
                if (Content.Machines.Get(p.Kind).Exhaust is int reach && !ExhaustsThroughHull(cell, Content.Machines.Get(p.Kind).Height, reach))
                    return "grid.noExhaust";
                if (!Content.Machines.Is(p.Kind, MachineTrait.Lands)) return null;
                // Off the ship a pod is only ever landed by its module, on the ground.
                if (Surfaces is { Here: not 0 }) return RowUnder(p.Kind, cell, Solid) ? null : "grid.noGround";
                return OnRun(p.Kind, cell) ? null : "grid.noPlatform";
            default:
                return "grid.noSuchLayer";
        }
    }

    public bool OnRun(string kind, int cell) => RowUnder(kind, cell, c => Doorways.IsPlatform(State.Structure[c]));

    public bool Taken(int cell) => State.Structure[cell] != Structure.Empty || State.Occupancy[cell] != 0;

    public bool ClearBelow(int cell)
    {
        for (int c = cell + State.Width; c < State.Structure.Length; c += State.Width)
            if (Taken(c)) return false;
        return true;
    }

    /// <summary>Whether a launch platform stands above <paramref name="cell"/> in its column, however far up: the cell is in its shaft.</summary>
    private bool UnderPlatform(int cell)
    {
        for (int c = cell - State.Width; c >= 0; c -= State.Width)
            if (Doorways.IsPlatform(State.Structure[c])) return true;
        return false;
    }

    /// <summary>
    /// Why a structure of <paramref name="kind"/> may not cover <paramref name="covers"/> for the shafts the pod falls through: in one
    /// (<c>grid.underPlatform</c>), or a platform over one not empty to the edge (<c>grid.shaftBlocked</c>); null when it may.
    /// </summary>
    private string? ShaftProblem(string kind, IReadOnlyList<int> covers)
    {
        if (covers.Any(UnderPlatform)) return "grid.underPlatform";
        return Structures.Placed(kind) is int code && Doorways.IsPlatform(code) && !covers.All(ClearBelow) ? "grid.shaftBlocked" : null;
    }

    /// <summary>
    /// Whether every cell of the row under the footprint of a machine of <paramref name="kind"/> from <paramref name="cell"/> is
    /// <paramref name="under"/>: aboard a launch platform's, shut or open, so it stands over one run; off the ship, ground
    /// (<see cref="MachineTrait.Lands"/>).
    /// </summary>
    private bool RowUnder(string kind, int cell, Func<int, bool> under)
    {
        var spec = Content.Machines.Get(kind);
        int x = cell % State.Width, below = cell / State.Width + spec.Height;
        if (below >= State.Height) return false;
        for (int dx = 0; dx < spec.Width; dx++)
            if (!under(below * State.Width + x + dx)) return false;
        return true;
    }

    /// Whether a nozzle reaching <paramref name="reach"/> cells out of the left side of a machine at <paramref name="cell"/>,
    /// <paramref name="rows"/> tall, passes through the hull (2026-10-25-the-engine-through-the-hull): in every row, from its
    /// side, one wall or more, then space past them, open and with no back wall, up to the nozzle's tip or a cell past it.
    /// </summary>
    private bool ExhaustsThroughHull(int cell, int rows, int reach)
    {
        int w = State.Width, x0 = cell % w, y0 = cell / w;
        for (int y = y0; y < y0 + rows; y++)
        {
            int walls = 0;
            while (walls < reach && IsWall(x0 - 1 - walls, y)) walls++;
            if (walls == 0) return false;
            for (int k = walls; k < Math.Max(walls + 1, reach); k++)
                if (!IsSpace(x0 - 1 - k, y)) return false;
        }
        return true;
    }

    private bool IsWall(int x, int y) => x >= 0 && Structures.Is(State.Structure[y * State.Width + x], Trait.Terrain);

    private bool IsSpace(int x, int y)
    {
        if (x < 0) return true;
        int c = y * State.Width + x;
        return State.Structure[c] == Structure.Empty && State.Back[c] == Back.None && State.Occupancy[c] == 0;
    }

    public void Place(int cell, Placement p) => Put(cell, p, laid: false);

    public void Lay(int cell, Placement p) => Put(cell, p, laid: true);

    public void Reserve(Box? box)
    {
        if (State.Reserved == box) return;
        State.Version++;
        State.Reserved = box;
    }

    private void Put(int cell, Placement p, bool laid)
    {
        var problem = Problem(cell, p, laid);
        if (problem != null) throw new InvalidOperationException($"grid.{(laid ? "lay" : "place")} at {cell}: {problem}");
        State.Version++;
        Grow(cell);
        switch (p.Layer)
        {
            case "structure":
                int code = Structures.Placed(p.Kind) ?? throw new InvalidOperationException($"grid.place: no structure {p.Kind}");
                if (Builds.PieceOf(p.Kind) != null)
                {
                    var piece = Footprint(p.Kind, cell)!;
                    foreach (int c in piece) Set(State.Structure, GridLayers.Structure, c, code);
                    Grow(piece[^1]);
                    return;
                }
                Set(State.Structure, GridLayers.Structure, cell, code);
                if (Structures.Is(code, Trait.Terrain)) Set(State.Material, GridLayers.Material, cell, Materials.Get(p.Material!).Code);
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
                foreach (var with in State.ByTrait)
                    if (IndexOf(with, removed.Id) is int at and >= 0) with.RemoveAt(at);
                var footprint = Footprint(removed.Kind, removed.Cell) ?? [];
                foreach (int c in footprint) Set(State.Occupancy, GridLayers.Machine, c, 0);
                Shrink(footprint);
                return;
            default: return;
        }
        Shrink([cell]);
    }
}
