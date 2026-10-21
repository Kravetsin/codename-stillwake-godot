using System.Text.Json;
using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Building;

/// <summary>
/// A plant or a stone on a planet's surface (0079), or a growth of its caves (0080), at <c>Cell</c> of <c>Kind</c>
/// (<see cref="Flora"/>): on the surface the open cell over the soil it grows from; in a cave the open cell over its floor or
/// under its ceiling, or the wall's own cell it lies in.
/// </summary>
public sealed record Plant(int Cell, string Kind);

/// <summary>
/// What grows on a planet, on its surface (0079) and in its caves (0080): grown from its seed with the ground
/// (<see cref="Layout.Flora"/>), never saved, but for the growths felled since, by cell. One is felled by a hit on any open
/// cell it stands in, or once what it holds on to is gone — the wall under it, over it, or its own — or something stands
/// in its open cell.
/// </summary>
public sealed partial class BuildingModule
{
    /// <summary>The plants the ground grew with, ascending by cell: derived from the layout, the ground a load grows again.</summary>
    private IReadOnlyList<(int Cell, string Kind)> _grown = [];

    /// <summary>Derived: the plants standing, ascending by cell; each by its own cell; and the plant standing in each cell it stands in.</summary>
    private readonly List<Plant> _plants = [];
    private readonly Dictionary<int, Plant> _plantOwn = new();
    private readonly Dictionary<int, Plant> _plantIn = new();

    /// <summary>Every kind of growth by id, looked up as often as a plant is.</summary>
    private static readonly Dictionary<string, Flora.Growth> Kinds =
        Flora.All.Cast<Flora.Growth>().Concat(Flora.Cave).ToDictionary(k => k.Id, StringComparer.Ordinal);

    /// <summary>The plants standing, ascending by cell: live and read-only. Felling only takes from it, so its count tells a change.</summary>
    public IReadOnlyList<Plant> Plants => _plants;

    /// <summary>The plant standing in <paramref name="cell"/>, its own or one of the cells it stands up or hangs down into; null for none.</summary>
    public Plant? PlantAt(int cell) => _plantIn.GetValueOrDefault(cell);

    /// <summary>The cell <paramref name="plant"/> holds on to: the wall under it, over it, or its own (0080).</summary>
    public int HoldOf(Plant plant) => Kinds[plant.Kind].On switch
    {
        Flora.Anchor.Floor => plant.Cell + _grid.Width,
        Flora.Anchor.Ceiling => plant.Cell - _grid.Width,
        _ => plant.Cell,
    };

    protected override BuildingState Init(Layout layout)
    {
        _grown = layout.Flora ?? [];
        return new();
    }

    protected override BuildingState? Load(JsonElement saved, JsonSerializerOptions options, Layout? ground)
    {
        _grown = ground?.Flora ?? [];
        return base.Load(saved, options, ground);
    }

    public override void Rebuild()
    {
        _plantsSeen = -1;
        _plants.Clear();
        _plantOwn.Clear();
        _plantIn.Clear();
        foreach (var (cell, kind) in _grown)
            if (State.Felled.BinarySearch(cell) < 0) Stand(new Plant(cell, kind));
    }

    private void Stand(Plant plant)
    {
        _plants.Add(plant);
        _plantOwn[plant.Cell] = plant;
        foreach (int cell in CellsOf(plant)) _plantIn[cell] = plant;
    }

    /// <summary>The cells <paramref name="plant"/> stands in, its own and up from it, or down from one hanging, those on the grid.</summary>
    private IEnumerable<int> CellsOf(Plant plant)
    {
        var kind = Kinds[plant.Kind];
        int step = kind.On == Flora.Anchor.Ceiling ? _grid.Width : -_grid.Width, cells = _grid.Width * _grid.Height;
        for (int i = 0, cell = plant.Cell; i < kind.Tall && cell >= 0 && cell < cells; i++, cell += step) yield return cell;
    }

    /// <summary>
    /// Fells <paramref name="plant"/>: it is noted felled, for good, and gives what it yields to <paramref name="peer"/>; felled
    /// by nobody — built over, its hold gone to a meteor — it lets its yields fall at its cell for anyone (0085).
    /// </summary>
    private void Fell(Plant plant, int? peer)
    {
        int at = State.Felled.BinarySearch(plant.Cell);
        if (at >= 0) throw new InvalidOperationException($"building: plant at {plant.Cell} felled twice");
        State.Felled.Insert(~at, plant.Cell);
        _plants.RemoveAt(_plants.BinarySearch(plant, ByCell));
        _plantOwn.Remove(plant.Cell);
        foreach (int cell in CellsOf(plant)) _plantIn.Remove(cell);
        // Its crack goes with it, before a wall built in its cell could take it over; one in the rock's is the wall's.
        if (Kinds[plant.Kind].On != Flora.Anchor.Rock && CrackIndex(plant.Cell) is >= 0 and var crack) State.Cracks.RemoveAt(crack);
        if (Kinds[plant.Kind].Yields is not { Count: > 0 } yields) return;
        if (peer is int sender) _itemsOps.GiveOrDrop(sender, yields, plant.Cell);
        else _itemsOps.DropAt(FloorStack.Nobody, yields, plant.Cell);
    }

    private static readonly Comparer<Plant> ByCell = Comparer<Plant>.Create((a, b) => a.Cell.CompareTo(b.Cell));

    /// <summary>
    /// Fells every plant whose hold is gone — no wall under it, over it or in its own cell, dug or taken apart — or whose open
    /// cell holds something now, built or placed. In derivation, after every command and construction system; looked at only
    /// when the grid moved, and then only round the cells its log names, all of them when it names none.
    /// </summary>
    private void SweepPlants()
    {
        if (_plants.Count == 0 || _grid.Version == _plantsSeen) return;
        var log = _plantsSeen < 0 ? null : _grid.ChangedSince(_plantsSeen);
        _plantsSeen = _grid.Version;
        if (log == null)
        {
            for (int i = _plants.Count - 1; i >= 0; i--)
                if (Uprooted(_plants[i])) Fell(_plants[i], null);
            return;
        }
        int w = _grid.Width;
        _nearChanges.Clear();
        foreach (var change in log)
            foreach (int cell in (ReadOnlySpan<int>)[change.Cell, change.Cell - w, change.Cell + w])
                if (_plantOwn.TryGetValue(cell, out var plant)) _nearChanges.Add(plant);
        // Each at most once, by cell: felling takes it out of what the next would find.
        foreach (var plant in _nearChanges.Distinct().OrderBy(p => p.Cell).ToList())
            if (Uprooted(plant)) Fell(plant, null);
    }

    /// <summary>Scratch for <see cref="SweepPlants"/>: the plants whose cell or hold a change touched.</summary>
    private readonly List<Plant> _nearChanges = [];

    /// <summary>Whether <paramref name="plant"/>'s hold is gone or cut to a slope (it grows on whole walls only), or its open cell holds something now.</summary>
    private bool Uprooted(Plant plant)
    {
        if (Kinds[plant.Kind].On == Flora.Anchor.Rock) return _grid.StructureAt(plant.Cell) != Structure.Wall;
        int hold = HoldOf(plant);
        return _grid.StructureAt(hold) != Structure.Wall || _grid.ShapeAt(hold) != Shape.None || _grid.StructureAt(plant.Cell) != Structure.Empty || _grid.MachineAt(plant.Cell) != null;
    }

    /// <summary>The grid's version when the plants were last looked at: derived, so after a load they are looked at again.</summary>
    private int _plantsSeen = -1;
}
