using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Stillwake.Sim.Rooms;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Death leaves a body (0060): what a hero carried, but its gear, lies where its suit ran dry.</summary>
public class DeathTests
{
    /// <summary>Space on the left, an airless sealed room on the right with the spawn in it; no zone, so no cold on the grid.</summary>
    private static readonly string[] Map =
    [
        "          ##########",
        "          #S.......#",
        "          #........#",
        "          ##########",
    ];

    private const int Width = 20;
    private static int At(int x, int y) => y * Width + x;
    private static readonly int Spawn = At(11, 1), Inside = At(15, 1), InSpace = At(4, 1);

    private static Scenario Room() => new(Layout.Parse(Map));
    private static IReadOnlyList<FloorStack> Floor(Scenario w) => w.M.Items.Floor;
    private static int Ticks(Scenario w) => ((ItemsState)w.M.Items.Slice).Ticks;
    private static Hero HeroOf(Scenario w, int peer = 0) => w.M.Crew.HeroOf(peer)!;
    private static int Count(Scenario w, int peer, string item) => w.M.Items.Totals(peer).Where(t => t.Item == item).Sum(t => t.Count);

    /// <summary>Peer <paramref name="peer"/>'s hero brought to <paramref name="cell"/> and run until it passes out there.</summary>
    private static Scenario DieAt(Scenario w, int cell, int peer = 0)
    {
        w.Do(new Enter(cell) { From = peer }).Run();
        for (int i = 0; i < 2 * Content.Crew.Suit * Content.Crew.BreathEvery && HeroOf(w, peer).Out == 0; i++) w.Run();
        Assert.Equal(Content.Crew.PassOut, HeroOf(w, peer).Out);
        return w;
    }

    /// <summary>A hero carrying resources and every kind of gear, some of it worn.</summary>
    private static Scenario Laden(Scenario w, int peer = 0) =>
        w.GiveItems(peer, ("scrap", 30), ("canister", 3), ("fireAxe", 1), ("harpoon", 1), ("drill", 1), ("jetpack", 1), ("iron", 600))
            .Wear(peer, "jetpack").Wear(peer, "battery");

    [Fact]
    public void LeavesWhatTheHeroCarriedWhereItSuffocatedAndKeepsItsGearInItsSlots()
    {
        var w = DieAt(Laden(Room()), Inside);
        var body = new Body(0, Ticks(w));
        // Iron in a full stack and the rest, scrap and canisters: slot order, each stack as it was.
        Assert.Equal(
            [("scrap", 30), ("canister", 3), ("iron", 500), ("iron", 100)],
            Floor(w).Select(s => (s.Item, s.Count)));
        Assert.All(Floor(w), s => Assert.Equal((body, FloorStack.Nobody, Inside), (s.Body, s.By, s.Cell)));
        var slots = w.M.Items.Inventory(0);
        Assert.Equal(["fireAxe", "harpoon", "drill", "jetpack"], slots.Take(Content.Inventory.Slots).OfType<Stack>().Select(s => s.Item));
        Assert.True(w.M.Items.Equipped(0, "jetpack"));
        Assert.True(w.M.Items.Equipped(0, "battery"));
        // It wakes at the spawn with what it kept.
        w.Run(Content.Crew.PassOut);
        Assert.Equal((0, Spawn), (HeroOf(w).Out, HeroOf(w).Cell));
        Assert.Equal(0, Count(w, 0, "scrap"));
    }

    [Fact]
    public void LetsTheBodyLieStillIndoorsAndDriftInSpaceTillItLeavesTheWorld()
    {
        var indoors = DieAt(Laden(Room()), Inside).Run(60);
        var lying = Floor(indoors).Select(s => (s.Id, s.Cell, s.X, s.Y)).ToList();
        Assert.All(Floor(indoors), s => Assert.Equal((At(15, 2), 0, 0), (s.Cell, s.Vx, s.Vy)));
        indoors.Run(2000);
        Assert.Equal(lying, Floor(indoors).Select(s => (s.Id, s.Cell, s.X, s.Y)));

        var outside = DieAt(Laden(Room()), InSpace);
        int x = Floor(outside)[0].X;
        outside.Run(10);
        Assert.All(Floor(outside), s => Assert.Equal(x - 10 * Content.Floor.SpaceDrift, s.X));
        Assert.Equal(RoomsModule.Space, outside.M.Rooms.RoomAt(Floor(outside)[0].Cell));
        outside.Run(x / Content.Floor.SpaceDrift);
        Assert.Empty(Floor(outside));
    }

