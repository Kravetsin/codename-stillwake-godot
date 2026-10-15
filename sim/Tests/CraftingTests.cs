using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using CraftingSpec = Stillwake.Content.Crafting;
using Crafts = Stillwake.Content.Crafts;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>The crafting module: each crafting machine's shared queue, paid when queued, made a unit at a time into its outputs.</summary>
public class CraftingTests
{
    /// <summary>A room with the lifepod and a crate, no power anywhere.</summary>
    private const string Room = """
        ##########
        #........#
        #.OOO....#
        #.OOO....#
        #.OOOKK..#
        ##########
        """;
    /// <summary>The pod's machine id and the crate's, by their top-left cells in reading order.</summary>
    private const int Pod = 1, Crate = 2;
    private static readonly int Wire = Crafts.Get("wire").Ticks * 1000 / Crafts.PodSpeed;
    private static readonly int WireScrap = Crafts.Get("wire").Cost.Single().Count;
    private static readonly int Outputs = Content.Machines.Get("lifepod").Slots!.Value;

    /// <summary>A room knowing every blueprint and craft, or what the wreck grants when <paramref name="start"/>.</summary>
    private static Scenario World(bool start = false)
    {
        var layout = Layout.Parse(Room);
        return new Scenario(start ? layout with { Known = Content.Research.StartKnown } : layout);
    }

    private static int Count(Scenario w, string item, int peer = 0) =>
        w.M.Items.Inventory(peer).Sum(s => s?.Item == item ? s.Count : 0);

    private static int Made(Scenario w, string item) => w.M.Items.Crate(Pod).Sum(s => s?.Item == item ? s.Count : 0);

    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static Scenario Craft(Scenario w, string recipe, int count = 1, int from = 0, int at = Pod) =>
        w.Do(new Craft(at, recipe, count) { From = from }).Run();
    private static Scenario Cancel(Scenario w, int entry, int from = 0, int at = Pod) => w.Do(new Cancel(at, entry) { From = from }).Run();

    [Fact]
    public void MakesAWireAtThePodInTwiceItsTicksWithNoPowerPayingWhenQueued()
    {
        var w = World().GiveItems(0, ("scrap", 10));
        Assert.Equal(2 * Crafts.Get("wire").Ticks, Wire);
        Assert.Empty(w.M.Power.Networks);
        // The tick of the craft is the first of its work.
        Craft(w, "wire");
        Assert.Equal(10 - WireScrap, Count(w, "scrap"));
        Assert.Equal([new Entry(1, "wire", 1, 0)], w.M.Crafting.Queue(Pod));
        w.Run(Wire - 2);
        Assert.Equal((Wire - 1) * Crafts.PodSpeed, w.M.Crafting.Progress(Pod));
        Assert.Equal(0, Made(w, "wire"));
        w.Run();
        Assert.Equal(1, Made(w, "wire"));
        Assert.Equal(0, Count(w, "wire"));
        Assert.Empty(w.M.Crafting.Queue(Pod));
        Assert.Equal(0, w.M.Crafting.Progress(Pod));
    }

    [Fact]
    public void MakesFiveQueuedAtOnceOneByOneEachInItsOwnTicks()
    {
        var w = World().GiveItems(0, ("scrap", 10));
        Craft(w, "wire", 5);
        Assert.Equal(0, Count(w, "scrap"));
        w.Run(Wire - 1);
        for (int made = 1; made <= 5; made++)
        {
            Assert.Equal(made, Made(w, "wire"));
            if (made < 5) Assert.Equal([new Entry(1, "wire", 5 - made, 0)], w.M.Crafting.Queue(Pod));
            w.Run(Wire);
        }
        Assert.Empty(w.M.Crafting.Queue(Pod));
        Assert.Equal(5, Made(w, "wire"));
    }

    [Fact]
    public void MakesAPartOfOtherPartsTakingThemAll()
    {
        var w = World().GiveItems(0, ("gear", 1), ("wire", 2));
        Craft(w, "rotor");
        Assert.Equal((0, 0), (Count(w, "gear"), Count(w, "wire")));
        w.Run(Crafts.Get("rotor").Ticks * 1000 / Crafts.PodSpeed - 1);
        Assert.Equal(1, Made(w, "rotor"));
    }

