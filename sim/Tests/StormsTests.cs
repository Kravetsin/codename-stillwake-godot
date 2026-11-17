using System.Text.Json;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Sky;
using Xunit;
using Spec = Stillwake.Content.Stretches;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Storms on the first planet (docs/plans/surface-storms.md): two stretches of its kind, dust and rain, open from the start, about three a day, warned
/// over its own 800 ticks, the open sky 20° colder while it runs; drawn from a fork, so the world's own Rng never moves for it.
/// </summary>
public class StormsTests
{
    private const string Ship = """
        ............
        .##########.
        .#..D..S..#.
        .##########.
        ............
        """;

    private static readonly Content.Surfaces.Kind Strip = Content.Surfaces.TestStrip;
    /// <summary>The dust storm, whose numbers the rain storm shares: what a rule of either is checked on.</summary>
    private static readonly Spec.Stretch Storm = Spec.DustStorm, Rain = Spec.RainStorm;
    private static readonly Content.Sky.Cycle Day = Strip.Cycle!;
    private static readonly int Width = 2 * Strip.Rows[0].Length;

    /// <summary>The bottom-left cell of the strip's character at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private static int Feet(int x, int y) => 2 * y * Width + 2 * x + Width;

    /// <summary>Out on the strip's flat under the open sky, standing on the ground (as <c>SkyTests</c>).</summary>
    private static readonly int Open = Feet(20, 30 - 1);

    private static int NightAt => Day.Length - Day.Night;

