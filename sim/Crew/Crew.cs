using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Knowledge;
using Stillwake.Sim.Players;
using Stillwake.Sim.Sky;

namespace Stillwake.Sim.Crew;

/// <summary>
/// The sender's hero is at <c>Cell</c>: sent when the hero enters another room as its client sees it,
/// and first of all when it wakes, which brings it in with its air full, bare on its peer's first hero; and as its report, at most every
/// <c>Crew.ReportEvery</c> ticks while its body is in another cell than the sim's (0050).
/// </summary>
public sealed record Enter(int Cell) : Command;

/// <summary>
/// The sender's peer has left the session: its hero goes, and its colour with it; off the ship, what it carried
/// waits aboard for its seat (0076). The host sends it on the peer's behalf, hearing it go.
/// </summary>
public sealed record Leave : Command;

/// <summary>The sender's hero puts on suit colour <c>Colour</c>, if no other hero wears it, on any surface.</summary>
public sealed record PickColour(int Colour) : Command;

/// <summary>
/// The sender chooses <c>Look</c> for its hero, worn from the pod's next print of it (2026-10-28-looks-are-printed): with a
/// hero or none yet, never refused. Its client sends it before its first <see cref="Enter"/> and whenever its player
/// changes the look.
/// </summary>
public sealed record ChooseLook(Look Look) : Command;

/// <summary>The sender's hero starts running (<c>On</c>), or stops: sent when it changes.</summary>
public sealed record Exert(bool On) : Command;

/// <summary>
/// The sender's hero starts thrusting with its jetpack (<c>On</c>), or stops: sent while its client's
/// thrust differs from the sim's, as <see cref="Exert"/> is.
/// </summary>
public sealed record Thrust(bool On) : Command;

/// <summary>The sender's hero breathes a canister it carries, refilling its air.</summary>
public sealed record Refill : Command;

/// <summary>
/// The sender's hero switches its suit's mode on (<c>On</c>) or off: V, or its key on the tablet's edge; on only in a body
/// with <c>Switch</c> (2026-11-03-the-regen-suit).
/// </summary>
public sealed record SuitMode(bool On) : Command;

/// <summary>What any module or the client may read of the heroes.</summary>
public interface ICrewQueries
{
    /// <summary>Where heroes are made: the lifepod's hatch, else the map's first S, or cell 0 on a map with neither.</summary>
    int Spawn { get; }
    /// <summary>Whether a hero in <paramref name="cell"/> draws the lifepod's air: within <c>Crew.PodReach</c> of it.</summary>
    bool ByPod(int cell);
    /// <summary>
    /// What the cold past the field takes from a hero's air a breath in <paramref name="cell"/>: nothing inside the field, the
    /// build zone grown by <c>Field.Margin</c>; past it more the deeper out, up to <c>Field.EdgeCost</c>
    /// at the world's edge and off the grid (0050).
    /// </summary>
    int EdgeCost(int cell);
    /// <summary>Hero <paramref name="id"/>, played by that peer; null while the sim has none (TypeScript's <c>hero</c>).</summary>
    Hero? HeroOf(int id);
    /// <summary>Every hero, in id order: live and read-only, for counting who breathes where.</summary>
    IReadOnlyList<Hero> Heroes { get; }
    /// <summary>
    /// The most air hero <paramref name="peer"/> holds: the <c>Tank</c> of the helmet it wears, or <c>Crew.Lungs</c> without one
    /// (2026-10-27-suits-are-worn). Its air is cut to it each tick.
    /// </summary>
    int CapacityOf(int peer);
    /// <summary>What a canister breathed gives any hero's air: <c>Crew.CanisterRefill</c>, and <c>Crew.CanisterUp</c> more for each canister upgrade bought.</summary>
    int CanisterRefill { get; }
    /// <summary>
    /// What the cold takes from hero <paramref name="peer"/>'s air a breath where it was last reported: the edge's cold, and
    /// with no body worn where the air is not aired <c>Crew.BareCold</c>, and at night with no back wall the sky's chill (each paid on
    /// its beat, fewer breaths than this says); 0 with no
    /// hero. What the frost is drawn at; the lava burns besides.
    /// </summary>
    int Cold(int peer);
    /// <summary>
    /// Thousandths of <see cref="Cold"/> hero <paramref name="peer"/>'s wear spares: its cape's <c>Cape.Warmth</c>, else 0
    /// (2026-10-27-the-cape-is-worn). The frost is drawn that much lighter.
    /// </summary>
    int Warmth(int peer);
    /// <summary>
    /// Whether hero <paramref name="peer"/>'s suit's mode works: switched on, in a body with <c>Switch</c>; false with no hero
    /// (2026-11-03-the-regen-suit).
    /// </summary>
    bool SuitOn(int peer);
    /// <summary>Why hero <paramref name="peer"/> could not switch its suit's mode on now: none, or no body with a mode; null when it could.</summary>
    string? SuitModeProblem(int peer);
}

