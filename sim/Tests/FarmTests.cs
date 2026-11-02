using Stillwake.Sim.Crafting;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Items;
using Xunit;
using Machines = Stillwake.Content.Machines;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The farm (0060): a grow capsule is a fed station of the "grow" process, on power, turning a seed in its input into
/// 4 biomass and none, one or two seeds; biomass smelts into biofuel; soil is pressed at the Former; seeds come from
/// supply pods.
/// </summary>
public class FarmTests
{
    private const int Width = 12;

    /// <summary>
    /// A solar panel out under the stars over a room with a grow capsule and an oxygen generator; the panel is cabled to
    /// the capsule, and with <paramref name="short"/> to the generator too, which draws 4 of its 5 (0057's tests).
    /// </summary>
    private static Scenario World(bool @short = false)
    {
        string[] rows = ["PPP         ", "            ", "############", "#gg........#", "#gg.GG.....#", "#gg.GG.....#", "############"];
        IReadOnlyList<((int X, int Y) A, (int X, int Y) B)> cables = @short ? [((0, 0), (1, 3)), ((0, 0), (4, 4))] : [((0, 0), (1, 3))];
        return new Scenario(Layout.Parse(rows, null, cables), doubled: true).GiveItems(0, ("seed", 10), ("rock", 2), ("planks", 2));
    }

