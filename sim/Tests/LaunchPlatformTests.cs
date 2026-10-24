using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;
using Pairs = System.Collections.Generic.IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The launch platform's first slice: a deck piece two cells by one, planned in creative mode only, a scrap each; pieces side
/// by side one run; its lamp green while its network has power enough. It does not open yet.
/// </summary>
public class LaunchPlatformTests
{
    /// <summary>Twelve cells across and ten down, a cell a character: a room with a floor to lay a deck along.</summary>
    private const string Room = """
        ############
        #..........#
        #..........#
        #..........#
        #..........#
        #..........#
        #..........#
        #..........#
        #..........#
        ############
        """;

    private const int Width = 12;
    private static int At(int x, int y) => y * Width + x;
    private static readonly Placement Platform = new("structure", Builds.Platform);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    private static void Finish(Scenario w, int id, int ticks = 200)
    {
        for (int i = 0; i < ticks && w.M.Building.Plans.Any(p => p.Id == id); i++) w.Do(new Work(id)).Run();
    }

    [Fact]
    public void PlansAPlatformOnlyInCreativeMode()
    {
        var w = new Scenario(Room, cells: true).GiveItems(0, ("scrap", 5));
        Assert.Equal("building.creativeOnly", w.M.Building.Problem(At(3, 8), Platform));
        w.Do(new PlacePlan(At(3, 8), Platform)).Run();
        Assert.Equal(["building.creativeOnly"], Reasons(w));
        Assert.Empty(w.M.Building.Plans);
        w.Do(new SetCreative(true)).Do(new PlacePlan(At(3, 8), Platform)).Run();
        Assert.Single(w.M.Building.Plans);
        Assert.Single(w.Refusals);
    }

    [Fact]
    public void IsAPieceTwoByOneCostingAScrapAndStoodOnAsAShutHatchIs()
    {
        var w = new Scenario(Room, cells: true).Do(new SetCreative(true));
        Assert.Equal((2, 1), Builds.PieceOf(Builds.Platform));
        Assert.Equal([("scrap", 1)], w.M.Building.CostFor(Platform));
        w.Do(new PlacePlan(At(3, 8), Platform)).Run();
        Finish(w, Assert.Single(w.M.Building.Plans).Id);
        Assert.Empty(w.Refusals);
        int code = Structures.Placed(Builds.Platform)!.Value;
        Assert.Equal([code, code], [w.M.Grid.StructureAt(At(3, 8)), w.M.Grid.StructureAt(At(4, 8))]);
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(At(5, 8)));
        Assert.Equal(Trait.Supports | Trait.BlocksFromBelow | Trait.Seals | Trait.BlocksLight, Structures.Of(code).Traits);
    }

    [Fact]
    public void JoinsPiecesSideBySideIntoOneRunAndNotAcrossAGapOrAnotherStructure()
    {
        var w = new Scenario(Room, cells: true);
        foreach (int x in new[] { 1, 3, 5, 8 }) w.Do(new Place(At(x, 8), Platform));
        w.Do(new Place(At(1, 6), new Placement("structure", "floor"))).Do(new Place(At(2, 6), new Placement("structure", "floor"))).Run();
        Assert.Empty(w.Refusals);
        Func<int, int> codeAt = w.M.Grid.StructureAt;
        Assert.Equal((At(1, 8), 6), StructureRuns.RunOf(At(4, 8), Width, codeAt));
        Assert.Equal((At(1, 8), 6), StructureRuns.RunOf(At(6, 8), Width, codeAt));
        Assert.Equal((At(8, 8), 2), StructureRuns.RunOf(At(9, 8), Width, codeAt));
    }

    // The lamp: PowerTests' room, a run of two pieces on its floor's row, cabled as each case says.

    private static readonly string[] Rows =
    [
        "PPP             ",
        "                ",
        "################",
        "#GG.GG.........#",
        "#GGVGG.__......#",
        "################",
    ];
    private static readonly string[] Under = ["", "", "", "", " ---"];
    private const int Wide = 2 * 16;
    private static int Char(int x, int y) => 2 * y * Wide + 2 * x;

    private static Scenario Cabled(Pairs cables) => new(Layout.Parse(Rows, Under, cables), doubled: true);

    /// <summary>Whether any cell of the run at character (7, 4) is fed: what its lamp shows.</summary>
    private static bool Lit(Scenario w)
    {
        var (first, length) = StructureRuns.RunOf(Char(7, 4), Wide, w.M.Grid.StructureAt);
        Assert.Equal(4, length);
        return Enumerable.Range(first, length).Any(w.M.Power.Feeds);
    }

    [Fact]
    public void LightsGreenOnANetworkWithPowerThatCoversWhatItsMachinesDraw()
    {
        var w = Cabled([((0, 0), (1, 3)), ((1, 3), (7, 4))]);
        var network = Assert.Single(w.M.Power.Networks);
        Assert.Equal((5, 4, 1000), (network.Supply, network.Demand, network.Satisfaction));
        Assert.True(Lit(w));
        Assert.True(w.M.Power.Feeds(Char(7, 4)));
        // Only the cell a cable ends on is on the network; the run is lit by any of its cells.
        Assert.False(w.M.Power.Feeds(Char(8, 4)));
    }

    [Fact]
    public void StaysRedUncabledShortOfPowerOrOnANetworkWithNoneAtAll()
    {
        Assert.False(Lit(Cabled([])));
        // Two generators drawing 8 on a panel making 5.
        Assert.False(Lit(Cabled([((0, 0), (1, 3)), ((1, 3), (4, 3)), ((1, 3), (7, 4))])));
        // A generator and no panel: drawn on, nothing given.
        Assert.False(Lit(Cabled([((1, 3), (7, 4))])));
        // A cable to the wall beside it: a network drawing nothing, satisfied, but with no power in it.
        var dead = Cabled([((7, 4), (6, 3))]);
        Assert.Equal(1000, Assert.Single(dead.M.Power.Networks).Satisfaction);
        Assert.False(Lit(dead));
    }
}
