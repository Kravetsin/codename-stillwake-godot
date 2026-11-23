using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Knowledge;
using Stillwake.Sim.Rooms;
using Stillwake.Sim.Sky;

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
/// A hit of a running tool (tool-edge) on the wrecks its drawn edge touches, <c>Pieces</c> by id, as the client saw them: each
/// distinct one still a wreck in space (<see cref="DebrisModule.Wrecks"/>) loses the weapon's <c>Damage</c> once, ascending by id,
/// the cutter's salvaging as a <see cref="HewWrecks"/> does; one gone by the tick is skipped. Neither reach nor position is checked:
/// the client's word, among friends (0002).
/// </summary>
public sealed record HewPieces(string Tool, IReadOnlyList<int> Pieces) : Command;

/// <summary>
/// The sender's harpoon rod stuck in piece <c>Piece</c> at (<c>HookX</c>, <c>HookY</c>), in thousandths of a cell from its middle
/// in its own frame, clamped into its box (debris-physics): a <see cref="Line"/> as long as the way from its hero's reported cell to
/// the hook, at most <c>Harpoon.Range</c>. It replaces the sender's own line, and the last hook on a piece wins.
/// </summary>
public sealed record Hook(int Piece, int HookX, int HookY) : Command;

/// <summary>The sender's line wound in while <c>On</c>, the left button held; the host player's rope winds it.</summary>
public sealed record Wind(bool On) : Command;

/// <summary>The sender lets its line go: a right click, or its hero entering a room.</summary>
public sealed record LetGo() : Command;

/// <summary>A body's pose in <see cref="Moved"/>: piece <c>Id</c>'s middle, motion a tick, angle and spin, as <see cref="Piece"/> keeps them.</summary>
public readonly record struct Body(int Id, int X, int Y, int Vx, int Vy, int Angle, int Spin);

/// <summary>A line's length in <see cref="Moved"/>: hero <c>By</c>'s on piece <c>Piece</c>, in thousandths of a cell.</summary>
public readonly record struct LineLength(int By, int Piece, int Length);

/// <summary>
/// The host player's feed (debris-physics): <c>Bodies</c> as its physics had them at the start of the debris tick <c>ForTick</c>
/// (<see cref="IDebrisQueries.Clock"/>), before its drift, each carried on its own motion to the tick it applies on; and the
/// <c>Lines</c>' lengths its ropes wound and paid out. A piece gone or a line no longer there is skipped. From the host player alone,
/// addressed to <see cref="DebrisModule.BodiesOn"/> wherever its hero stands, a planet too.
/// </summary>
public sealed record Moved(int ForTick, IReadOnlyList<Body> Bodies, IReadOnlyList<LineLength> Lines) : Command, ISurfaceAddressed;

/// <summary>What another module may read of the debris: the pieces, and one by its id.</summary>
public interface IDebrisQueries
{
    IReadOnlyList<Piece> Pieces { get; }

    /// <summary>Ticks the debris has run: what a blow on a piece is stamped with (<see cref="Piece.Struck"/>).</summary>
    int Clock { get; }

    Piece? PieceById(int id);

    IReadOnlyList<TurnedBox> Wrecks();

    /// <summary>The turned box <paramref name="piece"/> is hit in (<see cref="DebrisModule.ShapeOf"/>); none for a piece caught by hand.</summary>
    TurnedBox? Shape(Piece piece);

    /// <summary>Whether a hit of <paramref name="damage"/> on wreck <paramref name="piece"/> breaks it: what <see cref="IDebrisOps.Hit"/> decides by.</summary>
    bool Breaks(int piece, int damage);

    /// <summary>The harpoons' lines, in the order hooked; live and read-only.</summary>
    IReadOnlyList<Line> Lines { get; }

    /// <summary>The line on piece <paramref name="piece"/>; null for none.</summary>
    Line? LineOn(int piece);

    /// <summary>The line hero <paramref name="peer"/> holds; null for none.</summary>
    Line? LineOf(int peer);
}

