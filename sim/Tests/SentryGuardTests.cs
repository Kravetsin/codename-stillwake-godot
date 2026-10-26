using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Xunit;
using Xunit.Abstractions;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Sentries out on the derelicts (2026-10-26-creatures-are-composed, C-4): each new engine block or cargo hold whose look has a perch
/// draws once whether one comes with it, standing on a perch, and crawls along it while it watches.
/// </summary>
public class SentryGuardTests(ITestOutputHelper output)
{
    private const int Width = 160, Unit = CreaturesModule.Unit, Pixel = Unit / Spec.Wreckage.PixelsPerCell;

    /// <summary>Open space 160 cells by 80, a cell a character: no hull, so no stream of debris.</summary>
    private static readonly string Space = string.Join('\n', Enumerable.Repeat(new string('.', Width), 80));

    private static Scenario World() => new Scenario(Space, cells: true).Do(new Enter(40 * Width + 4)).Run();

    private static Piece Out(Scenario w, string item, int x, int y, int? id = null)
    {
        var debris = (DebrisState)w.M.Debris.Slice;
        if (id is int at) debris.NextId = at;
        var piece = new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = item, X = x, Y = y, Start = x, SeenAt = debris.Clock };
        debris.Pieces.Add(piece);
        return piece;
    }

    /// <summary>Where on <paramref name="piece"/> a sentry may stand: each perch's middle points between which its middle goes, and its feet's row, in thousandths of a cell.</summary>
    private static List<(int Lo, int Hi, int Y)> Stands(Piece piece)
    {
        var (w, h) = Spec.Wreckage.SizeOf(piece.Item)!.Value;
        return [.. Spec.Wreckage.PerchesOf(piece.Item, piece.Id).Select(p =>
            (piece.X - w * Unit / 2 + p.Left * Pixel + 3 * Unit / 2, piece.X - w * Unit / 2 + p.Right * Pixel - 3 * Unit / 2, piece.Y - h * Unit / 2 + p.Top * Pixel))];
    }

    [Fact]
    public void EachDerelictWithAPerchDrawsOnceAndAboutEveryOtherComesWithASentryStandingOnIt()
    {
        var w = World();
        int perched = 0, guarded = 0;
        string[] kinds = ["engineBlock", "cargoHold"];
        for (int round = 0; round < 40; round++)
        {
            var pieces = Enumerable.Range(0, 4).Select(i => Out(w, kinds[(round + i) % 2], 20_000 + i * 36_000, 40_000)).ToList();
            w.Run();
            var all = w.M.Creatures.Creatures;
            foreach (var c in all)
            {
                Assert.Equal(Tune.Sentry.Id, c.Species);
                Assert.True(CreaturesModule.Clamped(c));
                var piece = pieces.Single(p => p.Id == c.Recall(ClingerBody.Piece));
                // Set in the middle of one of its piece's perches, its feet on it; it has crawled a tick since.
                Assert.Contains(Stands(piece), s => Math.Abs(c.X - (s.Lo + s.Hi) / 2) <= ((Tune.Clinger)Tune.Sentry.Body).Crawl && c.Y == s.Y);
            }
            Assert.Equal(all.Count, all.Select(c => c.Recall(ClingerBody.Piece)).Distinct().Count());
            // Drawn once: ticks on, none more comes.
            w.Run(5);
            Assert.Equal(all.Count, w.M.Creatures.Creatures.Count);
            perched += pieces.Count(p => Stands(p).Count > 0);
            guarded += all.Count;
            ((CreaturesState)w.M.Creatures.Slice).Creatures.Clear();
            ((DebrisState)w.M.Debris.Slice).Pieces.Clear();
        }
        output.WriteLine($"{guarded} sentries on {perched} derelicts with a perch");
        // Half of them, three deviations either side.
        Assert.InRange(guarded * 1000 / perched, 380, 620);
    }

    [Fact]
    public void AnyOtherPieceOrALookWithNoPerchComesAloneAndIsLookedAtOnce()
    {
        var w = World();
        foreach (var item in new[] { "hullSection", "scrapChunk", "asteroidLarge", "cargoCrate", "podCluster", "podCluster", "podCluster", "podCluster" })
            Out(w, item, 80_000, 40_000);
        // The fourth engine block's picture has no flat stretch to stand on.
        var last = Out(w, "engineBlock", 120_000, 40_000, id: 4 * 50 + 3);
        Assert.Empty(Spec.Wreckage.PerchesOf(last.Item, last.Id));
        w.Run(3);
        Assert.Empty(w.M.Creatures.Creatures);
        Assert.Equal(last.Id, ((CreaturesState)w.M.Creatures.Slice).Watermark);
    }

    [Fact]
    public void NoneComesPastTheSurfacesCap()
    {
        var w = World();
        for (int i = 0; i < 8 * Tune.Max; i++) Out(w, "cargoHold", 20_000 + i % 12 * 10_000, 20_000 + i / 12 * 1_500);
        w.Run();
        Assert.Equal(Tune.Max, w.M.Creatures.Creatures.Count);
    }

    [Fact]
    public void LetOutOverAPerchItStandsOnItAndElsewhereOnTheWrecksBox()
    {
        var w = World();
        // The first cargo hold's picture: one perch from pixel 1 to 85 across its 96, three pixels down.
        var hold = Out(w, "cargoHold", 80_000, 40_000, id: 4 * 20);
        var stand = Assert.Single(Stands(hold));
        SentryTests.Guarded(w);
        w.Do(new Release(Tune.Sentry.Id, 37 * Width + 76)).Run();
        var c = w.M.Creatures.Creatures.Single();
        Assert.Equal((76_500, stand.Y), (c.X, c.Y));
        // Over its right end, past the perch, on the box's top.
        ((CreaturesState)w.M.Creatures.Slice).Creatures.Clear();
        w.Do(new Release(Tune.Sentry.Id, 37 * Width + 85)).Run();
        c = w.M.Creatures.Creatures.Single();
        Assert.Equal((84_500, 40_000 - 3 * Unit), (c.X, c.Y));
    }

    [Fact]
    public void WatchingItCrawlsAlongItsPerchEndToEndAndAlertedItStops()
    {
        var w = World();
        var hold = Out(w, "cargoHold", 80_000, 40_000, id: 4 * 20);
        var stand = Assert.Single(Stands(hold));
        SentryTests.Guarded(w);
        w.Do(new Release(Tune.Sentry.Id, 37 * Width + 80)).Run();
        var c = w.M.Creatures.Creatures.Single();
        int lo = int.MaxValue, hi = int.MinValue, turns = 0, last = c.X, way = 0;
        for (int t = 0; t < 600; t++)
        {
            w.Run();
            Assert.Equal(stand.Y, c.Y);
            Assert.InRange(c.X, stand.Lo, stand.Hi);
            int now = Math.Sign(c.X - last);
            if (now != 0 && way != 0 && now != way) turns++;
            if (now != 0) way = now;
            Assert.InRange(Math.Abs(c.X - last), 0, ((Tune.Clinger)Tune.Sentry.Body).Crawl);
            (lo, hi, last) = (Math.Min(lo, c.X), Math.Max(hi, c.X), c.X);
        }
        Assert.Equal((stand.Lo, stand.Hi), (lo, hi));
        Assert.True(turns >= 2, $"{turns} turns");
        // A hero near: alerted, it holds still.
        w.Do(new Enter(37 * Width + c.X / Unit - 4)).Run();
        Assert.True(CreaturesModule.Alerted(c));
        int held = c.X;
        w.Run(Tune.Sentry.Brain.OfType<Tune.Guard>().Single().AlertTicks - 2);
        Assert.Equal(held, c.X);
    }

    /// <summary>
    /// A hero hovering far out (0052's far field, past 512 cells from the hull) for an hour: about every other derelict with a perch
    /// that drifted in came with a sentry. The numbers are printed.
    /// </summary>
    [Fact]
    public void AnHourHoveringFarOutMeetsASentryOnAboutEveryOtherDerelictWithAPerch()
    {
        var map = DebrisZonesTests.Open(4096, 800, 20, 780, 12, 6);
        var w = new Scenario(map).GiveItems(0, ("canister", 60));
        var derelicts = new HashSet<int>();
        var perched = new HashSet<int>();
        var guarded = new HashSet<int>();
        string[] kinds = [.. Tune.Derelicts.Kinds.Select(k => k.Item)];
        for (int t = 0; t < 60 * 60 * Sim.World.TicksPerSecond; t++)
        {
            if (t % Spec.Crew.ReportEvery == 0) w.Do(new Enter(400 * map.Width + 2000));
            if (t % 1200 == 1199) w.Do(new Refill());
            w.Run();
            if (t % 20 != 0) continue;
            foreach (var p in w.M.Debris.Pieces)
                if (kinds.Contains(p.Item) && derelicts.Add(p.Id) && Spec.Wreckage.PerchesOf(p.Item, p.Id).Count > 0) perched.Add(p.Id);
            foreach (var c in w.M.Creatures.Creatures)
                guarded.Add(c.Recall(ClingerBody.Piece));
        }
        output.WriteLine($"an hour hovering far out: {derelicts.Count} engine blocks and cargo holds drifted in, {perched.Count} with a perch, {guarded.Count} with a sentry");
        Assert.InRange(perched.Count, 100, 800);
        Assert.InRange(guarded.Count * 1000 / perched.Count, 300, 700);
    }
}
