using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Rooms;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/items/floor.test.ts, ported.</summary>
public class ItemsFloorTests
{
    /// <summary>A room ten cells wide with a floor across it and three rows of air above, and space to its right.</summary>
    private static readonly string[] Map =
        new[] { "############", "#..........#", "#..........#", "#..........#", "#==========#", "############" }
            .Select(r => r.PadRight(Width)).ToArray();

    private const int Width = 26;
    private static int At(int x, int y) => y * Width + x;
    private static Scenario Room() => new(Layout.Parse(Map));
    private static Items.Place Own(int slot) => new(slot);

    private static IReadOnlyList<FloorStack> Floor(Scenario w) => w.M.Items.Floor;
    private static int Column(Scenario w, int i = 0) => (i < Floor(w).Count ? Floor(w)[i].Cell : -1) % Width;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    private static Scenario Drop(Scenario w, Items.Place src, int count, int cell, int from = 0, int facing = 0) =>
        w.Do(new Drop(src, count, cell, facing) { From = from }).Run();

    [Fact]
    public void TakesAStackLetGoWhichFallsUntilSomethingHoldsItAndComesToRest()
    {
        var w = Room().GiveItems(0, ("scrap", 10));
        Drop(w, Own(0), 4, At(2, 1));
        Assert.Equal(new Stack("scrap", 6), w.M.Items.Inventory(0)[0]);
        w.Run(40);
        // On the floor row's top: the cell above it.
        var s = Assert.Single(Floor(w));
        Assert.Equal(("scrap", 4, At(2, 3), 0, 0), (s.Item, s.Count, s.Cell, s.Vx, s.Vy));
    }

    [Fact]
    public void ThrowsAStackTheWayTheHeroFacesAFewCellsAlongBeforeItComesToRest()
    {
        var right = Room().GiveItems(0, ("scrap", 1));
        Drop(right, Own(0), 1, At(3, 3), 0, 1);
        right.Run(60);
        Assert.InRange(Column(right), 5, 7);
        Assert.Equal((0, 0), (Floor(right)[0].Vx, Floor(right)[0].Vy));

        var left = Room().GiveItems(0, ("scrap", 1));
        Drop(left, Own(0), 1, At(8, 3), 0, -1);
        left.Run(60);
        Assert.InRange(Column(left), 4, 6);
    }

    [Fact]
    public void StopsAStackThrownAtAWallAgainstIt()
    {
        var w = Room().GiveItems(0, ("scrap", 1));
        Drop(w, Own(0), 1, At(10, 3), 0, 1);
        w.Run(60);
        Assert.Equal(At(10, 3), Floor(w)[0].Cell);
    }

    [Fact]
    public void LetsAStackThrownIntoSpaceFlyOnAndDriftAwayAndPastTheEdgeItIsGone()
    {
        var w = Room().GiveItems(0, ("scrap", 2));
        Drop(w, Own(0), 1, At(14, 1), 0, 1);
        w.Run(10);
        // Flying on as thrown, less the drift back past the moving ship; nothing falls in space.
        Assert.True(Column(w) > 14);
        Assert.True(Floor(w)[0].Cell / Width <= 1);
        w.Run(200);
        Assert.Empty(Floor(w));
        // One let go out there, not thrown, drifts back the other way.
        Drop(w, Own(0), 1, At(20, 2));
        w.Run(30);
        Assert.True(Column(w) < 20);
    }

    [Fact]
    public void LiftsAStackOutOfSomethingBuiltWhereItLiesOntoTheTopOfIt()
    {
        var w = Room().GiveItems(0, ("scrap", 1));
        // Into the hull's bottom row: out through the floor row, the nearest room cell, onto the floor.
        Drop(w, Own(0), 1, At(3, 5));
        w.Run(40);
        Assert.Equal(At(3, 3), Floor(w)[0].Cell);
    }

    /// <summary>A world from <paramref name="map"/> with peer 0 carrying one scrap for each cell of <paramref name="at"/>, let go still there in turn.</summary>
    private static Scenario DroppedInto(string[] map, params (int X, int Y)[] at)
    {
        var w = new Scenario(Layout.Parse(map)).GiveItems(0, ("scrap", at.Length));
        foreach (var (x, y) in at) Drop(w, Own(0), 1, CellOf(map, x, y));
        return w;
    }

    private static int CellOf(string[] map, int x, int y) => y * map[0].Length + x;
    private static (string, int, int)[] Lying(Scenario w) => Floor(w).Select(s => (s.Item, s.Count, s.Cell)).ToArray();

