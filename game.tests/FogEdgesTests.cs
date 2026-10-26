using Stillwake.Content;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Xunit;
using Hero = Stillwake.Game.Features.Crew.Hero;

namespace Stillwake.Game.Tests;

/// <summary>
/// A planet's fog at its sides as the client has it (fog-edges): its bands read from the grid, the hero gliding through them
/// at the height it came in, moved across a seam to the other band at the drop height, and out of it falling to the ground.
/// </summary>
public class FogEdgesTests
{
    private const int Unit = Hero.Unit;
    private static readonly HeroInput Still = new(0, 0, false), Left = Still with { X = -1 }, Up = Still with { Y = -1 },
        Down = Still with { Y = 1 }, Jump = Still with { Jump = true, JumpHeld = true };

    /// <summary>The small planet, opened from a ship: its registry.</summary>
    private static readonly Lazy<Registry> Small = new(() =>
    {
        var world = new TestWorld("""
            ......
            .S....
            ######
            """).Step(new OpenSurface(PlanetTypes.Small.Id));
        return Registry.On(world.World, 1);
    });

    private static FogEdges Edges => FogEdges.Of(Small.Value.Grid, Small.Value.Rules)!;

    private static Ship ShipOf(Registry m) => Ship.Of(m.Grid, m.Rooms, rules: m.Rules);

    /// <summary>The top of the small planet's ground in a column, as a moved hero's arrival reads it.</summary>
    private static int Top(int column) => Hero.TopOf(ShipOf(Small.Value), column);

    /// <summary>A ship, with no fog.</summary>
    private static readonly TestWorld Aboard = new("""
        ......
        .S....
        ######
        """);

    /// <summary>A band column's ground line as the grid has it: its first row with a back wall.</summary>
    private static int LineOf(Registry m, int x) =>
        Enumerable.Range(0, m.Grid.Height).First(y => m.Grid.BackLayer[y * m.Grid.Width + x] != Stillwake.Sim.Grid.Back.None);

    [Fact]
    public void APlanetsBandsAreReadFromItsZoneWithTheirSeamsPeriodAndLinesBesideTheZone()
    {
        var m = Small.Value;
        var g = PlanetTypes.Small.Ground;
        var e = Edges;
        Assert.Equal((g.Width, g.Fog, g.FogDrop), (e.Width, e.Fog, e.Drop));
        Assert.Equal((4 * Unit, (g.Width - 4) * Unit, (g.Width - 8) * Unit), (e.LeftSeam, e.RightSeam, e.Period));
        Assert.Equal((LineOf(m, g.Fog - 1), LineOf(m, g.Width - g.Fog)), (e.LeftLine, e.RightLine));
        Assert.True(e.Outside(g.Fog * Unit - 1) && !e.Outside(g.Fog * Unit) && !e.Outside((g.Width - g.Fog) * Unit - 1) && e.Outside((g.Width - g.Fog) * Unit));
    }

    [Fact]
    public void NoBandAboardOnATestMapWithAZoneNorOnAPlanetWhoseZoneIsTheWholeGrid()
    {
        var aboard = new TestWorld("""
            ......
            .S....
            ######
            """);
        Assert.Null(FogEdges.Of(aboard.M.Grid, aboard.M.Rules));
        var zoned = new TestWorld(Layout.Parse(["..........", ".S........", "##########"]) with { Zone = new Stillwake.Sim.Box(2, 0, 7, 2) });
        Assert.Null(FogEdges.Of(zoned.M.Grid, zoned.M.Rules));
        // Saved before the bands: a planet's grid whose zone is every column.
        var whole = new TestWorld(Layout.Parse(["..........", ".S........", "##########"]) with { Kind = PlanetTypes.Small.Id });
        Assert.Null(FogEdges.Of(whole.M.Grid, whole.M.Rules));
    }

    [Fact]
    public void OnlyCrossingASeamOutwardShiftsByThePeriodToTheOtherBandAtTheDropHeight()
    {
        var e = Edges;
        Assert.Equal(e.Period, e.Shift(e.LeftSeam, e.LeftSeam - 1));
        Assert.Equal(-e.Period, e.Shift(e.RightSeam, e.RightSeam + 1));
        // Standing on a seam, turning back before it, or crossing it inward: no move.
        Assert.Equal(0, e.Shift(e.LeftSeam, e.LeftSeam));
        Assert.Equal(0, e.Shift(e.LeftSeam + 300, e.LeftSeam + 100));
        Assert.Equal(0, e.Shift(e.LeftSeam - 1, e.LeftSeam + 200));
        Assert.Equal(0, e.Shift(e.RightSeam + 1, e.RightSeam - 200));
        // Over the highest ground it will glide on over: the band's line beside the zone, or a tower past it as built.
        Assert.Equal((e.RightLine - e.Drop) * Unit, e.Arrive(e.Period, _ => 1000));
        Assert.Equal((e.LeftLine - e.Drop) * Unit, e.Arrive(-e.Period, _ => 1000));
        int tower = e.RightLine - 4;
        Assert.Equal((tower - e.Drop) * Unit, e.Arrive(e.Period, c => c == e.Width - 1 - e.Fog - e.Out + 1 ? tower : 1000));
        Assert.Equal(Stillwake.Content.Hero.Height, e.Arrive(e.Period, _ => 0));
    }

