using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

public class CoreTests
{
    private const string Map = """
        ..........
        .########.
        .#..D.TT#.
        .#....TT#.
        .########.
        """;

    [Fact]
    public void DrawsTheSameRandomNumbersAsTheTypeScriptSim()
    {
        // createRng(42) in packages/sim/src/core/rng.ts draws these.
        var rng = new Rng(new RngState { S = 42 });
        var draws = new ulong[] { 1000, 1000, 7, 4294967296, 3 }.Select(n => rng.Below(n));
        Assert.Equal(new uint[] { 956, 290, 0, 2876485805, 0 }, draws.ToArray());
    }

    [Fact]
    public void RefusesACommandWithItsReasonAndLeavesTheWorldAsItWas()
    {
        var w = new Scenario(Map);
        var before = w.World.Hash();
        w.Do(new ToggleDoor(w.At(2, 2))).Run();
        var refused = Assert.Single(w.Refusals);
        Assert.Equal("grid.noDoor", refused.Reason);
        Assert.NotEqual(before, w.World.Hash()); // the tick moved on
        Assert.Equal(Structure.Empty, w.Grid.StructureAt(w.At(2, 2)));
    }

    [Fact]
    public void SavesLoadsAndPlaysOnAsIfNeverSaved()
    {
        var a = new Scenario(Map);
        var b = new Scenario(Map);
        a.Do(new ToggleDoor(a.At(4, 2))).Run(3);
        b.Do(new ToggleDoor(b.At(4, 2))).Run(3);
        b.Reload();
        Assert.Equal(a.World.Save(), b.World.Save());
        a.Do(new Place(a.At(2, 3), Placement.Wall())).Run(5);
        b.Do(new Place(b.At(2, 3), Placement.Wall())).Run(5);
        Assert.Equal(a.World.Hash(), b.World.Hash());
        Assert.Equal(a.Rooms.All.Count, b.Rooms.All.Count);
        // A machine's footprint is derived: rebuilt after the load, so a wall still cannot go there.
        b.Do(new Place(b.At(6, 3), Placement.Wall())).Run();
        Assert.Equal("grid.machineInTheWay", Assert.Single(b.Refusals).Reason);
    }

    [Fact]
    public void RefusesASaveOfAnotherSetOfModules()
    {
        var w = new Scenario(Map);
        var save = w.World.Save().Replace("\"builder\"", "\"other\"");
        Assert.Throws<InvalidDataException>(() => World.Load(save, w.Modules()));
    }

    [Fact]
    public void CarriesCommandsOverTheWireByTheirNames()
    {
        var w = new Scenario(Map);
        var text = w.World.Encode(new ToggleDoor(12) { From = 2 });
        Assert.Equal("""{"type":"grid.toggleDoor","cell":12,"from":2}""", text);
        Assert.Equal(new ToggleDoor(12) { From = 2 }, w.World.Decode(text));
    }
}