/// <summary>How another module may change a hero.</summary>
public interface ICrewOps
{
    /// <summary>
    /// Hero <paramref name="peer"/>'s air pays <paramref name="units"/> at once, as the cold and the lava take it:
    /// a ruin's resonator zapping it (0091). Air run dry starts its last breath, which the rest cuts; nothing in creative mode, nor for a hero out or none.
    /// Counted as a hurt of <paramref name="harm"/> (2026-11-08-hurts-are-shown).
    /// </summary>
    void Shock(int peer, int units, Harm harm);

    /// <summary>
    /// An enemy's blow of <paramref name="units"/> on hero <paramref name="peer"/>: a creature's ram or pounce, its bolt. A worn
    /// cape spares its share (<c>Cape.Guard</c>); the rest is paid as <see cref="Shock"/> pays (2026-10-27-the-cape-is-worn). A hurt of <paramref name="harm"/>.
    /// </summary>
    void Blow(int peer, int units, Harm harm);
}

/// <summary>
/// Heroes inside the sim: where each one is, as its client says, and how it breathes — room air in a
/// cell holding it, its own air anywhere else (the suit's, or its lungs bare), passing out when that runs dry
/// (packages/sim/src/modules/crew).
/// </summary>
public sealed partial class CrewModule : Module<CrewState>, ICrewQueries, ICrewOps, ICarries
{
    public override string Id => "crew";

    private readonly IAtmosphereQueries _air;
    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IPlayersQueries _players;
    private readonly IPlayersOps _playersOps;
    private readonly IKnowledgeQueries _knowledge;
    private readonly ISkyQueries _sky;

    private static readonly IReadOnlyList<(string Item, int Count)> Canister = [(Content.Crew.Canister, 1)];

