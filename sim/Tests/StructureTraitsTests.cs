using Stillwake.Content;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The structure table (0112): what each code is, and the grid's trait queries reading it.</summary>
public class StructureTraitsTests
{
    [Fact]
    public void TheTableIsIndexedByItsCodes()
    {
        for (int code = 0; code < Structures.All.Count; code++) Assert.Equal(code, Structures.All[code].Code);
    }

    [Fact]
    public void ADoorwayTogglesWithinItsGroupBetweenShutAndOpen()
    {
        foreach (var s in Structures.All.Where(s => s.Toggle != null))
        {
            var other = Structures.Of(s.Toggle!.Value);
            Assert.Equal(s.Code, other.Toggle);
            Assert.Equal(s.Group, other.Group);
            Assert.NotEqual(s.Has(Trait.Opening), other.Has(Trait.Opening));
            Assert.NotEqual(StructureGroup.None, s.Group);
        }
        Assert.All(Structures.All.Where(s => s.Group != StructureGroup.None), s => Assert.NotNull(s.Toggle));
    }

    [Fact]
    public void EveryStructureIsBuiltByAKnownBuildAndDrawnInTheLegend()
    {
        foreach (var s in Structures.All.Where(s => s.Build != null))
            Assert.True(s.Build == "wall" || Builds.TryGet(s.Build!, out _), $"{s.Build} is no build");
        foreach (var s in Structures.All.Where(s => s.Char != null))
            Assert.Contains(s.Char!.Value, Legend.MapChars);
        Assert.Equal(Structure.DoorClosed, Structures.Placed("door"));
        Assert.Equal(Structure.HatchClosed, Structures.Placed(Builds.Hatch));
        Assert.Null(Structures.Placed("crate"));
    }

    /// <summary>The user's rules (0111, 0112): a hatch differs from a door only in being climbed when open.</summary>
    [Theory]
    [InlineData(Structure.Wall, Trait.Blocks | Trait.Seals | Trait.BlocksLight | Trait.Terrain)]
    [InlineData(Structure.Floor, Trait.Supports)]
    [InlineData(Structure.Ladder, Trait.Climbable)]
    [InlineData(Structure.DoorClosed, Trait.Blocks | Trait.Seals | Trait.BlocksLight)]
    [InlineData(Structure.DoorOpen, Trait.Opening)]
    [InlineData(Structure.HatchClosed, Trait.Supports | Trait.BlocksFromBelow | Trait.Seals | Trait.BlocksLight)]
    [InlineData(Structure.HatchOpen, Trait.Opening | Trait.Climbable)]
    [InlineData(Structure.Platform, Trait.Supports | Trait.BlocksFromBelow | Trait.Seals | Trait.BlocksLight)]
    public void EachKindHasItsTraits(int code, Trait traits) => Assert.Equal(traits, Structures.Of(code).Traits);

    [Fact]
    public void TheGridAnswersByTraitAndPastItsEdgeEverythingBlocks()
    {
        var w = new Scenario("#=HDdho.", cells: true);
        var g = w.M.Grid;
        Assert.Equal([true, false, false, true, false, false, false, false], Enumerable.Range(0, 8).Select(g.Blocks));
        Assert.Equal([false, true, false, false, false, true, false, false], Enumerable.Range(0, 8).Select(g.Supports));
        Assert.Equal([false, false, true, false, false, false, true, false], Enumerable.Range(0, 8).Select(g.Climbable));
        Assert.Equal([true, false, false, true, false, true, false, false], Enumerable.Range(0, 8).Select(g.Seals));
        Assert.Equal([true, false, false, true, false, true, false, false], Enumerable.Range(0, 8).Select(g.BlocksLight));
        Assert.Equal([true, true, false, true, false, true, false, false], Enumerable.Range(0, 8).Select(g.Solid));
        foreach (int outside in (int[])[-1, 8])
        {
            Assert.True(g.Blocks(outside));
            Assert.True(g.Solid(outside));
            Assert.False(g.Supports(outside) || g.Climbable(outside) || g.Seals(outside) || g.BlocksLight(outside));
        }
    }

    /// <summary>Materials say what they do too (0112): the rules that once named lava or soil read these; machines, <see cref="MachineTraitsTests"/>.</summary>
    [Fact]
    public void MaterialsAndMachinesCarryTheirTraits()
    {
        Assert.Equal([Lava.Material], Materials.All.Where(m => m.Hurts).Select(m => m.Id));
        Assert.Equal(["soil"], Materials.All.Where(m => m.Grows).Select(m => m.Id));
        Assert.All(Materials.All, m => Assert.Equal(m.Natural, m.Footing != Tread.Deck));
        Assert.Equal(["iron"], Materials.All.Where(m => m.Look == MaterialLook.Panels).Select(m => m.Id));
        Assert.Single(Machines.All, m => m.Fixed);
    }
}