    [Fact]
    public void GivesWhatIsMadeToWhoeverTakesItFromTheOutputs()
    {
        var w = World().GiveItems(0, ("scrap", 4));
        Craft(w, "wire", 2).Run(2 * Wire);
        w.Do(new Move(new Slot(0, Pod), new Target(), 99) { From = 3 }).Run();
        Assert.Equal((0, 2), (Made(w, "wire"), Count(w, "wire", 3)));
    }

    [Fact]
    public void RefusesACraftOrCancelAtAMachineThatDoesNotCraftOrIsNotThere()
    {
        var w = World().GiveItems(0, ("scrap", 10));
        Craft(w, "wire", at: Crate);
        Craft(w, "wire", at: 9);
        Cancel(w, 1, at: Crate);
        Assert.Equal(["crafting.noStation", "crafting.noStation", "crafting.noStation"], Reasons(w));
        Assert.Equal(10, Count(w, "scrap"));
        Assert.Empty(w.M.Crafting.Queue(Crate));
    }

    [Fact]
    public void RefusesACraftNotKnownOrNoneAtAllTakingNothing()
    {
        var w = World(start: true).GiveItems(0, ("iron", 10));
        Craft(w, "plate");
        Craft(w, "iron");
        Assert.Equal(["crafting.unknown", "crafting.unknown"], Reasons(w));
        Assert.Equal(10, Count(w, "iron"));
        Assert.Empty(w.M.Crafting.Queue(Pod));
    }

    [Fact]
    public void RefusesTenWiresWithScrapForSixWholeQueuingNothing()
    {
        var w = World().GiveItems(0, ("scrap", 6 * WireScrap));
        Craft(w, "wire", 10);
        Assert.Equal(["items.notEnough"], Reasons(w));
        Assert.Equal(6 * WireScrap, Count(w, "scrap"));
        Assert.Empty(w.M.Crafting.Queue(Pod));
    }

    [Fact]
    public void RefusesACraftPastTheMostTheSharedQueueHoldsWhoeverSendsIt()
    {
        var w = World().GiveItems(0, ("scrap", 100)).GiveItems(1, ("scrap", 100));
        for (int i = 0; i < CraftingSpec.QueueMost; i++) w.Do(new Craft(Pod, "wire", 1) { From = i % 2 });
        w.Run();
        Craft(w, "wire", from: 1);
        Assert.Equal(["crafting.full"], Reasons(w));
        Assert.Equal(CraftingSpec.QueueMost, w.M.Crafting.Queue(Pod).Count);
        Assert.Equal(200 - CraftingSpec.QueueMost * WireScrap, Count(w, "scrap", 0) + Count(w, "scrap", 1));
    }

    [Fact]
    public void CancelGivesBackTheWholeEntryTheUnitAtWorkIncluded()
    {
        var w = World().GiveItems(0, ("scrap", 6));
        Craft(w, "wire", 3);
        w.Run(Wire / 2);
        Cancel(w, 1);
        Assert.Empty(w.Refusals);
        Assert.Equal((6, 0), (Count(w, "scrap"), Made(w, "wire")));
        Assert.Empty(w.M.Crafting.Queue(Pod));
        Assert.Equal(0, w.M.Crafting.Progress(Pod));
    }

    [Fact]
    public void CancelOfALaterEntryLeavesTheOneAtWorkWhereItWas()
    {
        var w = World().GiveItems(0, ("scrap", 6), ("iron", 2));
        Craft(w, "wire").Do(new Craft(Pod, "gear", 2)).Run(4);
        Cancel(w, 2);
        Assert.Equal(6 * Crafts.PodSpeed, w.M.Crafting.Progress(Pod));
        Assert.Equal([new Entry(1, "wire", 1, 0)], w.M.Crafting.Queue(Pod));
        Assert.Equal(2, Count(w, "iron"));
    }

