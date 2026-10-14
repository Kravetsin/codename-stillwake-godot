using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using CraftingSpec = Stillwake.Content.Crafting;
using Crafts = Stillwake.Content.Crafts;

namespace Stillwake.Sim.Tests;

/// <summary>The crafting module: queues paid when queued, made a unit at a time into the inventory.</summary>
public class CraftingTests
{
    private const string Room = """
        ######
        #....#
        ######
        """;
    private static readonly int Wire = Crafts.Get("wire").Ticks;
    private static readonly int WireScrap = Crafts.Get("wire").Cost.Single().Count;

    /// <summary>A room knowing every blueprint and craft, or what the wreck grants when <paramref name="start"/>.</summary>
    private static Scenario World(bool start = false)
    {
        var layout = Layout.Parse(Room);
        return new Scenario(start ? layout with { Known = Content.Research.StartKnown } : layout);
    }

    private static int Count(Scenario w, string item, int peer = 0) =>
        w.M.Items.Inventory(peer).Sum(s => s?.Item == item ? s.Count : 0);

    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static Scenario Craft(Scenario w, string recipe, int count = 1, int from = 0) =>
        w.Do(new Craft(recipe, count) { From = from }).Run();
    private static Scenario Cancel(Scenario w, int entry, int from = 0) => w.Do(new Cancel(entry) { From = from }).Run();

