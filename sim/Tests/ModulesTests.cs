using Stillwake.Sim.Grid;
using Xunit;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The generic suites (packages/sim/src/modules/modules.test.ts): they hold for every registered
/// module without a test of its own.
/// </summary>
public class ModulesTests
{
    /// <summary>
    /// Up to three random commands drawn from the fuzz generators of the modules of each sender's surface, and now and
    /// then the world's own (0076): surfaces opened, heroes moved between them.
    /// </summary>
    internal static List<Command> RandomCommands(World world, Rng rng)
    {
        var commands = new List<Command>();
        for (uint n = rng.Below(4); n > 0; n--)
        {
            // Senders with gaps, so what a module keeps per sender is made out of order too.
            int from = (int)rng.Below(5);
            var fuzzable = world.ModulesOn(world.SurfaceOf(from)).Where(m => m.Fuzzes).ToList();
            var c = rng.Below(40) == 0 || fuzzable.Count == 0 ? world.Fuzz(rng, from) : fuzzable[(int)rng.Below((uint)fuzzable.Count)].Fuzz(rng, from);
            if (c != null) commands.Add(c);
        }
        return commands;
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(7u)]
    [InlineData(2026u)]
    public void SavesLoadsAndPlaysOnTheSame(uint seed)
    {
        var rng = new Rng(new RngState { S = seed });
        var played = new Registry();
        var world = World.Create(seed, Registry.Wreck, played.All);
        for (int i = 0; i < 100; i++) world.Step(RandomCommands(world, rng));

        var loaded = World.Load(world.Save(), new Registry().All);
        Assert.Equal(world.Hash(), loaded.Hash());
        for (int i = 0; i < 100; i++)
        {
            var commands = RandomCommands(world, rng);
            world.Step(commands);
            loaded.Step(commands);
        }
        Assert.Equal(world.Hash(), loaded.Hash());
    }

    [Theory]
    [InlineData(3u)]
    [InlineData(42u)]
    public void KeepsTwoWorldsFedTheSameCommandsAlike(uint seed)
    {
        var rng = new Rng(new RngState { S = seed });
        var a = new Registry();
        var b = new Registry();
        var first = World.Create(seed, Registry.Wreck, a.All);
        var second = World.Create(seed, Registry.Wreck, b.All);
        for (int i = 0; i < 300; i++)
        {
            var commands = RandomCommands(first, rng);
            first.Step(commands);
            second.Step(commands);
            Assert.Equal(first.Hash(), second.Hash());
        }
    }

    /// <summary>A wall, door or back wall built, toggled or taken apart somewhere in the ship's area, hull included.</summary>
    private static Command ShipEdit(GridModule grid, Rng rng, int x0, int y0, int width, int height)
    {
        int cell = (y0 + (int)rng.Below((uint)height)) * grid.Width + x0 + (int)rng.Below((uint)width);
        if (rng.Below(4) == 0)
            return grid.BackLayer[cell] == Back.Wall ? new ClearAt(cell, "back") : new Place(cell, Placement.BackWall());
        int structure = grid.StructureAt(cell);
        if (structure == Structure.Empty) return new Place(cell, rng.Below(2) == 0 ? Placement.Wall() : new Placement("structure", "door"));
        if (structure is Structure.DoorClosed or Structure.DoorOpen && rng.Below(2) == 0) return new SetDoor(cell, structure == Structure.DoorClosed);
        return new ClearAt(cell, "structure");
    }

    /// <summary>A cell in the box given emptied if anything is built there, otherwise walled or backed.</summary>
    private static Command[] SpaceEdit(GridModule grid, Rng rng, int x0, int y0, int width, int height)
    {
        int cell = (y0 + (int)rng.Below((uint)height)) * grid.Width + x0 + (int)rng.Below((uint)width);
        if (grid.StructureAt(cell) != Structure.Empty || grid.BackLayer[cell] != Back.None)
            return [new ClearAt(cell, "structure"), new ClearAt(cell, "back")];
        return [new Place(cell, rng.Below(2) == 0 ? Placement.Wall() : Placement.BackWall())];
    }