    [Fact]
    public void TwoPeersQueueAtThePodAndOneCancelsTheOthersEntryGettingItsCost()
    {
        var w = World().GiveItems(1, ("scrap", 2 * WireScrap)).GiveItems(2, ("iron", 2));
        Craft(w, "wire", 2, from: 1);
        Craft(w, "gear", 2, from: 2);
        Assert.Equal([new Entry(1, "wire", 2, 1), new Entry(2, "gear", 2, 2)], w.M.Crafting.Queue(Pod));
        Cancel(w, 1, from: 2);
        Assert.Empty(w.Refusals);
        Assert.Equal((0, 2 * WireScrap), (Count(w, "scrap", 1), Count(w, "scrap", 2)));
        Assert.Equal([new Entry(2, "gear", 2, 2)], w.M.Crafting.Queue(Pod));
    }

    [Fact]
    public void RefusesACancelOfNoEntryOneFinishedAndOneWhoseRefundDoesNotFit()
    {
        var w = World().GiveItems(0, ("scrap", 10));
        Craft(w, "wire").Run(Wire);
        Cancel(w, 1);
        Cancel(w, 7);
        Assert.Equal(["crafting.noEntry", "crafting.noEntry"], Reasons(w));

        // Peer 2's inventory full: the four scrap of the two wires would not fit.
        Craft(w, "wire", 2);
        w.GiveItems(2, ("canister", Content.Inventory.Slots * Content.Items.Get("canister").Stack));
        Cancel(w, 2, from: 2);
        Assert.Equal("items.full", Reasons(w)[^1]);
        Assert.Single(w.M.Crafting.Queue(Pod));
    }

    [Fact]
    public void KeepsAnEntrysIdWhileThoseBeforeItFinishNeverGivingAnIdTwice()
    {
        var w = World().GiveItems(0, ("scrap", 20));
        Craft(w, "wire").Do(new Craft(Pod, "wire", 2)).Run(Wire);
        Assert.Equal([new Entry(2, "wire", 2, 0)], w.M.Crafting.Queue(Pod));
        Cancel(w, 2);
        Craft(w, "wire");
        Assert.Equal([new Entry(3, "wire", 1, 0)], w.M.Crafting.Queue(Pod));
    }

    [Fact]
    public void HoldsAFinishedUnitWhileTheOutputsAreFullUntilASlotIsTakenFrom()
    {
        var w = World().GiveItems(0, ("scrap", 2), ("iron", 3), ("gear", 1), ("wire", 2));
        // A gear, a plate and a rotor fill an output each: the wire finds no room.
        Assert.Equal(3, Outputs);
        foreach (var part in new[] { "gear", "plate", "rotor", "wire" }) w.Do(new Craft(Pod, part, 1));
        w.Run(20 * Wire);
        Assert.Equal(0, Made(w, "wire"));
        Assert.True(w.M.Crafting.Progress(Pod) > 0);
        Assert.Equal(Crafts.Get("wire").Ticks * 1000, w.M.Crafting.Progress(Pod));
        Assert.Single(w.M.Crafting.Queue(Pod));
        Assert.All(w.M.Items.Crate(Pod), s => Assert.NotNull(s));
        w.Do(new Move(new Slot(0, Pod), new Target(), 99)).Run();
        Assert.Equal(1, Made(w, "wire"));
        Assert.Equal("wire", w.M.Items.Crate(Pod)[0]?.Item);
        Assert.Empty(w.M.Crafting.Queue(Pod));
    }

    [Fact]
    public void CraftsForFreeInCreativeModeKnowingEverything()
    {
        var w = World(start: true);
        w.Do(new SetCreative(true)).Run();
        Craft(w, "rotor", 2);
        Assert.Empty(w.Refusals);
        w.Run(2 * Crafts.Get("rotor").Ticks * 1000 / Crafts.PodSpeed);
        Assert.Equal(2, Made(w, "rotor"));
    }

    [Fact]
    public void SavesAndLoadsAQueuePartWayThroughAUnit()
    {
        var w = World().GiveItems(0, ("scrap", 10)).GiveItems(1, ("iron", 2));
        Craft(w, "wire", 2).Do(new Craft(Pod, "gear", 1) { From = 1 }).Run(Wire + 3);
        var before = w.World.Save();
        w.Reload();
        Assert.Equal(before, w.World.Save());
        Assert.Equal([new Entry(1, "wire", 1, 0), new Entry(2, "gear", 1, 1)], w.M.Crafting.Queue(Pod));
        Assert.Equal(4 * Crafts.PodSpeed, w.M.Crafting.Progress(Pod));
    }

    // Invariants.

