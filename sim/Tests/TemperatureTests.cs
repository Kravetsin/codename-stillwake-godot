using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Sky;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Temperature (2026-11-14-temperature-in-degrees): a cell's degrees by surface, band, air and night; a hero's limit by its body and
/// cape; each degree felt past it paid on the clock's schedule; the last breath running on while any is felt.
/// </summary>
public class TemperatureTests
{
    /// <summary>A pod in a vacuum room, never aired.</summary>
    private const string PodRoom = """
        ###########
        #.........#
        #..OOO...S#
        #..OOO....#
        #..OOO....#
        ###########
        """;

    /// <summary>Open ground under the sky: no back walls, a floor.</summary>
    private static Layout Open => Layout.Parse(["        ", "        ", "########"]);

    private static int AtPod(int x, int y) => y * 22 + x;
    /// <summary>The first cell right of the pod past its air's reach: vacuum, and no pod.</summary>
    private static readonly int Beyond = AtPod(14, 6);

    private static Hero HeroOf(Scenario w, int id = 0) => w.M.Crew.HeroOf(id)!;

    /// <summary>
    /// The whole units <paramref name="felt"/> degrees cost the breath on tick <paramref name="clock"/> of its surface's crew:
    /// <c>Bite</c> thousandths each, paid as the breaths counted pass whole units.
    /// </summary>
    internal static int ColdOn(int clock, int felt)
    {
        long rate = (long)felt * Content.Temperature.Bite, n = clock / Content.Crew.BreathEvery;
        return (int)((n + 1) * rate / 1000 - n * rate / 1000);
    }

    /// <summary>Runs whole breaths: from just after one beat to just after the <paramref name="count"/>-th next.</summary>
    private static Scenario Breaths(Scenario w, int count) =>
        w.Run(count * Content.Crew.BreathEvery - w.Crew().Clock % Content.Crew.BreathEvery);

    /// <summary>A surface of <paramref name="kind"/> as the crew and the sky see it: the one a test puts them on.</summary>
    private sealed class On(Content.Surfaces.Kind kind) : ISurfaces
    {
        public int Here => 1;
        public Content.Surfaces.Kind Kind => kind;
        public void Send(int peer, int surface) => throw new NotSupportedException("no one is sent anywhere");
        public bool CanOpen => false;
        public bool Landed => false;
        public bool Away => false;
        public void LeavePlanet() => throw new NotSupportedException();
        public int OpenPlanet() => throw new NotSupportedException("no planet is opened");
        public int Open(string kind, uint seed) => throw new NotSupportedException("no surface is opened");
        public uint Seed => 0;
        public int Visit(int peer, string kind, uint seed) => throw new NotSupportedException();
    }

    /// <summary>The <see cref="Open"/> map as a surface of <paramref name="kind"/>, hero 0 in it, at night when <paramref name="night"/>.</summary>
    private static Scenario OpenGround(Content.Surfaces.Kind kind, bool night, bool bare = false)
    {
        var w = new Scenario(Open, doubled: true) { Bare = bare };
        w.M.Crew.Where = w.M.Sky.Where = new On(kind);
        if (night) ((SkyState)w.M.Sky.Slice).Time = kind.Cycle!.Length - kind.Cycle.Night;
        return w.Do(new Enter(Cell(w, 3, 2)) { From = 0 }).Run();
    }

    private static int Cell(Scenario w, int x, int y) => y * w.M.Grid.Width + x;

    /// <summary>The first planet with every row a test's map has in band <paramref name="band"/>, every band as warm as that one.</summary>
    private static Content.PlanetType InBand(int band) => Content.PlanetTypes.First with
    {
        Bands = [.. Content.PlanetTypes.First.Bands.Select((bottom, i) => i < band ? -1000 - 1000 * (band - i) : bottom)],
        Temperatures = [.. Content.PlanetTypes.First.Temperatures.Select(_ => Content.PlanetTypes.First.Temperatures[band])],
    };

