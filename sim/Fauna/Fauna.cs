using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content.Fauna;

namespace Stillwake.Sim.Fauna;

/// <summary>
/// Strikes creature <c>Creature</c> with weapon <c>Tool</c>, by its item: takes the tool's damage (<c>ToolStats</c>, 0098) off
/// its hit points and knocks it away from the striker's reported cell, flinching, then fleeing; at none left it dies, its yields
/// falling where it was. An electric weapon hits only while the sender's hero runs it (0055). Whether the sender holds it, how
/// often it strikes and how far it reaches is the client's word (0034, 0054).
/// </summary>
public sealed record Strike(int Creature, string Tool) : Command;

/// <summary>
/// Lets a creature of species <c>Species</c> out standing in cell <c>Cell</c>, its body's bottom left: a dev command (<c>--fauna</c>),
/// as opening a surface is. Refused aboard the ship (<c>fauna.notHere</c>), at the surface's cap (<c>fauna.full</c>), and where
/// its body would not fit (<c>fauna.blocked</c>).
/// </summary>
public sealed record Release(string Species, int Cell) : Command;

/// <summary>
/// Creatures living on a planet's surface (0102): spawned out of sight round every hero by the habitats of its type, walking,
/// idling, struck, knocked, fleeing and dying into their yields, gone again when no hero is near. The ship has none.
/// </summary>
public sealed partial class FaunaModule : Module<FaunaState>
{
    /// <summary>Thousandths of a cell: a creature's position and speed are in these.</summary>
    public const int Unit = 1000;

    private readonly IGridQueries _grid;
    private readonly ICrewQueries _crew;
    private readonly IItemsQueries _held;
    private readonly IItemsOps _items;

    public override string Id => "fauna";

    public FaunaModule(IGridQueries grid, ICrewQueries crew, IItemsQueries held, IItemsOps items)
    {
        _grid = grid;
        _crew = crew;
        _held = held;
        _items = items;
        Handles<Strike>(c => StrikeProblem(c.From, c.Creature, c.Tool) ?? _held.WeaponProblem(c.From, c.Tool, running: true), Apply);
        Handles<Release>(c => ReleaseProblem(c.Species, c.Cell), Apply);
        // After crew in the simulation phase: where heroes are is this tick's.
        System(Phase.Simulation, Live);
    }

    protected override FaunaState Init(Layout layout) => new();

    // Queries.

    /// <summary>The creatures, in id order; live and read-only.</summary>
    public IReadOnlyList<Creature> Creatures => State.Creatures;

    /// <summary>Creature <paramref name="id"/>; null for one gone or never spawned.</summary>
    public Creature? CreatureById(int id)
    {
        int at = IndexOf(id);
        return at < 0 ? null : State.Creatures[at];
    }

    /// <summary>
    /// Why peer <paramref name="peer"/>'s strike of creature <paramref name="id"/> with <paramref name="tool"/> would be refused: it
    /// is gone (<c>fauna.gone</c>), or the tool is electric and no battery on the belt can pay a tick of its draw (<c>items.flat</c>).
    /// What the client asks before it sends; the command is refused besides while an electric tool is not running
    /// (<c>items.notRunning</c>).
    /// </summary>
    public string? StrikeProblem(int peer, int id, string tool)
    {
        if (Content.Weapons.Of(tool) == null) throw new InvalidOperationException($"fauna.strike: {tool} deals no damage");
        return IndexOf(id) < 0 ? "fauna.gone" : _held.WeaponProblem(peer, tool, running: false);
    }

    /// <summary>Why a release of <paramref name="species"/> in <paramref name="cell"/> would be refused.</summary>
    public string? ReleaseProblem(string species, int cell)
    {
        var spec = Spec.Get(species);
        if (Rules.Gravity == null) return "fauna.notHere";
        if (State.Creatures.Count >= Spec.Max) return "fauna.full";
        int width = _grid.Width;
        if (cell < 0 || cell >= width * _grid.Height) return "fauna.blocked";
        var (x, y) = Standing(spec, cell % width, cell / width);
        return Overlaps(spec, x, y) ? "fauna.blocked" : null;
    }

