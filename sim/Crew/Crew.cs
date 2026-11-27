using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Players;
using Stillwake.Sim.Sky;
using Diet = Stillwake.Content.Diet;
using Foods = Stillwake.Content.Foods;
using Rates = Stillwake.Content.Rates;

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

/// <summary>
/// The sender's hero wears skins <c>Ids</c> from now on, in place of every one before (hero-skins): with a hero or none yet,
/// never refused, never checked. Its client sends the ones its player owns on every join and whenever a pick changes.
/// </summary>
public sealed record ChooseSkins(IReadOnlyList<string> Ids) : Command;

/// <summary>The sender's hero starts running (<c>On</c>), or stops: sent when it changes.</summary>
public sealed record Exert(bool On) : Command;

/// <summary>
/// The sender's hero starts thrusting with its jetpack (<c>On</c>), or stops: sent while its client's
/// thrust differs from the sim's, as <see cref="Exert"/> is.
/// </summary>
public sealed record Thrust(bool On) : Command;

/// <summary>The sender's hero breathes a canister it carries, refilling its air; then not again for <c>Crew.CanisterCooldown</c> ticks.</summary>
public sealed record Refill : Command;

/// <summary>
/// The sender's hero eats one <c>Item</c> it carries (food-and-the-body): a meal for the food's <c>Lasts</c> ticks in a free slot, or the
/// same food's meal back to full in its last minute.
/// </summary>
public sealed record Eat(string Item) : Command;

/// <summary>The sender's hero throws up every meal at once (food-tuning): the slots empty, hungry until it eats again.</summary>
public sealed record Purge : Command;

/// <summary>
/// The sender's hero touches the alien core <paramref name="Core"/> with E (drone-companion): a shard of it becomes the hero's drone,
/// the pairing its player's for good, its bag rows open. The core stays for the next hero.
/// </summary>
public sealed record PairDrone(int Core) : Command;

/// <summary>
/// The sender's drone learned a form and shows the hero what it does (form-vision): the hero held and spared as in the drone's meeting, for
/// <c>Crew.VisionTicks</c> or what is left of a longer spare. Sent by its client as the vision begins.
/// </summary>
public sealed record Envision : Command;

/// <summary>
/// The sender's hero passes out of its body's cold or heat (body-temperature): its client keeps the body, from the degrees the sim
/// says it feels, and sends this as it reaches <c>Temperature.ColdDeath</c> or <c>HeatDeath</c>.
/// </summary>
public sealed record Succumb : Command;

/// <summary>
/// The sender's hero switches its suit's mode on (<c>On</c>) or off: V, or its key on the tablet's edge; on only in a body
/// with <c>Switch</c> (2026-11-03-the-regen-suit).
/// </summary>
public sealed record SuitMode(bool On) : Command;

/// <summary>
/// Brings the sender's hunt near (tracker-catchers): its meter <c>Short</c> worth short of full, its need drawn if it has none; at 0
/// full, its alarm drawn. A dev command (<c>--catcher</c>), as <c>Release</c> is.
/// </summary>
public sealed record Track(int Short) : Command;