    /// <summary>The ship with the strip opened and hero 0 beamed down to it.</summary>
    private static Scenario Down()
    {
        var w = new Scenario(Ship);
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Strip.Id)).Run();
        return w.Do(new Transfer(1)).Run();
    }

    private static SkyState S(Scenario w, int surface = 1) => (SkyState)w.On(surface).Sky.Slice;
    private static SkyNow Now(Scenario w, int surface = 1) => w.On(surface).Sky.Now;

    /// <summary>The world's own random state, as its save holds it.</summary>
    private static uint WorldRng(Scenario w) => JsonDocument.Parse(w.World.Save()).RootElement.GetProperty("rng").GetUInt32();

    /// <summary>Runs until <paramref name="until"/> holds, at most <paramref name="most"/> ticks; the ticks run.</summary>
    private static int RunUntil(Scenario w, Func<bool> until, int most = 100_000)
    {
        for (int t = 1; t <= most; t++)
        {
            w.Run();
            if (until()) return t;
        }
        throw new Xunit.Sdk.XunitException($"not within {most} ticks");
    }

    [Fact]
    public void BothStormsAreOpenFromTheFirstTickEachFirstWithinItsFirstAndEveryLaterOneWithinGapOfTheLastEnd()
    {
        Assert.Equal((Storm.First, Storm.Gap, Storm.Duration, Storm.Warning), (Rain.First, Rain.Gap, Rain.Duration, Rain.Warning));
        var w = new Scenario(Ship);
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Strip.Id)).Run();
        // Opened on the strip's first tick: the dust storm chosen, its wait drawn and counted down from it.
        Assert.Equal([Storm.Id, Rain.Id], S(w).Opened);
        Assert.Equal(Storm.Id, S(w).Next);
        int first = S(w).NextIn;
        Assert.InRange(first, Storm.First.Least - 1, Storm.First.Most);
        Assert.Equal(first, RunUntil(w, () => Now(w).Stretch != null));
        for (int storm = 0; storm < 4; storm++)
        {
            int left = S(w).Left;
            Assert.InRange(left, Storm.Duration.Least, Storm.Duration.Most);
            Assert.Equal(left, RunUntil(w, () => Now(w).Stretch == null));
            // Its wait drawn on the tick it ended, warning included: the rain's first, as it never came; a gap after.
            int wait = S(w).NextIn;
            if (storm == 0) Assert.Equal((Rain.Id, true), (S(w).Next, wait >= Rain.First.Least && wait <= Rain.First.Most));
            else Assert.InRange(wait, Storm.Gap.Least, Storm.Gap.Most);
            Assert.Equal(wait, RunUntil(w, () => Now(w).Stretch != null));
        }
        Assert.Equal([Storm.Id, Rain.Id], S(w).Came);
    }

    /// <summary>Once both have come, each next storm is either, drawn: over a few days both keep coming.</summary>
    [Fact]
    public void OverDaysBothKindsKeepComing()
    {
        var w = Down();
        var seen = new List<string>();
        for (int storm = 0; storm < 10; storm++)
        {
            RunUntil(w, () => Now(w).Stretch != null);
            seen.Add(Now(w).Stretch!);
            RunUntil(w, () => Now(w).Stretch == null);
        }
        Assert.Equal([Storm.Id, Rain.Id], seen.Take(2));
        Assert.Contains(Storm.Id, seen.Skip(2));
        Assert.Contains(Rain.Id, seen.Skip(2));
        // About three a 20-minute day: ten storms past ten of the shortest waits and runs, within ten of the longest.
        Assert.InRange(w.World.Tick, 10 * (Storm.Gap.Least + Storm.Duration.Least), 10 * (Storm.First.Most + Storm.Duration.Most));
    }

    [Fact]
    public void ItIsSeenComingForItsOwnWarningNotTheShips()
    {
        Assert.True(Storm.Warning < Spec.IceBelt.Warning);
        var w = Down();
        RunUntil(w, () => Now(w).Coming != null);
        Assert.Equal((Storm.Id, Storm.Warning, null), (Now(w).Coming, Now(w).ComingIn, Now(w).Stretch));
        w.Run(Storm.Warning - 1);
        Assert.Equal((Storm.Id, 1), (Now(w).Coming, Now(w).ComingIn));
        w.Run();
        Assert.Equal((Storm.Id, null, Storm.Chill), (Now(w).Stretch, Now(w).Coming, Now(w).Chill));
    }

    [Fact]
    public void StretchNowBringsAStormOnTheStripAfterItsWarning()
    {
        var w = Down();
        w.Do(new StretchNow(Storm.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((Storm.Id, Storm.Warning - 1), (Now(w).Coming, Now(w).ComingIn));
        w.Run(Storm.Warning - 1);
        Assert.Equal(Storm.Id, Now(w).Stretch);
    }

    [Fact]
    public void StretchNowIsRefusedForAStretchOfAnotherKindAndWhileOneRuns()
    {
        var ship = new Scenario(Ship);
        ship.Do(new StretchNow(Storm.Id)).Run();
        Assert.Equal("sky.noStretch", Assert.Single(ship.Refusals).Reason);
        Assert.Empty(S(ship, 0).Opened);

        var w = Down();
        w.Do(new StretchNow(Spec.IceBelt.Id)).Run();
        Assert.Equal("sky.noStretch", Assert.Single(w.Refusals).Reason);
        Assert.Equal([Storm.Id, Rain.Id], S(w).Opened);

        w.Refusals.Clear();
        w.Do(new StretchNow(Storm.Id)).Run(Storm.Warning);
        Assert.Equal(Storm.Id, Now(w).Stretch);
        int left = S(w).Left;
        w.Do(new StretchNow(Rain.Id)).Run();
        Assert.Equal("sky.stretchRunning", Assert.Single(w.Refusals).Reason);
        Assert.Equal((Storm.Id, left - 1), (Now(w).Stretch, S(w).Left));
    }

    [Fact]
    public void TheShipHasOnlyItsStretchesAndTheIcyAndScorchedPlanetsNone()
    {
        Assert.Equal([Spec.IceBelt, Spec.ContrabandDrift], Content.Surfaces.Ship.Stretches);
        Assert.Equal([Storm, Rain], Content.PlanetTypes.First.Stretches);
        Assert.Equal([Storm, Rain], Strip.Stretches);
        Assert.Empty(Content.PlanetTypes.Icy.Stretches);
        Assert.Empty(Content.PlanetTypes.Scorched.Stretches);
        // Over a long run, nothing is opened, chosen or run on either.
        foreach (var kind in new[] { Content.PlanetTypes.Icy, Content.PlanetTypes.Scorched })
        {
            var w = TemperatureTests.OpenGround(kind, night: false);
            // Kept breathing: thin air alone would pass the hero out first.
            for (int t = 0; t < Storm.First.Most + Storm.Warning; t += 1000)
            {
                w.M.Crew.HeroOf(0)!.Air = Scenario.Full;
                w.Run(1000);
            }
            Assert.Equal((null, null, 0), (w.M.Sky.Now.Stretch, w.M.Sky.Now.Coming, w.M.Sky.Now.Chill));
            Assert.Empty(((SkyState)w.M.Sky.Slice).Opened);
        }
    }

    [Fact]
    public void AStormSavesLoadsAndPlaysOnTheSame()
    {
        var w = Down();
        w.Do(new StretchNow(Storm.Id)).Run(Storm.Warning + 50);
        Assert.Equal(Storm.Id, Now(w).Stretch);
        Assert.True(S(w).Drawn > 0);
        var loaded = new Scenario(Ship).Reload(w.World.Save());
        Assert.Equal(w.World.Hash(), loaded.World.Hash());
        Assert.Equal(Now(w), Now(loaded));
        Assert.Equal(S(w).Left, S(loaded).Left);
        w.Run(2 * Storm.Duration.Most + Storm.Gap.Most);
        loaded.Run(2 * Storm.Duration.Most + Storm.Gap.Most);
        Assert.Equal(w.World.Hash(), loaded.World.Hash());
        Assert.Equal(Now(w), Now(loaded));
    }

    [Fact]
    public void ChoosingOrStartingAStormLeavesTheWorldsRngAsItWas()
    {
        var w = Down();
        w.Do(new StretchNow(Rain.Id)).Run(Storm.Warning - 1);
        Assert.Equal((Rain.Id, 1), (Now(w).Coming, Now(w).ComingIn));
        uint before = WorldRng(w), drawn = (uint)S(w).Drawn;
        // It starts, its duration drawn.
        w.Run();
        Assert.Equal(Rain.Id, Now(w).Stretch);
        Assert.Equal((before, drawn + 1), (WorldRng(w), (uint)S(w).Drawn));
        // It ends, the next chosen and its gap drawn.
        w.Run(S(w).Left - 1);
        before = WorldRng(w);
        w.Run();
        Assert.Equal((null, Storm.Id), (Now(w).Stretch, S(w).Next));
        Assert.Equal(before, WorldRng(w));
    }

    // The cold, mild up top (the user's, 2026-11-17).

    [Fact]
    public void TheOpenSkyIsMildByDayAndTheNightOrAStormOnlyCoolsIt()
    {
        Assert.Equal((12, 20, 10, 5), (Strip.Temperatures[0], Day.Chill, Storm.Chill, Rain.Chill));
        var w = Down();
        var crew = w.On(1).Crew;
        int shelter = crew.Spawn;
        Assert.NotNull(w.On(1).Grid.BackAt(shelter));
        Assert.Null(w.On(1).Grid.BackAt(Open));
        Assert.Equal((12, 12), (crew.TemperatureAt(Open), crew.TemperatureAt(shelter)));
        w.Do(new SetTime(NightAt)).Run();
        Assert.Equal((-8, 12), (crew.TemperatureAt(Open), crew.TemperatureAt(shelter)));
        w.Do(new StretchNow(Rain.Id)).Run(Rain.Warning);
        Assert.Equal(Rain.Id, Now(w).Stretch);
        Assert.Equal((-13, 12), (crew.TemperatureAt(Open), crew.TemperatureAt(shelter)));
        w.Do(new SetTime(0)).Run();
        Assert.Equal((7, 12), (crew.TemperatureAt(Open), crew.TemperatureAt(shelter)));
        // Off on the tick it ends.
        w.Run(S(w).Left);
        Assert.Null(Now(w).Stretch);
        Assert.Equal(12, crew.TemperatureAt(Open));
    }


    [Fact]
    public void AStormHalvesAHerosPaceUnderTheOpenSkyOnlyWhileItRunsNeitherInItsWarningNorUnderABackWall()
    {
        Assert.Equal((500, 500), (Storm.Pace, Rain.Pace));
        var w = Down();
        var crew = w.On(1).Crew;
        int shelter = crew.Spawn;
        Assert.Equal((1000, 1000), (crew.PaceAt(Open), crew.PaceAt(shelter)));
        w.Do(new StretchNow(Storm.Id)).Run();
        Assert.Equal(Storm.Id, Now(w).Coming);
        Assert.Equal(1000, crew.PaceAt(Open));
        w.Run(Storm.Warning);
        Assert.Equal(Storm.Id, Now(w).Stretch);
        Assert.Equal((500, 1000), (crew.PaceAt(Open), crew.PaceAt(shelter)));
        w.Run(S(w).Left);
        Assert.Null(Now(w).Stretch);
        Assert.Equal(1000, crew.PaceAt(Open));
    }

    [Fact]
    public void BelowThePlanetsTopBandAStormChangesNothing()
    {
        var garden = TemperatureTests.OpenGround(TemperatureTests.InBand(1), night: true);
        int cell = garden.M.Crew.HeroOf(0)!.Cell;
        Assert.Null(garden.M.Grid.BackAt(cell));
        garden.Do(new StretchNow(Storm.Id)).Run(Storm.Warning);
        Assert.Equal((Storm.Id, Storm.Chill + Day.Chill), (garden.M.Sky.Now.Stretch, garden.M.Sky.Now.Chill));
        Assert.Equal(Content.PlanetTypes.First.Temperatures[1], garden.M.Crew.TemperatureAt(cell));
        Assert.Equal(1000, garden.M.Crew.PaceAt(cell));
    }

    /// <summary>
    /// The air hero 0, out on the flat in a basic suit, loses over <paramref name="storm"/> brought at the hour <paramref name="time"/>:
    /// what it loses there less what a hero on a calm day loses over the same ticks, breathing's own cost; the air topped up each tick.
    /// </summary>
    private static int LostOverAStorm(Spec.Stretch storm, int time)
    {
        var stormy = Down();
        var calm = Down();
        stormy.Do(new Enter(Open)).Do(new SetTime(time)).Do(new StretchNow(storm.Id)).Run(storm.Warning);
        calm.Do(new Enter(Open)).Do(new SetTime(0)).Run(storm.Warning);
        Assert.Equal(storm.Id, Now(stormy).Stretch);
        Assert.Null(Now(calm).Stretch);
        int lost = 0, ticks = 0;
        while (Now(stormy).Stretch != null)
        {
            Hero a = stormy.On(1).Crew.HeroOf(0)!, b = calm.On(1).Crew.HeroOf(0)!;
            (a.Air, b.Air) = (Scenario.Full, Scenario.Full);
            stormy.Run();
            calm.Run();
            lost += (Scenario.Full - a.Air) - (Scenario.Full - b.Air);
            ticks++;
        }
        Assert.InRange(ticks, storm.Duration.Least - 1, storm.Duration.Most);
        Assert.Equal(Open, stormy.On(1).Crew.HeroOf(0)!.Cell);
        return lost;
    }

    [Fact]
    public void ABasicSuitFeelsNoColdInTheRainEvenAtNight()
    {
        Assert.Equal(-40, Content.Items.Get(Content.Suit.Basic).Low);
        Assert.Equal(0, LostOverAStorm(Rain, NightAt));
    }

    // The grit.

    [Fact]
    public void TheDustStormsGritGrindsTwoUnitsASecondOutOfASuitInTheOpen()
    {
        Assert.Equal((2, 0), (Storm.Grit, Rain.Grit));
        // A breath a second, 60 to 90 s: a basic helmet's 45 gone in some 20 s.
        int least = Storm.Duration.Least / Content.Crew.BreathEvery, most = Storm.Duration.Most / Content.Crew.BreathEvery;
        Assert.InRange(LostOverAStorm(Storm, 0), 2 * (least - 1), 2 * (most + 1));
    }

    [Fact]
    public void GritGrindsOnlyUnderTheOpenSkyWhileTheDustStormRuns()
    {
        var w = Down();
        var crew = w.On(1).Crew;
        int shelter = crew.Spawn;
        w.Do(new StretchNow(Storm.Id)).Run();
        Assert.Equal(Storm.Id, Now(w).Coming);
        Assert.Equal(0, crew.GritAt(Open));
        w.Run(Storm.Warning);
        Assert.Equal(Storm.Id, Now(w).Stretch);
        Assert.Equal((2, 0), (crew.GritAt(Open), crew.GritAt(shelter)));
        w.Run(S(w).Left);
        Assert.Null(Now(w).Stretch);
        Assert.Equal(0, crew.GritAt(Open));
        w.Do(new StretchNow(Rain.Id)).Run(Rain.Warning + 1);
        Assert.Equal((Rain.Id, 0), (Now(w).Stretch, crew.GritAt(Open)));
    }

    [Fact]
    public void GritRunsAHeroInTheOpenOutOfAirIntoItsLastBreath()
    {
        var w = Down();
        w.Do(new Enter(Open)).Do(new StretchNow(Storm.Id)).Run(Storm.Warning);
        var hero = w.On(1).Crew.HeroOf(0)!;
        w.Run(25 * Content.Crew.BreathEvery);
        Assert.Equal(0, hero.Air);
        Assert.True(hero.Gasp > 0 || hero.Out > 0);
    }
}
