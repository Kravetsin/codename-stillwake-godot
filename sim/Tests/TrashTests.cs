using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The trash (trash-slot): one slot after the equipment; a stack moved in replaces what it held, which is gone.</summary>
public class TrashTests
{
    private static readonly Items.Place Trash = new(Inventory.Trash);

    private static Items.Place Own(int slot) => new(slot);
    private static Items.Place In(int crate, int slot) => new(slot, crate);
    private static IReadOnlyList<Stack?> Slots(Scenario w, int peer = 0) => w.M.Items.Inventory(peer);
    private static int Count(Scenario w, string item, int peer = 0) => w.M.Items.Totals(peer).Where(t => t.Item == item).Sum(t => t.Count);

    private static Scenario Move(Scenario w, Items.Place src, Target dst, int count = 999, int from = 0) =>
        w.Do(new Move(src, dst, count) { From = from }).Run();

    /// <summary>A crate, machine 1, and peer 0 carrying a jetpack in its first slot and scrap in its second.</summary>
    private static Scenario Carrying() => new Scenario("#KK....#").GiveItems(0, ("jetpack", 1), ("scrap", 30));

    [Fact]
    public void IsTheLastSlotOfEveryInventoryAfterItsEquipment()
    {
        Assert.Equal(Inventory.Slots + Inventory.Equipment.Count, Inventory.Trash);
        Assert.Equal(Inventory.Trash + 1, Inventory.Total);
        Assert.Equal(Inventory.Total, Slots(new Scenario(Layout.Empty), 3).Count);
        Assert.True(new Scenario(Layout.Empty).M.Items.SlotOpen(0, Trash));
    }

    [Fact]
    public void AStackMovedIntoTheEmptyTrashLiesThereAndIsNoLongerCarried()
    {
        var w = Move(Carrying(), Own(1), Trash);
        Assert.Empty(w.Refusals);
        Assert.Null(Slots(w)[1]);
        Assert.Equal(new Stack("scrap", 30), Slots(w)[Inventory.Trash]);
        Assert.Equal(0, Count(w, "scrap"));
        Assert.Equal("items.notEnough", w.M.Items.ShortOf(0, [("scrap", 1)]));
    }