    public CrewModule(
        IAtmosphereQueries air, IAtmosphereOps atmosphere, IGridQueries grid, IItemsQueries items, IItemsOps itemsOps, IPlayersQueries players, IPlayersOps playersOps,
        IKnowledgeQueries knowledge, ISkyQueries sky)
    {
        _players = players;
        _playersOps = playersOps;
        _knowledge = knowledge;
        _sky = sky;
        _air = air;
        _grid = grid;
        _items = items;
        _itemsOps = itemsOps;
        Handles<Enter>(_ => null, c =>
        {
            var hero = HeroOf(c.From);
            if (hero != null)
            {
                hero.Cell = c.Cell;
                return;
            }
            hero = new Hero { Id = c.From, Cell = c.Cell };
            Add(hero);
            _playersOps.Dress(c.From);
            _playersOps.Print(c.From);
            _itemsOps.Kit(c.From);
            // Printed full in what it wears: bare as a peer's first hero, in its kept helmet coming back after Leave.
            hero.Air = CapacityOf(c.From);
        });
        Handles<Leave>(
            c => HeroOf(c.From) == null ? "crew.noHero" : null,
            c =>
            {
                State.Heroes.RemoveAll(h => h.Id == c.From);
                _playersOps.Undress(c.From);
                // Its inventory stays for its seat; a tool it ran would drain the belt with no one at it.
                _itemsOps.StopRunning(c.From);
                // Its player comes back aboard, whatever surface it left from: its inventory waits there (0076).
                if (Surfaces.Here != 0) Surfaces.Send(c.From, 0);
            });
        Handles<PickColour>(c => ColourProblem(c.From, c.Colour), c => _playersOps.Wear(c.From, c.Colour));
        Handles<ChooseLook>(_ => null, c => _playersOps.Choose(c.From, c.Look));
        Handles<Exert>(c => HeroOf(c.From) == null ? "crew.noHero" : null, c => HeroOf(c.From)!.Exerting = c.On);
        Handles<Thrust>(ThrustProblem, c => HeroOf(c.From)!.Thrusting = c.On);
        Handles<Refill>(c => RefillProblem(c.From), c =>
        {
            _itemsOps.Take(c.From, Canister);
            var hero = HeroOf(c.From)!;
            hero.Air = Math.Min(CapacityOf(c.From), hero.Air + CanisterRefill);
        });
        Handles<SuitMode>(c => c.On ? SuitModeProblem(c.From) : HeroOf(c.From) == null ? "crew.noHero" : null, c => HeroOf(c.From)!.SuitOn = c.On);
        // After atmosphere in the simulation phase, so a breath reads this tick's air.
        System(Phase.Simulation, () =>
        {
            State.Clock += 1;
            bool breath = State.Clock % Content.Crew.BreathEvery == 0;
            foreach (var hero in State.Heroes)
            {
                if (hero.Out > 0)
                {
                    // A thrust or a run sent before passing out lands on a hero already out: it thrusts and runs nothing.
                    hero.Thrusting = false;
                    _itemsOps.StopRunning(hero.Id);
                    hero.Out -= 1;
                    if (hero.Out == 0)
                    {
                        hero.Cell = State.Spawn;
                        _playersOps.Print(hero.Id);
                        // Printed again in what it died in: the suit is gear (2026-10-27-suits-are-worn).
                        hero.Air = CapacityOf(hero.Id);
                        // Off the ship a hero wakes aboard, at the pod; its body stays where it fell (0076).
                        if (Surfaces.Here != 0) Surfaces.Send(hero.Id, 0);
                    }
                    continue;
                }
                // A helmet taken off loses what the lungs cannot hold; one put on keeps the air, which then fills.
                int capacity = CapacityOf(hero.Id);
                hero.Air = Math.Min(hero.Air, capacity);
                Thrusts(hero);
                if (_items.Creative(hero.Id))
                {
                    // A creative peer's air is endless, and its hero takes no room's air.
                    hero.Air = capacity;
                    hero.Gasp = 0;
                    continue;
                }
                // The last breath (2026-11-05-the-last-breath): any air back ends it; run down, the hero passes out.
                if (hero.Air > 0) hero.Gasp = 0;
                else if (hero.Gasp > 0 && --hero.Gasp == 0)
                {
                    PassOut(hero);
                    continue;
                }
                if (!breath) continue;
                // The pod's air first, for the breath and the suit alike; then one draw from the room for
                // what is left of both, the room's refill as before. What the air falls short of the breath the
                // hero's own air pays, and the cold after that, its own air alone.
                int breathes = hero.Exerting ? Content.Crew.Exerted : 1;
                int lack = capacity - hero.Air;
                int pod = ByPod(hero.Cell) ? Math.Min(Content.Crew.PodRefill, breathes + lack) : 0;
                int want = Math.Min(breathes + Math.Min(Content.Crew.Refill, lack), breathes + lack - pod);
                int taken = want > 0 ? atmosphere.Breathe(hero.Cell, want) : 0;
                int net = pod + taken - breathes;
                // Whether air made the breath, so a hero with none of its own breathes on: room air, the pod's, the regen suit.
                bool covered = net >= 0;
                // Thin air (0076): what the air falls short of the breath, and the cold without a body, paid one breath in ThinAir.
                bool beat = Rules.ThinAir is not int every || State.Clock / Content.Crew.BreathEvery % every == 0;
                if (net < 0 && !beat) net = 0;
                // The regen suit gives back what the breath took, paid from the belt after any tool's draw; never the cold.
                if (net < 0 && SuitOn(hero.Id) && _items.WornIn(hero.Id, Content.Suit.Slot) == Content.Suit.Regen)
                {
                    int made = Math.Min(Content.Suit.RegenMost, -net);
                    if (_itemsOps.Drain(hero.Id, made * Content.Suit.RegenCost)) net += made;
                    covered = net >= 0;
                }
                // The night's chill under the open sky (2026-11-07-night-chills) on its own beat, thin air or not.
                bool chill = Rules.Cycle is { } day && State.Clock / Content.Crew.BreathEvery % day.Every == 0;
                int cold = Soften(hero, ColdAt(hero.Id, hero.Cell, bare: beat, chill), Content.Cape.Warmth);
                int burn = Burns(hero.Cell) ? Content.Lava.Burn : 0;
                if (burn > 0) Hurt(hero, Harm.Burn);
                int left = hero.Air + net - cold;
                hero.Air = Math.Max(0, left - burn);
                if (hero.Air > 0)
                {
                    hero.Gasp = 0;
                    continue;
                }
                // With no air of its own, a breath air made ends the last breath; one short of it, or the cold, starts it; the
                // lava past what the air held cuts it.
                if (covered && cold == 0 && burn == 0) hero.Gasp = 0;
                else if (net < 0 || cold > 0 || burn > 0) Gasps(hero, burn - Math.Max(0, left));
            }
        });
    }

