using Stillwake.Game.Shared;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Every slot's size and the HUD laid out from it, how a slot is tinted, and the setting as the settings file keeps it (0058).</summary>
public class SlotsTests
{
    [Fact]
    public void TintsToolsAmberWornItemsGreenAndTheRestPlain()
    {
        Assert.Equal(SlotTint.Tool, Slots.TintOf("fireAxe"));
        Assert.Equal(SlotTint.Tool, Slots.TintOf("harpoon"));
        Assert.Equal(SlotTint.Tool, Slots.TintOf("drill"));
        Assert.Equal(SlotTint.Worn, Slots.TintOf("jetpack"));
        Assert.Equal(SlotTint.Worn, Slots.TintOf("battery"));
        Assert.Equal(SlotTint.Plain, Slots.TintOf("canister"));
        Assert.Equal(SlotTint.Plain, Slots.TintOf("scrap"));
    }

    [Fact]
    public void TintsEveryItemByWhatContentSaysItIs()
    {
        Assert.All(Content.Items.All, item => Assert.Equal(
            ItemCards.IsTool(item.Id) ? SlotTint.Tool : item.Equip != null ? SlotTint.Worn : SlotTint.Plain,
            Slots.TintOf(item.Id)));
    }

    [Fact]
    public void ASlotIs44PxNormallyAnd60LargeTheHudGrowingFromItsBaseOf40()
    {
        Assert.Equal((44, 36), (Slots.SizeOf(SlotScale.Normal), Slots.IconOf(SlotScale.Normal)));
        Assert.Equal((60, 52), (Slots.SizeOf(SlotScale.Large), Slots.IconOf(SlotScale.Large)));
        Assert.All(Enum.GetValues<SlotScale>(), scale => Assert.True(Slots.SizeOf(scale) > Slots.Base));
    }

    [Fact]
    public void OpensTheInventoryUnderTheHotbarAsItGrows()
    {
        Assert.Equal(74, Slots.InventoryTop(36));
        foreach (var scale in Enum.GetValues<SlotScale>())
        {
            int slot = Slots.SizeOf(scale);
            Assert.True(Slots.InventoryTop(slot) > Slots.Margin + slot + 8, $"{scale}: the inventory clears the hotbar");
        }
        Assert.Equal(400, Slots.Column(40));
        Assert.Equal(544, Slots.Column(56));
    }

    [Fact]
    public void KeepsEverySettingAsAWordAndReadsItBack()
    {
        Assert.All(Enum.GetValues<SlotScale>(), scale => Assert.Equal(scale, Slots.ScaleNamed(Slots.NameOf(scale))));
        Assert.Equal("normal", Slots.NameOf(SlotScale.Normal));
        Assert.Equal("large", Slots.NameOf(SlotScale.Large));
    }

    [Fact]
    public void ReadsNoneOrAWordItDoesNotKnowAsNormal()
    {
        Assert.Equal(SlotScale.Normal, Slots.ScaleNamed(null));
        Assert.Equal(SlotScale.Normal, Slots.ScaleNamed(""));
        Assert.Equal(SlotScale.Normal, Slots.ScaleNamed("huge"));
    }

    [Fact]
    public void TheSlotsSizeFollowsTheSettingAtOnce()
    {
        var ui = new Ui();
        Assert.Equal(44, ui.Slot);
        ui.SlotScale.Set(SlotScale.Large);
        Assert.Equal(60, ui.Slot);
    }

    [Fact]
    public void AToolsIconFillsItsSlotAndAnyOtherSitsInTheInset()
    {
        Assert.Equal((0, 44), Slots.IconBox(Spec.Shots.Railgun.Item, Slots.SizeOf(SlotScale.Normal)));
        // The screwdriver is held as a picture, though its card is not a tool's.
        Assert.Equal((0, 60), Slots.IconBox(Spec.Screwdrivers.Basic.Item, Slots.SizeOf(SlotScale.Large)));
        Assert.Equal((Slots.Inset, 36), Slots.IconBox("scrap", Slots.SizeOf(SlotScale.Normal)));
        Assert.Equal((Slots.Inset, 52), Slots.IconBox(Spec.Shots.Nail.Id, Slots.SizeOf(SlotScale.Large)));
    }
}
