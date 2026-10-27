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
/// A hit of weapon <c>Tool</c> in space (the fan of rays): its fan cast from the hand at (<c>X</c>, <c>Y</c>), in thousandths
/// of a cell, aimed at <c>Heading</c>, each ray striking the first wreck it meets, by the cells it crosses, and stopping there
/// or at a wall. Each wreck struck loses the weapon's <c>Damage</c> once, however many rays meet it, and at none left breaks
/// into its loot, floating where it was. An electric weapon hits only while the sender's hero runs it (<c>Run</c>), which pays
/// for it by the tick (0055); the hit itself spends nothing. Whether the sender holds it, and how often it strikes, is the
/// client's word (0034, 0054).
/// </summary>
public sealed record HewWrecks(string Tool, int X, int Y, int Heading) : Command;

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
/// <summary>What another module may read of the debris: the pieces, and one by its id.</summary>
public interface IDebrisQueries
{
    IReadOnlyList<Piece> Pieces { get; }

    /// <summary>Ticks the debris has run: what a blow on a piece is stamped with (<see cref="Piece.Struck"/>).</summary>
    int Clock { get; }

    Piece? PieceById(int id);

    IReadOnlyList<WreckBox> Wrecks();

    /// <summary>Whether a hit of <paramref name="damage"/> on wreck <paramref name="piece"/> breaks it: what <see cref="IDebrisOps.Hit"/> decides by.</summary>
    bool Breaks(int piece, int damage);
}

/// <summary>
/// A wreck a hit may strike, its middle in space: piece <paramref name="Piece"/>'s box (<see cref="Content.Wreckage.SizeOf"/>),
/// its edges in thousandths of a cell, the right and bottom ones past it.
/// </summary>
public readonly record struct WreckBox(int Piece, int Left, int Top, int Right, int Bottom)
{
    /// <summary>Whether the box covers part of <paramref name="cell"/> of a grid <paramref name="width"/> cells wide.</summary>
    public bool Covers(int cell, int width)
    {
        int left = cell % width * DebrisModule.Unit, top = cell / width * DebrisModule.Unit;
        return Left < left + DebrisModule.Unit && Right > left && Top < top + DebrisModule.Unit && Bottom > top;
    }
}

/// <summary>How another module may change the debris.</summary>
public interface IDebrisOps
{
    /// <summary>Piece <paramref name="id"/>'s reel let go, if it has one: it drifts on from where it is. A piece gone is let be.</summary>
    void Unreel(int id);

    /// <summary>Wreck <paramref name="id"/> struck by peer <paramref name="peer"/> for <paramref name="damage"/>: true when it broke.</summary>
    bool Hit(int peer, int id, int damage);
}

