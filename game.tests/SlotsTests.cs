using Stillwake.Game.Shared;
using Xunit;

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
    public void DrawsTheArtAtTwiceItsSizeNormallyAndThriceLarge()
    {
        Assert.Equal((40, 32), (Slots.SizeOf(SlotScale.Normal), Slots.IconOf(SlotScale.Normal)));
        Assert.Equal((56, 48), (Slots.SizeOf(SlotScale.Large), Slots.IconOf(SlotScale.Large)));
        // The item art is 16 px: a whole multiple keeps every pixel the same width.
        Assert.All(Enum.GetValues<SlotScale>(), scale => Assert.Equal(0, Slots.IconOf(scale) % 16));
    }

    [Fact]
    public void LaysTheGaugesAndTheInventoryUnderTheHotbarAsItGrows()
    {
        // The places they had at the old 36 px slot.
        Assert.Equal(64, Slots.GaugeTop(36));
        Assert.Equal(112, Slots.InventoryTop(36));
        foreach (var scale in Enum.GetValues<SlotScale>())
        {
            int slot = Slots.SizeOf(scale);
            Assert.True(Slots.GaugeTop(slot) > Slots.Margin + slot + 8, $"{scale}: the gauges clear the hotbar");
            Assert.True(Slots.InventoryTop(slot) >= Slots.GaugeTop(slot) + 30, $"{scale}: the inventory clears the gauges");
        }
    }

    [Fact]
    public void TheGaugesFillTheHotbarsColumnAndOnlyTheReadoutStandsOnItsRail()
    {
        Assert.Equal(400, Slots.Column(40));
        Assert.Equal(544, Slots.Column(56));
        foreach (var scale in Enum.GetValues<SlotScale>())
        {
            int slot = Slots.SizeOf(scale), column = Slots.Column(slot);
            var row = Slots.GaugeRowOf(slot);
            Assert.Equal(Slots.Margin, row.SuitX);
            Assert.Equal(row.SuitX + row.SuitWidth + WristLook.TabGap, row.EnergyX);
            // The two tabs end where the hotbar's screen ends; the readout one seam right of it, as the bag's sort key.
            Assert.Equal(Slots.Margin + column, row.EnergyX + row.EnergyWidth);
            Assert.Equal(Slots.Margin + column + WristLook.TabGap, row.ReadoutX);
            Assert.Equal(0, row.SuitWidth % 2);
            Assert.Equal(0, row.EnergyX % 2);
            // Cells of about one width in both: within a pixel of each other.
            double suitCell = (row.SuitWidth - WristLook.SuitMeterLeft - WristLook.MeterRight - 14.0 * WristLook.CellGap) / WristLook.SuitCells;
            double energyCell = (row.EnergyWidth - WristLook.EnergyMeterLeft - WristLook.MeterRight - 9.0 * WristLook.CellGap - 2 * WristLook.GroupGap) / 12;
            Assert.InRange(suitCell - energyCell, -1, 1);
        }
    }

    [Fact]
    public void LaysAGaugesCellsAcrossItsMeterExactlyAGapBetweenBatteries()
    {
        foreach (int width in new[] { 100, 106, 140, 203 })
        {
            var cells = MeterCells.Lay(width, 12, 4, 2, 4);
            Assert.Equal(12, cells.Count);
            Assert.Equal(0, cells[0].X);
            Assert.Equal(width, cells[^1].X + cells[^1].Width);
            for (int i = 1; i < cells.Count; i++)
                Assert.Equal(i % 4 == 0 ? 4 : 2, cells[i].X - (cells[i - 1].X + cells[i - 1].Width));
            Assert.InRange(cells.Max(c => c.Width) - cells.Min(c => c.Width), 0, 1);
        }
        var one = MeterCells.Lay(170, 15, 0, 2, 4);
        Assert.Equal(170, one[^1].X + one[^1].Width);
        Assert.All(one.Skip(1).Zip(one), p => Assert.Equal(2, p.First.X - (p.Second.X + p.Second.Width)));
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
        Assert.Equal(40, ui.Slot);
        ui.SlotScale.Set(SlotScale.Large);
        Assert.Equal(56, ui.Slot);
    }
}