    [Fact]
    public void ACrossingAndATurnBackAtOnceBringTheHeroBackToTheFirstSeamAtTheDropHeight()
    {
        var e = Edges;
        var at = new Hero(e.LeftSeam, 30 * Unit, -240, 0, false, -1);
        var over = FogMove.Of(ShipOf(Small.Value), at, at with { X = e.LeftSeam - 100 }, seated: false)!.Value;
        Assert.Equal((e.RightSeam - 100, e.Arrive(e.Period, Top), -240, 0), (over.X, over.Y, over.Vx, over.Vy));
        var back = FogMove.Of(ShipOf(Small.Value), over, over with { X = e.RightSeam + 50, Vx = 240 }, seated: false)!.Value;
        Assert.Equal((e.LeftSeam + 50, e.Arrive(-e.Period, Top)), (back.X, back.Y));
        Assert.Equal(PlanetTypes.Small.Ground.Fog / 2, back.X / Unit);
    }

    [Fact]
    public void ASeatedHeroOrOneAboardIsNeverMoved()
    {
        var e = Edges;
        var at = new Hero(e.LeftSeam, 30 * Unit, -240, 0, false, -1);
        Assert.Null(FogMove.Of(ShipOf(Small.Value), at, at with { X = e.LeftSeam - 100 }, seated: true));
        Assert.Null(FogMove.Of(Ship.Of(Aboard.M.Grid, Aboard.M.Rooms), at, at with { X = e.LeftSeam - 100 }, seated: false));
    }

    [Fact]
    public void InABandTheHeroGlidesAtItsHeightUpDownAndJumpDoingNothingAndTheKeysPacingItAcross()
    {
        var m = Small.Value;
        var ship = ShipOf(m);
        var e = Edges;
        // In the left band, over its seam, ten rows over its line and forty under it: weightless both.
        foreach (int y in new[] { (e.LeftLine - 10) * Unit, (e.LeftLine + 40) * Unit })
            foreach (var keys in new[] { Still, Up, Down, Jump })
            {
                var hero = new Hero(6 * Unit, y, 0, 0, false, 1);
                for (int i = 0; i < 60; i++) hero = hero.Step(keys, ship);
                Assert.Equal((6 * Unit, y, 0), (hero.X, hero.Y, hero.Vy));
            }
        var walking = new Hero(7 * Unit, (e.LeftLine - 10) * Unit, 0, 0, false, -1);
        for (int i = 0; i < 12; i++) walking = walking.Step(Left, ship);
        Assert.Equal(-Stillwake.Content.Hero.Walk, walking.Vx);
        Assert.Equal((e.LeftLine - 10) * Unit, walking.Y);
        Assert.True(walking.X < 5 * Unit, $"glided only to {walking.X}");
    }

    [Fact]
    public void InTheZoneTheSameJumpRisesEvenInTheColumnsPastABandForAHeroThatWalkedThere()
    {
        var m = Small.Value;
        var ship = ShipOf(m);
        int x = PlanetTypes.Small.Ground.Fog + PlanetTypes.Small.Ground.FogOut / 2, top = Enumerable.Range(0, m.Grid.Height).First(y => m.Grid.StructureAt(y * m.Grid.Width + x) != Stillwake.Content.Structure.Empty);
        var hero = new Hero(x * Unit + Unit / 2, top * Unit, 0, 0, false, 1).Step(Still, ship);
        Assert.True(hero.Step(Jump, ship).Vy < 0);
    }

    [Fact]
    public void ArrivingTheHeroGlidesOutOfTheBandAndFallsToTheGroundAndStands()
    {
        var m = Small.Value;
        var ship = ShipOf(m);
        var e = Edges;
        var hero = new Hero(e.RightSeam - 100, e.Arrive(e.Period, Top), -Stillwake.Content.Hero.Walk, 0, false, -1, Gliding: true);
        bool fell = false;
        for (int i = 0; i < 150; i++)
        {
            hero = hero.Step(Left, ship);
            // It glides on through the band and the columns past it, and only then falls, out of the thick fog.
            if (hero.Vy > 0 && !fell) Assert.False(e.Carries(hero.X), $"it fell at {hero.X / Unit}, in the thick fog");
            fell |= hero.Vy > 0;
        }
        Assert.True(fell, "it never fell");
        Assert.False(e.Outside(hero.X), "it never left the band");
        // Let go, it comes to stand on the ground: no fall left, its feet kept.
        for (int i = 0; i < 120; i++) hero = hero.Step(Still, ship);
        Assert.Equal((0, hero.Y), (hero.Vy, hero.Step(Still, ship).Y));
    }
}
