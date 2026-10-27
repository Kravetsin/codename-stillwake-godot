using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// Strikes creature <c>Creature</c> with weapon <c>Tool</c>, by its item: takes the tool's damage (<c>ToolStats</c>, 0098) off
/// its hit points and knocks it away from the striker's reported cell, flinching, then fleeing; at none left it dies, its yields
/// falling where it was. An electric weapon hits only while the sender's hero runs it (0055). Whether the sender holds it, how
/// often it strikes and how far it reaches is the client's word (0034, 0054).
/// </summary>
public sealed record Strike(int Creature, string Tool) : Command;

/// <summary>
/// Lets a creature of species <c>Species</c> out standing in cell <c>Cell</c>, its body's bottom left: a dev command (<c>--creatures</c>),
/// as opening a surface is. Refused aboard the ship (<c>creatures.notHere</c>), at the surface's cap (<c>creatures.full</c>), and where
/// its body would not fit (<c>creatures.blocked</c>).
/// </summary>
public sealed record Release(string Species, int Cell) : Command;

/// <summary>
/// The sender's hero was hit by creature <c>Creature</c>'s charge (0103), as its client saw them touch: its suit pays the species'
/// <c>Ram</c> (<see cref="ICrewOps.Shock"/>) and the creature recoils, fleeing, so a charge hits once. Refused for a creature gone
/// (<c>creatures.gone</c>) or not charging (<c>creatures.notAttacking</c>): a second hero it touched on the same charge pays nothing.
/// </summary>
public sealed record HitBy(int Creature) : Command;

/// <summary>
/// Creatures living on a planet's surface (0102): spawned out of sight round every hero by the habitats of its type, walking,
/// idling, struck, knocked, fleeing and dying into their yields, gone again when no hero is near. The ship has none.
/// </summary>
public sealed partial class CreaturesModule : Module<CreaturesState>
{
    /// <summary>Thousandths of a cell: a creature's position and speed are in these.</summary>
    public const int Unit = 1000;

    private readonly IGridQueries _grid;
    private readonly ICrewQueries _crew;
    private readonly ICrewOps _crewOps;
    private readonly IItemsQueries _held;
    private readonly IItemsOps _items;
    private readonly IDebrisQueries _debris;
    private readonly IDebrisOps _debrisOps;
    private readonly IBuildingOps _building;

    public override string Id => "creatures";

    public CreaturesModule(IGridQueries grid, ICrewQueries crew, ICrewOps crewOps, IItemsQueries held, IItemsOps items, IDebrisQueries debris, IDebrisOps debrisOps,
        IBuildingOps building)
    {
        _debris = debris;
        _debrisOps = debrisOps;
        _building = building;
        _grid = grid;
        _crew = crew;
        _crewOps = crewOps;
        _held = held;
        _items = items;
        Handles<Strike>(c => StrikeProblem(c.From, c.Creature, c.Tool) ?? _held.WeaponProblem(c.From, c.Tool, running: true), Apply);
        Handles<Release>(c => ReleaseProblem(c.Species, c.Cell), Apply);
        Handles<HitBy>(c => HitByProblem(c.Creature), Apply);
        Handles<Fire>(c => FireProblem(c.From, c.Tool, c.X, c.Y), Apply);
        Handles<Bolted>(c => BoltedProblem(c.From, c.Arc), Apply);
        // After crew in the simulation phase: where heroes are is this tick's.
        System(Phase.Simulation, Live);
        // After the creatures move, and after the debris drifts (registered before): a shot meets them where they are now.
        System(Phase.Simulation, Fly);
    }

    protected override CreaturesState Init(Layout layout) => new();

    // Queries.

    /// <summary>The creatures, in id order; live and read-only. One killed by a command is in it till the system sweeps it, within the tick.</summary>
    public IReadOnlyList<Creature> Creatures => State.Creatures;

    /// <summary>
    /// Why peer <paramref name="peer"/>'s strike of creature <paramref name="id"/> with <paramref name="tool"/> would be refused: it
    /// is gone (<c>creatures.gone</c>), or the tool is electric and no battery on the belt can pay a tick of its draw (<c>items.flat</c>).
    /// What the client asks before it sends; the command is refused besides while an electric tool is not running
    /// (<c>items.notRunning</c>).
    /// </summary>
    public string? StrikeProblem(int peer, int id, string tool)
    {
        if (Content.Weapons.Of(tool) == null) throw new InvalidOperationException($"creatures.strike: {tool} deals no damage");
        return Alive(id) < 0 ? "creatures.gone" : _held.WeaponProblem(peer, tool, running: false);
    }

    /// <summary>
    /// Why a hit by creature <paramref name="id"/>'s charge would be refused: it is gone (<c>creatures.gone</c>) or not charging
    /// (<c>creatures.notAttacking</c>), its charge spent on another hero or ended. What the client asks before it sends.
    /// </summary>
    public string? HitByProblem(int id)
    {
        int at = Alive(id);
        if (at < 0) return "creatures.gone";
        var c = State.Creatures[at];
        return c.Act is { } act && RuleBook.Move(Spec.Get(c.Species).Moves[act.Move]).Hits(act) ? null : "creatures.notAttacking";
    }

