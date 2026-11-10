namespace Stillwake.Content;

/// <summary>
/// Creatures (0102): the species there are and how every one of them lives, in thousandths of a cell (8 px, 0104) and ticks (20 a second).
/// A planet type says which live on it and where (<see cref="PlanetType.Creatures"/>); the ship has none.
/// </summary>
public static class Creatures
{
    /// <summary>
    /// A species, composed (2026-10-26-creatures-are-composed): its <paramref name="Look"/>, the design the client draws it in (its
    /// parts in <c>game/art/hd/creatures/</c>); its <paramref name="Body"/>, how it is shaped and moves; its <paramref name="Life"/>;
    /// its <paramref name="Faction"/>, one of <see cref="Factions"/>; how long it lives; its <paramref name="Brain"/>, the drives it
    /// may follow, the first ranked highest; its <paramref name="Moves"/>, the acts it may make; what it gives when it dies, drawn
    /// entry by entry as a container's loot is, and the credits it carries to the ship, its <paramref name="Purse"/>, if any. Each record is served by one rule in the creatures module.
    /// A creature saves its drive in charge and its act by their places in <paramref name="Brain"/> and <paramref name="Moves"/>:
    /// reorder or insert into them only with a save-format bump whose step drops or remaps the creatures (<c>CreatureContractTests</c>).
    /// </summary>
    public sealed record Species(
        string Id, string Look, Shape Body, Life Life, string Faction, Lifetime Lifetime, IReadOnlyList<Drive> Brain,
        IReadOnlyList<Move> Moves, IReadOnlyList<LootEntry> Yields, Purse? Purse = null);

    /// <summary>The credits a creature carries, from <paramref name="Min"/> to <paramref name="Max"/> both included, drawn when it dies.</summary>
    public sealed record Purse(int Min, int Max);

    /// <summary>A body's shape: <paramref name="Width"/> × <paramref name="Height"/> cells, its bottom middle where it stands.</summary>
    public abstract record Shape(int Width, int Height);

    /// <summary>
    /// A walker under the surface's gravity: how fast it walks and runs a tick, and its jump — the speed up it leaps at most, and
    /// across it in the air (its highest and longest follow from them under the planet's gravity: `Leaps`), 0 for one that never
    /// jumps.
    /// </summary>
    public sealed record Walker(int Width, int Height, int Walk, int Run, int Jump, int JumpAcross) : Shape(Width, Height);

    /// <summary>
    /// A clinger: clamped to a piece of debris at a point along its top, on one of its perches (<c>Wreckage.Perches</c>), riding its
    /// drift; watching, it crawls along its perch at <paramref name="Crawl"/> a tick, turning at its ends. It leaves the piece only to
    /// fly, straight and through walls, and flies back to clamp on again.
    /// </summary>
    public sealed record Clinger(int Width, int Height, int Crawl = 0) : Shape(Width, Height);

    /// <summary>
    /// A flyer (the catcher's jetpack, tracker-catchers): no gravity; it flies its drive's way, rising first, else across first, solid cells
    /// stopping it; leaving, nothing stops it. On ground it runs at <paramref name="Run"/> a tick (catcher-paths); 0 for one that never does.
    /// </summary>
    public sealed record Flyer(int Width, int Height, int Run = 0) : Shape(Width, Height);

    /// <summary>
    /// How creatures find their way on a grid (catcher-paths): a search at most every <see cref="Replan"/> ticks each, at most one a tick on a
    /// surface, through a window <see cref="Margin"/> cells round its start and goal (<see cref="FlownMargin"/> flying where things float,
    /// round a hull's length, catcher-motion), at most <see cref="Nodes"/> places weighed.
    /// </summary>
    public static class Paths
    {
        public const int Replan = 10, Margin = 24, FlownMargin = 64, Nodes = 3000;
    }

    /// <summary>Its hit points, and the knock a hit gives it across and the hop up, at a full pace's blow (<see cref="KnockPace"/>).</summary>
    public sealed record Life(int Hp, int Knock, int Hop);

    /// <summary>
    /// How long a creature lives: <see cref="Wild"/>, while some hero is near (<see cref="Keep"/>, <see cref="Linger"/>);
    /// <see cref="Posted"/>, that too, but it comes of a post (<c>Flora.Post</c>) rather than a habitat, and goes without a drop when
    /// its post or its arena's relic is gone or its drive calms. Never let out: no post, no guard.
    /// <see cref="Hunter"/>, a catcher (tracker-catchers): kept while it hunts, whatever the keep boxes say, gone once it has left every
    /// hero's view.
    /// </summary>
    public enum Lifetime { Wild, Posted, Hunter }

