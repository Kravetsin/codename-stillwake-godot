using System.Text.Json;
using Stillwake.Content;
using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Building;

/// <summary>
/// A plant or a stone on a planet's surface (0079) at <c>Cell</c>, the open cell over the soil it grows from, of
/// <c>Kind</c> (<see cref="Flora"/>).
/// </summary>
public sealed record Plant(int Cell, string Kind);

/// <summary>
/// What grows on a planet's surface (0079): grown from its seed with the ground (<see cref="Layout.Flora"/>), never saved,
/// but for the plants felled since, by cell. One is felled by a hit on any cell it stands in, or once the ground under it is
/// gone or something stands in its cell.
/// </summary>
public sealed partial class BuildingModule
{
    /// <summary>The plants the ground grew with, ascending by cell: derived from the layout, the ground a load grows again.</summary>
    private IReadOnlyList<(int Cell, string Kind)> _grown = [];

    /// <summary>Derived: the plants standing, ascending by cell, and the plant standing in each cell it stands in.</summary>
    private readonly List<Plant> _plants = [];
    private readonly Dictionary<int, Plant> _plantIn = new();

    /// <summary>The plants standing, ascending by cell: live and read-only. Felling only takes from it, so its count tells a change.</summary>
    public IReadOnlyList<Plant> Plants => _plants;

    /// <summary>The plant standing in <paramref name="cell"/>, its own or one of the cells it stands up into; null for none.</summary>
    public Plant? PlantAt(int cell) => _plantIn.GetValueOrDefault(cell);

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
        _plantIn.Clear();
        foreach (var (cell, kind) in _grown)
            if (State.Felled.BinarySearch(cell) < 0) Stand(new Plant(cell, kind));
    }

    private void Stand(Plant plant)
    {
        _plants.Add(plant);
        foreach (int cell in CellsOf(plant)) _plantIn[cell] = plant;
    }

    /// <summary>The cells <paramref name="plant"/> stands in, its own and up from it, those on the grid.</summary>
    private IEnumerable<int> CellsOf(Plant plant)
    {
        int tall = Flora.Get(plant.Kind).Tall;
        for (int i = 0; i < tall && plant.Cell - i * _grid.Width >= 0; i++) yield return plant.Cell - i * _grid.Width;
    }

    /// <summary>Fells <paramref name="plant"/>: it is noted felled, for good, and gives what it yields to <paramref name="peer"/>, if any.</summary>
    private void Fell(Plant plant, int? peer)
    {
        int at = State.Felled.BinarySearch(plant.Cell);
        if (at >= 0) throw new InvalidOperationException($"building: plant at {plant.Cell} felled twice");
        State.Felled.Insert(~at, plant.Cell);
        _plants.RemoveAt(_plants.BinarySearch(plant, ByCell));
        foreach (int cell in CellsOf(plant)) _plantIn.Remove(cell);
        if (peer is int sender && Flora.Get(plant.Kind).Yields is { Count: > 0 } yields) _itemsOps.GiveOrDrop(sender, yields, plant.Cell);
    }

    private static readonly Comparer<Plant> ByCell = Comparer<Plant>.Create((a, b) => a.Cell.CompareTo(b.Cell));

    /// <summary>
    /// Fells every plant whose ground is gone — no wall under its cell, dug or taken apart — or whose cell holds something
    /// now, built or placed. In derivation, after every command and construction system; looked at only when the grid moved.
    /// </summary>
    private void SweepPlants()
    {
        if (_plants.Count == 0 || _grid.Version == _plantsSeen) return;
        _plantsSeen = _grid.Version;
        for (int i = _plants.Count - 1; i >= 0; i--)
        {
            var plant = _plants[i];
            int under = plant.Cell + _grid.Width;
            if (_grid.StructureAt(under) != Structure.Wall || _grid.StructureAt(plant.Cell) != Structure.Empty || _grid.MachineAt(plant.Cell) != null)
                Fell(plant, null);
        }
    }

    /// <summary>The grid's version when the plants were last looked at: derived, so after a load they are looked at again.</summary>
    private int _plantsSeen = -1;
}