    [Fact]
    public void TakesTheBodyAwayTheLimitAfterTheDeathWhereverItLies()
    {
        var w = DieAt(Laden(Room()), Inside);
        int died = Ticks(w);
        w.Run(Content.Floor.BodyLasts - 1);
        Assert.Equal(4, Floor(w).Count);
        // Its own drop beside it is no body's and stays.
        w.GiveItems(1, ("scrap", 5)).Do(new Drop(new Items.Place(0), 5, Inside, 0) { From = 1 }).Run();
        Assert.Equal(died + Content.Floor.BodyLasts, Ticks(w));
        Assert.Equal([("scrap", 5)], Floor(w).Select(s => (s.Item, s.Count)));
        Assert.Null(Floor(w)[0].Body);
    }

    [Fact]
    public void LeavesNoBodyInCreativeModeOrWithNothingButGear()
    {
        var creative = Laden(Room()).Do(new SetCreative(true));
        DieAt(creative, Inside);
        Assert.Empty(Floor(creative));
        Assert.Equal(30, Count(creative, 0, "scrap"));

        var geared = Room().GiveItems(0, ("fireAxe", 1), ("drill", 1)).Wear(0, "battery");
        DieAt(geared, Inside);
        Assert.Empty(Floor(geared));
        Assert.Equal(1, Count(geared, 0, "fireAxe"));
        // With nothing at all, nothing either.
        Assert.Empty(Floor(DieAt(Room(), Inside, peer: 2)));
    }

    [Fact]
    public void LeavesTheBodyAtTheSpawnWhenTheHeroDiedOffTheGrid()
    {
        // Off the grid a breath costs a full suit (0050): dead at the first.
        var w = DieAt(Room().GiveItems(0, ("scrap", 7)), -5);
        var s = Assert.Single(Floor(w));
        Assert.Equal(("scrap", 7, Spawn, new Body(0, Ticks(w))), (s.Item, s.Count, s.Cell, s.Body));
        w.Run(40);
        Assert.Equal(At(11, 2), Assert.Single(Floor(w)).Cell);
    }

    [Fact]
    public void LetsAFriendOrTheOwnerPickUpTheBodyAtOnceAsMuchAsFits()
    {
        var w = DieAt(Laden(Room()), Inside);
        int[] ids = [.. Floor(w).Select(s => s.Id)];
        // Peer 1 has room for 20 iron more: its own slots full of canisters but one, half of it iron.
        w.GiveItems(1, ("canister", 5 * (Content.Inventory.Slots - 1)), ("iron", 480));
        w.Do(new PickUp(ids[0]) { From = 0 }).Do(new PickUp(ids[2]) { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(30, Count(w, 0, "scrap"));
        Assert.Equal(500, Count(w, 1, "iron"));
        Assert.Equal([3, 480, 100], Floor(w).Select(s => s.Count));
        w.Do(new PickUp(ids[2]) { From = 1 }).Do(new PickUp(ids[0]) { From = 1 }).Run();
        Assert.Equal(["items.full", "items.gone"], w.Refusals.Select(r => r.Reason));
        w.Do(new PickUp(ids[1]) { From = 0 }).Do(new PickUp(ids[2]) { From = 0 }).Do(new PickUp(ids[3]) { From = 0 }).Run();
        Assert.Empty(Floor(w));
    }

    [Fact]
    public void LeavesTwoBodiesForTwoDeathsInOneCellThatNeverMerge()
    {
        var w = DieAt(Room().GiveItems(0, ("scrap", 10)), Spawn);
        var first = new Body(0, Ticks(w));
        // Waking at the spawn in the airless room, it dies there again with what it gathered since.
        w.Run(Content.Crew.PassOut);
        w.GiveItems(0, ("scrap", 4));
        for (int i = 0; i < 2000 && HeroOf(w).Out == 0; i++) w.Run();
        var second = new Body(0, Ticks(w));
        w.Run(60);
        Assert.True(second.Died > first.Died);
        Assert.Equal([("scrap", 10, first), ("scrap", 4, second)], Floor(w).Select(s => (s.Item, s.Count, s.Body)));
        Assert.Equal(Floor(w)[0].Cell, Floor(w)[1].Cell);
        // The first goes at its own limit, the second stays.
        w.Run(first.Died + Content.Floor.BodyLasts - Ticks(w));
        Assert.Equal([second], Floor(w).Select(s => s.Body));
    }

    [Fact]
    public void KeepsTheBodyThroughASaveAndALoad()
    {
        var w = DieAt(Laden(Room()), Inside);
        var body = new Body(0, Ticks(w));
        Assert.Contains("\"body\":", w.World.Save());
        w.Reload();
        Assert.All(Floor(w), s => Assert.Equal(body, s.Body));
        w.Run(Content.Floor.BodyLasts - 1);
        Assert.Equal(4, Floor(w).Count);
        w.Run();
        Assert.Empty(Floor(w));
    }

    [Fact]
    public void GivesTheStacksOfASaveBeforeBodiesNoBody()
    {
        string old = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "format-14.json"));
        var m = new Registry();
        var world = World.Load(Migrations.Upgrade(old), m.All);
        Assert.All(m.Items.Floor, s => Assert.Null(s.Body));
        Assert.DoesNotContain("\"body\":", world.Save());
    }

