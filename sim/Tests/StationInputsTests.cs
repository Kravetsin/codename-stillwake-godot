using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using Crafts = Stillwake.Content.Crafts;
using Machines = Stillwake.Content.Machines;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Input slots for queued stations (0057): the Former and the assembler take items in, and an entry fed from the
/// inputs takes each unit's cost from them as it begins; the pod stays paid by hand.
/// </summary>
public class StationInputsTests
{
    /// <summary>A solar panel under the stars cabled to a Former, the lifepod beside it; peer 0 carries 40 iron.</summary>
    private static Scenario World()
    {
        string[] rows = ["PPP         ", "            ", "############", "#MM.OOO....#", "#MM.OOO....#", "#...OOO....#", "############"];
        return new Scenario(Layout.Parse(rows, null, [((0, 0), (1, 3))])).GiveItems(0, ("iron", 40));
    }

    private static int Former(Scenario w) => w.M.Grid.MachinesOf("former").Single().Id;
    private static int Pod(Scenario w) => w.M.Grid.MachinesOf("lifepod").Single().Id;
    private static int Demand(Scenario w) => w.M.Power.NetworkOf(0)!.Demand;
    private static readonly int Inputs = Machines.Get("former").Inputs!.Value;
    private static int InInputs(Scenario w, string item) => w.M.Items.Crate(Former(w)).Take(Inputs).Sum(s => s?.Item == item ? s.Count : 0);
    private static int Made(Scenario w, string item) => w.M.Items.Crate(Former(w)).Skip(Inputs).Sum(s => s?.Item == item ? s.Count : 0);
    private static int Carried(Scenario w, string item, int peer = 0) => w.M.Items.Inventory(peer).Sum(s => s?.Item == item ? s.Count : 0);
    private static (string, int)[] Lacks(Scenario w) => [.. w.M.Crafting.Lacks(Former(w))];
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static readonly int Plate = Crafts.Get("plate").Ticks * 1000;
    private static readonly int Draws = Machines.Get("former").Draws!.Value;

    /// <summary><paramref name="w"/> with <paramref name="count"/> iron moved by hand from peer 0's first slot into the Former, naming no slot.</summary>
    private static Scenario Feed(Scenario w, int count) => w.Do(new Move(new Slot(0), new Target(null, Former(w)), count)).Run();

    [Fact]
    public void TheFormerTakesThreeInputsAndTheAssemblerFourBeforeTheirThreeOutputsThePodNone()
    {
        Assert.Equal((3, 6), (Machines.Get("former").Inputs, Machines.Get("former").Slots));
        Assert.Equal((4, 7), (Machines.Get("assembler").Inputs, Machines.Get("assembler").Slots));
        Assert.Equal((0, 3), (Machines.Get("lifepod").Inputs, Machines.Get("lifepod").Slots));
    }