/// <summary>What any module or the client may read of the heroes.</summary>
public interface ICrewQueries
{
    /// <summary>Where heroes are made: the lifepod's hatch, else the map's first S, or cell 0 on a map with neither.</summary>
    int Spawn { get; }
    /// <summary>Whether a hero in <paramref name="cell"/> draws the lifepod's air: within <c>Crew.PodReach</c> of it.</summary>
    bool ByPod(int cell);
    /// <summary>
    /// <paramref name="cell"/>'s temperature in degrees (2026-11-14-temperature-in-degrees): <c>Temperature.Deep</c> off the grid,
    /// <c>Temperature.Room</c> aired; afloat <c>Temperature.Hull</c> by the ship, falling to space's at the field's line and on
    /// to the deep's at the world's edge (body-temperature); on a planet its depth's (<c>Terrain.TemperatureOf</c>), the top band <c>SkyNow.Chill</c> colder
    /// where the cell has no back wall. What moves a hero's body, which its client keeps.
    /// </summary>
    int TemperatureAt(int cell);
    /// <summary>
    /// The thousandths of its speed a hero walks and runs at in <paramref name="cell"/>: <c>SkyNow.Pace</c> where the sky's chill
    /// reaches (<see cref="TemperatureAt"/>'s open sky), so a storm slows it out in the open and not under a back wall; whole elsewhere.
    /// </summary>
    int PaceAt(int cell);
    /// <summary>
    /// Whether the sky reaches <paramref name="cell"/>: on the grid, in the top band, with no back wall. Night, the stretches and
    /// lightning reach only there (night-chills): a back wall or a cave deeper down shelters.
    /// </summary>
    bool OpenSky(int cell);
    /// <summary>
    /// The coldest hero <paramref name="peer"/> withstands, in degrees: <c>Temperature.BareLow</c> plus its helmet's and body's
    /// <c>Low</c> (gear-roles), <c>Cape.Warmth</c> lower with the cape on.
    /// </summary>
    int LowOf(int peer);
    /// <summary>
    /// The degrees <paramref name="cell"/> is colder than hero <paramref name="peer"/> withstands (<see cref="LowOf"/>), 0 within
    /// it, and 0 with no hero: each draws its body, kept by its client, <c>Temperature.Sway</c> hundredths under <c>Temperature.Normal</c> (body-temperature).
    /// </summary>
    int Felt(int peer, int cell);
    /// <summary>
    /// The hottest hero <paramref name="peer"/> withstands, in degrees (2026-11-14-heat-limits): <c>Temperature.BareHigh</c> plus its
    /// helmet's and body's <c>High</c> (gear-roles), <c>Cape.Warmth</c> lower with the cape on.
    /// </summary>
    int HighOf(int peer);
    /// <summary>
    /// The degrees <paramref name="cell"/> is hotter than hero <paramref name="peer"/> withstands (<see cref="HighOf"/>), 0 within
    /// it, and 0 with no hero: each draws its body, kept by its client, <c>Temperature.Sway</c> hundredths over <c>Temperature.Normal</c>, never felt
    /// with <see cref="Felt"/> at once (body-temperature).
    /// </summary>
    int Heat(int peer, int cell);
    /// <summary>Why hero <paramref name="peer"/> cannot succumb to its body's cold or heat now (<see cref="Succumb"/>); null when it can.</summary>
    string? SuccumbProblem(int peer);
    /// <summary>Hero <paramref name="id"/>, played by that peer; null while the sim has none (TypeScript's <c>hero</c>).</summary>
    Hero? HeroOf(int id);
    /// <summary>Every hero, in id order: live and read-only, for counting who breathes where.</summary>
    IReadOnlyList<Hero> Heroes { get; }
    /// <summary>
    /// The most air hero <paramref name="peer"/> holds: the sum of its <see cref="PartsOf"/>. Its air is cut to it each tick.
    /// </summary>
    int CapacityOf(int peer);
    /// <summary>
    /// The parts of hero <paramref name="peer"/>'s air in order (air-segments): <c>Crew.Lungs</c>, the worn helmet's <c>Tank</c>,
    /// what food lends; filled the first first, spent the last first.
    /// </summary>
    IReadOnlyList<AirPart> PartsOf(int peer);
    /// <summary>What a canister breathed gives hero <paramref name="peer"/>'s air, up to full: <c>Crew.CanisterRefill</c>, more by its deep rate.</summary>
    int RefillOf(int peer);
    /// <summary>The rates hero <paramref name="peer"/>'s meals give, summed and capped by <c>Diet.Caps</c>; none with no hero or no meal.</summary>
    Rates RatesOf(int peer);
    /// <summary>Whether hero <paramref name="peer"/> is hungry: awake, every meal gone, not creative; its running costs <c>Diet.HungrySprint</c>.</summary>
    bool Hungry(int peer);
    /// <summary>Whether hero <paramref name="peer"/> is spared all harm and drain now, its drone's meeting (drone-meeting) or a form's vision (form-vision) playing.</summary>
    bool Spared(int peer);
    /// <summary>
    /// Whether hero <paramref name="peer"/>'s suit's mode works: switched on, in a body with <c>Switch</c>; false with no hero
    /// (2026-11-03-the-regen-suit).
    /// </summary>
    bool SuitOn(int peer);
    /// <summary>Why hero <paramref name="peer"/> could not switch its suit's mode on now: none, or no body with a mode; null when it could.</summary>
    string? SuitModeProblem(int peer);
    /// <summary>What hero <paramref name="peer"/>'s tracker bracelet shows of its wanted meter (tracker-catchers); <see cref="Signal.Calm"/> with no hero.</summary>
    Signal SignalOf(int peer);
    /// <summary>Whether hero <paramref name="peer"/>'s wanted meter is full: a catcher hunts it (tracker-catchers).</summary>
    bool Hunted(int peer);
}

/// <summary>How another module may change a hero.</summary>
public interface ICrewOps
{
    /// <summary>
    /// Hero <paramref name="peer"/>'s air pays <paramref name="units"/> at once, as the lava takes it:
    /// a ruin's resonator zapping it (0091). Air run dry starts its last breath, which the rest cuts; nothing in creative mode, nor for a hero out or none.
    /// Counted as a hurt of <paramref name="harm"/> (2026-11-08-hurts-are-shown).
    /// </summary>
    void Shock(int peer, int units, Harm harm);

    /// <summary>
    /// An enemy's blow of <paramref name="units"/> on hero <paramref name="peer"/>: a creature's ram or pounce, its bolt. A worn
    /// cape spares its share (<c>Cape.Guard</c>); the rest is paid as <see cref="Shock"/> pays (2026-10-27-the-cape-is-worn). A hurt of <paramref name="harm"/>.
    /// </summary>
    void Blow(int peer, int units, Harm harm);