    [Fact]
    public void MakesAWireInItsTicksPayingForItWhenQueued()
    {
        var w = World().GiveItems(0, ("scrap", 10));
        // The tick of the craft is the first of its work.
        Craft(w, "wire");
        Assert.Equal(10 - WireScrap, Count(w, "scrap"));
        Assert.Equal([new Entry(1, "wire", 1)], w.M.Crafting.Queue(0));
        w.Run(Wire - 2);
        Assert.Equal(Wire - 1, w.M.Crafting.Progress(0));
        Assert.Equal(0, Count(w, "wire"));
        w.Run();
        Assert.Equal(1, Count(w, "wire"));
        Assert.Empty(w.M.Crafting.Queue(0));
        Assert.Equal(0, w.M.Crafting.Progress(0));
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
            Assert.Equal(made, Count(w, "wire"));
            if (made < 5) Assert.Equal([new Entry(1, "wire", 5 - made)], w.M.Crafting.Queue(0));
            w.Run(Wire);
        }
        Assert.Empty(w.M.Crafting.Queue(0));
        Assert.Equal(5, Count(w, "wire"));
    }

    [Fact]
    public void MakesAPartOfOtherPartsTakingThemAll()
    {
        var w = World().GiveItems(0, ("gear", 1), ("wire", 2));
        Craft(w, "rotor");
        Assert.Equal((0, 0), (Count(w, "gear"), Count(w, "wire")));
        w.Run(Crafts.Get("rotor").Ticks - 1);
        Assert.Equal(1, Count(w, "rotor"));
    }

    [Fact]
    public void RefusesACraftNotKnownOrNoneAtAllTakingNothing()
    {
        var w = World(start: true).GiveItems(0, ("iron", 10));
        Craft(w, "plate");
        Craft(w, "iron");
        Assert.Equal(["crafting.unknown", "crafting.unknown"], Reasons(w));
        Assert.Equal(10, Count(w, "iron"));
        Assert.Empty(w.M.Crafting.Queue(0));
    }

    [Fact]
    public void RefusesTenWiresWithScrapForSixWholeQueuingNothing()
    {
        var w = World().GiveItems(0, ("scrap", 6 * WireScrap));
        Craft(w, "wire", 10);
        Assert.Equal(["items.notEnough"], Reasons(w));
        Assert.Equal(6 * WireScrap, Count(w, "scrap"));
        Assert.Empty(w.M.Crafting.Queue(0));
    }

    [Fact]
    public void RefusesACraftPastTheMostAQueueHolds()
    {
        var w = World().GiveItems(0, ("scrap", 100));
        for (int i = 0; i < CraftingSpec.QueueMost; i++) w.Do(new Craft("wire", 1));
        w.Run();
        Craft(w, "wire");
        Assert.Equal(["crafting.full"], Reasons(w));
        Assert.Equal(CraftingSpec.QueueMost, w.M.Crafting.Queue(0).Count);
        Assert.Equal(100 - CraftingSpec.QueueMost * WireScrap, Count(w, "scrap"));
    }

    [Fact]
    public void CancelGivesBackTheWholeEntryTheUnitAtWorkIncluded()
    {
        var w = World().GiveItems(0, ("scrap", 6));
        Craft(w, "wire", 3);
        w.Run(Wire / 2);
        Cancel(w, 1);
        Assert.Empty(w.Refusals);
        Assert.Equal((6, 0), (Count(w, "scrap"), Count(w, "wire")));
        Assert.Empty(w.M.Crafting.Queue(0));
        Assert.Equal(0, w.M.Crafting.Progress(0));
    }

    [Fact]
    public void CancelOfALaterEntryLeavesTheOneAtWorkWhereItWas()
    {
        var w = World().GiveItems(0, ("scrap", 6), ("iron", 2));
        Craft(w, "wire").Do(new Craft("gear", 2)).Run(4);
        Cancel(w, 2);
        Assert.Equal(6, w.M.Crafting.Progress(0));
        Assert.Equal([new Entry(1, "wire", 1)], w.M.Crafting.Queue(0));
        Assert.Equal(2, Count(w, "iron"));
    }

    [Fact]
    public void RefusesACancelOfNoEntryOneFinishedOrAnothersAndOneWhoseRefundDoesNotFit()
    {
        var w = World().GiveItems(0, ("scrap", 10)).GiveItems(1, ("scrap", 2));
        Craft(w, "wire").Run(Wire);
        Cancel(w, 1);
        Cancel(w, 7);
        Craft(w, "wire", 1, from: 1);
        Cancel(w, 1, from: 0);
        Assert.Equal(["crafting.noEntry", "crafting.noEntry", "crafting.noEntry"], Reasons(w));
        Assert.Single(w.M.Crafting.Queue(1));

        // Peer 2 queues two wires, then its inventory fills: the four scrap would not fit.
        w.GiveItems(2, ("scrap", 2 * WireScrap));
        Craft(w, "wire", 2, from: 2);
        w.GiveItems(2, ("canister", Content.Inventory.Slots * Content.Items.Get("canister").Stack));
        Cancel(w, 1, from: 2);
        Assert.Equal("items.full", Reasons(w)[^1]);
        Assert.Single(w.M.Crafting.Queue(2));
    }

    [Fact]
    public void KeepsAnEntrysIdWhileThoseBeforeItFinishNeverGivingAnIdTwice()
    {
        var w = World().GiveItems(0, ("scrap", 20));
        Craft(w, "wire").Do(new Craft("wire", 2)).Run(Wire);
        Assert.Equal([new Entry(2, "wire", 2)], w.M.Crafting.Queue(0));
        Cancel(w, 2);
        Craft(w, "wire");
        Assert.Equal([new Entry(3, "wire", 1)], w.M.Crafting.Queue(0));
    }

    [Fact]
    public void HoldsAFinishedUnitWhileTheInventoryIsFullUntilASlotIsFree()
    {
        var w = World().GiveItems(0, ("scrap", WireScrap));
        var canisters = Content.Items.Get("canister").Stack;
        w.GiveItems(0, ("canister", (Content.Inventory.Slots - 1) * canisters));
        Craft(w, "wire");
        // What it took freed its slot: filled again, there is no room for the wire.
        w.GiveItems(0, ("canister", canisters));
        w.Run(2 * Wire);
        Assert.Equal(0, Count(w, "wire"));
        Assert.Equal(Wire, w.M.Crafting.Progress(0));
        Assert.Single(w.M.Crafting.Queue(0));
        w.Do(new Drop(new Items.Place(4), canisters, w.At(2, 1), 1)).Run();
        Assert.Equal(1, Count(w, "wire"));
        Assert.Equal("wire", w.M.Items.Inventory(0)[4]?.Item);
        Assert.Empty(w.M.Crafting.Queue(0));
    }

    [Fact]
    public void CraftsForFreeInCreativeModeKnowingEverything()
    {
        var w = World(start: true);
        w.Do(new SetCreative(true)).Run();
        Craft(w, "rotor", 2);
        Assert.Empty(w.Refusals);
        w.Run(2 * Crafts.Get("rotor").Ticks);
        Assert.Equal(2, Count(w, "rotor"));
    }

    [Fact]
    public void CraftsForEveryPeerAtOnceEachIntoItsOwnInventory()
    {
        var w = World().GiveItems(3, ("scrap", 2)).GiveItems(1, ("scrap", 2));
        Craft(w, "wire", 1, from: 3).Run();
        Craft(w, "wire", 1, from: 1);
        w.Run(Wire - 3);
        Assert.Equal((0, 1), (Count(w, "wire", 1), Count(w, "wire", 3)));
        w.Run(2);
        Assert.Equal((1, 1), (Count(w, "wire", 1), Count(w, "wire", 3)));
    }

    [Fact]
    public void SavesAndLoadsQueuesPartWayThroughAUnit()
    {
        var w = World().GiveItems(0, ("scrap", 10), ("iron", 2));
        Craft(w, "wire", 2).Do(new Craft("gear", 1)).Run(Wire + 3);
        var before = w.World.Save();
        w.Reload();
        Assert.Equal(before, w.World.Save());
        Assert.Equal([new Entry(1, "wire", 1), new Entry(2, "gear", 1)], w.M.Crafting.Queue(0));
        Assert.Equal(4, w.M.Crafting.Progress(0));
    }

    // Invariants.

    /// <summary>A craft, a cancel, an inventory filled with canisters or a stack of them dropped, then ticks run, by one peer.</summary>
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

    /// <summary>The raw worth of every inventory, every queued unit and the floor: what crafting never changes.</summary>
    private static string Worth(Scenario w)
    {
        var all = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int peer = 0; peer < 3; peer++)
        {
            foreach (var s in w.M.Items.Inventory(peer)) if (s != null) Raw(s.Item, s.Count, all);
            foreach (var e in w.M.Crafting.Queue(peer)) Raw(e.Recipe, e.Count, all);
        }
        foreach (var s in w.M.Items.Floor) Raw(s.Item, s.Count, all);
        return string.Join(",", all.Where(a => a.Value != 0).OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => $"{a.Key}:{a.Value}"));
    }

    /// <summary>
    /// The README's invariants: a queue never holds more than <c>QueueMost</c>; its first entry's progress
    /// stays below its ticks but while its unit waits for room; ids within a queue only grow; and items are
    /// conserved, what a craft took coming back as its units or a cancel's refund.
    /// </summary>
    [Property(MaxTest = 60)]
    public Property KeepsQueuesBoundedProgressInItsTicksIdsGrowingAndItemsConserved()
    {
        var step =
            from kind in Gen.Frequency((5, Gen.Constant(0)), (3, Gen.Constant(1)), (1, Gen.Constant(2)), (1, Gen.Constant(3)))
            from recipe in Gen.Elements(Recipes)
            from count in Gen.Choose(1, 9)
            from peer in Gen.Choose(0, 2)
            from ticks in Gen.Frequency((3, Gen.Choose(0, 10)), (1, Gen.Choose(10, 200)))
            select new Step(kind, recipe, count, peer, ticks);
        var steps = from n in Gen.Choose(0, 40) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = World();
            for (int peer = 0; peer < 3; peer++) w.GiveItems(peer, ("scrap", 60), ("iron", 30), ("gear", 2), ("wire", 4));
            string worth = Worth(w);
            var seen = new int[3];
            foreach (var s in all)
            {
                switch (s.Kind)
                {
                    case 0:
                        w.Do(new Craft(s.Recipe, s.Count) { From = s.From });
                        break;
                    case 1:
                        w.Do(new Cancel(s.Count) { From = s.From });
                        break;
                    case 2:
                        {
                            // A full inventory: units wait. What fills it is counted in the worth from now on.
                            int empty = w.M.Items.Inventory(s.From).Count(slot => slot == null);
                            w.GiveItems(s.From, ("canister", empty * Content.Items.Get("canister").Stack));
                            worth = Worth(w);
                            break;
                        }
                    default:
                        {
                            int slot = w.M.Items.Inventory(s.From).ToList().FindIndex(x => x?.Item == "canister");
                            if (slot >= 0) w.Do(new Drop(new Items.Place(slot), 99, w.At(1 + s.Count % 4, 1), 1) { From = s.From });
                            break;
                        }
                }
                for (int t = 0; t <= s.Ticks; t++)
                {
                    w.Run();
                    for (int peer = 0; peer < 3; peer++)
                    {
                        var queue = w.M.Crafting.Queue(peer);
                        if (queue.Count > CraftingSpec.QueueMost) return false;
                        int progress = w.M.Crafting.Progress(peer);
                        if (queue.Count == 0)
                        {
                            if (progress != 0) return false;
                            continue;
                        }
                        var first = Crafts.Get(queue[0].Recipe);
                        if (progress > first.Ticks) return false;
                        if (progress == first.Ticks && w.M.Items.FullFor(peer, [(first.Item, 1)]) == null) return false;
                        for (int i = 0; i < queue.Count; i++)
                            if (i > 0 ? queue[i].Id <= queue[i - 1].Id : queue[0].Id < seen[peer]) return false;
                        seen[peer] = queue[0].Id;
                        if (queue.Any(e => e.Count < 1)) return false;
                    }
                    if (Worth(w) != worth) return false;
                }
            }
            return true;
        });
    }
}