    [Fact]
    public void TheNextStackReplacesWhatTheTrashHeldWhichIsGone()
    {
        var w = Move(Move(Carrying(), Own(0), Trash), Own(1), Trash);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack("scrap", 30), Slots(w)[Inventory.Trash]);
        Assert.Null(Slots(w)[0]);
        Assert.Null(Slots(w)[1]);
        Assert.DoesNotContain(Slots(w), s => s?.Item == "jetpack");
    }

    [Fact]
    public void PartOfAStackGoesInAndAnAlikeOneReplacesItRatherThanMerging()
    {
        var w = Move(Carrying(), Own(1), Trash, 10);
        Assert.Equal(new Stack("scrap", 10), Slots(w)[Inventory.Trash]);
        Assert.Equal(new Stack("scrap", 20), Slots(w)[1]);
        Move(w, Own(1), Trash, 5);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack("scrap", 5), Slots(w)[Inventory.Trash]);
        Assert.Equal(new Stack("scrap", 15), Slots(w)[1]);
    }

    [Fact]
    public void AStackIsTakenBackOutOfTheTrashIntoASlotOrACrate()
    {
        var w = Move(Move(Carrying(), Own(1), Trash), Trash, new Target(5), 10);
        Assert.Equal(new Stack("scrap", 10), Slots(w)[5]);
        Assert.Equal(new Stack("scrap", 20), Slots(w)[Inventory.Trash]);
        Move(w, Trash, new Target(Crate: 1));
        Assert.Empty(w.Refusals);
        Assert.Null(Slots(w)[Inventory.Trash]);
        Assert.Equal(new Stack("scrap", 20), w.M.Items.Crate(1)[0]);
    }

    [Fact]
    public void AStackGoesFromAnOpenCrateStraightIntoTheTrash()
    {
        var w = new Scenario("#KK....#").FillCrate(1, ("iron", 12));
        Move(w, In(1, 0), Trash);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack("iron", 12), Slots(w)[Inventory.Trash]);
        Assert.DoesNotContain(w.M.Items.Crate(1), s => s != null);
    }

    [Fact]
    public void ATrashedStackIsNeverWornBySwappingIntoEquipment()
    {
        // The scrap from the trash onto the worn back would swap the jetpack into the trash and scrap onto the back.
        var w = Move(Move(Carrying(), Own(0), new Target(Inventory.Slots + 1)), Own(1), Trash);
        Move(w, Trash, new Target(Inventory.Slots + 1));
        Assert.Equal(["items.notEquipment"], w.Refusals.Select(r => r.Reason));
        Assert.True(w.M.Items.Equipped(0, "jetpack"));
    }

    [Fact]
    public void NothingButAMoveNamingItReachesTheTrash()
    {
        var w = Move(Carrying(), Own(1), Trash).FillCrate(1, ("scrap", 5));
        // Onto the inventory as a whole: into its own slots, never the trash.
        Move(w, In(1, 0), new Target());
        Assert.Equal(5, Count(w, "scrap"));
        w.Do(new Sort(null)).Run();
        w.Do(new Stash(0, [])).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack("scrap", 30), Slots(w)[Inventory.Trash]);
        Assert.False(w.M.Items.Equipped(0, "scrap"));
    }

    [Fact]
    public void ADeadHeroLeavesTheTrashInItsSlot()
    {
        var w = DeathTests.Room().GiveItems(0, ("scrap", 30), ("iron", 4));
        Move(w, Own(0), Trash);
        DeathTests.DieAt(w, DeathTests.Inside);
        Assert.Equal(new Stack("scrap", 30), Slots(w)[Inventory.Trash]);
        Assert.DoesNotContain(w.M.Items.Floor, f => f.Item == "scrap");
        Assert.Contains(w.M.Items.Floor, f => f.Item == "iron");
    }

    [Fact]
    public void TheTrashOutlastsASaveAndLoad()
    {
        var w = Move(Carrying(), Own(1), Trash).Reload();
        Assert.Equal(new Stack("scrap", 30), Slots(w)[Inventory.Trash]);
        Move(w, Trash, new Target(1));
        Assert.Equal(30, Count(w, "scrap"));
    }

    /// <summary>Everything every peer's inventory and both crates hold, item by item.</summary>
    private static Dictionary<string, int> Everything(Scenario w)
    {
        var all = Enumerable.Range(0, 3).SelectMany(p => w.M.Items.Inventory(p)).Concat(w.M.Items.Crate(1)).Concat(w.M.Items.Crate(2));
        return all.OfType<Stack>().GroupBy(s => s.Item, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Sum(s => s.Count), StringComparer.Ordinal);
    }

    [Property(MaxTest = 200)]
    public Property AMoveMakesNothingAndLosesOnlyWhatTheTrashItFilledHeld()
    {
        // Every slot of three peers and two crates, the trash often, as source and target.
        var place = Gen.OneOf(
            Gen.Frequency((2, Gen.Constant(Inventory.Trash)), (3, Gen.Choose(0, 3)), (1, Gen.Choose(0, Inventory.Total - 1))).Select(Own),
            from crate in Gen.Choose(1, 2) from slot in Gen.Choose(0, 3) select In(crate, slot));
        var step = from src in place
                   from dst in Gen.OneOf(place.Select(p => (Target)p), Gen.Constant(new Target()), Gen.Choose(1, 2).Select(c => new Target(Crate: c)))
                   from count in Gen.OneOf(Gen.Constant(999), Gen.Choose(1, 40))
                   from peer in Gen.Choose(0, 2)
                   select new Move(src, dst, count) { From = peer };
        var steps = from n in Gen.Choose(0, 60) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), moves =>
        {
            var w = new Scenario("#KK.KK#").FillCrate(1, ("scrap", 30), ("iron", 20), ("canister", 3))
                .GiveItems(1, ("canister", 9), ("iron", 600)).GiveItems(2, ("jetpack", 1));
            foreach (var m in moves)
            {
                var before = Everything(w);
                int refused = w.Refusals.Count;
                var lost = m.Dst is { Crate: null, Slot: int slot } && slot == Inventory.Trash ? Slots(w, m.From)[Inventory.Trash] : null;
                w.Do(m).Run();
                if (w.Refusals.Count == refused && lost != null) before[lost.Item] -= lost.Count;
                Assert.Equal(before.Where(e => e.Value > 0).OrderBy(e => e.Key, StringComparer.Ordinal), Everything(w).OrderBy(e => e.Key, StringComparer.Ordinal));
            }
        });
    }
}
