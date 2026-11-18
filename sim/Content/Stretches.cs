namespace Stillwake.Content;

/// <summary>
/// Stretches (docs/decisions/2026-11-15-stretches-of-space.md): spells a surface goes through, listed on its kind
/// (<c>Surfaces.Kind.Stretches</c>), each opened by what the crew has obtained or from the start, then coming again and again. One at
/// a time is chosen, seen coming for its <see cref="Stretch.Warning"/> ticks, and runs: colder under the open sky by its
/// <see cref="Stretch.Chill"/>; aboard, its own debris zone thickens the stream; on a planet it is weather.
/// All numbers are placeholders for a play pass.
/// </summary>
public static class Stretches
{
    /// <summary>A span of ticks drawn by the sky's fork of the world's Rng, both ends included.</summary>
    public sealed record Span(int Least, int Most);

    /// <summary>
    /// A storm's lightning (docs/plans/surface-storms.md): its first bolt drawn <paramref name="At"/> ticks into the storm, it glows
    /// <paramref name="Glow"/> ticks over the highest open-sky wall cell within <paramref name="Columns"/> columns of one hero drawn, then
    /// strikes it with a burst of <paramref name="Power"/> within <paramref name="Reach"/> cells, and every hero under the open sky within
    /// <paramref name="Spread"/> columns of it loses <paramref name="Shock"/> air. Each next one <paramref name="Again"/> ticks after the
    /// last struck; none more a storm without.
    /// </summary>
    public sealed record BoltRule(Span At, int Glow, int Columns, int Power, int Reach, int Spread, int Shock, Span? Again = null);

    /// <summary>
    /// A stretch: opened once any of <paramref name="Unlock"/> has been obtained, from the start with none; its first coming
    /// <paramref name="First"/> after it is chosen, every later one <paramref name="Gap"/> after the last ended, the last
    /// <paramref name="Warning"/> ticks of either its warning; lasting <paramref name="Duration"/>; <paramref name="Chill"/> degrees
    /// colder under the open sky while it runs; its pieces drifting by in <paramref name="Zone"/>, whose <c>Stretch</c> is its id, none
    /// on a planet; the stream always by the hull beating <paramref name="Thins"/> times slower meanwhile, so its own pieces are not lost
    /// among the junk; a hero under the open sky walking and running at <paramref name="Pace"/> thousandths of its speed while it runs;
    /// one bolt of lightning by <paramref name="Bolt"/> each time it comes, none without; <paramref name="Grit"/> units of a hero's own air
    /// a breath under the open sky while it runs, past what it breathes: what blown grit grinds out of a suit.
    /// </summary>
    public sealed record Stretch(string Id, IReadOnlyList<string> Unlock, Span First, Span Gap, Span Duration, int Chill, int Warning,
        DebrisZone? Zone = null, int Thins = 1, int Pace = 1000, BoltRule? Bolt = null, int Grit = 0);

    /// <summary>Ticks in a minute.</summary>
    private const int Minute = 60 * 20;

    /// <summary>The ice belt: opened by a warm helmet; ice blocks drift by in place of most of the junk, and outside is 40° colder.</summary>
    public static readonly Stretch IceBelt = new("iceBelt", [Helmet.Quilted, Helmet.Heavy],
        First: new(5 * Minute, 10 * Minute), Gap: new(20 * Minute, 40 * Minute), Duration: new(4 * Minute, 6 * Minute), Chill: 40,
        Warning: 3 * Minute,
        Zone: new DebrisZone("iceBelt", new ByHull(Debris.Margin, Debris.Lead), Every: 120, Jitter: 60, Max: 16, Debris.DriftMin, Debris.DriftMax,
            [new(0, [("iceBlock", 1)])])
        { Stretch = "iceBelt" }, Thins: 4);

    /// <summary>The contraband drift: opened by the railgun; the mafia's crates drift by, each a hold of contraband the trackers notice.</summary>
    public static readonly Stretch ContrabandDrift = new("contrabandDrift", [Shots.Railgun.Item],
        First: new(5 * Minute, 10 * Minute), Gap: new(25 * Minute, 45 * Minute), Duration: new(5 * Minute, 8 * Minute), Chill: 0,
        Warning: 3 * Minute,
        Zone: new DebrisZone("contrabandDrift", new ByHull(Debris.Margin, Debris.Lead), Every: 400, Jitter: 200, Max: 6, Debris.DriftMin, Debris.DriftMax,
            [new(0, [("contrabandCrate", 1)])])
        { Stretch = "contrabandDrift" });

    /// <summary>
    /// The first planet's dust storm (docs/plans/surface-storms.md): open from the start, one of its two storms, about three storms in
    /// a 20-minute day between them (gap and duration average 8000 ticks), each a minute to a minute and a half, warned 40 s ahead,
    /// the open sky 10° colder and a hero out in it at half its pace, leaning into the wind, its grit grinding 2 units of air a second
    /// out of the suit (the user's, 2026-11-17: dig in). The hour is not read. Its bolt's zone reaches 10 columns either side.
    /// </summary>
    public static readonly Stretch DustStorm = new("dustStorm", [],
        First: new(6000, 9600), Gap: new(5700, 7300), Duration: new(1200, 1800), Chill: 10, Warning: 800, Pace: 500, Bolt: Bolt(spread: 10), Grit: 2);

    /// <summary>
    /// The first planet's rain storm: the dust storm's numbers, another weather to see (rain is a look, not water): only 5° colder, no
    /// grit; its bolt's zone 20 columns either side, the ground wet. Its danger is the lightning (the user's, 2026-11-17): a bolt every
    /// 12 to 18 s, glow included, the first 9 to 15 s in.
    /// </summary>
    public static readonly Stretch RainStorm = DustStorm with { Id = "rainStorm", Chill = 5, Grit = 0, Bolt = Bolt(spread: 20, every: new(180, 300)) };

    /// <summary>
    /// A storm's bolt (docs/plans/surface-storms.md): 200 to 1000 ticks in, or with <paramref name="every"/> its first that far in and
    /// each next that far after the last, glowing 3 s (a run out of a 20-column zone at half pace,
    /// and a reaction), aimed within 24 columns, shocking 40, nearly all of a basic helmet's 45.
    /// </summary>
    /// <remarks>
    /// Power 60, reach 2: a burst's first cell costs its strength, each step past it <c>Damage.Step</c> and the next cell's, so struck soil
    /// (40) breaks and the soil round it (42 more) only cracks, by up to 18; a built soil roof breaks, a scrap one (400) only cracks.
    /// </remarks>
    private static BoltRule Bolt(int spread, Span? every = null) =>
        new(At: every ?? new(200, 1000), Glow: 60, Columns: 24, Power: 60, Reach: 2, Spread: spread, Shock: 100, Again: every);

    /// <summary>The ship's stretches, in the order they open on a tick and are kept in.</summary>
    public static readonly IReadOnlyList<Stretch> Ship = [IceBelt, ContrabandDrift];

    /// <summary>The first planet's stretches: its weathers, one at a time.</summary>
    public static readonly IReadOnlyList<Stretch> FirstPlanet = [DustStorm, RainStorm];

    /// <summary>The item a contraband crate holds: one, worth what a few crates take to fill a wanted meter.</summary>
    public const string Contraband = "contraband";
}
