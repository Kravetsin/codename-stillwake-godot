using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Sky;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>A planet's day and night (2026-11-07-night-chills), on the test strip: the hour, the night line, the chill outside in degrees (2026-11-14-temperature-in-degrees).</summary>
public class SkyTests
{
    private const string Ship = """
        ............
        .##########.
        .#..D..S..#.
        .##########.
        ............
        """;

    private static readonly Content.Surfaces.Kind Strip = Content.Surfaces.TestStrip;
    private static readonly Content.Sky.Cycle Day = Strip.Cycle!;
    private static readonly int Width = 2 * Strip.Rows[0].Length;

    /// <summary>The bottom-left cell of the strip's character at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private static int Feet(int x, int y) => 2 * y * Width + 2 * x + Width;

    private static int Cell(int x, int y) => y * Width + x;

    /// <summary>The strip's ground on the flat; its sheltered spawn is cut into the cliff at column 30.</summary>
    private const int Ground = 30;

    /// <summary>Out on the flat under the open sky, standing on the ground.</summary>
    private static readonly int Open = Feet(20, Ground - 1);

    /// <summary>Thin air's beat on the strip: its <c>ThinAir</c> breaths.</summary>
    private static int Beat => Strip.ThinAir!.Value * Content.Crew.BreathEvery;

    /// <summary>
    /// The degrees the night under the open sky is felt past the limit of a hero with no suit, <paramref name="warmth"/> fewer with a
    /// cape: what draws its body, which its client keeps (body-temperature).
    /// </summary>
    private static int NightFelt(int warmth = 0) => Content.Temperature.BareLow - warmth - (Strip.Temperatures[0] - Day.Chill);

    private static int FeltOf(Scenario w) => w.On(1).Crew.Felt(0, w.On(1).Crew.HeroOf(0)!.Cell);

