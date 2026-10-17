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

    /// <summary>
    /// The reference ship in a world of space, a hull wall beside space taken apart and built again every
    /// tick: rooms are flooded again within the ship's box, so the world around it costs nothing.
    /// </summary>
    [Theory]
    [InlineData(1024, 512)]
    [InlineData(2048, 1024)]
    public void KeepsABreachBesideSpaceAsCheapInALargeWorldAsTheShipIs(int width, int height)
    {
        const int margin = 8;
        var w = new Scenario(Reference(width, height, margin: margin));
        int wall = w.At(margin + 9, margin);
        Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(wall));
        Time(w.World, 100, _ => []);
        IReadOnlyList<Command> Breach(int i) => [i % 2 == 0 ? new ClearAt(wall, "structure") : new Place(wall, Placement.Wall())];
        Time(w.World, 10, Breach);
        var breaching = Time(w.World, 100, Breach);
        Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(wall));
        output.WriteLine($"{width}×{height}, the reference ship {margin} cells in: rooms {w.M.Rooms.All.Count}");
        output.WriteLine($"tick breaching and mending a hull wall: {breaching.Mean:F3} ms mean, {breaching.Max:F3} ms max");
        // Loose for CI's shared runners (17 ms seen there, 3 ms locally); the whole-world flood took 105 ms locally.
        Assert.True(breaching.Mean < 40, "a breach costs the world, not the ship");
    }

    /// <summary>
    /// A thousand furnaces and a thousand analyzers at work, in aired rooms of their own: each finds its
    /// machine, slots and records at once, so the tick grows with the machines, not with their square.
    /// </summary>
    [Fact]
    public void KeepsAThousandFurnacesAndAnalyzersAtWorkUnderBudget()
    {
        string[] tile = ["########", "#FF.AA.#", "#FF.AA.#", "#......#", "#.TT.V.#", "#.TT...#", "#......#", "########"];
        string[] under = ["", "", "", "", "  o  -", "  ----", "", ""];
        string[] Rows(string[] of) => [.. Enumerable.Range(0, 256).Select(y => string.Concat(Enumerable.Repeat(of[y % 8].PadRight(8), 32)))];
        var w = new Scenario(Layout.Parse(Rows(tile), Rows(under)));
        foreach (var m in w.M.Grid.Machines)
        {
            if (m.Kind == "furnace") w.FillCrate(m.Id, ("scrapChunk", 10), ("planks", 50));
            if (m.Kind == "analyzer") w.FillCrate(m.Id, ("scrap", 1));
        }
        // Aired first, so the timed ticks burn, smelt and breathe.
        Time(w.World, 100, _ => []);
        var ticks = Time(w.World, 300, _ => []);
        var furnaces = w.M.Grid.Machines.Where(m => m.Kind == "furnace").ToList();
        int burning = furnaces.Count(m => w.M.Furnace.At(m.Id).Burning);
        int analysing = w.M.Grid.Machines.Count(m => m.Kind == "analyzer" && w.M.Research.At(m.Id) != null);
        output.WriteLine($"{w.M.Grid.Machines.Count} machines, {burning} of {furnaces.Count} furnaces burning, {analysing} analyzers at work");
        output.WriteLine($"tick: {ticks.Mean:F3} ms mean, {ticks.Max:F3} ms max");
        Assert.True(burning > 1000 && analysing > 1000, "the machines stand idle");
        Assert.True(ticks.Mean < 10, "a tick is far over its budget");
    }
}
