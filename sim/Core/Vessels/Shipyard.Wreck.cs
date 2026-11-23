using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>Wrecked vessels (vessel-wrecks): a class built, then its chain of events drawn or forced and applied to its parts.</summary>
public static partial class Shipyard
{
    /// <summary>
    /// The directions an event may take, unscaled, and as unit vectors in thousandths: straight down, steep, slanting, shallow,
    /// straight across; signs as the event draws them.
    /// </summary>
    public static readonly IReadOnlyList<(int X, int Y, int Ux, int Uy)> Directions =
        [(0, 1, 0, 1000), (1, 2, 447, 894), (1, 1, 707, 707), (2, 1, 894, 447), (1, 0, 1000, 0)];

    /// <summary>
    /// A vessel of <paramref name="cls"/> from <paramref name="seed"/> as built (vessel-wrecks): described, stamped, then its chain applied.
    /// The chain is <paramref name="forced"/>'s steps in order, each skipped where it is not allowed at its turn (an empty one an intact
    /// vessel); with none, a wreck <c>WreckPercent</c> times in a hundred, its count drawn in the class's <c>Events</c>, each next event
    /// drawn evenly from those allowed on the ship as it stands. Draws are the vessel's own streams: the wreck 3, event <c>i</c>'s
    /// geometry 4 + i, scavenging 9; an intact vessel draws only whether it is a wreck, so stamps as before.
    /// </summary>
    public static Built Build(VesselClass cls, uint seed, IReadOnlyList<WreckStep>? forced = null) => Build(cls, seed, forced, Wrecks.WreckPercent);

    /// <summary>
    /// Whether <see cref="Build"/> makes a wreck of <paramref name="cls"/> from <paramref name="seed"/>, without building it: a forced
    /// chain with steps, else the seed's own first draw (a drawn wreck always has an event: icing first is always allowed). Cheap, so
    /// the radar marks every contact by it. A forced chain whose every step is refused still builds intact: dev tools' only.
    /// </summary>
    public static bool IsWreck(VesselClass cls, uint seed, IReadOnlyList<WreckStep>? forced = null) =>
        forced != null ? forced.Count > 0 : Draws(cls, seed, 3).Below(100) < Wrecks.WreckPercent;

    /// <summary><see cref="Build(VesselClass, uint, IReadOnlyList{WreckStep}?)"/> with a vessel a wreck <paramref name="wreckPercent"/> times in a hundred: the tests' every seed a wreck.</summary>
    internal static Built Build(VesselClass cls, uint seed, IReadOnlyList<WreckStep>? forced, int wreckPercent)
    {
        var v = Describe(cls, seed);
        var parts = Stamp(v);
        int entry = EntryOf(v);
        var intact = new Built(v, null, parts) { Entry = entry };
        if (forced is { Count: 0 }) return intact;
        var rng = Draws(cls, seed, 3);
        if (forced == null && rng.Below(100) >= wreckPercent) return intact;
        int count = forced?.Count ?? Between(rng, cls.Events);
        var hulk = new Hulk(cls, v, parts, entry);
        var chain = new List<WreckKind>();
        var asked = new List<bool>();
        for (int i = 0; i < count; i++)
        {
            var allowed = hulk.Allowed(chain);
            WreckKind kind;
            if (forced != null)
            {
                kind = forced[i].Kind;
                if (!allowed.Contains(kind)) continue;
                asked.Add(forced[i].Old);
            }
            else
            {
                if (allowed.Count == 0) break;
                kind = allowed[(int)rng.Below((uint)allowed.Count)];
            }
            hulk.Apply(kind, chain, Draws(cls, seed, 4u + (uint)chain.Count));
            chain.Add(kind);
        }
        if (chain.Count == 0) return intact;
        var groups = new int[chain.Count];
        for (int i = 1; i < chain.Count; i++) groups[i] = Follows(chain[i - 1], chain[i]) ? groups[i - 1] : groups[i - 1] + 1;
        var old = new bool[chain.Count];
        bool lastFresh = forced == null && rng.Below(100) < Wrecks.FreshPercent;
        for (int i = 0; i < chain.Count; i++)
            old[i] = forced != null ? (i > 0 && groups[i] == groups[i - 1] ? old[i - 1] : asked[i]) : groups[i] != groups[^1] || !lastFresh;
        IReadOnlyList<WreckPatch> patches = old[0] ? hulk.Strip(Draws(cls, seed, 9)) : [];
        return hulk.Laid(entry, [.. Enumerable.Range(0, chain.Count).Select(i => (old[i], groups[i]))], old[0], patches);
    }

    /// <summary>
    /// Whether <paramref name="next"/> right after <paramref name="before"/> is its consequence, so joins its group: fire after an
    /// explosion; icing after an explosion, an asteroid or a break; a break after an explosion or an asteroid.
    /// </summary>
    public static bool Follows(WreckKind before, WreckKind next) => next switch
    {
        WreckKind.Fire => before == WreckKind.Explosion,
        WreckKind.Icing => before is WreckKind.Explosion or WreckKind.Asteroid or WreckKind.Break,
        WreckKind.Break => before is WreckKind.Explosion or WreckKind.Asteroid,
        _ => false,
    };

    /// <summary>The intact vessel's way in: its first door in reading order, by index.</summary>
    private static int EntryOf(Vessel v)
    {
        int best = 0;
        for (int d = 1; d < v.Doors.Count; d++)
            if (v.Doors[d].Y < v.Doors[best].Y || v.Doors[d].Y == v.Doors[best].Y && v.Doors[d].X < v.Doors[best].X) best = d;
        return best;
    }
}
