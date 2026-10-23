using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Pieces (0104): a ladder two by two and a door two by six, a hatch six by two, each placed, paid, built and taken apart as one
/// thing, the size and the price of the 16 px cell or doorway it was; a doorway two cells thick opens and shuts whole.
/// </summary>
public class PieceTests
{
    /// <summary>Twelve cells across and ten down, a cell a character: a room with room for a door or a hatch.</summary>
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
    private static readonly Placement Ladder = new("structure", "ladder"), Door = new("structure", "door"), Hatch = new("structure", Builds.Hatch);

    private static Scenario Stocked()
    {
        var w = new Scenario(Room, cells: true);
        w.GiveItems(0, ("scrap", 10), ("plate", 20), ("gear", 10));
        return w;
    }

    private static int Count(Scenario w, string item) => w.M.Items.Totals(0).Where(t => t.Item == item).Sum(t => t.Count);

    /// <summary>Works plan <paramref name="id"/> a tick at a time till it is gone, at most <paramref name="ticks"/> ticks.</summary>
    private static void Finish(Scenario w, int id, int ticks = 200)
    {
        for (int i = 0; i < ticks && w.M.Building.Plans.Any(p => p.Id == id); i++) w.Do(new Work(id)).Run();
    }

    private static IEnumerable<int> Box(int x, int y, int across, int tall) =>
        from dy in Enumerable.Range(0, tall) from dx in Enumerable.Range(0, across) select At(x + dx, y + dy);