    /// <summary>
    /// The ship with the strip opened and hero 0 beamed down to it bare, wearing the cape when <paramref name="caped"/>: the strip's
    /// mild night is felt only past a bare body's limit (2026-11-17-mild-climate); a helmet alone keeps it out (gear-roles).
    /// </summary>
    private static Scenario Down(bool caped = false)
    {
        var w = new Scenario(Ship) { Bare = true };
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Strip.Id)).Run();
        if (caped) w.Wear(0, Content.Cape.Item);
        return w.Do(new Transfer(1)).Run();
    }

    /// <summary>Hero 0 at <paramref name="cell"/> at the hour <paramref name="time"/>, then the air it has lost over <paramref name="beats"/> beats.</summary>
    private static int Lost(Scenario w, int cell, int time, int beats = 1)
    {
        var m = w.On(1);
        w.Do(new Enter(cell)).Do(new SetTime(time)).Run();
        // Any span of a beat holds exactly one paying breath, wherever it starts.
        int before = m.Crew.HeroOf(0)!.Air;
        w.Run(beats * Beat);
        return before - m.Crew.HeroOf(0)!.Air;
    }

    private static int NightAt => Day.Length - Day.Night;

    [Fact]
    public void APlanetOpensAtTheStartOfItsDayAndCountsATickATick()
    {
        var w = Down();
        var sky = w.On(1).Sky;
        int at = sky.Now.Time;
        Assert.InRange(at, 1, 10);
        Assert.False(sky.Now.Night);
        Assert.Equal(0, sky.Now.Chill);
        w.Run(25);
        Assert.Equal(at + 25, sky.Now.Time);
    }

    [Fact]
    public void NightIsTheDaysLastStretchAndTheDayWrapsAtItsLength()
    {
        var w = Down();
        var sky = w.On(1).Sky;
        // A command applies before the hour advances on its tick.
        w.Do(new SetTime(NightAt - 2)).Run();
        Assert.Equal(new SkyNow(NightAt - 1, false, 0, null, null, 0), sky.Now);
        w.Run();
        Assert.Equal(new SkyNow(NightAt, true, Day.Chill, null, null, 0), sky.Now);
        w.Do(new SetTime(Day.Length - 2)).Run();
        Assert.Equal(new SkyNow(Day.Length - 1, true, Day.Chill, null, null, 0), sky.Now);
        w.Run();
        Assert.Equal(new SkyNow(0, false, 0, null, null, 0), sky.Now);
    }

    [Theory]
    [InlineData(5, 6)]
    [InlineData(-2, -1)]
    [InlineData(3 * 24_000 + 7, 8)]
    public void SetTimeTakesTheHourModuloTheDayEitherWay(int time, int then)
    {
        var w = Down();
        w.Do(new SetTime(time)).Run();
        Assert.Equal((then % Day.Length + Day.Length) % Day.Length, w.On(1).Sky.Now.Time);
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void TheShipHasNoDaySetTimeIsRefusedThereAndItsSkyIsCalm()
    {
        var w = new Scenario(Ship);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        string before = w.World.Save();
        w.Do(new SetTime(NightAt)).Run();
        Assert.Equal("sky.noDay", Assert.Single(w.Refusals).Reason);
        Assert.Equal(new SkyNow(0, false, 0, null, null, 0), w.M.Sky.Now);
        Assert.Equal(0, ((SkyState)w.M.Sky.Slice).Time);
        Assert.Contains("\"sky\":{\"time\":0}", before);
    }

    [Fact]
    public void AnHourSavedPastAShorterDaysEndIsReadModuloAndWrapsOnItsNextTick()
    {
        var w = Down();
        var m = w.On(1);
        ((SkyState)m.Sky.Slice).Time = Day.Length + 3;
        Assert.Equal(3, m.Sky.Now.Time);
        w.Run();
        Assert.Equal(4, m.Sky.Now.Time);
    }

    [Fact]
    public void AtNightAHeroUnderTheOpenSkyFeelsTheColdAndPaysOnlyThinAir()
    {
        // 8 degrees felt settle a bare body at 35.4° within five beats, its air paying thin air alone (body-temperature).
        Assert.Equal(8, NightFelt());
        var day = Down();
        Assert.Equal((5, 0), (Lost(day, Open, 0, beats: 5), FeltOf(day)));
        var night = Down();
        Assert.Equal((5, NightFelt()), (Lost(night, Open, NightAt, beats: 5), FeltOf(night)));
        var longer = Down();
        Assert.Equal((10, NightFelt()), (Lost(longer, Open, NightAt, beats: 10), FeltOf(longer)));
        // Late in the night, five beats before dawn, it still bites.
        var late = Down();
        Assert.Equal((5, NightFelt()), (Lost(late, Open, Day.Length - 6 * Beat, beats: 5), FeltOf(late)));
    }

    [Fact]
    public void ABasicSuitFeelsNothingOfTheMildNight()
    {
        var w = new Scenario(Ship);
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Strip.Id)).Run();
        w.Do(new Transfer(1)).Run();
        Assert.Equal(5, Lost(w, Open, NightAt, beats: 5));
        Assert.Equal(0, w.On(1).Crew.Felt(0, Open));
    }

    [Fact]
    public void TheOpenSkyIsTheChillsDegreesColderAtNightAndASuitFeelsItPastItsLimit()
    {
        var w = Down();
        var crew = w.On(1).Crew;
        w.Do(new Enter(Open)).Do(new SetTime(NightAt)).Run();
        Assert.Equal((Strip.Temperatures[0] - Day.Chill, 8), (crew.TemperatureAt(Open), crew.Felt(0, Open)));
        w.Do(new SetTime(0)).Run();
        Assert.Equal((Strip.Temperatures[0], 0), (crew.TemperatureAt(Open), crew.Felt(0, Open)));
    }

    [Fact]
    public void UnderABackWallTheNightCostsNothingMore()
    {
        var w = Down();
        // The shelter cut into the cliff's foot, where heroes beam down: its back the rock.
        int shelter = w.On(1).Crew.Spawn;
        Assert.NotNull(w.On(1).Grid.BackAt(shelter));
        Assert.Equal((1, 0), (Lost(w, shelter, NightAt), FeltOf(w)));
        // A back wall put up over a cell of the open flat shelters it as well.
        var v = Down();
        v.On(1).Grid.Place(Open, Placement.BackWall());
        Assert.Equal((1, 0), (Lost(v, Open, NightAt), FeltOf(v)));
    }

    [Fact]
    public void WallsAndARoofWithNoBackWallsAreNoShelter()
    {
        var w = Down();
        var m = w.On(1);
        // A hut of walls a cell thick on the flat, three wide inside and six tall, a roof, no back walls.
        int left = 2 * 18, right = left + 4, floor = 2 * Ground, top = floor - 7;
        for (int y = top; y < floor; y++)
            for (int x = left; x <= right; x++)
                if (y == top || x == left || x == right) m.Grid.Place(Cell(x, y), Placement.Wall());
        int inside = Cell(left + 2, floor - 1);
        Assert.Null(m.Grid.BackAt(inside));
        Assert.Equal((5, NightFelt()), (Lost(w, inside, NightAt, beats: 5), FeltOf(w)));
    }

    [Fact]
    public void TheCapesDegreesLeaveLessOfTheNightsCold()
    {
        // 3 of 8 felt with the cape (2026-11-14-temperature-in-degrees): its body settles at 36.4°, its air paying thin air alone.
        const int beats = 10;
        Assert.Equal(3, NightFelt(Content.Cape.Warmth));
        var w = Down(caped: true);
        Assert.Equal((beats, NightFelt(Content.Cape.Warmth)), (Lost(w, Open, NightAt, beats), FeltOf(w)));
    }

    [Fact]
    public void AboardTheShipThereIsNoChill()
    {
        var w = new Scenario(Ship);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        Assert.Equal(0, w.M.Sky.Now.Chill);
        Assert.Equal(0, w.M.Crew.Felt(0, w.M.Crew.Spawn));
    }
}
