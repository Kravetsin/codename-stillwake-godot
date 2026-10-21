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
    private static int At(int x, int y) => y * W + x;

    /// <summary>
    /// A cave in stone: a geode in the left wall at (0, 1), an ice crystal hanging at (3, 1), a teal cap at (2, 2), an amber
    /// crystal at (3, 2), a violet cap at (5, 2), a stalagmite at (6, 2), a copper vein in the right wall at (7, 2).
    /// </summary>
    private static Scenario Cave() => new(Layout.Parse("""
        ########
        #......#
        #......#
        ########
        """).WithMaterials("""
        tttttttt
        t......t
        t......t
        tttttttt
        """) with
    {
        Flora = [(At(0, 1), "geode"), (At(3, 1), "crystalIceHang"), (At(2, 2), "glowcapTeal"), (At(3, 2), "crystalAmber"),
            (At(5, 2), "glowcapViolet"), (At(6, 2), "stalagmite"), (At(7, 2), "veinCopper")],
    });

    private static readonly string Axe = Weapons.Axe.Item;

    /// <summary>The axe's hits that break stone.</summary>
    private static readonly int Hits = (Materials.Get("stone").Strength + Weapons.Axe.Damage - 1) / Weapons.Axe.Damage;

    private static int Count(Scenario w, string item) => w.M.Items.Totals(0).Where(t => t.Item == item).Sum(t => t.Count);
    private static string[] Kinds(Scenario w) => w.M.Building.Plants.Select(p => p.Kind).ToArray();

    private static Scenario DigOut(Scenario w, int cell)
    {
        for (int i = 0; i < Hits; i++) w.Do(new Dig(cell, Axe)).Run();
        Assert.Null(w.M.Grid.WallAt(cell));
        return w;
    }

    [Fact]
    public void ADugCopperVeinGivesItsOreToTheDiggerWithItsRock()
    {
        var w = DigOut(Cave(), At(7, 2));
        Assert.Empty(w.Refusals);
        Assert.Equal((2, 1), (Count(w, "copperOre"), Count(w, "rock")));
        Assert.DoesNotContain("veinCopper", Kinds(w));
        Assert.Empty(w.M.Items.Floor);
    }

    [Fact]
    public void ADugGeodeGivesSixCrystals() =>
        Assert.Equal(6, Count(DigOut(Cave(), At(0, 1)), "crystal"));

    [Fact]
    public void BreakingTheFloorUnderACrystalOrTheCeilingOverOneGivesItToTheDigger()
    {
        var w = DigOut(Cave(), At(3, 3));
        Assert.Equal((1, 0), (Count(w, "crystal"), Count(w, "ice")));
        DigOut(w, At(3, 0));
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
        w.Do(new Dig(At(2, 2), Axe)).Do(new Dig(At(5, 2), Axe)).Do(new Dig(At(6, 2), Axe)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((2, 1), (Count(w, "biomass"), Count(w, "spore")));
        Assert.Equal(3, w.M.Items.Totals(0).Sum(t => t.Count));
        Assert.Equal(["geode", "crystalIceHang", "crystalAmber", "veinCopper"], Kinds(w));
    }

    [Fact]
    public void WhatAFullInventoryCannotTakeFallsWhereTheGrowthWas()
    {
        var w = Cave().GiveItems(0, ("scrap", Content.Items.Get("scrap").Stack * Inventory.Slots));
        w.Do(new Dig(At(2, 2), Axe)).Run();
        DigOut(w, At(7, 2));
        var floor = w.M.Items.Floor.Select(s => (s.Item, s.Count, s.Cell)).OrderBy(s => s.Item, StringComparer.Ordinal);
        Assert.Equal([("biomass", 2, At(2, 2)), ("copperOre", 2, At(7, 2)), ("rock", 1, At(7, 2))], floor);
    }

    [Fact]
    public void AFelledAcaciaGivesPlanksABushASeedAndASurfaceRockRock()
    {
        var layout = Layout.Parse("""
            ........
            ........
            ########
            """).WithMaterials("""
            ........
            ........
            ssssssss
            """) with { Flora = [(At(1, 1), "acacia"), (At(4, 1), "bush"), (At(6, 1), "rock")] };
        var w = new Scenario(layout);
        w.Do(new Dig(At(1, 0), Axe)).Do(new Dig(At(4, 1), Axe)).Run();
        Assert.Equal((4, 1), (Count(w, "planks"), Count(w, "seed")));
        // The rock's soil dug from under it: it goes to the digger too, with the soil.
        w.Do(new Dig(At(6, 2), Axe)).Do(new Dig(At(6, 2), Axe)).Run();
        Assert.Equal((2, 1), (Count(w, "rock"), Count(w, "soil")));
        Assert.Empty(w.M.Building.Plants);
    }

    /// <summary>A furnace and an electric smelter in an aired room, powered; peer 0 carrying copper ore and planks.</summary>
    private static Scenario Smelting()
    {
        var w = new Scenario(Layout.Parse(
                ["PPP       ", "          ", "##########", "#FF.TT.EE#", "#FF.TTVEE#", "##########"],
                ["", "", "", "    o", "    ---"], [((0, 0), (7, 3))]))
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

    [Fact]
    public void AMotorIsPressedOfARotorAndFourCopperAtTheFormerAlone()
    {
        var w = new Scenario(Layout.Parse(
                ["PPP   PPP      ", "               ", "###############", "#OOO..........#", "#OOO.MM.YY....#", "#OOO.MM.YY....#", "###############"],
                null, [((0, 0), (5, 4)), ((6, 0), (8, 4))]))
            .GiveItems(0, ("rotor", 1), ("copper", 4));
        int former = w.M.Grid.MachinesOf("former").Single().Id, pod = w.M.Grid.MachinesOf("lifepod").Single().Id;
        int ticks = Content.Crafts.Get("motor").Ticks;
        Assert.Equal([("rotor", 1), ("copper", 4)], Content.Crafts.Get("motor").Cost);
        w.Do(new Craft(pod, "motor", 1)).Run();
        Assert.Equal(["crafting.notHere"], w.Refusals.Select(r => r.Reason));
        w.Do(new Craft(former, "motor", 1)).Run();
        Assert.Single(w.Refusals);
        Assert.Equal((0, 0), (Count(w, "rotor"), Count(w, "copper")));
        w.Run(ticks - 2);
        Assert.Equal(0, Held(w, former, "motor"));
        w.Run();
        Assert.Equal(1, Held(w, former, "motor"));
    }
}