    /// <summary>A room whose hull has space to its left and above.</summary>
    private static readonly string[] HullInSpace = ["          ", "   #######", "   #.....#", "   #.....#", "   #######"];

    [Fact]
    public void PushesAStackOutOfTheHullIntoTheRoomRatherThanIntoSpaceEvenWhenSpaceIsNearer()
    {
        // Space and the room a step away either side: the room, though space's cell comes first.
        var w = DroppedInto(HullInSpace, (3, 2));
        Assert.Equal(CellOf(HullInSpace, 4, 2), Floor(w)[0].Cell);
        // The hull's corner: space a step away up and left, the room two.
        Drop(w.GiveItems(0, ("scrap", 1)), Own(0), 1, CellOf(HullInSpace, 3, 1));
        w.Run(40);
        Assert.Equal([("scrap", 2, CellOf(HullInSpace, 4, 3))], Lying(w));
    }

    [Fact]
    public void PushesAStackInAWallBetweenRoomsIntoTheNearestRoomCellTheFirstByIndexOnATie()
    {
        string[] map = ["#######", "#..#..#", "#..#..#", "#######", "#.....#", "#######"];
        // Between two rooms side by side: the left; between one above and one below: the one above.
        var w = DroppedInto(map, (3, 2), (5, 3));
        w.Run(40);
        Assert.Equal([("scrap", 1, CellOf(map, 2, 2)), ("scrap", 1, CellOf(map, 5, 2))], Lying(w));
    }

    [Fact]
    public void PushesAStackInAWallWithNoRoomWithinReachIntoTheNearestSpaceWhereItDriftsAway()
    {
        string[] map = ["      #####", "  #   #...#", "  #   #####"];
        var w = DroppedInto(map, (2, 2));
        Assert.Equal(CellOf(map, 1, 2), Floor(w)[0].Cell);
        Assert.Equal(RoomsModule.Space, w.M.Rooms.RoomAt(CellOf(map, 1, 2)));
        w.Run(2 * 1000 / Content.Floor.SpaceDrift);
        Assert.Empty(Floor(w));
    }

    [Fact]
    public void RaisesAStackDeepInsideSomethingBuiltACellATickUntilARoomIsWithinReach()
    {
        int depth = Content.Floor.PushReach + 2;
        string[] map = ["#######", "#.....#", .. Enumerable.Repeat("#######", depth)];
        var w = DroppedInto(map, (3, depth + 1));
        Assert.Equal(CellOf(map, 3, depth), Floor(w)[0].Cell);
        w.Run();
        Assert.Equal(CellOf(map, 3, depth - 1), Floor(w)[0].Cell);
        w.Run();
        Assert.Equal(CellOf(map, 3, 1), Floor(w)[0].Cell);
    }

    /// <summary>A room with a grow capsule (2×3) standing on its floor, two rows of air above the capsule.</summary>
    private static readonly string[] Capsule =
        ["##########", "#........#", "#........#", "#..gg....#", "#..gg....#", "#..gg....#", "#========#", "##########"];

    [Fact]
    public void AStackFallingOntoAMachineFallsThroughItOntoTheFloorBeneath()
    {
        // Let go still above the capsule, and thrown over it: both lie on the floor it stands on.
        var w = DroppedInto(Capsule, (3, 1));
        Drop(w.GiveItems(0, ("canister", 1)), Own(0), 1, CellOf(Capsule, 2, 2), 0, 1);
        w.Run(80);
        Assert.All(Floor(w), s => Assert.Equal((6 * 1000, 0, 0), (s.Y, s.Vx, s.Vy)));
        Assert.Equal(CellOf(Capsule, 3, 5), Floor(w)[0].Cell);
        Assert.Equal(5, Floor(w)[1].Cell / Capsule[0].Length);
    }

    [Fact]
    public void LeavesAStackInsideAMachineWhereItIsToFallToTheFloorUnderIt()
    {
        string[] map = ["######", "#TT..#", "#TT..#", "######"];
        var w = DroppedInto(map, (2, 2), (1, 1));
        w.Run(40);
        Assert.Equal([("scrap", 1, CellOf(map, 2, 2)), ("scrap", 1, CellOf(map, 1, 2))], Lying(w));
    }

    [Fact]
    public void AStackLeftOnAMachineByAnOlderSaveFallsOffItOnTheNextTick()
    {
        var w = DroppedInto(Capsule, (3, 1));
        w.Run(80);
        // Where the rule that machines hold stacks left it: on the capsule's top.
        var s = Floor(w)[0];
        (s.Y, s.Cell) = (3 * 1000, CellOf(Capsule, 3, 2));
        w.Reload().Run();
        Assert.True(Floor(w)[0].Vy > 0);
        w.Run(40);
        Assert.Equal((CellOf(Capsule, 3, 5), 6 * 1000), (Floor(w)[0].Cell, Floor(w)[0].Y));
    }

