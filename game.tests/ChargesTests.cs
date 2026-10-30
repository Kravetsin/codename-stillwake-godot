using System.Text.Json.Nodes;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Charge on screen (0053): a slot's bar, the empty battery's picture, the card's meter, the station's words and the energy gauge.</summary>
public class ChargesTests
{
    private const string Battery = "battery";
    private static readonly int Full = Spec.Items.Get(Battery).Capacity!.Value;

    /// <summary>A full battery in the power-ticks machines draw in.</summary>
    private static readonly int Units = Full / Spec.Charge.Scale;
    private static Stack Charged(int charge) => new(Battery, 1, charge);

    [Fact]
    public void ASlotsBarIsGreenAmberUnderAQuarterAndEmptyAtNone()
    {
        Assert.Equal(ChargeBar.Flat, Charges.BarOf(0, Full));
        Assert.Equal(ChargeBar.Low, Charges.BarOf(1, Full));
        Assert.Equal(ChargeBar.Low, Charges.BarOf(Full / 4 - 1, Full));
        Assert.Equal(ChargeBar.Good, Charges.BarOf(Full / 4, Full));
        Assert.Equal(ChargeBar.Good, Charges.BarOf(Full, Full));
        Assert.Equal(ChargeBar.Low, Charges.BarOf(Charged(Full / 10)));
        // Anything that holds no charge has no bar at all.
        Assert.Null(Charges.BarOf(new Stack("scrap", 40)));
        Assert.Equal(0.5f, Charges.Share(Charged(Full / 2)));
        Assert.Equal(0f, Charges.Share(new Stack("scrap", 40)));
    }

    [Fact]
    public void AnEmptyBatteryShowsItsOwnPicture()
    {
        Assert.Equal("battery_empty", Charges.Picture(Battery, 0));
        Assert.Equal(Battery, Charges.Picture(Battery, 1));
        Assert.Equal("scrap", Charges.Picture("scrap", 0));
        Assert.Equal("drone_empty", Charges.Picture(Spec.Logistics.Drone, 0));
        // Every item that holds charge has its empty picture drawn.
        string items = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art", "items");
        Assert.All(Spec.Items.All.Where(i => i.Capacity != null), i => Assert.True(File.Exists(Path.Combine(items, $"{i.Id}_empty.png")), i.Id));
    }

    [Fact]
    public void TheCardsMeterGivesWholePercentAndWhatItHoldsInPowerTicksNeverATime()
    {
        // 65% of a battery is 65% of its power-ticks, in the units machines draw in.
        Assert.Equal(new ChargeMeter(65, Full * 65 / 100 / Spec.Charge.Scale, Units, 0.65f), Charges.MeterOf(Charged(Full * 65 / 100)));
        Assert.Equal(new ChargeMeter(100, Units, Units, 1f), Charges.MeterOf(Charged(Full)));
        // Rounded down: a hair short of full is 99% and a power-tick short, a stored unit is none.
        Assert.Equal((99, Units - 1), (Charges.MeterOf(Charged(Full - 1))!.Percent, Charges.MeterOf(Charged(Full - 1))!.Stored));
        Assert.Equal((0, 0), (Charges.MeterOf(Charged(1))!.Percent, Charges.MeterOf(Charged(1))!.Stored));
        Assert.Null(Charges.MeterOf(new Stack("scrap", 3)));
    }

    [Fact]
    public void TheTipOverABatteryCarriesItsChargeAndPicture()
    {
        var reading = new Reading([], null, Creative: true, null);
        var card = reading.TipOf(Charged(Full * 6 / 10)).Card!;
        Assert.Equal(new ChargeMeter(60, Full * 6 / 10 / Spec.Charge.Scale, Units, 0.6f), card.Charge);
        Assert.Equal(Battery, card.Picture);
        var empty = reading.TipOf(Charged(0)).Card!;
        Assert.Equal("battery_empty", empty.Picture);
        // Two batteries holding different charge say different things, so the tip redraws.
        Assert.NotEqual(card, empty);
        Assert.Null(reading.TipOf(new Stack("scrap", 2)).Card!.Charge);
        Assert.Equal(reading.TipOf("scrap").Card, reading.TipOf(new Stack("scrap", 2)).Card);
    }

    [Fact]
    public void AnElectricToolsCardShowsItsDrawAsAMachineShowsItsOwn()
    {
        // From content, as a machine's Draws: no item is electric yet, so no card has one.
        Assert.All(Spec.Items.All, i => Assert.Equal(i.Draw, ItemCards.Of(i.Id).Draw));
        var axe = ItemCards.Of("fireAxe");
        Assert.NotEqual(axe, axe with { Draw = 3 });
        Assert.Equal(axe with { Draw = 3 }, axe with { Draw = 3 });
    }

