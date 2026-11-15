namespace Stillwake.Content;

/// <summary>
/// Stretches of space (docs/plans/stretches.md): spells the ship drifts through, each opened by what the crew has obtained, then
/// coming again and again. One at a time is chosen, seen coming for <see cref="Warning"/> ticks, and runs: colder outside by its
/// <see cref="Stretch.Chill"/>, its own debris zone thickening the stream. Only the ship has them (<c>Surfaces.Kind.Stretches</c>).
/// All numbers are placeholders for a play pass.
/// </summary>
public static class Stretches
{
    /// <summary>A span of ticks drawn by the world's Rng, both ends included.</summary>
    public sealed record Span(int Least, int Most);

    /// <summary>
    /// A stretch: opened once any of <paramref name="Unlock"/> has been obtained; its first coming <paramref name="First"/> after it is
    /// chosen, every later one <paramref name="Gap"/> after the last ended; lasting <paramref name="Duration"/>; <paramref name="Chill"/>
    /// degrees colder under the open sky while it runs; its pieces drifting by in <paramref name="Zone"/>, whose <c>Stretch</c> is its id;
    /// the stream always by the hull beating <paramref name="Thins"/> times slower meanwhile, so its own pieces are not lost among the junk.
    /// </summary>
    public sealed record Stretch(string Id, IReadOnlyList<string> Unlock, Span First, Span Gap, Span Duration, int Chill, DebrisZone Zone, int Thins = 1);

    /// <summary>Ticks in a minute.</summary>
    private const int Minute = 60 * 20;

    /// <summary>How long every stretch is seen coming, in ticks: three minutes.</summary>
    public const int Warning = 3 * Minute;

    /// <summary>The ice belt: opened by a warm helmet; ice blocks drift by in place of most of the junk, and outside is 40° colder.</summary>
    public static readonly Stretch IceBelt = new("iceBelt", [Helmet.Quilted, Helmet.Heavy],
        First: new(5 * Minute, 10 * Minute), Gap: new(20 * Minute, 40 * Minute), Duration: new(4 * Minute, 6 * Minute), Chill: 40,
        Zone: new DebrisZone("iceBelt", new ByHull(Debris.Margin, Debris.Lead), Every: 120, Jitter: 60, Max: 16, Debris.DriftMin, Debris.DriftMax,
            [new(0, [("iceBlock", 1)])])
        { Stretch = "iceBelt" }, Thins: 4);

    /// <summary>The contraband drift: opened by the railgun; the mafia's crates drift by, each a hold of contraband the trackers notice.</summary>
    public static readonly Stretch ContrabandDrift = new("contrabandDrift", [Shots.Railgun.Item],
        First: new(5 * Minute, 10 * Minute), Gap: new(25 * Minute, 45 * Minute), Duration: new(5 * Minute, 8 * Minute), Chill: 0,
        Zone: new DebrisZone("contrabandDrift", new ByHull(Debris.Margin, Debris.Lead), Every: 400, Jitter: 200, Max: 6, Debris.DriftMin, Debris.DriftMax,
            [new(0, [("contrabandCrate", 1)])])
        { Stretch = "contrabandDrift" });

    /// <summary>Every stretch, in the order they open on a tick and are kept in.</summary>
    public static readonly IReadOnlyList<Stretch> All = [IceBelt, ContrabandDrift];

    /// <summary>The item a contraband crate holds: one, worth what a few crates take to fill a wanted meter.</summary>
    public const string Contraband = "contraband";

    /// <summary>The stretch of that id, or null for none.</summary>
    public static Stretch? Get(string id)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Id == id) return All[i];
        return null;
    }
}