    [Fact]
    public void AnEntryFedFromTheInputsTakesNothingWhenQueuedAndAPlatesTwoIronAsItBegins()
    {
        var w = Feed(World(), 10);
        int former = Former(w);
        Assert.Equal((10, 30), (InInputs(w, "iron"), Carried(w, "iron")));
        // Peer 1 carries nothing: never refused as unpaid.
        w.Do(new Craft(former, "plate", 2, FromInputs: true) { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((8, 30), (InInputs(w, "iron"), Carried(w, "iron")));
        Assert.Equal(1000, w.M.Crafting.Progress(former));
        Assert.True(w.M.Crafting.Queue(former).Single() is { FromInputs: true, Endless: false, Count: 2, By: 1 });
        w.Run(Plate / 1000 - 1);
        Assert.Equal((1, 8), (Made(w, "plate"), InInputs(w, "iron")));
        // The next unit takes its own as it begins, on the next tick.
        w.Run();
        Assert.Equal(6, InInputs(w, "iron"));
        w.Run(Plate / 1000);
        Assert.Equal((2, 6), (Made(w, "plate"), InInputs(w, "iron")));
        Assert.Empty(w.M.Crafting.Queue(former));
    }

    [Fact]
    public void TakesACostFromItsInputsInSlotOrder()
    {
        var w = World().GiveItems(0, ("iron", 3));
        int former = Former(w);
        w.Do(new Move(new Slot(0), new Target(0, former), 1)).Do(new Move(new Slot(0), new Target(1, former), 5)).Run();
        w.Do(new Craft(former, "plate", 1, FromInputs: true)).Run();
        // The first slot's one, then one of the second's five.
        Assert.Null(w.M.Items.Crate(former)[0]);
        Assert.Equal(new Stack("iron", 4), w.M.Items.Crate(former)[1]);
    }

    [Fact]
    public void WaitsWithEmptyInputsRestingAndDrawingNothingUntilTheyCome()
    {
        var w = World();
        int former = Former(w);
        w.Do(new Craft(former, "plate", 1, FromInputs: true)).Run(3);
        Assert.Equal(0, w.M.Crafting.Progress(former));
        Assert.False(w.M.Crafting.Works(former));
        Assert.Equal(new[] { ("iron", 2) }, Lacks(w));
        Assert.Equal(0, Demand(w));
        // One iron is not a plate's: still waiting, for the one left.
        Feed(w, 1).Run();
        Assert.Equal(new[] { ("iron", 1) }, Lacks(w));
        Assert.Equal((0, 0), (w.M.Crafting.Progress(former), Demand(w)));
        // The tick its cost comes is the first of its work, at the share the network gave it resting.
        Feed(w, 1);
        Assert.Equal(1000, w.M.Crafting.Progress(former));
        Assert.Empty(w.M.Crafting.Lacks(former));
        w.Run();
        Assert.Equal(Draws, Demand(w));
        Assert.Equal(0, InInputs(w, "iron"));
    }

    [Fact]
    public void APaidEntryBehindAWaitingOneWaitsTooTheQueueFirstInFirstOut()
    {
        var w = World();
        int former = Former(w);
        w.Do(new Craft(former, "plate", 1, FromInputs: true)).Do(new Craft(former, "gear", 1)).Run(200);
        Assert.Equal(39, Carried(w, "iron"));
        Assert.Equal((0, 0), (Made(w, "gear"), w.M.Crafting.Progress(former)));
        Assert.Equal(0, Demand(w));
        // Cancelled, the waiting entry lets the paid one through.
        w.Do(new Cancel(former, 1)).Run(Crafts.Get("gear").Ticks);
        Assert.Equal(1, Made(w, "gear"));
        Assert.Equal(39, Carried(w, "iron"));
    }

    [Fact]
    public void ACancelGivesBackOnlyTheUnitBegunOfAnEntryFedFromTheInputs()
    {
        var w = Feed(World(), 10);
        int former = Former(w);
        w.Do(new Craft(former, "plate", 3, FromInputs: true)).Do(new Craft(former, "plate", 3, FromInputs: true)).Run(5);
        Assert.Equal((8, 30), (InInputs(w, "iron"), Carried(w, "iron")));
        // One not begun gives nothing back; nor does it touch the inputs.
        w.Do(new Cancel(former, 2)).Run();
        Assert.Equal((8, 30), (InInputs(w, "iron"), Carried(w, "iron")));
        // The one at work gives back its unit begun, to whoever cancels; the inputs keep what they hold.
        w.Do(new Cancel(former, 1) { From = 1 }).Run();
        Assert.Equal((8, 30, 2), (InInputs(w, "iron"), Carried(w, "iron"), Carried(w, "iron", 1)));
        Assert.Equal(0, w.M.Crafting.Progress(former));
        Assert.Empty(w.M.Crafting.Queue(former));
        Assert.Empty(w.Refusals);

        // Waiting at nothing done, it has begun nothing: a cancel gives nothing back.
        w.Do(new Move(new Slot(0, former), new Target(), 99)).Run();
        w.Do(new Craft(former, "plate", 1, FromInputs: true)).Run(3).Do(new Cancel(former, 3)).Run();
        Assert.Equal((0, 38), (InInputs(w, "iron"), Carried(w, "iron")));
    }

    [Fact]
    public void AnEndlessEntryRepeatsWhileThereIsIronWaitsWhenItRunsOutAndGoesOnWhenItComes()
    {
        var w = Feed(World(), 6);
        int former = Former(w);
        w.Do(new Craft(former, "plate", 1, FromInputs: true)).Do(new SetEndless(former, 1, true)).Run(3 * Plate / 1000 + 10);
        Assert.Empty(w.Refusals);
        Assert.Equal((3, 0), (Made(w, "plate"), InInputs(w, "iron")));
        Assert.True(w.M.Crafting.Queue(former).Single() is { Endless: true, Count: 1 });
        Assert.Equal(new[] { ("iron", 2) }, Lacks(w));
        Assert.Equal(0, Demand(w));
        Feed(w, 4).Run(2 * Plate / 1000);
        Assert.Equal(5, Made(w, "plate"));
        // Endless no more, its one unit left is its last.
        Feed(w, 4).Do(new SetEndless(former, 1, false)).Run(2 * Plate / 1000);
        Assert.Equal((6, 2), (Made(w, "plate"), InInputs(w, "iron")));
        Assert.Empty(w.M.Crafting.Queue(former));
    }

    [Fact]
    public void ACancelTakesAnEndlessEntryOffGivingBackItsUnitBegun()
    {
        var w = Feed(World(), 6);
        int former = Former(w);
        w.Do(new Craft(former, "plate", 1, FromInputs: true)).Do(new SetEndless(former, 1, true)).Run(Plate / 1000 + 3);
        Assert.Equal((1, 2), (Made(w, "plate"), InInputs(w, "iron")));
        w.Do(new Cancel(former, 1)).Run();
        Assert.Equal((36, 2), (Carried(w, "iron"), InInputs(w, "iron")));
        Assert.Empty(w.M.Crafting.Queue(former));
    }

    [Fact]
    public void ItemsGoIntoTheInputsByHandTheFirstWhenNoSlotIsNamedTheOutputsTakeOnly()
    {
        var w = World().GiveItems(0, ("gear", 3));
        int former = Former(w);
        w.Do(new Move(new Slot(0), new Target(null, former), 4)).Do(new Move(new Slot(1), new Target(1, former), 3)).Run();
        Assert.Equal([new Stack("iron", 4), new Stack("gear", 3), null, null, null, null], w.M.Items.Crate(former));
        for (int slot = Inputs; slot < Machines.Get("former").Slots; slot++)
            w.Do(new Move(new Slot(0), new Target(slot, former), 1));
        w.Run();
        Assert.Equal(Enumerable.Repeat("items.takeOnly", 3), Reasons(w));
        // Taken back out by hand as from any slot.
        w.Do(new Move(new Slot(1, former), new Target(), 3)).Run();
        Assert.Equal(3, Carried(w, "gear"));
        // The pod takes nothing in, whichever slot is named.
        w.Do(new Move(new Slot(0), new Target(null, Pod(w)), 1)).Do(new Move(new Slot(0), new Target(0, Pod(w)), 1)).Run();
        Assert.Equal(Enumerable.Repeat("items.takeOnly", 5), Reasons(w));
    }

    [Fact]
    public void AFormerWithAPaidEntryAndEmptyInputsCountsItsDraw()
    {
        var w = World();
        int former = Former(w);
        w.Do(new Craft(former, "plate", 1)).Run(2);
        Assert.All(w.M.Items.Crate(former).Take(Inputs), Assert.Null);
        Assert.Equal(Draws, Demand(w));
    }

    [Fact]
    public void RefusesAnEntryFedFromInputsAtThePodWhichHasNoneAndOnePastTheQueue()
    {
        var w = World();
        w.Do(new Craft(Pod(w), "plate", 1, FromInputs: true)).Run();
        Assert.Equal(["crafting.noInputs"], Reasons(w));
        Assert.Empty(w.M.Crafting.Queue(Pod(w)));
        int former = Former(w);
        for (int i = 0; i < Content.Stations.Get("former").QueueMost; i++) w.Do(new Craft(former, "plate", 1, FromInputs: true));
        w.Do(new Craft(former, "plate", 1, FromInputs: true)).Do(new Craft(former, "fireAxe", 1, FromInputs: true)).Run();
        Assert.Equal(["crafting.noInputs", "crafting.full", "crafting.notHere"], Reasons(w));
        w.Do(new Craft(999, "plate", 1, FromInputs: true)).Do(new Craft(Pod(w), "scrap", 1, FromInputs: true)).Run();
        Assert.Equal(["crafting.noStation", "crafting.unknown"], Reasons(w).Skip(3));
    }

    [Fact]
    public void RefusesAnEndlessSwitchOfAPaidEntryOfNoneAndAtWhatIsNoQueuedStation()
    {
        var w = new Scenario(Layout.Parse(["PPP         ", "            ", "############", "#MM.FF.....#", "#MM.FF.....#", "############"], null, [((0, 0), (1, 3))]))
            .GiveItems(0, ("iron", 40));
        int former = Former(w), furnace = w.M.Grid.MachinesOf("furnace").Single().Id;
        w.Do(new Craft(former, "gear", 2)).Run();
        w.Do(new SetEndless(former, 1, true)).Do(new SetEndless(former, 7, true)).Do(new SetEndless(furnace, 1, true)).Do(new SetEndless(999, 1, true)).Run();
        Assert.Equal(["crafting.paid", "crafting.noEntry", "crafting.notHere", "crafting.noStation"], Reasons(w));
        Assert.False(w.M.Crafting.Queue(former).Single().Endless);
    }

    [Fact]
    public void RefusesACancelWhoseUnitBegunDoesNotFitTheCancellersInventory()
    {
        var w = Feed(World(), 10);
        int former = Former(w);
        w.Do(new Craft(former, "plate", 1, FromInputs: true)).Run();
        w.GiveItems(2, ("canister", Content.Inventory.Slots * Content.Items.Get("canister").Stack));
        w.Do(new Cancel(former, 1) { From = 2 }).Run();
        Assert.Equal(["items.full"], Reasons(w));
        Assert.Single(w.M.Crafting.Queue(former));
    }
}
