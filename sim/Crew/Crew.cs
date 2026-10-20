using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Players;

namespace Stillwake.Sim.Crew;

/// <summary>
/// The sender's hero is at <c>Cell</c>: sent when the hero enters another room as its client sees it,
/// and first of all when it wakes, which brings it in with a full suit; and as its report, at most every
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

/// <summary>The sender's hero starts running (<c>On</c>), or stops: sent when it changes.</summary>
public sealed record Exert(bool On) : Command;

/// <summary>
/// The sender's hero starts thrusting with its jetpack (<c>On</c>), or stops: sent while its client's
/// thrust differs from the sim's, as <see cref="Exert"/> is.
/// </summary>
public sealed record Thrust(bool On) : Command;

/// <summary>The sender's hero breathes a canister it carries into its suit.</summary>
public sealed record Refill : Command;

/// <summary>What any module or the client may read of the heroes.</summary>
public interface ICrewQueries
{
    /// <summary>Where heroes are made: the lifepod's hatch, else the map's first S, or cell 0 on a map with neither.</summary>
    int Spawn { get; }
    /// <summary>Whether a hero in <paramref name="cell"/> draws the lifepod's air into its suit: within <c>Crew.PodReach</c> of it.</summary>
    bool ByPod(int cell);
    /// <summary>
    /// What the cold takes from a suit a breath in <paramref name="cell"/>: nothing inside the field, the
    /// build zone grown by <c>Field.Margin</c>; past it more the deeper out, up to <c>Field.EdgeCost</c>
    /// at the world's edge and off the grid (0050).
    /// </summary>
    int EdgeCost(int cell);
    /// <summary>Hero <paramref name="id"/>, played by that peer; null while the sim has none (TypeScript's <c>hero</c>).</summary>
    Hero? HeroOf(int id);
    /// <summary>Every hero, in id order: live and read-only, for counting who breathes where.</summary>
    IReadOnlyList<Hero> Heroes { get; }
}

/// <summary>
/// Heroes inside the sim: where each one is, as its client says, and how it breathes — room air in a
/// cell holding it, the suit anywhere else, passing out when the suit runs dry
/// (packages/sim/src/modules/crew).
/// </summary>
public sealed partial class CrewModule : Module<CrewState>, ICrewQueries, ICarries
{
    public override string Id => "crew";

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IPlayersQueries _players;
    private readonly IPlayersOps _playersOps;

    private static readonly IReadOnlyList<(string Item, int Count)> Canister = [(Content.Crew.Canister, 1)];

