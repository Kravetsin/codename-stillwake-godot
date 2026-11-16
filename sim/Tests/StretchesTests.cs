using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Sky;
using Xunit;
using ByHull = Stillwake.Content.ByHull;
using DebrisZone = Stillwake.Content.DebrisZone;
using Spec = Stillwake.Content.Stretches;

namespace Stillwake.Sim.Tests;

/// <summary>Stretches of space (docs/decisions/2026-11-15-stretches-of-space.md): opened by what was obtained, chosen, seen coming, run, ended; their cold and debris.</summary>
public class StretchesTests
{
    /// <summary>A pod in a vacuum room, never aired, its hull the debris stream's; open space right of it.</summary>
    private static readonly Layout PodRoom = Layout.Parse(["###########    ", "#.........#    ", "#..OOO...S#    ", "#..OOO....#    ", "#..OOO....#    ", "###########    "]);

    private static Scenario Ship() => new(PodRoom, doubled: true);

    private static readonly Spec.Stretch Belt = Spec.IceBelt, Drift = Spec.ContrabandDrift;
    private static readonly DebrisZone BeltZone = Belt.Zone!, DriftZone = Drift.Zone!;

    /// <summary>A cell of open space right of the hull: vacuum, no back wall.</summary>
    private const int Beyond = 6 * 30 + 25;

    private static SkyState S(Scenario w) => (SkyState)w.M.Sky.Slice;
    private static SkyNow Now(Scenario w) => w.M.Sky.Now;

    /// <summary><paramref name="item"/> obtained by the crew, as a catch counts it.</summary>
    private static Scenario Obtain(Scenario w, string item)
    {
        w.M.Items.Caught([(item, 1)]);
        return w;
    }

    /// <summary>Runs until <paramref name="until"/> holds, at most <paramref name="most"/> ticks; the ticks run.</summary>
    private static int RunUntil(Scenario w, Func<bool> until, int most = 200_000)
    {
        for (int t = 1; t <= most; t++)
        {
            w.Run();
            if (until()) return t;
        }
        throw new Xunit.Sdk.XunitException($"not within {most} ticks");
    }

    private static void InSpan(Spec.Span span, int ticks) => Assert.InRange(ticks, span.Least, span.Most);

    [Fact]
    public void AContrabandCrateAlwaysHoldsItsContrabandAndNowAndThenEveryGoodTheBlackMarketSells()
    {
        var loot = Content.Loot.Of("contrabandCrate")!;
        Assert.Contains(loot, e => e.Item == Spec.Contraband && e.OneIn == null && e.Min >= 1);
        foreach (var good in Content.Research.GearSold) Assert.Contains(loot, e => e.Item == good && e.OneIn > 1);
    }

    [Fact]
    public void NothingObtainedOpensNothingAndDrawsNothing()
    {
        var w = Ship().Run(2000);
        Assert.Equal(default, Now(w));
        Assert.Contains("\"sky\":{\"time\":0}", w.World.Save());
    }

    [Theory]
    [InlineData("quiltedHelmet")]
    [InlineData("heavyHelmet")]
    public void EitherWarmHelmetAloneOpensTheBeltWhichComesAtItsFirstWarnsRunsAndEnds(string helmet)
    {
        var w = Obtain(Ship().Run(), helmet).Run();
        Assert.Equal([Belt.Id], S(w).Opened);
        Assert.Equal(Belt.Id, S(w).Next);
        int first = S(w).NextIn;
        InSpan(Belt.First, first);
        // Unseen till its warning.
        w.Run(first - Belt.Warning - 1);
        Assert.Equal((null, null, 0), (Now(w).Stretch, Now(w).Coming, Now(w).ComingIn));
        w.Run();
        Assert.Equal((null, Belt.Id, Belt.Warning), (Now(w).Stretch, Now(w).Coming, Now(w).ComingIn));
        w.Run(Belt.Warning - 1);
        Assert.Equal((null, Belt.Id, 1), (Now(w).Stretch, Now(w).Coming, Now(w).ComingIn));
        w.Run();
        Assert.Equal((Belt.Id, null, 0, Belt.Chill, 0), (Now(w).Stretch, Now(w).Coming, Now(w).ComingIn, Now(w).Chill, Now(w).Ran));
        Assert.Equal([Belt.Id], S(w).Came);
        int left = S(w).Left;
        InSpan(Belt.Duration, left);
        w.Run(left - 1);
        Assert.Equal((Belt.Id, left - 1), (Now(w).Stretch, Now(w).Ran));
        w.Run();
        Assert.Equal(default, Now(w));
        // Its gap measured from the end, drawn on the tick it ended; the drift never opened.
        Assert.Equal(Belt.Id, S(w).Next);
        InSpan(Belt.Gap, S(w).NextIn);
        Assert.Equal([Belt.Id], S(w).Opened);
    }

