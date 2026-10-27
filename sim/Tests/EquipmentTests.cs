using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Equipment (0047): the slots after an inventory's own, each wearing only items of its kind.</summary>
public class EquipmentTests
{
    private const string Jetpack = "jetpack";
    private static readonly int Head = Inventory.Slots, Back = Inventory.Slots + 1, Belt = Inventory.Slots + 2;

    private static Items.Place Own(int slot) => new(slot);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static IReadOnlyList<Stack?> Slots(Scenario w, int peer = 0) => w.M.Items.Inventory(peer);

    private static Scenario Move(Scenario w, Items.Place src, Target dst, int count = 999, int from = 0) =>
        w.Do(new Move(src, dst, count) { From = from }).Run();

    /// <summary>A crate, machine 1, and peer 0 carrying a jetpack in its first slot and scrap in its second.</summary>
    private static Scenario Carrying() => new Scenario("#KK....#").GiveItems(0, (Jetpack, 1), ("scrap", 30));

    [Fact]
    public void EveryInventoryHasItsOwnSlotsThenAHeadABackThreeBeltSlotsTheBodyAndTheLampLast()
    {
        Assert.Equal(["head", "back", "belt", "belt", "belt", "suit", "lamp"], Inventory.Equipment);
        Assert.Equal(Inventory.Slots + 7, Inventory.Total);
        Assert.Equal(Inventory.Total, Slots(new Scenario(Layout.Empty), 3).Count);
        Assert.Equal("back", Content.Items.Get(Jetpack).Equip);
    }

    [Fact]
    public void WearsAJetpackOnTheBackAndNowhereElse()
    {
        var w = Carrying();
        Move(w, Own(0), new Target(Head));
        Move(w, Own(0), new Target(Belt));
        Assert.Equal(["items.notEquipment", "items.notEquipment"], Reasons(w));
        Assert.False(w.M.Items.Equipped(0, Jetpack));
        Move(w, Own(0), new Target(Back));
        Assert.Equal(2, w.Refusals.Count);
        Assert.Equal(new Stack(Jetpack, 1), Slots(w)[Back]);
        Assert.Null(Slots(w)[0]);
        Assert.True(w.M.Items.Equipped(0, Jetpack));
        Assert.False(w.M.Items.Equipped(1, Jetpack));
    }

    [Fact]
    public void RefusesAnythingButEquipmentInAnEquipmentSlotAndChangesNothing()
    {
        var w = Carrying();
        var before = Slots(w).ToArray();
        Move(w, Own(1), new Target(Back));
        Move(w, Own(1), new Target(Head), 1);
        Assert.Equal(["items.notEquipment", "items.notEquipment"], Reasons(w));
        Assert.Equal(before, Slots(w));
    }

    [Fact]
    public void RefusesASwapThatWouldLeaveAnythingElseOnTheBackEitherWay()
    {
        var w = Carrying();
        Move(w, Own(0), new Target(Back));
        // The scrap onto the jetpack worn, or the jetpack worn onto the scrap: either swap puts scrap on the back.
        Move(w, Own(1), new Target(Back));
        Move(w, Own(Back), new Target(1));
        Assert.Equal(["items.notEquipment", "items.notEquipment"], Reasons(w));
        Assert.Equal(new Stack(Jetpack, 1), Slots(w)[Back]);
    }