    // Invariants.

    /// <summary>An <c>Enter</c> of a hero somewhere, a pick-up of the <c>Id</c>-th stack on the floor, or a wait of <c>Ticks</c>.</summary>
    private sealed record Step(string Type, int Peer, int Cell, int Id, int Ticks);

    private static readonly string[] Counted = ["scrap", "iron", "canister", "planks"];

    /// <summary>Of each counted item, what both heroes carry and what lies on the floor.</summary>
    private static int[] Total(Scenario w) =>
        [.. Counted.Select(item => new[] { 0, 1 }.Sum(p => Count(w, p, item)) + Floor(w).Where(s => s.Item == item).Sum(s => s.Count))];

    /// <summary>
    /// A tick, <paramref name="total"/> being what was carried and lying before it. Nothing is made, and nothing lost
    /// but stacks that leave: those in space past the grid's edge, and a body's at its limit. No body holds gear.
    /// </summary>
    private static void Tick(Scenario w, ref int[] total)
    {
        int next = Ticks(w) + 1;
        var leaving = Floor(w)
            .Where(s => w.M.Rooms.RoomAt(s.Cell) == RoomsModule.Space || s.Body is { } b && next - b.Died >= Content.Floor.BodyLasts)
            .Select(s => (s.Id, s.Item, s.Count)).ToList();
        // Counted only around a tick when something may leave, and after each step: every tick is slow.
        if (leaving.Count == 0)
        {
            w.Run();
            return;
        }
        Assert.Equal(total, Total(w));
        w.Run();
        var after = Total(w);
        for (int i = 0; i < Counted.Length; i++)
        {
            int lost = total[i] - after[i];
            int left = leaving.Where(s => s.Item == Counted[i] && Floor(w).All(f => f.Id != s.Id)).Sum(s => s.Count);
            Assert.InRange(lost, 0, left);
        }
        total = after;
    }

    private static void NoGearInABody(Scenario w) =>
        Assert.All(Floor(w).Where(s => s.Body != null), s => Assert.False(Content.Items.Get(s.Item).Gear, $"{s.Item} left in a body"));

    [Property(MaxTest = 40)]
    public Property NeverMakesAnItemLosesOnlyWhatLeavesTheWorldOrOutlivesItsBodyAndLeavesNoGear()
    {
        var cell = Gen.OneOf(
            from x in Gen.Choose(11, 18) from y in Gen.Choose(1, 2) select At(x, y),
            from x in Gen.Choose(0, 9) from y in Gen.Choose(0, 3) select At(x, y),
            Gen.Elements(-1, Width * Map.Length + 3));
        // A pick-up names a stack by its place on the floor as it is then: mostly one there, now and then none.
        var step = Gen.Frequency(
            (2, from c in cell select new Step("enter", 0, c, 0, 0)),
            (4, from p in Gen.Choose(0, 1) from at in Gen.Choose(0, 12) select new Step("pick", p, 0, at, 0)),
            (4, from t in Gen.Frequency((3, Gen.Choose(1, 200)), (3, Gen.Choose(200, 1200)), (1, Gen.Constant(Content.Floor.BodyLasts)))
                select new Step("wait", 0, 0, 0, t)));
        // Peer 0's hero comes in first, somewhere, so its suit runs down from the start; peer 1 has none, so it never dies.
        var runs = from first in cell from n in Gen.Choose(0, 16) from all in step.ArrayOf(n) select (First: first, Steps: all);
        return Prop.ForAll(runs.ToArbitrary(), run =>
        {
            // Peer 1 has room for ten scrap and no slot free: what it picks up of a body, it picks up in part.
            var w = Laden(Room()).GiveItems(0, ("planks", 20)).GiveItems(1, ("scrap", 490), ("canister", 5 * (Content.Inventory.Slots - 2)), ("harpoon", 1));
            w.Do(new Enter(run.First) { From = 0 });
            var total = Total(w);
            foreach (var s in run.Steps)
            {
                if (s.Type == "enter") w.Do(new Enter(s.Cell) { From = s.Peer });
                else if (s.Type == "pick") w.Do(new PickUp(s.Id < Floor(w).Count ? Floor(w)[s.Id].Id : 999) { From = s.Peer });
                for (int t = 0; t < Math.Max(1, s.Ticks); t++) Tick(w, ref total);
                Assert.Equal(total, Total(w));
                NoGearInABody(w);
            }
        });
    }
}