    [Fact]
    public void PushesAStackDriftingIntoTheHullFromSpaceIntoTheRoomBehindIt()
    {
        var w = Room().GiveItems(0, ("scrap", 1));
        Drop(w, Own(0), 1, At(14, 2));
        // Let go still, it drifts back onto the hull's right wall in under a hundred ticks.
        w.Run(120);
        Assert.Equal([("scrap", 1, At(10, 3))], Lying(w));
    }

    [Fact]
    public void GathersStacksOfOneItemAtRestInACellIntoOneUpToTheStackLimit()
    {
        var w = Room().GiveItems(0, ("scrap", 10), ("canister", 5));
        Drop(w, Own(0), 3, At(2, 1));
        w.Run(40);
        // Falling onto the first, already at rest on the floor.
        Drop(w, Own(0), 4, At(2, 1));
        Drop(w, Own(1), 5, At(2, 1));
        w.Run(40);
        Assert.Equal(new[] { ("scrap", 7, At(2, 3)), ("canister", 5, At(2, 3)) }, Floor(w).Select(s => (s.Item, s.Count, s.Cell)));
        var more = w.GiveItems(1, ("canister", 5));
        Drop(more, Own(0), 2, At(2, 1), 1);
        more.Run(40);
        // A full stack takes no more: the two lie side by side.
        Assert.Equal(new[] { ("scrap", 7), ("canister", 5), ("canister", 2) }, Floor(more).Select(s => (s.Item, s.Count)));
    }

    [Fact]
    public void RefusesADropFromNowhereOfNothingOrOntoNoCell()
    {
        var w = Room().GiveItems(0, ("scrap", 1));
        Drop(w, Own(99), 1, At(2, 1));
        Drop(w, Own(5), 1, At(2, 1));
        Drop(w, Own(0), 0, At(2, 1));
        Drop(w, new Items.Place(0, Crate: 7), 1, At(2, 1));
        Drop(w, Own(0), 1, -1);
        Drop(w, Own(0), 1, Width * Map.Length);
        Assert.Equal(
            new[] { "items.noSlot", "items.empty", "items.empty", "items.noCrate", "items.noCell", "items.noCell" },
            Reasons(w));
        Assert.Empty(Floor(w));
    }

    [Fact]
    public void GivesAStackToWhoeverPicksItUpAsMuchAsFitsTheRestLeftLying()
    {
        var w = Room().GiveItems(0, ("scrap", 20));
        Drop(w, Own(0), 20, At(2, 3));
        int id = Assert.Single(Floor(w)).Id;
        // Peer 1 has room for 15 more: its inventory is canisters but for one slot of scrap.
        w.GiveItems(1, ("canister", 5 * (36 - 1)), ("scrap", Content.Items.Get("scrap").Stack - 15));
        Assert.Equal(15, w.M.Items.PickUpAmount(1, id));
        w.Do(new PickUp(id) { From = 1 }).Run();
        var left = Assert.Single(Floor(w));
        Assert.Equal((id, 5), (left.Id, left.Count));
        Assert.Equal(0, w.M.Items.PickUpAmount(1, id));
        w.Do(new PickUp(id) { From = 1 }).Run();
        Assert.Equal(new[] { "items.full" }, Reasons(w));
    }

    [Fact]
    public void KeepsItsOwnDropFromTheDropperForAMomentNotFromAnyoneElse()
    {
        var w = Room().GiveItems(0, ("scrap", 2));
        Drop(w, Own(0), 1, At(2, 3));
        Drop(w, Own(0), 1, At(4, 3));
        int mine = Floor(w)[0].Id, theirs = Floor(w)[1].Id;
        w.Do(new PickUp(mine) { From = 0 }).Run();
        w.Do(new PickUp(theirs) { From = 1 }).Run();
        Assert.Equal(new[] { "items.tooSoon" }, Reasons(w));
        w.Run(Content.Floor.OwnPickupDelay);
        w.Do(new PickUp(mine) { From = 0 }).Run();
        w.Do(new PickUp(mine) { From = 1 }).Run();
        Assert.Equal(new[] { "items.tooSoon", "items.gone" }, Reasons(w));
        Assert.Empty(Floor(w));
    }

    private static readonly string[] All = ["scrap", "iron", "canister"];