    /// <summary>Something a creature may want to do, ranked by its place in a brain.</summary>
    public abstract record Drive;

    /// <summary>Idling and wandering by turns, drawn lengths (<see cref="IdleLeast"/>…): what a creature does when nothing else.</summary>
    public sealed record Graze : Drive;

    /// <summary>
    /// Running from what struck it for <paramref name="Ticks"/>; cornered at a wall or a drop with a hero within
    /// <paramref name="CorneredReach"/> cells on the side it would turn to, it turns on it with its first move <paramref name="CorneredChance"/>
    /// times in a thousand (0103); 0 for one that never does.
    /// </summary>
    public sealed record Flee(int Ticks, int CorneredChance = 0, int CorneredReach = 0) : Drive;

    /// <summary>
    /// Struck by a hero within <paramref name="Reach"/> cells, <paramref name="Chance"/> times in a thousand it runs a drawn
    /// <paramref name="FleeLeast"/>..<paramref name="FleeMost"/> ticks, then turns back on it with its first move (0103).
    /// </summary>
    public sealed record Retaliate(int Chance, int Reach, int FleeLeast, int FleeMost) : Drive;

    /// <summary>Running for <paramref name="Ticks"/> after its move, hit or missed: a flight that never turns back.</summary>
    public sealed record Escape(int Ticks) : Drive;

    /// <summary>
    /// Guarding its piece: watching, and alerted while a hero is within <paramref name="Notice"/> cells (box distance); alerted
    /// <paramref name="AlertTicks"/> and a hero within <paramref name="Strike"/>, or struck, or its piece struck, it pounces with its
    /// first move. Alerted, it waits for one to come that close. With <paramref name="Provokes"/> (a cave lurker, lurker-attacks) it
    /// shoots only <paramref name="Provokes"/> ticks after it last answered a hero: struck or within its strike, never at one just
    /// come into sight.
    /// </summary>
    public sealed record Guard(int Notice, int Strike, int AlertTicks, int Provokes = 0) : Drive;

    /// <summary>
    /// A shell (the geode snail, lurker-attacks): a reflex, open <paramref name="Hold"/> ticks after an awake hero was last within
    /// <paramref name="Reach"/> cells, else shut. Shut, no blow hurts it; open, only melee does: never a shot.
    /// </summary>
    public sealed record Shell(int Reach, int Hold) : Drive;

    /// <summary>Flying back to where it clamps on at <paramref name="Speed"/> a tick, wanted whenever it is not clamped on.</summary>
    public sealed record Return(int Speed) : Drive;

    /// <summary>A reflex, never in charge: a harpoon's line on its piece is cut <paramref name="Ticks"/> after it is seen, if still on.</summary>
    public sealed record CutTether(int Ticks) : Drive;

    /// <summary>
    /// Keeping its post's arena (spire guards): asleep till an awake hero's reported cell is in the arena, a blow lands or a sibling is
    /// up; <paramref name="Wake"/> ticks waking; then waiting for its turn to make its first move, no sibling crouching or flying; after
    /// that <paramref name="Stuck"/> ticks on the floor, counted from its landing. <paramref name="Calm"/> ticks with no awake hero in the
    /// arena and no blow, it goes, whole again on a later beat.
    /// </summary>
    public sealed record Ward(int Wake, int Stuck, int Calm) : Drive;

    /// <summary>
    /// Hunting one hero (tracker-catchers): <paramref name="Ticks"/> on its way to it from the tick it comes, running its path on the
    /// ground (catcher-paths), flying or falling at <paramref name="Speed"/> a tick, making its first move at it within
    /// <paramref name="Strike"/> cells in line; where things float its velocity turns toward its way by at most <paramref name="Accel"/> a
    /// tick on each axis (catcher-motion), so it swings wide and overshoots. Out of time it calls the hunt off and leaves straight up. Struck by another hero, it
    /// answers with its second move at most every <paramref name="AnswerEvery"/> ticks, then hunts on.
    /// </summary>
    /// <remarks>
    /// Its fight (catcher-combat): its second move's arc fired on the run (<paramref name="Gunfire"/>); its distance not shrunk for
    /// <paramref name="LagAfter"/> ticks, or no run reaching its quarry, a <paramref name="Blink"/> ahead of it, by how far it went in the last
    /// <paramref name="Sample"/> ticks; a <paramref name="Salvo"/> from the air; a <paramref name="Lob"/> at a shelter it has no way into.
    /// Where things float (catcher-motion) it keeps its <paramref name="Range"/> and now and then fires a <paramref name="Barrage"/>.
    /// </remarks>
    public sealed record Hunt(int Ticks, int Strike, int Speed, int Accel, int AnswerEvery, int LagAfter, int Sample, Blink Blink, Gunfire Gunfire, Salvo Salvo, Lob Lob,
        Range Range, Barrage Barrage) : Drive;

