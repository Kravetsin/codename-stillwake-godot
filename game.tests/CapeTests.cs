using System;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A sheet of cloth (<see cref="Cloth"/>) and the cape made of one (<see cref="Cape"/>).</summary>
public class CapeTests
{
    [Fact]
    public void AClothHungFromItsTopRowHangsBelowItKeepingItsWidth()
    {
        var start = Enumerable.Range(0, 12).Select(i => ((double)(i % 3) * 4, (double)(i / 3) * 4)).ToArray();
        var cloth = new Cloth(new ClothStyle(Keep: 0.98, Gravity: 0.4, Shear: 0.5, Squash: 0.3, Passes: 10), 3, 4, start);
        for (int i = 0; i < 600; i++) cloth.Update([0, 1, 2], p => start[p], Rope.StepMs);
        var (left, right) = (cloth.At(9), cloth.At(11));
        Assert.InRange(right.X - left.X, 7.5, 8.5);
        Assert.InRange(left.Y, 11, 13.5);
    }

    /// <summary>The figure's frame is the world's, its feet at <c>Feet</c> and <c>Down</c>, turned by <c>Facing</c>.</summary>
    private sealed class Frame
    {
        public double Feet, Down, Facing = 1;
        public (double X, double Y) ToWorld((double X, double Y) p) => (Feet + p.X * Facing, p.Y + Down);
        public (double X, double Y) ToFrame((double X, double Y) p) => ((p.X - Feet) * Facing, p.Y - Down);
    }

    private static readonly int Points = Cape.Cols * Cape.Rows;

    /// <summary>A cape stepped <paramref name="steps"/> times, the feet moving <paramref name="by"/> pixels a step, weightless if <paramref name="afloat"/>.</summary>
    private static void Move(Cape cape, Frame f, int steps, double by = 0, bool afloat = false)
    {
        for (int i = 0; i < steps; i++)
        {
            f.Feet += by;
            cape.Step(Rope.StepMs, f.ToWorld, f.ToFrame, afloat);
        }
    }

    private const double Sprint = 211.0 / 60;

