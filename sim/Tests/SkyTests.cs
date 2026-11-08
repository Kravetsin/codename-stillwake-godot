using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Sky;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>A planet's day and night (2026-11-07-night-chills), on the test strip: the hour, the night line, the chill outside.</summary>
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

    /// <summary>A beat of the chill: <c>Every</c> breaths, which on the strip is also thin air's beat.</summary>
    private static int Beat => Day.Every * Content.Crew.BreathEvery;

    /// <summary>The ship with the strip opened and hero 0 beamed down to it, wearing the cape when <paramref name="caped"/>.</summary>
    private static Scenario Down(bool caped = false)
    {
        var w = new Scenario(Ship);
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
        Assert.Equal(new SkyNow(NightAt - 1, false, 0), sky.Now);
        w.Run();
        Assert.Equal(new SkyNow(NightAt, true, Day.Chill), sky.Now);
        w.Do(new SetTime(Day.Length - 2)).Run();
        Assert.Equal(new SkyNow(Day.Length - 1, true, Day.Chill), sky.Now);
        w.Run();
        Assert.Equal(new SkyNow(0, false, 0), sky.Now);
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
    public void TheShipHasNoDaySetTimeIsRefusedThereAndItsSkyIsAllZeros()
    {
        var w = new Scenario(Ship);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        string before = w.World.Save();
        w.Do(new SetTime(NightAt)).Run();
        Assert.Equal("sky.noDay", Assert.Single(w.Refusals).Reason);
        Assert.Equal(default, w.M.Sky.Now);
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
    public void AtNightAHeroUnderTheOpenSkyPaysTheChillOnTopOfThinAir()
    {
        Assert.Equal(1, Lost(Down(), Open, 0));
        Assert.Equal(1 + Day.Chill, Lost(Down(), Open, NightAt));
        Assert.Equal(3 * (1 + Day.Chill), Lost(Down(), Open, NightAt, beats: 3));
        // Late in the night, a beat before dawn, it still bites.
        Assert.Equal(1 + Day.Chill, Lost(Down(), Open, Day.Length - 2 * Beat));
    }

    [Fact]
    public void ColdReportsTheChillOutAtNightAndNothingByDay()
    {
        var w = Down();
        var crew = w.On(1).Crew;
        w.Do(new Enter(Open)).Do(new SetTime(NightAt)).Run();
        Assert.Equal(Day.Chill, crew.Cold(0));
        w.Do(new SetTime(0)).Run();
        Assert.Equal(0, crew.Cold(0));
    }

    [Fact]
    public void UnderABackWallTheNightCostsNothingMore()
    {
        var w = Down();
        // The shelter cut into the cliff's foot, where heroes beam down: its back the rock.
        int shelter = w.On(1).Crew.Spawn;
        Assert.NotNull(w.On(1).Grid.BackAt(shelter));
        Assert.Equal(1, Lost(w, shelter, NightAt));
        // A back wall put up over a cell of the open flat shelters it as well.
        var v = Down();
        v.On(1).Grid.Place(Open, Placement.BackWall());
        Assert.Equal(1, Lost(v, Open, NightAt));
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
        Assert.Equal(1 + Day.Chill, Lost(w, inside, NightAt));
    }

    [Fact]
    public void TheCapeSparesItsShareOfTheChill()
    {
        const int beats = 10;
        int spared = beats * Day.Chill * Content.Cape.Warmth / 1000;
        Assert.True(spared > 0);
        Assert.Equal(beats * (1 + Day.Chill) - spared, Lost(Down(caped: true), Open, NightAt, beats));
    }

    [Fact]
    public void AboardTheShipThereIsNoChill()
    {
        var w = new Scenario(Ship);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        Assert.Equal(0, w.M.Sky.Now.Chill);
        Assert.Equal(0, w.M.Crew.Cold(0));
    }
}