    [Fact]
    public void TheStationSaysWhetherItIsEmptyFullChargingOrWithoutPower()
    {
        Assert.Equal("charger.empty", Charges.StationState(null, false));
        Assert.Equal("charger.full", Charges.StationState(Charged(Full), false));
        Assert.Equal("charger.charging", Charges.StationState(Charged(5), true));
        Assert.Equal("charger.noPower", Charges.StationState(Charged(5), false));
    }

    [Fact]
    public void TheEnergyGaugeIsAShareOfAFullBeltInWholeUnitsLowUnderAQuarterOfWhatIsWorn()
    {
        // Three stores on the belt (0081), the share of three of the largest, accumulators (0099): a battery is a ninth.
        Assert.Equal(3, Charges.BeltSlots);
        Assert.Equal(3 * Spec.Charge.Accumulator / Spec.Charge.Scale, Charges.BeltMost);
        Assert.Equal(new Belt(1.0 / 9, Units, Low: false, Worn: true), Charges.BeltOf((Full, Full)));
        Assert.Equal(new Belt(1.0 / 3, 3 * Units, Low: false, Worn: true), Charges.BeltOf((3 * Full, 3 * Full)));
        Assert.Equal(new Belt(1, 3 * 3 * Units, Low: false, Worn: true), Charges.BeltOf((9 * Full, 9 * Full)));
        // Units round down, as the card's meter: a hair short of a full battery is a power-tick short.
        Assert.Equal(Units - 1, Charges.BeltOf((Full - 1, Full)).Units);
        // Low under a quarter of what the worn batteries hold full, as each slot's bar; an empty one too.
        Assert.True(Charges.BeltOf((Full / 2 - 1, 2 * Full)).Low);
        Assert.False(Charges.BeltOf((Full / 2, 2 * Full)).Low);
        Assert.True(Charges.BeltOf((0, Full)).Low);
        // None worn: shown, dim and empty, never low, so it does not pulse over nothing.
        Assert.Equal(new Belt(0, 0, Low: false, Worn: false), Charges.BeltOf((0, 0)));
    }

    [Fact]
    public void TheReadoutSetsThousandsApartByAThinSpace()
    {
        Assert.Equal("0", Charges.Grouped(0));
        Assert.Equal("950", Charges.Grouped(950));
        Assert.Equal("4 300", Charges.Grouped(4300));
        Assert.Equal("12 345 678", Charges.Grouped(12345678));
        Assert.Equal("0", Charges.Grouped(-5));
    }

    [Fact]
    public void TheEnergyGaugeSumsTheBatteriesOnTheBeltAndNothingElse()
    {
        static JsonObject Stored(int charge) => new() { ["item"] = Battery, ["count"] = 1, ["charge"] = charge };
        var (m, _) = ItemsWorlds.With(Layout.Empty, items =>
        {
            var slots = new JsonArray();
            for (int i = 0; i < Spec.Inventory.Total; i++)
                slots.Add(i == 0 ? Stored(Full) : i == Spec.Inventory.Slots + 2 ? Stored(Full / 2) : i == Spec.Inventory.Slots + 4 ? Stored(Full / 4) : null);
            items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        });
        var belt = m.Items.BeltCharge(0);
        Assert.Equal((Full * 3 / 4, 2 * Full), belt);
        Assert.Equal(new Belt(0.25 / 3, Full * 3 / 4 / Spec.Charge.Scale, Low: false, Worn: true), Charges.BeltOf(belt));
    }

    [Fact]
    public void TheEnergyNumberIsTheSumOfEveryStoreOnTheBeltTwoAccumulatorsAndABatteryTwelveThousandSixHundred()
    {
        // The playtest's belt (0099): it read 5 400, a belt of three batteries, the share capped at one.
        int accumulator = Spec.Charge.Accumulator;
        var (m, _) = ItemsWorlds.With(Layout.Empty, items =>
        {
            var slots = new JsonArray();
            for (int i = 0; i < Spec.Inventory.Total; i++)
                slots.Add(i == Spec.Inventory.Slots + 2 || i == Spec.Inventory.Slots + 3 ? new JsonObject { ["item"] = "accumulator", ["count"] = 1, ["charge"] = accumulator }
                    : i == Spec.Inventory.Slots + 4 ? new JsonObject { ["item"] = Battery, ["count"] = 1, ["charge"] = Full } : null);
            items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        });
        var belt = m.Items.BeltCharge(0);
        Assert.Equal((2 * accumulator + Full, 2 * accumulator + Full), belt);
        var shown = Charges.BeltOf(belt);
        Assert.Equal(12_600, shown.Units);
        Assert.Equal(12_600, Charges.UnitsOf(shown.Share));
        Assert.Equal($"12{Charges.Thin}600", Charges.Grouped(Charges.UnitsOf(shown.Share)));
        // Three accumulators full fill it; nothing passes its end.
        Assert.Equal(Charges.BeltMost, Charges.UnitsOf(Charges.BeltOf((3 * accumulator, 3 * accumulator)).Share));
    }
}
