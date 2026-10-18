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
/// Strikes wreck <c>Piece</c> with weapon <c>Tool</c>, by its item: takes the weapon's <c>Damage</c> off its hit
/// points, and at none left it breaks into its loot, floating where it was. An electric weapon hits only while
/// the sender's hero runs it (<c>Run</c>), which pays for it by the tick (0055); the hit itself spends nothing.
/// Whether the sender holds it, and how often it strikes, is the client's word (0034, 0054).
/// </summary>
public sealed record Hit(int Piece, string Tool) : Command;

/// <summary>
/// Reels piece <c>Piece</c> by a harpoon to (<c>ToX</c>, <c>ToY</c>), in thousandths of a cell: the point
/// the client puts before the gun, kept on the grid. The last reel of a piece wins (0044).
/// </summary>
public sealed record Reel(int Piece, int ToX, int ToY) : Command;

/// <summary>
/// The pieces of debris drifting by, catching them, reeling them in and breaking wreckage with
/// weapons, in zones by the hull, in its asteroid band and round every hero (ADRs 0006, 0009, 0034, 0044, 0052 and 0054;
/// packages/sim/src/modules/debris).
/// </summary>
public sealed partial class DebrisModule : Module<DebrisState>
{
    /// <summary>Thousandths of a cell: a piece's position and speed are in these.</summary>
    public const int Unit = 1000;

    private readonly IGridQueries _grid;
    private readonly ICrewQueries _crew;
    private readonly IRoomsQueries _rooms;
    private readonly IItemsQueries _held;
    private readonly IItemsOps _items;

    public override string Id => "debris";

    public DebrisModule(IGridQueries grid, ICrewQueries crew, IRoomsQueries rooms, IItemsQueries held, IItemsOps items)
    {
        _grid = grid;
        _crew = crew;
        _rooms = rooms;
        _held = held;
        _items = items;
        Handles<Grab>(c => GrabProblem(c.From, c.Piece), Apply);
        Handles<Hit>(c => HitProblem(c.From, c.Piece, c.Tool) ?? NotRunning(c.From, c.Tool), Apply);
        Handles<Reel>(c => ReelProblem(c.From, c.Piece), Apply);
        // After derivation, like the air: the grid's size is that of this tick.
        System(Phase.Simulation, Drift);
    }

    /// <summary>Every zone's first piece due on its own <c>Every</c>.</summary>
    protected override DebrisState Init(Layout layout) => new()
    {
        AtStart = layout.DebrisAtStart,
        Beats = [.. Content.Debris.Zones.Select(z => new Beat { Zone = z.Id, Next = z.Every })],
    };

    // Queries.

    /// <summary>The pieces out, in spawn order; live and read-only.</summary>
    public IReadOnlyList<Piece> Pieces => State.Pieces;

    /// <summary>
    /// What catching piece <paramref name="id"/> put into its catcher's inventory, by item, or null when no catch of it
    /// is among the last <see cref="CaughtKept"/>: for the client to show its own catch over its hero (none if all fell).
    /// </summary>
    public IReadOnlyList<(string Item, int Count)>? CaughtFrom(int id)
    {
        for (int i = _caught.Count - 1; i >= 0; i--)
            if (_caught[i].Piece == id) return _caught[i].Gave;
        return null;
    }

    /// <summary>
    /// Every kind of debris found in this world (0066), in the order first found, each with the items seen come out
    /// of it: what a <see cref="Grab"/> or a breaking <see cref="Hit"/> drew. Live and read-only: the Handbook's.
    /// </summary>
    public IReadOnlyList<FoundKind> Found => State.Found;

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
    /// Why peer <paramref name="peer"/>'s hit of piece <paramref name="id"/> with weapon <paramref name="tool"/>
    /// would be refused: it is gone, the hero or the piece is not in space, it is no wreckage, or the weapon is
    /// electric and no battery on the belt can pay a tick of its draw. Any weapon breaks any wreck (0054). What
    /// the client asks before it sends: the command is refused besides while an electric weapon is not running.
    /// </summary>
    public string? HitProblem(int peer, int id, string tool)
    {
        if (Content.Weapons.Of(tool) == null) throw new InvalidOperationException($"debris.hit: {tool} deals no damage");
        return Reachable(peer, id, out var piece)
            ?? (Content.Wreckage.HpOf(piece!.Item) == null ? "debris.notWreckage"
            : Content.Items.Get(tool).Draw is int draw && !_held.CanDrain(peer, draw) ? "debris.flat"
            : null);
    }