/// <summary>
/// A box turned about its middle (debris-physics): thing <paramref name="Piece"/>'s (a wreck's id, or a creature's for its body),
/// its middle at (<paramref name="X"/>, <paramref name="Y"/>), <paramref name="HalfW"/> by <paramref name="HalfH"/> either way along
/// its own axes, all in thousandths of a cell, turned so its own x runs along (<paramref name="Cos"/>, <paramref name="Sin"/>), in
/// thousandths (Core's sine table). Every product is a <c>long</c>, truncated toward zero: the sim's and the client's hit tests alike.
/// </summary>
public readonly record struct TurnedBox(int Piece, int X, int Y, int HalfW, int HalfH, int Cos, int Sin)
{
    private const int Unit = Rays.Cell;

    /// <summary>The box of <paramref name="piece"/> at (<paramref name="x"/>, <paramref name="y"/>) turned by <paramref name="angle"/>, in 65536ths of a turn.</summary>
    public static TurnedBox Of(int piece, int x, int y, int halfW, int halfH, int angle)
    {
        var (cos, sin) = Direction(angle);
        return new TurnedBox(piece, x, y, halfW, halfH, cos, sin);
    }

    /// <summary>The way an angle in 65536ths of a turn points, in thousandths: Core's fine table, 4096 headings to the turn.</summary>
    public static (int Cos, int Sin) Direction(int angle) => Rays.FineDirection((angle & 0xFFFF) >> 4);

    /// <summary>The point (<paramref name="localX"/>, <paramref name="localY"/>) of a frame turned by <paramref name="angle"/> as an offset in the world's.</summary>
    public static (int X, int Y) Turn(int angle, int localX, int localY)
    {
        var (cos, sin) = Direction(angle);
        return Turned(cos, sin, localX, localY);
    }

    private static (int X, int Y) Turned(int cos, int sin, long x, long y) => ((int)((x * cos - y * sin) / Unit), (int)((x * sin + y * cos) / Unit));

    /// <summary>The point (<paramref name="localX"/>, <paramref name="localY"/>) of the box's own frame, from its middle, in the world.</summary>
    public (int X, int Y) At(int localX, int localY)
    {
        var (dx, dy) = Turned(Cos, Sin, localX, localY);
        return (X + dx, Y + dy);
    }

    /// <summary>The world's point (<paramref name="x"/>, <paramref name="y"/>) in the box's own frame, from its middle: <see cref="At"/> turned back.</summary>
    public (int X, int Y) Local(int x, int y)
    {
        long dx = (long)x - X, dy = (long)y - Y;
        return ((int)((dx * Cos + dy * Sin) / Unit), (int)((dy * Cos - dx * Sin) / Unit));
    }

    /// <summary>How far the point (<paramref name="x"/>, <paramref name="y"/>) is from the box, in thousandths of a cell, rounded down: 0 inside, on an edge too.</summary>
    public int Gap(int x, int y)
    {
        var (u, v) = Local(x, y);
        long ex = Math.Max(0, Math.Abs((long)u) - HalfW), ey = Math.Max(0, Math.Abs((long)v) - HalfH);
        return (int)Root(ex * ex + ey * ey);
    }

    /// <summary>
    /// Whether the box covers part of <paramref name="cell"/> of a grid <paramref name="width"/> cells wide: no axis of either
    /// separates them, a box only touching the cell's edge not covering it. Exact, every term a whole <c>long</c>.
    /// </summary>
    public bool Covers(int cell, int width)
    {
        const long half = Unit / 2;
        long dx = X - (cell % width * (long)Unit + half), dy = Y - (cell / width * (long)Unit + half);
        long c = Math.Abs((long)Cos), s = Math.Abs((long)Sin);
        return Math.Abs(dx) * Unit < half * Unit + HalfW * c + HalfH * s
            && Math.Abs(dy) * Unit < half * Unit + HalfW * s + HalfH * c
            && Math.Abs(dx * Cos + dy * Sin) < HalfW * (long)Unit + half * (c + s)
            && Math.Abs(dy * Cos - dx * Sin) < HalfH * (long)Unit + half * (c + s);
    }

    /// <summary>The whole square root of <paramref name="n"/>, rounded down; 0 for none.</summary>
    public static long Root(long n)
    {
        if (n <= 0) return 0;
        long x = 1L << (System.Numerics.BitOperations.Log2((ulong)n) / 2 + 1), y = (x + n / x) / 2;
        while (y < x) (x, y) = (y, (y + n / y) / 2);
        return x;
    }
}

