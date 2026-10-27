using Stillwake.Content;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Power;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;
using Xunit;
using Hero = Stillwake.Game.Features.Crew.Hero;

namespace Stillwake.Game.Tests;

/// <summary>The launch platform in the client: its card, its runs and their lamps.</summary>
public class LaunchPlatformTests
{
    private static readonly Tool Card = Tools.All.Single(t => t is PlaceTool { Placement.Kind: Builds.Platform });

    [Fact]
    public void PutsThePlatformsCardOnTheShipTab() => Assert.Contains(Card, Tools.Tabs.Single(t => t.Id == MenuTabs.Ship).Tools);

    /// <summary>A panel in space above a room: a run of two pieces cabled to the panel, a piece alone uncabled.</summary>
    private static readonly string[] Rows =
    [
        "PPP             ",
        "                ",
        "################",
        "#..............#",
        "#..__....._....#",
        "################",
    ];

    [Fact]
    public void FindsEachRunAndLightsOnlyTheOneOnANetworkWithPower()
    {
        var w = new TestWorld(Layout.Parse(Rows, [], [((0, 0), (3, 4))]).Doubled());
        int width = w.M.Grid.Width;
        int Char(int x, int y) => 2 * y * width + 2 * x;
        var runs = PlatformRuns.Of(w.M.Grid);
        Assert.Equal([(Char(3, 4), 4), (Char(10, 4), 2)], runs);
        Assert.Equal([true, false], runs.Select(r => PlatformRuns.Lit(w.M.Power, r)));
    }

    /// <summary>
    /// The runs followed through the grid's log are those found anew over the whole grid, as pieces are built and taken down
    /// beside each other, walls and floors among them, and runs open and shut.
    /// </summary>
    [Fact]
    public void RunsFollowedThroughTheLogAreThoseFoundAnew()
    {
        var rng = new Random(3);
        Placement[] placements = [new("structure", Builds.Platform), new("structure", Builds.Platform), Placement.Wall(), new("structure", "floor")];
        for (int world = 0; world < 20; world++)
        {
            int w = rng.Next(4, 40), h = rng.Next(2, 12);
            var m = new Registry();
            Stillwake.Sim.World.Create(1, Layout.Parse(Enumerable.Repeat(new string(' ', w), h).ToList()), m.All);
            var known = new KnownPlatforms();
            known.Follow(m.Grid);
            for (int step = 0; step < 30; step++)
            {
                for (int k = rng.Next(1, 4); k > 0; k--)
                {
                    int cell = rng.Next(w * h);
                    var p = placements[rng.Next(placements.Length)];
                    if (rng.Next(3) == 0) m.Grid.Clear(cell, "structure");
                    else if (m.Grid.PlacementProblem(cell, p) == null) m.Grid.Place(cell, p);
                }
                var before = known.Runs.ToList();
                bool changed = known.Follow(m.Grid);
                var anew = PlatformRuns.Of(m.Grid);
                Assert.Equal(anew, known.Runs);
                Assert.Equal(!before.SequenceEqual(anew), changed);
                Assert.False(known.Follow(m.Grid));
            }
        }
    }

    [Fact]
    public void PlansAPieceWholeAsItIsBuiltNotADoorsQuarterACell()
    {
        Assert.False(Tools.LaysDoor(new Placement("structure", Builds.Platform)));
        Assert.True(Tools.LaysDoor(new Placement("structure", "door")));
        Assert.True(Tools.LaysDoor(new Placement("structure", Builds.Hatch)));
    }

    [Fact]
    public void KnowsAPlatformPieceForTheShaftItKeepsAndNothingElse()
    {
        Assert.True(Tools.IsPlatform(new Placement("structure", Builds.Platform)));
        Assert.False(Tools.IsPlatform(new Placement("structure", Builds.Hatch)));
        Assert.False(Tools.IsPlatform(Placement.Wall("iron")));
        Assert.False(Tools.IsPlatform(new Placement("machine", "dropPod")));
    }

    [Fact]
    public void RunsEveryCableToARunIntoItsSocketAtItsLeftEnd()
    {
        var w = new TestWorld(Layout.Parse(Rows, [], [((0, 0), (4, 4))]).Doubled());
        int width = w.M.Grid.Width;
        int Char(int x, int y) => 2 * y * width + 2 * x;
        var plug = Structures.Of(Structure.PlatformClosed).Plug!.Value;
        Assert.Equal(Char(3, 4), w.M.Power.NodeAt(Char(4, 4)));
        Assert.Equal((3 * 2 + plug.X / 8.0, 4 * 2 + plug.Y / 8.0), Stillwake.Game.Render.CableShape.EndOf(w.M.Grid, Char(3, 4)));
        Assert.True(PlatformRuns.Lit(w.M.Power, PlatformRuns.Of(w.M.Grid)[0]));
    }

    [Fact]
    public void OpensALitRunOnEAndSaysWhyADarkOneStaysShut()
    {
        var w = new TestWorld(Layout.Parse(Rows, [], [((0, 0), (3, 4))]).Doubled());
        int width = w.M.Grid.Width;
        int Char(int x, int y) => 2 * y * width + 2 * x;
        Aim? AimAt(int x, int y) =>
            Aiming.AimAt(w.M, 0, new Hero(x * HeroTests.Block - Hero.Unit, 4 * HeroTests.Block - Hero.Unit + Hero.MiddleUp, 0, 0, false, 1), (2 * x + 0.5, 2 * y + 0.5));
        var lit = AimAt(4, 4);
        Assert.Equal(new Use(new SetPlatform(Char(4, 4), true), null), lit?.Press);
        Assert.Equal(new Prompt("use.openPlatform", null), Prompts.Of(w.M, lit));
        var dark = AimAt(10, 4);
        Assert.Equal(new Use(new SetPlatform(Char(10, 4), true), "landing.unpowered"), dark?.Press);
        Assert.Equal(new Prompt("use.openPlatform", "landing.unpowered"), Prompts.Of(w.M, dark));
        // Opened, the whole run shuts on E, the lamps still found on it.
        w.Step(lit!.Press!.Command);
        Assert.All(Enumerable.Range(Char(3, 4), 4), c => Assert.Equal(Structure.PlatformOpen, w.M.Grid.StructureAt(c)));
        Assert.Equal(new Prompt("use.closePlatform", null), Prompts.Of(w.M, AimAt(3, 4)));
        Assert.Equal([(Char(3, 4), 4), (Char(10, 4), 2)], PlatformRuns.Of(w.M.Grid));
    }
}
