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
    internal static Layout Reference() => Reference(256, 128);

    /// <summary>
    /// The reference tile over <paramref name="shipWidth"/>×<paramref name="shipHeight"/>, <paramref name="margin"/>
    /// cells of space in from the top-left of a <paramref name="width"/>×<paramref name="height"/> world.
    /// </summary>
    internal static Layout Reference(int width, int height, int shipWidth = 256, int shipHeight = 128, int margin = 0)
    {
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
        string Row(int y, string[] of) =>
            (y >= margin && y < margin + shipHeight
                ? new string(' ', margin) + string.Concat(Enumerable.Repeat(of[(y - margin) % 8].PadRight(8), shipWidth / 8))
                : "").PadRight(width);
        var rows = Enumerable.Range(0, height).Select(y => Row(y, tile)).ToArray();
        var below = Enumerable.Range(0, height).Select(y => Row(y, under)).ToArray();
        return Layout.Parse(rows, below);
    }

    [Fact]
    public void SavesASmallShipInALargeWorldNoBiggerThanTheShipAlone()
    {
        // Bytes rather than time, so it never flakes: what a save holds is what a hash, a welcome and a load cost.
        string ship = World.Create(1, Reference(), new Registry().All).Save();
        string large = World.Create(1, Reference(1024, 512), new Registry().All).Save();
        output.WriteLine($"the ship alone {ship.Length / 1024} KB, in a 1024×512 world {large.Length / 1024} KB");
        Assert.True(large.Length < ship.Length * 3 / 2, $"{large.Length} bytes for the large world, {ship.Length} for the ship");
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

    /// <summary>The reference ship alone, and in the corner of a world of space: rooms are found again only around a door, so the space costs nothing.</summary>
    [Theory]
    [InlineData(256, 128)]
    [InlineData(1024, 512)]
    public void KeepsASimTickWithTheReferenceShipUnderBudget(int width, int height)
    {
        var m = new Registry();
        var world = World.Create(1, Reference(width, height), m.All);
        Assert.True(m.Rooms.All.Count > 500);
        // Filling first, so the timed ticks fill, leak and survey hundreds of rooms.
        Time(world, 100, _ => []);
        var still = Time(world, 300, _ => []);
        Assert.True(m.Atmosphere.AiredCells.Count > 5000);
        // A door toggled every tick: rooms found again, orders and leaks with them.
        var door = Enumerable.Range(0, width * height).First(c => m.Grid.StructureAt(c) == Structure.DoorClosed);
        var toggling = Time(world, 300, _ => [new ToggleDoor(door)]);
        // The same without atmosphere, for its share of the tick.
        var bare = new Registry();
        var without = World.Create(1, Reference(width, height), bare.All.Where(x => x != bare.Atmosphere).ToList());
        Time(without, 100, _ => []);
        var others = Time(without, 300, _ => []);
        var othersToggling = Time(without, 300, _ => [new ToggleDoor(door)]);
        output.WriteLine($"{width}×{height}: rooms {m.Rooms.All.Count}, aired cells {m.Atmosphere.AiredCells.Count}");
        output.WriteLine($"without atmosphere: {others.Mean:F3} ms mean, {othersToggling.Mean:F3} ms toggling a door");
        output.WriteLine($"tick: {still.Mean:F3} ms mean, {still.Max:F3} ms max");
        output.WriteLine($"tick toggling a door: {toggling.Mean:F3} ms mean, {toggling.Max:F3} ms max");
        Assert.True(still.Mean < 20 && toggling.Mean < 20, "a tick is far over its budget");
    }
}
