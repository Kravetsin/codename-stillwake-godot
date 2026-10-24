using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;
using Xunit;
using Pairs = System.Collections.Generic.IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The launch platform's first slice: a deck piece two cells by one, of a deck drive, two plates and a glass; pieces side
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

    private static int Count(Scenario w, string item) => w.M.Items.Totals(0).Where(t => t.Item == item).Sum(t => t.Count);

    [Fact]
    public void IsAPieceTwoByOneOfADeckDriveTwoPlatesAndAGlassStoodOnAsAShutHatchIs()
    {
        var w = new Scenario(Room, cells: true).GiveItems(0, ("deckDrive", 2), ("plate", 4), ("glass", 2));
        Assert.Equal((2, 1), Builds.PieceOf(Builds.Platform));
        Assert.Equal([("deckDrive", 1), ("plate", 2), ("glass", 1)], w.M.Building.CostFor(Platform));
        w.Do(new PlacePlan(At(3, 8), Platform)).Run();
        Finish(w, Assert.Single(w.M.Building.Plans).Id);
        Assert.Empty(w.Refusals);
        Assert.Equal((1, 2, 1), (Count(w, "deckDrive"), Count(w, "plate"), Count(w, "glass")));
        int code = Structures.Placed(Builds.Platform)!.Value;
        Assert.Equal([code, code], [w.M.Grid.StructureAt(At(3, 8)), w.M.Grid.StructureAt(At(4, 8))]);
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(At(5, 8)));
        Assert.Equal(Trait.Supports | Trait.BlocksFromBelow | Trait.Seals | Trait.BlocksLight, Structures.Of(code).Traits);
    }

    [Fact]
    public void ItsDeckDriveIsMadeAtTheCircuitAssemblerOfTwoCircuitsARotorAndACoilAndAnalysedToOpenIt()
    {
        Assert.Equal([("circuit", 2), ("rotor", 1), ("coil", 1)], Crafts.Get("deckDrive").Cost);
        Assert.Equal(["assembler"], Stations.All.Where(s => s.Rows.Any(r => r.Contains("deckDrive"))).Select(s => s.Machine));
        // Its blueprint opens once the drive, the plate and the glass are analysed: after the assembler and the smelter.
        Assert.All(["deckDrive", "plate", "glass"], item => Assert.True(Stillwake.Sim.Research.Analysable.NeedsAnalysis(item)));
        Assert.DoesNotContain(Builds.Platform, Stillwake.Content.Research.StartKnown);
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

    // Opening: the same room, a run of two pieces laid in its floor with space under it.

    private static readonly string[] Deck =
    [
        "PPP             ",
        "                ",
        "################",
        "#GG.GG.........#",
        "#GGVGG.........#",
        "#######__#######",
        "                ",
    ];

    private static Scenario Decked(Pairs cables) => new(Layout.Parse(Deck, Under, cables), doubled: true);

    private static readonly Pairs ToDeck = [((0, 0), (7, 5))];
    private static int[] RunCells => [Char(7, 5), Char(7, 5) + 1, Char(8, 5), Char(8, 5) + 1];
    private static int[] Codes(Scenario w) => RunCells.Select(w.M.Grid.StructureAt).ToArray();
    private static readonly int Above = Char(9, 4);
    private static Stillwake.Sim.Rooms.Room RoomAbove(Scenario w) => w.M.Rooms.RoomById(w.M.Rooms.RoomAt(Above)!.Value)!;

    [Fact]
    public void OpensAPoweredRunWholeItsRoomOpenToSpaceAndShutsItAgain()
    {
        var w = Decked(ToDeck);
        Assert.Empty(RoomAbove(w).Openings);
        // Opened from its last cell, the whole run opens, and its room vents through it.
        w.Do(new SetPlatform(Char(8, 5) + 1, true)).Run();
        Assert.Empty(w.Refusals);
        Assert.All(Codes(w), c => Assert.Equal(Structure.PlatformOpen, c));
        Assert.NotEmpty(RoomAbove(w).Openings);
        Assert.Equal(Stillwake.Sim.Atmosphere.RoomStatus.Venting, w.M.Atmosphere.StatusOf(RoomAbove(w).Id));
        w.Do(new SetPlatform(Char(7, 5), false)).Run();
        Assert.All(Codes(w), c => Assert.Equal(Structure.PlatformClosed, c));
        Assert.Empty(RoomAbove(w).Openings);
    }

    [Fact]
    public void KeepsWhatLiesOnTheDeckWhereItLayOnceTheRunIsOpen()
    {
        var w = Decked(ToDeck).GiveItems(0, ("plate", 3));
        // Dropped on the deck's left piece, from the cell over it.
        w.Do(new Stillwake.Sim.Items.Drop(new Stillwake.Sim.Items.Place(0), 3, Char(7, 5) - Wide, 0) { From = 0 }).Run(60);
        var before = Assert.Single(w.M.Items.Floor);
        w.Do(new SetPlatform(Char(7, 5), true)).Run(600);
        Assert.Equal(Stillwake.Sim.Atmosphere.RoomStatus.Venting, w.M.Atmosphere.StatusOf(RoomAbove(w).Id));
        var after = Assert.Single(w.M.Items.Floor);
        Assert.Equal((before.Item, before.Count, before.X, before.Y), (after.Item, after.Count, after.X, after.Y));
    }

    [Fact]
    public void RefusesToOpenAnUnpoweredRunOrWhatIsNoPlatformAndADoorCommandOnIt()
    {
        var w = Decked([]);
        w.Do(new SetPlatform(Char(7, 5), true)).Do(new SetPlatform(Char(0, 5), true)).Do(new SetDoor(Char(7, 5), true)).Run();
        Assert.Equal(["landing.unpowered", "landing.noPlatform", "grid.noDoor"], Reasons(w));
        Assert.All(Codes(w), c => Assert.Equal(Structure.PlatformClosed, c));
        // Shutting needs no power: a run left open on a network gone dark can still be shut.
        Assert.Null(w.M.Landing.Problem(Char(7, 5), open: false));
    }

    [Fact]
    public void StaysADeckToStandOnOpenLettingAirAndLightThrough()
    {
        var open = Structures.Of(Structure.PlatformOpen);
        Assert.Equal(Trait.Supports | Trait.Opening, open.Traits);
        Assert.Equal(Structure.PlatformClosed, open.Toggle);
        Assert.Equal(Structure.PlatformClosed, Structures.Placed(Builds.Platform));
        // Every cell of a run joins its neighbours along its row, so the doorway is the run, not a piece.
        var w = Decked(ToDeck);
        Assert.Equal(Doorways.Left | Doorways.Right, Doorways.LinksOf(Char(7, 5) + 1, Wide, Wide * 14, w.M.Grid.StructureAt));
        Assert.Equal(Doorways.Left | Doorways.Right, Doorways.LinksOf(Char(8, 5), Wide, Wide * 14, w.M.Grid.StructureAt));
    }
}
