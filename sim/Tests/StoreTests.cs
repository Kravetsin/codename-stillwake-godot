using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Furnace;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;
using Xunit;
using Move = Stillwake.Sim.Items.Move;
using Pairs = System.Collections.Generic.IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>;

namespace Stillwake.Sim.Tests;

/// <summary>The capacitor bank: a network's store of energy, filled from what it makes beyond its draw, drawn on when short (0101).</summary>
public class StoreTests
{
    /// <summary>
    /// Twenty-four wide: three panels out in space above a room holding two banks, three lamps (1 each) and three oxygen
    /// generators (4 each), so a network makes up to 15 and draws up to 15. Cables are chosen per test.
    /// </summary>
    private static readonly string[] Rows =
    [
        "PPP PPP PPP             ",
        "                        ",
        "########################",
        "#QQ.QQ.L.L.L.GG.GG.GG..#",
        "#QQ.QQ.......GG.GG.GG..#",
        "########################",
    ];
    private static int At(Scenario w, (int X, int Y) p) => w.At(p.X, p.Y);
    private static readonly (int, int)[] Panels = [(0, 0), (4, 0), (8, 0)], Lamps = [(7, 3), (9, 3), (11, 3)], Generators = [(13, 3), (16, 3), (19, 3)];
    private static readonly (int, int) First = (1, 3), Second = (4, 3);
    private static readonly StoreSpec Bank = Machines.Get("capacitorBank").Store!;

    /// <summary>
    /// The map with <paramref name="panels"/> panels and <paramref name="lamps"/> lamps cabled to the first bank, the second
    /// bank with <paramref name="second"/>, and with <paramref name="generators"/> the three oxygen generators through the
    /// third lamp, which they bring in. With <paramref name="first"/> false the first bank's cables end on the wall beside it.
    /// </summary>
    private static Scenario World(int panels = 1, int lamps = 0, bool second = false, bool generators = false, bool first = true)
    {
        var hub = first ? First : (1, 2);
        var cables = new List<((int, int), (int, int))>();
        cables.AddRange(Panels.Take(panels).Select(p => (p, hub)));
        if (second) cables.Add((Second, hub));
        cables.AddRange(Lamps.Take(generators ? 3 : lamps).Select(l => (l, hub)));
        if (generators) cables.AddRange(Generators.Select(g => (g, Lamps[2])));
        return new Scenario(Layout.Parse(Rows, null, (Pairs)cables), with: m => [new Rester(m.Power)], doubled: true);
    }

    private static int IdAt(Scenario w, (int X, int Y) cell) => w.M.Grid.MachineAt(w.At(cell.X, cell.Y))!.Id;
    private static StoreLevel Level(Scenario w, (int, int) bank) => w.M.Power.Store(IdAt(w, bank))!;
    private static int Energy(Scenario w, (int, int) bank) => Level(w, bank).Energy;
    private static int Lit(Scenario w, int lamp = 0) => w.M.Power.Satisfaction(IdAt(w, Lamps[lamp]));
    private static PowerNetwork Network(Scenario w) => w.M.Power.NetworkOf(At(w, First))!;

    /// <summary>The world reloaded with each bank named holding what is given, as a save would have it.</summary>
    private static Scenario Holding(Scenario w, params ((int, int) Bank, int Energy)[] stores)
    {
        var root = JsonNode.Parse(w.World.Save())!;
        var held = stores.Select(s => (Id: IdAt(w, s.Bank), s.Energy)).OrderBy(s => s.Id)
            .Select(s => (JsonNode)new JsonObject { ["machine"] = s.Id, ["energy"] = s.Energy });
        root["slices"]!["power"]!["stores"] = new JsonArray([.. held]);
        return w.Reload(root.ToJsonString());
    }

    /// <summary>Rests every machine at <paramref name="cells"/>: a panel makes nothing, a lamp or a generator draws nothing.</summary>
    private static Scenario Rest(Scenario w, bool resting, params (int, int)[] cells)
    {
        foreach (var c in cells) w.Do(new RestMachine(IdAt(w, c), resting));
        return w.Run();
    }

    [Fact]
    public void SavesWhatItHoldsUnderPowerAsAMachineAndItsEnergy()
    {
        var w = World(panels: 1).Run(3);
        var stores = JsonNode.Parse(w.World.Save())!["slices"]!["power"]!["stores"]!.AsArray();
        var only = Assert.Single(stores);
        Assert.Equal((IdAt(w, First), 15), ((int)only!["machine"]!, (int)only["energy"]!));
    }