    private static int[] Everything(Scenario w)
    {
        var held = new[] { 0, 1 }.SelectMany(p => w.M.Items.Inventory(p)).ToList();
        return All.Select(item =>
            held.Sum(s => s?.Item == item ? s.Count : 0) + Floor(w).Sum(s => s.Item == item ? s.Count : 0)).ToArray();
    }

    /// <summary>A drop or a pick-up, then <c>Wait</c> ticks more.</summary>
    private sealed record Step(Command Command, int Wait);

    [Property(MaxTest = 150)]
    public Property NeverMakesOrLosesAnItemInsideTheShipWhateverIsMovedDroppedOrPickedUp()
    {
        // Anywhere inside the room, or off the grid: only space throws things away.
        var cell = Gen.OneOf(
            from x in Gen.Choose(1, 10) from y in Gen.Choose(1, 3) select At(x, y),
            Gen.Constant(-1));
        var step = Gen.OneOf(
            from slot in ItemsTests.Slots(37)
            from count in Gen.Choose(0, 600)
            from at in cell
            from peer in Gen.Choose(0, 1)
            from facing in Gen.Choose(-1, 1)
            from wait in Gen.Choose(0, 20)
            select new Step(new Drop(Own(slot), count, at, facing) { From = peer }, wait),
            from id in Gen.Choose(0, 12)
            from peer in Gen.Choose(0, 1)
            from wait in Gen.Choose(0, Content.Floor.OwnPickupDelay)
            select new Step(new PickUp(id) { From = peer }, wait));
        var steps = from n in Gen.Choose(0, 40) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = Room().GiveItems(0, ("scrap", 900), ("canister", 7)).GiveItems(1, ("iron", 40));
            var start = Everything(w);
            foreach (var s in all)
            {
                w.Do(s.Command).Run(1 + s.Wait);
                Assert.Equal(start, Everything(w));
                foreach (var stack in Floor(w)) Assert.InRange(stack.Count, 1, Content.Items.Get(stack.Item).Stack);
            }
        });
    }

    // A ramp of two ◢ up to the right wall, a room above it.
    private static readonly string[] Ramp = ["##########", "#........#", "#........#", "#....../##", "#...../###", "##########"];

    private static Scenario Dropped(int x, int y, int facing = 0)
    {
        var w = new Scenario(Layout.Parse(Ramp)).GiveItems(0, ("scrap", 1));
        return w.Do(new Drop(new Items.Place(0), 1, y * Ramp[0].Length + x, facing)).Run();
    }

    /// <summary>The ground under <paramref name="x"/> on <see cref="Ramp"/>: the floor, or a slope's diagonal there.</summary>
    private static int RampGround(int x) => (x / Shape.Unit) switch
    {
        6 => 4 * Shape.Unit + Shape.TopAt(Shape.BottomRight, x % Shape.Unit),
        7 => 3 * Shape.Unit + Shape.TopAt(Shape.BottomRight, x % Shape.Unit),
        _ => 5 * Shape.Unit,
    };

    [Fact]
    public void AStackFallingOnASlopeRestsOnItsDiagonalAtItsXAndStaysThere()
    {
        var w = Dropped(6, 3);
        w.Run(40);
        var s = Assert.Single(w.M.Items.Floor);
        Assert.Equal((6500, 4 * Shape.Unit + 499, w.At(6, 4), 0, 0), (s.X, s.Y, s.Cell, s.Vx, s.Vy));
        // Lying in the slope's cell, it is not pushed out of it.
        w.Run(200);
        Assert.Equal((6500, 4 * Shape.Unit + 499), (w.M.Items.Floor[0].X, w.M.Items.Floor[0].Y));
    }

    [Fact]
    public void AStackThrownUpASlopeSkidsUpItAndComesToRestOnIt()
    {
        var rested = new List<int>();
        foreach (int from in new[] { 3, 4, 5 })
        {
            var w = Dropped(from, 4, 1);
            w.Run(120);
            var s = Assert.Single(w.M.Items.Floor);
            Assert.Equal((0, 0), (s.Vx, s.Vy));
            Assert.Equal(RampGround(s.X), s.Y);
            rested.Add(s.X);
        }
        Assert.Contains(rested, x => x / Shape.Unit >= 6);
    }

    [Fact]
    public void AStackOnASlopeFallsWhenItsWallIsTakenApart()
    {
        var w = Dropped(6, 3);
        w.Run(40);
        w.Do(new ClearAt(w.At(6, 4), "structure")).Run(40);
        var s = Assert.Single(w.M.Items.Floor);
        Assert.Equal((6500, 5 * Shape.Unit, w.At(6, 4)), (s.X, s.Y, s.Cell));
    }
}