/// <summary>How another module may change the debris.</summary>
public interface IDebrisOps
{
    /// <summary>The line on piece <paramref name="id"/> dropped, if it has one (a sentry cuts it): it moves on as it was. A piece gone is let be.</summary>
    void Unreel(int id);

    /// <summary>Wreck <paramref name="id"/> struck by peer <paramref name="peer"/> for <paramref name="damage"/>: true when it broke.</summary>
    bool Hit(int peer, int id, int damage);

    /// <summary>
    /// The wrecks broken that burst and have not gone off (fuel-drums), in the order broken, taken to go off now: those broken while
    /// they do wait for the next call.
    /// </summary>
    IReadOnlyList<PendingBurst> TakeBursting();

    /// <summary>
    /// Piece <paramref name="id"/> taken by a salvage drone (salvage-drones): gone, what it gives rolled as a net's catch is,
    /// recorded as found and counted obtained, with no popup; null for one gone.
    /// </summary>
    IReadOnlyList<(string Item, int Count)>? Catch(int id);
}

/// <summary>
/// The pieces of debris drifting by, catching them, hooking them on lines and breaking wreckage with weapons, in zones by the hull,
/// in its asteroid band and round every hero (ADRs 0006, 0009, 0034, 0044, 0052 and 0054; debris-physics).
/// </summary>
public sealed partial class DebrisModule : Module<DebrisState>, IDebrisQueries, IDebrisOps
{
    /// <summary>Thousandths of a cell: a piece's position and speed are in these.</summary>
    public const int Unit = 1000;

    private readonly IGridQueries _grid;
    private readonly ICrewQueries _crew;
    private readonly ICrewOps _crewOps;
    private readonly IRoomsQueries _rooms;
    private readonly IItemsQueries _held;
    private readonly IItemsOps _items;
    private readonly IKnowledgeQueries _knowledge;
    private readonly IKnowledgeOps _knowledgeOps;
    private readonly ISkyQueries _sky;

    public override string Id => "debris";

