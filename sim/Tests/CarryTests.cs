using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Places;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// A tool's carry (unified-damage): a wall or piece a blow breaks passes its weapon's share of what went over to the cell one step
/// further along the line from where the blow came from, the pickaxe's 70%.
/// </summary>
public class CarryTests
{
    private static readonly Weapon Pick = Weapons.Pickaxe;
    private const int Right = 0;

    private static (int X, int Y) Mid(int x, int y) => (x * 1000 + 500, y * 1000 + 500);

    private static int At(Scenario w, int x, int y) => y * w.M.Grid.Width + x;

    private static (int Cell, int Damage)[] Cracks(Scenario w) => [.. w.M.Building.Cracks.Select(c => (c.Cell, c.Damage))];

    private static readonly int Soil = Materials.Get("soil").Strength;

    /// <summary>What the pickaxe's blow carries on past a fresh cell of <paramref name="strength"/>.</summary>
    private static int Carry(int strength) => (Pick.BlockDamage - strength) * Pick.Carries / 100;

    // A tunnel a cell tall, bedrock over and under it, so a blow from its mouth strikes its face alone.
    private const string Tunnel = """
        ....................
        ....................
        ............########
        ............########
        ............########
        ....................
        """;

    /// <summary>The tunnel, <paramref name="row"/> (8 characters) its middle row from column 12, bedrock round it; in <paramref name="zone"/>, or all known.</summary>
    private static Scenario Bored(string row, Box? zone = null)
    {
        string materials = "....................\n....................\n............bbbbbbbb\n............" + row + "\n............bbbbbbbb\n....................";
        var layout = Layout.Parse(Tunnel).WithMaterials(materials).OnPlanet();
        return new(zone is { } z ? layout with { Zone = z } : layout);
    }

    /// <summary>A blow of <paramref name="tool"/> straight at the tunnel's face at column <paramref name="face"/>, from two cells off.</summary>
    private static void Swing(Scenario w, Weapon tool, int face)
    {
        var at = Mid(face - 2, 3);
        w.Do(new Hew(tool.Item, at.X, at.Y, Right)).Run();
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void APickaxeSwingBreaksSoilAndCracksTheCellBehindByHalfItsSurplus()
    {
        var w = Bored("ssssbbbb");
        Swing(w, Pick, 12);
        Assert.Null(w.M.Grid.WallAt(At(w, 12, 3)));
        Assert.Equal([(At(w, 13, 3), Carry(Soil))], Cracks(w));
        Assert.Equal(24, Carry(Soil));
        // The axe carries nothing on.
        var axe = Bored("ssssbbbb");
        Swing(axe, Weapons.Axe, 12);
        Assert.Null(axe.M.Grid.WallAt(At(axe, 12, 3)));
        Assert.Empty(Cracks(axe));
    }

    /// <summary>Open air left of a block of soil, columns 6 to 11, rows 2 to 6.</summary>
    private static Scenario Block() => new(Layout.Parse("""
        ............
        ............
        ......######
        ......######
        ......######
        ......######
        ......######
        ............
        """).WithMaterials("""
        ............
        ............
        ......ssssss
        ......ssssss
        ......ssssss
        ......ssssss
        ......ssssss
        ............
        """).OnPlanet());

    [Fact]
    public void ACarryGoesAlongTheLineFromTheHeroThroughTheBrokenCellAslantToo()
    {
        var w = Block();
        w.Do(new Enter(At(w, 4, 2))).Run();
        // From the hero's cell: level; a slope under two in five, still level; and one in one, a diagonal.
        w.Do(new HewCells(Pick.Item, new List<int> { At(w, 7, 2), At(w, 8, 3), At(w, 6, 4) })).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal([(At(w, 8, 2), Carry(Soil)), (At(w, 9, 3), Carry(Soil)), (At(w, 7, 5), Carry(Soil))], Cracks(w));
    }

    [Fact]
    public void HewCellsWithNoHeroCarriesNothing()
    {
        var w = Block();
        Assert.Null(w.M.Crew.HeroOf(0));
        w.Do(new HewCells(Pick.Item, new List<int> { At(w, 7, 2) })).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.WallAt(At(w, 7, 2)));
        Assert.Empty(Cracks(w));
    }