    [Fact]
    public void AboardAnAirlessCellInTheZoneIsTheHullsAndOffTheGridIsSpaceOnAnySurface()
    {
        var w = new Scenario(PodRoom);
        w.Do(new Enter(Beyond) { From = 0 }).Run();
        int size = w.M.Grid.Width * w.M.Grid.Height;
        Assert.Equal(Content.Temperature.Hull, w.M.Crew.TemperatureAt(Beyond));
        Assert.Equal((Content.Temperature.Space, Content.Temperature.Space), (w.M.Crew.TemperatureAt(-1), w.M.Crew.TemperatureAt(size)));
        var planet = OpenGround(Content.PlanetTypes.First, night: true);
        int width = planet.M.Grid.Width;
        // A small negative cell rounds into row 0, one past the bottom into the deepest band: neither is a cell.
        Assert.Equal(Content.Temperature.Space, planet.M.Crew.TemperatureAt(-1));
        Assert.Equal(Content.Temperature.Space, planet.M.Crew.TemperatureAt(width * planet.M.Grid.Height + width));
    }

    [Fact]
    public void APlanetsDegreesRunFromTheTopBandsToTheLastsADegreeARowAtMostWithNoStepAtABandsEdge()
    {
        // The user's ask: no band reads one round number all through (2026-11-14-temperature-in-degrees).
        var t = Content.PlanetTypes.First;
        var rows = Enumerable.Range(0, t.Ground.Height).Select(row => Terrain.TemperatureOf(t, row)).ToList();
        Assert.Equal((t.Temperatures[0], t.Temperatures[^1]), (rows[0], rows[^1]));
        for (int row = 1; row < rows.Count; row++)
            Assert.InRange(rows[row] - rows[row - 1], 0, 1);
        Assert.All(t.Temperatures, degrees => Assert.Contains(degrees, rows));
        // Where the cold band gives way to the garden, a reading between theirs.
        int edge = Enumerable.Range(0, t.Ground.Height).First(row => Terrain.BandOf(t, row) == 1);
        Assert.InRange(rows[edge], t.Temperatures[0] + 1, t.Temperatures[1] - 1);
    }

    [Fact]
    public void TheNightReachesOnlyTheTopBandUnderTheOpenSky()
    {
        var top = OpenGround(InBand(0), night: true);
        Assert.Equal(Content.PlanetTypes.First.Temperatures[0] - Content.Sky.First.Chill, top.M.Crew.TemperatureAt(HeroOf(top).Cell));
        var garden = OpenGround(InBand(1), night: true);
        Assert.Null(garden.M.Grid.BackAt(HeroOf(garden).Cell));
        Assert.Equal(Content.PlanetTypes.First.Temperatures[1], garden.M.Crew.TemperatureAt(HeroOf(garden).Cell));
        Assert.Equal(0, garden.M.Crew.Felt(0, HeroOf(garden).Cell));
        // The deep bands' heat costs nothing yet: no body withstands less than it.
        var ember = OpenGround(InBand(3), night: false, bare: true);
        Assert.Equal((Content.PlanetTypes.First.Temperatures[3], 0), (ember.M.Crew.TemperatureAt(HeroOf(ember).Cell), ember.M.Crew.Felt(0, HeroOf(ember).Cell)));
    }

    [Fact]
    public void TheBodysLimitThenTheCapesDegreesSetWhatIsWithstood()
    {
        var w = new Scenario(PodRoom) { Bare = true };
        w.Do(new Enter(Beyond) { From = 0 }).Run();
        Assert.Equal(Content.Temperature.BareLow, w.M.Crew.LowOf(0));
        w.Wear(0, Content.Suit.Basic);
        Assert.Equal(-40, w.M.Crew.LowOf(0));
        w.Wear(0, Content.Cape.Item);
        Assert.Equal(-40 - Content.Cape.Warmth, w.M.Crew.LowOf(0));
        Assert.Equal(0, w.M.Crew.Felt(0, Beyond));
        // No hero, nothing felt.
        Assert.Equal(0, w.M.Crew.Felt(1, Beyond));
    }

