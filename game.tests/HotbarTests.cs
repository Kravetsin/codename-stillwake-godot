using Stillwake.Game.Features.Items;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The wheel on the hotbar: apps/game/src/features/items/hotbar.test.ts.</summary>
public class HotbarTests
{
    [Fact]
    public void TurnsToTheNextSlotDownAndTheOneBeforeUpRoundTheRow()
    {
        Assert.Equal(1, Hotbar.WheelSlot(0, 100));
        Assert.Equal(0, Hotbar.WheelSlot(Hotbar.Row - 1, 3));
        Assert.Equal(Hotbar.Row - 1, Hotbar.WheelSlot(0, -100));
        Assert.Equal(3, Hotbar.WheelSlot(4, -1));
        Assert.Equal(4, Hotbar.WheelSlot(4, 0));
    }

    [Fact]
    public void TheBagIsTheSlotsPastTheHotbarNotACratesNorTheEquipment()
    {
        Assert.True(Hotbar.InBag(new Stillwake.Sim.Items.Place(Hotbar.Row)));
        Assert.True(Hotbar.InBag(new Stillwake.Sim.Items.Place(Stillwake.Content.Inventory.Slots - 1)));
        Assert.False(Hotbar.InBag(new(0)));
        Assert.False(Hotbar.InBag(new(Stillwake.Content.Inventory.Slots)));
        Assert.False(Hotbar.InBag(new(Hotbar.Row, 3)));
    }
    [Fact]
    public void EachSlotShowsTheDigitThatChoosesItFromOneToNine()
    {
        Assert.Equal("1", Hotbar.KeyOf(0));
        Assert.Equal("9", Hotbar.KeyOf(Hotbar.Row - 1));
        for (int slot = 0; slot < Hotbar.Row; slot++) Assert.Equal(slot, Hotbar.SlotOfDigit(int.Parse(Hotbar.KeyOf(slot))));
    }

    [Fact]
    public void NoDigitButOneToNineChoosesASlot()
    {
        Assert.Null(Hotbar.SlotOfDigit(0));
        Assert.Null(Hotbar.SlotOfDigit(Hotbar.Row + 1));
    }
}
