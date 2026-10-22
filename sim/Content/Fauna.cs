namespace Stillwake.Content;

/// <summary>
/// Creatures (0102): the species there are and how every one of them lives, in thousandths of a cell and ticks (20 a second).
/// A planet type says which live on it and where (<see cref="PlanetType.Fauna"/>); the ship has none.
/// </summary>
public static class Fauna
{
    /// <summary>
    /// A species: its body <paramref name="Width"/> × <paramref name="Height"/> cells, its hit points, how fast it wanders and
    /// flees a tick, how long it flees once hurt, its hit-stun — the ticks a hit holds it steering nothing before it flees, 0 for
    /// a neutral one that bolts at once — the knock a hit gives it across and the hop up, its jump — the speed up it leaps at
    /// most, and across it in the air (its highest and longest follow from them under the planet's gravity: `Leaps`), 0 for a
    /// species that never jumps — and what it gives when it dies, drawn entry by entry as a container's loot is.
    /// </summary>
    public sealed record Species(
        string Id, int Width, int Height, int Hp, int Wander, int Flee, int FleeTicks, int Stun, int Knock, int Hop,
        int Jump, int JumpAcross, IReadOnlyList<LootEntry> Yields);

    /// <summary>Where a habitat's creatures spawn: on the open ground under the sky, or on a cave's floor in one depth band (0080).</summary>
    public enum Place { Surface, Band }

    /// <summary>
    /// A species living on a planet type, at <paramref name="Place"/> (in depth band <paramref name="Band"/> for a cave's), drawn
    /// among the type's habitats by <paramref name="Weight"/>.
    /// </summary>
    public sealed record Habitat(string Species, Place Place, int Weight, int Band = 0);

    /// <summary>
    /// The loper: a soft round walker of the first planet's open ground, two cells by two, neutral. It wanders and idles, and
    /// bolts for three seconds when hit at 15 cells a second, faster than a hero's sprint of 13.2, then calms and wanders: a
    /// hunter follows and finds it again, corners it at a cliff or a wall, or stuns it (the arc's stun module to come). No
    /// hit-stun: neutral, it bolts at the first blow, and the drill cannot pin it (the user's word). It jumps (the user's word:
    /// pinned to a wall it could do nothing): at 440 a tick up, on the first planet's gravity some 4.4 cells high, clearing a wall
    /// of four, and 250 across, some ten cells long landing level, clearing a gap of four and more. Twelve hit points: six of the
    /// axe's hits. Its yields are placeholders (the user's word, 2026-10-22): two or three biomass, a seed one time in four.
    /// </summary>
    public static readonly Species Loper = new("loper", Width: 2, Height: 2, Hp: 12, Wander: 100, Flee: 750, FleeTicks: 60,
        Stun: 0, Knock: 200, Hop: 180, Jump: 440, JumpAcross: 250, Yields: [new("biomass", 2, 3), new("seed", 1, 1, OneIn: 4)]);

    public static readonly IReadOnlyList<Species> All = [Loper];

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
    public const int RingFrom = 62, RingTo = 76;

    /// <summary>A creature in some hero's keep box is seen; out of every one it is gone <see cref="Linger"/> after it was last seen, and at once out of every hard box.</summary>
    public static readonly HalfBox Keep = new(80, 48), Hard = new(110, 64);

    public const int Linger = 600;

    /// <summary>How long a creature idles, and wanders, each time: a draw between the least and the most, in ticks.</summary>
    public const int IdleLeast = 20, IdleMost = 80, WanderLeast = 40, WanderMost = 140;

    /// <summary>A walker turns back from a drop this many cells deep or deeper; a shallower one it walks off.</summary>
    public const int Drop = 3;

    /// <summary>
    /// Wandering, a creature jumps only up a ledge this many cells high, so it does not bound about; fleeing, it leaps every wall
    /// and gap its jump clears, a landing found for it first.
    /// </summary>
    public const int WanderLedge = 2;

    /// <summary>What a fall gains a tick aboard's measure, scaled by the planet's gravity (<c>Kind.Fall</c>), as stacks fall; the fastest fall a tick.</summary>
    public const int Gravity = 30, MaxFall = 600;
}