    /// <summary>A craft, a cancel, an output taken from, then ticks run, by one peer.</summary>
    private sealed record Step(int Kind, string Recipe, int Count, int From, int Ticks);

    private static readonly string[] Recipes = [.. Crafts.All.Select(c => c.Item)];

    /// <summary>What <paramref name="item"/> is made of, down to what no craft makes: its worth in raw items.</summary>
    private static Dictionary<string, int> Raw(string item, int count, Dictionary<string, int>? into = null)
    {
        into ??= new Dictionary<string, int>(StringComparer.Ordinal);
        if (Crafts.TryGet(item, out var craft))
            foreach (var (part, n) in craft.Cost) Raw(part, n * count, into);
        else into[item] = into.GetValueOrDefault(item) + count;
        return into;
    }

    /// <summary>The raw worth of every inventory, every queued unit, the pod's outputs and the floor: what crafting never changes.</summary>
    private static string Worth(Scenario w)
    {
        var all = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int peer = 0; peer < 3; peer++)
            foreach (var s in w.M.Items.Inventory(peer)) if (s != null) Raw(s.Item, s.Count, all);
        foreach (var e in w.M.Crafting.Queue(Pod)) Raw(e.Recipe, e.Count, all);
        foreach (var s in w.M.Items.Crate(Pod)) if (s != null) Raw(s.Item, s.Count, all);
        foreach (var s in w.M.Items.Floor) Raw(s.Item, s.Count, all);
        return string.Join(",", all.Where(a => a.Value != 0).OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => $"{a.Key}:{a.Value}"));
    }

    /// <summary>
    /// The README's invariants: a queue never holds more than <c>QueueMost</c>; its first entry's progress
    /// stays below its work but while its unit waits for room; ids only grow; and items are conserved,
    /// what a craft took coming back as its units or a cancel's refund, whoever cancels.
    /// </summary>
    [Property(MaxTest = 60)]
    public Property KeepsTheQueueBoundedProgressInItsWorkIdsGrowingAndItemsConserved()
    {
        var step =
            from kind in Gen.Frequency((5, Gen.Constant(0)), (3, Gen.Constant(1)), (2, Gen.Constant(2)))
            from recipe in Gen.Elements(Recipes)
            from count in Gen.Choose(1, 9)
            from peer in Gen.Choose(0, 2)
            from ticks in Gen.Frequency((3, Gen.Choose(0, 10)), (1, Gen.Choose(10, 400)))
            select new Step(kind, recipe, count, peer, ticks);
        var steps = from n in Gen.Choose(0, 40) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = World();
            for (int peer = 0; peer < 3; peer++) w.GiveItems(peer, ("scrap", 60), ("iron", 30), ("gear", 2), ("wire", 4));
            string worth = Worth(w);
            int seen = 0;
            foreach (var s in all)
            {
                switch (s.Kind)
                {
                    case 0:
                        w.Do(new Craft(Pod, s.Recipe, s.Count) { From = s.From });
                        break;
                    case 1:
                        w.Do(new Cancel(Pod, s.Count) { From = s.From });
                        break;
                    default:
                        // Now and then an output emptied into an inventory: else the outputs fill and units wait.
                        w.Do(new Move(new Slot(s.Count % Outputs, Pod), new Target(), 999) { From = s.From });
                        break;
                }
                for (int t = 0; t <= s.Ticks; t++)
                {
                    w.Run();
                    var queue = w.M.Crafting.Queue(Pod);
                    if (queue.Count > CraftingSpec.QueueMost) return false;
                    int progress = w.M.Crafting.Progress(Pod);
                    if (queue.Count == 0)
                    {
                        if (progress != 0) return false;
                    }
                    else
                    {
                        var first = Crafts.Get(queue[0].Recipe);
                        int work = first.Ticks * 1000;
                        if (progress > work) return false;
                        if (progress == work && w.M.Items.OutputFull(Pod, [(first.Item, 1)]) == null) return false;
                        for (int i = 1; i < queue.Count; i++)
                            if (queue[i].Id <= queue[i - 1].Id) return false;
                        if (queue[0].Id < seen || queue.Any(e => e.Count < 1)) return false;
                        seen = queue[0].Id;
                    }
                    if (Worth(w) != worth) return false;
                }
            }
            return true;
        });
    }
}
