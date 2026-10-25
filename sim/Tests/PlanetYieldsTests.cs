using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using Craft = Stillwake.Sim.Crafting.Craft;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>
/// What a planet's growth gives (0080): felled by a hit, or with the wall it holds on to, to whoever broke it; copper ore
/// smelted into copper, and the motor of it pressed at the Former.
/// </summary>
public class PlanetYieldsTests
{
    private const int W = 8;
    /// <summary>The drawn map's cell at (<paramref name="x"/>, <paramref name="y"/>): where its growths are listed, before it doubles.</summary>
    private static int At(int x, int y) => y * W + x;

    /// <summary>The lower row of the drawn character at (<paramref name="x"/>, <paramref name="y"/>): a floor growth's own cell, the wall a ceiling's hangs from.</summary>
    private static int Bottom(Scenario w, int x, int y) => w.At(x, y) + w.Layout.Width;

    /// <summary>
    /// A cave in stone: a geode in the left wall at (0, 1), an ice crystal hanging at (3, 1), a teal cap at (2, 2), an amber
    /// crystal at (3, 2), a violet cap at (5, 2), a stalagmite at (6, 2); the right wall at (7, 2) a copper vein.
    /// </summary>
    private static Scenario Cave() => new(Layout.Parse("""
        ########
        #......#
        #......#
        ########
        """).WithMaterials("""
        tttttttt
        t......t
        t......c
        tttttttt
        """) with
    {
        Flora = [(At(0, 1), "geode"), (At(3, 1), "crystalIceHang"), (At(2, 2), "glowcapTeal"), (At(3, 2), "crystalAmber"),
            (At(5, 2), "glowcapViolet"), (At(6, 2), "stalagmite")],
    }, doubled: true);

    private static readonly string Axe = Weapons.Axe.Item;

    /// <summary>The axe's hits that break stone.</summary>
    private static readonly int Hits = (Materials.Get("stone").Strength + Weapons.Axe.Damage - 1) / Weapons.Axe.Damage;

    private static int Count(Scenario w, string item) => w.M.Items.Totals(0).Where(t => t.Item == item).Sum(t => t.Count);
    private static string[] Kinds(Scenario w) => w.M.Building.Plants.Select(p => p.Kind).ToArray();

    private static Scenario DigOut(Scenario w, int cell)
    {
        for (int i = 0; i < Hits; i++) w.Do(HewTesting.Dig(w, cell, Axe)).Run();
        Assert.Null(w.M.Grid.WallAt(cell));
        return w;
    }

    [Fact]
    public void ADugCopperVeinGivesItsOreToTheDiggerAndLeavesTheStoneItRanThroughBehind()
    {
        var w = Cave();
        DigOut(w, w.At(7, 2));
        Assert.Empty(w.Refusals);
        Assert.Equal((1, 0), (Count(w, "copperOre"), Count(w, "rock")));
        Assert.Equal("stone", w.M.Grid.BackAt(w.At(7, 2))?.Id);
        Assert.Empty(w.M.Items.Floor);
    }

    [Fact]
    public void ADugGeodeGivesSixCrystals()
    {
        var w = Cave();
        Assert.Equal(6, Count(DigOut(w, w.At(0, 1)), "crystal"));
    }

    [Fact]
    public void BreakingTheFloorUnderACrystalOrTheCeilingOverOneGivesItToTheDigger()
    {
        var w = Cave();
        DigOut(w, w.At(3, 3));
        Assert.Equal((1, 0), (Count(w, "crystal"), Count(w, "ice")));
        DigOut(w, Bottom(w, 3, 0));
        Assert.Equal(1, Count(w, "ice"));
        Assert.DoesNotContain("crystalAmber", Kinds(w));
        Assert.DoesNotContain("crystalIceHang", Kinds(w));
        // What the next wall along held stands: the teal cap's floor was not dug.
        Assert.Contains("glowcapTeal", Kinds(w));
    }

