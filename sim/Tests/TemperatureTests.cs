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

    /// <summary>Runs whole breaths: from just after one beat to just after the <paramref name="count"/>-th next.</summary>
    private static Scenario Breaths(Scenario w, int count) =>
        w.Run(count * Content.Crew.BreathEvery - w.Crew().Clock % Content.Crew.BreathEvery);

    /// <summary>A surface of <paramref name="kind"/> as the crew and the sky see it: the one a test puts them on.</summary>
    internal sealed class On(Content.Surfaces.Kind kind) : ISurfaces
    {
        public int Here => 1;
        public Content.Surfaces.Kind Kind => kind;
        public void Send(int peer, int surface) => throw new NotSupportedException("no one is sent anywhere");
        public bool CanOpen => false;
        public bool Landed => false;
        public bool Away => false;
        public void LeavePlanet() => throw new NotSupportedException();
        public int OpenPlanet(string kind, IReadOnlyList<string>? ores) => throw new NotSupportedException("no planet is opened");
        public int Open(string kind, uint seed, IReadOnlyList<Content.WreckStep>? wreck = null) => throw new NotSupportedException("no surface is opened");
        public uint Seed => 0;
        public IReadOnlyList<Content.WreckStep>? Wreck => null;
        public int Visit(int peer, string kind, uint seed, IReadOnlyList<Content.WreckStep>? wreck = null) => throw new NotSupportedException();
    }

    /// <summary>The <see cref="Open"/> map as a surface of <paramref name="kind"/>, hero 0 in it, at night when <paramref name="night"/>.</summary>
    internal static Scenario OpenGround(Content.Surfaces.Kind kind, bool night, bool bare = false)
    {
        var w = new Scenario(Open, doubled: true) { Bare = bare };
        w.M.Crew.Where = w.M.Sky.Where = new On(kind);
        if (night) ((SkyState)w.M.Sky.Slice).Time = kind.Cycle!.Length - kind.Cycle.Night;
        return w.Do(new Enter(Cell(w, 3, 2)) { From = 0 }).Run();
    }

    private static int Cell(Scenario w, int x, int y) => y * w.M.Grid.Width + x;

    /// <summary>The first planet with every row a test's map has in band <paramref name="band"/>, every band as warm as that one, its surface's air too.</summary>
    internal static Content.PlanetType InBand(int band) => Content.PlanetTypes.First with
    {
        Air = null,
        Bands = [.. Content.PlanetTypes.First.Bands.Select((bottom, i) => i < band ? -1000 - 1000 * (band - i) : bottom)],
        Temperatures = [.. Content.PlanetTypes.First.Temperatures.Select(_ => Content.PlanetTypes.First.Temperatures[band])],
    };

    [Fact]
    public void AboardAnAirlessCellInTheZoneIsTheHullsAndOffTheGridIsTheDeepsOnAnySurface()
    {
        var w = new Scenario(PodRoom);
        w.Do(new Enter(Beyond) { From = 0 }).Run();
        int size = w.M.Grid.Width * w.M.Grid.Height;
        Assert.Equal(Content.Temperature.Hull, w.M.Crew.TemperatureAt(Beyond));
        Assert.Equal((Content.Temperature.Deep, Content.Temperature.Deep), (w.M.Crew.TemperatureAt(-1), w.M.Crew.TemperatureAt(size)));
        var planet = OpenGround(Content.PlanetTypes.First, night: true);
        int width = planet.M.Grid.Width;
        // A small negative cell rounds into row 0, one past the bottom into the deepest band: neither is a cell.
        Assert.Equal(Content.Temperature.Deep, planet.M.Crew.TemperatureAt(-1));
        Assert.Equal(Content.Temperature.Deep, planet.M.Crew.TemperatureAt(width * planet.M.Grid.Height + width));
    }

    [Fact]
    public void APlanetsDegreesRunFromTheTopBandsToTheLastsADegreeARowAtMostWithNoStepAtABandsEdge()
    {
        // The user's ask: no band reads one round number all through (2026-11-14-temperature-in-degrees). The mild air up top cools to
        // the cold band's middle, then the planet warms to its core (2026-11-17-mild-climate).
        var t = Content.PlanetTypes.First;
        var rows = Enumerable.Range(0, t.Ground.Height).Select(row => Terrain.TemperatureOf(t, row)).ToList();
        Assert.Equal((t.Air!.Value, t.Temperatures[^1]), (rows[0], rows[^1]));
        Assert.Equal(t.Air.Value, rows[t.Ground.Surface]);
        int coldest = rows.IndexOf(rows.Min());
        Assert.Equal(t.Temperatures[0], rows[coldest]);
        for (int row = 1; row < rows.Count; row++)
            Assert.InRange(rows[row] - rows[row - 1], row <= coldest ? -1 : 0, row <= coldest ? 0 : 1);
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
        Assert.Equal(-110, w.M.Crew.LowOf(0));
        w.Wear(0, Content.Cape.Item);
        Assert.Equal(-110 - Content.Cape.Warmth, w.M.Crew.LowOf(0));
        Assert.Equal(0, w.M.Crew.Felt(0, Beyond));
        // No hero, nothing felt.
        Assert.Equal(0, w.M.Crew.Felt(1, Beyond));
    }

    [Fact]
    public void ARegenSuitWithNoAirOutInTheColdBreathesOnForTheColdTakesNoAir()
    {
        // It gives back each breath, and the cold moves the body, not the air (body-temperature): the last breath ends.
        var w = OpenGround(At(-120), night: false, bare: true)
            .Wear(0, Content.Helmet.Basic).Wear(0, Content.Suit.Regen);
        w.Put(Content.Inventory.Slots + Content.Inventory.Equipment.ToList().IndexOf(Content.Charge.Belt), new Items.Stack("battery", 1, Content.Charge.Battery));
        w.Do(new SuitMode(true) { From = 0 }).Run();
        Assert.Equal(10, w.M.Crew.Felt(0, HeroOf(w).Cell));
        HeroOf(w).Air = 0;
        Breaths(w, 2 * Content.Crew.LastBreath / Content.Crew.BreathEvery);
        Assert.Equal((0, 0), (HeroOf(w).Out, HeroOf(w).Gasp));
    }

    /// <summary>The first planet with every band at <paramref name="degrees"/>: a test's open ground reads them by day.</summary>
    private static Content.PlanetType At(int degrees) => Content.PlanetTypes.First with
    {
        Air = null,
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
    // Ten breaths: 10° felt, 15° caped, warm the body toward 39.0° and 40.0°, and cost the air nothing (body-temperature).
    [InlineData(false)]
    [InlineData(true)]
    public void HeatWarmsTheBodyAndCostsNoAir(bool caped)
    {
        var (calm, hot) = (InHeat(45, Content.Suit.Basic), InHeat(60, Content.Suit.Basic, caped));
        foreach (var w in new[] { calm, hot })
        {
            HeroOf(w).Air = w.M.Crew.CapacityOf(0);
            Breaths(w, 10);
        }
        Assert.Equal(HeroOf(calm).Air, HeroOf(hot).Air);
        Assert.Equal(0, calm.M.Crew.Heat(0, HeroOf(calm).Cell));
        Assert.Equal(caped ? 15 : 10, hot.M.Crew.Heat(0, HeroOf(hot).Cell));
    }

    [Fact]
    public void ARegenSuitWithNoAirInTheEmberBreathesOnForTheHeatTakesNoAir()
    {
        // It gives back each breath, and the heat moves the body, not the air (body-temperature): the last breath ends.
        var w = InHeat(60, Content.Suit.Regen);
        w.Put(Content.Inventory.Slots + Content.Inventory.Equipment.ToList().IndexOf(Content.Charge.Belt), new Items.Stack("battery", 1, Content.Charge.Battery));
        w.Do(new SuitMode(true) { From = 0 }).Run();
        Assert.Equal(10, w.M.Crew.Heat(0, HeroOf(w).Cell));
        HeroOf(w).Air = 0;
        Breaths(w, 2 * Content.Crew.LastBreath / Content.Crew.BreathEvery);
        Assert.Equal((0, 0), (HeroOf(w).Out, HeroOf(w).Gasp));
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
        // Bare and caped at −10°: 5 felt, the cape's remainder untouched.
        var w = InHeat(-10, null, caped: true);
        w.M.Crew.Blow(0, 3, Harm.Blow);
        Assert.Equal(600, HeroOf(w).Spared);
        Breaths(w, 10);
        Assert.Equal(600, HeroOf(w).Spared);
        Assert.Equal(5, w.M.Crew.Felt(0, HeroOf(w).Cell));
    }
}
