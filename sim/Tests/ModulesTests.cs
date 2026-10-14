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

    [Fact]
    public void DrawsTheCommandsOfEveryModuleThatHasAny()
    {
        foreach (var m in new Registry().All.Where(m => m.HasCommands))
            Assert.True(m.Fuzzes, $"{m.Id} has commands and no fuzz generator");
    }
}