    [Fact]
    public void TheRailgunObtainedDuringTheBeltBringsTheDriftNextAndNeverBeforeTheBeltEnds()
    {
        var w = Obtain(Ship().Run(), Content.Helmet.Quilted);
        RunUntil(w, () => Now(w).Stretch == Belt.Id);
        Obtain(w, Content.Shots.Railgun.Item).Run();
        Assert.Equal([Belt.Id, Drift.Id], S(w).Opened);
        // A choice once made stands: nothing is chosen while the belt runs.
        Assert.Null(S(w).Next);
        RunUntil(w, () => Now(w).Stretch == null);
        Assert.Equal(Drift.Id, S(w).Next);
        InSpan(Drift.First, S(w).NextIn);
        RunUntil(w, () => Now(w).Stretch != null);
        Assert.Equal(Drift.Id, Now(w).Stretch);
        Assert.Equal(0, Now(w).Chill);
        Assert.Equal([Belt.Id, Drift.Id], S(w).Came);
    }

    [Fact]
    public void AnOlderSaveWithTheHelmetLongObtainedGetsItsFirstStretchAtItsFirstNeverOnItsFirstTick()
    {
        var w = Obtain(Ship().Run(), Content.Helmet.Heavy).Run(50);
        Assert.Contains("\"opened\"", w.World.Save());
        // As a save from before stretches: the sky its hour alone.
        string older = Regex.Replace(w.World.Save(), "\"sky\":\\{[^}]*\\}", "\"sky\":{\"time\":0}");
        w.Reload(older);
        Assert.Empty(S(w).Opened);
        w.Run();
        Assert.Equal(Belt.Id, S(w).Next);
        InSpan(Belt.First, S(w).NextIn);
        Assert.Equal((null, null), (Now(w).Stretch, Now(w).Coming));
    }

    [Fact]
    public void AHelmetSpawnedInCreativeModeOpensNothing()
    {
        var w = Ship();
        w.Do(new Enter(Beyond)).Do(new SetCreative(true)).Run();
        w.Do(new Spawn(Content.Helmet.Quilted, 1)).Do(new Spawn(Content.Shots.Railgun.Item, 1)).Run(20);
        Assert.Empty(w.Refusals);
        Assert.Contains(w.M.Items.Inventory(0), s => s?.Item == Content.Helmet.Quilted);
        Assert.Empty(S(w).Opened);
        Assert.Null(S(w).Next);
    }