    /// <summary>
    /// Hero <paramref name="peer"/> broke or killed what gave <paramref name="yield"/> (catcher-loot): its wanted meter fills by its worth,
    /// up to its need, the alarm drawn as it fills, and so does its drone's core (drone-abilities); nothing in creative mode, nor for a
    /// hero out or none.
    /// </summary>
    void Loot(int peer, IReadOnlyList<(string Item, int Count)> yield);

    /// <summary>
    /// Hero <paramref name="peer"/>'s hunt is over (tracker-catchers): its catcher out of time or dead. Its wanted meter empties, to fill
    /// again to a new need; nothing with no hero.
    /// </summary>
    void CallOff(int peer);

    /// <summary>
    /// A <c>Work</c> command of hero <paramref name="peer"/>'s, a building's or a repair's (food-and-the-body): its work rate, hungry
    /// <c>Diet.HungryWork</c>, fed <c>Diet.Work</c> and food's, adds to <c>Worked</c>; the whole ticks it is worth, 0 to 2, come back.
    /// No hero, one out or creative: 1, <c>Worked</c> untouched.
    /// </summary>
    int Works(int peer);
}

/// <summary>
/// Heroes inside the sim: where each one is, as its client says, and how it breathes вЂ” room air in a
/// cell holding it, its own air anywhere else (the suit's, or its lungs bare), passing out when that runs dry
/// (packages/sim/src/modules/crew).
/// </summary>
public sealed partial class CrewModule : Module<CrewState>, ICrewQueries, ICrewOps, ICarries
{
    public override string Id => "crew";

    private readonly IAtmosphereQueries _air;
    private readonly IGridQueries _grid;
    private readonly IGridOps _gridOps;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IPlayersQueries _players;
    private readonly IPlayersOps _playersOps;
    private readonly ISkyQueries _sky;

    private static readonly IReadOnlyList<(string Item, int Count)> Canister = [(Content.Crew.Canister, 1)];