    /// <summary>Whether <paramref name="c"/> is alerted: a guard that has noticed a hero and will pounce.</summary>
    public static bool Alerted(Creature c) => Spec.Get(c.Species).Brain[c.Drive] is Spec.Guard && c.Act == null && c.Step == GuardDrive.Alerted;

    /// <summary>Whether <paramref name="c"/> is clamped on to its wreck: a clinger not flying.</summary>
    public static bool Clamped(Creature c) => Spec.Get(c.Species).Body is Spec.Clinger && c.Recall(ClingerBody.Mounted) == 1;

    /// <summary>The creatures' clock when <paramref name="c"/> last cut a harpoon line, plus one; 0 for never.</summary>
    public static int LastCut(Creature c) => c.Recall(CutTetherDrive.Cut);

    /// <summary>
    /// How many ticks till <paramref name="c"/> cuts the harpoon line on its wreck, and how many it takes in all; (0, 0) when it is
    /// cutting none.
    /// </summary>
    public (int Left, int Of) CutLeft(Creature c) =>
        CutTetherDrive.Cutting(c) && Spec.Get(c.Species).Brain.OfType<Spec.CutTether>().FirstOrDefault() is { } cut
            ? (Math.Max(0, c.Recall(CutTetherDrive.CutsAt) - State.Clock), cut.Ticks)
            : (0, 0);

    /// <summary>The piece <paramref name="c"/> clings to, by its id: what a clinger was let out on; null for any other.</summary>
    public int? ClingsTo(Creature c) => Spec.Get(c.Species).Body is Spec.Clinger && c.Recall(ClingerBody.Piece) is int piece and > 0 ? piece : null;

    /// <summary>
    /// Whether peer <paramref name="peer"/>'s hero is threatened: within <c>Creatures.ThreatReach</c> cells of it (box distance, by its
    /// reported cell) a creature acts, retaliates, or is of a faction that hunts the crew. What its client asks to report where it
    /// is every tick it moves (2026-10-26-creatures-are-composed); false with no hero on the grid.
    /// </summary>
    public bool Threatens(int peer)
    {
        if (_crew.HeroOf(peer)?.Cell is not int cell || cell < 0 || cell >= _grid.Width * _grid.Height) return false;
        int hx = cell % _grid.Width, hy = cell / _grid.Width;
        foreach (var c in State.Creatures)
        {
            if (c.Dead || Math.Max(Math.Abs(Down(c.X, Unit) - hx), Math.Abs(Down(c.Y - 1, Unit) - hy)) > Spec.ThreatReach) continue;
            var spec = Spec.Get(c.Species);
            if (c.Act != null || RuleBook.Drive(spec.Brain[c.Drive]).Menaces || Hunts(spec.Faction, Spec.Crew)) return true;
        }
        return false;
    }

    /// <summary>Whether faction <paramref name="from"/> hunts faction <paramref name="to"/> (<c>Creatures.Relations</c>; none listed: it ignores it).</summary>
    private static bool Hunts(string from, string to)
    {
        foreach (var r in Spec.Relations)
            if (r.From == from && r.To == to) return r.Stance == Spec.Stance.Hunt;
        return false;
    }

    /// <summary>Why a release of <paramref name="species"/> in <paramref name="cell"/> would be refused.</summary>
    public string? ReleaseProblem(string species, int cell)
    {
        var spec = Spec.Get(species);
        if (cell < 0 || cell >= _grid.Width * _grid.Height) return "creatures.blocked";
        if (State.Creatures.Count(c => !c.Dead) >= Spec.Max) return "creatures.full";
        return RuleBook.Body(spec.Body).ReleaseProblem(this, spec, cell);
    }

    /// <summary>The bottom middle of a body of <paramref name="spec"/> whose bottom left cell is (<paramref name="column"/>, <paramref name="row"/>).</summary>
    internal static (int X, int Y) Standing(Spec.Species spec, int column, int row) =>
        (column * Unit + spec.Body.Width * Unit / 2, (row + 1) * Unit);

    // Commands.

    private void Apply(Strike c)
    {
        int at = Alive(c.Creature);
        if (at < 0) throw new InvalidOperationException("creatures.strike: applied to a creature that is gone");
        var creature = State.Creatures[at];
        var spec = Spec.Get(creature.Species);
        // A tool's pace is its weapon's (0098): the drill's quick hits each knock a share, rounded toward none.
        Hurt(creature, spec, _held.Damage(c.From, c.Tool), new Hit(c.From, AwayFrom(creature, _crew.HeroOf(c.From)?.Cell),
            Math.Min(Spec.KnockPace, Content.Weapons.Of(c.Tool)!.Pace), _debris.Clock + 1));
    }