    [Fact]
    public void StretchNowOpensItAndBringsItAfterItsWarningInPlaceOfTheOneChosen()
    {
        var w = Obtain(Ship().Run(), Content.Helmet.Quilted).Run();
        Assert.Equal(Belt.Id, S(w).Next);
        w.Do(new StretchNow(Drift.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal([Belt.Id, Drift.Id], S(w).Opened);
        // Counted down on the tick it came, as any wait is.
        Assert.Equal((Drift.Id, Drift.Warning - 1), (Now(w).Coming, Now(w).ComingIn));
        w.Run(Drift.Warning - 1);
        Assert.Equal(Drift.Id, Now(w).Stretch);
        // The belt, chosen before and never come, comes next.
        RunUntil(w, () => Now(w).Stretch == null);
        Assert.Equal(Belt.Id, S(w).Next);
        InSpan(Belt.First, S(w).NextIn);
    }

    [Fact]
    public void StretchNowIsRefusedForOneUnknownOrOfAnotherSurfacesKindAndWhileOneRuns()
    {
        var w = Ship();
        w.Do(new StretchNow("noSuchStretch")).Run();
        Assert.Equal("sky.noStretch", Assert.Single(w.Refusals).Reason);
        Assert.Empty(S(w).Opened);

        w.Refusals.Clear();
        w.Do(new Enter(Beyond)).Do(new OpenSurface(Content.Surfaces.TestStrip.Id)).Run().Do(new Transfer(1)).Run();
        Assert.Empty(w.Refusals);
        w.Do(new StretchNow(Belt.Id)).Run();
        Assert.Equal("sky.noStretch", Assert.Single(w.Refusals).Reason);
        // A planet has its own: the belt is not among them.
        Assert.Equal([Content.Stretches.Storm.Id], ((SkyState)w.On(1).Sky.Slice).Opened);

        var ship = Ship();
        ship.Do(new StretchNow(Belt.Id)).Run(Belt.Warning);
        Assert.Equal(Belt.Id, Now(ship).Stretch);
        int left = S(ship).Left;
        ship.Do(new StretchNow(Drift.Id)).Run();
        Assert.Equal("sky.stretchRunning", Assert.Single(ship.Refusals).Reason);
        Assert.Equal((Belt.Id, left - 1), (Now(ship).Stretch, S(ship).Left));
        Assert.Equal([Belt.Id], S(ship).Opened);
        Assert.Null(S(ship).Next);
    }

    [Fact]
    public void ARunningStretchSavesLoadsAndPlaysOnTheSame()
    {
        var w = Ship();
        Obtain(w, Content.Helmet.Quilted).Do(new StretchNow(Belt.Id)).Run(Belt.Warning + 10);
        Assert.Equal(Belt.Id, Now(w).Stretch);
        var loaded = Ship().Reload(w.World.Save());
        Assert.Equal(w.World.Hash(), loaded.World.Hash());
        // How long it has run, too: its far picture is the same after a load.
        Assert.True(Now(w).Ran > 0);
        Assert.Equal(Now(w), Now(loaded));
        w.Run(Belt.Duration.Most + 100);
        loaded.Run(Belt.Duration.Most + 100);
        Assert.Equal(w.World.Hash(), loaded.World.Hash());
        Assert.Equal(Now(w), Now(loaded));
    }

    // Crew and Debris.

    [Fact]
    public void TheBeltsColdReachesAHeroOutsideAndNoAiredCell()
    {
        var w = Ship();
        // The hero's temperature as Crew reads it, the hero reported at Beyond again: afloat, it drifts.
        int Reading()
        {
            w.Do(new Enter(Beyond)).Run();
            Assert.Equal(Beyond, w.M.Crew.HeroOf(0)!.Cell);
            return w.M.Crew.TemperatureAt(Beyond);
        }
        Assert.Null(w.M.Grid.BackAt(Beyond));
        Assert.False(w.M.Atmosphere.AiredAt(Beyond));
        int before = Reading();
        w.Do(new StretchNow(Belt.Id)).Run(Belt.Warning - 1);
        Assert.Equal(before - Belt.Chill, Reading());
        Assert.Equal(Belt.Id, Now(w).Stretch);
        // A room aired by a vaporizer's vent (AirTesting.Vaporized) stays a room's warmth.
        var room = new Scenario(AirTesting.Vaporized(["#########", "#V......#", "#TT.....#", "#TT.....#", "#########"], ["", " ---", " o -", "   -"]), doubled: true);
        int aired = room.At(4, 1);
        room.Run(60).Do(new StretchNow(Belt.Id)).Run(Belt.Warning);
        Assert.Equal(Belt.Id, Now(room).Stretch);
        Assert.True(room.M.Atmosphere.AiredAt(aired));
        Assert.Equal(Content.Temperature.Room, room.M.Crew.TemperatureAt(aired));
        // Off on the tick it ends.
        w.Run(S(w).Left - 1);
        Assert.Equal(before, Reading());
        Assert.Null(Now(w).Stretch);
    }

    /// <summary>
    /// Every piece spawned in <paramref name="ticks"/> ticks, by the tick it came on and the stretch running then, <paramref name="first"/>
    /// sent on the first.
    /// </summary>
    private static List<(int Tick, string? Running, Piece Piece)> Spawns(Scenario w, int ticks, Command? first = null)
    {
        var seen = new List<(int, string?, Piece)>();
        var known = w.M.Debris.Pieces.Select(p => p.Id).ToHashSet();
        if (first != null) w.Do(first);
        for (int t = 1; t <= ticks; t++)
        {
            w.Run();
            foreach (var p in w.M.Debris.Pieces)
                if (known.Add(p.Id)) seen.Add((t, Now(w).Stretch, p));
        }
        return seen;
    }

    [Fact]
    public void IceBlocksDriftByOnlyWhileTheBeltRunsTheFirstABeatAfterItStarts()
    {
        var w = Ship();
        // The belt brought by hand: it starts on tick Warning.
        var spawns = Spawns(w, 2 * Belt.Warning + Belt.Duration.Most, new StretchNow(Belt.Id));
        Assert.Null(Now(w).Stretch);
        var ice = spawns.Where(s => s.Piece.Zone == BeltZone.Id).ToList();
        Assert.True(ice.Count > 1);
        Assert.All(ice, s => Assert.Equal(("iceBlock", Belt.Id), (s.Piece.Item, s.Running)));
        Assert.Equal(Belt.Warning + BeltZone.Every, ice[0].Tick);
        Assert.Contains(spawns, s => s.Piece.Zone == Content.Debris.Ship.Id && s.Running == null);
        Assert.Contains(spawns, s => s.Piece.Zone == Content.Debris.Ship.Id && s.Running == Belt.Id);
        Assert.DoesNotContain(spawns, s => s.Piece.Zone == DriftZone.Id);
    }

    [Fact]
    public void TheBeltThinsTheStreamByTheHullWhileItRunsAndTheStreamFillsAgainAfter()
    {
        var w = Ship();
        Assert.True(Belt.Thins > 1);
        w.Do(new StretchNow(Belt.Id)).Run(Belt.Warning);
        Assert.Equal(Belt.Id, Now(w).Stretch);
        // Over a span the belt surely runs through, and as long again after it.
        int span = Belt.Duration.Least - 1;
        int during = Spawns(w, span).Count(s => s.Piece.Zone == Content.Debris.Ship.Id);
        RunUntil(w, () => Now(w).Stretch == null);
        int after = Spawns(w, span).Count(s => s.Piece.Zone == Content.Debris.Ship.Id);
        int beat = Content.Debris.Every + Content.Debris.Jitter;
        Assert.InRange(during, 1, span / (Belt.Thins * Content.Debris.Every) + 1);
        Assert.True(after >= span / beat - 1, $"{after} after, {during} during");
    }

    [Fact]
    public void AStretchsZoneBeatsAgainABeatAfterEachStart()
    {
        var w = Ship();
        w.Do(new StretchNow(Belt.Id)).Run(Belt.Warning);
        RunUntil(w, () => Now(w).Stretch == null);
        Assert.DoesNotContain(((DebrisState)w.M.Debris.Slice).Beats, b => b.Zone == BeltZone.Id);
        var spawns = Spawns(w, Belt.Warning + BeltZone.Every, new StretchNow(Belt.Id));
        Assert.Equal(Belt.Warning + BeltZone.Every, spawns.First(s => s.Piece.Zone == BeltZone.Id).Tick);
    }

    [Fact]
    public void TheStreamsBoxLeavesOutAStretchsZoneWhileItsStretchIsNotRunning()
    {
        var hull = (Left: 100, Right: 110, Top: 50, Bottom: 55);
        var wide = new DebrisZone("wide", new ByHull(Margin: 40, Lead: 10), 10, 0, 1, 1, 1, []) { Stretch = Belt.Id };
        IReadOnlyList<DebrisZone> zones = [Content.Debris.Ship, wide];
        // Inside the wide zone's box alone: 30 rows above the hull, past the stream's margin.
        int x = 105, y = hull.Top - 30;
        Assert.True(Content.Debris.Margin < 30);
        Assert.False(DebrisModule.InStream(x, y, hull, zones, running: null));
        Assert.False(DebrisModule.InStream(x, y, hull, zones, running: Drift.Id));
        Assert.True(DebrisModule.InStream(x, y, hull, zones, running: Belt.Id));
        Assert.True(DebrisModule.InStream(x, hull.Top, hull, zones, running: null));
    }

    // Invariants.

    /// <summary>A step of the property: an unlock obtained, a stretch brought, ticks run, or a wait or a run cut short.</summary>
    public enum Act { Obtain, Bring, Run, Hurry }

    /// <summary>Every item that opens a stretch of the ship's: obtained on a planet too, where it opens nothing.</summary>
    private static readonly string[] Unlocks = [.. Spec.Ship.SelectMany(s => s.Unlock)];

    /// <summary>Every stretch of every kind and one unknown, so each surface is brought another's too and refuses it.</summary>
    private static readonly string[] Ids = [.. Spec.Ship.Concat(Spec.FirstPlanet).Select(s => s.Id), "noSuchStretch"];

    /// <summary>
    /// The ship, or the test strip under the first planet's rules (its storms) with hero 0 beamed down; and the surface whose sky
    /// the property reads.
    /// </summary>
    private static (Scenario World, int Surface) Surface(bool planet)
    {
        if (!planet) return (Ship(), 0);
        var w = Ship();
        w.Do(new Enter(Beyond)).Do(new OpenSurface(Content.Surfaces.TestStrip.Id)).Run();
        return (w.Do(new Transfer(1)).Run(), 1);
    }

    /// <summary>
    /// Whatever is obtained and brought, and however waits and runs fall, aboard and on a planet: at most one stretch chosen, coming
    /// or running, never two; one coming only in its own warning, and always then; a running one's ticks left within its duration and
    /// a chosen one's wait within the longer of its first and its gap; <c>Opened</c> and <c>Came</c> only growing, each id once and
    /// only the surface's own, in its order, <c>Came</c> within <c>Opened</c>; and the chill the running stretch's and the night's alone.
    /// </summary>
    [Property(MaxTest = 60)]
    public Property AtMostOneIsChosenComingOrRunningWithinItsOwnSpansAndWhatOpenedOrCameOnlyGrows()
    {
        var step = from act in Gen.Frequency((1, Gen.Constant(Act.Obtain)), (1, Gen.Constant(Act.Bring)), (3, Gen.Constant(Act.Run)), (2, Gen.Constant(Act.Hurry)))
                   from n in Gen.Choose(0, 1000)
                   select (act, n);
        var cases = from planet in Gen.Elements(false, true) from n in Gen.Choose(1, 60) from steps in step.ArrayOf(n) select (planet, steps);
        return Prop.ForAll(cases.ToArbitrary(), c =>
            {
                var (w, surface) = Surface(c.planet);
                var kind = w.On(surface).Rules;
                var ids = kind.Stretches.Select(x => x.Id).ToList();
                SkyState Slice() => (SkyState)w.On(surface).Sky.Slice;
                List<string> opened = [], came = [];
                foreach (var (act, n) in c.steps)
                {
                    var s = Slice();
                    switch (act)
                    {
                        case Act.Obtain: Obtain(w, Unlocks[n % Unlocks.Length]); break;
                        case Act.Bring: w.Do(new StretchNow(Ids[n % Ids.Length])); break;
                        case Act.Run: break;
                        // Lowered only, so what the schedule drew stays within its span; a wait now and then past a short warning.
                        case Act.Hurry when s.Active != null: s.Left = Math.Min(s.Left, 1 + n % 50); break;
                        case Act.Hurry when s.Next != null: s.NextIn = Math.Min(s.NextIn, 1 + n); break;
                    }
                    w.Run(1 + n % 40);
                    s = Slice();
                    var now = w.On(surface).Sky.Now;
                    Assert.False(s.Active != null && s.Next != null);
                    if (s.Active is { } active)
                        Assert.InRange(s.Left, 1, kind.StretchOf(active)!.Duration.Most);
                    else
                        Assert.Equal(0, s.Left);
                    Assert.Equal(s.Next == null, s.NextIn == 0);
                    if (s.Next is { } next)
                    {
                        var chosen = kind.StretchOf(next)!;
                        Assert.InRange(s.NextIn, 1, Math.Max(chosen.First.Most, chosen.Gap.Most));
                        Assert.Equal(s.NextIn <= chosen.Warning ? ((string?)next, s.NextIn) : (null, 0), (now.Coming, now.ComingIn));
                    }
                    else
                        Assert.Equal(((string?)null, 0), (now.Coming, now.ComingIn));
                    Assert.Equal(s.Opened.Distinct().Count(), s.Opened.Count);
                    Assert.Equal(ids.Where(s.Opened.Contains), s.Opened);
                    Assert.Equal(ids.Where(s.Came.Contains), s.Came);
                    Assert.Subset(s.Opened.ToHashSet(), s.Came.ToHashSet());
                    Assert.Subset(s.Opened.ToHashSet(), opened.ToHashSet());
                    Assert.Subset(s.Came.ToHashSet(), came.ToHashSet());
                    int night = now.Night ? kind.Cycle!.Chill : 0;
                    Assert.Equal((s.Active is { } a ? kind.StretchOf(a)!.Chill : 0) + night, now.Chill);
                    (opened, came) = ([.. s.Opened], [.. s.Came]);
                }
            });
    }
}
