using System;
using System.Linq;
using Stillwake.Game.Menu;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The scene behind the title: the plate drawn, how it sways, what drifts past it and how.</summary>
public class TitleSceneTests
{
    [Fact]
    public void ThePlatesSeatLampAndRingSitOnItsPaintedTopItsSeatAnEdgeWithSpaceAboveAndToItsRight()
    {
        var (w, h, plate) = Png.Logical("title_plate");
        Assert.Equal((PlateArt.Width, PlateArt.Height), (w, h));
        bool Drawn(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && plate[y * w + x] >> 24 != 0;
        // The top below the rib, which stands out of the plate's left third.
        int TopOf(int x) => Enumerable.Range(PlateArt.Seat.Y - 4, h - PlateArt.Seat.Y + 4).First(y => Drawn(x, y));
        foreach (var (x, y) in new[] { PlateArt.Seat, PlateArt.Lamp, PlateArt.Anchor })
            Assert.InRange(TopOf(x), y - 1, y + 1);
        var (sx, sy) = PlateArt.Seat;
        for (int up = 2; up < 10; up++) Assert.False(Drawn(sx, sy - up), $"drawn {up} above the seat");
        // The legs hang past the end: nothing a few pixels to its right, all the way down.
        for (int down = 0; down < 20; down++) Assert.False(Drawn(sx + 4, sy + down), $"drawn {down} under the seat's right");
        Assert.True(TopOf(10) > sy + 4, "the left end is not bent down");
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