    /// <summary>
    /// A hero with no air left takes <paramref name="hit"/> units of a hit past it (2026-11-05-the-last-breath): its last
    /// breath starts if it has not, and loses <c>Crew.LastBreathPerUnit</c> ticks a unit, passing it out once spent.
    /// </summary>
    private void Gasps(Hero hero, int hit)
    {
        if (hero.Gasp == 0) hero.Gasp = Content.Crew.LastBreath;
        hero.Gasp = Math.Max(0, hero.Gasp - Math.Max(0, hit) * Content.Crew.LastBreathPerUnit);
        if (hero.Gasp == 0) PassOut(hero);
    }

    /// <summary>A hurt taken, counted with its kind, for the client to show (2026-11-08-hurts-are-shown).</summary>
    private static void Hurt(Hero hero, Harm harm)
    {
        hero.Hurts += 1;
        hero.Hurt = harm;
    }

    /// <summary>Passing out is death (0061): what it carried stays where it fell, off the grid at the spawn.</summary>
    private void PassOut(Hero hero)
    {
        hero.Gasp = 0;
        hero.Out = Content.Crew.PassOut;
        hero.Exerting = false;
        hero.Thrusting = false;
        _itemsOps.StopRunning(hero.Id);
        bool onGrid = hero.Cell >= 0 && hero.Cell < _grid.Width * _grid.Height;
        _itemsOps.LeaveBody(hero.Id, onGrid ? hero.Cell : State.Spawn);
    }

    public void Shock(int peer, int units, Harm harm)
    {
        if (HeroOf(peer) is not { Out: 0 } hero || _items.Creative(peer) || units <= 0) return;
        Hurt(hero, harm);
        // The air pays what it holds; the last breath, what it falls short by.
        int beyond = units - hero.Air;
        hero.Air = Math.Max(0, hero.Air - units);
        if (hero.Air == 0) Gasps(hero, beyond);
    }

    public void Blow(int peer, int units, Harm harm)
    {
        if (HeroOf(peer) is not { Out: 0 } hero || _items.Creative(peer) || units <= 0) return;
        Shock(peer, Soften(hero, units, Content.Cape.Guard), harm);
    }

    /// <summary>
    /// What is left of <paramref name="units"/> once <paramref name="hero"/>'s cape has spared its <paramref name="share"/> of
    /// them, in thousandths: spared units rounded down, the rest of one kept in <see cref="Hero.Spared"/> for the next. All of
    /// them without a cape, the remainder untouched (2026-10-27-the-cape-is-worn).
    /// </summary>
    private int Soften(Hero hero, int units, int share)
    {
        if (units <= 0 || !_items.Equipped(hero.Id, Content.Cape.Item)) return units;
        int thousandths = units * share + hero.Spared;
        hero.Spared = thousandths % 1000;
        return units - thousandths / 1000;
    }

