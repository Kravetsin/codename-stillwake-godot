using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// A tool aimed at the ground (the fan of rays): the axe or the drill cast from the hero's shoulder toward the pointer, what it
/// would strike the sim's own query's, and what of it glints.
/// </summary>
public class FanAimTests
{
    private const int Unit = Hero.Unit;
    private static readonly string Axe = Spec.Weapons.Axe.Item, Drill = Spec.Weapons.Drill.Item;
    private const int W = 24;

    /// <summary>A cave in soil a character a cell: open from column 1 to 22, rows 1 to 8, over stone and bedrock.</summary>
    private static Layout CaveLayout => Layout.Parse("""
        ########################
        #......................#
        #......................#
        #......................#
        #......................#
        #......................#
        #......................#
        #......................#
        #......................#
        ########################
        ########################
        ########################
        """).WithMaterials("""
        ssssssssssssssssssssssss
        s......................s
        s......................s
        s......................s
        s......................s
        s......................s
        s......................s
        s......................s
        s......................s
        ssssssssssssssssssssssss
        tttttttttttttttttttttttt
        bbbbbbbbbbbbbbbbbbbbbbbb
        """);

    private static TestWorld Cave() => new(CaveLayout);

    private static int At(int x, int y) => y * W + x;

    /// <summary>A hero standing on the cave's floor in column <paramref name="x"/>, its feet on row 9.</summary>
    private static Hero Standing(int x) => new(x * Unit + Unit / 2, 9 * Unit, 0, 0, false, 1);

    private static (double X, double Y) Centre(int x, int y) => (x + 0.5, y + 0.5);

    [Fact]
    public void TheAxeAimsItsFanAtTheWallAndStrikesWhatTheSimSaysAimedAtTheCellNearestThePointer()
    {
        var m = Cave().M;
        var hero = Standing(20);
        var aim = Aiming.AimAt(m, 0, hero, Centre(23, 6), axe: true);
        var hand = Aiming.HandOf(hero);
        var fan = aim!.Fan!;
        Assert.Equal(new Hew(Axe, hand.X, hand.Y, Aiming.HeadingOf(hand, Aiming.PointOf(Centre(23, 6)))), fan.Ground);
        Assert.Equal(m.Building.Struck(Axe, hand.X, hand.Y, fan.Ground.Heading).Cells, fan.Hewn.Cells);
        Assert.Contains(At(23, 6), fan.Hewn.Cells);
        Assert.Equal(new Aim(At(23, 6), true, new Use(fan.Ground, null), Fan: fan), aim);
        Assert.Equal(new Prompt("use.dig", null), Prompts.Of(m, aim));
        // A press never digs: the swing does.
        Assert.Empty(Aiming.UsesOf(aim, pressed: true, held: false, tick: 1, workedOnTick: -1).Commands);
        // A drill with no battery is refused flat before its run is asked.
        Assert.Equal("items.flat", Aiming.AimAt(m, 0, hero, Centre(23, 6), drill: Drill)?.Press?.Problem);
        // Bare hands aim no fan.
        Assert.Null(Aiming.AimAt(m, 0, hero, Centre(23, 6))?.Fan);
    }

    [Fact]
    public void AFanOutOfRangeOfEverythingAimsAtNothingButStillShowsItsReach()
    {
        var m = Cave().M;
        // Up from the middle of the cave: the ceiling is farther than the axe reaches.
        var aim = Aiming.AimAt(m, 0, Standing(11), Centre(11, 1), axe: true);
        Assert.NotNull(aim?.Fan);
        Assert.True(aim!.Fan!.IsEmpty);
        Assert.Null(aim.Press);
        Assert.Null(Prompts.Of(m, aim));
    }

    [Fact]
    public void AFanThroughAPlantStrikesItAndAimsAtIt()
    {
        var m = new TestWorld(CaveLayout with { Flora = [(At(13, 8), "bellTeal")] }).M;
        var plant = m.Building.PlantAt(At(13, 8));
        var aim = Aiming.AimAt(m, 0, Standing(11), Centre(13, 8), axe: true);
        Assert.Contains(plant, aim!.Fan!.Hewn.Plants);
        Assert.Equal(new Prompt("use.dig", null), Prompts.Of(m, aim));
    }

    [Fact]
    public void AStrikeIsHeardAtTheCellItStrikesNearestThePointer()
    {
        var m = Cave().M;
        var fan = Aiming.FanOf(m, Standing(20), Aiming.PointOf(Centre(23, 6)), Axe);
        foreach (int cell in fan.Hewn.Cells)
            Assert.Equal(cell, Aiming.LeadOf(fan, new Point(cell % W * Unit + 500, cell / W * Unit + 500), W));
    }

    [Fact]
    public void TheFacesThatGlintAreTheStruckWallsSidesOpenToTheAirAndTurnedToTheHand()
    {
        var m = Cave().M;
        var hand = new Point(20_500, 5_500);
        // The right wall struck from its left: its left faces glint, never its top, bottom or right, walls all.
        var hewn = new Hewn([At(23, 5), At(23, 6)], []);
        Assert.Equal([new Glint(23, 5, 23, 6), new Glint(23, 6, 23, 7)], FanFaces.Of(m.Grid, hewn, hand, back: false));
        // The floor's corner cell struck from above and left: its top and its left face, each open.
        var corner = new Hewn([At(22, 9)], []);
        Assert.Equal([new Glint(22, 9, 23, 9)], FanFaces.Of(m.Grid, corner, hand, back: false));
        // A back wall and a plant glint over their whole cell.
        Assert.Equal([new Glint(5, 5, 6, 6)], FanFaces.Of(m.Grid, new Hewn([At(5, 5)], []), hand, back: true));
        Assert.Equal([new Glint(4, 8, 5, 9)], FanFaces.Of(m.Grid, new Hewn([], [new Plant(At(4, 8), "bellTeal")]), hand, back: false));
    }

    [Fact]
    public void TheAxesReachIsAnArcOverItsFanAndTheDrillsADottedLine()
    {
        var hand = new Point(10_000, 5_000);
        var axe = Spec.Weapons.Axe;
        var (x0, y0) = FanFaces.ArcAt(axe, hand, 0, 0);
        var (x1, y1) = FanFaces.ArcAt(axe, hand, 0, 1);
        // A quarter turn wide, its range from the hand, either side of the heading.
        Assert.Equal(10 + 3.5 * System.Math.Cos(System.Math.PI / 4), x0, 6);
        Assert.Equal(5 - 3.5 * System.Math.Sin(System.Math.PI / 4), y0, 6);
        Assert.Equal((x0, 10 - y0), (x1, y1 - 0), new TupleTolerance());
        var dots = FanFaces.DotsOf(Spec.Weapons.Drill, hand, 0);
        Assert.Equal((int)(5.5 / FanFaces.DotEvery), dots.Count);
        Assert.Equal((10 + FanFaces.DotEvery, 5.0), dots[0], new TupleTolerance());
    }

    private sealed class TupleTolerance : IEqualityComparer<(double, double)>
    {
        public bool Equals((double, double) a, (double, double) b) => System.Math.Abs(a.Item1 - b.Item1) < 1e-6 && System.Math.Abs(a.Item2 - b.Item2) < 1e-6;
        public int GetHashCode((double, double) t) => 0;
    }
}