    [Fact]
    public void ARegenSuitWithNoAirOutAtNightPassesOutThoughItCoversEveryBreath()
    {
        // It gives back each breath, but not the cold: the last breath runs on through the breaths the cold's schedule skips.
        var w = OpenGround(Content.PlanetTypes.First, night: true, bare: true)
            .Wear(0, Content.Helmet.Basic).Wear(0, Content.Suit.Regen);
        w.Put(Content.Inventory.Slots + Content.Inventory.Equipment.ToList().IndexOf(Content.Charge.Belt), new Items.Stack("battery", 1, Content.Charge.Battery));
        w.Do(new SuitMode(true) { From = 0 }).Run();
        Assert.Equal(10, w.M.Crew.Felt(0, HeroOf(w).Cell));
        HeroOf(w).Air = 0;
        for (int i = 0; i < 2 * Content.Crew.LastBreath && HeroOf(w).Out == 0; i++) w.Run();
        Assert.True(HeroOf(w).Out > 0);
    }

    /// <summary>The first planet with every band at <paramref name="degrees"/>: a test's open ground reads them by day.</summary>
    private static Content.PlanetType At(int degrees) => Content.PlanetTypes.First with
    {
        Temperatures = [.. Content.PlanetTypes.First.Temperatures.Select(_ => degrees)],
    };

    /// <summary>Hero 0 on open ground at <paramref name="degrees"/> by day, in a basic helmet, the given body and the cape if <paramref name="caped"/>.</summary>
    private static Scenario InHeat(int degrees, string? body, bool caped = false)
    {
        var w = OpenGround(At(degrees), night: false, bare: true).Wear(0, Content.Helmet.Basic);
        if (body != null) w.Wear(0, body);
        if (caped) w.Wear(0, Content.Cape.Item);
        return w;
    }

    [Theory]
    // The heat plan's walk-through (2026-11-14-heat-limits): the amber deep's bottom, a fifth into the ember band, its middle and below;
    // the heat suit free in the middle.
    [InlineData(43, "suit", false, 0)]
    [InlineData(50, "suit", false, 0)]
    [InlineData(60, "suit", false, 10)]
    [InlineData(43, "suit", true, 0)]
    [InlineData(50, "suit", true, 5)]
    [InlineData(60, "suit", true, 15)]
    [InlineData(60, "regenSuit", false, 10)]
    [InlineData(60, "heatSuit", false, 0)]
    [InlineData(65, "heatSuit", false, 5)]
    [InlineData(60, "heatSuit", true, 5)]
    [InlineData(43, null, false, 13)]
    [InlineData(50, null, false, 20)]
    [InlineData(60, null, false, 30)]
    public void HeatIsTheDegreesPastTheBodysHighTheCapeFiveLowerAndNoColdBesides(int degrees, string? body, bool caped, int heat)
    {
        var w = InHeat(degrees, body, caped);
        int cell = HeroOf(w).Cell;
        Assert.Equal(degrees, w.M.Crew.TemperatureAt(cell));
        Assert.Equal((body == null ? Content.Temperature.BareHigh : body == Content.Suit.Heat ? 60 : 50) - (caped ? Content.Cape.Warmth : 0), w.M.Crew.HighOf(0));
        Assert.Equal((heat, 0), (w.M.Crew.Heat(0, cell), w.M.Crew.Felt(0, cell)));
        // No hero, nothing felt.
        Assert.Equal(0, w.M.Crew.Heat(1, cell));
    }

    [Theory]
    // Ten breaths: 10° felt are 2 units more than none, 15° 3 (Bite 20‰ a degree a breath).
    [InlineData(false, 2)]
    [InlineData(true, 3)]
    public void HeatCostsAsTheColdDoesBiteABreathADegree(bool caped, int more)
    {
        var (calm, hot) = (InHeat(45, Content.Suit.Basic), InHeat(60, Content.Suit.Basic, caped));
        foreach (var w in new[] { calm, hot })
        {
            HeroOf(w).Air = w.M.Crew.CapacityOf(0);
            Breaths(w, 10);
        }
        Assert.Equal(more, HeroOf(calm).Air - HeroOf(hot).Air);
    }

