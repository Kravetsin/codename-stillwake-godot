using Stillwake.Game.Features.Items;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Clicks on slots: apps/game/src/features/items/click.test.ts.</summary>
public class ClickTests
{
    private static Stack Scrap(int count) => new("scrap", count);
    private static Place Inv(int slot) => new(slot);
    private static Place Box(int slot) => new(slot, 7);

    private static readonly Place Back = new(Spec.Inventory.Slots + 1), Belt1 = new(Spec.Inventory.Slots + 2), Belt2 = new(Spec.Inventory.Slots + 3), Belt3 = new(Spec.Inventory.Slots + 4);

    private static SlotClick Click(
        Place place, Stack? stack, bool right = false, bool shift = false, int? crate = 7, int? fuelSlot = null,
        Stack?[]? inventory = null, bool charger = false) =>
        new(place, stack, right, shift, crate, inventory ?? new Stack?[Spec.Inventory.Total], fuelSlot, charger);

    /// <summary>An inventory holding <paramref name="stacks"/> at their slots, the rest empty.</summary>
    private static Stack?[] Holding(params (Place At, Stack Stack)[] stacks)
    {
        var slots = new Stack?[Spec.Inventory.Total];
        foreach (var (at, stack) in stacks) slots[at.Slot] = stack;
        return slots;
    }

    private static Move MoveOf(Place src, Target dst, int count) => new(src, dst, count);

    [Fact]
    public void PickAWholeStackWithTheLeftButtonAndPutItOnTheNextSlot()
    {
        var picked = Clicks.On(null, Click(Box(2), Scrap(9)));
        Assert.Equal(new Clicked(new Picked(Box(2), 9)), picked);
        Assert.Equal(new Clicked(null, MoveOf(Box(2), Inv(4), 9)), Clicks.On(picked.Picked, Click(Inv(4), Scrap(3))));
    }

    [Fact]
    public void PickHalfAStackRoundedUpWithTheRightButton()
    {
        Assert.Equal(new Picked(Inv(0), 5), Clicks.On(null, Click(Inv(0), Scrap(9), right: true)).Picked);
        Assert.Equal(1, Clicks.On(null, Click(Inv(0), Scrap(1), right: true)).Picked?.Count);
    }

    [Fact]
    public void BreatheACanisterOfTheHerosOwnWithTheRightButtonNothingPicked()
    {
        var canisters = new Stack("canister", 3);
        Assert.Equal(new Clicked(null, Breathes: true), Clicks.On(null, Click(Inv(4), canisters, right: true)));
        // In a crate, half of them as any stack; with a stack picked, one of it put down there.
        Assert.Equal(new Clicked(new Picked(Box(1), 2)), Clicks.On(null, Click(Box(1), canisters, right: true)));
        var picked = new Picked(Inv(0), 2);
        Assert.Equal(new Clicked(new Picked(Inv(0), 1), MoveOf(Inv(0), Inv(4), 1)), Clicks.On(picked, Click(Inv(4), canisters, right: true)));
    }

    [Fact]
    public void PutOneOfAPickedStackWithTheRightButtonKeepingTheRestPicked()
    {
        var picked = new Picked(Inv(0), 2);
        var once = Clicks.On(picked, Click(Inv(5), null, right: true));
        Assert.Equal(new Clicked(new Picked(Inv(0), 1), MoveOf(Inv(0), Inv(5), 1)), once);
        Assert.Null(Clicks.On(once.Picked, Click(Inv(5), Scrap(1), right: true)).Picked);
    }

    [Fact]
    public void PutAPickedStackBackWithAClickOnItsOwnSlotAndPickNothingFromNothing()
    {
        Assert.Equal(new Clicked(null), Clicks.On(new Picked(Inv(3), 4), Click(Inv(3), Scrap(4))));
        Assert.Equal(new Clicked(null), Clicks.On(null, Click(Inv(3), null)));
    }

    [Fact]
    public void SendAFuelWithShiftIntoAnOpenFurnacesFuelSlotAndSalvageIntoItsInput()
    {
        var planks = new Stack("planks", 3);
        Assert.Equal(
            MoveOf(Inv(1), new Target(1, 7), 3),
            Clicks.On(null, Click(Inv(1), planks, shift: true, fuelSlot: 1)).Command);
        var chunk = new Stack("scrapChunk", 2);
        Assert.Equal(
            MoveOf(Inv(1), new Target(Crate: 7), 2),
            Clicks.On(null, Click(Inv(1), chunk, shift: true, fuelSlot: 1)).Command);
    }

    [Fact]
    public void SendAStackToTheOtherSideOfAnOpenCrateWithShiftAndNowhereWithoutOne()
    {
        Assert.Equal(new Clicked(null, MoveOf(Inv(1), new Target(Crate: 7), 6)), Clicks.On(null, Click(Inv(1), Scrap(6), shift: true)));
        Assert.Equal(MoveOf(Box(1), new Target(), 6), Clicks.On(null, Click(Box(1), Scrap(6), shift: true)).Command);
        var held = new Picked(Inv(9), 1);
        Assert.Equal(new Clicked(held), Clicks.On(held, Click(Inv(1), Scrap(6), shift: true, crate: null)));
    }

