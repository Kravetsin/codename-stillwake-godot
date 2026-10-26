using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Xunit;
using Xunit.Abstractions;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>Sentries out on the derelicts (2026-10-26-creatures-are-composed, C-4): each new derelict draws once whether one comes with it.</summary>
public class SentryGuardTests(ITestOutputHelper output)
{
    private const int Width = 160;

    /// <summary>Open space 160 cells by 80, a cell a character: no hull, so no stream of debris.</summary>
    private static readonly string Space = string.Join('\n', Enumerable.Repeat(new string('.', Width), 80));

    private static Piece Out(Scenario w, string item, int x, int y)
    {
        var debris = (DebrisState)w.M.Debris.Slice;
        var piece = new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = item, X = x, Y = y, Start = x, SeenAt = debris.Clock };
        debris.Pieces.Add(piece);
        return piece;
    }

    [Fact]
    public void EachDerelictDrawsOnceAndAboutEveryOtherComesWithASentryClampedOnItsTop()
    {
        var w = new Scenario(Space, cells: true).Do(new Enter(40 * Width + 4)).Run();
        int derelicts = 0, guarded = 0;
        string[] kinds = ["engineBlock", "cargoHold", "podCluster"];
        for (int round = 0; round < 40; round++)
        {
            var pieces = Enumerable.Range(0, 3).Select(i => Out(w, kinds[(round + i) % 3], 30_000 + i * 40_000, 40_000)).ToList();
            w.Run();
            var all = w.M.Creatures.Creatures;
            Assert.All(all, c => Assert.Equal(Tune.Sentry.Id, c.Species));
            Assert.All(all, c => Assert.True(CreaturesModule.Clamped(c)));
            // Each on its own piece's top, over its middle.
            Assert.Equal(all.Count, all.Select(c => pieces.Single(p => Math.Abs(p.X - c.X) < 1000).Id).Distinct().Count());
            // Drawn once: a tick on, none more comes.
            w.Run(5);
            Assert.Equal(all.Count, w.M.Creatures.Creatures.Count);
            derelicts += 3;
            guarded += all.Count;
            ((CreaturesState)w.M.Creatures.Slice).Creatures.Clear();
            ((DebrisState)w.M.Debris.Slice).Pieces.Clear();
        }
        output.WriteLine($"{guarded} sentries on {derelicts} derelicts");
        // Half of 120, three deviations either side.
        Assert.InRange(guarded, 44, 76);
    }

    [Fact]
    public void AnyOtherPieceComesAloneAndIsLookedAtOnce()
    {
        var w = new Scenario(Space, cells: true).Do(new Enter(40 * Width + 4)).Run();
        foreach (var item in new[] { "hullSection", "scrapChunk", "asteroidLarge", "cargoCrate" }) Out(w, item, 80_000, 40_000);
        var last = Out(w, "machineBlock", 120_000, 40_000);
        w.Run(3);
        Assert.Empty(w.M.Creatures.Creatures);
        Assert.Equal(last.Id, ((CreaturesState)w.M.Creatures.Slice).Watermark);
    }

    [Fact]
    public void NoneComesPastTheSurfacesCap()
    {
        var w = new Scenario(Space, cells: true).Do(new Enter(40 * Width + 4)).Run();
        for (int i = 0; i < 4 * Tune.Max; i++) Out(w, "engineBlock", 20_000 + i % 12 * 10_000, 20_000 + i / 12 * 3_000);
        w.Run();
        Assert.Equal(Tune.Max, w.M.Creatures.Creatures.Count);
    }

    /// <summary>
    /// A hero hovering far out (0052's far field, past 512 cells from the hull) for an hour: about every other derelict that drifted
    /// in came with a sentry, and a sortie meets a handful. The numbers are printed.
    /// </summary>
    [Fact]
    public void AnHourHoveringFarOutMeetsASentryOnAboutEveryOtherDerelict()
    {
        var map = DebrisZonesTests.Open(4096, 800, 20, 780, 12, 6);
        var w = new Scenario(map).GiveItems(0, ("canister", 60));
        var derelicts = new HashSet<int>();
        var guarded = new HashSet<int>();
        string[] kinds = [.. Tune.Derelicts.Kinds.Select(k => k.Item)];
        for (int t = 0; t < 60 * 60 * World.TicksPerSecond; t++)
        {
            if (t % Spec.Crew.ReportEvery == 0) w.Do(new Enter(400 * map.Width + 2000));
            if (t % 1200 == 1199) w.Do(new Refill());
            w.Run();
            if (t % 20 != 0) continue;
            foreach (var p in w.M.Debris.Pieces)
                if (kinds.Contains(p.Item)) derelicts.Add(p.Id);
            foreach (var c in w.M.Creatures.Creatures)
                guarded.Add(c.Recall(ClingerBody.Piece));
        }
        output.WriteLine($"an hour hovering far out: {derelicts.Count} derelicts drifted in, {guarded.Count} with a sentry");
        Assert.InRange(derelicts.Count, 100, 800);
        Assert.InRange(guarded.Count * 1000 / derelicts.Count, 300, 700);
    }
}
