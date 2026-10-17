using Stillwake.Game.Features.Items;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Clicks on slots: apps/game/src/features/items/click.test.ts.</summary>
public class ClickTests
{
    private static Stack Scrap(int count) => new("scrap", count);
    private static Place Inv(int slot) => new(slot);
    private static Place Box(int slot) => new(slot, 7);

    private static SlotClick Click(Place place, Stack? stack, bool right = false, bool shift = false, int? crate = 7, int? fuelSlot = null) =>
        new(place, stack, right, shift, crate, fuelSlot);

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
}