    /// <summary>A sealed room aired by a vaporizer piped to a vent, its tank and panel under the hull, holding a furnace, and an electric smelter on a panel of its own.</summary>
    private static Scenario Smelting()
    {
        var w = new Scenario(AirTesting.Vaporized(
                ["PPP       ", "          ", "##########", "#FF.TT.EE#", "#FF.TTVEE#", "##########"],
                ["", "", "", "    o", "    ---"], [((0, 0), (7, 3))]), doubled: true)
            .GiveItems(0, ("biomass", 4), ("seed", 2), ("planks", 2));
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(w.At(1, 3)); i++) w.Run();
        return w;
    }

    private static int Capsule(Scenario w) => w.M.Grid.MachinesOf("growCapsule").Single().Id;
    private static int Demand(Scenario w) => w.M.Power.NetworkOf(0)!.Demand;
    private static int Held(Scenario w, int machine, string item) => w.M.Items.Crate(machine).Sum(s => s?.Item == item ? s.Count : 0);
    private static int Output(Scenario w, int machine, string item) => w.M.Items.Crate(machine).Skip(1).Sum(s => s?.Item == item ? s.Count : 0);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static readonly int Crop = Content.Items.Get("seed").SmeltTicks!.Value * 1000;
    private static readonly int Draws = Machines.Get("growCapsule").Draws!.Value;
    private static int StackOf(string item) => Content.Items.Get(item).Stack;

    /// <summary>Moves <paramref name="count"/> of what peer 0 carries of <paramref name="item"/> into slot <paramref name="slot"/> of <paramref name="machine"/>, and runs the tick.</summary>
    private static Scenario Put(Scenario w, int machine, string item, int count = 1, int slot = 0)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
        return w.Do(new Move(new Slot(from), new Target(slot, machine), count)).Run();
    }

    /// <summary>Sets what machine <paramref name="id"/> holds, slot by slot, as no command would: a test's starting stock.</summary>
    private static Scenario Stock(Scenario w, int id, params Stack?[] slots)
    {
        Stacks.SetCrate((ItemsState)w.M.Items.Slice, id, slots);
        return w;
    }

    /// <summary>Gives fed station <paramref name="id"/> <paramref name="progress"/> of work on its input, as ticks at work would.</summary>
    private static Scenario Ripen(Scenario w, int id, int progress)
    {
        var state = (CraftingState)w.M.Crafting.Slice;
        state.Smelts = [.. state.Smelts.Where(s => s.Machine != id), new Smelt(id, w.M.Items.Crate(id)[0]!.Item, progress)];
        state.Smelts.Sort((a, b) => a.Machine.CompareTo(b.Machine));
        return w;
    }

    [Fact]
    public void GrowsASeedIntoFourBiomassInNinetySixHundredTicksAtFullPowerDrawingWhileItWorks()
    {
        var w = World().Run(2);
        int capsule = Capsule(w);
        Assert.Equal(("grow", 1000, 2), (Content.Stations.Get("growCapsule").Process, Content.Stations.Get("growCapsule").Speed, Draws));
        Assert.Equal(9600, Content.Items.Get("seed").SmeltTicks);
        Assert.Equal(0, Demand(w));
        // The tick it is planted is the first of its growth, at the share the network gave it resting.
        Put(w, capsule, "seed", 2);
        Assert.Equal(1000, w.M.Crafting.Progress(capsule));
        Assert.Equal(Crop, w.M.Crafting.WorkOf(capsule));
        Assert.True(w.M.Crafting.Works(capsule));
        w.Run();
        Assert.Equal(Draws, Demand(w));
        w.Run(Crop / 1000 - 3);
        Assert.Equal((0, 2), (Output(w, capsule, "biomass"), Held(w, capsule, "seed")));
        w.Run();
        // Planting used the seed: one left in the input, and what it gave back in the outputs.
        Assert.Equal(4, Output(w, capsule, "biomass"));
        Assert.Equal(new Stack("seed", 1), w.M.Items.Crate(capsule)[0]);
        Assert.InRange(Output(w, capsule, "seed"), 0, 2);
        // The next seed grows from nothing.
        Assert.Equal(0, w.M.Crafting.Progress(capsule));
        w.Run();
        Assert.Equal(1000, w.M.Crafting.Progress(capsule));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void GrowsAtFiveSixthsOnANetworkShortOfWhatItDraws()
    {
        var w = World(@short: true).Run();
        int capsule = Capsule(w);
        Put(w, capsule, "seed");
        // Resting until now, it drew nothing on the tick it began: the panel's 5 covered the generator's 4.
        Assert.Equal(1000, w.M.Crafting.Progress(capsule));
        w.Run();
        int share = w.M.Power.Satisfaction(capsule);
        Assert.Equal(5000 / 6, share);
        int gain = 1000 * share / 1000;
        Assert.Equal(1000 + gain, w.M.Crafting.Progress(capsule));
        int ticks = (Crop - 1000 + gain - 1) / gain;
        Assert.True(ticks > Crop / 1000);
        w.Run(ticks - 2);
        Assert.Equal(0, Output(w, capsule, "biomass"));
        w.Run();
        Assert.Equal(4, Output(w, capsule, "biomass"));
    }

    [Fact]
    public void PausesUnpoweredKeepingItsGrowthAndGrowsOnWhenPoweredAgain()
    {
        var w = World();
        int capsule = Capsule(w);
        Put(w, capsule, "seed").Run(999);
        Assert.Equal(1000 * 1000, w.M.Crafting.Progress(capsule));
        var cut = new Sim.Power.Cut(w.At(0, 0), w.At(1, 3));
        w.Do(cut).Run(500);
        Assert.Empty(w.Refusals);
        Assert.False(w.M.Crafting.Works(capsule));
        Assert.Equal(1000 * 1000, w.M.Crafting.Progress(capsule));
        // Cabled again: it grows on from where it stood, the whole crop in its ticks of power.
        w.Do(new Sim.Power.Lay(w.At(0, 0), w.At(1, 3))).Run();
        Assert.Equal(1001 * 1000, w.M.Crafting.Progress(capsule));
        w.Run(Crop / 1000 - 1002);
        Assert.Equal(0, Output(w, capsule, "biomass"));
        w.Run();
        Assert.Equal(4, Output(w, capsule, "biomass"));
    }

    /// <summary>
    /// Full outputs pause it, resting, its growth kept; and it waits for room for the most a crop may give, biomass and two
    /// seeds, whatever the draw will be.
    /// </summary>
    [Fact]
    public void PausesRestingWhileItsOutputsCannotTakeAWholeCropKeepingItsGrowth()
    {
        var w = World();
        int capsule = Capsule(w);
        Put(w, capsule, "seed").Run(99);
        Assert.Equal(100 * 1000, w.M.Crafting.Progress(capsule));
        // Room for the biomass but not for two seeds more: it pauses as if there were none.
        Stock(w, capsule, new Stack("seed", 1), new Stack("biomass", StackOf("biomass") - 4), new Stack("seed", StackOf("seed") - 1)).Run(3);
        Assert.False(w.M.Crafting.CanSmelt(capsule));
        Assert.False(w.M.Crafting.Works(capsule));
        Assert.Equal(0, Demand(w));
        Assert.Equal(100 * 1000, w.M.Crafting.Progress(capsule));
        // One seed taken out: two more fit, and it grows on from where it stood.
        w.Do(new Move(new Slot(2, capsule), new Target(), 1)).Run(2);
        Assert.Equal(Draws, Demand(w));
        Assert.Equal(102 * 1000, w.M.Crafting.Progress(capsule));
        // Its outputs full of what is never grown: it waits again.
        Stock(w, capsule, new Stack("seed", 1), new Stack("canister", StackOf("canister")), new Stack("canister", StackOf("canister"))).Run(3);
        Assert.Equal(0, Demand(w));
        Assert.Equal(102 * 1000, w.M.Crafting.Progress(capsule));
    }

    /// <summary>
    /// Every crop uses its seed and gives back none, one or two, drawn from the world's Rng by the seed's weights (35, 50,
    /// 15 in a hundred): over two thousand crops each share within a few points, and the farm eats about one seed in five.
    /// </summary>
    [Fact]
    public void EveryCropUsesItsSeedAndGivesBackNoneOneOrTwoByTheSeedsOdds()
    {
        var w = World();
        int capsule = Capsule(w);
        Assert.Equal([35, 50, 15], Content.Items.Get("seed").Returns);
        const int crops = 2000;
        var back = new int[3];
        for (int i = 0; i < crops; i++)
        {
            Stock(w, capsule, new Stack("seed", 5), null, null);
            Ripen(w, capsule, Crop - 1000).Run();
            Assert.Equal(new Stack("seed", 4), w.M.Items.Crate(capsule)[0]);
            Assert.Equal(4, Output(w, capsule, "biomass"));
            back[Output(w, capsule, "seed")]++;
        }
        Assert.Equal(crops, back.Sum());
        Assert.InRange(back[0], crops * 31 / 100, crops * 39 / 100);
        Assert.InRange(back[1], crops * 46 / 100, crops * 54 / 100);
        Assert.InRange(back[2], crops * 12 / 100, crops * 18 / 100);
        Assert.InRange(back[1] + 2 * back[2], crops * 76 / 100, crops * 84 / 100);
    }

    /// <summary>The draw is the world's: a crop completed after a reload gives what it gives without one, and the seeds' sequence follows the world's seed.</summary>
    [Fact]
    public void ACropsSeedsAreDrawnFromTheWorldsRngAliveAcrossASaveAndLoad()
    {
        IEnumerable<int> Crops(uint seed, bool reload)
        {
            var w = new Scenario(World().Layout, seed).GiveItems(0, ("seed", 1));
            int capsule = Capsule(w);
            for (int i = 0; i < 40; i++)
            {
                Stock(w, capsule, new Stack("seed", 1), null, null);
                Ripen(w, capsule, Crop - 2000).Run();
                if (reload) w.Reload();
                w.Run();
                yield return Output(w, capsule, "seed");
            }
        }
        Assert.Equal(Crops(3, reload: false), Crops(3, reload: true));
        Assert.NotEqual(Crops(3, reload: false), Crops(4, reload: false));
    }

    /// <summary>A furnace takes a seed into its input and does nothing with it: no fire lit, no smelt, no complaint. Rock in a capsule likewise.</summary>
    [Fact]
    public void AFurnaceTakesASeedAndDoesNothingAndACapsuleTakesRockAndDoesNothing()
    {
        var w = Smelting();
        int furnace = w.M.Grid.MachinesOf("furnace").Single().Id;
        int fuel = Machines.Get("furnace").FuelSlot!.Value;
        Put(w, furnace, "planks", 2, fuel);
        Put(w, furnace, "seed", 2).Run(100);
        Assert.Empty(w.Refusals);
        Assert.False(w.M.Crafting.CanSmelt(furnace));
        Assert.False(w.M.Crafting.TooCold(furnace));
        Assert.Null(w.M.Crafting.WorkOf(furnace));
        Assert.Equal(new Furnace.Smelting(false), w.M.Furnace.At(furnace));
        Assert.Equal((2, 2), (Held(w, furnace, "seed"), Held(w, furnace, "planks")));

        var farm = World();
        int capsule = Capsule(farm);
        Put(farm, capsule, "rock").Run(100);
        Assert.Empty(farm.Refusals);
        Assert.False(farm.M.Crafting.Works(capsule));
        Assert.False(farm.M.Crafting.TooCold(capsule));
        Assert.Null(farm.M.Crafting.WorkOf(capsule));
        Assert.Equal(0, farm.M.Crafting.Progress(capsule));
        Assert.Equal(0, Demand(farm));
        Assert.Equal(1, Held(farm, capsule, "rock"));
    }

    [Fact]
    public void SmeltsBiomassIntoBiofuelOneForOneAtTheFurnaceAndTheSmelter()
    {
        var w = Smelting();
        int furnace = w.M.Grid.MachinesOf("furnace").Single().Id, smelter = w.M.Grid.MachinesOf("smelter").Single().Id;
        Assert.Equal((160, 1), (Content.Items.Get("biomass").SmeltTicks, Content.Items.Get("biomass").Heat));
        Assert.Equal([("biofuel", 1)], Content.Items.Get("biomass").Yields);
        Put(w, furnace, "planks", 1, Machines.Get("furnace").FuelSlot!.Value);
        Put(w, furnace, "biomass", 2);
        Put(w, smelter, "biomass", 2);
        // The smelter twice as fast; the furnace from the tick after it lights.
        w.Run(2 * 160 / 2);
        Assert.Equal((0, 2), (Held(w, smelter, "biomass"), Held(w, smelter, "biofuel")));
        w.Run(2 * 160);
        Assert.Equal((0, 2), (Held(w, furnace, "biomass"), Held(w, furnace, "biofuel")));
        Assert.Empty(w.Refusals);
    }

    /// <summary>Soil is pressed of a rock and a plank at the Former in 60 ticks; the lifepod and the assembler refuse it as not made there.</summary>
    [Fact]
    public void PressesSoilOfGravelAndPlanksAtTheFormerAloneInSixtyTicks()
    {
        var w = new Scenario(Layout.Parse(
                ["PPP   PPP      ", "               ", "###############", "#OOO..........#", "#OOO.MM.YY....#", "#OOO.MM.YY....#", "###############"],
                null, [((0, 0), (5, 4)), ((6, 0), (8, 4))]), doubled: true)
            .GiveItems(0, ("gravel", 6), ("planks", 3));
        int former = w.M.Grid.MachinesOf("former").Single().Id;
        int pod = w.M.Grid.MachinesOf("lifepod").Single().Id, assembler = w.M.Grid.MachinesOf("assembler").Single().Id;
        Assert.Equal(60, Content.Crafts.Get("soil").Ticks);
        Assert.Equal([("gravel", 2), ("planks", 1)], Content.Crafts.Get("soil").Cost);
        w.Do(new Craft(pod, "soil", 1)).Do(new Craft(assembler, "soil", 1)).Run();
        Assert.Equal(["crafting.notHere", "crafting.notHere"], Reasons(w));
        w.Do(new Craft(former, "soil", 1)).Run();
        Assert.Equal(2, w.Refusals.Count);
        Assert.Equal((4, 2), (w.M.Items.Inventory(0).Sum(s => s?.Item == "gravel" ? s.Count : 0), w.M.Items.Inventory(0).Sum(s => s?.Item == "planks" ? s.Count : 0)));
        w.Run(60 - 2);
        Assert.Equal(0, Held(w, former, "soil"));
        w.Run();
        Assert.Equal(1, Held(w, former, "soil"));
    }

    /// <summary>One supply pod in two holds seeds, one or two (0060); the soil clod is their only other source.</summary>
    [Fact]
    public void ASupplyPodHoldsOneOrTwoSeedsOneTimeInTwo()
    {
        var rng = new Rng(new RngState { S = 11 });
        const int pods = 2000;
        int with = 0;
        var counts = new HashSet<int>();
        for (int i = 0; i < pods; i++)
        {
            var seeds = DebrisModule.LootOf("supplyPod", rng).Where(l => l.Item == "seed").ToList();
            if (seeds.Count == 0) continue;
            with++;
            counts.Add(seeds.Single().Count);
        }
        Assert.InRange(with, pods * 45 / 100, pods * 55 / 100);
        Assert.Equal([1, 2], counts.Order());
        Assert.Equal(["supplyPod", "soilClod"], Content.Loot.All.Where(l => l.Entries.Any(e => e.Item == "seed")).Select(l => l.Container));
    }

    /// <summary>The fuzz draws crafts at the capsule, refused as not made there, and the items fuzz moves into and out of it.</summary>
    [Fact]
    public void TheFuzzDrawsCraftsAndMovesAtTheCapsule()
    {
        var w = World();
        int capsule = Capsule(w);
        w.Do(new Move(new Slot(0), new Target(0, capsule), 3)).Run();
        var rng = new Rng(new RngState { S = 5 });
        int crafts = 0, moves = 0, taken = 0;
        for (int i = 0; i < 4000; i++)
        {
            var c = i % 2 == 0 ? w.M.Crafting.Fuzz(rng, 0) : w.M.Items.Fuzz(rng, 0);
            if (c is null) continue;
            int refused = w.Refusals.Count;
            w.Do(c).Run();
            if (c is Craft { } k && k.Machine == capsule) crafts++;
            if (c is Move { } m && (m.Src.Crate == capsule || m.Dst.Crate == capsule))
            {
                moves++;
                if (w.Refusals.Count == refused) taken++;
            }
        }
        Assert.True(crafts > 50, $"{crafts} crafts at the capsule");
        Assert.True(moves > 50, $"{moves} moves at the capsule");
        Assert.True(taken > 0 && taken < moves, $"{taken} of {moves} moves taken");
        Assert.Contains(w.Refusals, r => r.Reason == "crafting.notHere" && r.Command is Craft { } k && k.Machine == capsule);
    }
}
