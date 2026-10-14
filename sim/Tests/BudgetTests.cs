using System.Diagnostics;
using Stillwake.Sim.Grid;
using Xunit;
using Xunit.Abstractions;

namespace Stillwake.Sim.Tests;

/// <summary>
/// A sim tick on the reference world (CLAUDE.md's budgets: 256×128, every layer in use), timed with a
/// Stopwatch here, never in the sim. The bound asserted is loose, for slow CI machines; the output
/// says how far under 2 ms a tick stays.
/// </summary>
public class BudgetTests(ITestOutputHelper output)
{
    /// <summary>
    /// apps/game/src/dev/perf-map.ts: an 8×8 tile repeated, every chunk holding every kind of cell; here
    /// each tile's tank also starts with a canister and is piped to its vent, so every room fills and leaks.
    /// </summary>
    internal static Layout Reference()
    {
        const int width = 256, height = 128;
        string[] tile =
        [
            "########",
            "#TT.V.-#",
            "#TT.H.v#",
            "#=.DH.-#",
            "#--x-H.#",
            "#.V.dH.#",
            "#==..=.#",
            "########",
        ];
        string[] under = ["", " o --", " ---", "", "", "", "", ""];
        var rows = Enumerable.Range(0, height).Select(y => string.Concat(Enumerable.Repeat(tile[y % 8], width / 8))).ToArray();
        var below = Enumerable.Range(0, height).Select(y => string.Concat(Enumerable.Repeat(under[y % 8].PadRight(8), width / 8))).ToArray();
        return Layout.Parse(rows, below);
    }

    private (double Mean, double Max) Time(World world, int ticks, Func<int, IReadOnlyList<Command>> commands)
    {
        var watch = new Stopwatch();
        double max = 0;
        for (int i = 0; i < ticks; i++)
        {
            var these = commands(i);
            long start = Stopwatch.GetTimestamp();
            watch.Start();
            world.Step(these);
            watch.Stop();
            max = Math.Max(max, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        return (watch.Elapsed.TotalMilliseconds / ticks, max);
    }

    [Fact]
    public void KeepsASimTickOnTheReferenceWorldUnderBudget()
    {
        var m = new Registry();
        var world = World.Create(1, Reference(), m.All);
        Assert.True(m.Rooms.All.Count > 500);
        // Filling first, so the timed ticks fill, leak and survey hundreds of rooms.
        Time(world, 100, _ => []);
        var still = Time(world, 300, _ => []);
        Assert.True(m.Atmosphere.AiredCells.Count > 5000);
        // A door toggled every tick: rooms found again, orders and leaks with them.
        var door = Enumerable.Range(0, 128 * 256).First(c => m.Grid.StructureAt(c) == Structure.DoorClosed);
        var toggling = Time(world, 300, _ => [new ToggleDoor(door)]);
        // The same without atmosphere, for its share of the tick.
        var bare = new Registry();
        var without = World.Create(1, Reference(), bare.All.Where(x => x != bare.Atmosphere).ToList());
        Time(without, 100, _ => []);
        var others = Time(without, 300, _ => []);
        var othersToggling = Time(without, 300, _ => [new ToggleDoor(door)]);
        output.WriteLine($"rooms {m.Rooms.All.Count}, aired cells {m.Atmosphere.AiredCells.Count}");
        output.WriteLine($"without atmosphere: {others.Mean:F3} ms mean, {othersToggling.Mean:F3} ms toggling a door");
        output.WriteLine($"tick: {still.Mean:F3} ms mean, {still.Max:F3} ms max");
        output.WriteLine($"tick toggling a door: {toggling.Mean:F3} ms mean, {toggling.Max:F3} ms max");
        Assert.True(still.Mean < 20 && toggling.Mean < 40, "a tick is far over its budget");
    }
}
