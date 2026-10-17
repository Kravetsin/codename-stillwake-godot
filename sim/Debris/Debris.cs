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
/// Swings an axe at wreck <c>Piece</c>: takes <c>Axe.Damage</c> off its hit points, and at none left it
/// breaks into its loot, floating where it was. Whether the sender holds an axe is the client's word (0034).
/// </summary>
public sealed record Hit(int Piece) : Command;

/// <summary>
/// Reels piece <c>Piece</c> by a harpoon to (<c>ToX</c>, <c>ToY</c>), in thousandths of a cell: the point
/// the client puts before the gun, kept on the grid. The last reel of a piece wins (0044).
/// </summary>
public sealed record Reel(int Piece, int ToX, int ToY) : Command;

/// <summary>
/// The pieces of debris drifting past the ship, catching them, reeling them in and breaking wreckage with
/// an axe (ADRs 0006, 0009, 0034 and 0044; packages/sim/src/modules/debris).
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
        Handles<Hit>(c => HitProblem(c.From, c.Piece), Apply);
        Handles<Reel>(c => ReelProblem(c.From, c.Piece), Apply);
        // After derivation, like the air: the grid's size is that of this tick.
        System(Phase.Simulation, Drift);
    }

    protected override DebrisState Init(Layout layout) => new() { AtStart = layout.DebrisAtStart };

    // Queries.

    /// <summary>The pieces out, in spawn order; live and read-only.</summary>
    public IReadOnlyList<Piece> Pieces => State.Pieces;

    /// <summary>The first piece, in spawn order, whose middle is in <paramref name="cell"/>.</summary>
    public Piece? PieceAt(int cell) => State.Pieces.Find(p => CellOf(p.X, p.Y, _grid.Width) == cell);

    /// <summary>
    /// Why peer <paramref name="peer"/>'s grab of piece <paramref name="id"/> would be refused: it is
    /// gone, the hero or the piece is not in space, or it is wreckage, too big to catch by hand; what the
    /// client shows first. A full inventory is no reason: what does not fit floats where the hero is.
    /// </summary>
    public string? GrabProblem(int peer, int id) =>
        Reachable(peer, id, out var piece) ?? (Content.Wreckage.HpOf(piece!.Item) != null ? "debris.tooBig" : null);

    /// <summary>
    /// Why peer <paramref name="peer"/>'s hit of piece <paramref name="id"/> would be refused: it is gone,
    /// the hero or the piece is not in space, or it is no wreckage.
    /// </summary>
    public string? HitProblem(int peer, int id) =>
        Reachable(peer, id, out var piece) ?? (Content.Wreckage.HpOf(piece!.Item) == null ? "debris.notWreckage" : null);

    /// <summary>
    /// Why peer <paramref name="peer"/>'s reel of piece <paramref name="id"/> would be refused: it is gone,
    /// or the hero or the piece is not in space, as for a grab. One reeled already is no reason: the last reel wins.
    /// </summary>
    public string? ReelProblem(int peer, int id) => Reachable(peer, id, out _);

    /// <summary>Why peer <paramref name="peer"/>'s hands cannot get at piece <paramref name="id"/>: it is gone, or the hero or the piece's middle is not in space.</summary>
    private string? Reachable(int peer, int id, out Piece? piece)
    {
        piece = State.Pieces.Find(p => p.Id == id);
        if (piece == null) return "debris.gone";
        var hero = _crew.HeroOf(peer);
        if (hero == null || !Outside(hero.Cell) || !Outside(CellOf(piece.X, piece.Y, _grid.Width))) return "debris.notOutside";
        return null;
    }

    private bool Outside(int cell) => _rooms.RoomAt(cell) == RoomsModule.Space;

    /// <summary>The cell under a piece's middle at (<paramref name="x"/>, <paramref name="y"/>); a piece is always on the grid, so neither is negative.</summary>
    public static int CellOf(int x, int y, int width) => y / Unit * width + x / Unit;

    /// <summary>
    /// What catching a piece of <paramref name="item"/> gives: the item itself, or, for a container (or a
    /// wreck broken), its loot drawn by <paramref name="rng"/> entry by entry: the one-in roll, then the count. Merged by
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

    private void Apply(Hit c)
    {
        int at = State.Pieces.FindIndex(p => p.Id == c.Piece);
        if (at < 0) throw new InvalidOperationException("debris.hit: applied to a piece that is gone");
        var piece = State.Pieces[at];
        int hp = Content.Wreckage.HpOf(piece.Item) ?? throw new InvalidOperationException("debris.hit: applied to no wreck");
        piece.Damage += Content.Axe.Damage;
        if (piece.Damage < hp) return;
        State.Pieces.RemoveAt(at);
        // Dropped by nobody, so whoever broke it may sweep it up at once, as anyone may.
        _items.DropAt(FloorStack.Nobody, LootOf(piece.Item, Random), CellOf(piece.X, piece.Y, _grid.Width));
    }

    private void Apply(Reel c)
    {
        var piece = State.Pieces.Find(p => p.Id == c.Piece) ?? throw new InvalidOperationException("debris.reel: applied to a piece that is gone");
        int toX = Math.Clamp(c.ToX, 0, _grid.Width * Unit - 1), toY = Math.Clamp(c.ToY, 0, _grid.Height * Unit - 1);
        int far = Math.Max(Math.Abs(toX - piece.X), Math.Abs(toY - piece.Y));
        // Rounded up, so no tick moves it more than the reel's speed; at least one, so it lands on a tick.
        int left = Math.Max(1, (far + Content.Harpoon.ReelSpeed - 1) / Content.Harpoon.ReelSpeed);
        piece.Reel = new Reeling { By = c.From, ToX = toX, ToY = toY, Left = left };
    }

    // Systems.

    /// <summary>
    /// Moves a reeled piece its share of the way still to go, truncated toward zero, so the last tick
    /// lands it exactly; the line comes free on that tick.
    /// </summary>
    private static void Pull(Piece piece, Reeling reel)
    {
        piece.X += (reel.ToX - piece.X) / reel.Left;
        piece.Y += (reel.ToY - piece.Y) / reel.Left;
        reel.Left -= 1;
        if (reel.Left == 0) piece.Reel = null;
    }

    private void Drift()
    {
        var s = State;
        // A world begins with pieces already on their way, as if the ship had drifted a while.
        if (s.Clock == 0 && s.AtStart > 0 && HullOf() is { } first)
            for (int i = 0; i < Math.Min(s.AtStart, Content.Debris.Max); i++) Spawn(first, along: true);
        s.Clock += 1;
        foreach (var piece in s.Pieces)
            if (piece.Reel is { } reel) Pull(piece, reel);
            else piece.X -= piece.Speed;
        s.Pieces.RemoveAll(p => p.X < p.End);
        if (s.Clock < s.NextSpawn) return;
        s.NextSpawn = s.Clock + Content.Debris.Every + (int)Random.Below(Content.Debris.Jitter + 1);
        if (s.Pieces.Count >= Content.Debris.Max) return;
        // Found again on every beat, so a ship grown taller gets a taller stream.
        if (HullOf() is { } hull) Spawn(hull, along: false);
    }

    /// <summary>
    /// A piece drawn by the world's Rng: its row, kind and speed, then, <paramref name="along"/>, how far
    /// along its way it already is; otherwise at its start.
    /// </summary>
    private void Spawn((int Left, int Right, int Top, int Bottom) hull, bool along)
    {
        var s = State;
        var (left, right, top, bottom) = hull;
        int width = _grid.Width, height = _grid.Height;
        int from = Math.Max(0, top - Content.Debris.Margin);
        int to = Math.Min(height - 1, bottom + Content.Debris.Margin);
        int row = from + (int)Random.Below((uint)(to - from + 1));
        var item = KindOf();
        int speed = Content.Debris.DriftMin + (int)Random.Below(Content.Debris.DriftMax - Content.Debris.DriftMin + 1);
        int start = Math.Min(width - 1, right + Content.Debris.Lead) * Unit + Unit / 2;
        int end = Math.Max(0, left - Content.Debris.Lead) * Unit;
        s.Pieces.Add(new Piece
        {
            Id = s.NextId,
            Item = item,
            X = along ? end + (int)Random.Below((uint)(start - end + 1)) : start,
            Y = row * Unit + Unit / 2,
            Speed = speed,
            Start = start,
            End = end,
        });
        s.NextId += 1;
    }

    /// <summary>
    /// The columns and rows the hull spans: every cell with a structure in it. None for no hull. Walked
    /// within the grid's <c>Built</c>, past which nothing stands, so it costs the ship and not the world.
    /// </summary>
    private (int Left, int Right, int Top, int Bottom)? HullOf()
    {
        var structure = _grid.StructureLayer;
        int width = _grid.Width;
        var built = _grid.Built;
        (int Left, int Right, int Top, int Bottom)? hull = null;
        for (int y = built.Top; y <= built.Bottom; y++)
            for (int x = built.Left; x <= built.Right; x++)
            {
                if (structure[y * width + x] == Structure.Empty) continue;
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