    /// <summary>
    /// A conscious hero thrusting pays its air a unit every <c>Jetpack.TicksPerUnit</c> ticks of thrust,
    /// the ticks short of a unit kept for the next burst; never below nothing. One that no longer wears a
    /// jetpack, or stands where nothing floats, stops thrusting.
    /// </summary>
    private void Thrusts(Hero hero)
    {
        if (!hero.Thrusting) return;
        // Under gravity everywhere the jetpack, an air-thrust suit for space, lifts nothing and vents nothing (0076).
        if (!Rules.Floats || !_items.Equipped(hero.Id, Content.Jetpack.Item))
        {
            hero.Thrusting = false;
            return;
        }
        hero.ThrustTicks += 1;
        if (hero.ThrustTicks < Content.Jetpack.TicksPerUnit) return;
        hero.ThrustTicks = 0;
        hero.Air = Math.Max(0, hero.Air - 1);
    }

    private string? ThrustProblem(Thrust c)
    {
        if (HeroOf(c.From) == null) return "crew.noHero";
        return c.On && !_items.Equipped(c.From, Content.Jetpack.Item) ? "crew.noJetpack" : null;
    }

    /// <summary>
    /// Why hero <paramref name="peer"/> could not breathe a canister now: none, its air already
    /// full, or no canister carried (<c>items.notEnough</c>, never in creative mode); null when it could.
    /// </summary>
    public string? RefillProblem(int peer)
    {
        var hero = HeroOf(peer);
        if (hero == null) return "crew.noHero";
        if (hero.Air >= CapacityOf(peer)) return "crew.suitFull";
        return _items.ShortOf(peer, Canister);
    }

    public bool SuitOn(int peer) => HeroOf(peer) is { SuitOn: true } && Switched(peer);

    public string? SuitModeProblem(int peer)
    {
        if (HeroOf(peer) == null) return "crew.noHero";
        return Switched(peer) ? null : "crew.noSuitMode";
    }

    private bool Switched(int peer) => _items.WornIn(peer, Content.Suit.Slot) is { } body && Content.Items.Get(body).Switch;

    /// <summary>The map's first lifepod makes heroes at its hatch; a map without one wakes them at its first S.</summary>
    protected override CrewState Init(Layout layout)
    {
        var pods = layout.Machines().Where(m => Content.Machines.Get(m.Kind).Fixed).Select(m => m.Cell).ToList();
        if (pods.Count == 0) return new() { Spawn = layout.CellsOf('S').DefaultIfEmpty(0).First() };
        var (x, y) = Content.Crew.PodHatch;
        return new() { Pod = pods[0], Spawn = pods[0] + y * layout.Width + x };
    }

    /// <summary>Whether <paramref name="cell"/> is within <c>Crew.PodReach</c> cells of every side of the pod, either way.</summary>
    public bool ByPod(int cell)
    {
        if (State.Pod is not int pod || cell < 0 || _grid.MachineAt(pod) is not { } machine) return false;
        var spec = Content.Machines.Get(machine.Kind);
        int width = _grid.Width;
        int x = cell % width, y = cell / width, left = pod % width, top = pod / width;
        int dx = Math.Max(0, Math.Max(left - x, x - (left + spec.Width - 1)));
        int dy = Math.Max(0, Math.Max(top - y, y - (top + spec.Height - 1)));
        return Math.Max(dx, dy) <= Content.Crew.PodReach;
    }

    /// <summary>
    /// How deep past the field <paramref name="cell"/> is, in thousandths of the way to the world's edge, by
    /// the deeper axis (a box, no root); squared and scaled to <c>Field.EdgeCost</c>, rounded up so a cell
    /// just past the line costs a unit. A world whose field reaches past the grid, as every test map's, costs nothing on it.
    /// </summary>
    public int EdgeCost(int cell)
    {
        int width = _grid.Width, height = _grid.Height;
        if (cell < 0 || cell >= width * height) return Content.Field.OffGrid;
        var zone = _grid.Zone;
        int margin = Content.Field.Margin;
        int left = zone.Left - margin, right = zone.Right + margin, top = zone.Top - margin, bottom = zone.Bottom + margin;
        int x = cell % width, y = cell / width;
        // A vessel's box is as warm as the field (generated-vessels): one reaching past it is still worked in.
        if (_grid.Reserved is { } vessel && vessel.Contains(x, y)) return 0;
        int depth = Math.Min(1000, Math.Max(
            Math.Max(Depth(left - x, left), Depth(x - right, width - 1 - right)),
            Math.Max(Depth(top - y, top), Depth(y - bottom, height - 1 - bottom))));
        return (Content.Field.EdgeCost * depth * depth + 999_999) / 1_000_000;
    }

