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
    /// A stretch: opened once any of <paramref name="Unlock"/> has been obtained, from the start with none; its first coming
    /// <paramref name="First"/> after it is chosen, every later one <paramref name="Gap"/> after the last ended, the last
    /// <paramref name="Warning"/> ticks of either its warning; lasting <paramref name="Duration"/>; <paramref name="Chill"/> degrees
    /// colder under the open sky while it runs; its pieces drifting by in <paramref name="Zone"/>, whose <c>Stretch</c> is its id, none
    /// on a planet; the stream always by the hull beating <paramref name="Thins"/> times slower meanwhile, so its own pieces are not lost
    /// among the junk.
    /// </summary>
    public sealed record Stretch(string Id, IReadOnlyList<string> Unlock, Span First, Span Gap, Span Duration, int Chill, int Warning,
        DebrisZone? Zone = null, int Thins = 1);

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
    /// The first planet's storm (docs/plans/surface-storms.md): open from the start, about three in a 20-minute day (gap and duration
    /// average 8000 ticks), each a minute to a minute and a half, warned 40 s ahead, the open sky 20° colder. The hour is not read.
    /// </summary>
    public static readonly Stretch Storm = new("storm", [],
        First: new(6000, 9600), Gap: new(5700, 7300), Duration: new(1200, 1800), Chill: 20, Warning: 800);

    /// <summary>The ship's stretches, in the order they open on a tick and are kept in.</summary>
    public static readonly IReadOnlyList<Stretch> Ship = [IceBelt, ContrabandDrift];

    /// <summary>The first planet's stretches: its weather.</summary>
    public static readonly IReadOnlyList<Stretch> FirstPlanet = [Storm];

    /// <summary>The item a contraband crate holds: one, worth what a few crates take to fill a wanted meter.</summary>
    public const string Contraband = "contraband";
}
