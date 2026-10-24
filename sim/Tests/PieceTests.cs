using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Pieces (0104, 0111): a ladder two by two, a door segment two by two and a hatch two by one, each placed, paid, built and
/// taken apart as one thing, the size and the price of the 16 px cell it was; door segments stacked are one doorway, a hatch
/// a doorway of its own.
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
    private static readonly Placement Ladder = new("structure", "ladder"), Door = new("structure", "door");

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
        Assert.Equal("grid.occupied", w.M.Building.Problem(At(10, 4), Door));
        Assert.Equal("grid.outOfBounds", w.M.Building.Problem(At(11, 9), Ladder));
        Assert.Null(w.M.Building.Problem(At(1, 1), Door));
        w.Do(new PlacePlan(At(1, 1), Door)).Run();
        // The piece claims its cells: a ladder over its lower right cell is planned there already.
        Assert.Equal("building.planned", w.M.Building.Problem(At(2, 2), Ladder));
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
    public void ADoorSegmentIsOnePieceTwoByTwoCostingWhatA16PxDoorCellDid()
    {
        var w = Stocked();
        w.Do(new PlacePlan(At(5, 2), Door)).Run();
        var plan = Assert.Single(w.M.Building.Plans);
        Assert.All(Box(5, 2, 2, 2), c => Assert.Equal(plan.Id, w.M.Building.PlansAt(c).Single().Id));
        Assert.Equal([("plate", 2), ("gear", 1)], w.M.Building.CostOf(plan.Id));
        Assert.Equal(20, w.M.Building.TicksFor(plan.Id));
        Finish(w, plan.Id);
        Assert.Empty(w.Refusals);
        Assert.All(Box(5, 2, 2, 2), c => Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(c)));
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(At(5, 4)));
        Assert.Equal((18, 9), (Count(w, "plate"), Count(w, "gear")));
    }

    private static readonly Placement HatchSegment = new("structure", Builds.Hatch);

    [Fact]
    public void AHatchIsTwoByOneItsOwnBlueprintAtTheDoorsCostAndGivenBackWhole()
    {
        var w = Stocked();
        Assert.Equal((2, 1), Builds.PieceOf(Builds.Hatch));
        Assert.Equal([At(5, 2), At(6, 2)], w.M.Grid.Footprint(Builds.Hatch, At(5, 2)));
        Assert.Equal([("plate", 2), ("gear", 1)], w.M.Building.CostFor(HatchSegment));
        w.Do(new PlacePlan(At(5, 2), HatchSegment)).Run();
        var plan = Assert.Single(w.M.Building.Plans);
        Assert.All(Box(5, 2, 2, 1), c => Assert.Equal(plan.Id, w.M.Building.PlansAt(c).Single().Id));
        Assert.Empty(w.M.Building.PlansAt(At(5, 3)));
        Assert.Equal([("plate", 2), ("gear", 1)], w.M.Building.CostOf(plan.Id));
        Finish(w, plan.Id);
        Assert.Empty(w.Refusals);
        Assert.All(Box(5, 2, 2, 1), c => Assert.Equal(Structure.HatchClosed, w.M.Grid.StructureAt(c)));
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(At(5, 3)));
        Assert.True(Doorways.IsHatch(w.M.Grid.StructureAt(At(5, 2))));
        Assert.NotEqual(0, Doorways.LinksOf(At(5, 2), Width, Width * 10, w.M.Grid.StructureAt) & Doorways.Right);
        w.Do(new Deconstruct(At(6, 2), "structure")).Run();
        var mark = Assert.Single(w.M.Building.Plans);
        Assert.Equal([("plate", 2), ("gear", 1)], w.M.Building.RefundOf(mark.Id));
    }

    [Fact]
    public void ALadderIsPlacedFlushUnderAndOverABuiltHatch()
    {
        var w = Stocked();
        w.Do(new Place(At(5, 4), HatchSegment)).Run();
        Assert.Null(w.M.Building.Problem(At(5, 5), Ladder));
        Assert.Null(w.M.Building.Problem(At(5, 2), Ladder));
        w.Do(new PlacePlan(At(5, 5), Ladder)).Run();
        Assert.Empty(w.Refusals);
        Assert.Single(w.M.Building.Plans);
    }

    [Fact]
    public void ADoorStaysADoorLaidAcrossAndAHatchIsItsOwnDoorway()
    {
        var w = Stocked();
        w.Do(new Place(At(1, 1), Door)).Do(new Place(At(3, 1), Door)).Do(new Place(At(5, 1), Door))
            .Do(new Place(At(1, 4), HatchSegment)).Run();
        bool Hatch(int c) => Doorways.IsHatch(w.M.Grid.StructureAt(c));
        Assert.All(Box(1, 1, 4, 2).Concat(Box(5, 1, 2, 2)), c => Assert.False(Hatch(c)));
        Assert.All(Box(1, 4, 2, 1), c => Assert.True(Hatch(c)));
        // A lone segment is two cells wide: the hero, 1.9 cells, fits through it.
        Assert.True(Stillwake.Content.Hero.Width < Builds.Get("door").Across * 1000);
    }

    [Fact]
    public void ADoorsSegmentsOpenWholeStackedAndApartSideBySide()
    {
        var w = Stocked();
        w.Do(new Place(At(1, 1), Door)).Do(new Place(At(1, 3), Door)).Do(new Place(At(1, 5), Door))
            .Do(new Place(At(5, 2), Door)).Do(new Place(At(7, 2), Door)).Do(new Place(At(5, 6), HatchSegment)).Run();
        w.Do(new SetDoor(At(2, 6), true)).Run();
        Assert.All(Box(1, 1, 2, 6), c => Assert.Equal(Structure.DoorOpen, w.M.Grid.StructureAt(c)));
        Assert.All(Box(5, 2, 2, 2).Concat(Box(7, 2, 2, 2)), c => Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(c)));
        Assert.All(Box(5, 6, 2, 1), c => Assert.Equal(Structure.HatchClosed, w.M.Grid.StructureAt(c)));
        // Side by side, each door opens alone.
        w.Do(new SetDoor(At(8, 3), true)).Run();
        Assert.All(Box(7, 2, 2, 2), c => Assert.Equal(Structure.DoorOpen, w.M.Grid.StructureAt(c)));
        Assert.All(Box(5, 2, 2, 2), c => Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(c)));
        // A hatch opens alone too.
        w.Do(new SetDoor(At(6, 6), true)).Run();
        Assert.All(Box(5, 6, 2, 1), c => Assert.Equal(Structure.HatchOpen, w.M.Grid.StructureAt(c)));
        Assert.All(Box(7, 2, 2, 2), c => Assert.Equal(Structure.DoorOpen, w.M.Grid.StructureAt(c)));
        w.Do(new SetDoor(At(1, 1), false)).Run();
        Assert.All(Box(1, 1, 2, 6), c => Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(c)));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void AHatchBesideOrUnderADoorIsADoorwayOfItsOwn()
    {
        var w = new Scenario("""
            #######
            #hhDD.#
            #..DD.#
            #..hh.#
            #######
            """, cells: true);
        w.Do(new SetDoor(w.At(3, 1), true)).Run();
        Assert.All([w.At(3, 1), w.At(4, 1), w.At(3, 2), w.At(4, 2)], c => Assert.Equal(Structure.DoorOpen, w.M.Grid.StructureAt(c)));
        Assert.All([w.At(1, 1), w.At(2, 1), w.At(3, 3), w.At(4, 3)], c => Assert.Equal(Structure.HatchClosed, w.M.Grid.StructureAt(c)));
        w.Do(new SetDoor(w.At(2, 1), true)).Run();
        Assert.All([w.At(1, 1), w.At(2, 1)], c => Assert.Equal(Structure.HatchOpen, w.M.Grid.StructureAt(c)));
        Assert.All([w.At(3, 1), w.At(4, 1), w.At(3, 2), w.At(4, 2)], c => Assert.Equal(Structure.DoorOpen, w.M.Grid.StructureAt(c)));
        w.Do(new SetDoor(w.At(4, 3), true)).Run();
        Assert.All([w.At(3, 3), w.At(4, 3)], c => Assert.Equal(Structure.HatchOpen, w.M.Grid.StructureAt(c)));
        Assert.All([w.At(1, 1), w.At(2, 1)], c => Assert.Equal(Structure.HatchOpen, w.M.Grid.StructureAt(c)));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void ADoorComesApartASegmentAtATimeGivingBackWhatItCost()
    {
        var w = Stocked();
        w.Do(new Place(At(1, 1), Door)).Do(new Place(At(1, 3), Door)).Do(new Place(At(1, 5), Door)).Run();
        // Marked by its lower right cell, the middle segment is kept at its top-left.
        w.Do(new Deconstruct(At(2, 4), "structure")).Run();
        var mark = Assert.Single(w.M.Building.Plans);
        Assert.Equal(At(1, 3), mark.Cell);
        Assert.Equal([("plate", 2), ("gear", 1)], w.M.Building.RefundOf(mark.Id));
        Finish(w, mark.Id);
        Assert.All(Box(1, 3, 2, 2), c => Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(c)));
        Assert.All(Box(1, 1, 2, 2).Concat(Box(1, 5, 2, 2)), c => Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(c)));
        Assert.Equal((22, 11), (Count(w, "plate"), Count(w, "gear")));
    }

    [Fact]
    public void ADrawnMapsLoneDoorIsOneSegment()
    {
        // A lone door character doubles to two by two: one segment, two plates and a gear, as one 16 px door cell cost.
        var w = new Scenario("""
            ###
            #D#
            ###
            """);
        w.Do(new Deconstruct(w.At(1, 1), "structure")).Run();
        Assert.Equal([("plate", 2), ("gear", 1)], w.M.Building.RefundOf(w.M.Building.Plans.Single().Id));
    }
}