    /// <summary>
    /// What modules keep between ticks — rooms found again around a change, the air's survey, leaks and
    /// orders — is what a load finds from scratch: a world loaded from the live one's save before every
    /// tick, fed the same commands, hashes the same after it. Closed rooms six cells from space, the drawing doubled (0104), so
    /// changes are followed around them; builds out in space above, so breaches flood a ship's box that
    /// grows and shrinks; with the air read every tick, as an open panel does, and without; and a few ticks
    /// that write more than the grid's log holds, so every reader finds what it follows again from scratch.
    /// </summary>
    [Theory]
    [InlineData(5u, false)]
    [InlineData(5u, true)]
    [InlineData(9u, true)]
    public void KeepsWhatItFollowsBetweenTicksAsAWorldLoadedEveryTickFindsIt(uint seed, bool readAir)
    {
        var layout = BudgetTests.Reference(38, 30, shipWidth: 32, shipHeight: 24, margin: 3).Doubled();
        var rng = new Rng(new RngState { S = seed });
        var live = new Registry();
        var world = World.Create(seed, layout, [.. live.All, new Builder(live)]);
        int around = 0, grown = 0, shrunk = 0, behind = 0;
        for (int i = 0; i < 400; i++)
        {
            if (readAir && live.Rooms.All.Count > 0)
            {
                live.Atmosphere.RoomsAir();
                live.Atmosphere.StatusOf(live.Rooms.All[0].Id);
            }
            var loaded = new Registry();
            var copy = World.Load(world.Save(), [.. loaded.All, new Builder(loaded)]);
            var commands = RandomCommands(world, rng);
            if (rng.Below(2) == 0) commands.Add(ShipEdit(live.Grid, rng, 6, 6, 64, 48));
            // Out in the space above the ship, so what is built grows and shrinks.
            if (rng.Below(3) == 0) commands.AddRange(SpaceEdit(live.Grid, rng, 38, 0, 2, 6));
            // Now and then a door worked more times in a tick than the grid's log holds: every reader falls behind it.
            int door = live.Grid.StructureLayer.IndexOfAny(Structure.DoorClosed, Structure.DoorOpen);
            if (i % 100 == 60 && door >= 0)
            {
                commands.AddRange(Enumerable.Range(0, ChangeLog.DefaultCapacity + 2).Select(k => new SetDoor(door, k % 2 == 0)));
                // With a vent or a tank taken apart, which only the log would tell the air of.
                var gone = live.Grid.MachinesOf(i % 200 == 60 ? "vent" : "tank");
                if (gone.Count > 0) commands.Add(new ClearAt(gone[(int)rng.Below((uint)gone.Count)].Cell, "machine"));
            }
            int version = live.Rooms.Version;
            var built = live.Grid.Built;
            int gridWas = live.Grid.Version;
            world.Step(commands);
            copy.Step(commands);
            if (live.Rooms.ChangedSince(version) is { Count: > 0 }) around++;
            if (live.Grid.Built.Height > built.Height) grown++;
            if (live.Grid.Built.Height < built.Height) shrunk++;
            if (live.Grid.ChangedSince(gridWas) == null) behind++;
            Assert.True(world.Hash() == copy.Hash(), $"tick {i}: the world differs from one loaded before it");
        }
        Assert.True(around > 30, $"rooms found around a change {around} times");
        Assert.True(grown > 3 && shrunk > 3, $"the built box grew {grown} times and shrank {shrunk}");
        Assert.True(behind >= 2, $"the log was outrun {behind} times");
    }

    [Fact]
    public void DrawsTheCommandsOfEveryModuleThatHasAny()
    {
        foreach (var m in new Registry().All.Where(m => m.HasCommands))
            Assert.True(m.Fuzzes, $"{m.Id} has commands and no fuzz generator");
    }
}