    [Fact]
    public void ALadderIsOnePieceTwoByTwoCostingAScrapAndBuiltAsLongAsA16PxCellWas()
    {
        var w = Stocked();
        w.Do(new PlacePlan(At(3, 2), Ladder)).Run();
        var plan = Assert.Single(w.M.Building.Plans);
        Assert.All(Box(3, 2, 2, 2), c => Assert.Equal(plan.Id, w.M.Building.PlansAt(c).Single().Id));
        Assert.Empty(w.M.Building.PlansAt(At(5, 2)));
        Assert.Equal([("scrap", 1)], w.M.Building.CostFor(Ladder));
        Assert.Equal(Builds.Get("ladder").BuildTicks, w.M.Building.TicksFor(plan.Id));
        Finish(w, plan.Id);
        Assert.Empty(w.Refusals);
        Assert.All(Box(3, 2, 2, 2), c => Assert.Equal(Structure.Ladder, w.M.Grid.StructureAt(c)));
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(At(5, 2)));
        Assert.Equal(9, Count(w, "scrap"));
    }

    [Fact]
    public void APieceIsRefusedWhereAnyOfItsCellsIsTakenOrOffTheGrid()
    {
        var w = Stocked();
        Assert.Equal("grid.occupied", w.M.Building.Problem(At(10, 2), Ladder));
        Assert.Equal("grid.occupied", w.M.Building.Problem(At(2, 4), Door));
        Assert.Equal("grid.occupied", w.M.Building.Problem(At(6, 2), Hatch));
        Assert.Equal("grid.outOfBounds", w.M.Building.Problem(At(11, 9), Ladder));
        Assert.Null(w.M.Building.Problem(At(1, 1), Door));
        w.Do(new PlacePlan(At(1, 1), Door)).Run();
        // The piece claims its cells: a ladder over its lower half is planned there already.
        Assert.Equal("building.planned", w.M.Building.Problem(At(2, 5), Ladder));
    }

    [Fact]
    public void ALadderPieceComesApartWholeFromAMarkOnAnyOfItsCellsTheOneAboveItStanding()
    {
        var w = Stocked();
        w.Do(new Place(At(3, 2), Ladder)).Do(new Place(At(3, 4), Ladder)).Run();
        // Marked by its lower right cell, the lower piece is kept at its top-left.
        w.Do(new Deconstruct(At(4, 5), "structure")).Run();
        var mark = Assert.Single(w.M.Building.Plans);
        Assert.Equal(At(3, 4), mark.Cell);
        Assert.Equal("building.planned", w.M.Building.Problem(At(3, 5), "structure"));
        Assert.Equal([("scrap", 1)], w.M.Building.RefundOf(mark.Id));
        Finish(w, mark.Id);
        Assert.All(Box(3, 4, 2, 2), c => Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(c)));
        Assert.All(Box(3, 2, 2, 2), c => Assert.Equal(Structure.Ladder, w.M.Grid.StructureAt(c)));
        Assert.Equal(11, Count(w, "scrap"));
    }

    [Fact]
    public void ADoorIsOnePieceTwoBySixCostingWhatThreeDoorCellsDidAndBuiltAsLong()
    {
        var w = Stocked();
        w.Do(new PlacePlan(At(5, 2), Door)).Run();
        var plan = Assert.Single(w.M.Building.Plans);
        // What a doorway of three 16 px cells cost and took (0104).
        Assert.Equal([("plate", 6), ("gear", 3)], w.M.Building.CostOf(plan.Id));
        Assert.Equal(60, w.M.Building.TicksFor(plan.Id));
        Finish(w, plan.Id);
        Assert.Empty(w.Refusals);
        Assert.All(Box(5, 2, 2, 6), c => Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(c)));
        Assert.Equal((14, 7), (Count(w, "plate"), Count(w, "gear")));
    }

    [Fact]
    public void AHatchIsADoorOnItsSideAtADoorsCostKnownWithIt()
    {
        var w = Stocked();
        Assert.Equal(w.M.Building.CostFor(Door), w.M.Building.CostFor(Hatch));
        Assert.Equal(Research.Blueprints.Of(Door), Research.Blueprints.Of(Hatch));
        w.Do(new SetCreative(true)).Do(new PlacePlan(At(3, 4), Hatch)).Run();
        Finish(w, w.M.Building.Plans.Single().Id);
        Assert.All(Box(3, 4, 6, 2), c => Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(c)));
        Assert.All(Box(3, 4, 6, 2), c =>
            Assert.True(Doorways.IsHatch(Doorways.LinksOf(c, Width, Width * 10, w.M.Grid.StructureAt))));
    }

    [Fact]
    public void ADoorTwoCellsThickOpensAndShutsWholeFromAnyOfItsCells()
    {
        var w = Stocked();
        w.Do(new Place(At(5, 2), Door)).Do(new Place(At(8, 2), Door)).Run();
        w.Do(new SetDoor(At(6, 7), true)).Run();
        Assert.All(Box(5, 2, 2, 6), c => Assert.Equal(Structure.DoorOpen, w.M.Grid.StructureAt(c)));
        Assert.All(Box(8, 2, 2, 6), c => Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(c)));
        w.Do(new SetDoor(At(5, 2), false)).Run();
        Assert.All(Box(5, 2, 2, 6), c => Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(c)));
        Assert.All(Box(5, 2, 2, 6), c =>
            Assert.False(Doorways.IsHatch(Doorways.LinksOf(c, Width, Width * 10, w.M.Grid.StructureAt))));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void AHatchBesideADoorIsADoorwayOfItsOwn()
    {
        // A hatch run stops beside a door (0067): their cells touch, yet each opens alone.
        var w = new Scenario("""
            ...DD.
            ...DD.
            DDDDD.
            DDDDD.
            ...DD.
            ...DD.
            """, cells: true);
        w.Do(new SetDoor(0 * 6 + 3, true)).Run();
        var door = new[] { 3, 4, 9, 10, 15, 16, 21, 22, 27, 28, 33, 34 };
        Assert.All(door, c => Assert.Equal(Structure.DoorOpen, w.M.Grid.StructureAt(c)));
        Assert.All(new[] { 12, 13, 14, 18, 19, 20 }, c => Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(c)));
        w.Do(new SetDoor(18, true)).Run();
        Assert.All(new[] { 12, 13, 14, 18, 19, 20 }, c => Assert.Equal(Structure.DoorOpen, w.M.Grid.StructureAt(c)));
    }

    [Fact]
    public void ADoorComesApartWholeGivingBackWhatItCost()
    {
        var w = Stocked();
        w.Do(new Place(At(5, 2), Door)).Run();
        w.Do(new Deconstruct(At(6, 6), "structure")).Run();
        var mark = Assert.Single(w.M.Building.Plans);
        Assert.Equal(At(5, 2), mark.Cell);
        Assert.Equal([("plate", 6), ("gear", 3)], w.M.Building.RefundOf(mark.Id));
        Finish(w, mark.Id);
        Assert.All(Box(5, 2, 2, 6), c => Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(c)));
        Assert.Equal((26, 13), (Count(w, "plate"), Count(w, "gear")));
    }

    [Fact]
    public void ADoorwayOfADrawnMapsLoneDoorGivesBackItsShareOfADoor()
    {
        // A lone door character doubles to two by two: a third of a door, two plates and a gear, as one 16 px door cell cost.
        var w = new Scenario("""
            ###
            #D#
            ###
            """);
        w.Do(new Deconstruct(w.At(1, 1), "structure")).Run();
        Assert.Equal([("plate", 2), ("gear", 1)], w.M.Building.RefundOf(w.M.Building.Plans.Single().Id));
    }
}
