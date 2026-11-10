using FsCheck;
using FsCheck.Xunit;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Grass parted by a body, leaned the way it runs and sprung back behind it (plant-stirs).</summary>
public class StirsTests
{
    private static readonly FloraLooks.Feel Soft = new("test", Reach: 12, Part: 4, Lean: 3, SpringHz: 2.5f, Damping: 0.3f);
    private const double Frame = 1 / 60.0;

    /// <summary>A hero standing on the ground at row 10, its feet at column 50's point, for <paramref name="frames"/>.</summary>
    private static Stirs Standing(int frames, float vx = 0, FloraLooks.Feel? feel = null)
    {
        var stirs = new Stirs();
        for (int i = 0; i < frames; i++)
        {
            stirs.Body(50 * Stirs.Step, 10 * Stirs.Step, vx, feel ?? Soft);
            stirs.Advance(Frame, feel ?? Soft);
        }
        return stirs;
    }

    [Fact]
    public void AHeroStandingPartsTheGrassAwayFromItsFeetAndHoldsItThere()
    {
        var stirs = Standing(240);
        Assert.Equal(0, stirs.At(50, 10), 2);
        Assert.True(stirs.At(48, 10) < -2, $"left of the feet leans left: {stirs.At(48, 10)}");
        Assert.True(stirs.At(52, 10) > 2, $"right of the feet leans right: {stirs.At(52, 10)}");
        Assert.Equal(-stirs.At(48, 10), stirs.At(52, 10), 3);
        float held = stirs.At(52, 10);
        var later = Standing(480);
        Assert.Equal(held, later.At(52, 10), 2);
    }

    [Fact]
    public void ARunnerLeansTheGrassUnderItTheWayItRuns()
    {
        var right = Standing(240, vx: Stirs.Run);
        var left = Standing(240, vx: -Stirs.Run);
        Assert.Equal(Soft.Lean, right.At(50, 10), 1);
        Assert.Equal(-Soft.Lean, left.At(50, 10), 1);
    }

    [Fact]
    public void OnlyTheGroundAtTheFeetIsPushedNotGrassAboveOrBelow()
    {
        var stirs = Standing(60);
        Assert.Equal(0, stirs.At(52, 9));
        Assert.Equal(0, stirs.At(52, 11));
        Assert.Equal(0, stirs.At(50 + 10, 10));
    }

    [Fact]
    public void GrassLeftBehindSpringsPastUprightThenSettlesAndIsForgotten()
    {
        var stirs = Standing(240);
        float parted = stirs.At(52, 10);
        float least = parted;
        for (int i = 0; i < 60; i++)
        {
            stirs.Advance(Frame, Soft);
            least = Math.Min(least, stirs.At(52, 10));
        }
        Assert.True(least < -0.2f * parted, $"rings back past upright: {least} after {parted}");
        for (int i = 0; i < 600; i++) stirs.Advance(Frame, Soft);
        Assert.Equal(0, stirs.Count);
    }

    [Fact]
    public void AHeavyFeelComesBackWithoutPassingUpright()
    {
        var heavy = Soft with { SpringHz = 1.5f, Damping = 1 };
        var stirs = Standing(600, feel: heavy);
        float least = float.MaxValue;
        for (int i = 0; i < 300; i++)
        {
            stirs.Advance(Frame, heavy);
            least = Math.Min(least, stirs.At(52, 10));
        }
        Assert.True(least > -0.01f, $"never past upright: {least}");
    }

    [Fact]
    public void TwoHeroesSideBySidePushTogether()
    {
        var stirs = new Stirs();
        for (int i = 0; i < 240; i++)
        {
            stirs.Body(50 * Stirs.Step, 10 * Stirs.Step, Stirs.Run, Soft);
            stirs.Body(50 * Stirs.Step, 10 * Stirs.Step, Stirs.Run, Soft);
            stirs.Advance(Frame, Soft);
        }
        Assert.Equal(2 * Soft.Lean, stirs.At(50, 10), 1);
    }

    [Fact]
    public void WrittenPointsAreAtTheirColumnAndTheGroundInPixels()
    {
        var stirs = Standing(60);
        var into = new (float X, float Ground, float Offset)[Stirs.Most];
        int n = stirs.Write(into);
        Assert.Equal(stirs.Count, n);
        var right = into.Take(n).Single(p => p.X == 52 * Stirs.Step);
        Assert.Equal(10 * Stirs.Step, right.Ground);
        Assert.Equal(stirs.At(52, 10), right.Offset);
    }

    /// <summary>However many bodies run wherever, no more than <see cref="Stirs.Most"/> points are kept, the busiest kept.</summary>
    [Property(MaxTest = 50)]
    public void NeverMoreThanMostPoints(PositiveInt bodies, int seed)
    {
        var random = new Random(seed);
        var stirs = new Stirs();
        for (int frame = 0; frame < 30; frame++)
        {
            for (int b = 0; b < Math.Min(bodies.Get, 40); b++)
                stirs.Body(random.Next(0, 4000), random.Next(0, 40) * Stirs.Step, (float)(random.NextDouble() * 2 - 1) * Stirs.Run, Soft);
            stirs.Advance(Frame, Soft);
            Assert.True(stirs.Count <= Stirs.Most, $"{stirs.Count} points");
        }
    }

    [Fact]
    public void ClumpsSideBySideSpringOutOfStepWithinAFifthOfTheFeel()
    {
        var rates = Enumerable.Range(-50, 100).Select(Stirs.Uneven).ToList();
        Assert.All(rates, r => Assert.InRange(r, 0.8f, 1.2f));
        Assert.True(rates.Zip(rates.Skip(1)).Count(p => Math.Abs(p.First - p.Second) > 0.05f) > 60, "neighbours differ");
    }
}
