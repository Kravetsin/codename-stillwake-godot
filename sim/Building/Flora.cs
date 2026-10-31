using System.Text.Json;
using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Building;

/// <summary>
/// A plant or a stone on a planet's surface (0079), a growth of its caves (0080) or a piece of a ruin's home (0091), at
/// <c>Cell</c> of <c>Kind</c> (<see cref="Flora"/>): on the surface the open cell over the soil it grows from; in a cave the
/// open cell over its floor or under its ceiling, or the wall's own cell it lies in; a piece wider or taller than a cell by its
/// own cell, the bottom-left on a floor, the top-left else.
/// </summary>
public sealed record Plant(int Cell, string Kind);

/// <summary>
/// A relic a ruin grew with (0096, <see cref="Ruins.Relics"/>): a spire's cradle or a home's niche at its own <c>Cell</c>, and
/// whether it is <c>Taken</c>, felled since the ground grew. Read from what is saved alone, so every peer and every load agrees
/// which spires and homes still burn.
/// </summary>
public sealed record Relic(int Cell, string Kind, bool Taken);

/// <summary>
/// What grows on a planet, on its surface (0079) and in its caves (0080), and the pieces of its ruins (0091): grown from its seed
/// with the ground (<see cref="Layout.Flora"/>), never saved, but for the growths felled since, by cell. One is felled by a hit on
/// any open cell it stands in, or once what it holds on to is gone — the wall under any of its columns, over them, its own, or
/// the piece it stands on — or something stands in its own open cell.
/// </summary>
public sealed partial class BuildingModule
{
    /// <summary>The plants the ground grew with, ascending by cell: derived from the layout, the ground a load grows again.</summary>
    private IReadOnlyList<(int Cell, string Kind)> _grown = [];

    /// <summary>
    /// Derived: the plants standing, ascending by cell; each by its own cell; the plant standing in front in each cell it stands in,
    /// and behind (0091: a back wall's pieces, a rug); and the resonators standing, by their own cells.
    /// </summary>
    private readonly List<Plant> _plants = [];
    private readonly Dictionary<int, Plant> _plantOwn = new();
    private readonly Dictionary<int, Plant> _plantIn = new();
    private readonly Dictionary<int, Plant> _plantBehind = new();
    private readonly List<int> _resonators = [];
    private readonly List<Relic> _relics = [];
    private readonly List<Plant> _posts = [];

    /// <summary>The relics the ground grew with, ascending by cell, standing or taken (0096): live and read-only.</summary>
    public IReadOnlyList<Relic> Relics => _relics;

    /// <summary>The posts standing, ascending by cell (spire guards): live and read-only.</summary>
    public IReadOnlyList<Plant> Posts => _posts;

    /// <summary>The post standing with its own cell at <paramref name="cell"/>; null for none, or for one gone.</summary>
    public Plant? PostAt(int cell) => _plantOwn.TryGetValue(cell, out var plant) && Kinds[plant.Kind].Post != null ? plant : null;

    /// <summary>Fells the post at <paramref name="cell"/> for good, its creature killed; a wall broken may have felled it already.</summary>
    public void FellPost(int cell)
    {
        if (PostAt(cell) is { } post) Fell(post, null);
    }

    /// <summary>Every kind of growth by id, looked up as often as a plant is.</summary>
    private static readonly Dictionary<string, Flora.Growth> Kinds = Flora.Every.ToDictionary(k => k.Id, StringComparer.Ordinal);

    /// <summary>The plants standing, ascending by cell: live and read-only. Felling only takes from it, so its count tells a change.</summary>
    public IReadOnlyList<Plant> Plants => _plants;

    /// <summary>
    /// The plant a hit on <paramref name="cell"/> takes: the one standing in front there, its own or one of the cells it stands up,
    /// hangs down or reaches across into, else the one behind (0091), never one painted on the back wall; null for none.
    /// </summary>
    public Plant? PlantAt(int cell) =>
        _plantIn.GetValueOrDefault(cell) ?? (_plantBehind.TryGetValue(cell, out var behind) && !Kinds[behind.Kind].Fixed ? behind : null);

    /// <summary>The cell <paramref name="plant"/> holds on to under, over or in its own column: the wall under it, over it, or its own (0080).</summary>
    public int HoldOf(Plant plant) => Kinds[plant.Kind].On switch
    {
        Flora.Anchor.Floor => plant.Cell + _grid.Width,
        Flora.Anchor.Ceiling => plant.Cell - _grid.Width,
        _ => plant.Cell,
    };