    [Fact]
    public void ARegenSuitWithNoAirInTheEmberPassesOutForItGivesNoneOfTheHeatBack()
    {
        // It gives back each breath, not the heat: the last breath runs on through the breaths the heat's schedule skips.
        var w = InHeat(60, Content.Suit.Regen);
        w.Put(Content.Inventory.Slots + Content.Inventory.Equipment.ToList().IndexOf(Content.Charge.Belt), new Items.Stack("battery", 1, Content.Charge.Battery));
        w.Do(new SuitMode(true) { From = 0 }).Run();
        Assert.Equal(10, w.M.Crew.Heat(0, HeroOf(w).Cell));
        HeroOf(w).Air = 0;
        for (int i = 0; i < 2 * Content.Crew.LastBreath && HeroOf(w).Out == 0; i++) w.Run();
        Assert.True(HeroOf(w).Out > 0);
    }

    /// <summary>Any body or none, caped or not, at any degrees a planet may read: the cold and the heat are never felt at once.</summary>
    [Property(MaxTest = 40)]
    public Property NeverFeelsTheColdAndTheHeatAtOnce()
    {
        var cases = from body in Gen.Elements<string?>(null, Content.Suit.Basic, Content.Suit.Regen)
                    from caped in Gen.Elements(false, true)
                    from degrees in Gen.Choose(-80, 100)
                    select (body, caped, degrees);
        return Prop.ForAll(cases.ToArbitrary(), c =>
        {
            var w = InHeat(c.degrees, c.body, c.caped);
            int cell = HeroOf(w).Cell, felt = w.M.Crew.Felt(0, cell), heat = w.M.Crew.Heat(0, cell);
            return (felt == 0 || heat == 0).Label($"{c}: felt {felt}, heat {heat}");
        });
    }

    [Fact]
    public void TheColdLeavesTheCapesRemainderForBlowsAlone()
    {
        var w = new Scenario(PodRoom) { Bare = true }.Wear(0, Content.Cape.Item);
        w.Do(new Enter(Beyond) { From = 0 }).Run();
        w.M.Crew.Blow(0, 3, Harm.Blow);
        Assert.Equal(600, HeroOf(w).Spared);
        Breaths(w, 10);
        Assert.Equal(600, HeroOf(w).Spared);
    }

    /// <summary>
    /// A hero in vacuum aboard, in a body or bare, caped or not, breathing <c>k</c> breaths from any tick: its air pays the breaths
    /// and, within a unit, <c>k</c> × felt × <c>Bite</c> thousandths; and more felt never pays less.
    /// </summary>
    [Property(MaxTest = 40)]
    public Property PaysTheDegreesFeltAtBiteABreathWithinAUnit()
    {
        var cases = from bodied in Gen.Elements(false, true)
                    from caped in Gen.Elements(false, true)
                    from start in Gen.Choose(0, 200)
                    from k in Gen.Choose(1, 10)
                    select (bodied, caped, start, k);
        return Prop.ForAll(cases.ToArbitrary(), c =>
        {
            var w = new Scenario(PodRoom) { Bare = !c.bodied };
            if (c.caped) w.Wear(0, Content.Cape.Item);
            w.Do(new Enter(Beyond) { From = 0 }).Run(1 + c.start);
            while (w.Crew().Clock % Content.Crew.BreathEvery != 0) w.Run();
            int felt = w.M.Crew.Felt(0, Beyond);
            HeroOf(w).Air = w.M.Crew.CapacityOf(0);
            Breaths(w, c.k);
            int paid = w.M.Crew.CapacityOf(0) - HeroOf(w).Air - c.k;
            int owed = c.k * felt * Content.Temperature.Bite;
            return (paid * 1000 > owed - 1000 && paid * 1000 < owed + 1000).Label($"felt {felt} over {c.k}: paid {paid}, owed {owed}‰");
        });
    }
}
