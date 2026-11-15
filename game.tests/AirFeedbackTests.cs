using System;
using System.Linq;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The suit gauge's feedback as picked (air-feedback): a lamp lost chips off, a canister floods the tab.</summary>
public class AirFeedbackTests
{
    private static readonly (float X, float W)[] Cells = [.. Enumerable.Range(0, 15).Select(i => (30f + i * 19, 17f))];

    [Fact]
    public void ALampLostWholeWhileTheAirFallsPopsAChipAndSparksThatFallOutAndGo()
    {
        var flakes = new LampFlakes(new Random(1));
        flakes.Step(0.01, 0.6, 8, Cells, 10, 10);
        Assert.Empty(flakes.Pops);
        flakes.Step(0.01, 0.58, 8, Cells, 10, 10);
        Assert.Empty(flakes.Chips);
        flakes.Step(0.01, 0.53, 7, Cells, 10, 10);
        Assert.Equal([8], flakes.Pops.Select(p => p.Lamp));
        var chip = Assert.Single(flakes.Chips);
        Assert.InRange(chip.X, Cells[8].X, Cells[8].X + Cells[8].W);
        Assert.Equal(LampFlakes.Sparks, flakes.SparksFlying.Count);
        float y = chip.Y;
        for (int i = 0; i < 90; i++) flakes.Step(0.01, 0.53, 7, Cells, 10, 10);
        // It jumps up first, then falls below the tab.
        Assert.True(chip.Y > y + 20, $"{chip.Y}: it falls out of the tab");
        for (int i = 0; i < 30; i++) flakes.Step(0.01, 0.53, 7, Cells, 10, 10);
        Assert.Empty(flakes.Chips);
        Assert.Empty(flakes.SparksFlying);
        Assert.Empty(flakes.Pops);
    }

    [Fact]
    public void AFallOfSeveralLampsPopsEachAndARiseNone()
    {
        var flakes = new LampFlakes(new Random(2));
        flakes.Step(0.01, 0.6, 8, Cells, 10, 10);
        flakes.Step(0.01, 0.4, 5, Cells, 10, 10);
        Assert.Equal([6, 7, 8], flakes.Pops.Select(p => p.Lamp).Order());
        flakes.Step(0.01, 0.8, 11, Cells, 10, 10);
        Assert.Equal(3, flakes.Chips.Count);
    }

    [Fact]
    public void ACanisterShakesTheRowAMomentAndCoolsItsStretchesFromTheLeft()
    {
        var wash = new GaugeWash();
        Assert.Null(wash.Since);
        Assert.Equal(0.0, wash.White(0));
        wash.Breathe(3);
        Assert.Equal(1.0, wash.White(0));
        wash.Step(0.05);
        Assert.NotEqual((0, 0), wash.Shake);
        wash.Step(GaugeWash.ShakeS);
        Assert.Equal((0, 0), wash.Shake);
        // The left stretches have begun to cool, the right ones still burn white.
        Assert.True(wash.White(0) < 1 && wash.White(14) == 1);
        // Only the stretches it filled glow on.
        Assert.Equal(0, wash.Glow(2));
        Assert.True(wash.Glow(3) > 0);
        for (int i = 0; i < 300; i++) wash.Step(0.01);
        Assert.Null(wash.Since);
        Assert.Equal((0.0, 0.0), (wash.White(14), wash.Glow(3)));
    }
}