    /// <summary>
    /// Whether a hero reported at <paramref name="cell"/> touches lava: a wall of it there or up to <c>Lava.Reach</c> rows under
    /// it, where the feet of a hero standing on it are. Off the grid nothing burns.
    /// </summary>
    public bool Burns(int cell)
    {
        int width = _grid.Width, size = width * _grid.Height;
        if (cell < 0 || cell >= size) return false;
        for (int at = cell, i = 0; i <= Content.Lava.Reach && at < size; i++, at += width)
            if (_grid.WallAt(at) is { Hurts: true }) return true;
        return false;
    }

    /// <summary>
    /// <paramref name="beyond"/> cells past the field of the <paramref name="span"/> from it to the grid's
    /// edge, in thousandths, rounded down; none inside. A cell on the grid is never past its edge, so the span holds it.
    /// </summary>
    private static int Depth(int beyond, int span) => beyond <= 0 ? 0 : beyond * 1000 / span;

    /// <summary>
    /// What the cold at <paramref name="cell"/> takes from <paramref name="peer"/>'s air a breath: the edge's, and, when
    /// <paramref name="bare"/> counts, <c>Crew.BareCold</c> with no body worn where the cell is not aired; when <paramref name="chill"/>
    /// counts, the night's chill where the cell has no back wall, suit or none (2026-11-07-night-chills).
    /// </summary>
    private int ColdAt(int peer, int cell, bool bare, bool chill)
    {
        int cold = EdgeCost(cell);
        if (bare && _items.WornIn(peer, Content.Suit.Slot) == null && !_air.AiredAt(cell)) cold += Content.Crew.BareCold;
        if (chill && _grid.BackAt(cell) == null) cold += _sky.Now.Chill;
        return cold;
    }

    private string? ColourProblem(int peer, int colour)
    {
        if (HeroOf(peer) == null) return "crew.noHero";
        if (colour < 0 || colour >= Content.Crew.Colours) return "crew.noSuchColour";
        // Worn by a hero on any surface: they may meet.
        foreach (var w in _players.Worn)
            if (w.Peer != peer && w.Colour == colour) return "crew.colourTaken";
        return null;
    }

    // Queries.

    public int Spawn => State.Spawn;
    public Hero? HeroOf(int id) => State.Heroes.Find(h => h.Id == id);
    public IReadOnlyList<Hero> Heroes => State.Heroes;

    public int CapacityOf(int peer) =>
        _items.WornIn(peer, Content.Helmet.Slot) is { } helmet ? Content.Items.Get(helmet).Tank : Content.Crew.Lungs;

    public int CanisterRefill =>
        Content.Crew.CanisterRefill + Content.Crew.CanisterUp * Content.Upgrades.Added(Content.Upgrades.Canister, _knowledge.Granted);

    public int Cold(int peer) => HeroOf(peer) is { } hero ? ColdAt(peer, hero.Cell, bare: true, chill: true) : 0;

    public int Warmth(int peer) => _items.Equipped(peer, Content.Cape.Item) ? Content.Cape.Warmth : 0;

    // Carried between surfaces (0076).

    /// <summary>The hero of <paramref name="peer"/>, gone from this surface; null when it has none here.</summary>
    public object? Depart(int peer)
    {
        var hero = HeroOf(peer);
        if (hero != null) State.Heroes.Remove(hero);
        return hero;
    }

    /// <summary>
    /// Takes the hero <paramref name="carried"/> as it left, air, breath and thrust, at <paramref name="cell"/> or the
    /// spawn. A peer with no hero brings none: its client sends <see cref="Enter"/> as anywhere.
    /// </summary>
    public void Arrive(int peer, object? carried, int? cell)
    {
        if (carried is not Hero hero) return;
        // A seat's hero is on one surface only: one here already is our bug.
        if (HeroOf(peer) != null) throw new InvalidOperationException($"crew: peer {peer} arrives where its hero is already");
        hero.Cell = cell ?? State.Spawn;
        Add(hero);
    }

    private void Add(Hero hero)
    {
        State.Heroes.Add(hero);
        State.Heroes.Sort((a, b) => a.Id.CompareTo(b.Id));
    }
}
