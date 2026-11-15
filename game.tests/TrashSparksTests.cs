using System;
using System.Linq;
using Stillwake.Game.Features.Items;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The trash's burst (trash-effect A): sparks out of the ring, all gone within the burst.</summary>
public class TrashSparksTests
{
    [Fact]
    public void SparksLeaveFromTheRingAndFlyOutPastIt()
    {
        foreach (var spark in TrashSparks.Make(new Random(7)))
        {
            var (x0, y0) = TrashSparks.At(spark, 0, 40, 13.76f);
            Assert.Equal(13.76f, MathF.Sqrt(x0 * x0 + y0 * y0), 3);
            // Outward along its own direction, past the ring, whatever it sags.
            var (x1, y1) = TrashSparks.At(spark, 1, 40, 13.76f);
            Assert.True(x1 * MathF.Cos(spark.Angle) + y1 * MathF.Sin(spark.Angle) > 13.76f + 0.1f * 40);
        }
    }

    [Fact]
    public void EverySparkAndTheFlashAreOutWithinTheBurst()
    {
        var sparks = TrashSparks.Make(new Random(3));
        Assert.Equal(TrashSparks.Count, sparks.Count);
        Assert.All(sparks, s => Assert.InRange(s.LifeMs, 1, TrashSparks.Ms));
        Assert.All(sparks, s => Assert.Null(TrashSparks.Lived(s, TrashSparks.Ms)));
        Assert.Equal(1, TrashSparks.Flash(0));
        Assert.Equal(0, TrashSparks.Flash(TrashSparks.FlashMs));
    }

    [Fact]
    public void SparksSpreadRoundTheWholeRing()
    {
        var angles = TrashSparks.Make(new Random(11)).Select(s => s.Angle).ToArray();
        Assert.All(angles, a => Assert.InRange(a, 0, 2 * MathF.PI));
        // One in each of the circle's quarters at least.
        Assert.Equal(4, angles.Select(a => (int)(a / (MathF.PI / 2))).Distinct().Count());
    }
}