    /// <summary>
    /// Whether a hero reported at <paramref name="cell"/> stands right by a resonator (0091): within <see cref="Ruins.ZapReach"/>
    /// cells of one of its cells either way. The client draws its arc to the hero while it does.
    /// </summary>
    public bool Zaps(int cell)
    {
        int w = _grid.Width;
        if (_resonators.Count == 0 || cell < 0 || cell >= w * _grid.Height) return false;
        var kind = Kinds[Ruins.Resonator];
        int x = cell % w, y = cell / w, reach = Ruins.ZapReach;
        foreach (int own in _resonators)
        {
            int left = own % w, bottom = own / w;
            if (x >= left - reach && x < left + kind.Across + reach && y > bottom - kind.Tall - reach && y <= bottom + reach) return true;
        }
        return false;
    }

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
        _plantBehind.Clear();
        _resonators.Clear();
        _relics.Clear();
        _posts.Clear();
        foreach (var (cell, kind) in _grown)
        {
            bool felled = State.Felled.BinarySearch(cell) >= 0;
            if (!felled) Stand(new Plant(cell, kind));
            if (Ruins.Relics.Contains(kind)) _relics.Add(new Relic(cell, kind, felled));
        }
    }

    private void Stand(Plant plant)
    {
        _plants.Add(plant);
        _plantOwn[plant.Cell] = plant;
        var layer = LayerOf(plant);
        foreach (int cell in CellsOf(plant)) layer[cell] = plant;
        if (plant.Kind == Ruins.Resonator) _resonators.Add(plant.Cell);
        if (Kinds[plant.Kind].Post != null) _posts.Add(plant);
    }

    /// <summary>Where <paramref name="plant"/>'s cells are kept: in front, or behind for a back wall's piece or a rug (0091).</summary>
    private Dictionary<int, Plant> LayerOf(Plant plant) => Kinds[plant.Kind].Behind ? _plantBehind : _plantIn;

    /// <summary>
    /// The cells <paramref name="plant"/> stands in, those on the grid: each of its <c>Across</c> columns from its own, up from it
    /// on a floor, down from it from a ceiling, on a back wall or in the rock.
    /// </summary>
    private IEnumerable<int> CellsOf(Plant plant)
    {
        var kind = Kinds[plant.Kind];
        int w = _grid.Width, step = kind.On == Flora.Anchor.Floor ? -w : w, cells = w * _grid.Height, x = plant.Cell % w;
        for (int dx = 0; dx < kind.Across && x + dx < w; dx++)
            for (int i = 0, cell = plant.Cell + dx; i < kind.Tall && cell >= 0 && cell < cells; i++, cell += step) yield return cell;
    }

    /// <summary>
    /// The cells <paramref name="plant"/> holds on to: the cell under each of its columns on a floor (a wall, or a piece it stands
    /// on), over each from a ceiling, its own walls in the rock; for one on a back wall, the back walls of its own cells.
    /// </summary>
    private IEnumerable<int> HoldsOf(Plant plant)
    {
        var kind = Kinds[plant.Kind];
        if (kind.On is Flora.Anchor.Rock or Flora.Anchor.Back)
        {
            foreach (int cell in CellsOf(plant)) yield return cell;
            yield break;
        }
        int w = _grid.Width, by = kind.On == Flora.Anchor.Floor ? w : -w, x = plant.Cell % w;
        for (int dx = 0; dx < kind.Across && x + dx < w; dx++) yield return plant.Cell + dx + by;
    }

    /// <summary>
    /// Fells <paramref name="plant"/>: it is noted felled, for good, and gives what it yields to <paramref name="peer"/>; felled
    /// by nobody — built over, its hold gone to a meteor — it lets its yields fall at its cell for anyone (0085). What stood on it
    /// falls with it, its yields to the same (0091: the dishes with the table).
    /// </summary>
    private void Fell(Plant plant, int? peer)
    {
        int at = State.Felled.BinarySearch(plant.Cell);
        if (at >= 0) throw new InvalidOperationException($"building: plant at {plant.Cell} felled twice");
        State.Felled.Insert(~at, plant.Cell);
        _plants.RemoveAt(_plants.BinarySearch(plant, ByCell));
        _plantOwn.Remove(plant.Cell);
        var layer = LayerOf(plant);
        foreach (int cell in CellsOf(plant)) layer.Remove(cell);
        if (plant.Kind == Ruins.Resonator) _resonators.Remove(plant.Cell);
        _posts.Remove(plant);
        int relic = _relics.FindIndex(r => r.Cell == plant.Cell);
        if (relic >= 0) _relics[relic] = _relics[relic] with { Taken = true };
        // Its crack goes with it, before a wall built in its cell could take it over; one in the rock's is the wall's.
        if (Kinds[plant.Kind].On != Flora.Anchor.Rock && CrackIndex(plant.Cell) is >= 0 and var crack) State.Cracks.RemoveAt(crack);
        if (Kinds[plant.Kind].Yields is { Count: > 0 } yields)
        {
            if (peer is int sender) _itemsOps.GiveOrDrop(sender, yields, plant.Cell);
            else _itemsOps.DropAt(FloorStack.Nobody, yields, plant.Cell);
        }
        if (layer != _plantIn) return;
        var standing = new List<Plant>();
        foreach (int cell in CellsOf(plant))
            if (_plantIn.TryGetValue(cell - _grid.Width, out var above) && !standing.Contains(above) && Kinds[above.Kind].On == Flora.Anchor.Floor
                && HoldsOf(above).Contains(cell))
                standing.Add(above);
        foreach (var above in standing.OrderBy(p => p.Cell)) Fell(above, peer);
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
                if (i < _plants.Count && Uprooted(_plants[i])) Fell(_plants[i], null);
            return;
        }
        int w = _grid.Width;
        _nearChanges.Clear();
        foreach (var change in log)
            foreach (int cell in (ReadOnlySpan<int>)[change.Cell, change.Cell - w, change.Cell + w])
            {
                if (_plantIn.TryGetValue(cell, out var plant)) _nearChanges.Add(plant);
                if (_plantBehind.TryGetValue(cell, out var behind)) _nearChanges.Add(behind);
            }
        // Each at most once, by cell: felling takes it out of what the next would find, and what stood on it with it.
        foreach (var plant in _nearChanges.Distinct().OrderBy(p => p.Cell).ToList())
            if (_plantOwn.GetValueOrDefault(plant.Cell) == plant && Uprooted(plant)) Fell(plant, null);
    }

    /// <summary>Scratch for <see cref="SweepPlants"/>: the plants whose cells or holds a change touched.</summary>
    private readonly List<Plant> _nearChanges = [];

    /// <summary>
    /// Whether <paramref name="plant"/>'s hold is gone (it grows on walls only), or its own open cell holds
    /// something now. A floor's piece may stand on the piece in front under it instead (0091); one on a back wall needs its own.
    /// </summary>
    private bool Uprooted(Plant plant)
    {
        var on = Kinds[plant.Kind].On;
        if (on == Flora.Anchor.Rock) return HoldsOf(plant).Any(cell => _grid.WallAt(cell) == null);
        if (on == Flora.Anchor.Back)
        {
            if (_grid.BackAt(plant.Cell) == null) return true;
        }
        else
            foreach (int hold in HoldsOf(plant))
            {
                bool whole = _grid.WallAt(hold) != null;
                bool onPiece = on == Flora.Anchor.Floor && _plantIn.TryGetValue(hold, out var under) && under != plant && Kinds[under.Kind].On == Flora.Anchor.Floor;
                if (!whole && !onPiece) return true;
            }
        return _grid.StructureAt(plant.Cell) != Structure.Empty || _grid.MachineAt(plant.Cell) != null;
    }

    /// <summary>The grid's version when the plants were last looked at: derived, so after a load they are looked at again.</summary>
    private int _plantsSeen = -1;

    /// <summary>
    /// Every breath (<see cref="Content.Crew.BreathEvery"/> of this module's clock) a hero standing right by a resonator pays its
    /// zap (0091), in id order; none out cold.
    /// </summary>
    private void Zap()
    {
        if (_resonators.Count == 0 || State.Clock % Content.Crew.BreathEvery != 0) return;
        foreach (var hero in _crew.Heroes)
            if (hero.Out == 0 && Zaps(hero.Cell)) _crewOps.Shock(hero.Id, Ruins.Zap);
    }
}