    /// <summary>
    /// A hunter's teleport (catcher-combat): <paramref name="Gather"/> ticks standing, then there, <paramref name="Spent"/> ticks still; at
    /// most every <paramref name="Every"/> ticks. Where: its quarry's way over the gather and <paramref name="Ahead"/> cells past, at most
    /// <paramref name="LeadMost"/>; for a quarry it has no run to, the free spot nearest it within <paramref name="Near"/> cells in its sight.
    /// </summary>
    public sealed record Blink(int Gather, int Spent, int Every, int Ahead, int LeadMost, int Near);

    /// <summary>
    /// A hunter's arc on the run (catcher-combat): every <paramref name="Every"/> ticks at its quarry in sight within the arc's reach, its
    /// gun held still <paramref name="Tell"/> ticks before and the beam struck along it. Its gun turns toward its quarry at most
    /// <paramref name="Turn"/> fine headings a tick (<see cref="Headings.Fine"/>, catcher-motion). On the ground its tell waits till its gun
    /// is on it; where things float it leads its quarry by its drift over the tell and fires on its beat, on or not (catcher-motion).
    /// </summary>
    public sealed record Gunfire(int Every, int Tell, int Turn);

    /// <summary>
    /// A hunter's range where things float (catcher-motion): a spot on a ring <c>(Near + Far) / 2</c> cells round its quarry,
    /// open and in its sight, picked every <c>Paths.Replan</c> ticks, its bearing turned <paramref name="Orbit"/> fine headings its way
    /// round; within <paramref name="Near"/>..<paramref name="Far"/> cells and seeing its quarry, it flies there at half its speed.
    /// </summary>
    public sealed record Range(int Near, int Far, int Orbit);

    /// <summary>
    /// A hunter's barrage where things float (catcher-motion): every <paramref name="Every"/> ticks, <paramref name="Blinks"/>
    /// blinks in a row round its quarry, <paramref name="Step"/> ticks apart, each to a ring <paramref name="From"/> or
    /// <paramref name="To"/> cells off a third of a turn on from the last, its gun set on its quarry's cell and fired a <c>Gunfire.Tell</c>
    /// after; the first after a blink's gather.
    /// </summary>
    public sealed record Barrage(int Every, int Blinks, int Step, int From, int To);

    /// <summary>
    /// A hunter's salvo (catcher-combat): every <paramref name="Every"/> ticks, <paramref name="Headroom"/> cells clear above it (not under
    /// a roof) and its quarry within its arc's reach, it rises up to <paramref name="Hover"/> cells, into the sky, or till it meets solid, and
    /// lobs <paramref name="Shells"/> of <paramref name="Projectile"/> on the ground spread over <paramref name="Spread"/> cells round its
    /// quarry, the first landing <paramref name="Flight"/> ticks later and each next <paramref name="Stagger"/> after it; then it falls and runs on.
    /// </summary>
    public sealed record Salvo(int Every, int Hover, int Headroom, int Shells, int Spread, int Flight, int Stagger, string Projectile);

    /// <summary>
    /// A hunter's lob at a shelter (catcher-combat): no run to its quarry nor a spot in its sight, every <paramref name="Every"/> ticks a
    /// <paramref name="Projectile"/> at what stands between them nearest its quarry, landing <paramref name="Flight"/> ticks later. Where
    /// things float it first keeps <paramref name="Standoff"/> cells off the hull it meets on the way (catcher-standoff), so the throw is seen.
    /// It holds the charge still in its hand <paramref name="Tell"/> ticks before it throws (catcher-motion).
    /// </summary>
    public sealed record Lob(int Every, int Tell, int Flight, int Standoff, string Projectile);

    /// <summary>An act a creature makes, in phases, the suit units it takes from a hero it hits.</summary>
    public abstract record Move(int Damage);

    /// <summary>
    /// A ram along the ground (0103): it crouches <paramref name="WindUp"/> ticks once grounded, then charges the nearest hero at
    /// <paramref name="Speed"/> a tick, never leaping, till it is <paramref name="Past"/> cells past it, a wall or a drop, or
    /// <paramref name="Ticks"/>: a hero farther than that run as the wind-up ends is let be. A blow cuts it short, unless it is
    /// <paramref name="Steady"/> (the geode snail's roll, cave-hostiles): then neither its crouch nor its charge is cut, so a drill held on
    /// it cannot keep it crouching.
    /// </summary>
    public sealed record Lunge(int WindUp, int Speed, int Ticks, int Past, int Damage, bool Steady = false) : Move(Damage);

