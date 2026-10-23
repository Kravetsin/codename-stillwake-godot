using Stillwake.Content;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The grid's layers as the client draws them: every code of every cell, kept current.</summary>
public class GridLayersTests
{
    [Fact]
    public void HandsOutEveryLayerAsTheMapDrawsIt()
    {
        var w = new Scenario("""
             #D-v
            .=Hdx
            """, cells: true);
        var g = w.M.Grid;
        int scrap = Materials.Get("scrap").Code;
        Assert.Equal(new[] { 0, 1, 4, 0, 0, 0, 2, 3, 5, 0 }, g.StructureLayer.ToArray());
        Assert.Equal(new[] { 0, scrap, 0, 0, 0, 0, 0, 0, 0, 0 }, g.MaterialLayer.ToArray());
        Assert.Equal(new[] { 0, 0, 0, Conduit.Pipe, Conduit.ValveOpen, 0, 0, 0, 0, Conduit.ValveClosed }, g.ConduitLayer.ToArray());
        Assert.Equal(new[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, g.BackLayer.ToArray());
        Assert.Equal(new[] { 0, scrap, scrap, scrap, scrap, scrap, scrap, scrap, scrap, scrap }, g.BackMaterialLayer.ToArray());
    }

    [Fact]
    public void KeepsMaterialsAndDoorsCurrentAsTheShipChanges()
    {
        var w = new Scenario("""
            ...
            .D.
            """);
        int iron = Materials.Get("iron").Code;
        w.Do(new Place(w.At(0, 0), Placement.Wall("iron")))
            .Do(new ClearAt(w.At(1, 0), "back"))
            .Do(new ClearAt(w.At(2, 0), "back"))
            .Do(new Place(w.At(2, 0), Placement.BackWall("iron")))
            .Do(new Place(w.At(2, 1), new Placement("conduit", "pipe")))
            .Do(new SetDoor(w.At(1, 1), true))
            .Run();
        var g = w.M.Grid;
        Assert.Equal(iron, g.MaterialLayer[w.At(0, 0)]);
        Assert.Equal(Structure.DoorOpen, g.StructureLayer[w.At(1, 1)]);
        Assert.Equal(Back.None, g.BackLayer[w.At(1, 0)]);
        Assert.Equal(0, g.BackMaterialLayer[w.At(1, 0)]);
        Assert.Equal(Back.Wall, g.BackLayer[w.At(2, 0)]);
        Assert.Equal(iron, g.BackMaterialLayer[w.At(2, 0)]);
        Assert.Equal(Conduit.Pipe, g.ConduitLayer[w.At(2, 1)]);
        w.Do(new ClearAt(w.At(0, 0), "structure")).Run();
        Assert.Equal(0, g.MaterialLayer[w.At(0, 0)]);
    }
}
