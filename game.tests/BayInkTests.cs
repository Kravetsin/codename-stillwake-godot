using System.Linq;
using Stillwake.Game.Features.Logistics;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The picked bay's and salvage drone's numbers (salvage-drones) agree with the sim's bay and with the pictures they place.</summary>
public class BayInkTests
{
    private static readonly Spec.MachineSpec Bay = Spec.Machines.Get(Spec.Logistics.Bay);

    [Fact]
    public void ItsPadsAreTheBaysDroneSlotsAndItsFuelPartsTheSlotOfCanisters()
    {
        Assert.Equal((BayInk.Pads.Length, Spec.Crew.Canister), Bay.SlotTakes);
        Assert.Equal(BayInk.Pads.Length + 1, Bay.Slots);
    }

    [Fact]
    public void EveryPictureItPlacesIsDrawnAtTheSizeItWasDrawnFor()
    {
        int m = BayInk.Margin;
        Assert.Equal((Bay.Width * 8, Bay.Height * 8), Png.SizeOf($"machine_{Spec.Logistics.Bay}"));
        Assert.Equal((32 + 2 * m, 32 + 2 * m), Png.SizeOf("droneBay_front"));
        foreach (var lamps in new[] { "0", "1", "2", "3", "4", "low", "off" }) Assert.Equal((6 + 2 * m, 2 + 2 * m), Png.SizeOf($"droneBay_lamps_{lamps}"));
        foreach (var fuel in new[] { "off", "empty", "can", "feed_0", "feed_1" }) Assert.Equal((10 + 2 * m, 17 + 2 * m), Png.SizeOf($"droneBay_fuel_{fuel}"));
        Assert.Equal((4 + 2 * m, 3 + 2 * m), Png.SizeOf("droneBay_beacon_on"));
        Assert.Equal(Png.SizeOf("droneBay_beacon_on"), Png.SizeOf("droneBay_beacon_off"));
        Assert.Equal((10 + 2 * m, 10 + 2 * m), Png.SizeOf("droneBay_canister"));
        // The canister's picture holds its middle on the breech.
        Assert.InRange(BayInk.BreechX - BayInk.Canister.X, 4, 6);
        Assert.InRange(BayInk.BreechY - BayInk.Canister.Y, 4, 6);
        // What the old perch drew is gone.
        Assert.False(Png.Has("droneBay_pad"));
        Assert.False(Png.Has("droneBay_charge_0"));
    }

    [Fact]
    public void ASalvageDronesPicturesAreItsBoxesWithTheirMargin()
    {
        int m = SalvageInk.Margin;
        foreach (var fly in new[] { "salvage_fly_open", "salvage_fly_shut", "salvage_front", "salvage_eye", "salvage_lamp_work", "salvage_lamp_low" })
            Assert.Equal((SalvageInk.FlyWidth + 2 * m, SalvageInk.FlyHeight + 2 * m), Png.SizeOf(fly));
        Assert.Equal((SalvageInk.DockWidth + 2 * m, SalvageInk.DockHeight + 2 * m), Png.SizeOf("salvage_dock"));
        Assert.Equal(Png.SizeOf("salvage_dock"), Png.SizeOf("salvage_dock_eye"));
        Assert.All(Enumerable.Range(0, SalvageInk.Puffs), i => Assert.Equal((7, 7), Png.SizeOf($"salvage_puff_{i}")));
        // Docked on a pad, its feet on the arm as a drone's: taller, so it sits higher.
        Assert.True(SalvageInk.DockFeet > BayInk.Feet);
        Assert.InRange(SalvageInk.HoldBottom, SalvageInk.AnchorY, SalvageInk.FlyHeight);
    }
}