    [Fact]
    public void ACapeHangsAsItWasCutAndOnARunTrailsBack()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 300);
        for (int i = 0; i < Points; i++)
        {
            Assert.InRange(cape.Points[i].X - Cape.Rest(i).X, -2.5, 1.01);
            Assert.InRange(cape.Points[i].Y - Cape.Rest(i).Y, -2.5, 2.5);
        }
        Assert.DoesNotContain(true, Enumerable.Range(0, Cape.Triangles.Length / 3).Select(cape.Flipped));
        Move(cape, f, 90, Sprint);
        double hem = cape.Points[^1].X - Cape.Rest(Points - 1).X;
        Assert.True(hem < -4, $"the hem only {hem} back of its cut");
    }

    [Fact]
    public void ARunLiftsItFarMoreThanAWalk()
    {
        double Trail(double speed)
        {
            var (f, cape) = (new Frame(), new Cape());
            Move(cape, f, 120);
            Move(cape, f, 120, speed);
            return Cape.Rest(Points - 1).X - cape.Points[^1].X;
        }
        double walk = Trail(115.0 / 60), run = Trail(Sprint);
        Assert.True(run > walk * 1.8, $"the hem trails {run:F1} running, {walk:F1} walking");
    }

    [Fact]
    public void WalkingBackwardItKeepsItsCutRoundTheBackRatherThanWrappingTheHero()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 120);
        double most = double.MinValue;
        for (int step = 0; step < 120; step++)
        {
            Move(cape, f, 1, -Sprint);
            for (int i = 0; i < Points; i++) most = Math.Max(most, cape.Points[i].X - Cape.Rest(i).X);
        }
        Assert.True(most <= 1.01, $"a point came {most} forward of its cut");
    }

    [Fact]
    public void FacingLeftItHangsAndTrailsAsFacingRightMirrored()
    {
        Cape Run(int facing)
        {
            var (f, cape) = (new Frame { Facing = facing }, new Cape());
            Move(cape, f, 120);
            Move(cape, f, 90, facing * Sprint);
            return cape;
        }
        var (right, left) = (Run(1), Run(-1));
        for (int i = 0; i < Points; i++)
        {
            Assert.InRange(left.Points[i].X - right.Points[i].X, -0.01, 0.01);
            Assert.InRange(left.Points[i].Y - right.Points[i].Y, -0.01, 0.01);
        }
    }

    [Fact]
    public void TurnedAboutStandingItSwingsRoundToHangBehindAgain()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 300);
        for (int turn = 0; turn < 4; turn++)
        {
            f.Facing = -f.Facing;
            Move(cape, f, 120);
            for (int i = 0; i < Points; i++)
            {
                Assert.InRange(cape.Points[i].X - Cape.Rest(i).X, -2.5, 1.01);
                Assert.InRange(cape.Points[i].Y - Cape.Rest(i).Y, -2.5, 2.5);
            }
            Assert.DoesNotContain(true, Enumerable.Range(0, Cape.Triangles.Length / 3).Select(cape.Flipped));
        }
    }

    [Fact]
    public void ItsHoodHangsAsCutAndTossesUpWhenTheHeroStopsRising()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 300);
        var hood = Enumerable.Range(0, Cape.HoodCols * Cape.HoodRows).ToArray();
        foreach (int i in hood)
        {
            Assert.InRange(cape.Hood[i].X - Cape.HoodRest(i).X, -1.5, 1.01);
            Assert.InRange(cape.Hood[i].Y - Cape.HoodRest(i).Y, -1.5, 1.5);
        }
        // Rising, as from a jump or the bounce of a run, then stopped: the tip goes on up, bunching, and drops back.
        for (int i = 0; i < 20; i++)
        {
            f.Down -= 2;
            Move(cape, f, 1);
        }
        double most = 0;
        for (int i = 0; i < 60; i++)
        {
            Move(cape, f, 1);
            most = Math.Max(most, Cape.HoodRest(HoodTip).Y - cape.Hood[^2].Y);
        }
        Assert.True(most > 0.5, $"the tip only rose {most:F2}");
        Move(cape, f, 240);
        Assert.InRange(cape.Hood[^2].Y - Cape.HoodRest(HoodTip).Y, -1, 1);
    }

    [Fact]
    public void AFallThrowsItUpOverTheShoulders()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 300);
        Assert.False(cape.TurnedUp);
        bool up = false;
        for (int i = 0; i < 40 && !up; i++)
        {
            f.Down += 5;
            Move(cape, f, 1);
            up = cape.TurnedUp;
        }
        Assert.True(up);
    }

    private static readonly int HoodTip = Cape.HoodCols * Cape.HoodRows - 2;

    [Fact]
    public void StoppedAfterARunItStraightensRatherThanStayingBunched()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 120, Sprint);
        Move(cape, f, 240);
        // Its front edge, top to hem, as long as it was cut: hanging, not folded up against the legs.
        double along = 0, cut = 0;
        for (int r = 1; r < Cape.Rows; r++)
        {
            along += cape.Points[r * Cape.Cols].Y - cape.Points[(r - 1) * Cape.Cols].Y;
            cut += Cape.Rest(r * Cape.Cols).Y - Cape.Rest((r - 1) * Cape.Cols).Y;
        }
        Assert.True(along > cut * 0.85, $"its front edge hangs {along:F1} of {cut:F1}");
    }

    [Fact]
    public void AfloatItOpensOutBehindAndRipplesWithoutComingToRest()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 300);
        Move(cape, f, 600, afloat: true);
        var hem = cape.Points[^1];
        Assert.True(hem.X < Cape.Rest(Points - 1).X - 3, $"the hem only {Cape.Rest(Points - 1).X - hem.X:F1} back of its hang");
        Assert.True(hem.Y < Cape.Rest(Points - 1).Y - 1, $"the hem only {Cape.Rest(Points - 1).Y - hem.Y:F1} up from its hang");
        double least = double.MaxValue, most = double.MinValue;
        for (int step = 0; step < 200; step++)
        {
            Move(cape, f, 1, afloat: true);
            Assert.DoesNotContain(true, Enumerable.Range(0, Cape.Triangles.Length / 3).Select(cape.Flipped));
            (least, most) = (Math.Min(least, cape.Points[^1].X), Math.Max(most, cape.Points[^1].X));
        }
        Assert.InRange(most - least, 0.3, 6);
    }

    [Fact]
    public void AfloatAPushOffLeavesItBehindAndAStopCarriesItOn()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 600, afloat: true);
        double start = cape.Points[^1].X;
        double Off() => cape.Points[^1].X - start;
        double behind = 0, ahead = 0;
        for (int step = 0; step < 60; step++)
        {
            Move(cape, f, 1, 2, afloat: true);
            behind = Math.Min(behind, Off());
        }
        for (int step = 0; step < 120; step++)
        {
            Move(cape, f, 1, afloat: true);
            ahead = Math.Max(ahead, Off());
        }
        Assert.True(behind < -2, $"the hem only trailed {-behind:F1}");
        Assert.True(ahead > 1, $"the hem only carried on {ahead:F1}");
        Move(cape, f, 600, afloat: true);
        Assert.InRange(Off(), -4, 4);
        for (int i = 0; i < Points; i++) Assert.True(cape.Points[i].X <= Cape.Rest(i).X + 1.01);
    }

    [Fact]
    public void BackInGravityAfterFloatingItHangsAsCutAgain()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 600, afloat: true);
        Move(cape, f, 300);
        for (int i = 0; i < Points; i++)
        {
            Assert.InRange(cape.Points[i].X - Cape.Rest(i).X, -2.5, 1.01);
            Assert.InRange(cape.Points[i].Y - Cape.Rest(i).Y, -2.5, 2.5);
        }
        Assert.DoesNotContain(true, Enumerable.Range(0, Cape.Triangles.Length / 3).Select(cape.Flipped));
    }
}
