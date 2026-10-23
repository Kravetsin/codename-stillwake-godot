using Stillwake.Sim.Building;
using Stillwake.Sim.Items;
using Xunit;
using Slot = Stillwake.Sim.Items.Place;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The arc module in the charger (0100): its draw five times, and so its charging, 15 a tick for 3 (the user's call, a fair
/// trade); the charger without it as it was; the drone bay no host yet.
/// </summary>
public class ChargerArcTests
{
    private const string Arc = "arcModule", Battery = "battery", Accumulator = "accumulator";
    private const int S = Spec.Charge.Scale;

    /// <summary>
    /// A room with a charger (machine 4), a drone bay, a crate and a lamp; three solar panels out under the stars, 15 of power.
    /// With <paramref name="short"/> only the first is cabled, to the charger and the lamp: 5 for what wants 16 with the arc.
    /// </summary>
    private static Scenario World(bool @short = false)
    {
        string[] rows = ["PPPPPPPPP    ", "             ", "#############", "#C.RR.KK.L..#", "#C.RR.......#", "#############"];
        List<((int X, int Y) A, (int X, int Y) B)> cables = @short
            ? [((0, 0), (1, 3)), ((0, 0), (9, 3))]
            : [((0, 0), (3, 0)), ((3, 0), (6, 0)), ((0, 0), (1, 3))];
        var w = new Scenario(Layout.Parse(rows, null, cables), doubled: true).GiveItems(0, (Arc, 2)).Run(2);
        Assert.Equal("charger", w.M.Grid.MachineById(Charger(w))!.Kind);
        return w;
    }

    private static int Charger(Scenario w) => w.M.Grid.MachinesOf("charger").Single().Id;
    private static int Bay(Scenario w) => w.M.Grid.MachinesOf(Spec.Logistics.Bay).Single().Id;
    private static int SlotOf(Scenario w, string item) => w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static Stack? Held(Scenario w) => w.M.Items.Crate(Charger(w))[0];
    private static Scenario Fit(Scenario w, int machine) => w.Do(new Fit(new Slot(SlotOf(w, Arc)), machine)).Run();
    private static Scenario Empty(Scenario w, string item) => w.Put(0, new Stack(item, 1, 0), crate: Charger(w));

    [Fact]
    public void WithTheArcItDrawsFifteenAndFillsABatteryInSixSecondsFiveTimesAsFast()
    {
        var w = World();
        Fit(w, Charger(w));
        Assert.Empty(w.Refusals);
        Assert.Equal([Arc], w.M.Items.FittedTo(Charger(w)));
        Assert.Equal(new Boost(0, 4000, 0, 0), w.M.Items.Boost(Charger(w)));
        // What its panel's bolt says, while it charges or not.
        Assert.Equal(15, w.M.Power.Draw(Charger(w)));
        Empty(w, Battery).Run(119);
        Assert.Equal(119 * 15 * S, Held(w)!.Charge);
        // What its panel's rate says: what goes in a tick.
        Assert.Equal(15 * S, w.M.Power.ChargeRate(Charger(w)));
        Assert.Equal((15, 15, 1000), (w.M.Power.Networks[0].Supply, w.M.Power.Networks[0].Demand, w.M.Power.Networks[0].Satisfaction));
        w.Run();
        // 120 ticks, 6 s at 20 a second: 30 s without it.
        Assert.Equal(new Stack(Battery, 1, Spec.Charge.Battery), Held(w));
        Assert.Equal(6, 120 / Sim.World.TicksPerSecond);
        // Full, it rests: it draws nothing, module or none.
        w.Run(5);
        Assert.Equal(0, w.M.Power.Networks[0].Demand);
        Assert.False(w.M.Power.Charges(Charger(w)));
        Assert.Equal(0, w.M.Power.ChargeRate(Charger(w)));
    }

    [Fact]
    public void WithTheArcAnAccumulatorFillsInEighteenSecondsWithoutItInNinety()
    {
        var fitted = World();
        Fit(fitted, Charger(fitted));
        Empty(fitted, Accumulator).Run(359);
        Assert.True(Held(fitted)!.Charge < Spec.Charge.Accumulator);
        fitted.Run();
        Assert.Equal(new Stack(Accumulator, 1, Spec.Charge.Accumulator), Held(fitted));

        var plain = Empty(World(), Accumulator);
        plain.Run(1799);
        Assert.Equal(1799 * 3 * S, Held(plain)!.Charge);
        Assert.Equal(3, plain.M.Power.Networks[0].Demand);
        plain.Run();
        Assert.Equal(new Stack(Accumulator, 1, Spec.Charge.Accumulator), Held(plain));
        Assert.Equal((18, 90), (360 / Sim.World.TicksPerSecond, 1800 / Sim.World.TicksPerSecond));
    }

    [Fact]
    public void WithoutTheModuleItChargesAsBeforeAndTakenOutItIsPlainAgain()
    {
        var w = World();
        Assert.Equal(default, w.M.Items.Boost(Charger(w)));
        Assert.Equal(3, w.M.Power.Draw(Charger(w)));
        Fit(w, Charger(w)).Do(new Unfit(Charger(w), Arc)).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Items.FittedTo(Charger(w)));
        Assert.Equal(3, w.M.Power.Draw(Charger(w)));
        Empty(w, Battery).Run(600);
        Assert.Equal(new Stack(Battery, 1, Spec.Charge.Battery), Held(w));
    }

    [Fact]
    public void OnAShortNetworkItsFiveTimesTheDrawLowersEveryUsersShareItsOwnToo()
    {
        var w = World(@short: true);
        Fit(w, Charger(w));
        Empty(w, Battery).Run();
        var network = w.M.Power.NetworkOf(w.M.Grid.MachineById(Charger(w))!.Cell)!;
        // 15 of the charger and 1 of the lamp on a panel's 5.
        Assert.Equal((5, 16), (network.Supply, network.Demand));
        int share = w.M.Power.Satisfaction(Charger(w));
        Assert.True(share is > 0 and < 1000, $"{share}");
        Assert.Equal(share, w.M.Power.Satisfaction(w.M.Grid.MachinesOf("lamp").Single().Id));
        // What it puts in is its draw times its share, rounded down: still faster than the plain charger at full power.
        int perTick = 15 * S * share / 1000;
        Assert.Equal(perTick, w.M.Power.ChargeRate(Charger(w)));
        int was = Held(w)!.Charge;
        w.Run(10);
        Assert.Equal(was + 10 * perTick, Held(w)!.Charge);
        Assert.True(perTick > 3 * S, $"{perTick}");
    }

    [Fact]
    public void TheDroneBayTakesNoModuleItsDrawAndChargingAsTheyWere()
    {
        var w = World();
        Assert.False(Spec.Modules.Hosted(Spec.Logistics.Bay));
        Fit(w, Bay(w));
        Assert.Equal(["items.notModule"], Reasons(w));
        Assert.Empty(w.M.Items.FittedTo(Bay(w)));
        Assert.Equal(default, w.M.Items.Boost(Bay(w)));
        Assert.Equal(Spec.Machines.Get(Spec.Logistics.Bay).Draws, w.M.Power.Draw(Bay(w)));
    }

    [Fact]
    public void ItTakesOneArcAtMostAndIsNotTakenApartWhileItHoldsIt()
    {
        var w = World();
        Fit(w, Charger(w));
        Fit(w, Charger(w));
        Assert.Equal(["items.fitted"], Reasons(w));
        Assert.Equal(1, w.M.Items.Inventory(0).Sum(s => s?.Item == Arc ? s.Count : 0));
        // Uncabled, so only the module keeps it standing.
        var loose = new Scenario(Layout.Parse(["#####", "#C..#", "#C..#", "#####"]), doubled: true).GiveItems(0, (Arc, 1));
        Fit(loose, Charger(loose)).Do(new Deconstruct(loose.M.Grid.MachineById(Charger(loose))!.Cell, "machine")).Run();
        Assert.Equal(["building.notEmpty"], Reasons(loose));
        loose.Do(new Unfit(Charger(loose), Arc)).Do(new Deconstruct(loose.M.Grid.MachineById(Charger(loose))!.Cell, "machine")).Run();
        Assert.Equal(["building.notEmpty"], Reasons(loose));
    }
}