    public DebrisModule(IGridQueries grid, ICrewQueries crew, ICrewOps crewOps, IRoomsQueries rooms, IItemsQueries held, IItemsOps items, IKnowledgeQueries knowledge, IKnowledgeOps knowledgeOps,
        ISkyQueries sky)
    {
        _knowledge = knowledge;
        _knowledgeOps = knowledgeOps;
        _sky = sky;
        _grid = grid;
        _crew = crew;
        _crewOps = crewOps;
        _rooms = rooms;
        _held = held;
        _items = items;
        Handles<Grab>(c => GrabProblem(c.From, c.Piece), Apply);
        Handles<HewWrecks>(c => HewProblemOf(c.From, Struck(c.Tool, c.X, c.Y, c.Heading), c.Tool, running: true), Apply);
        Handles<HewPieces>(c => HewProblemOf(c.From, Listed(c.Pieces), c.Tool, running: true), Apply);
        Handles<Hook>(c => HookProblem(c.From, c.Piece), Apply);
        Handles<Wind>(c => LineProblem(c.From), Apply);
        Handles<LetGo>(c => LineProblem(c.From), Apply);
        Handles<Moved>(c => c.From == HostPlayer ? null : "debris.notHost", Apply);
        Handles<Collect>(c => CollectProblem(c.From, c.Machine), Apply);
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

    public void Unreel(int id) => DropLineOn(id);

    /// <summary>
    /// Wreck <paramref name="id"/> struck by peer <paramref name="peer"/> for <paramref name="damage"/>: the blow stamped with the
    /// debris clock (<see cref="Piece.Struck"/>), and at its hit points broken into its loot, floating where it was, anyone's. What
    /// a <see cref="HewWrecks"/> does to each wreck it strikes, and a shot to the ones it meets. A piece gone is let be. True when it broke.
    /// </summary>
    public bool Hit(int peer, int id, int damage) => Hit(peer, id, damage, cuts: false);

    /// <summary>
    /// <see cref="Hit(int, int, int)"/>, <paramref name="cuts"/> when a salvaging tool lands it (<c>Weapons.Salvages</c>): its damage
    /// counted in <see cref="Piece.Cut"/> too. Whatever breaks a piece with some cut rolls for the bonus, a shot's hit too.
    /// </summary>
    private bool Hit(int peer, int id, int damage, bool cuts)
    {
        int at = IndexOf(id);
        if (at < 0) return false;
        var piece = State.Pieces[at];
        bool breaks = Breaks(id, damage);
        piece.Damage += damage;
        if (cuts) piece.Cut += damage;
        piece.Struck = new Blow { By = peer, At = State.Clock };
        if (!breaks) return false;
        RemoveAt(at);
        // The drone watches its player salvage (drone-lessons): a crate counts as wreckage; a burst by no hero, for no one.
        if (peer != FloorStack.Nobody) _knowledgeOps.Did(peer, Content.Lessons.WreckBroken);
        if (piece.Delivery != 0 && piece.Item != Content.Research.Confiscated)
        {
            Unpack(piece, peer);
            return true;
        }
        if (piece.Delivery != 0) _knowledgeOps.Delivered(piece.Delivery);
        var loot = CutLoot(piece, LootOf(piece.Item, Random));
        Record(piece, loot);
        // Dropped by nobody, so whoever broke it may sweep it up at once, as anyone may; its worth is the breaker's (catcher-loot).
        _items.DropAt(FloorStack.Nobody, loot, CellOf(piece.X, piece.Y, _grid.Width));
        _crewOps.Loot(peer, loot);
        // A drum goes off where it was, by whoever broke it, when Creatures next takes the list (fuel-drums).
        if (Content.Wreckage.BurstOf(piece.Item) != null) State.Bursting.Add(new PendingBurst(CellOf(piece.X, piece.Y, _grid.Width), peer, piece.Item));
        return true;
    }

    public IReadOnlyList<PendingBurst> TakeBursting()
    {
        var taken = State.Bursting;
        if (taken.Count == 0) return [];
        State.Bursting = [];
        return taken;
    }

    /// <summary>
    /// Delivery <paramref name="piece"/>, caught or broken by <paramref name="peer"/>, gone from the queue and given (2026-11-08-mafia-deliveries):
    /// an upgrade or blueprint known to the crew, shown to that peer as loot where the piece was; gear falling under the piece as a
    /// wreck's loot does, anyone's. Recorded as found by no one: it is a purchase, not salvage.
    /// </summary>
    private void Unpack(Piece piece, int peer)
    {
        var good = _knowledge.Deliveries.FirstOrDefault(d => d.Id == piece.Delivery)?.Good;
        _knowledgeOps.Delivered(piece.Delivery);
        if (good == null) return;
        if (!Content.Research.GearSold.Contains(good, StringComparer.Ordinal))
        {
            _knowledgeOps.Grant(good);
            if (_crew.HeroOf(peer) != null) _items.NoteDelivered(peer, good, CellOf(piece.X, piece.Y, _grid.Width));
            return;
        }
        _items.DropAt(FloorStack.Nobody, [(good, 1)], CellOf(piece.X, piece.Y, _grid.Width));
    }

    /// <summary>
    /// <paramref name="loot"/>, the draw of broken <paramref name="piece"/>, half again, rounded down a line, with a chance of its
    /// <see cref="Piece.Cut"/> in its hit points: drawn only for a piece some cut, so a world no cutter touched draws as before.
    /// </summary>
    private IReadOnlyList<(string Item, int Count)> CutLoot(Piece piece, IReadOnlyList<(string Item, int Count)> loot)
    {
        if (piece.Cut <= 0) return loot;
        int hp = Content.Wreckage.HpOf(piece.Item)!.Value;
        if (Random.Below((ulong)hp) >= (ulong)piece.Cut) return loot;
        return [.. loot.Select(l => (l.Item, l.Count * Content.Weapons.CutBonus / 1000))];
    }

    public IReadOnlyList<(string Item, int Count)>? Catch(int id)
    {
        int at = IndexOf(id);
        if (at < 0) return null;
        var piece = State.Pieces[at];
        RemoveAt(at);
        var loot = LootOf(piece.Item, Random);
        Record(piece, loot);
        _items.Caught(loot);
        return loot;
    }

    /// <inheritdoc/>
    public bool Breaks(int piece, int damage) =>
        PieceById(piece) is { } p && p.Damage + damage >= (Content.Wreckage.HpOf(p.Item) ?? throw new InvalidOperationException($"debris.hit: {p.Item} is no wreck"));

    /// <summary>
    /// What the last catch of piece <paramref name="id"/>, by peer <paramref name="by"/> or by anyone, put into its catcher's
    /// inventory, by item, or null when no such catch is among the last <see cref="CaughtKept"/>: a grab of it drifting, or a
    /// collect of it out of a net, each a new list. For the client to show its own catch over its hero (none if all fell).
    /// </summary>
    public IReadOnlyList<(string Item, int Count)>? CaughtFrom(int id, int? by = null)
    {
        for (int i = _caught.Count - 1; i >= 0; i--)
            if (_caught[i].Piece == id && (by == null || _caught[i].By == by)) return _caught[i].Gave;
        return null;
    }

    /// <summary>Notes that catching piece <paramref name="id"/> put <paramref name="gave"/> into peer <paramref name="by"/>'s inventory (<see cref="CaughtFrom"/>).</summary>
    private void Report(int id, int by, IReadOnlyList<(string Item, int Count)> gave)
    {
        if (_caught.Count == CaughtKept) _caught.RemoveAt(0);
        _caught.Add((id, by, gave));
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
    public IReadOnlyList<TurnedBox> Wrecks()
    {
        var wrecks = new List<TurnedBox>();
        foreach (var p in State.Pieces)
            if (ShapeOf(p) is { } shape && Outside(CellOf(p.X, p.Y, _grid.Width))) wrecks.Add(shape);
        return wrecks;
    }

    public TurnedBox? Shape(Piece piece) => ShapeOf(piece);

    /// <summary>
    /// The box <paramref name="piece"/> is hit in (debris-physics): its kind's size (<see cref="Content.Wreckage.SizeOf"/>) about
    /// its middle, turned by its <see cref="Piece.Angle"/>; none for a piece caught by hand, which has no box.
    /// </summary>
    public static TurnedBox? ShapeOf(Piece piece) => Content.Wreckage.SizeOf(piece.Item) is var (w, h)
        ? TurnedBox.Of(piece.Id, piece.X, piece.Y, w * Unit / 2, h * Unit / 2, piece.Angle)
        : null;

    /// <summary>
    /// How a new piece <paramref name="id"/> of <paramref name="item"/> starts turned and tumbles (debris-physics): from its id alone, a
    /// splitmix64 step, so every peer and every migration gives it the same. Its angle any; its spin either way, from half its fastest
    /// to it, never under <c>TumbleMin</c>; its fastest <c>TumbleMax</c> up to <c>TumbleSide</c> cells, falling as one over its longer side.
    /// </summary>
    public static (int Angle, int Spin) TumbleOf(int id, string item)
    {
        ulong z = unchecked((ulong)id + 0x9E3779B97F4A7C15UL);
        z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
        z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
        z ^= z >> 31;
        const int fastest = Content.Debris.TumbleMax, slowest = Content.Debris.TumbleMin, small = Content.Debris.TumbleSide;
        int side = Content.Wreckage.SizeOf(item) is var (w, h) ? Math.Max(w, h) : 0;
        int most = Math.Max(slowest, side <= small ? fastest : fastest * small / side);
        int least = Math.Max(slowest, most / 2);
        int spin = least + (int)((z >> 16) % (ulong)(most - least + 1));
        return ((int)(z & 0xFFFF), (z >> 48 & 1) == 0 ? spin : -spin);
    }

    /// <summary>
    /// Why peer <paramref name="peer"/>'s hit of <paramref name="tool"/> (<see cref="HewWrecks"/>) would be refused: the hero
    /// is not in space, the fan strikes no wreck, or the weapon is electric and no battery on the belt can pay a tick of its draw
    /// (<c>items.flat</c>). Any weapon breaks any wreck (0054). What the client asks before it sends: the command is refused
    /// besides while an electric weapon is not running (<c>items.notRunning</c>, <see cref="IItemsQueries.WeaponProblem"/>).
    /// </summary>
    public string? HewProblem(int peer, string tool, int x, int y, int heading) =>
        HewProblemOf(peer, Struck(tool, x, y, heading), tool, running: false);

    /// <summary>Why peer <paramref name="peer"/>'s <see cref="HewPieces"/> of <paramref name="tool"/> on <paramref name="pieces"/> would be refused, as <see cref="HewProblem"/> asks it.</summary>
    public string? HewPiecesProblem(int peer, string tool, IReadOnlyList<int> pieces) =>
        HewProblemOf(peer, Listed(pieces), tool, running: false);

    /// <summary>The wrecks in space among <paramref name="ids"/>, each once, ascending by id.</summary>
    private List<Piece> Listed(IReadOnlyList<int> ids)
    {
        var listed = new List<Piece>();
        foreach (var w in Wrecks())
            if (ids.Contains(w.Piece)) listed.Add(PieceById(w.Piece)!);
        listed.Sort((a, b) => a.Id.CompareTo(b.Id));
        return listed;
    }

    private string? HewProblemOf(int peer, IReadOnlyList<Piece> struck, string tool, bool running)
    {
        if (_crew.HeroOf(peer) is not { } hero || !Outside(hero.Cell)) return "debris.notOutside";
        return struck.Count == 0 ? "debris.nothingToHit" : _held.WeaponProblem(peer, tool, running);
    }

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
        RemoveAt(at);
        var hero = _crew.HeroOf(c.From) ?? throw new InvalidOperationException("debris.grab: applied with no hero");
        if (piece.Delivery != 0)
        {
            Unpack(piece, c.From);
            return;
        }
        var loot = LootOf(piece.Item, Random);
        // From the roll, not from what went in: what floats off a full inventory was seen all the same.
        Record(piece, loot);
        Report(piece.Id, c.From, _items.GiveOrDrop(c.From, loot, hero.Cell, noted: false));
        _crewOps.Loot(c.From, loot);
    }

    private void Apply(HewWrecks c)
    {
        int hit = _held.Damage(c.From, c.Tool);
        bool cuts = Content.Weapons.Of(c.Tool) is { } weapon && Content.Weapons.Salvages(weapon);
        foreach (var piece in Struck(c.Tool, c.X, c.Y, c.Heading)) Hit(c.From, piece.Id, hit, cuts);
    }

    private void Apply(HewPieces c)
    {
        int hit = _held.Damage(c.From, c.Tool);
        bool cuts = Content.Weapons.Of(c.Tool) is { } weapon && Content.Weapons.Salvages(weapon);
        foreach (var piece in Listed(c.Pieces)) Hit(c.From, piece.Id, hit, cuts);
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
}
