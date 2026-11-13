using System;
using System.Linq;
using Stillwake.Game.Shared;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The vitals block at the screen's bottom-left (hud-vitals): its place, its rows fitted, the stores' order.</summary>
public class VitalsTests
{
    private const float Screen = 720, EdgeKeys = 540;

    public static TheoryData<SlotScale> Scales => new() { SlotScale.Normal, SlotScale.Large };

    [Fact]
    public void ARowKeepsItsScaleUntilItWouldPassTheMostThenShrinksAlikeKeepingItsGaps()
    {
        // A heavy helmet's 100 units fit as they are.
        var air = Vitals.Fit([100], Vitals.AirPerUnit);
        Assert.Equal((Vitals.RowLeft, 230f), (air[0].X, air[0].Width));
        // Three accumulators, nine batteries long, would pass the most: shrunk alike to end there, the gaps kept.
        var stores = Vitals.Fit([3, 3, 3], Vitals.BatteryWidth);
        Assert.Equal(Vitals.RowLeft + Vitals.MostWidth, stores[^1].X + stores[^1].Width, 3);
        Assert.All(stores, s => Assert.Equal(stores[0].Width, s.Width, 4));
        for (int i = 1; i < stores.Count; i++) Assert.Equal(Vitals.Gap, stores[i].X - (stores[i - 1].X + stores[i - 1].Width), 3);
        // In proportion: a battery beside an accumulator is a third as long.
        var mixed = Vitals.Fit([1, 3, 3, 3], Vitals.BatteryWidth);
        Assert.Equal(mixed[1].Width / 3, mixed[0].Width, 3);
    }

    [Theory]
    [MemberData(nameof(Scales))]
    public void TheLargestRowsEndLeftOfTheEdgeKeys(SlotScale scale)
    {
        int slot = Slots.SizeOf(scale);
        float s = Vitals.ScaleOf(slot);
        var heavy = Vitals.Fit([Spec.Items.All.Max(i => i.Tank)], Vitals.AirPerUnit)[0];
        var stores = Vitals.Fit(Enumerable.Repeat((double)Spec.Charge.Accumulator / Spec.Charge.Battery, Charges.BeltSlots).ToList(), Vitals.BatteryWidth)[^1];
        foreach (var end in new[] { heavy.X + heavy.Width, stores.X + stores.Width })
            Assert.True(Vitals.Origin(Screen, slot).X + end * s < EdgeKeys, $"{scale}: a row ends at {end * s}");
        var (x, _, w, _) = Vitals.Covers(Screen, slot);
        Assert.True(x + w < EdgeKeys);
    }

    [Theory]
    [MemberData(nameof(Scales))]
    public void TheBlockStandsUnderTheOpenBagAndOverTheTabletsRim(SlotScale scale)
    {
        int slot = Slots.SizeOf(scale);
        // The bag at its largest: every slot past the hotbar, a row of the hotbar's length each, in its display's frame.
        int rows = (Spec.Inventory.Slots - Spec.Inventory.Hotbar + Spec.Inventory.Hotbar - 1) / Spec.Inventory.Hotbar;
        float bag = Slots.InventoryTop(slot) + rows * (slot + Slots.Gap) + 2 * Slots.ScreenPadding + WristPiece.Scale * WristPieces.Display.Window.Bottom;
        var (_, top, _, height) = Vitals.Covers(Screen, slot);
        Assert.True(top > bag, $"{scale}: the block's top {top} under the bag's foot {bag}");
        Assert.Equal(Screen - Vitals.Below, top + height, 3);
    }

    [Fact]
    public void TheStoreAToolDrainsFirstStandsAtTheRightEnd()
    {
        Assert.Equal([2, 1, 0], Vitals.ChargeOrder(3));
        Assert.Empty(Vitals.ChargeOrder(0));
    }

    [Fact]
    public void TheBlockGrowsWithTheSlots()
    {
        Assert.Equal(1, Vitals.ScaleOf(Slots.SizeOf(SlotScale.Normal)));
        Assert.Equal(1.4f, Vitals.ScaleOf(Slots.SizeOf(SlotScale.Large)), 4);
    }
}