    public CrewModule(
        IAtmosphereQueries air, IAtmosphereOps atmosphere, IGridQueries grid, IGridOps gridOps, IItemsQueries items, IItemsOps itemsOps, IPlayersQueries players,
        IPlayersOps playersOps, ISkyQueries sky)
    {
        _gridOps = gridOps;
        _players = players;
        _playersOps = playersOps;
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
            // Printed full in what it wears and hungry: bare as a peer's first hero, in its kept helmet coming back after Leave.
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
        Handles<ChooseSkins>(_ => null, c => _playersOps.Skin(c.From, c.Ids));
        Handles<Exert>(c => HeroOf(c.From) == null ? "crew.noHero" : null, c => HeroOf(c.From)!.Exerting = c.On);
        Handles<Thrust>(ThrustProblem, c => HeroOf(c.From)!.Thrusting = c.On);
        Handles<Refill>(c => RefillProblem(c.From), c =>
        {
            _itemsOps.Take(c.From, Canister);
            var hero = HeroOf(c.From)!;
            hero.Air = Math.Min(CapacityOf(c.From), hero.Air + RefillOf(c.From));
            if (!_items.Creative(c.From)) hero.CanisterIn = Content.Crew.CanisterCooldown;
        });
        Handles<Eat>(c => EatProblem(c.From, c.Item), c =>
        {
            _itemsOps.Take(c.From, [(c.Item, 1)]);
            var hero = HeroOf(c.From)!;
            int lasts = Foods.Of(c.Item)!.Lasts;
            // The same food eaten again keeps its slot, its ticks back to full.
            if (hero.Eaten.Find(m => m.Food == c.Item) is { } meal) meal.Left = lasts;
            else hero.Eaten.Add(new Meal { Food = c.Item, Left = lasts });
            // A fruit eaten raw draws the rolls its crushing would: its seed back now and then (fruit-not-biomass).
            if (Content.Items.Get(c.Item).Byproducts is { } kept && Loots.Roll(kept, Random) is { Count: > 0 } back)
                _itemsOps.GiveOrDrop(c.From, back, hero.Cell);
        });
        Handles<PairDrone>(c => PairProblem(c.From, c.Core), c =>
        {
            _playersOps.Pair(c.From);
            HeroOf(c.From)!.Meets = Content.Crew.MeetingTicks;
        });
        // The same spare as the meeting's (form-vision), never cut short.
        Handles<Envision>(c => EnvisionProblem(c.From), c => HeroOf(c.From)!.Meets = Math.Max(HeroOf(c.From)!.Meets, Content.Crew.VisionTicks));
        Handles<Purge>(c => PurgeProblem(c.From), c =>
        {
            var hero = HeroOf(c.From)!;
            hero.Eaten.Clear();
            hero.Purges += 1;
        });
        Handles<Succumb>(c => SuccumbProblem(c.From), c => PassOut(HeroOf(c.From)!));
        Handles<SuitMode>(c => c.On ? SuitModeProblem(c.From) : HeroOf(c.From) == null ? "crew.noHero" : null, c => HeroOf(c.From)!.SuitOn = c.On);
        Handles<Track>(c => HeroOf(c.From) == null ? "crew.noHero" : null, c => Track(HeroOf(c.From)!, c.Short));
        // After atmosphere in the simulation phase, so a breath reads this tick's air.
        System(Phase.Simulation, () =>
        {
            State.Clock += 1;
            bool breath = State.Clock % Content.Crew.BreathEvery == 0;
            LoseCore();
            bool coreLost = State.CoreLost;
            foreach (var hero in State.Heroes)
            {
                // The core broken loose, its shards find every hero still without one (drone-companion).
                if (coreLost && !_players.HasDrone(hero.Id)) _playersOps.Pair(hero.Id);
                if (hero.CanisterIn > 0) hero.CanisterIn -= 1;
                // Its drone's meeting playing, nothing drains or harms it, its meals waiting too (drone-meeting).
                if (hero.Meets > 0)
                {
                    hero.Meets -= 1;
                    hero.Thrusting = false;
                    continue;
                }
                for (int i = hero.Eaten.Count - 1; i >= 0; i--)
                    if (--hero.Eaten[i].Left <= 0) hero.Eaten.RemoveAt(i);
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
                        // Printed again in what it died in, hungry: the suit is gear (2026-10-27-suits-are-worn).
                        (hero.Air, hero.Shape) = (CapacityOf(hero.Id), null);
                        // Off the ship a hero wakes aboard, at the pod; its body stays where it fell (0076).
                        if (Surfaces.Here != 0) Surfaces.Send(hero.Id, 0);
                    }
                    continue;
                }
                // Each part keeps what it held: one gone, a helmet off or a stimulant ended, takes its air with it; one new comes
                // empty (air-segments).
                var layout = LayoutOf(hero);
                if (hero.Shape is not { } was || !Same(was, layout))
                {
                    if (hero.Shape != null) hero.Air = Kept(hero.Air, hero.Shape, layout);
                    hero.Shape = Parts(layout);
                }
                int capacity = CapacityOf(hero.Id);
                hero.Air = Math.Min(hero.Air, capacity);
                Thrusts(hero);
                // The alarm runs out whatever the hero does, creative or not.
                if (breath && hero.Coming > 0) hero.Coming -= 1;
                if (breath) Drain(hero);
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
                // A breath costs a share of a unit by its meals (food-and-the-body), paid whole as the shares add up.
                int owed = hero.Owed + BreathCost(hero);
                int breathes = owed / 1000;
                int lack = capacity - hero.Air;
                int pod = ByPod(hero.Cell) ? Math.Min(Content.Crew.PodRefill, breathes + lack) : 0;
                int want = Math.Min(breathes + Math.Min(FillOf(capacity), lack), breathes + lack - pod);
                int taken = want > 0 ? atmosphere.Breathe(hero.Cell, want) : 0;
                int net = pod + taken - breathes;
                // Whether air made the breath, so a hero with none of its own breathes on: room air, the pod's, the regen suit.
                bool covered = net >= 0;
                // Thin air (0076): what the air falls short of the breath, paid one breath in ThinAir.
                bool beat = Rules.ThinAir is not int every || State.Clock / Content.Crew.BreathEvery % every == 0;
                if (net < 0 && !beat) net = 0;
                // Off the beat a breath owes only where air made it: in vacuum the shares move on the beat alone, or they would pile
                // up off it and every beat would pay whole.
                if (beat || covered && (_air.AiredAt(hero.Cell) || ByPod(hero.Cell))) hero.Owed = owed % 1000;
                // The regen suit gives back what the breath took, paid from the belt after any tool's draw; never the grit.
                if (net < 0 && SuitOn(hero.Id) && _items.WornIn(hero.Id, Content.Suit.Slot) == Content.Suit.Regen)
                {
                    int made = Math.Min(Content.Suit.RegenMost, -net);
                    if (_itemsOps.Drain(hero.Id, made * Content.Suit.RegenCost)) net += made;
                    covered = net >= 0;
                }
                // A storm's grit grinds out the suit's own air, thin air or not; the cold and the heat move the body, not the air
                // (body-temperature).
                int cold = GritAt(hero.Cell);
                int burn = Burns(hero.Cell) ? Content.Lava.Burn : 0;
                if (burn > 0) Hurt(hero, Harm.Burn);
                int left = hero.Air + net - cold;
                hero.Air = Math.Max(0, left - burn);
                if (hero.Air > 0)
                {
                    hero.Gasp = 0;
                    continue;
                }
                // With no air of its own, a breath air made ends the last breath where no grit grinds; one short of it, or the
                // grit, starts it; the lava past what the air held cuts it.
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
        // Out of the cold with air left, it holds none while out: a part swapped then never holds more than it may.
        (hero.Air, hero.Gasp) = (0, 0);
        hero.Out = Content.Crew.PassOut;
        // The pod prints a new person: what the body ate and was waiting on goes with it.
        hero.CanisterIn = 0;
        hero.Eaten.Clear();
        hero.Owed = 0;
        hero.Worked = 0;
        CallOff(hero);
        hero.Exerting = false;
        hero.Thrusting = false;
        _itemsOps.StopRunning(hero.Id);
        bool onGrid = hero.Cell >= 0 && hero.Cell < _grid.Width * _grid.Height;
        _itemsOps.LeaveBody(hero.Id, onGrid ? hero.Cell : State.Spawn);
    }

    public void Shock(int peer, int units, Harm harm)
    {
        if (HeroOf(peer) is not { Out: 0, Meets: 0 } hero || _items.Creative(peer) || units <= 0) return;
        Hurt(hero, harm);
        // The air pays what it holds; the last breath, what it falls short by.
        int beyond = units - hero.Air;
        hero.Air = Math.Max(0, hero.Air - units);
        if (hero.Air == 0) Gasps(hero, beyond);
    }

    public void Blow(int peer, int units, Harm harm)
    {
        if (HeroOf(peer) is not { Out: 0, Meets: 0 } hero || _items.Creative(peer) || units <= 0) return;
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
    /// Why hero <paramref name="peer"/> could not breathe a canister now: none or passed out, its air already full, the last
    /// canister too recent (<c>crew.canisterCooling</c>), or no canister carried (<c>items.notEnough</c>); creative mode
    /// skips the last two. Null when it could.
    /// </summary>
    public string? RefillProblem(int peer)
    {
        var hero = HeroOf(peer);
        if (hero is null or { Out: > 0 }) return "crew.noHero";
        if (hero.Air >= CapacityOf(peer)) return "crew.suitFull";
        if (hero.CanisterIn > 0 && !_items.Creative(peer)) return "crew.canisterCooling";
        return _items.ShortOf(peer, Canister);
    }

    /// <summary>
    /// Why hero <paramref name="peer"/> could not eat a <paramref name="item"/> now: none or passed out, not a food, none carried
    /// (never in creative), the same food eaten and not in its last minute (<c>Diet.RefreshWithin</c>), or every slot full. Null
    /// when it could.
    /// </summary>
    public string? EatProblem(int peer, string item)
    {
        if (HeroOf(peer) is not { Out: 0 } hero) return "crew.noHero";
        if (Foods.Of(item) is null) return "crew.notFood";
        if (_items.ShortOf(peer, [(item, 1)]) is { } none) return none;
        if (hero.Eaten.Find(m => m.Food == item) is { } meal) return meal.Left > Diet.RefreshWithin ? "crew.sameFood" : null;
        return hero.Eaten.Count >= Diet.Meals ? "crew.mealsFull" : null;
    }

    /// <summary>
    /// Why hero <paramref name="peer"/> could not pair with a drone at machine <paramref name="core"/> now: none or passed out
    /// (<c>crew.noHero</c>), its player paired already (<c>crew.hasDrone</c>), or no alien core there (<c>crew.noCore</c>). Null when it could.
    /// </summary>
    public string? PairProblem(int peer, int core)
    {
        if (HeroOf(peer) is not { Out: 0 }) return "crew.noHero";
        if (_players.HasDrone(peer)) return "crew.hasDrone";
        return _grid.MachineById(core) is { } m && Content.Machines.Is(m.Kind, Content.MachineTrait.Wakes) && !CoreLost() ? null : "crew.noCore";
    }

    /// <summary>Why hero <paramref name="peer"/>'s vision could not spare it now (<see cref="Envision"/>): none or passed out (<c>crew.noHero</c>). Null when it could.</summary>
    public string? EnvisionProblem(int peer) => HeroOf(peer) is { Out: 0 } ? null : "crew.noHero";

    /// <summary>
    /// Whether the map's alien core broke loose (drone-companion): a cell of the floor under it was broken, however. Then the core is
    /// gone for good, a floor laid back or not, and every hero here without a drone is given one as it stands. A map with no core
    /// never loses it.
    /// </summary>
    public bool CoreLost() => State.CoreLost;

    /// <summary>The core taken off the map, lost, once a cell of the floor under it is broken: its place is free to build in.</summary>
    private void LoseCore()
    {
        int width = _grid.Width;
        foreach (var core in _grid.MachinesWith(Content.MachineTrait.Wakes))
        {
            var spec = Content.Machines.Get(core.Kind);
            int below = core.Cell + spec.Height * width;
            for (int x = 0; x < spec.Width; x++)
            {
                if (_grid.Solid(below + x)) continue;
                _gridOps.Clear(core.Cell, "machine");
                State.CoreLost = true;
                return;
            }
        }
    }

    /// <summary>Why hero <paramref name="peer"/> could not purge: none, or out (<c>crew.noHero</c>), or nothing eaten (<c>crew.noMeals</c>). Null when it could.</summary>
    // A creative hero never freezes: its client keeps its body well, so one sent anyway is a stale one.
    // Nor in its drone's meeting, which spares it (drone-meeting).
    public string? SuccumbProblem(int peer) => HeroOf(peer) is not { Out: 0, Meets: 0 } || _items.Creative(peer) ? "crew.noHero" : null;

    public string? PurgeProblem(int peer)
    {
        if (HeroOf(peer) is not { Out: 0 } hero) return "crew.noHero";
        return hero.Eaten.Count == 0 ? "crew.noMeals" : null;
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
        var pods = layout.Machines().Where(m => Content.Machines.Get(m.Kind) is { Fixed: true } spec && spec.Is(Content.MachineTrait.AirSource)).Select(m => m.Cell).ToList();
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

    public int TemperatureAt(int cell)
    {
        int width = _grid.Width;
        if (cell < 0 || cell >= width * _grid.Height) return Content.Temperature.Deep;
        if (_air.AiredAt(cell)) return Content.Temperature.Room;
        int row = cell / width;
        int degrees = Rules is Content.PlanetType planet ? Terrain.TemperatureOf(planet, row) : Rules.Floats ? Afloat(cell) : Rules.Temperatures[0];
        return OpenSky(cell) ? degrees - _sky.Now.Chill : degrees;
    }

    public int PaceAt(int cell) =>
        cell < 0 || cell >= _grid.Width * _grid.Height || _air.AiredAt(cell) || !OpenSky(cell) ? 1000 : _sky.Now.Pace;

    /// <summary>Units of a hero's own air a breath the running storm's grit grinds out at <paramref name="cell"/>: under the open sky, unaired; 0 elsewhere.</summary>
    public int GritAt(int cell) =>
        cell < 0 || cell >= _grid.Width * _grid.Height || _air.AiredAt(cell) || !OpenSky(cell) ? 0 : _sky.Now.Grit;

    public bool OpenSky(int cell) =>
        cell >= 0 && cell < _grid.Width * _grid.Height
        && (Rules is not Content.PlanetType deep || Terrain.BandOf(deep, cell / _grid.Width) == 0) && _grid.BackAt(cell) == null;

    /// <summary>
    /// An airless cell's degrees afloat: <c>Temperature.Hull</c> in the zone and a vessel's box, falling straight to the kind's own
    /// (space's, <c>Temperature.Space</c>) at the field's line, <c>Field.Margin</c> cells out by the farther axis; past it on to
    /// <c>Temperature.Deep</c> at the world's edge, by the deeper axis's share of the way there (body-temperature). Rounded toward
    /// the warmer. A field reaching past the grid, as a test map's, has no deep.
    /// </summary>
    private int Afloat(int cell)
    {
        int width = _grid.Width, height = _grid.Height, x = cell % width, y = cell / width;
        var zone = _grid.Zone;
        int beyond = Math.Max(Math.Max(zone.Left - x, x - zone.Right), Math.Max(zone.Top - y, y - zone.Bottom));
        int far = Rules.Temperatures[0], near = Content.Temperature.Hull, margin = Content.Field.Margin;
        if (beyond <= margin) return near + (far - near) * Math.Max(0, beyond) / margin;
        int left = zone.Left - margin, right = zone.Right + margin, top = zone.Top - margin, bottom = zone.Bottom + margin;
        int deep = Math.Max(Math.Max(Share(left - x, left), Share(x - right, width - 1 - right)),
            Math.Max(Share(top - y, top), Share(y - bottom, height - 1 - bottom)));
        return far + (Content.Temperature.Deep - far) * deep / 1000;
    }

    /// <summary><paramref name="beyond"/> cells past the field's line of the <paramref name="span"/> from it to the grid's edge, in thousandths, rounded down; none inside.</summary>
    private static int Share(int beyond, int span) => beyond <= 0 || span <= 0 ? 0 : Math.Min(1000, beyond * 1000 / span);

    public int LowOf(int peer) => Caped(peer, Content.Temperature.BareLow + (Worn(peer, Content.Helmet.Slot)?.Low ?? 0) + (Worn(peer, Content.Suit.Slot)?.Low ?? 0));

    public int HighOf(int peer) => Caped(peer, Content.Temperature.BareHigh + (Worn(peer, Content.Helmet.Slot)?.High ?? 0) + (Worn(peer, Content.Suit.Slot)?.High ?? 0));

    /// <summary>The item hero <paramref name="peer"/> wears in the <paramref name="slot"/>, its helmet or suit; null bare.</summary>
    private Content.ItemSpec? Worn(int peer, string slot) => _items.WornIn(peer, slot) is { } item ? Content.Items.Get(item) : null;

    /// <summary>A limit with the cape's warmth taken off it, cold's or heat's alike (2026-11-14-heat-limits).</summary>
    private int Caped(int peer, int limit) => _items.Equipped(peer, Content.Cape.Item) ? limit - Content.Cape.Warmth : limit;

    public int Felt(int peer, int cell) => HeroOf(peer) == null ? 0 : Math.Max(0, LowOf(peer) - TemperatureAt(cell));

    public int Heat(int peer, int cell) => HeroOf(peer) == null ? 0 : Math.Max(0, TemperatureAt(cell) - HighOf(peer));

    private string? ColourProblem(int peer, int colour)
    {
        if (HeroOf(peer) == null) return "crew.noHero";
        if (colour < 0 || colour >= Content.Crew.Colours) return "crew.noSuchColour";
        // Worn by a hero on any surface: they may meet.
        foreach (var w in _players.Worn)
            if (w.Peer != peer && w.Colour == colour) return "crew.colourTaken";
        return null;
    }

    public void Loot(int peer, IReadOnlyList<(string Item, int Count)> yield)
    {
        if (HeroOf(peer) is not { Out: 0 } hero || _items.Creative(peer)) return;
        long worth = 0;
        foreach (var (item, count) in yield) worth += (long)Content.Market.WorthOf(item) * count;
        if (worth <= 0) return;
        Fill(hero, worth);
        // Beside the catcher meter, not in it: that stops once full, the drone's core does not (drone-abilities).
        _playersOps.Charge(peer, worth);
    }

    /// <summary>
    /// A breath of <paramref name="hero"/> in the starting belt (catchers-space), where things float: its meter, not full, loses
    /// <c>Wanted.Drain</c>, never below none. Out of the belt, or full with its catcher coming, nothing.
    /// </summary>
    private void Drain(Hero hero)
    {
        if (!Rules.Floats || hero.Wanted == 0 || hero.Wanted >= hero.Need || !InBelt(hero.Cell)) return;
        hero.Wanted = Math.Max(0, hero.Wanted - Content.Wanted.Drain);
    }

    /// <summary>Whether <paramref name="cell"/> lies in <c>Wanted.Belt</c> round the middle of the grid.</summary>
    private bool InBelt(int cell)
    {
        int width = _grid.Width, height = _grid.Height;
        if (cell < 0 || cell >= width * height) return false;
        return Math.Abs(cell % width - width / 2) <= Content.Wanted.Belt.X && Math.Abs(cell / width - height / 2) <= Content.Wanted.Belt.Y;
    }

    /// <summary>Fills <paramref name="hero"/>'s meter by <paramref name="worth"/>, never past its need, drawn first; filled now, its alarm drawn.</summary>
    private void Fill(Hero hero, long worth)
    {
        Draw(hero);
        if (hero.Wanted >= hero.Need) return;
        hero.Wanted = (int)Math.Min(hero.Need, hero.Wanted + worth);
        if (hero.Wanted == hero.Need)
            hero.Coming = Content.Wanted.ComingLeast + (int)Random.Below(Content.Wanted.ComingMost - Content.Wanted.ComingLeast + 1);
    }

    private void Track(Hero hero, int @short)
    {
        Draw(hero);
        if (hero.Wanted >= hero.Need) return;
        hero.Wanted = Math.Max(0, hero.Need - Math.Max(@short, 1));
        // Filled as loot fills it: its alarm drawn.
        if (@short <= 0) Fill(hero, 1);
    }

    /// <summary>A need drawn for <paramref name="hero"/> if it has none: how much activity its next hunt takes.</summary>
    private void Draw(Hero hero)
    {
        if (hero.Need == 0) hero.Need = Content.Wanted.NeedLeast + (int)Random.Below(Content.Wanted.NeedMost - Content.Wanted.NeedLeast + 1);
    }

    public void CallOff(int peer)
    {
        if (HeroOf(peer) is { } hero) CallOff(hero);
    }

    public int Works(int peer)
    {
        if (HeroOf(peer) is not { Out: 0 } hero || _items.Creative(peer)) return 1;
        int worked = hero.Worked + (hero.Eaten.Count == 0 ? Diet.HungryWork : Diet.Work + RatesOf(hero).Work);
        hero.Worked = worked % 1000;
        return worked / 1000;
    }

    /// <summary>Empties <paramref name="hero"/>'s wanted meter, its need drawn again once it fills.</summary>
    private static void CallOff(Hero hero)
    {
        hero.Wanted = 0;
        hero.Need = 0;
        hero.Coming = 0;
    }

    public Signal SignalOf(int peer) => HeroOf(peer) switch
    {
        { Need: > 0, Coming: > 0 } hero when hero.Wanted >= hero.Need => Signal.Coming,
        { Need: > 0 } hero when hero.Wanted >= hero.Need => Signal.Hunted,
        _ => Signal.Calm,
    };

    public bool Hunted(int peer) => SignalOf(peer) == Signal.Hunted;

    // Queries.

    public int Spawn => State.Spawn;
    public Hero? HeroOf(int id) => State.Heroes.Find(h => h.Id == id);
    public IReadOnlyList<Hero> Heroes => State.Heroes;

    public int CapacityOf(int peer)
    {
        var (_, tank, lent) = LayoutOf(peer, RatesOf(peer));
        return Content.Crew.Lungs + tank + lent;
    }

    public IReadOnlyList<AirPart> PartsOf(int peer) => Parts(LayoutOf(peer, RatesOf(peer)));

    /// <summary>What an aired room gives a hero's air of <paramref name="capacity"/> units a breath: full from empty in <c>Crew.FillBreaths</c>, rounded up.</summary>
    public static int FillOf(int capacity) => (capacity + Content.Crew.FillBreaths - 1) / Content.Crew.FillBreaths;

    /// <summary>A hero's air past its lungs: the helmet it wears and its tank, none bare; what its food lends.</summary>
    private (string? Helmet, int Tank, int Lent) LayoutOf(int peer, Rates rates) =>
        _items.WornIn(peer, Content.Helmet.Slot) is { } helmet && Content.Items.Get(helmet).Tank is > 0 and var tank
            ? (helmet, tank, rates.Lungs)
            : (null, 0, rates.Lungs);

    private (string? Helmet, int Tank, int Lent) LayoutOf(Hero hero) => LayoutOf(hero.Id, RatesOf(hero));

    private static List<AirPart> Parts((string? Helmet, int Tank, int Lent) layout)
    {
        var parts = new List<AirPart> { new(AirPart.Lungs, Content.Crew.Lungs) };
        if (layout.Helmet != null) parts.Add(new(layout.Helmet, layout.Tank));
        if (layout.Lent > 0) parts.Add(new(AirPart.Food, layout.Lent));
        return parts;
    }

    private static bool Same(List<AirPart> was, (string? Helmet, int Tank, int Lent) now)
    {
        int i = 0;
        bool Next(string key, int size) => i < was.Count && was[i].Key == key && was[i++].Size == size;
        return Next(AirPart.Lungs, Content.Crew.Lungs)
            && (now.Helmet == null || Next(now.Helmet, now.Tank))
            && (now.Lent == 0 || Next(AirPart.Food, now.Lent))
            && i == was.Count;
    }

    /// <summary>
    /// <paramref name="air"/> laid in the parts it <paramref name="was"/> in, the first filled first, each part's kept as far as
    /// the part is still there <paramref name="now"/>, at its size now: a part gone takes what it held.
    /// </summary>
    private static int Kept(int air, List<AirPart> was, (string? Helmet, int Tank, int Lent) now)
    {
        int left = air, kept = 0;
        foreach (var part in was)
        {
            int held = Math.Min(left, part.Size);
            left -= held;
            int size = part.Key == AirPart.Lungs ? Content.Crew.Lungs : part.Key == AirPart.Food ? now.Lent : part.Key == now.Helmet ? now.Tank : 0;
            kept += Math.Min(held, size);
        }
        return kept;
    }

    public int RefillOf(int peer) => Content.Crew.CanisterRefill + RatesOf(peer).Deep;

    public Rates RatesOf(int peer) => HeroOf(peer) is { } hero ? RatesOf(hero) : default;

    public bool Spared(int peer) => HeroOf(peer) is { Meets: > 0 };

    public bool Hungry(int peer) => HeroOf(peer) is { Out: 0, Eaten.Count: 0 } && !_items.Creative(peer);

    private static Rates RatesOf(Hero hero)
    {
        int calm = 0, deep = 0, work = 0, lungs = 0;
        foreach (var meal in hero.Eaten)
            if (Foods.Of(meal.Food) is { Rates: var r })
                (calm, deep, work, lungs) = (calm + r.Calm, deep + r.Deep, work + r.Work, lungs + r.Lungs);
        var cap = Diet.Caps;
        return new(Math.Min(calm, cap.Calm), Math.Min(deep, cap.Deep), Math.Min(work, cap.Work), Math.Min(lungs, cap.Lungs));
    }

    /// <summary>Thousandths of a unit a breath costs <paramref name="hero"/>: at rest or running, dearer running hungry, less by its calm.</summary>
    private static int BreathCost(Hero hero)
    {
        int cost = !hero.Exerting ? Diet.Rest : hero.Eaten.Count == 0 ? Diet.HungrySprint : Diet.Sprint;
        return cost * (1000 - RatesOf(hero).Calm) / 1000;
    }

    // Carried between surfaces (0076).

    /// <summary>The hero of <paramref name="peer"/>, gone from this surface; null when it has none here.</summary>
    public object? Depart(int peer)
    {
        var hero = HeroOf(peer);
        if (hero != null)
        {
            State.Heroes.Remove(hero);
            // A hunt ends with the surface it was on: launching home is an escape (tracker-catchers).
            CallOff(hero);
        }
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
