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
    /// <summary>
    /// Twelve cells across and ten down, a cell a character: a room to lay a deck along its last row inside, open to space under it,
    /// so every piece's shaft is clear to the edge.
    /// </summary>
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
        #          #
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
    public void IsAPieceTwoByOneOfADeckDrivePlatesAndGlassStoodOnAsAShutHatchIs()
    {
        var cost = Builds.Get(Builds.Platform).Cost;
        var w = new Scenario(Room, cells: true).GiveItems(0, [.. cost.Select(c => (c.Item, 2 * c.Count))]);
        Assert.Equal((2, 1), Builds.PieceOf(Builds.Platform));
        Assert.Equal(["deckDrive", "plate", "glass"], cost.Select(c => c.Item));
        Assert.Equal(cost, w.M.Building.CostFor(Platform));
        w.Do(new PlacePlan(At(3, 8), Platform)).Run();
        Finish(w, Assert.Single(w.M.Building.Plans).Id);
        Assert.Empty(w.Refusals);
        Assert.All(cost, c => Assert.Equal(c.Count, Count(w, c.Item)));
        int code = Structures.Placed(Builds.Platform)!.Value;
        Assert.Equal([code, code], [w.M.Grid.StructureAt(At(3, 8)), w.M.Grid.StructureAt(At(4, 8))]);
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(At(5, 8)));
        Assert.Equal(Trait.Supports | Trait.BlocksFromBelow | Trait.Seals | Trait.BlocksLight, Structures.Of(code).Traits);
    }

    [Fact]
    public void ItsDeckDriveIsMadeAtTheCircuitAssemblerOfCircuitsRotorsAndCoils()
    {
        Assert.Equal(["circuit", "rotor", "coil"], Crafts.Get("deckDrive").Cost.Select(c => c.Item));
        Assert.Equal(["assembler"], Stations.All.Where(s => s.Rows.Any(r => r.Contains("deckDrive"))).Select(s => s.Machine));
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

    /// <summary>Whether the run at character (7, 4) is fed at its plug: what its lamp shows.</summary>
    private static bool Lit(Scenario w)
    {
        Assert.Equal((Char(7, 4), 4), Stillwake.Sim.Landing.LandingModule.RunOf(w.M.Grid, Char(8, 4)));
        return w.M.Landing.Powered(Char(8, 4));
    }

    [Fact]
    public void LightsGreenOnANetworkWithPowerThatCoversWhatItsMachinesDraw()
    {
        var w = Cabled([((0, 0), (1, 3)), ((1, 3), (7, 4))]);
        var network = Assert.Single(w.M.Power.Networks);
        Assert.Equal((5, 4, 1000), (network.Supply, network.Demand, network.Satisfaction));
        Assert.True(Lit(w));
        Assert.True(w.M.Power.Feeds(Char(7, 4)));
        // Only its plug, at its left end, is on the network.
        Assert.False(w.M.Power.Feeds(Char(8, 4)));
    }

    [Fact]
    public void PlugsEveryCableToARunInAtItsLeftEndWhereverItIsLaid()
    {
        // A map's cable to the run's right piece, and one laid to its last cell.
        var w = Cabled([((0, 0), (1, 3)), ((1, 3), (8, 4))]);
        Assert.Equal(Char(7, 4), w.M.Power.NodeAt(Char(8, 4) + 1));
        Assert.Contains(w.M.Power.Cables, c => c.A == Char(7, 4) || c.B == Char(7, 4));
        Assert.True(Lit(w));
        w.Do(new Stillwake.Sim.Items.SetCreative(true)).Do(new Stillwake.Sim.Power.Lay(Char(4, 3), Char(8, 4) + 1) { From = 0 }).Run();
        Assert.Empty(Reasons(w));
        Assert.Contains(w.M.Power.Cables, c => (c.A, c.B) == (Char(4, 3), Char(7, 4)) || (c.A, c.B) == (Char(7, 4), Char(4, 3)));
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
    private const int Space = Stillwake.Sim.Rooms.RoomsModule.Space;

    [Fact]
    public void OpensAPoweredRunWholeIntoAHoleTheRoomTurnedToSpaceAndShutsItAgain()
    {
        var w = Decked(ToDeck);
        Assert.NotEqual(Space, w.M.Rooms.RoomAt(Above));
        Assert.False(w.M.Rooms.Afloat(Above));
        // Opened from its last cell, the whole run opens: space comes in through it, the room's air and gravity gone.
        w.Do(new SetPlatform(Char(8, 5) + 1, true)).Run();
        Assert.Empty(w.Refusals);
        Assert.All(Codes(w), c => Assert.Equal(Structure.PlatformOpen, c));
        Assert.Equal(Space, w.M.Rooms.RoomAt(Above));
        Assert.True(w.M.Rooms.Afloat(Above));
        // Shut, the room is closed again, an airless room with gravity.
        w.Do(new SetPlatform(Char(7, 5), false)).Run();
        Assert.All(Codes(w), c => Assert.Equal(Structure.PlatformClosed, c));
        Assert.NotEqual(Space, w.M.Rooms.RoomAt(Above));
        Assert.False(w.M.Rooms.Afloat(Above));
    }

    [Fact]
    public void KeepsWhatLiesOnTheDeckWhereItLayOnceTheRunIsOpen()
    {
        var w = Decked(ToDeck).GiveItems(0, ("plate", 3));
        // Dropped on the deck's left piece, from the cell over it.
        w.Do(new Stillwake.Sim.Items.Drop(new Stillwake.Sim.Items.Place(0), 3, Char(7, 5) - Wide, 0) { From = 0 }).Run(60);
        var before = Assert.Single(w.M.Items.Floor);
        w.Do(new SetPlatform(Char(7, 5), true)).Run(600);
        Assert.True(w.M.Rooms.Afloat(before.Cell));
        // Afloat before a back wall, inside the ship, it does not drift back as a stack out in space does.
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
    public void IsAHoleOpenThatNothingStopsOrStandsOnAndNoRoomEndsAt()
    {
        var open = Structures.Of(Structure.PlatformOpen);
        Assert.Equal(Trait.None, open.Traits);
        Assert.False(Structures.Solid(Structure.PlatformOpen));
        Assert.Equal(Structure.PlatformClosed, open.Toggle);
        Assert.Equal(Structure.PlatformClosed, Structures.Placed(Builds.Platform));
        Assert.True(Doorways.IsOpen(Structure.PlatformOpen));
        Assert.True(Doorways.IsClosed(Structure.PlatformClosed));
        // Every cell of a run joins its neighbours along its row, so the doorway is the run, not a piece.
        var w = Decked(ToDeck);
        Assert.Equal(Doorways.Left | Doorways.Right, Doorways.LinksOf(Char(7, 5) + 1, Wide, Wide * 14, w.M.Grid.StructureAt));
        Assert.Equal(Doorways.Left | Doorways.Right, Doorways.LinksOf(Char(8, 5), Wide, Wide * 14, w.M.Grid.StructureAt));
    }
}
