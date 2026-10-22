namespace Stillwake.Content;

/// <summary>
/// Creatures (0102): the species there are and how every one of them lives, in thousandths of a cell (8 px, 0104) and ticks (20 a second).
/// A planet type says which live on it and where (<see cref="PlanetType.Fauna"/>); the ship has none.
/// </summary>
public static class Fauna
{
    /// <summary>
    /// A species: its <paramref name="Look"/>, the design the client draws it in (its parts in <c>game/art/fauna/</c>), its body
    /// <paramref name="Width"/> × <paramref name="Height"/> cells, its hit points, how fast it wanders and
    /// flees a tick, how long it flees once hurt, its hit-stun — the ticks a hit holds it steering nothing before it flees, 0 for
    /// a neutral one that bolts at once — the knock a hit gives it across and the hop up, its jump — the speed up it leaps at
    /// most, and across it in the air (its highest and longest follow from them under the planet's gravity: `Leaps`), 0 for a
    /// species that never jumps — what it gives when it dies, drawn entry by entry as a container's loot is — and its ram (0103):
    /// the units of suit air a charge of it that hits takes, 0 for one that never rams, and the chance in a thousand that, struck
    /// by a hero within <see cref="RamBackReach"/>, it turns back on it after a short flight, or, fleeing and cornered with a hero
    /// near, it winds up to charge rather than turn.
    /// </summary>
    public sealed record Species(
        string Id, string Look, int Width, int Height, int Hp, int Wander, int Flee, int FleeTicks, int Stun, int Knock, int Hop,
        int Jump, int JumpAcross, IReadOnlyList<LootEntry> Yields, int Ram = 0, int RamChance = 0);

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
    public static readonly Species Loper = new("loper", Look: "classic", Width: 4, Height: 4, Hp: 12, Wander: 200, Flee: 1500,
        FleeTicks: 60, Stun: 0, Knock: 400, Hop: 360, Jump: 880, JumpAcross: 800,
        Yields: [new("biomass", 2, 3), new("seed", 1, 1, OneIn: 4)], Ram: 3, RamChance: 700);

    /// <summary>
    /// The tapir («Тапирчик»): a round, dark-coated walker of the open ground, a glowing seed pod on its snout; the loper's life
    /// in a heavier body. Sixteen hit points (eight of the axe's); a blow knocks it three quarters as far and hops it lower; it
    /// ambles at 3.2 cells a second and bolts at 28, still past a sprint; its jump of 800 up and 720 across is lower and shorter,
    /// 7.2 cells high on the first planet: a wall of six, not of eight. It gives more: three or four biomass, a seed one time in
    /// three (placeholders). Struck, it rams the most often and the hardest (0103): nine times in ten, five units of a suit.
    /// </summary>
    public static readonly Species Tapir = new("tapir", Look: "tapir", Width: 4, Height: 4, Hp: 16, Wander: 160, Flee: 1400,
        FleeTicks: 60, Stun: 0, Knock: 300, Hop: 280, Jump: 800, JumpAcross: 720,
        Yields: [new("biomass", 3, 4), new("seed", 1, 1, OneIn: 3)], Ram: 5, RamChance: 900);

    /// <summary>
    /// The sixleg («Шестиног»): a barrel-bodied grazer of the open ground on six hooves, its tail a stem hung with glowing
    /// berries. The loper's size and the loper's numbers, but for two more hit points for its barrel: fourteen, seven of the
    /// axe's; and the loper's ram (0103).
    /// </summary>
    public static readonly Species Sixleg = new("sixleg", Look: "sixleg", Width: 4, Height: 4, Hp: 14, Wander: 200, Flee: 1500,
        FleeTicks: 60, Stun: 0, Knock: 400, Hop: 360, Jump: 880, JumpAcross: 800,
        Yields: [new("biomass", 2, 3), new("seed", 1, 1, OneIn: 4)], Ram: 3, RamChance: 700);

    /// <summary>
    /// The fluff («Пушок»): a ball of fur on tiny paws, glowing spots down its back and lantern antennae, living on the floors of
    /// the upper caves and the garden band, where its glow lights the dark. Small and light: eight hit points (four of the
    /// axe's); a blow knocks it a fifth farther and hops it higher; it potters at 3.2 cells a second and bolts at 28; its hop is
    /// smaller, 760 up and 720 across, 6.4 cells high on the first planet, still over a ledge of six. One or two biomass, a
    /// seed one time in four (placeholders). It never rams.
    /// </summary>
    public static readonly Species Fluff = new("fluff", Look: "fluff", Width: 4, Height: 4, Hp: 8, Wander: 160, Flee: 1400,
        FleeTicks: 60, Stun: 0, Knock: 480, Hop: 440, Jump: 760, JumpAcross: 720,
        Yields: [new("biomass", 1, 2), new("seed", 1, 1, OneIn: 4)]);

    public static readonly IReadOnlyList<Species> All = [Loper, Tapir, Sixleg, Fluff];

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
    /// A ram (0103). Struck by a hero within <see cref="RamBackReach"/> cells (box distance, by its reported cell), a rammer that
    /// draws its <c>RamChance</c> flees a draw of <see cref="RamFleeLeast"/>..<see cref="RamFleeMost"/> ticks (0.6 to 1 s), then
    /// turns back; fleeing and cornered with the nearest hero within <see cref="RamReach"/>, it draws it again. Either way it
    /// crouches <see cref="WindUp"/> ticks, standing, then charges the nearest hero at <see cref="Charge"/> a tick (28 cells a
    /// second, past a hero's sprint of 26.4) till it is <see cref="RamPast"/> cells past it, at most <see cref="ChargeTicks"/>
    /// (56 cells): a hero farther than that as the wind-up ends is let be.
    /// </summary>
    public const int RamReach = 12, RamBackReach = 24, RamFleeLeast = 12, RamFleeMost = 20, WindUp = 8, ChargeTicks = 40, Charge = 1400,
        RamPast = 6;

    /// <summary>What a fall gains a tick aboard's measure, scaled by the planet's gravity (<c>Kind.Fall</c>), as stacks fall; the fastest fall a tick.</summary>
    public const int Gravity = 60, MaxFall = 1200;
}
