namespace Stillwake.Content;

/// <summary>
/// Creatures (0102): the species there are and how every one of them lives, in thousandths of a cell (8 px, 0104) and ticks (20 a second).
/// A planet type says which live on it and where (<see cref="PlanetType.Creatures"/>); the ship has none.
/// </summary>
public static class Creatures
{
    /// <summary>
    /// A species, composed (2026-10-26-creatures-are-composed): its <paramref name="Look"/>, the design the client draws it in (its
    /// parts in <c>game/art/creatures/</c>); its <paramref name="Body"/>, how it is shaped and moves; its <paramref name="Life"/>;
    /// its <paramref name="Faction"/>, one of <see cref="Factions"/>; how long it lives; its <paramref name="Brain"/>, the drives it
    /// may follow, the first ranked highest; its <paramref name="Moves"/>, the acts it may make; what it gives when it dies, drawn
    /// entry by entry as a container's loot is. Each record is served by one rule in the creatures module.
    /// A creature saves its drive in charge and its act by their places in <paramref name="Brain"/> and <paramref name="Moves"/>:
    /// reorder or insert into them only with a save-format bump whose step drops or remaps the creatures (<c>CreatureContractTests</c>).
    /// </summary>
    public sealed record Species(
        string Id, string Look, Shape Body, Life Life, string Faction, Lifetime Lifetime, IReadOnlyList<Drive> Brain,
        IReadOnlyList<Move> Moves, IReadOnlyList<LootEntry> Yields);

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

    /// <summary>Its hit points, and the knock a hit gives it across and the hop up, at a full pace's blow (<see cref="KnockPace"/>).</summary>
    public sealed record Life(int Hp, int Knock, int Hop);

    /// <summary>How long a creature lives: <see cref="Wild"/>, while some hero is near (<see cref="Keep"/>, <see cref="Linger"/>).</summary>
    public enum Lifetime { Wild }

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
    /// <paramref name="AlertTicks"/>, or struck, or its piece struck, it pounces with its first move.
    /// </summary>
    public sealed record Guard(int Notice, int AlertTicks) : Drive;

    /// <summary>Flying back to where it clamps on at <paramref name="Speed"/> a tick, wanted whenever it is not clamped on.</summary>
    public sealed record Return(int Speed) : Drive;

    /// <summary>A reflex, never in charge: a harpoon's line on its piece is cut the tick it is seen.</summary>
    public sealed record CutTether : Drive;

    /// <summary>An act a creature makes, in phases, the suit units it takes from a hero it hits.</summary>
    public abstract record Move(int Damage);

    /// <summary>
    /// A ram along the ground (0103): it crouches <paramref name="WindUp"/> ticks once grounded, then charges the nearest hero at
    /// <paramref name="Speed"/> a tick, never leaping, till it is <paramref name="Past"/> cells past it, a wall or a drop, or
    /// <paramref name="Ticks"/>: a hero farther than that run as the wind-up ends is let be. A blow cuts it short.
    /// </summary>
    public sealed record Lunge(int WindUp, int Speed, int Ticks, int Past, int Damage) : Move(Damage);

    /// <summary>The factions a creature may be of: heroes and what is theirs are <see cref="Crew"/>.</summary>
    public const string Crew = "crew", Grazer = "grazer", Machines = "machines";

    public static readonly IReadOnlyList<string> Factions = [Crew, Grazer, Machines];

    /// <summary>How one faction stands to another: none listed, it ignores it.</summary>
    public enum Stance { Ignore, Fear, Hunt }

    public sealed record Relation(string From, string To, Stance Stance);

    public static readonly IReadOnlyList<Relation> Relations = [new(Grazer, Crew, Stance.Ignore), new(Machines, Crew, Stance.Hunt)];

    /// <summary>
    /// A pounce: crouched <paramref name="WindUp"/> ticks, then a flight straight at its target at <paramref name="Speed"/> a tick on
    /// the longer axis, for <paramref name="Ticks"/>; never cut short by a blow (2026-10-26-creatures-are-composed).
    /// </summary>
    public sealed record Pounce(int WindUp, int Speed, int Ticks, int Damage) : Move(Damage);

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
    /// axe's hits. Its yields are placeholders (the user's word, 2026-10-22): two or three biomass, a seed one time in four.
    /// Struck, it mostly turns back and rams (0103, the user's word after playing): seven times in ten, three units of a suit.
    /// </summary>
    public static readonly Species Loper = new("loper", Look: "classic",
        Body: new Walker(Width: 4, Height: 4, Walk: 200, Run: 1500, Jump: 880, JumpAcross: 800), Life: new(Hp: 12, Knock: 400, Hop: 360),
        Faction: Grazer, Lifetime: Lifetime.Wild,
        Brain: [new Retaliate(Chance: 700, Reach: 24, FleeLeast: 12, FleeMost: 20), new Flee(FleeTicks, CorneredChance: 700, CorneredReach: 12),
            new Escape(FleeTicks), new Graze()],
        Moves: [LightRam], Yields: [new("biomass", 2, 3), new("seed", 1, 1, OneIn: 4)]);