    [Fact]
    public void AHitOnATealCapGivesBiomassOnAVioletOneSporesAndOnAStalagmiteNothing()
    {
        var w = Cave();
        w.Do(HewTesting.Dig(w, w.At(2, 2), Axe)).Do(HewTesting.Dig(w, w.At(5, 2), Axe)).Do(HewTesting.Dig(w, w.At(6, 2), Axe)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((2, 1), (Count(w, "biomass"), Count(w, "spore")));
        Assert.Equal(3, w.M.Items.Totals(0).Sum(t => t.Count));
        // By own cell: the ice crystal's is its block's top row, the amber crystal's its block's bottom (0104).
        Assert.Equal(["geode", "crystalIceHang", "crystalAmber"], Kinds(w));
    }

    [Fact]
    public void WhatAFullInventoryCannotTakeFallsWhereTheGrowthWas()
    {
        var w = Cave().GiveItems(0, ("scrap", Content.Items.Get("scrap").Stack * Inventory.Slots));
        w.Do(HewTesting.Dig(w, w.At(2, 2), Axe)).Run();
        DigOut(w, w.At(7, 2));
        var floor = w.M.Items.Floor.Select(s => (s.Item, s.Count, s.Cell)).OrderBy(s => s.Item, StringComparer.Ordinal);
        Assert.Equal([("biomass", 2, Bottom(w, 2, 2)), ("copperOre", 1, w.At(7, 2))], floor);
    }

    [Fact]
    public void AFelledAcaciaGivesPlanksABushASeedAndASurfaceRockRock()
    {
        var w = Meadow();
        Fell(w, w.At(1, 0));
        Fell(w, w.At(4, 1));
        Assert.Equal((10, 1), (Count(w, "planks"), Count(w, "seed")));
        // The rock's soil dug from under it: it goes to the digger too, whole, with the soil.
        w.Do(HewTesting.Dig(w, w.At(6, 2), Axe)).Do(HewTesting.Dig(w, w.At(6, 2), Axe)).Run();
        Assert.Equal((2, 1), (Count(w, "rock"), Count(w, "soil")));
        Assert.Empty(w.M.Building.Plants);
        Assert.Empty(w.M.Building.Cracks);
    }

    /// <summary>Soil under open sky: an acacia at (1, 1), its trunk up through (1, 0); a bush at (4, 1); a rock at (6, 1).</summary>
    private static Scenario Meadow() => new(Layout.Parse("""
        ........
        ........
        ########
        """).WithMaterials("""
        ........
        ........
        ssssssss
        """) with { Flora = [(At(1, 1), "acacia"), (At(4, 1), "bush"), (At(6, 1), "rock")] }, doubled: true);

    /// <summary>Hits <paramref name="cell"/> with the axe, a hit a tick, till what stands there is felled; the hits it took.</summary>
    private static int Fell(Scenario w, int cell)
    {
        int count = w.M.Building.Plants.Count, hits = 0;
        while (w.M.Building.Plants.Count == count)
        {
            Assert.True(++hits <= 100, $"nothing at {cell} felled");
            w.Do(HewTesting.Dig(w, cell, Axe)).Run();
        }
        Assert.Empty(w.Refusals);
        return hits;
    }

    [Fact]
    public void ATreeTakesThreeSecondsOfTheAxeARockTwoABushOneAndACapOneHit()
    {
        var w = Meadow();
        int axe = Weapons.Axe.Damage, pace = Weapons.Axe.Pace;
        Assert.Equal((12, 8, 4), (Flora.Get("acacia").Strength, Flora.Get("rock").Strength, Flora.Get("bush").Strength));
        // Swings at the axe's pace, the first at once: a tree's sixth lands two and a half seconds in, three of swinging.
        Assert.Equal(Flora.Get("acacia").Strength / axe, Fell(w, w.At(1, 1)));
        Assert.Equal(3 * World.TicksPerSecond, Flora.Get("acacia").Strength / axe * pace);
        Assert.Equal(Flora.Get("rock").Strength / axe, Fell(w, w.At(6, 1)));
        Assert.Equal(Flora.Get("bush").Strength / axe, Fell(w, w.At(4, 1)));
        var cave = Cave();
        Assert.Equal(1, Fell(cave, cave.At(2, 2)));
    }

    [Fact]
    public void ATreesHitsAddUpWhicheverOfItsCellsTheyLandOnAndHealUnhitForAWhile()
    {
        var w = Meadow();
        int axe = Weapons.Axe.Damage;
        w.Do(HewTesting.Dig(w, w.At(1, 1), Axe)).Run().Do(HewTesting.Dig(w, w.At(1, 0), Axe)).Run();
        // Cracked by its own cell, wherever it was hit; still standing, nothing given.
        Assert.Equal([new Crack(Bottom(w, 1, 1), 2 * axe, w.M.Building.Cracks[0].At)], w.M.Building.Cracks);
        Assert.Contains("acacia", w.M.Building.Plants.Select(p => p.Kind));
        Assert.Equal(0, Count(w, "planks"));
        w.Run(Builds.CrackHeals);
        Assert.Empty(w.M.Building.Cracks);
        Assert.Equal(Flora.Get("acacia").Strength / axe, Fell(w, w.At(1, 0)));
    }

    [Fact]
    public void ATreeCrackedAndThenFelledByItsGroundLeavesNoCrackBehind()
    {
        var w = Meadow();
        w.Do(HewTesting.Dig(w, w.At(1, 0), Axe)).Run();
        Assert.Single(w.M.Building.Cracks);
        // Its soil dug out from under it: the tree goes whole to the digger, its crack with it.
        w.Do(HewTesting.Dig(w, w.At(1, 2), Axe)).Run().Do(HewTesting.Dig(w, w.At(1, 2), Axe)).Run();
        Assert.Equal((10, 1), (Count(w, "planks"), Count(w, "soil")));
        Assert.Empty(w.M.Building.Cracks);
    }

    [Fact]
    public void DugMicaGivesItsFlakesAndAHeatCrackMagnetite()
    {
        var w = new Scenario(Layout.Parse("""
            ########
            #......#
            ########
            ########
            """).WithMaterials("""
            tttttttt
            t......t
            tttttttt
            tttttttt
            """) with { Flora = [(At(0, 1), "mica"), (At(3, 2), "heatCrack")] }, doubled: true);
        DigOut(w, w.At(0, 1));
        DigOut(w, w.At(3, 2));
        Assert.Equal((1, 2, 2), (Count(w, "mica"), Count(w, "magnetite"), Count(w, "rock")));
    }

    /// <summary>A furnace and an electric smelter in an aired room, powered; peer 0 carrying copper ore and planks.</summary>
    private static Scenario Smelting()
    {
        var w = new Scenario(Layout.Parse(
                ["PPP       ", "          ", "##########", "#FF.TT.EE#", "#FF.TTVEE#", "##########"],
                ["", "", "", "    o", "    ---"], [((0, 0), (7, 3))]), doubled: true)
            .GiveItems(0, ("copperOre", 4), ("planks", 2));
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(w.At(1, 3)); i++) w.Run();
        return w;
    }