    /// <summary>The bottom middle of a body of <paramref name="spec"/> whose bottom left cell is (<paramref name="column"/>, <paramref name="row"/>).</summary>
    internal static (int X, int Y) Standing(Spec.Species spec, int column, int row) =>
        (column * Unit + spec.Width * Unit / 2, (row + 1) * Unit);

    // Commands.

    private void Apply(Strike c)
    {
        int at = IndexOf(c.Creature);
        if (at < 0) throw new InvalidOperationException("fauna.strike: applied to a creature that is gone");
        var creature = State.Creatures[at];
        var spec = Spec.Get(creature.Species);
        creature.Damage += _held.Damage(c.From, c.Tool);
        if (creature.Damage >= spec.Hp)
        {
            State.Creatures.RemoveAt(at);
            // From its top row, so what it gives falls to where it stood; anyone may sweep it up at once.
            _items.DropAt(FloorStack.Nobody, Loots.Roll(spec.Yields, Random), TopCell(creature, spec));
            return;
        }
        int away = AwayFrom(creature, _crew.HeroOf(c.From)?.Cell);
        // A tool's pace is its weapon's (0098): the drill's quick hits each knock a share, rounded toward none.
        int pace = Math.Min(Spec.KnockPace, Content.Weapons.Of(c.Tool)!.Pace);
        // Stunned, it steers nothing a while, then flees; with no stun, a neutral one, it bolts at once, the knock's way, at its
        // flight's speed or the knock's if more: hops off quick hits never ground it long enough to steer, so it runs from the first.
        bool stunned = spec.Stun > 0;
        int knock = Math.Max(spec.Knock * pace / Spec.KnockPace, stunned ? 0 : spec.Flee);
        (creature.Mode, creature.ModeLeft) = stunned ? (Mode.Hurt, spec.Stun) : (Mode.Flee, spec.FleeTicks);
        // A blow never slows one already running the knock's way: a run of the drill's quick hits cannot hold it.
        creature.VX = away * (Math.Sign(creature.VX) == away ? Math.Max(Math.Abs(creature.VX), knock) : knock);
        // No hop while it rises already: a run of quick hits keeps it low rather than juggling it up.
        if (creature.VY >= 0) creature.VY = -spec.Hop * pace / Spec.KnockPace;
    }

    private void Apply(Release c)
    {
        var spec = Spec.Get(c.Species);
        var (x, y) = Standing(spec, c.Cell % _grid.Width, c.Cell / _grid.Width);
        Add(spec, x, y);
    }

    /// <summary>A creature of <paramref name="spec"/> standing at (<paramref name="x"/>, <paramref name="y"/>), idling, seen now: its id the next.</summary>
    private void Add(Spec.Species spec, int x, int y)
    {
        State.Creatures.Add(new Creature
        {
            Id = State.NextId,
            Species = spec.Id,
            X = x,
            Y = y,
            Mode = Mode.Idle,
            ModeLeft = Spec.IdleLeast,
            SeenAt = State.Clock,
        });
        State.NextId += 1;
    }

    /// <summary>The cell of <paramref name="c"/>'s top row under its middle: on the grid, and open, as its body is.</summary>
    private int TopCell(Creature c, Spec.Species spec) => Down(c.Y - spec.Height * Unit, Unit) * _grid.Width + Down(c.X, Unit);

    /// <summary>
    /// Which way across <paramref name="c"/> goes away from a hero reported at <paramref name="cell"/>: the sign of its middle less the
    /// cell's; level with it, or with no hero on the grid, back against its way (right when still).
    /// </summary>
    private int AwayFrom(Creature c, int? cell)
    {
        if (cell is int at && at >= 0 && at < _grid.Width * _grid.Height)
        {
            int dx = c.X - (at % _grid.Width * Unit + Unit / 2);
            if (dx != 0) return Math.Sign(dx);
        }
        return c.VX > 0 ? -1 : 1;
    }

    /// <summary>The binary search of <paramref name="id"/> among the creatures, in id order as they spawned; -1 for none.</summary>
    private int IndexOf(int id)
    {
        var all = State.Creatures;
        int lo = 0, hi = all.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2, at = all[mid].Id;
            if (at == id) return mid;
            if (at < id) lo = mid + 1;
            else hi = mid - 1;
        }
        return -1;
    }

    /// <summary>Division rounding down, for a point above or left of the grid.</summary>
    internal static int Down(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);
}