    public CrewModule(
        IAtmosphereOps atmosphere, IGridQueries grid, IItemsQueries items, IItemsOps itemsOps, IPlayersQueries players, IPlayersOps playersOps)
    {
        _players = players;
        _playersOps = playersOps;
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
            Add(new Hero { Id = c.From, Cell = c.Cell, Suit = Content.Crew.Suit });
            _playersOps.Dress(c.From);
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
        Handles<Exert>(c => HeroOf(c.From) == null ? "crew.noHero" : null, c => HeroOf(c.From)!.Exerting = c.On);
        Handles<Thrust>(ThrustProblem, c => HeroOf(c.From)!.Thrusting = c.On);
        Handles<Refill>(c => RefillProblem(c.From), c =>
        {
            _itemsOps.Take(c.From, Canister);
            var hero = HeroOf(c.From)!;
            hero.Suit = Math.Min(Content.Crew.Suit, hero.Suit + Content.Crew.CanisterRefill);
        });
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
                        hero.Suit = Content.Crew.Suit;
                        // Off the ship a hero wakes aboard, at the pod; its body stays where it fell (0076).
                        if (Surfaces.Here != 0) Surfaces.Send(hero.Id, 0);
                    }
                    continue;
                }
                Thrusts(hero);
                if (_items.Creative)
                {
                    // Creative mode: the suit is endless, and a hero takes no room's air.
                    hero.Suit = Content.Crew.Suit;
                    continue;
                }
                if (!breath) continue;
                // The pod's air first, for the breath and the suit alike; then one draw from the room for
                // what is left of both, the room's refill as before. What the air falls short of the breath the
                // suit pays, and the cold past the field after that, the suit alone.
                int breathes = hero.Exerting ? Content.Crew.Exerted : 1;
                int lack = Content.Crew.Suit - hero.Suit;
                int pod = ByPod(hero.Cell) ? Math.Min(Content.Crew.PodRefill, breathes + lack) : 0;
                int want = Math.Min(breathes + Math.Min(Content.Crew.Refill, lack), breathes + lack - pod);
                int taken = want > 0 ? atmosphere.Breathe(hero.Cell, want) : 0;
                int cold = EdgeCost(hero.Cell);
                int net = pod + taken - breathes;
                // Thin air (0076): what the air falls short of the breath, the suit pays one breath in ThinAir.
                if (net < 0 && Rules.ThinAir is int every && State.Clock / Content.Crew.BreathEvery % every != 0) net = 0;
                hero.Suit = Math.Max(0, hero.Suit + net - cold);
                if (hero.Suit == 0 && (net < 0 || cold > 0))
                {
                    hero.Out = Content.Crew.PassOut;
                    hero.Exerting = false;
                    hero.Thrusting = false;
                    _itemsOps.StopRunning(hero.Id);
                    // Passing out is death (0061): what it carried stays where it fell, off the grid at the spawn.
                    bool onGrid = hero.Cell >= 0 && hero.Cell < _grid.Width * _grid.Height;
                    _itemsOps.LeaveBody(hero.Id, onGrid ? hero.Cell : State.Spawn);
                }
            }
        });
    }

    /// <summary>
    /// A conscious hero thrusting pays its suit a unit every <c>Jetpack.TicksPerUnit</c> ticks of thrust,
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
        hero.Suit = Math.Max(0, hero.Suit - 1);
    }

    private string? ThrustProblem(Thrust c)
    {
        if (HeroOf(c.From) == null) return "crew.noHero";
        return c.On && !_items.Equipped(c.From, Content.Jetpack.Item) ? "crew.noJetpack" : null;
    }

    /// <summary>
    /// Why hero <paramref name="peer"/> could not breathe a canister into its suit now: none, a suit already
    /// full, or no canister carried (<c>items.notEnough</c>, never in creative mode); null when it could.
    /// </summary>
    public string? RefillProblem(int peer)
    {
        var hero = HeroOf(peer);
        if (hero == null) return "crew.noHero";
        if (hero.Suit >= Content.Crew.Suit) return "crew.suitFull";
        return _items.ShortOf(peer, Canister);
    }

    /// <summary>The map's first lifepod makes heroes at its hatch; a map without one wakes them at its first S.</summary>
    protected override CrewState Init(Layout layout)
    {
        var pods = layout.Machines().Where(m => m.Kind == Pod).Select(m => m.Cell).ToList();
        if (pods.Count == 0) return new() { Spawn = layout.CellsOf('S').DefaultIfEmpty(0).First() };
        var (x, y) = Content.Crew.PodHatch;
        return new() { Pod = pods[0], Spawn = pods[0] + y * layout.Width + x };
    }

    private const string Pod = "lifepod";

    /// <summary>Whether <paramref name="cell"/> is within <c>Crew.PodReach</c> cells of every side of the pod, either way.</summary>
    public bool ByPod(int cell)
    {
        if (State.Pod is not int pod || cell < 0) return false;
        var spec = Content.Machines.Get(Pod);
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
        if (cell < 0 || cell >= width * height) return Content.Field.EdgeCost;
        var zone = _grid.Zone;
        int margin = Content.Field.Margin;
        int left = zone.Left - margin, right = zone.Right + margin, top = zone.Top - margin, bottom = zone.Bottom + margin;
        int x = cell % width, y = cell / width;
        int depth = Math.Min(1000, Math.Max(
            Math.Max(Depth(left - x, left), Depth(x - right, width - 1 - right)),
            Math.Max(Depth(top - y, top), Depth(y - bottom, height - 1 - bottom))));
        return (Content.Field.EdgeCost * depth * depth + 999_999) / 1_000_000;
    }

    /// <summary>
    /// <paramref name="beyond"/> cells past the field of the <paramref name="span"/> from it to the grid's
    /// edge, in thousandths, rounded down; none inside. A cell on the grid is never past its edge, so the span holds it.
    /// </summary>
    private static int Depth(int beyond, int span) => beyond <= 0 ? 0 : beyond * 1000 / span;

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

    // Carried between surfaces (0076).

    /// <summary>The hero of <paramref name="peer"/>, gone from this surface; null when it has none here.</summary>
    public object? Depart(int peer)
    {
        var hero = HeroOf(peer);
        if (hero != null) State.Heroes.Remove(hero);
        return hero;
    }

    /// <summary>
    /// Takes the hero <paramref name="carried"/> as it left, suit, breath and thrust, at <paramref name="cell"/> or the
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