    /// <summary>
    /// The factions a creature may be of: heroes and what is theirs are <see cref="Crew"/>; <see cref="Beasts"/>, wild creatures that
    /// hunt heroes (cave-hostiles): the cave lurkers now, the carapace pack and the loper's predator next.
    /// </summary>
    public const string Crew = "crew", Grazer = "grazer", Machines = "machines", Beasts = "beasts", Catchers = "catchers";

    public static readonly IReadOnlyList<string> Factions = [Crew, Grazer, Machines, Beasts, Catchers];

    /// <summary>How one faction stands to another: none listed, it ignores it.</summary>
    public enum Stance { Ignore, Fear, Hunt }

    public sealed record Relation(string From, string To, Stance Stance);

    public static readonly IReadOnlyList<Relation> Relations =
        [new(Grazer, Crew, Stance.Ignore), new(Machines, Crew, Stance.Hunt), new(Beasts, Crew, Stance.Hunt), new(Catchers, Crew, Stance.Hunt)];

    /// <summary>
    /// A pounce: crouched <paramref name="WindUp"/> ticks, then a flight straight at its target at <paramref name="Speed"/> a tick on
    /// the longer axis, for <paramref name="Ticks"/>; never cut short by a blow (2026-10-26-creatures-are-composed). A walker's flies
    /// with no gravity and ends at the first solid cell on its way; a clinger's passes walls.
    /// </summary>
    public sealed record Pounce(int WindUp, int Speed, int Ticks, int Damage) : Move(Damage);

    /// <summary>
    /// An arc from where it clings (2026-10-27-sentry-arc): crouched <paramref name="WindUp"/> ticks, its eye flickering, its aim
    /// taken <paramref name="Lock"/> ticks before the end and held; then <paramref name="Projectile"/> (<c>Content.Shots</c>) strikes
    /// at once from its middle to that point, at most <paramref name="Reach"/> cells off, or to the first wall between; a hero it
    /// crossed may say so for <paramref name="Lingers"/> ticks; then <paramref name="Recover"/> ticks before it moves again. The arc
    /// hits, the body never: its damage is the projectile's. Never at a hero nearer than <paramref name="From"/> cells; where it ends
    /// <paramref name="Leaves"/>, a patch's kind, lies on the floor: such an arc is a lob, hurting no one, flying its <paramref name="Lingers"/>
    /// ticks and laying its patch as it lands (lurker-attacks). With a <paramref name="Through"/> it is a beam (catcher-combat): it runs
    /// on past that point, that many cells off in all, to the first wall, and hurts a hero touching any of it all its <paramref name="Lingers"/>.
    /// </summary>
    public sealed record Shoot(int WindUp, int Lock, int Recover, string Projectile, int Reach, int Lingers, int From = 0, string? Leaves = null, int Through = 0) : Move(Damage: 0);

    /// <summary>
    /// A vent (the capback's gas, lurker-attacks): no wind-up; a <paramref name="Patch"/> laid round its middle at once, then
    /// <paramref name="Ticks"/> spent venting; not begun again till <paramref name="Cooldown"/> ticks after it began.
    /// </summary>
    public sealed record Vent(string Patch, int Ticks, int Cooldown) : Move(Damage: 0);

    /// <summary>
    /// What a creature lays that lasts as an area (lurker-attacks): a box <paramref name="Width"/> by <paramref name="Height"/> cells,
    /// gone after <paramref name="Lasts"/> ticks; every breath (<see cref="Crew.BreathEvery"/>) an awake hero in it, in line with its
    /// middle, pays <paramref name="Damage"/> units of a suit.
    /// </summary>
    public sealed record PatchKind(string Id, int Width, int Height, int Lasts, int Damage);

    /// <summary>
    /// The capback's gas: nine cells by six round it, five seconds, two a breath; its slime's puddle: four across on the floor and
    /// four up, to a standing hero's middle, ten seconds, two a breath. Placeholder numbers, for a play pass.
    /// </summary>
    public static readonly PatchKind Gas = new("gas", Width: 9, Height: 6, Lasts: 100, Damage: 2), Slime = new("slime", Width: 4, Height: 4, Lasts: 200, Damage: 2);

    public static readonly IReadOnlyList<PatchKind> Patches = [Gas, Slime];

    /// <summary>How far a patch laid on the floor falls to it, in cells: a glob ending in the air lands this far down at most.</summary>
    public const int PatchFall = 30;

    /// <summary>The patch kind of id <paramref name="id"/>.</summary>
    public static PatchKind PatchOf(string id)
    {
        foreach (var p in Patches)
            if (p.Id == id) return p;
        throw new ArgumentException($"no patch kind {id}");
    }