    /// <summary>
    /// The tapir («Тапирчик»): a round, dark-coated walker of the open ground, a glowing seed pod on its snout; the loper's life
    /// in a heavier body. Sixteen hit points (eight of the axe's); a blow knocks it three quarters as far and hops it lower; it
    /// ambles at 3.2 cells a second and bolts at 28, still past a sprint; its jump of 800 up and 720 across is lower and shorter,
    /// 7.2 cells high on the first planet: a wall of six, not of eight. It gives more: three or four biomass, a seed one time in
    /// three (placeholders). Struck, it rams the most often and the hardest (0103): nine times in ten, five units of a suit.
    /// </summary>
    public static readonly Species Tapir = new("tapir", Look: "tapir",
        Body: new Walker(Width: 4, Height: 4, Walk: 160, Run: 1400, Jump: 800, JumpAcross: 720), Life: new(Hp: 16, Knock: 300, Hop: 280),
        Faction: Grazer, Lifetime: Lifetime.Wild,
        Brain: [new Retaliate(Chance: 900, Reach: 24, FleeLeast: 12, FleeMost: 20), new Flee(FleeTicks, CorneredChance: 900, CorneredReach: 12),
            new Escape(FleeTicks), new Graze()],
        Moves: [LightRam with { Damage = 5 }], Yields: [new("biomass", 3, 4), new("seed", 1, 1, OneIn: 3)]);

    /// <summary>
    /// The sixleg («Шестиног»): a barrel-bodied grazer of the open ground on six hooves, its tail a stem hung with glowing
    /// berries. The loper's size and the loper's numbers, but for two more hit points for its barrel: fourteen, seven of the
    /// axe's; and the loper's ram (0103).
    /// </summary>
    public static readonly Species Sixleg = Loper with { Id = "sixleg", Look = "sixleg", Life = Loper.Life with { Hp = 14 } };

    /// <summary>
    /// The fluff («Пушок»): a ball of fur on tiny paws, glowing spots down its back and lantern antennae, living on the floors of
    /// the upper caves and the garden band, where its glow lights the dark. Small and light: eight hit points (four of the
    /// axe's); a blow knocks it a fifth farther and hops it higher; it potters at 3.2 cells a second and bolts at 28; its hop is
    /// smaller, 760 up and 720 across, 6.4 cells high on the first planet, still over a ledge of six. One or two biomass, a
    /// seed one time in four (placeholders). It never rams.
    /// </summary>
    public static readonly Species Fluff = new("fluff", Look: "fluff",
        Body: new Walker(Width: 4, Height: 4, Walk: 160, Run: 1400, Jump: 760, JumpAcross: 720), Life: new(Hp: 8, Knock: 480, Hop: 440),
        Faction: Grazer, Lifetime: Lifetime.Wild, Brain: [new Flee(FleeTicks), new Graze()], Moves: [],
        Yields: [new("biomass", 1, 2), new("seed", 1, 1, OneIn: 4)]);

    /// <summary>
    /// The sentry (2026-10-26-creatures-are-composed): a robot clamped to a derelict, three cells by two, its eye a lamp, the tick
    /// the user picked. Watching, it crawls along its perch at 0.8 cells a second. It
    /// watches; a hero within twelve cells alerts it for a second, then it pounces: a crouch of 0.8 s, then a flight of 0.85 s at
    /// 24 cells a second, some twenty cells, six units of a suit if it hits. Struck, or its wreck struck, it crouches at once.
    /// Forty hit points, never knocked. A harpoon on its wreck is cut. Placeholder numbers, for a play pass.
    /// </summary>
    public static readonly Species Sentry = new("sentry", Look: "sentry", Body: new Clinger(Width: 3, Height: 2, Crawl: 40),
        Life: new(Hp: 40, Knock: 0, Hop: 0), Faction: Machines, Lifetime: Lifetime.Wild,
        Brain: [new CutTether(), new Return(Speed: 600), new Guard(Notice: 12, AlertTicks: 20)],
        Moves: [new Pounce(WindUp: 16, Speed: 1200, Ticks: 17, Damage: 6)], Yields: [new("scrapChunk", 3, 5)]);

    public static readonly IReadOnlyList<Species> All = [Loper, Tapir, Sixleg, Fluff, Sentry];

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

    /// <summary>No spawn for a hero whose keep box holds this many creatures already.</summary>
    public const int PerKeep = 10;

    /// <summary>At most this many creatures on a surface.</summary>
    public const int Max = 40;

    /// <summary>What a hero is taken to see: the farthest zoom's view, the debris' (0052).</summary>
    public static readonly HalfBox View = Debris.View;

    /// <summary>
    /// A spawn's column is this many columns either side of a hero's reported one, a draw between the two: just past the view, so
    /// what spawns is never seen to appear, and inside the keep box.
    /// </summary>
    public const int RingFrom = 124, RingTo = 152;

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
