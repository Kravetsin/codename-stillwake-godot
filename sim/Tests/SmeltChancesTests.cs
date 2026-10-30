using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using static Stillwake.Sim.Tests.TestItems;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>
/// What a fed station makes besides its yields by chance (<c>ItemSpec.Byproducts</c>), and smelts that take several of
/// their input (<c>ItemSpec.SmeltTakes</c>): no content has either yet, so on the test items (<see cref="TestItems"/>).
/// </summary>
public class SmeltChancesTests
{
    /// <summary>A solar panel out under the stars cabled to an electric smelter in a room, as the smelter's tests.</summary>
    private static Scenario World(uint seed = 1)
    {
        string[] rows = ["PPP         ", "            ", "############", "#EE.GG.....#", "#EE.GG.....#", "############"];
        return new Scenario(Layout.Parse(rows, null, [((0, 0), (1, 3))]), seed, doubled: true).GiveItems(0, (Ore, 20), (Gravel, 30));
    }

    /// <summary>A sealed room aired by a vaporizer piped to a vent, its tank and panel under the hull, holding a furnace, as the furnace's tests.</summary>
    private static Scenario Furnace()
    {
        var w = new Scenario(AirTesting.Vaporized(["##########", "#FF.TT...#", "#FF.TTV..#", "##########"], ["", "    o", "    ---"]), doubled: true)
            .GiveItems(0, (Gravel, 4), ("planks", 2));
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(w.At(1, 1)); i++) w.Run();
        return w;
    }

    private static int Smelter(Scenario w) => w.M.Grid.MachinesOf("smelter").Single().Id;
    private static int Demand(Scenario w) => w.M.Power.NetworkOf(0)!.Demand;
    private static int Input(Scenario w, int machine) => w.M.Items.Crate(machine)[0]?.Count ?? 0;
    private static int Output(Scenario w, int machine, string item) => w.M.Items.Crate(machine).Skip(1).Sum(s => s?.Item == item ? s.Count : 0);
    private static int Work(string item) => Content.Items.Get(item).SmeltTicks!.Value * 1000;
    private static readonly int Speed = Content.Stations.Get("smelter").Speed;

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

    /// <summary>One ore smelted at the smelter from nothing in its outputs, its work set to its last tick: the crystals it gave.</summary>
    private static int SmeltOre(Scenario w, int smelter)
    {
        Stock(w, smelter, new Stack(Ore, 1), null, null, null);
        var state = (CraftingState)w.M.Crafting.Slice;
        state.Smelts = [.. state.Smelts.Where(s => s.Machine != smelter), new Smelt(smelter, Ore, Work(Ore) - Speed)];
        state.Smelts.Sort((a, b) => a.Machine.CompareTo(b.Machine));
        w.Run();
        Assert.Equal((0, 1, 1), (Input(w, smelter), Output(w, smelter, "iron"), Output(w, smelter, "scrap")));
        return Output(w, smelter, "crystal");
    }

    /// <summary>
    /// The byproduct is drawn from the world's Rng as the smelt completes: on world seed 1 the first five ores give none and
    /// the next two a crystal each, pinned; the sequence is the world's, alive across a reload, and another seed draws another.
    /// </summary>
    [Fact]
    public void AByproductComesOrNotAsTheWorldsRngDrawsItAliveAcrossAReload()
    {
        IEnumerable<int> Crystals(uint seed, bool reload)
        {
            var w = World(seed).Run(2);
            int smelter = Smelter(w);
            for (int i = 0; i < 24; i++)
            {
                if (reload) w.Reload();
                yield return SmeltOre(w, smelter);
            }
        }
        var first = Crystals(1, reload: false).ToList();
        Assert.Equal([0, 0, 0, 0, 0, 1, 1], first.Take(7));
        Assert.Equal(first, Crystals(1, reload: true));
        Assert.NotEqual(first, Crystals(2, reload: false));
    }

    /// <summary>Its one-in is its odds: over two thousand ores about half give a crystal, every one its iron and scrap.</summary>
    [Fact]
    public void AByproductComesAboutOneSmeltInItsOneIn()
    {
        var w = World().Run(2);
        int smelter = Smelter(w);
        Assert.Equal(2, Content.Items.Get(Ore).Byproducts!.Single().OneIn);
        const int smelts = 2000;
        int crystals = 0;
        for (int i = 0; i < smelts; i++) crystals += SmeltOre(w, smelter);
        Assert.InRange(crystals, smelts * 46 / 100, smelts * 54 / 100);
    }

    /// <summary>
    /// It works only with room for every byproduct at its most: outputs that take the yields but not the crystal hold it
    /// still, resting, its reason no room rather than too few; a slot freed, it works.
    /// </summary>
    [Fact]
    public void StandsWhileItsOutputsLackRoomForTheMostItMayMake()
    {
        var w = World().Run(2);
        int smelter = Smelter(w);
        Stock(w, smelter, new Stack(Ore, 1), new Stack("canister", 5), new Stack("iron", 499), new Stack("scrap", 499)).Run();
        Assert.False(w.M.Crafting.CanSmelt(smelter));
        Assert.False(w.M.Crafting.TooFew(smelter));
        Assert.False(w.M.Crafting.Works(smelter));
        Assert.Equal(0, w.M.Crafting.Progress(smelter));
        w.Run();
        Assert.Equal(0, Demand(w));
        Stock(w, smelter, new Stack(Ore, 1), null, new Stack("iron", 499), new Stack("scrap", 499)).Run();
        Assert.True(w.M.Crafting.Works(smelter));
        Assert.Equal(Speed, w.M.Crafting.Progress(smelter));
    }

    /// <summary>
    /// A smelt that takes four waits, resting, while its input holds fewer (<c>TooFew</c>), works once a fourth comes,
    /// and takes all four as it completes into its one glass; what is left short waits again.
    /// </summary>
    [Fact]
    public void WaitsForAsManyAsOneSmeltTakesAndTakesThemAllAsItCompletes()
    {
        var w = World().Run(2);
        int smelter = Smelter(w);
        Assert.Equal(4, Content.Items.Get(Gravel).SmeltTakes);
        Put(w, smelter, Gravel, 3);
        Assert.True(w.M.Crafting.TooFew(smelter));
        Assert.False(w.M.Crafting.CanSmelt(smelter));
        Assert.False(w.M.Crafting.Works(smelter));
        Assert.Equal(0, w.M.Crafting.Progress(smelter));
        w.Run();
        Assert.Equal(0, Demand(w));
        Put(w, smelter, Gravel, 3);
        Assert.False(w.M.Crafting.TooFew(smelter));
        Assert.Equal(Speed, w.M.Crafting.Progress(smelter));
        w.Run(Work(Gravel) / Speed - 2);
        Assert.Equal((6, 0), (Input(w, smelter), Output(w, smelter, "glass")));
        w.Run();
        Assert.Equal((2, 1), (Input(w, smelter), Output(w, smelter, "glass")));
        Assert.True(w.M.Crafting.TooFew(smelter));
        Assert.False(w.M.Crafting.Works(smelter));
        Assert.Equal(0, w.M.Crafting.Progress(smelter));
    }

    /// <summary>A furnace with too few of its input says so rather than no room, and lights no fire till a smelt's worth comes.</summary>
    [Fact]
    public void AFurnaceWithTooFewSaysSoAndLightsNoFire()
    {
        var w = Furnace();
        int furnace = w.M.Grid.MachinesOf("furnace").Single().Id;
        Put(w, furnace, "planks", 1, slot: 1);
        Put(w, furnace, Gravel, 3).Run(5);
        var at = w.M.Furnace.At(furnace);
        Assert.Equal(("crafting.tooFew", false), (at.Problem, at.Burning));
        Put(w, furnace, Gravel, 1).Run(2);
        at = w.M.Furnace.At(furnace);
        Assert.Equal((null, true), (at.Problem, at.Burning));
        Assert.True(w.M.Crafting.Progress(furnace) > 0);
    }

    /// <summary>
    /// Items are conserved through both rules, whatever is put in or taken out and whenever: every ore smelted is one iron
    /// and one scrap and at most one crystal, every glass four gravel, and a station completes at most one smelt a tick.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property ConservesItemsThroughByproductsAndSmeltsOfSeveral()
    {
        var step =
            from kind in Gen.Frequency((4, Gen.Constant(0)), (2, Gen.Constant(1)), (3, Gen.Constant(2)))
            from ore in Gen.Elements(true, false)
            from count in Gen.Choose(1, 9)
            from slot in Gen.Choose(0, 3)
            from ticks in Gen.Frequency((4, Gen.Choose(0, 3)), (2, Gen.Choose(5, 40)))
            select (Kind: kind, Ore: ore, Count: count, Slot: slot, Ticks: ticks);
        var steps = from n in Gen.Choose(0, 40) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = World().Run(2);
            int smelter = Smelter(w);
            int Total(string item) =>
                w.M.Items.Inventory(0).Concat(w.M.Items.Crate(smelter)).Sum(s => s?.Item == item ? s.Count : 0);
            int ores = Total(Ore), gravel = Total(Gravel), smelts = 0;
            foreach (var s in all)
            {
                if (s.Kind == 0) Put(w, smelter, s.Ore ? Ore : Gravel, s.Count);
                else if (s.Kind == 1) w.Do(new Move(new Slot(s.Slot, smelter), new Target(), 999)).Run();
                for (int t = 0; t <= (s.Kind == 2 ? s.Ticks : 0); t++)
                {
                    if (s.Kind == 2) w.Run();
                    int oreSmelts = ores - Total(Ore), glass = Total("glass");
                    if (Total("iron") != oreSmelts || Total("scrap") != oreSmelts) return false;
                    if (Total("crystal") < 0 || Total("crystal") > oreSmelts) return false;
                    if (gravel - Total(Gravel) != glass * Content.Items.Get(Gravel).SmeltTakes) return false;
                    if (oreSmelts + glass > smelts + 1 || oreSmelts + glass < smelts) return false;
                    smelts = oreSmelts + glass;
                }
            }
            return true;
        });
    }
}