    /// <summary>The ram of the loper and the sixleg: three units of a suit; at 28 cells a second, past a hero's sprint of 26.4.</summary>
    private static readonly Lunge LightRam = new(WindUp: 8, Speed: 1400, Ticks: 40, Past: 6, Damage: 3);

    /// <summary>The flight of every grazer: three seconds.</summary>
    private const int FleeTicks = 60;

    /// <summary>Where a habitat's creatures spawn: on the open ground under the sky, or on a cave's floor in one depth band (0080).</summary>
    public enum Place { Surface, Band }

    /// <summary>
    /// A species living on a planet type, at <paramref name="Place"/> (in depth band <paramref name="Band"/> for a cave's), drawn
    /// among the type's habitats by <paramref name="Weight"/>.
    /// </summary>
    public sealed record Habitat(string Species, Place Place, int Weight, int Band = 0);

    /// <summary>
    /// The loper: a soft round walker of the first planet's open ground, four cells by four, neutral, drawn as the loper page's
    /// «Классика» (the user's pick, 2026-10-22, with the three kinds after it). It wanders and idles, and
    /// bolts for three seconds when hit at 30 cells a second, faster than a hero's sprint of 26.4, then calms and wanders: a
    /// hunter follows and finds it again, corners it at a cliff or a wall, or stuns it (the arc's stun module to come). No
    /// hit-stun: neutral, it bolts at the first blow, and the drill cannot pin it (the user's word). It jumps (the user's word:
    /// pinned to a wall it could do nothing): at 880 a tick up, on the first planet's gravity some 8.8 cells high, clearing a wall
    /// of eight, and 800 across, some 34 cells long landing level, clearing a gap of two dozen and more. Twelve hit points: six of the
    /// axe's hits. Its yields are placeholders (the user's word, 2026-10-22): two or three biomass, a seed one time in four, and one
    /// or two each of hide and fat (the economy rework: a grazer drops what it is made of).
    /// Struck, it mostly turns back and rams (0103, the user's word after playing): seven times in ten, three units of a suit.
    /// </summary>
    public static readonly Species Loper = new("loper", Look: "classic",
        Body: new Walker(Width: 4, Height: 4, Walk: 200, Run: 1500, Jump: 880, JumpAcross: 800), Life: new(Hp: 12, Knock: 400, Hop: 360),
        Faction: Grazer, Lifetime: Lifetime.Wild,
        Brain: [new Retaliate(Chance: 700, Reach: 24, FleeLeast: 12, FleeMost: 20), new Flee(FleeTicks, CorneredChance: 700, CorneredReach: 12),
            new Escape(FleeTicks), new Graze()],
        Moves: [LightRam], Yields: [new("biomass", 2, 3), new("seed", 1, 1, OneIn: 4), new("hide", 1, 2), new("fat", 1, 2)]);

    /// <summary>
    /// The tapir («Тапирчик»): a round, dark-coated walker of the open ground, a glowing seed pod on its snout; the loper's life
    /// in a heavier body. Sixteen hit points (eight of the axe's); a blow knocks it three quarters as far and hops it lower; it
    /// ambles at 3.2 cells a second and bolts at 28, still past a sprint; its jump of 800 up and 720 across is lower and shorter,
    /// 7.2 cells high on the first planet: a wall of six, not of eight. It gives the most: three or four biomass, a seed one time in
    /// three, two or three each of hide and fat (placeholders). Struck, it rams the most often and the hardest (0103): nine times
    /// in ten, five units of a suit.
    /// </summary>
    public static readonly Species Tapir = new("tapir", Look: "tapir",
        Body: new Walker(Width: 4, Height: 4, Walk: 160, Run: 1400, Jump: 800, JumpAcross: 720), Life: new(Hp: 16, Knock: 300, Hop: 280),
        Faction: Grazer, Lifetime: Lifetime.Wild,
        Brain: [new Retaliate(Chance: 900, Reach: 24, FleeLeast: 12, FleeMost: 20), new Flee(FleeTicks, CorneredChance: 900, CorneredReach: 12),
            new Escape(FleeTicks), new Graze()],
        Moves: [LightRam with { Damage = 5 }], Yields: [new("biomass", 3, 4), new("seed", 1, 1, OneIn: 3), new("hide", 2, 3), new("fat", 2, 3)]);

    /// <summary>
    /// The sixleg («Шестиног»): a barrel-bodied grazer of the open ground on six hooves, its tail a stem hung with glowing
    /// berries. The loper's size and the loper's numbers, but for two more hit points for its barrel: fourteen, seven of the
    /// axe's; and the loper's ram (0103).
    /// </summary>
    public static readonly Species Sixleg = Loper with { Id = "sixleg", Look = "sixleg", Life = Loper.Life with { Hp = 14 } };

