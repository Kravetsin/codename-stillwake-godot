using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The pickaxe (the-pickaxe): the drill's fallback underground, swung, needing no charge. Half the drill on stone and ore, two and a half
/// times the axe, its fan the drill's a cell longer.
/// </summary>
public class PickaxeTests
{
    private static readonly Weapon Pick = Weapons.Pickaxe, Drill = Weapons.Drill, Axe = Weapons.Axe;
    private const int Right = 0;

    private static Scenario World(string map, string materials) => new(Layout.Parse(map).WithMaterials(materials).OnPlanet());

    /// <summary>A world with <paramref name=tool/> in hero 0's bag and a full battery on its belt, the drill running.</summary>
    private static Scenario Holding(Weapon tool, string materials)
    {
        var w = World(Face, materials).GiveItems(0, (tool.Item, 1));
        w.Put(Content.Inventory.Slots + 2, new Stack("battery", 1, Charge.Battery));
        if (Content.Items.Get(tool.Item).Draw != null) w.Do(new Run(tool.Item, true)).Run();
        return w;
    }

    private static (int X, int Y) Mid(int x, int y) => (x * 1000 + 500, y * 1000 + 500);

    private static int At(Scenario w, int x, int y) => y * w.M.Grid.Width + x;

    // A face of three cells, column 12; the hand 8.5 cells before it, past the drill's 8 and inside the pickaxe's 9.
    private const string Face = """
        ................
        ................
        ............####
        ............####
        ............####
        ................
        """;

    private static string Of(char material) => Face.Replace('#', material);

    /// <summary>How many hits of <paramref name="tool"/> straight at the face's middle cell break it, that cell alone struck each time.</summary>
    private static int HitsToBreak(char material, Weapon tool)
    {
        var w = Holding(tool, Of(material));
        int cell = At(w, 12, 3);
        for (int hits = 1; hits <= 100; hits++)
        {
            w.Do(HewTesting.Dig(w, cell, tool.Item)).Run();
            Assert.Empty(w.Refusals);
            if (w.M.Grid.WallAt(cell) == null) return hits;
        }
        throw new InvalidOperationException($"{tool.Item} never broke {material}");
    }

    [Theory]
    [InlineData('t')]
    [InlineData('c')]
    [InlineData('i')]
    public void StoneAndOreBreakAtThePickaxesSecondBlowInHalfTheDrillsTimeAndTwoFifthsTheAxes(char material)
    {
        var (pick, drill, axe) = (HitsToBreak(material, Pick), HitsToBreak(material, Drill), HitsToBreak(material, Axe));
        // Never at the first blow: a wall still cracks before it breaks.
        Assert.Equal((2, 3, 3), (pick, drill, axe));
        // In ticks, each at its pace: 12 against the drill's 6 and the axe's 30.
        Assert.Equal((12, 6, 30), (pick * Pick.Pace, drill * Drill.Pace, axe * Axe.Pace));
        Assert.Equal(2 * drill * Drill.Pace, pick * Pick.Pace);
        Assert.Equal(5 * pick * Pick.Pace, 2 * axe * Axe.Pace);
    }

    [Fact]
    public void APickaxeBlowCracksStoneByItsBlockDamageAndTheDrillAndAxeByTheirs()
    {
        foreach (var tool in new[] { Pick, Drill, Axe })
        {
            var w = Holding(tool, Of('t'));
            int cell = At(w, 12, 3);
            w.Do(HewTesting.Dig(w, cell, tool.Item)).Run();
            Assert.Equal([(cell, tool.BlockDamage)], w.M.Building.Cracks.Select(c => (c.Cell, c.Damage)));
        }
    }

    [Fact]
    public void ThePickaxesFanReachesAFaceACellBeyondTheDrillsAndStrikesAsWideAFaceAsItsCloser()
    {
        var w = World(Face, Of('t'));
        var far = Mid(3, 3);
        int[] face = [At(w, 12, 2), At(w, 12, 3), At(w, 12, 4)];
        Assert.Empty(w.M.Building.Struck(Drill.Item, far.X, far.Y, Right).Cells);
        Assert.Empty(w.M.Building.Struck(Axe.Item, far.X, far.Y, Right).Cells);
        Assert.Contains(At(w, 12, 3), w.M.Building.Struck(Pick.Item, far.X, far.Y, Right).Cells);
        // Five cells off, both fans the same eighth of a turn: the face's three cells, none behind them.
        var near = Mid(7, 3);
        Assert.Equal(face, w.M.Building.Struck(Drill.Item, near.X, near.Y, Right).Cells);
        Assert.Equal(face, w.M.Building.Struck(Pick.Item, near.X, near.Y, Right).Cells);
        Assert.True(Pick.Range > Drill.Range && Pick.Range - Drill.Range <= 1000);
    }

    [Fact]
    public void ThePickaxeIsSwungWithNoChargeAndMadeAtTheWorkbenchOfTheFirstWrecksParts()
    {
        Assert.Null(Content.Items.Get(Pick.Item).Draw);
        Assert.Contains(Stations.Get("workbench").Rows, r => r.Contains(Pick.Item));
        Assert.Equal([("plate", 2), ("tube", 1)], Crafts.Get(Pick.Item).Cost);
        // On blocks a second: half the drill's 40, two and a half times the axe's 8; on wrecks under the axe's 4.
        Assert.Equal((20, 40, 8), (Pick.BlockDamage * Sim.World.TicksPerSecond / Pick.Pace, Drill.BlockDamage * Sim.World.TicksPerSecond / Drill.Pace, Axe.BlockDamage * Sim.World.TicksPerSecond / Axe.Pace));
        Assert.True(Pick.Damage * Sim.World.TicksPerSecond / Pick.Pace < Axe.Damage * Sim.World.TicksPerSecond / Axe.Pace);
    }
}
