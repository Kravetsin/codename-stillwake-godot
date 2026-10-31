using Stillwake.Sim.Grid;
using Xunit;
using Structure = Stillwake.Content.Structure;

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
        w.Do(new SetDoor(w.At(2, 2), true)).Run();
        var refused = Assert.Single(w.Refusals);
        Assert.Equal("grid.noDoor", refused.Reason);
        Assert.NotEqual(before, w.World.Hash()); // the tick moved on
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(w.At(2, 2)));
    }

    [Fact]
    public void HashesTheSavesBytesWithFnv1a()
    {
        // Hashed as it is written, never held: the same hash as the save's bytes, on a world big enough to need many batches.
        var w = new Scenario(Registry.Wreck);
        w.Run(3);
        ulong h = 14695981039346656037;
        foreach (byte b in System.Text.Encoding.UTF8.GetBytes(w.World.Save()))
            h = unchecked((h ^ b) * 1099511628211);
        Assert.Equal(h, w.World.Hash());
    }

    [Fact]
    public void SavesLoadsAndPlaysOnAsIfNeverSaved()
    {
        var a = new Scenario(Map);
        var b = new Scenario(Map);
        a.Do(new SetDoor(a.At(4, 2), true)).Run(3);
        b.Do(new SetDoor(b.At(4, 2), true)).Run(3);
        b.Reload();
        Assert.Equal(a.World.Save(), b.World.Save());
        a.Do(new Place(a.At(2, 3), Placement.Wall())).Run(5);
        b.Do(new Place(b.At(2, 3), Placement.Wall())).Run(5);
        Assert.Equal(a.World.Hash(), b.World.Hash());
        Assert.Equal(a.M.Rooms.All.Count, b.M.Rooms.All.Count);
        // A machine's footprint is derived: rebuilt after the load, so a wall still cannot go there.
        b.Do(new Place(b.At(6, 3), Placement.Wall())).Run();
        Assert.Equal("grid.machineInTheWay", Assert.Single(b.Refusals).Reason);
    }

    [Fact]
    public void AWorldKeepsItsIdThroughASaveAndHashesAlike()
    {
        var w = new Scenario(Map);
        var world = World.Create(1, Layout.Parse(["#.#"]), w.Modules(), 7);
        Assert.Equal(7u, world.Id);
        var loaded = World.Load(world.Save(), w.Modules());
        Assert.Equal(7u, loaded.Id);
        Assert.Equal(world.Hash(), loaded.Hash());
        Assert.NotEqual(world.Hash(), World.Create(1, Layout.Parse(["#.#"]), w.Modules()).Hash());
    }

    [Fact]
    public void AWorldWithNoIdSavesWithoutOne()
    {
        var w = new Scenario(Map);
        using var save = System.Text.Json.JsonDocument.Parse(w.World.Save());
        Assert.False(save.RootElement.TryGetProperty("id", out _));
        Assert.Equal(0u, World.Load(w.World.Save(), w.Modules()).Id);
    }

    [Fact]
    public void RefusesASaveOfAnotherSetOfModules()
    {
        var w = new Scenario(Map);
        var save = w.World.Save().Replace("\"builder\"", "\"other\"");
        Assert.Throws<InvalidDataException>(() => World.Load(save, w.Modules()));
    }
}
