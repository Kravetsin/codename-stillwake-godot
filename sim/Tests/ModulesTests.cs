using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The generic suites (packages/sim/src/modules/modules.test.ts): they hold for every registered
/// module without a test of its own.
/// </summary>
public class ModulesTests
{
    /// <summary>Up to three random commands drawn from the modules' own fuzz generators.</summary>
    internal static List<Command> RandomCommands(Registry modules, Rng rng)
    {
        var fuzzable = modules.All.Where(m => m.Fuzzes).ToList();
        var commands = new List<Command>();
        for (uint n = fuzzable.Count > 0 ? rng.Below(4) : 0; n > 0; n--)
        {
            var m = fuzzable[(int)rng.Below((uint)fuzzable.Count)];
            // Senders with gaps, so what a module keeps per sender is made out of order too.
            var c = m.Fuzz(rng, (int)rng.Below(5));
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
        for (int i = 0; i < 100; i++) world.Step(RandomCommands(played, rng));

        var loaded = World.Load(world.Save(), new Registry().All);
        Assert.Equal(world.Hash(), loaded.Hash());
        for (int i = 0; i < 100; i++)
        {
            var commands = RandomCommands(played, rng);
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
            // Encoded and decoded, as the network carries them.
            var commands = RandomCommands(a, rng);
            first.Step(commands);
            second.Step(commands.Select(c => second.Decode(first.Encode(c))).ToList());
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
        if (structure is Structure.DoorClosed or Structure.DoorOpen && rng.Below(2) == 0) return new ToggleDoor(cell);
        return new ClearAt(cell, "structure");
    }

    /// <summary>
    /// What modules keep between ticks — rooms found again around a change, the air's survey, leaks and
    /// orders — is what a load finds from scratch: a world loaded from the live one's save before every
    /// tick, fed the same commands, hashes the same after it. Closed rooms three cells from space, so
    /// changes are followed around them; with the air read every tick, as an open panel does, and without.
    /// </summary>
    [Theory]
    [InlineData(5u, false)]
    [InlineData(5u, true)]
    [InlineData(8u, true)]
    public void KeepsWhatItFollowsBetweenTicksAsAWorldLoadedEveryTickFindsIt(uint seed, bool readAir)
    {
        var layout = BudgetTests.Reference(38, 30, shipWidth: 32, shipHeight: 24, margin: 3);
        var rng = new Rng(new RngState { S = seed });
        var live = new Registry();
        var world = World.Create(seed, layout, [.. live.All, new Builder(live.Grid, live.Grid)]);
        int around = 0;
        for (int i = 0; i < 300; i++)
        {
            if (readAir && live.Rooms.All.Count > 0)
            {
                live.Atmosphere.RoomsAir();
                live.Atmosphere.StatusOf(live.Rooms.All[0].Id);
            }
            var loaded = new Registry();
            var copy = World.Load(world.Save(), [.. loaded.All, new Builder(loaded.Grid, loaded.Grid)]);
            var commands = RandomCommands(live, rng);
            if (rng.Below(2) == 0) commands.Add(ShipEdit(live.Grid, rng, 3, 3, 32, 24));
            int version = live.Rooms.Version;
            world.Step(commands);
            copy.Step(commands);
            if (live.Rooms.ChangedSince(version) is { Count: > 0 }) around++;
            Assert.True(world.Hash() == copy.Hash(), $"tick {i}: the world differs from one loaded before it");
        }
        Assert.True(around > 30, $"rooms found around a change {around} times");
    }

    [Fact]
    public void DrawsTheCommandsOfEveryModuleThatHasAny()
    {
        foreach (var m in new Registry().All.Where(m => m.HasCommands))
            Assert.True(m.Fuzzes, $"{m.Id} has commands and no fuzz generator");
    }
}