    /// <summary>
    /// The sentry (2026-10-26-creatures-are-composed): a robot clamped to a derelict, three cells by three, its eye a lamp, the tick
    /// the user picked. Watching, it crawls along its perch at 0.8 cells a second. A hero within 40 cells alerts it (the user's
    /// word: it sees far); alerted a second, a hero within twenty and it pounces: a crouch of 0.8 s, then a flight of half a
    /// second at 40 cells a second, its return's, some twenty cells, six units of a suit if it hits; farther, within its notice,
    /// it strikes (2026-10-27-sentry-arc): its eye flickers 0.6 s, its aim held for the last 0.2 s, then an arc strikes at once up
    /// to 60 cells, stopped by walls, four units of a suit off each hero it crosses, and it waits 2 s. Struck, or its wreck
    /// struck, it answers the striker at once the same way, by how far it is. It flies home at 40 cells a second, past a
    /// jetpack. Thirty hit points, five of the nailer's nails; never knocked. A harpoon on its wreck is cut 0.8 s after it bites.
    /// It dies with its wreck, dropping scrap and two or three circuits, a fighter's share of what is rare. Placeholder numbers, for a play pass.
    /// </summary>
    public static readonly Species Sentry = new("sentry", Look: "sentry", Body: new Clinger(Width: 3, Height: 3, Crawl: 40),
        Life: new(Hp: 30, Knock: 0, Hop: 0), Faction: Machines, Lifetime: Lifetime.Wild,
        Brain: [new CutTether(Ticks: 16), new Return(Speed: 2000), new Guard(Notice: 40, Strike: 20, AlertTicks: 20)],
        Moves: [new Pounce(WindUp: 16, Speed: 2000, Ticks: 10, Damage: 6), new Shoot(WindUp: 12, Lock: 4, Recover: 40, Projectile: "bolt", Reach: 60, Lingers: 10)],
        Yields: [new("scrapChunk", 3, 5), new("circuit", 2, 3)]);

    /// <summary>
    /// The warden, the Istukan (spire guards): a machine of the arc makers, two to a spire's hall, each by a post (<c>Ruins.PostLeft</c>,
    /// <c>Ruins.PostRight</c>), four cells by five, folded in its niche like a column. By turns it crouches 0.8 s and flies straight at
    /// 36 cells a second, 1.4 s at most, five units of a suit if it hits, and lies stuck a second where it fell. It takes 24 hit points,
    /// four of the nailer's nails or six of the axe's; never knocked. Killed it gives two or three circuits and a little alloy scrap, and
    /// its post is felled for good; the relic taken, it falls with nothing. Placeholder numbers, for a play pass.
    /// </summary>
    public static readonly Species Warden = new("warden", Look: "istukan",
        Body: new Walker(Width: 4, Height: 5, Walk: 0, Run: 0, Jump: 0, JumpAcross: 0), Life: new(Hp: 24, Knock: 0, Hop: 0),
        Faction: Machines, Lifetime: Lifetime.Posted, Brain: [new Ward(Wake: 10, Stuck: 20, Calm: 100)],
        Moves: [new Pounce(WindUp: 16, Speed: 1800, Ticks: 28, Damage: 5)],
        Yields: [new("circuit", 2, 3), new("alloyScrap", 1, 2)]);

    /// <summary>
    /// The capback («Шляпоспин», cave-hostiles, lurker-attacks): a low beetle of the teal garden whose shell grows glowcaps, four cells
    /// by three, lurking among the real ones. An awake hero in line within 20 cells alerts it, and it only watches. Alerted a quarter
    /// second, one within 4, about touching, and it vents at once: its gas (<see cref="Gas"/>) round it, then 10 s before it vents
    /// again. Venting, or struck, it is provoked 30 s: then a hero 8 to 20 cells off is spat at, a crouch of 0.6 s, its aim held the
    /// last 0.2 s, the glob lobbed 0.6 s to the floor there, its puddle (<see cref="Slime"/>) laid as it lands, 3 s before the next. It never walks off its spot. Ten hit
    /// points, five of the axe's. Placeholder numbers, for a play pass.
    /// </summary>
    public static readonly Species Capback = new("capback", Look: "capback",
        Body: new Walker(Width: 4, Height: 3, Walk: 100, Run: 100, Jump: 0, JumpAcross: 0), Life: new(Hp: 10, Knock: 300, Hop: 200),
        Faction: Beasts, Lifetime: Lifetime.Wild, Brain: [new Guard(Notice: 20, Strike: 4, AlertTicks: 5, Provokes: 600)],
        Moves: [new Vent("gas", Ticks: 10, Cooldown: 200), new Shoot(WindUp: 12, Lock: 4, Recover: 60, Projectile: "slime", Reach: 20, Lingers: 12, From: 8, Leaves: "slime")],
        Yields: [new("biomass", 2, 3), new("spore", 1, 1, OneIn: 2)]);

