using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Rooms;

namespace Stillwake.Sim.Debris;

/// <summary>
/// Catches piece <c>Piece</c> into the sender's inventory: as its salvage item, or, a container, as what
/// it opens into; what does not fit floats at the hero's cell.
/// </summary>
public sealed record Grab(int Piece) : Command;

/// <summary>
/// The pieces of debris drifting past the ship, and catching them (ADRs 0006 and 0009;
/// packages/sim/src/modules/debris).
/// </summary>
public sealed partial class DebrisModule : Module<DebrisState>
{
    /// <summary>Thousandths of a cell: a piece's position and speed are in these.</summary>
    public const int Unit = 1000;

    private static readonly int TotalWeight = Content.Debris.Kinds.Sum(k => k.Weight);

    private readonly IGridQueries _grid;
    private readonly ICrewQueries _crew;
    private readonly IRoomsQueries _rooms;
    private readonly IItemsOps _items;

    public override string Id => "debris";

    public DebrisModule(IGridQueries grid, ICrewQueries crew, IRoomsQueries rooms, IItemsOps items)
    {
        _grid = grid;
        _crew = crew;
        _rooms = rooms;
        _items = items;
        Handles<Grab>(c => GrabProblem(c.From, c.Piece), Apply);
        // After derivation, like the air: the grid's size is that of this tick.
        System(Phase.Simulation, Drift);
    }

    protected override DebrisState Init(Layout layout) => new();

    // Queries.

    /// <summary>The pieces out, in spawn order; live and read-only.</summary>
    public IReadOnlyList<Piece> Pieces => State.Pieces;

    /// <summary>The first piece, in spawn order, whose middle is in <paramref name="cell"/>.</summary>
    public Piece? PieceAt(int cell) => State.Pieces.Find(p => CellOf(p.X, p.Y, _grid.Width) == cell);

    /// <summary>
    /// Why peer <paramref name="peer"/>'s grab of piece <paramref name="id"/> would be refused: it is
    /// gone, or the hero or the piece is not in space; what the client shows first. A full inventory is
    /// no reason: what does not fit floats where the hero is.
    /// </summary>
    public string? GrabProblem(int peer, int id)
    {
        var piece = State.Pieces.Find(p => p.Id == id);
        if (piece == null) return "debris.gone";
        var hero = _crew.HeroOf(peer);
        if (hero == null || !Outside(hero.Cell) || !Outside(CellOf(piece.X, piece.Y, _grid.Width))) return "debris.notOutside";
        return null;
    }

    private bool Outside(int cell) => _rooms.RoomAt(cell) == RoomsModule.Space;

    /// <summary>The cell under a piece's middle at (<paramref name="x"/>, <paramref name="y"/>); a piece is always on the grid, so neither is negative.</summary>
    public static int CellOf(int x, int y, int width) => y / Unit * width + x / Unit;

    /// <summary>
    /// What catching a piece of <paramref name="item"/> gives: the item itself, or, for a container, its
    /// loot drawn by <paramref name="rng"/> entry by entry: the one-in roll, then the count. Merged by
    /// item, in the order the entries first give it.
    /// </summary>
    public static IReadOnlyList<(string Item, int Count)> LootOf(string item, Rng rng)
    {
        var loot = Content.Loot.Of(item);
        if (loot == null) return [(item, 1)];
        var got = new List<(string Item, int Count)>();
        foreach (var entry in loot)
        {
            if (entry.OneIn is int oneIn && rng.Below((uint)oneIn) != 0) continue;
            int count = entry.Min + (int)rng.Below((uint)(entry.Max - entry.Min + 1));
            if (count <= 0) continue;
            int at = got.FindIndex(g => g.Item == entry.Item);
            if (at < 0) got.Add((entry.Item, count));
            else got[at] = (entry.Item, got[at].Count + count);
        }
        return got;
    }

    // Commands.

    private void Apply(Grab c)
    {
        int at = State.Pieces.FindIndex(p => p.Id == c.Piece);
        if (at < 0) throw new InvalidOperationException("debris.grab: applied to a piece that is gone");
        var piece = State.Pieces[at];
        State.Pieces.RemoveAt(at);
        var hero = _crew.HeroOf(c.From) ?? throw new InvalidOperationException("debris.grab: applied with no hero");
        _items.GiveOrDrop(c.From, LootOf(piece.Item, Random), hero.Cell);
    }

    // Systems.

    private void Drift()
    {
        var s = State;
        s.Clock += 1;
        foreach (var piece in s.Pieces) piece.X -= piece.Speed;
        s.Pieces.RemoveAll(p => p.X < p.End);
        if (s.Clock < s.NextSpawn) return;
        s.NextSpawn = s.Clock + Content.Debris.Every + (int)Random.Below(Content.Debris.Jitter + 1);
        if (s.Pieces.Count >= Content.Debris.Max) return;
        // Found again on every beat, so a ship grown taller gets a taller stream.
        if (HullOf() is not { } hull) return;
        var (left, right, top, bottom) = hull;
        int width = _grid.Width, height = _grid.Height;
        int from = Math.Max(0, top - Content.Debris.Margin);
        int to = Math.Min(height - 1, bottom + Content.Debris.Margin);
        int row = from + (int)Random.Below((uint)(to - from + 1));
        var item = KindOf();
        int speed = Content.Debris.DriftMin + (int)Random.Below(Content.Debris.DriftMax - Content.Debris.DriftMin + 1);
        int start = Math.Min(width - 1, right + Content.Debris.Lead) * Unit + Unit / 2;
        s.Pieces.Add(new Piece
        {
            Id = s.NextId,
            Item = item,
            X = start,
            Y = row * Unit + Unit / 2,
            Speed = speed,
            Start = start,
            End = Math.Max(0, left - Content.Debris.Lead) * Unit,
        });
        s.NextId += 1;
    }

    /// <summary>The columns and rows the hull spans: every cell with a structure in it. None for no hull.</summary>
    private (int Left, int Right, int Top, int Bottom)? HullOf()
    {
        var structure = _grid.StructureLayer;
        int width = _grid.Width;
        (int Left, int Right, int Top, int Bottom)? hull = null;
        for (int cell = 0; cell < structure.Length; cell++)
        {
            if (structure[cell] == Structure.Empty) continue;
            int x = cell % width, y = cell / width;
            hull = hull is { } h ? (Math.Min(h.Left, x), Math.Max(h.Right, x), h.Top, y) : (x, x, y, y);
        }
        return hull;
    }

    /// <summary>A kind drawn by weight, walking <c>Debris.Kinds</c> in order.</summary>
    private string KindOf()
    {
        int roll = (int)Random.Below((uint)TotalWeight);
        foreach (var (item, weight) in Content.Debris.Kinds)
        {
            if (roll < weight) return item;
            roll -= weight;
        }
        throw new InvalidOperationException("debris: a roll past the total weight");
    }
}