    [Fact]
    public void APieceTwoCarriesReachTakesTheLargestOnce()
    {
        // Soil over stone, a door segment behind both; the stone cracked first, so both break to the next blow.
        var w = new Scenario(Layout.Parse("""
            ...........
            ...........
            ...........
            ........#DD
            ........#DD
            ...........
            """).WithMaterials("""
            ...........
            ...........
            ...........
            ........s..
            ........t..
            ...........
            """).OnPlanet());
        w.Do(new Enter(At(w, 4, 3))).Run();
        w.Do(new HewCells(Pick.Item, new List<int> { At(w, 8, 4) })).Run();
        w.Do(new HewCells(Pick.Item, new List<int> { At(w, 8, 3), At(w, 8, 4) })).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.WallAt(At(w, 8, 4)));
        int stone = Carry(Materials.Get("stone").Strength - Pick.BlockDamage);
        Assert.True(Carry(Soil) > stone && stone > 0);
        Assert.Equal([(At(w, 9, 3), Carry(Soil))], Cracks(w));
    }

    [Fact]
    public void ACarryIsNotCarriedAgain()
    {
        var w = Bored("ssssbbbb");
        // Cracked from its own middle, so nothing carries: then the carry breaks it, four over, and that goes no farther.
        w.Do(HewTesting.Dig(w, At(w, 13, 3), Weapons.Spear.Item)).Run();
        Assert.Equal([(At(w, 13, 3), Weapons.Spear.BlockDamage)], Cracks(w));
        Swing(w, Pick, 12);
        Assert.Null(w.M.Grid.WallAt(At(w, 13, 3)));
        Assert.NotNull(w.M.Grid.WallAt(At(w, 14, 3)));
        Assert.Empty(Cracks(w));
    }

    [Fact]
    public void ACarryStopsAtBedrockLavaAndTheFog()
    {
        foreach (var w in new[] { Bored("sbbbbbbb"), Bored("slbbbbbb"), Bored("ssssbbbb", new Box(0, 0, 12, 5)) })
        {
            Swing(w, Pick, 12);
            Assert.Null(w.M.Grid.WallAt(At(w, 12, 3)));
            Assert.NotNull(w.M.Grid.WallAt(At(w, 13, 3)));
            Assert.Empty(Cracks(w));
        }
    }

    [Fact]
    public void AboardTheShipNoCarryLeavesAVesselsBox()
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Find("shuttle")).Run();
        w.Do(new Jump(w.M.Places.Contacts[^1].Id)).Run();
        var box = w.M.Grid.Reserved!.Value;
        int width = w.M.Grid.Width;
        // The vessel's hull on its box's left edge, iron put just outside it; the hero inside, level with it.
        int hull = Enumerable.Range(box.Top, box.Bottom - box.Top + 1).Select(y => y * width + box.Left).First(c => w.M.Grid.WallAt(c) != null);
        w.Do(new Place(hull - 1, Placement.Wall("iron"))).Run();
        w.Do(new Enter(hull + 5)).Run();
        Assert.Empty(w.Refusals);
        int strength = w.M.Grid.WallAt(hull)!.Strength, blows = (strength + Pick.BlockDamage - 1) / Pick.BlockDamage;
        Assert.True(blows * Pick.BlockDamage - strength >= 2, "the last blow leaves something to carry");
        for (int i = 0; i < blows; i++) w.Do(new HewCells(Pick.Item, new List<int> { hull })).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.WallAt(hull));
        Assert.NotNull(w.M.Grid.WallAt(hull - 1));
        Assert.Empty(Cracks(w));
    }

    [Fact]
    public void DiggingStraightTakesFewerSwingsThanDiggingAcross()
    {
        var w = Bored("ttttttbb");
        int swings = 0;
        for (int face = 12; face < 18; face++)
            for (; w.M.Grid.WallAt(At(w, face, 3)) != null; swings++) Swing(w, Pick, face);
        // Across a face each cell is fresh stone, two blows (PickaxeTests); straight on, the carry fells every fourth at one: 2, 2, 2, 1, 2, 2.
        Assert.Equal(2, (Materials.Get("stone").Strength + Pick.BlockDamage - 1) / Pick.BlockDamage);
        Assert.Equal(11, swings);
    }
}