    /// <summary>
    /// The hit path (2026-10-26-creatures-are-composed): <paramref name="damage"/> taken; at its hit points it dies; else knocked,
    /// its act cut short, and its drives told in brain order till one claims it. True when it died.
    /// </summary>
    private bool Hurt(Creature c, Spec.Species spec, int damage, in Hit hit)
    {
        bool dies = Dies(c, damage);
        c.Damage += damage;
        if (dies)
        {
            Kill(c, spec);
            return true;
        }
        // A blow never slows one already going the knock's way: a run of the drill's quick hits cannot hold it.
        // A body never knocked (no knock, no hop) keeps its way: a sentry in flight flies on.
        int knock = spec.Life.Knock * hit.Pace / Spec.KnockPace;
        if (spec.Life.Knock > 0) c.VX = hit.Away * (Math.Sign(c.VX) == hit.Away ? Math.Max(Math.Abs(c.VX), knock) : knock);
        // No hop while it rises already: a run of quick hits keeps it low rather than juggling it up.
        if (spec.Life.Hop > 0 && c.VY >= 0) c.VY = -spec.Life.Hop * hit.Pace / Spec.KnockPace;
        if (c.Act is { } act && RuleBook.Move(spec.Moves[act.Move]).Interruptible) c.Act = null;
        for (int i = 0; i < spec.Brain.Count; i++)
            if (RuleBook.Drive(spec.Brain[i]).Hurt(this, c, spec, spec.Brain[i], i, hit)) break;
        return false;
    }

    /// <summary>Whether <paramref name="damage"/> more kills <paramref name="c"/>: what <see cref="Hurt"/> decides by.</summary>
    private static bool Dies(Creature c, int damage) => c.Damage + damage >= Spec.Get(c.Species).Life.Hp;

    /// <summary><paramref name="c"/> dies, giving what it gives: struck down, or gone with the wreck it clung to.</summary>
    internal void Kill(Creature c, Spec.Species spec)
    {
        c.Dead = true;
        // From its top row, so what it gives falls to where it stood; anyone may sweep it up at once.
        _items.DropAt(FloorStack.Nobody, Loots.Roll(spec.Yields, Random), TopCell(c, spec));
    }

    private void Apply(HitBy c)
    {
        var creature = State.Creatures[Alive(c.Creature)];
        var spec = Spec.Get(creature.Species);
        // A ram may take the suit's last unit: the hero passes out, as the cold or the lava would leave it.
        _crewOps.Shock(c.From, spec.Moves[creature.Act!.Move].Damage);
        // It recoils off the hero it struck, hopping back the way it came, its act spent: the hero thrown on, the two part at
        // once. Read from the creature alone.
        EndAct(creature, spec, creature.VX >= 0 ? -1 : 1, hop: true);
    }

    private void Apply(Release c)
    {
        var spec = Spec.Get(c.Species);
        RuleBook.Body(spec.Body).Place(this, Add(spec, 0, 0), spec, c.Cell);
    }

    /// <summary>
    /// A creature of <paramref name="spec"/> standing at (<paramref name="x"/>, <paramref name="y"/>), in the charge of the first drive
    /// that wants it, seen now: its id the next.
    /// </summary>
    private Creature Add(Spec.Species spec, int x, int y)
    {
        var c = new Creature { Id = State.NextId, Species = spec.Id, X = x, Y = y, SeenAt = State.Clock };
        c.Drive = Fallback(spec, c);
        RuleBook.Drive(spec.Brain[c.Drive]).Born(c, spec.Brain[c.Drive]);
        State.Creatures.Add(c);
        State.NextId += 1;
        return c;
    }

    /// <summary>The cell of <paramref name="c"/>'s top row under its middle: on the grid, and open, as its body is.</summary>
    private int TopCell(Creature c, Spec.Species spec) => Down(c.Y - spec.Body.Height * Unit, Unit) * _grid.Width + Down(c.X, Unit);

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

    /// <summary>Whether a hero reported at <paramref name="cell"/> is within <paramref name="reach"/> cells of <paramref name="c"/> by box distance; none on the grid is not.</summary>
    internal bool Near(Creature c, int? cell, int reach)
    {
        if (cell is not int at || at < 0 || at >= _grid.Width * _grid.Height) return false;
        int x = Down(c.X, Unit), y = Down(c.Y - 1, Unit);
        return Math.Max(Math.Abs(x - at % _grid.Width), Math.Abs(y - at / _grid.Width)) <= reach;
    }

    /// <summary>Where creature <paramref name="id"/> is among the creatures; -1 for one gone, never spawned, or killed this tick.</summary>
    private int Alive(int id)
    {
        int at = IndexOf(id);
        return at >= 0 && !State.Creatures[at].Dead ? at : -1;
    }

    /// <summary>The cell peer <paramref name="peer"/>'s hero is reported in; null for none.</summary>
    internal int? HeroCell(int peer) => _crew.HeroOf(peer)?.Cell;

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