    private static int Held(Scenario w, int machine, string item) => w.M.Items.Crate(machine).Sum(s => s?.Item == item ? s.Count : 0);

    private static Scenario Put(Scenario w, int machine, string item, int count = 1, int slot = 0)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
        return w.Do(new Move(new Slot(from), new Target(slot, machine), count)).Run();
    }

    [Fact]
    public void CopperOreSmeltsIntoCopperOneForOneAtTheFurnaceAndTheSmelter()
    {
        var w = Smelting();
        int furnace = w.M.Grid.MachinesOf("furnace").Single().Id, smelter = w.M.Grid.MachinesOf("smelter").Single().Id;
        int ticks = Content.Items.Get("copperOre").SmeltTicks!.Value;
        Put(w, furnace, "planks", 1, Machines.Get("furnace").FuelSlot!.Value);
        Put(w, furnace, "copperOre", 2);
        Put(w, smelter, "copperOre", 2);
        // The smelter twice as fast; the furnace from the tick after it lights.
        w.Run(2 * ticks / 2);
        Assert.Equal((0, 2), (Held(w, smelter, "copperOre"), Held(w, smelter, "copper")));
        w.Run(2 * ticks);
        Assert.Equal((0, 2), (Held(w, furnace, "copperOre"), Held(w, furnace, "copper")));
        Assert.Empty(w.Refusals);
    }

    /// <summary>A lifepod, a Former and an assembler, the Former and the assembler each on a panel of their own.</summary>
    private static Scenario Workshop() => new(Layout.Parse(
        ["PPP   PPP      ", "               ", "###############", "#OOO..........#", "#OOO.MM.YY....#", "#OOO.MM.YY....#", "###############"],
        null, [((0, 0), (5, 4)), ((6, 0), (8, 4))]), doubled: true);

    [Fact]
    public void AMotorIsPressedOfARotorTwoCopperAndAMagnetiteAtTheFormerAlone()
    {
        var w = Workshop().GiveItems(0, ("rotor", 1), ("copper", 2), ("magnetite", 1));
        int former = w.M.Grid.MachinesOf("former").Single().Id, pod = w.M.Grid.MachinesOf("lifepod").Single().Id;
        int ticks = Content.Crafts.Get("motor").Ticks;
        Assert.Equal([("rotor", 1), ("copper", 2), ("magnetite", 1)], Content.Crafts.Get("motor").Cost);
        w.Do(new Craft(pod, "motor", 1)).Run();
        Assert.Equal(["crafting.notHere"], w.Refusals.Select(r => r.Reason));
        w.Do(new Craft(former, "motor", 1)).Run();
        Assert.Single(w.Refusals);
        Assert.Equal((0, 0, 0), (Count(w, "rotor"), Count(w, "copper"), Count(w, "magnetite")));
        w.Run(ticks - 2);
        Assert.Equal(0, Held(w, former, "motor"));
        w.Run();
        Assert.Equal(1, Held(w, former, "motor"));
    }

    [Fact]
    public void AFormerPressesAMotorFromItsThreeInputs()
    {
        var w = Workshop().GiveItems(0, ("rotor", 1), ("copper", 2), ("magnetite", 1));
        int former = w.M.Grid.MachinesOf("former").Single().Id;
        Assert.Equal(3, Machines.Get("former").Inputs);
        Put(w, former, "rotor", 1, 0);
        Put(w, former, "copper", 2, 1);
        Put(w, former, "magnetite", 1, 2);
        w.Do(new Craft(former, "motor", 1, FromInputs: true)).Run(Content.Crafts.Get("motor").Ticks + 1);
        Assert.Empty(w.Refusals);
        Assert.Equal((1, 0, 0, 0), (Held(w, former, "motor"), Held(w, former, "rotor"), Held(w, former, "copper"), Held(w, former, "magnetite")));
    }
}
