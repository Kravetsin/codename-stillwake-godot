using System;
using System.Linq;
using Stillwake.Game.Menu;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The scene behind the title: the plate drawn, how it sways, what drifts past it and how.</summary>
public class TitleSceneTests
{
    [Fact]
    public void ThePlateIsTheSameForASeedAndItsSeatALevelEdgeWithSpaceAboveAndToItsRight()
    {
        var plate = PlateArt.Of(7);
        Assert.Equal(plate.Pixels, PlateArt.Of(7).Pixels);
        var (x, y) = plate.Seat;
        Assert.Equal(PlatePixel.Lit, plate.At(x, y));
        Assert.Equal(PlatePixel.Lit, plate.At(x - 6, y));
        for (int up = 1; up < 10; up++) Assert.Equal(PlatePixel.None, plate.At(x, y - up));
        // The legs hang past the end: nothing a few pixels to its right, all the way down.
        for (int down = 0; down < 20; down++) Assert.Equal(PlatePixel.None, plate.At(x + 4, y + down));
    }

    [Fact]
    public void TheLampAndTheTetherRingSitOnThePlatesLitTop()
    {
        var plate = PlateArt.Of(7);
        foreach (var (x, y) in new[] { plate.Lamp, plate.Anchor })
        {
            Assert.Equal(PlatePixel.Lit, plate.At(x, y));
            Assert.Equal(PlatePixel.None, plate.At(x, y - 1));
        }
    }

    [Fact]
    public void ThePlateIsBentDownAndTornAtItsLeftWithWiresHangingFromItsTears()
    {
        var plate = PlateArt.Of(7);
        int TopOf(int x) => Enumerable.Range(PlateArt.RibHigh, plate.Height - PlateArt.RibHigh).First(y => plate.At(x, y) != PlatePixel.None);
        Assert.True(TopOf(10) > plate.Seat.Y + 4, "the left end is not bent down");
        Assert.Contains(PlatePixel.Wire, plate.Pixels);
        Assert.Contains(PlatePixel.Rib, plate.Pixels);
    }

    [Fact]
    public void ThePlateSwaysWithinItsBounds()
    {
        for (double s = 0; s < 200; s += 1.7)
        {
            var (turn, dy) = TitleScene.Sway(s);
            Assert.InRange(turn, -TitleScene.Tilt, TitleScene.Tilt);
            Assert.InRange(dy, -TitleScene.Bob, TitleScene.Bob);
        }
    }

    [Fact]
    public void EverythingComesFromTheRightAndGoesOffTheLeftAsThePlateDriftsRightAndIsGoneOnceAcross()
    {
        var rng = new Random(7);
        for (int i = 0; i < 200; i++)
            foreach (var drift in new[] { TitleScene.NextDrifter(rng), TitleScene.NextShard(rng) })
            {
                Assert.True(drift.FromX > TitleScene.Wide && drift.ToX < 0);
                Assert.InRange(drift.Depth, 0, 1);
                Assert.True(drift.Seconds > 0);
                Assert.Equal((drift.FromX, drift.FromY, 0.0), drift.At(0));
                Assert.Null(drift.At(drift.Seconds + 0.01));
                Assert.Null(drift.At(-0.01));
            }
    }

    [Fact]
    public void AstronautsDriftAboveOrBelowThePlateNeverAcrossIt()
    {
        var rng = new Random(3);
        for (int i = 0; i < 200; i++)
        {
            double y = TitleScene.NextDrifter(rng).FromY;
            Assert.True(y <= TitleScene.High * 0.32 || y >= TitleScene.High * 0.7, $"{y} crosses the plate's band");
        }
    }

    [Fact]
    public void AShardIsAFewCornersAboutItsMiddle()
    {
        var rng = new Random(5);
        for (int i = 0; i < 50; i++)
        {
            var shard = TitleScene.ShardOf(rng, 10);
            Assert.InRange(shard.Length, 4, 7);
            Assert.All(shard, p => Assert.True(Math.Abs(p.X) <= 8 && Math.Abs(p.Y) <= 3.5));
        }
    }
}
