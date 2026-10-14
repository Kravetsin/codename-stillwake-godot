using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Items;
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
        // Into the hull's bottom row: up through the floor row onto the floor.
        Drop(w, Own(0), 1, At(3, 5));
        w.Run(40);
        Assert.Equal(At(3, 3), Floor(w)[0].Cell);
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
}