    /// <summary>
    /// The geode snail («Жеодник», cave-hostiles, lurker-attacks): a snail of the amber deep whose shell is a geode, four cells by
    /// three. It sits shut: no blow hurts it. An awake hero within 4 cells and it opens, a second after the last one left it shuts;
    /// open, melee hurts it, a shot never. Alerted (6 cells, in line) 0.8 s with one within 4, it bites: a lurch of 0.8 s, then 16
    /// cells a second for a quarter second at the hero, three units of a suit if it touches. Twenty-four hit points, never knocked. Placeholder
    /// numbers, for a play pass.
    /// </summary>
    public static readonly Species GeodeSnail = new("geodeSnail", Look: "geodeSnail",
        Body: new Walker(Width: 4, Height: 3, Walk: 30, Run: 30, Jump: 0, JumpAcross: 0), Life: new(Hp: 24, Knock: 0, Hop: 0),
        Faction: Beasts, Lifetime: Lifetime.Wild, Brain: [new Guard(Notice: 6, Strike: 4, AlertTicks: 16), new Shell(Reach: 4, Hold: 20)],
        Moves: [new Lunge(WindUp: 16, Speed: 800, Ticks: 5, Past: 0, Damage: 3, Steady: true)], Yields: [new("crystal", 2, 3), new("biomass", 1, 1)]);

    /// <summary>
    /// The catcher (tracker-catchers): a hunter on a jetpack hired by the hero's former owners, three cells by five, come for one hero whose
    /// wanted meter is full. From the tick it comes, 90 s on its way to its hero: running at a sprint's 26.4 cells a second, falling at
    /// 20; a laser at its hero in sight every 2.5 s, its gun turning at 3 radians a second and held 0.3 s, running on 120 cells and burning 0.3 s; lagging 3 s, or no
    /// run to its hero, it gathers 0.6 s and is there, 20 cells ahead of its hero or, for one off the ground's way, within 8 in sight, then stands 0.5 s, every 6 s at most; every 12 s
    /// under open sky it rises 20 cells and lobs seven shells over 48 round its hero, landing from 1.2 s; shut out, a blasting charge at the
    /// wall every 8 s. In space it keeps 20 to 35 cells off, drifting round its hero, its laser leading it and fired on its beat; every
    /// 25 s it blinks ten times round its hero, 0.4 s apart, a shot 0.3 s after each. Within 12 cells in line it crouches 0.8 s and flies fists first, 16 cells
    /// in half a second, six units of a suit if it hits. Struck by another hero, it answers with a quick laser at it, 0.3 s crouched,
    /// at most once in 3 s. Forty hit points, never knocked. Killed, scrap, a circuit, now and then a crystal or an opal, and 150 to 300
    /// credits for the ship: what it was paid for the hero. Placeholder numbers, for a play pass.
    /// </summary>
    public static readonly Species Catcher = new("catcher", Look: "catcher", Body: new Flyer(Width: 3, Height: 5, Run: 1320),
        Life: new(Hp: 40, Knock: 0, Hop: 0), Faction: Catchers, Lifetime: Lifetime.Hunter,
        Brain: [new Hunt(Ticks: 1800, Strike: 12, Speed: 1000, Accel: 100, AnswerEvery: 60, LagAfter: 60, Sample: 10,
            Blink: new(Gather: 12, Spent: 10, Every: 120, Ahead: 20, LeadMost: 96, Near: 8), Gunfire: new(Every: 50, Tell: 6, Turn: 98),
            Salvo: new(Every: 240, Hover: 20, Headroom: 6, Shells: 7, Spread: 48, Flight: 24, Stagger: 2, Projectile: "shell"),
            Lob: new(Every: 160, Tell: 12, Flight: 20, Standoff: 16, Projectile: "blastCharge"),
            Range: new(Near: 20, Far: 35, Orbit: 32), Barrage: new(Every: 500, Blinks: 10, Step: 8, From: 12, To: 20))],
        Moves: [new Pounce(WindUp: 16, Speed: 1600, Ticks: 10, Damage: 6), new Shoot(WindUp: 6, Lock: 2, Recover: 20, Projectile: "laser", Reach: 60, Lingers: 6, Through: 120)],
        Yields: [new("scrapChunk", 2, 4), new("circuit", 1, 1), new("crystal", 1, 1, OneIn: 5), new("opal", 1, 1, OneIn: 10)],
        Purse: new(150, 300));