    [Fact]
    public void TakesAJetpackOffIntoAnyOwnSlotOrACrate()
    {
        var w = Carrying();
        Move(w, Own(0), new Target(Back));
        Move(w, Own(Back), new Target(7));
        Assert.Equal(new Stack(Jetpack, 1), Slots(w)[7]);
        Move(w, Own(7), new Target(Back));
        Move(w, Own(Back), new Target(Crate: 1));
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack(Jetpack, 1), w.M.Items.Crate(1)[0]);
        Assert.False(w.M.Items.Equipped(0, Jetpack));
    }

    [Fact]
    public void NeverGivesTakesNorCountsWhatIsWorn()
    {
        var w = Carrying();
        Move(w, Own(0), new Target(Back));
        Assert.DoesNotContain(w.M.Items.Totals(0), t => t.Item == Jetpack);
        Assert.Equal("items.notEnough", w.M.Items.ShortOf(0, [(Jetpack, 1)]));
        // Its own slots full, a hero has no room, though its belt is empty.
        var full = new Scenario(Layout.Empty).GiveItems(0, ("canister", Inventory.Slots * Content.Items.Get("canister").Stack));
        Assert.Equal("items.full", full.M.Items.FullFor(0, [(Jetpack, 1)]));
        Assert.All(Slots(full).Skip(Inventory.Slots), Assert.Null);
    }

    [Fact]
    public void NeverPicksUpNorMovesAWholeStackIntoEquipment()
    {
        int canisters = Inventory.Slots * Content.Items.Get("canister").Stack;
        var w = new Scenario("#KK....#\n########").GiveItems(0, ("canister", canisters)).FillCrate(1, (Jetpack, 1));
        // From the crate onto the inventory as a whole: its own slots are full, and the back takes nothing so.
        Move(w, new Items.Place(0, 1), new Target());
        Assert.Equal(["items.full"], Reasons(w));
        w.M.Items.DropAt(FloorStack.Nobody, [(Jetpack, 1)], w.At(4, 0));
        int id = w.M.Items.Floor[0].Id;
        Assert.Equal("items.full", w.M.Items.PickUpProblem(0, id));
        w.Do(new PickUp(id) { From = 0 }).Run();
        Assert.Equal(["items.full", "items.full"], Reasons(w));
        Assert.All(Slots(w).Skip(Inventory.Slots), Assert.Null);
    }

    [Fact]
    public void ShowsAMoveInFlightOntoTheWrongSlotAsTheSimWouldRefuseIt()
    {
        var w = Carrying();
        var shown = w.M.Items.AfterMoves(0, [new Move(Own(1), new Target(Back), 30) { From = 0 }, new Move(Own(0), new Target(Back), 1) { From = 0 }]);
        Assert.Equal(new Stack("scrap", 30), shown.Inventory[1]);
        Assert.Equal(new Stack(Jetpack, 1), shown.Inventory[Back]);
    }

    /// <summary>A slot of the inventory, its equipment most of all, or of the crate.</summary>
    private static readonly Gen<Items.Place> Anywhere = Gen.Frequency(
        (3, Gen.Choose(Inventory.Slots, Inventory.Total - 1).Select(Own)),
        (2, Gen.Choose(0, 3).Select(Own)),
        (1, Gen.Choose(0, 2).Select(slot => new Items.Place(slot, 1))));

    [Property(MaxTest = 200)]
    public Property EquipmentSlotsHoldOnlyWhatIsWornInThem()
    {
        var step = from src in Anywhere
                   from dst in Gen.Frequency((4, Anywhere.Select(p => (Target)p)), (1, Gen.Constant(new Target())), (1, Gen.Constant(new Target(Crate: 1))))
                   from count in Gen.Elements(1, 999)
                   select new Move(src, dst, count) { From = 0 };
        var steps = from n in Gen.Choose(0, 60) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), moves =>
        {
            var w = new Scenario("#KK....#").GiveItems(0, (Jetpack, 1), ("scrap", 30), ("canister", 2)).FillCrate(1, (Jetpack, 1), ("iron", 5));
            foreach (var m in moves)
            {
                w.Do(m).Run();
                var slots = Slots(w);
                for (int i = 0; i < Inventory.Equipment.Count; i++)
                    if (slots[Inventory.Slots + i] is { } s)
                        Assert.Equal(Inventory.Equipment[i], Content.Items.Get(s.Item).Equip);
            }
        });
    }
}
