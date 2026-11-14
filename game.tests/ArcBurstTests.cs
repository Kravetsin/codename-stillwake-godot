using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The arc's «Срыв разряда» (spear-effects): a blade at rest only fizzes; moving, it tears bolts off its trailing edge.</summary>
public class ArcBurstTests
{
    private const double Frame = 1000.0 / 60;

    // A blade 40 by 4 pixels of its picture, its middle at (20, 4); drawn into the world as it is, moved by an offset.
    private static EffectArea Blade() => EffectArea.Of(40, 8, (x, y) => y >= 2 && y < 6, 1, (0, 0, 40, 8));

    private static (List<ArcBurst.Spark> Sparks, List<ArcBurst.Torn> Torn) Run(ArcBurst burst, int frames, Func<int, double> x)
    {
        var (sparks, torn) = (new List<ArcBurst.Spark>(), new List<ArcBurst.Torn>());
        for (int i = 0; i < frames; i++)
        {
            double dx = x(i);
            burst.Step(Frame, p => (p.X + dx, p.Y), sparks, torn);
        }
        return (sparks, torn);
    }

    [Fact]
    public void ABladeAtRestOnlyFizzesOverItsArea()
    {
        var burst = new ArcBurst(Blade(), new Random(1).NextDouble);
        var (sparks, torn) = Run(burst, 120, _ => 0);
        Assert.Empty(torn);
        Assert.InRange(sparks.Count, 20, 60);
        Assert.All(sparks, s => Assert.True(s.At.Y > 1.5 && s.At.Y < 6.5 && s.Px <= ArcBurst.SparkPx.To));
        Assert.Equal(0, burst.Charged);
    }

    [Fact]
    public void AMovingBladeTearsBoltsOffBackTheWayItCame()
    {
        var burst = new ArcBurst(Blade(), new Random(2).NextDouble);
        var (_, torn) = Run(burst, 120, i => 4.0 * i);
        Assert.True(torn.Count > 20, $"{torn.Count}");
        Assert.All(torn, t => Assert.True(t.Way.X < 0, $"{t.Way}"));
        Assert.All(torn, t => Assert.Equal(t.Px >= ArcBurst.ForkPx, t.Forked));
    }

    [Fact]
    public void AChargeLeaksAwayOnceTheBladeStops()
    {
        var burst = new ArcBurst(Blade(), new Random(3).NextDouble);
        Run(burst, 4, i => 2.0 * i);
        Assert.True(burst.Charged > 0.2, $"{burst.Charged}");
        var (_, torn) = Run(burst, 200, _ => 6);
        Assert.Empty(torn);
        Assert.True(burst.Charged < 0.05, $"{burst.Charged}");
    }

    [Fact]
    public void AJumpOfTheBladeIsNoMotion()
    {
        // Set down a little lower in one frame (a hard landing) or turned about: no charge from it.
        var burst = new ArcBurst(Blade(), new Random(4).NextDouble);
        var (sparks, torn) = (new List<ArcBurst.Spark>(), new List<ArcBurst.Torn>());
        for (int i = 0; i < 60; i++) burst.Step(Frame, p => (p.X, p.Y + (i == 30 ? 12 : 0) + (i > 30 ? 12 : 0)), sparks, torn);
        Assert.Empty(torn);
        Assert.Equal(0, burst.Charged);
    }

    [Fact]
    public void MostTornBoltsAreShortAndAFewLong()
    {
        var lengths = Enumerable.Range(0, 1000).Select(i => ArcBurst.LengthOf((i + 0.5) / 1000)).ToList();
        Assert.Equal(ArcBurst.BoltPx.From, ArcBurst.LengthOf(0), 9);
        Assert.Equal(ArcBurst.BoltPx.To, ArcBurst.LengthOf(1), 9);
        Assert.InRange(lengths.Count(l => l < 2) / 1000.0, 0.5, 0.8);
        Assert.InRange(lengths.Count(l => l >= 6) / 1000.0, 0.03, 0.15);
    }
}