    public static readonly IReadOnlyList<Species> All = [Loper, Tapir, Sixleg, Sentry, Warden, Capback, GeodeSnail, Catcher];

    /// <summary>
    /// A species that comes with debris pieces of some kinds as they drift in, clamped to a perch on one's top (<c>Wreckage.Perches</c>):
    /// each new piece of a kind, its look with a perch, draws once, the species coming with it <paramref name="PerMille"/>
    /// times in a thousand. The species is a clinger.
    /// </summary>
    public sealed record OnDebris(string Species, IReadOnlyList<(string Item, int PerMille)> Kinds);

    /// <summary>
    /// Sentries on about every other engine block and cargo hold out in the far field (0056), whose pictures have a flat top to stand
    /// on (<c>Wreckage.Perches</c>): a sortie's prize is guarded. Pod clusters have none.
    /// </summary>
    public static readonly OnDebris Derelicts = new(Sentry.Id, [("engineBlock", 500), ("cargoHold", 500)]);

    /// <summary>
    /// A tool striking this many ticks apart or more knocks a creature its species' whole knock and hop; a faster one its pace's
    /// share of them, rounded down, so a second of the drill's ten hits pushes about as far as the axe's two (the axe's pace).
    /// </summary>
    public const int KnockPace = 10;

    /// <summary>The species of that id; one not here is our bug: peers run one build.</summary>
    public static Species Get(string id)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Id == id) return All[i];
        throw new KeyNotFoundException($"No species '{id}'.");
    }

    /// <summary>Spawns come on this beat, the module's clock a multiple of it: every 2 s.</summary>
    public const int Beat = 40;

    /// <summary>Wild creatures come as a herd of one kind (creatures-in-herds), this many to that many, so many columns apart.</summary>
    public const int HerdLeast = 2, HerdMost = 4, Spacing = 6;

    /// <summary>No herd comes within this many cells of a creature already there, either way: herds keep apart.</summary>
    public const int Apart = 32;

    /// <summary>A grazer more than this many columns from the nearest of its kind wanders toward it: a herd keeps together.</summary>
    public const int Herd = 16;

    /// <summary>No herd for a hero whose keep box would hold more than this many creatures with it: about two herds.</summary>
    public const int PerKeep = 6;

    /// <summary>At most this many creatures on a surface.</summary>
    public const int Max = 40;

    /// <summary>What a hero is taken to see: the farthest zoom's view, the debris' (0052).</summary>
    public static readonly HalfBox View = Debris.View;

    /// <summary>
    /// A herd's first column is this many columns either side of a hero's reported one, a draw between the two, the rest going
    /// outward: just past the view, so what spawns is never seen to appear, and the farthest inside the keep box.
    /// </summary>
    public const int RingFrom = 124, RingTo = 136;

    /// <summary>A creature in some hero's keep box is seen; out of every one it is gone <see cref="Linger"/> after it was last seen, and at once out of every hard box.</summary>
    public static readonly HalfBox Keep = new(160, 96), Hard = new(220, 128);

    public const int Linger = 600;

    /// <summary>How long a creature idles, and wanders, each time: a draw between the least and the most, in ticks.</summary>
    public const int IdleLeast = 20, IdleMost = 80, WanderLeast = 40, WanderMost = 140;

    /// <summary>A walker turns back from a drop this many cells deep or deeper; a shallower one it walks off.</summary>
    public const int Drop = 6;

    /// <summary>
    /// How high a walker steps up onto a ledge it walks into, and down off one, without a jump: two cells, the one 16 px cell it
    /// stepped before the cell was halved (0104).
    /// </summary>
    public const int StepUp = 2;

    /// <summary>
    /// Wandering, a creature jumps only up a ledge this many cells high, so it does not bound about; fleeing, it leaps every wall
    /// and gap its jump clears, a landing found for it first.
    /// </summary>
    public const int WanderLedge = 4;

    /// <summary>
    /// How near, in cells by box distance, a creature that may hit a hero makes that hero's client report where it is every tick
    /// it moves (2026-10-26-creatures-are-composed, Terraria's way): past a retaliation's flight and a lunge's run of 56.
    /// </summary>
    public const int ThreatReach = 96;

    /// <summary>What a fall gains a tick aboard's measure, scaled by the planet's gravity (<c>Kind.Fall</c>), as stacks fall; the fastest fall a tick.</summary>
    public const int Gravity = 60, MaxFall = 1200;
}