    [Fact]
    public void ChargesFromWhatThePanelMakesBeyondWhatTheLampDraws()
    {
        var w = World(panels: 1, lamps: 1);
        Assert.Equal(new StoreLevel(0, Bank.Capacity, 4), Level(w, First));
        w.Run(100);
        Assert.Equal(new StoreLevel(400, Bank.Capacity, 4), Level(w, First));
        Assert.Equal(1000, Lit(w));
        var n = Network(w);
        Assert.Equal((5, 1, 1000, 396, Bank.Capacity, 4), (n.Supply, n.Demand, n.Satisfaction, n.Stored, n.Capacity, n.Flow));
    }

    [Fact]
    public void TakesInNoMoreThanItsInATickTheRestWasted()
    {
        var w = World(panels: 3).Run(10);
        Assert.Equal(15, Network(w).Supply);
        Assert.Equal(new StoreLevel(10 * Bank.In, Bank.Capacity, Bank.In), Level(w, First));
    }

    [Fact]
    public void StopsAtFullTakingNothingMore()
    {
        var w = World(panels: 3).Run(Bank.Capacity / Bank.In - 1);
        Assert.Equal(Bank.Capacity - Bank.In, Energy(w, First));
        w.Run(5);
        Assert.Equal(new StoreLevel(Bank.Capacity, Bank.Capacity, 0), Level(w, First));
        Assert.Equal(0, Network(w).Intake);
        // A little short of full, it takes only the room it has left.
        w = Holding(World(panels: 3), (First, Bank.Capacity - 3)).Run();
        Assert.Equal(new StoreLevel(Bank.Capacity, Bank.Capacity, 3), Level(w, First));
    }

    [Fact]
    public void GivesOutWhatItsNetworkIsShortOfSoEveryUserWorksInFull()
    {
        // A panel's 5 against the three lamps' 3 and the generators' 12: 10 short, the most the bank gives.
        var w = Holding(World(panels: 1, generators: true), (First, 1000)).Run();
        Assert.Equal(new StoreLevel(1000 - Bank.Out, Bank.Capacity, -Bank.Out), Level(w, First));
        Assert.Equal(1000, Lit(w));
        Assert.Equal(1000, w.M.Power.Satisfaction(IdAt(w, Generators[0])));
    }

    [Fact]
    public void GivesOutNoMoreThanItsOutATickTheUsersSlowedAlikeByWhatIsLeftShort()
    {
        // Nothing made against 15 drawn: the bank's 10 serves two thirds of it.
        var w = Holding(World(panels: 0, generators: true), (First, 1000)).Run();
        Assert.Equal(-Bank.Out, Level(w, First).Flow);
        Assert.Equal(1000 * Bank.Out / 15, Lit(w));
        Assert.Equal(1000 * Bank.Out / 15, Network(w).Satisfaction);
    }

    [Fact]
    public void RunsANetworkWithNothingMakingPowerOnItsStoreUntilItIsEmpty()
    {
        var w = Holding(World(panels: 0, lamps: 3), (First, 10)).Run(3);
        Assert.Equal((1, 1000), (Energy(w, First), Lit(w)));
        // What it has left serves a third of the draw, and then nothing does.
        w.Run();
        Assert.Equal((0, 1000 / 3), (Energy(w, First), Lit(w)));
        w.Run();
        Assert.Equal((0, 0), (Energy(w, First), Lit(w)));
        Assert.Empty(JsonNode.Parse(w.World.Save())!["slices"]!["power"]!["stores"]!.AsArray());
    }

    [Fact]
    public void TwoStoresFillAndGiveInTheReadingOrderOfTheirCellsEachToItsLimit()
    {
        var w = World(panels: 3, second: true).Run();
        Assert.Equal((Bank.In, 15 - Bank.In), (Level(w, First).Flow, Level(w, Second).Flow));
        Assert.Equal(15, Network(w).Flow);
        // Short by 3 a tick, the first gives what it has, the second the rest.
        w = Holding(World(panels: 0, lamps: 3, second: true), (First, 4), (Second, 100)).Run();
        Assert.Equal((1, 100), (Energy(w, First), Energy(w, Second)));
        w.Run();
        Assert.Equal((0, 98), (Energy(w, First), Energy(w, Second)));
        Assert.Equal(1000, Lit(w));
        w.Run();
        Assert.Equal((0, 95), (Energy(w, First), Energy(w, Second)));
    }

    [Fact]
    public void NeverFeedsOneStoreFromAnother()
    {
        // Nothing made or drawn: the full bank keeps what it has, the empty one stays empty.
        var w = Holding(World(panels: 0, second: true), (First, 500)).Run(20);
        Assert.Equal((500, 0), (Energy(w, First), Energy(w, Second)));
    }

    [Fact]
    public void HoldsWhatItHasOnNoNetwork()
    {
        var w = Holding(World(panels: 1, lamps: 1, first: false), (First, 700)).Run(20);
        Assert.Equal(new StoreLevel(700, Bank.Capacity, 0), Level(w, First));
        Assert.Null(w.M.Power.NetworkOf(At(w, First)));
    }