public sealed partial class DebrisModule : Module<DebrisState>, IDebrisQueries, IDebrisOps
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
        Handles<HewWrecks>(c => HewProblemOf(c.From, Struck(c.Tool, c.X, c.Y, c.Heading), c.Tool, running: true), Apply);
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

    public int Clock => State.Clock;

    /// <summary>Piece <paramref name="id"/>; null for one gone or never spawned.</summary>
    public Piece? PieceById(int id)
    {
        int at = IndexOf(id);
        return at < 0 ? null : State.Pieces[at];
    }

    // Operations.

    public void Unreel(int id)
    {
        if (PieceById(id) is { } piece) piece.Reel = null;
    }

    /// <summary>
    /// Wreck <paramref name="id"/> struck by peer <paramref name="peer"/> for <paramref name="damage"/>: the blow stamped with the
    /// debris clock (<see cref="Piece.Struck"/>), and at its hit points broken into its loot, floating where it was, anyone's. What
    /// a <see cref="HewWrecks"/> does to each wreck it strikes, and a shot to the ones it meets. A piece gone is let be. True when it broke.
    /// </summary>
    public bool Hit(int peer, int id, int damage)
    {
        int at = IndexOf(id);
        if (at < 0) return false;
        var piece = State.Pieces[at];
        bool breaks = Breaks(id, damage);
        piece.Damage += damage;
        piece.Struck = new Blow { By = peer, At = State.Clock };
        if (!breaks) return false;
        State.Pieces.RemoveAt(at);
        var loot = LootOf(piece.Item, Random);
        Record(piece, loot);
        // Dropped by nobody, so whoever broke it may sweep it up at once, as anyone may.
        _items.DropAt(FloorStack.Nobody, loot, CellOf(piece.X, piece.Y, _grid.Width));
        return true;
    }

    /// <inheritdoc/>
    public bool Breaks(int piece, int damage) =>
        PieceById(piece) is { } p && p.Damage + damage >= (Content.Wreckage.HpOf(p.Item) ?? throw new InvalidOperationException($"debris.hit: {p.Item} is no wreck"));

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
    /// of it: what a <see cref="Grab"/> or a breaking <see cref="HewWrecks"/> drew. Live and read-only: the Handbook's.
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
    /// The wrecks a hit of <paramref name="tool"/> from (<paramref name="x"/>, <paramref name="y"/>) toward
    /// <paramref name="heading"/> would strike (<see cref="HewWrecks"/>), in spawn order: each ray's first, whose box
    /// (<see cref="Content.Wreckage.SizeOf"/>) covers part of a cell the ray crosses, the first spawned of those in one cell; a
    /// ray stops at a wall or whatever stops a body, and a wreck whose middle is not in space is passed by. What the command
    /// strikes and the client's preview.
    /// </summary>
    public IReadOnlyList<Piece> Struck(string tool, int x, int y, int heading)
    {
        var weapon = Content.Weapons.Of(tool) ?? throw new InvalidOperationException($"debris.hew: {tool} deals no damage");
        var wrecks = Wrecks();
        var struck = new List<Piece>();
        if (wrecks.Count == 0) return struck;
        Rays.Fan(_grid.Width, _grid.Height, x, y, heading, weapon, cell =>
        {
            if (_grid.WallAt(cell) != null || _grid.Blocks(cell)) return true;
            foreach (var w in wrecks)
            {
                if (!w.Covers(cell, _grid.Width)) continue;
                var p = PieceById(w.Piece)!;
                if (!struck.Contains(p)) struck.Add(p);
                return true;
            }
            return false;
        });
        struck.Sort((a, b) => a.Id.CompareTo(b.Id));
        return struck;
    }

    /// <summary>
    /// The wrecks a hit may strike now, in spawn order, with their boxes: wreckage whose middle is in space, as for
    /// <see cref="Struck"/>. A new list a call: a shot's flight takes it once a tick and asks it of each cell it crosses.
    /// </summary>
    public IReadOnlyList<WreckBox> Wrecks()
    {
        var wrecks = new List<WreckBox>();
        foreach (var p in State.Pieces)
        {
            if (Content.Wreckage.SizeOf(p.Item) is not { } size || !Outside(CellOf(p.X, p.Y, _grid.Width))) continue;
            var (w, h) = size;
            wrecks.Add(new WreckBox(p.Id, p.X - w * Unit / 2, p.Y - h * Unit / 2, p.X + w * Unit / 2, p.Y + h * Unit / 2));
        }
        return wrecks;
    }

    /// <summary>
    /// Why peer <paramref name="peer"/>'s hit of <paramref name="tool"/> (<see cref="HewWrecks"/>) would be refused: the hero
    /// is not in space, the fan strikes no wreck, or the weapon is electric and no battery on the belt can pay a tick of its draw
    /// (<c>items.flat</c>). Any weapon breaks any wreck (0054). What the client asks before it sends: the command is refused
    /// besides while an electric weapon is not running (<c>items.notRunning</c>, <see cref="IItemsQueries.WeaponProblem"/>).
    /// </summary>
    public string? HewProblem(int peer, string tool, int x, int y, int heading) =>
        HewProblemOf(peer, Struck(tool, x, y, heading), tool, running: false);

    private string? HewProblemOf(int peer, IReadOnlyList<Piece> struck, string tool, bool running)
    {
        if (_crew.HeroOf(peer) is not { } hero || !Outside(hero.Cell)) return "debris.notOutside";
        return struck.Count == 0 ? "debris.nothingToHit" : _held.WeaponProblem(peer, tool, running);
    }

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
        return loot == null ? [(item, 1)] : Loots.Roll(loot, rng);
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

    private void Apply(HewWrecks c)
    {
        int hit = _held.Damage(c.From, c.Tool);
        foreach (var piece in Struck(c.Tool, c.X, c.Y, c.Heading)) Hit(c.From, piece.Id, hit);
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
