using System.Diagnostics;
using System.Globalization;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Features.Liquids;
using Stillwake.Sim;
using Xunit;
using L = Stillwake.Game.Light;
using Liquid = Stillwake.Content.Liquids;

namespace Stillwake.Game.Tests;

/// <summary>
/// What the client's work for a frame costs on a planet, timed with a Stopwatch: the bounds are a desk's, times
/// <c>STILLWAKE_BUDGET_SLACK</c> on CI's slower runners. Run apart from other tests, which would slow them.
/// </summary>
[Collection(nameof(FrameBudgetTests))]
public class FrameBudgetTests
{
    private static readonly double Slack =
        double.TryParse(Environment.GetEnvironmentVariable("STILLWAKE_BUDGET_SLACK"), CultureInfo.InvariantCulture, out double s) && s >= 1 ? s : 1;

    private static double Within(double ms) => ms * Slack;

    /// <summary>
    /// The water written for a dry view, as the liquids view does every frame (planet-frame-rate): the first planet's size, a lake far
    /// off. Every cell of it looked at took 3.6 ms in the game; skipping the dry rows by their bytes, a desk takes a few hundredths.
    /// </summary>
    [Fact]
    public void WritesTheWaterOfADryViewInAFractionOfAFrame()
    {
        int w = 4096, h = 1536;
        var layer = new byte[w * h];
        for (int y = 1200; y < 1220; y++)
            for (int x = 3000; x < 3100; x++) layer[y * w + x] = Liquid.Of(Liquid.Water, Liquid.Full);
        bool Blocks(int cell) => cell / w > 800;
        var texels = new byte[240 * 140 * 4];
        for (int i = 0; i < 20; i++) WaterCells.Write(layer, w, h, Blocks, 1900, 600, 240, 140, texels);
        const int Frames = 200;
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < Frames; i++) WaterCells.Write(layer, w, h, Blocks, 1900, 600, 240, 140, texels);
        double ms = watch.Elapsed.TotalMilliseconds / Frames;
        Assert.True(ms < Within(0.25), $"a dry view took {ms:F3} ms");
    }

    /// <summary>
    /// A dig on the first planet followed by the starlight, as each wall the hero breaks asks (planet-frame-rate). Worked out in a
    /// square as wide as daylight falls down a shaft, it took 3.8 ms on a desk; in the tall narrow box, a fifth of that.
    /// </summary>
    [Fact]
    public void FollowsADigOnAPlanetInAFractionOfAFrame()
    {
        var m = new Registry();
        World.Create(1, Terrain.Generate(Content.PlanetTypes.First, 2), m.All);
        var field = new StarField((0.9f, 0.9f, 0.8f), L.GroundKeep, L.GroundDownKeep, L.GroundSoak);
        field.Work(m.Grid);
        int cell = 600 * m.Grid.Width + 2000;
        int[] dug = [cell, cell + 1, cell + m.Grid.Width];
        for (int i = 0; i < 5; i++) field.Follow(m.Grid, dug);
        const int Digs = 30;
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < Digs; i++) field.Follow(m.Grid, dug);
        double ms = watch.Elapsed.TotalMilliseconds / Digs;
        Assert.True(ms < Within(1.2), $"following a dig took {ms:F3} ms");
    }

    /// <summary>
    /// A growth felled among a planet's caves' 57,000, found by the cave view (planet-frame-rate): a set of them all made each time
    /// took 3 to 8 ms; halving the runs that lost one, a desk takes a few thousandths.
    /// </summary>
    [Fact]
    public void FindsAGrowthFelledAmongAPlanetsInAFractionOfAFrame()
    {
        var all = Enumerable.Range(0, 57_000).Select(i => new Sim.Building.Plant(i * 97, "moss")).ToList();
        var standing = all.Where(p => p.Cell != 28_500 * 97).ToList();
        var felled = new List<int>();
        const int Times = 50;
        double ms = 0;
        for (int i = 0; i < Times + 5; i++)
        {
            var kept = all.Select(p => p.Cell).ToList();
            felled.Clear();
            var watch = Stopwatch.StartNew();
            Features.Flora.Felling.Take(kept, standing, felled);
            if (i >= 5) ms += watch.Elapsed.TotalMilliseconds / Times;
        }
        Assert.Equal([28_500 * 97], felled);
        Assert.True(ms < Within(0.1), $"finding a growth felled took {ms:F3} ms");
    }
}

[CollectionDefinition(nameof(FrameBudgetTests), DisableParallelization = true)]
public sealed class FrameBudgetCollection;
