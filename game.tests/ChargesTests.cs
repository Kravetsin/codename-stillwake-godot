using System.Text.Json.Nodes;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Charge on screen (0053): a slot's bar, the empty battery's picture, the card's meter, the station's words and the energy bar.</summary>
public class ChargesTests
{
    private const string Battery = "battery";
    private static readonly int Full = Spec.Items.Get(Battery).Capacity!.Value;
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
    }

    [Fact]
    public void TheCardsMeterGivesWholePercentAndTheSecondsOfDrillingLeft()
    {
        // The mockup's: 60% of thirty seconds of drilling is eighteen.
        Assert.Equal(new ChargeMeter(60, 18, 0.6f), Charges.MeterOf(Charged(Full * 6 / 10)));
        Assert.Equal(new ChargeMeter(100, Full / Spec.Charge.PerTick / World.TicksPerSecond, 1f), Charges.MeterOf(Charged(Full)));
        // Rounded down: a hair short of full is 99%, a unit is none.
        Assert.Equal(99, Charges.MeterOf(Charged(Full - 1))!.Percent);
        Assert.Equal((0, 0), (Charges.MeterOf(Charged(1))!.Percent, Charges.MeterOf(Charged(1))!.Seconds));
        Assert.Null(Charges.MeterOf(new Stack("scrap", 3)));
    }

    [Fact]
    public void TheTipOverABatteryCarriesItsChargeAndPicture()
    {
        var reading = new Reading([], null, Creative: true, null, null, []);
        var card = reading.TipOf(Charged(Full * 6 / 10)).Card!;
        Assert.Equal(new ChargeMeter(60, 18, 0.6f), card.Charge);
        Assert.Equal(Battery, card.Picture);
        var empty = reading.TipOf(Charged(0)).Card!;
        Assert.Equal("battery_empty", empty.Picture);
        // Two batteries holding different charge say different things, so the tip redraws.
        Assert.NotEqual(card, empty);
        Assert.Null(reading.TipOf(new Stack("scrap", 2)).Card!.Charge);
        Assert.Equal(reading.TipOf("scrap").Card, reading.TipOf(new Stack("scrap", 2)).Card);
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
    public void TheEnergyBarShowsOnlyWhileAnElectricToolIsHeldInItsColour()
    {
        // No item is electric yet: with none held, or anything held, the bar hides.
        Assert.DoesNotContain(Spec.Items.All, i => i.Electric);
        Assert.Null(Charges.EnergyOf(null, (5, 10)));
        Assert.Null(Charges.EnergyOf("fireAxe", (5, 10)));
        bool Electric(string item) => item == "fireAxe";
        Assert.Equal(new Energy(0.5f, Charges.Green), Charges.EnergyOf("fireAxe", (5, 10), Electric));
        Assert.Null(Charges.EnergyOf("harpoon", (5, 10), Electric));
        // Held with no battery worn, it shows empty: there is nothing to run on.
        Assert.Equal(new Energy(0f, Charges.Green), Charges.EnergyOf("fireAxe", (0, 0), Electric));
    }

    [Fact]
    public void TheEnergyBarSumsTheBatteriesOnTheBeltAndNothingElse()
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
        Assert.Equal(new Energy(0.375f, Charges.Green), Charges.EnergyOf("fireAxe", belt, item => item == "fireAxe"));
    }
}