    /// <summary>
    /// <c>debris.notRunning</c> for an electric weapon <paramref name="tool"/> the hero of <paramref name="peer"/> is
    /// not running: it hits only while its motor runs, paid by the tick (0055). The client sends its <c>Run</c> first.
    /// </summary>
    private string? NotRunning(int peer, string tool) =>
        Content.Items.Get(tool).Draw != null && _held.Running(peer) != tool ? "debris.notRunning" : null;

    /// <summary>
    /// Why peer <paramref name="peer"/>'s reel of piece <paramref name="id"/> would be refused: it is gone,
    /// or the hero or the piece is not in space, as for a grab. One reeled already is no reason: the last reel wins.
    /// </summary>
    public string? ReelProblem(int peer, int id) => Reachable(peer, id, out _);

    /// <summary>Why peer <paramref name="peer"/>'s hands cannot get at piece <paramref name="id"/>: it is gone, or the hero or the piece's middle is not in space.</summary>
    private string? Reachable(int peer, int id, out Piece? piece)
    {
        int at = IndexOf(id);
        piece = at < 0 ? null : State.Pieces[at];
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
        int at = IndexOf(c.Piece);
        if (at < 0) throw new InvalidOperationException("debris.grab: applied to a piece that is gone");
        var piece = State.Pieces[at];
        State.Pieces.RemoveAt(at);
        var hero = _crew.HeroOf(c.From) ?? throw new InvalidOperationException("debris.grab: applied with no hero");
        var loot = LootOf(piece.Item, Random);
        // From the roll, not from what went in: what floats off a full inventory was seen all the same.
        Record(piece, loot);
        var gave = _items.GiveOrDrop(c.From, loot, hero.Cell);
        if (_caught.Count == CaughtKept) _caught.RemoveAt(0);
        _caught.Add((piece.Id, gave));
    }

    private void Apply(Hit c)
    {
        int at = IndexOf(c.Piece);
        if (at < 0) throw new InvalidOperationException("debris.hit: applied to a piece that is gone");
        var piece = State.Pieces[at];
        int hp = Content.Wreckage.HpOf(piece.Item) ?? throw new InvalidOperationException("debris.hit: applied to no wreck");
        piece.Damage += Content.Weapons.Of(c.Tool)!.Damage;
        if (piece.Damage < hp) return;
        State.Pieces.RemoveAt(at);
        var loot = LootOf(piece.Item, Random);
        Record(piece, loot);
        // Dropped by nobody, so whoever broke it may sweep it up at once, as anyone may.
        _items.DropAt(FloorStack.Nobody, loot, CellOf(piece.X, piece.Y, _grid.Width));
    }

    /// <summary>
    /// Notes <paramref name="piece"/>'s kind as found, the look it wore (<c>Content.Looks</c>) as seen, and every item of
    /// <paramref name="loot"/> as seen come out of it (0066): each appended the first time, in the order given; the
    /// world's one record, whoever caught or broke it.
    /// </summary>
    private void Record(Piece piece, IReadOnlyList<(string Item, int Count)> loot)
    {
        string kind = piece.Item;
        var found = State.Found.Find(f => f.Kind == kind);
        if (found == null) State.Found.Add(found = new FoundKind { Kind = kind });
        int look = Content.Looks.Of(kind, piece.Id);
        if (!found.SeenLooks.Contains(look)) found.SeenLooks.Add(look);
        foreach (var (item, _) in loot)
            if (!found.Seen.Contains(item)) found.Seen.Add(item);
    }

    private void Apply(Reel c)
    {
        int at = IndexOf(c.Piece);
        if (at < 0) throw new InvalidOperationException("debris.reel: applied to a piece that is gone");
        var piece = State.Pieces[at];
        int toX = Math.Clamp(c.ToX, 0, _grid.Width * Unit - 1), toY = Math.Clamp(c.ToY, 0, _grid.Height * Unit - 1);
        int far = Math.Max(Math.Abs(toX - piece.X), Math.Abs(toY - piece.Y));
        // Rounded up, so no tick moves it more than the reel's speed; at least one, so it lands on a tick.
        int left = Math.Max(1, (far + Content.Harpoon.ReelSpeed - 1) / Content.Harpoon.ReelSpeed);
        piece.Reel = new Reeling { By = c.From, ToX = toX, ToY = toY, Left = left };
    }
}