    [Fact]
    public void KeepsWhatItHoldsAcrossASaveAndALoadAndChargesOnAlike()
    {
        var w = World(panels: 1, lamps: 1).Run(50);
        w.Reload();
        Assert.Equal(new StoreLevel(200, Bank.Capacity, 4), Level(w, First));
        w.Run(10);
        Assert.Equal(240, Energy(w, First));
    }

    [Fact]
    public void ChargesByWhatIsSpareAsTheLampsComeOnAndGivesOutOnceThePanelRests()
    {
        // The lamps rested, a panel fills it; lit again, they draw 3 of the panel's 5 and it still fills, by 2.
        var w = Rest(World(panels: 1, lamps: 3), true, Lamps).Run(9);
        Assert.Equal(50, Energy(w, First));
        Rest(w, false, Lamps).Run(9);
        Assert.Equal(70, Energy(w, First));
        // The panel rested, the bank lights them.
        Rest(w, true, Panels[0]).Run(9);
        Assert.Equal((40, 1000), (Energy(w, First), Lit(w)));
    }

    // Burners (0101 amends 0045): a fire is lit only while its network takes more than its panels make.

    /// <summary>
    /// Sixteen wide: a room aired by a vaporizer, its tank and panel under the hull (liquid-oxygen), with a burner
    /// generator cabled to a bank and a lamp, a panel out in space above cabled to the generator with <paramref name="panel"/>.
    /// </summary>
    private static Scenario Fired(bool panel = false)
    {
        var w = new Scenario(AirTesting.Vaporized(
            ["PPP             ", "                ", "################", "#BB.TT..QQ.L...#", "#BB.TTV.QQ.....#", "################"],
            ["", "", "", "    o", "    ---"],
            panel ? [((1, 3), (8, 3)), ((11, 3), (8, 3)), ((0, 0), (1, 3))] : [((1, 3), (8, 3)), ((11, 3), (8, 3))]),
            with: m => [new Rester(m.Power)], doubled: true).GiveItems(0, ("planks", 4));
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(w.At(1, 3)); i++) w.Run();
        Assert.True(w.M.Atmosphere.AiredAt(w.At(1, 3)));
        return w;
    }

    private static readonly (int, int) Burner = (1, 3), FiredBank = (8, 3), FiredLamp = (11, 3);

    private static Scenario Feed(Scenario w)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "planks");
        return w.Do(new Move(new Items.Place(from), new Target(Machines.Get("burnerGenerator").FuelSlot!.Value, IdAt(w, Burner)), 1)).Run(3);
    }

    private static Smelting Fire(Scenario w) => w.M.Furnace.At(IdAt(w, Burner));

    [Fact]
    public void ABurnerBurnsToFillItsBankWithNothingDrawing()
    {
        var w = Feed(Rest(Fired(), true, FiredLamp));
        Assert.True(Fire(w).Burning);
        Assert.Equal(Machines.Get("burnerGenerator").Makes, Level(w, FiredBank).Flow);
    }

    [Fact]
    public void ABurnerWaitsWithItsFuelWhileItsBankIsFullAndNothingDrawsAndLightsWhenTheLampComesOn()
    {
        var w = Feed(Holding(Rest(Fired(), true, FiredLamp), (FiredBank, Bank.Capacity)));
        Assert.Equal(new Smelting(false, Problem: "furnace.unneeded"), Fire(w));
        Assert.NotNull(w.M.Items.Crate(IdAt(w, Burner))[0]);
        Rest(w, false, FiredLamp).Run(2);
        Assert.True(Fire(w).Burning);
        Assert.Null(w.M.Items.Crate(IdAt(w, Burner))[0]);
    }

    [Fact]
    public void ABurnerWaitsWhileAPanelMakesWhatItsNetworkTakes()
    {
        // The panel's 5 covers the lamp's 1 and the full bank takes nothing more: the plank waits.
        var w = Feed(Holding(Fired(panel: true), (FiredBank, Bank.Capacity)));
        Assert.Equal("furnace.unneeded", Fire(w).Problem);
        Assert.Equal(1000, w.M.Power.Satisfaction(IdAt(w, FiredLamp)));
    }

    [Fact]
    public void ABurnerOnNoNetworkNeverLights()
    {
        var w = new Scenario(AirTesting.Vaporized(
            ["############", "#BB.TT.....#", "#BB.TTV....#", "############"], ["", "    o", "    ---"]), doubled: true).GiveItems(0, ("planks", 4));
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(w.At(1, 1)); i++) w.Run();
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "planks");
        w.Do(new Move(new Items.Place(from), new Target(0, w.M.Grid.MachinesOf("burnerGenerator")[0].Id), 1)).Run(5);
        Assert.Equal("furnace.unneeded", w.M.Furnace.At(w.M.Grid.MachinesOf("burnerGenerator")[0].Id).Problem);
    }

    // Invariants.

    private static readonly Gen<StoreSpec> Specs =
        from capacity in Gen.Choose(1, 20000) from i in Gen.Choose(0, 40) from o in Gen.Choose(0, 40) select new StoreSpec(capacity, i, o);

    [Property(MaxTest = 500)]
    public Property SettlingKeepsEveryStoreInBoundsMovesOnlyWhatIsSpareOrShortAndStopsOnlyAtALimit()
    {
        var held = from spec in Specs from energy in Gen.Choose(0, spec.Capacity) select new Banked(energy, spec);
        var cases = from supply in Gen.Choose(0, 120)
                    from demand in Gen.Choose(0, 120)
                    from n in Gen.Choose(0, 5)
                    from stores in held.ArrayOf(n)
                    select (supply, demand, stores);
        return Prop.ForAll(cases.ToArbitrary(), c =>
        {
            var (supply, demand, stores) = c;
            var flows = new int[stores.Length];
            int served = PowerNodes.Settle(supply, demand, stores, flows);
            int moved = flows.Sum();
            for (int i = 0; i < stores.Length; i++)
            {
                var (energy, spec) = stores[i];
                int after = energy + flows[i];
                if (after < 0 || after > spec.Capacity || flows[i] > spec.In || -flows[i] > spec.Out) return false;
            }
            if (supply >= demand)
            {
                // Generated = consumed + charged + wasted: all is drawn, what goes in is no more than is spare.
                if (served != supply || flows.Any(f => f < 0) || moved > supply - demand) return false;
                // What is wasted is wasted only once every store took all it could.
                if (moved < supply - demand && stores.Where((s, i) => flows[i] < PowerNodes.Intake(s)).Any()) return false;
            }
            else
            {
                if (flows.Any(f => f > 0) || served != supply - moved || served > demand) return false;
                if (served < demand && stores.Where((s, i) => -flows[i] < Math.Min(s.Spec.Out, s.Energy)).Any()) return false;
            }
            return true;
        });
    }

    private sealed record Step(int Resting, int Ticks);

    [Property(MaxTest = 60)]
    public Property ConservesEnergyEveryTickWithStoresInBoundsWhateverMakesAndDraws()
    {
        // Each step rests some of the three panels and six users and runs on: every limit of In, Out, full and empty reached.
        var step = from resting in Gen.Choose(0, (1 << 9) - 1) from ticks in Gen.Choose(1, 40) select new Step(resting, ticks);
        var cases = from a in Gen.Choose(0, Bank.Capacity)
                    from b in Gen.Elements(0, 5, Bank.Capacity - 5, Bank.Capacity)
                    from n in Gen.Choose(1, 12)
                    from steps in step.ArrayOf(n)
                    select (a, b, steps);
        return Prop.ForAll(cases.ToArbitrary(), c =>
        {
            var w = Holding(World(panels: 3, second: true, generators: true), (First, c.a), (Second, c.b));
            (int, int)[] machines = [.. Panels, .. Lamps, .. Generators];
            var held = new[] { c.a, c.b };
            foreach (var s in c.steps)
            {
                for (int i = 0; i < machines.Length; i++) w.Do(new RestMachine(IdAt(w, machines[i]), (s.Resting >> i & 1) == 1));
                for (int t = 0; t < s.Ticks; t++)
                {
                    w.Run();
                    var n = Network(w);
                    var now = new[] { Energy(w, First), Energy(w, Second) };
                    var flows = new[] { Level(w, First).Flow, Level(w, Second).Flow };
                    int into = flows.Where(f => f > 0).Sum(), outOf = -flows.Where(f => f < 0).Sum();
                    // Each store moved what it settled, from what it held, and stays between empty and full.
                    if (n.Stored != held.Sum() || n.Flow != flows.Sum()) return false;
                    for (int i = 0; i < 2; i++)
                        if (now[i] != held[i] + flows[i] || now[i] < 0 || now[i] > Bank.Capacity) return false;
                    // Generated + given out = consumed + taken in + wasted, the waste never below 0.
                    if (n.Supply + outOf - into < n.Demand * n.Satisfaction / 1000) return false;
                    // Only what is spare goes in, as much as the stores can take; short, they give all they can up to the shortfall.
                    int spare = Math.Max(0, n.Supply - n.Demand), shortfall = Math.Max(0, n.Demand - n.Supply);
                    int canTake = held.Sum(h => Math.Min(Bank.In, Bank.Capacity - h)), canGive = held.Sum(h => Math.Min(Bank.Out, h));
                    if (into != Math.Min(spare, canTake) || outOf != Math.Min(shortfall, canGive)) return false;
                    if (into > 0 && outOf > 0) return false;
                    held = now;
                }
            }
            return true;
        });
    }
}
