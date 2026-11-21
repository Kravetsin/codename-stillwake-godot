using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Where the drone's sparks land round a window (drone-frame, «Нити»): two rows of squares, counted from the near corner.</summary>
public class SootFrameTests
{
    private static readonly (float X, float Y)[] Box = [(0, 0), (300, 0), (300, 172), (0, 172)];

    private static (float X, float Y)[] Rotated(int by) => Enumerable.Range(0, 4).Select(k => Box[(k + by) % 4]).ToArray();

    [Fact]
    public void TwoRowsASquareEveryGapTheSecondOneGapIn()
    {
        var spots = SootFrame.Spots(Box, 0);
        int outer = Enumerable.Range(0, spots.Count).Count(i => spots.Row(i) == 0);
        int inner = spots.Count - outer;
        Assert.Equal((int)MathF.Round(2 * (300 + 172) / DroneInk.FrameGap), outer);
        Assert.Equal((int)MathF.Round(2 * (300 - 2 * DroneInk.FrameGap + 172 - 2 * DroneInk.FrameGap) / DroneInk.FrameGap), inner);
        for (int i = 0; i < spots.Count; i++)
        {
            var (x, y) = spots.Spot(i);
            float off = spots.Row(i) * DroneInk.FrameGap;
            // On its row's edge: as far in from the box as its row, never further.
            float fromEdge = MathF.Min(MathF.Min(x, 300 - x), MathF.Min(y, 172 - y));
            Assert.Equal(off, fromEdge, 3);
        }
    }

    [Fact]
    public void ARowNeverHoldsMoreThanTheMost()
    {
        var huge = new (float X, float Y)[] { (0, 0), (5000, 0), (5000, 3000), (0, 3000) };
        var spots = SootFrame.Spots(huge, 0);
        Assert.Equal(DroneInk.FrameMost, Enumerable.Range(0, spots.Count).Count(i => spots.Row(i) == 0));
        Assert.Equal(DroneInk.FrameMost, Enumerable.Range(0, spots.Count).Count(i => spots.Row(i) == 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void TheNearCornerLeavesFirstTheFarthestLast(int near)
    {
        var outline = Box;
        var spots = SootFrame.Spots(outline, near);
        var corner = outline[near];
        // The first square is the near corner's and leaves at once.
        Assert.Equal(corner, spots.Spot(0));
        Assert.Equal(0, spots.Lag(0));
        Assert.Equal(0, spots.Per(0));
        var opposite = outline[(near + 2) % 4];
        for (int i = 0; i < spots.Count; i++)
        {
            Assert.InRange(spots.Lag(i), 0, 1);
            var (x, y) = spots.Spot(i);
            // Round the box the shorter way, how far it is goes with how late it leaves.
            float round = MathF.Abs(x - corner.X) + MathF.Abs(y - corner.Y);
            if (spots.Row(i) == 0) Assert.Equal(round / ((300 + 172) / 1f), spots.Lag(i), 2);
        }
        int last = Enumerable.Range(0, spots.Count).Where(i => spots.Row(i) == 0).MaxBy(spots.Lag);
        Assert.Equal(opposite.X, spots.Spot(last).X, 1);
        Assert.Equal(opposite.Y, spots.Spot(last).Y, 1);
    }

    [Fact]
    public void EitherWindingIsFramedInside()
    {
        var reversed = Box.Reverse().ToArray();
        var spots = SootFrame.Spots(reversed, 0);
        for (int i = 0; i < spots.Count; i++)
        {
            var (x, y) = spots.Spot(i);
            Assert.InRange(x, -1e-3f, 300.001f);
            Assert.InRange(y, -1e-3f, 172.001f);
        }
    }

    [Theory]
    [InlineData(-50, -40, 0)]
    [InlineData(400, -10, 1)]
    [InlineData(350, 250, 2)]
    [InlineData(-5, 200, 3)]
    public void TheNearCornerIsTheOneNearestTheDrone(float x, float y, int corner) => Assert.Equal(corner, SootFrame.NearCorner(Box, (x, y)));

    [Fact]
    public void AnOutlineStartingElsewhereLaysTheSameSquaresFromTheSameCorner()
    {
        var a = SootFrame.Spots(Box, 2);
        var b = SootFrame.Spots(Rotated(1), 1);
        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++) Assert.Equal(a.Spot(i), b.Spot(i));
    }
}
