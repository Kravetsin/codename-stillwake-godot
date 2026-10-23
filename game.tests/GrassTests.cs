using Stillwake.Game.Features.Grid;
using Stillwake.Sim.Grid;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Where moss grows on a planet (0079): on soil with the open sky straight above it; and every plant drawn.</summary>
public class GrassTests
{
    private static readonly int Soil = Spec.Materials.Get("soil").Code, Stone = Spec.Materials.Get("stone").Code;

    private static bool Grows(int material = -1, int above = Structure.Empty, int back = Back.None, int structure = Structure.Wall) =>
        Grass.Grows(structure, material < 0 ? Soil : material, above, back, Soil);

    [Fact]
    public void GrowsOnSoilUnderTheOpenSky() => Assert.True(Grows());

    [Fact]
    public void NotOnStoneOrAnythingButAWall()
    {
        Assert.False(Grows(material: Stone));
        Assert.False(Grows(structure: Structure.Floor));
        Assert.False(Grows(structure: Structure.Empty));
    }

    [Fact]
    public void NotUnderARoofABackWallOrAnythingBuiltAbove()
    {
        // In a cave or a dug shaft the cell above has its back wall; under a built ladder or wall it holds something.
        Assert.False(Grows(back: Back.Wall));
        Assert.False(Grows(above: Structure.Wall));
        Assert.False(Grows(above: Structure.Ladder));
    }

    private static bool Edged(int material, bool natural = true, int above = Structure.Empty, int back = Back.None) =>
        Grass.Edged(Structure.Wall, material, natural, above, back, Soil);

    [Fact]
    public void RockShowsItsChipsUnderAnyOpenCellSoilItsMossOnlyUnderTheSky()
    {
        // A cave's floor: an empty cell above, its back wall behind.
        Assert.True(Edged(Stone, back: Back.Wall));
        Assert.True(Edged(Soil));
        Assert.False(Edged(Soil, back: Back.Wall));
    }

    [Fact]
    public void NoEdgeOnHullOrUnderAnything()
    {
        Assert.False(Edged(Stone, natural: false));
        Assert.False(Edged(Stone, above: Structure.Ladder));
        Assert.False(Grass.Edged(Structure.Floor, Stone, true, Structure.Empty, Back.None, Soil));
    }

    [Fact]
    public void EveryKindOfPlantHasALook()
    {
        Assert.All(Spec.Flora.All, k => Assert.True(FloraLooks.Of.TryGetValue(k.Id, out var look) && look.Variants >= 1, k.Id));
    }

    [Fact]
    public void ACellAlwaysShowsTheSameVariantAndNeighboursShowSeveral()
    {
        Assert.Equal(Grass.VariantOf(1234, 4), Grass.VariantOf(1234, 4));
        Assert.True(Enumerable.Range(0, 16).Select(c => Grass.VariantOf(1000 + c, 4)).Distinct().Count() > 2);
        Assert.All(Enumerable.Range(0, 400), c => Assert.InRange(Grass.VariantOf(c, 4), 0, 3));
    }
}