    [Fact]
    public void WearAnItemWithShiftOnTheFreeSlotOfItsKind()
    {
        var jetpack = new Stack("jetpack", 1);
        Assert.Equal(
            new Clicked(null, MoveOf(Inv(3), Back, 1)),
            Clicks.On(null, Click(Inv(3), jetpack, shift: true, crate: null, inventory: Holding((Inv(3), jetpack)))));
    }

    [Fact]
    public void PutABatteryWithShiftOnTheFirstFreeSlotOfTheBelt()
    {
        var worn = new Stack("battery", 1, 300);
        var battery = new Stack("battery", 1);
        var inventory = Holding((Belt1, worn), (Belt3, worn), (Inv(5), battery));
        Assert.Equal(MoveOf(Inv(5), Belt2, 1), Clicks.On(null, Click(Inv(5), battery, shift: true, crate: null, inventory: inventory)).Command);
    }

    [Fact]
    public void SwapWithTheFirstWornOfItsKindWithShiftWhenNoSlotOfItIsFree()
    {
        var worn = new Stack("battery", 1, 300);
        var battery = new Stack("battery", 1);
        var belt = Holding((Belt1, worn), (Belt2, worn), (Belt3, worn), (Inv(5), battery));
        Assert.Equal(MoveOf(Inv(5), Belt1, 1), Clicks.On(null, Click(Inv(5), battery, shift: true, crate: null, inventory: belt)).Command);
        var jetpack = new Stack("jetpack", 1);
        var back = Holding((Back, jetpack), (Inv(2), jetpack));
        Assert.Equal(MoveOf(Inv(2), Back, 1), Clicks.On(null, Click(Inv(2), jetpack, shift: true, crate: null, inventory: back)).Command);
    }

    [Fact]
    public void TakeOffWhatIsWornWithShiftOntoTheFirstFreeOwnSlotHotbarFirst()
    {
        var jetpack = new Stack("jetpack", 1);
        var inventory = Holding((Back, jetpack), (Inv(0), Scrap(4)));
        Assert.Equal(
            new Clicked(null, MoveOf(Back, Inv(1), 1)),
            Clicks.On(null, Click(Back, jetpack, shift: true, crate: null, inventory: inventory)));
        // With every own slot full it stays worn, and a stack picked stays picked.
        var full = Holding([.. Enumerable.Range(0, Spec.Inventory.Slots).Select(i => (Inv(i), Scrap(1))), (Back, jetpack)]);
        var held = new Picked(Inv(9), 1);
        Assert.Equal(new Clicked(held), Clicks.On(held, Click(Back, jetpack, shift: true, crate: null, inventory: full)));
    }

    [Fact]
    public void WearAndTakeOffWithShiftWhileACrateIsOpenAndSendFromItAsBefore()
    {
        var jetpack = new Stack("jetpack", 1);
        Assert.Equal(MoveOf(Inv(3), Back, 1), Clicks.On(null, Click(Inv(3), jetpack, shift: true, inventory: Holding((Inv(3), jetpack)))).Command);
        Assert.Equal(MoveOf(Back, Inv(0), 1), Clicks.On(null, Click(Back, jetpack, shift: true, inventory: Holding((Back, jetpack)))).Command);
        Assert.Equal(MoveOf(Box(2), new Target(), 1), Clicks.On(null, Click(Box(2), jetpack, shift: true)).Command);
    }

    [Fact]
    public void SendABatteryWithShiftIntoAnOpenChargingStationWornOrNot()
    {
        var battery = new Stack("battery", 1);
        var station = new Target(Crate: 7);
        Assert.Equal(MoveOf(Inv(5), station, 1), Clicks.On(null, Click(Inv(5), battery, shift: true, inventory: Holding((Inv(5), battery)), charger: true)).Command);
        Assert.Equal(MoveOf(Belt1, station, 1), Clicks.On(null, Click(Belt1, battery, shift: true, inventory: Holding((Belt1, battery)), charger: true)).Command);
        Assert.Equal(MoveOf(Box(0), new Target(), 1), Clicks.On(null, Click(Box(0), battery, shift: true, charger: true)).Command);
        // What holds no charge is worn as anywhere else.
        var jetpack = new Stack("jetpack", 1);
        Assert.Equal(MoveOf(Inv(3), Back, 1), Clicks.On(null, Click(Inv(3), jetpack, shift: true, inventory: Holding((Inv(3), jetpack)), charger: true)).Command);
    }

    [Fact]
    public void SendWhatIsNotWornWithShiftAsBeforeWithTheEquipmentFree()
    {
        Assert.Equal(MoveOf(Inv(1), new Target(Crate: 7), 6), Clicks.On(null, Click(Inv(1), Scrap(6), shift: true, inventory: Holding((Inv(1), Scrap(6))))).Command);
        Assert.Null(Clicks.On(null, Click(Inv(1), Scrap(6), shift: true, crate: null, inventory: Holding((Inv(1), Scrap(6))))).Command);
    }
}
