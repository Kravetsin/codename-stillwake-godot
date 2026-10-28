using System;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The cape (<see cref="Cape"/>), posed by how the hero moves.</summary>
public class CapeTests
{
    /// <summary>The figure's frame is the world's, its feet at <c>Feet</c> and <c>Down</c>, turned by <c>Facing</c>.</summary>
    private sealed class Frame
    {
        public double Feet, Down, Facing = 1;
        public (double X, double Y) ToWorld((double X, double Y) p) => (Feet + p.X * Facing, p.Y + Down);
    }

    private static readonly int Points = Cape.Cols * Cape.Rows;

    /// <summary>Whether it is turned over anywhere, its lining toward the eye.</summary>
    private static bool Folded(Cape cape) => Enumerable.Range(0, Cape.Triangles.Length / 3).Any(cape.Flipped);

    /// <summary>A cape stepped <paramref name="steps"/> times, the feet moving <paramref name="by"/> pixels a step, weightless if <paramref name="afloat"/>.</summary>
    private static void Move(Cape cape, Frame f, int steps, double by = 0, bool afloat = false)
    {
        for (int i = 0; i < steps; i++)
        {
            f.Feet += by;
            cape.Step(Rope.StepMs, f.ToWorld, afloat);
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
    public void ARunLiftsItFarHigherThanAWalk()
    {
        double Lift(double speed)
        {
            var (f, cape) = (new Frame(), new Cape());
            Move(cape, f, 120);
            Move(cape, f, 120, speed);
            return Cape.Rest(Points - 1).Y - cape.Points[^1].Y;
        }
        double walk = Lift(115.0 / 60), run = Lift(Sprint);
        Assert.True(run > walk * 1.8, $"the hem rises {run:F1} running, {walk:F1} walking");
        Assert.True(run > 12, $"the hem only rises {run:F1} running");
    }

    [Theory]
    [InlineData(115.0 / 60)]
    [InlineData(211.0 / 60)]
    public void GoingBackwardItStreamsBehindAsGoingForwardRatherThanWrappingTheHero(double speed)
    {
        Cape Go(double by)
        {
            var (f, cape) = (new Frame(), new Cape());
            Move(cape, f, 120);
            for (int step = 0; step < 120; step++)
            {
                Move(cape, f, 1, by);
                for (int i = 0; i < Points; i++) Assert.True(cape.Points[i].X - Cape.Rest(i).X <= 1.01, $"point {i} came forward of its cut");
            }
            return cape;
        }
        var (forward, backward) = (Go(speed), Go(-speed));
        double hem = Cape.Rest(Points - 1).X - backward.Points[^1].X;
        Assert.True(hem > 5, $"going backward the hem streams only {hem:F1} behind its cut");
        Assert.InRange(backward.Points[^1].Y - forward.Points[^1].Y, -3, 3);
    }

    [Fact]
    public void ItsInsideWrapsTheBodyHangingButNotSwungAwayOnARun()
    {
        // Its lower half: by the shoulders it lies on the body even swung, its inside hidden behind it.
        double Wrap(Cape cape) => Enumerable.Range(Points / 2, Points - Points / 2).Max(i => Math.Abs(cape.Inside[i].X - cape.Points[i].X));
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 300);
        Assert.True(Wrap(cape) > 3, $"hanging, its inside wraps only {Wrap(cape):F1}");
        Move(cape, f, 120, Sprint);
        Assert.True(Wrap(cape) < 1.5, $"swung away on a run, its inside still shows {Wrap(cape):F1} past it");
    }

    [Fact]
    public void StandingItStirsInTheBreezeWithoutComingToRest()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 300);
        double least = double.MaxValue, most = double.MinValue;
        for (int step = 0; step < 120; step++)
        {
            Move(cape, f, 1);
            (least, most) = (Math.Min(least, cape.Points[^1].X), Math.Max(most, cape.Points[^1].X));
        }
        Assert.InRange(most - least, 0.5, 5);
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
        var still = cape.Points.ToArray();
        for (int turn = 0; turn < 4; turn++)
        {
            f.Facing = -f.Facing;
            Move(cape, f, 300);
            // As it hung before the turn, but for the breeze.
            for (int i = 0; i < Points; i++)
            {
                Assert.InRange(cape.Points[i].X - still[i].X, -2, 2);
                Assert.InRange(cape.Points[i].Y - still[i].Y, -2, 2);
            }
            Assert.DoesNotContain(true, Enumerable.Range(0, Cape.Triangles.Length / 3).Select(cape.Flipped));
        }
    }

    [Fact]
    public void ItsHoodHangsAsCutAndSwingsBackWithTheCapeOnARun()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 300);
        var hood = Enumerable.Range(0, Cape.HoodCols * Cape.HoodRows).ToArray();
        foreach (int i in hood)
        {
            Assert.InRange(cape.Hood[i].X - Cape.HoodRest(i).X, -1.5, 1.01);
            Assert.InRange(cape.Hood[i].Y - Cape.HoodRest(i).Y, -1.5, 1.5);
        }
        Move(cape, f, 120, Sprint);
        double back = Cape.HoodRest(HoodTip).X - cape.Hood[^2].X;
        Assert.True(back > 1, $"the tip only swung {back:F2} back");
    }

    [Fact]
    public void RisingFromAJumpItTrailsDownBelowAFlatRun()
    {
        double Hem(double up)
        {
            var (f, cape) = (new Frame(), new Cape());
            Move(cape, f, 120, Sprint);
            for (int i = 0; i < 20; i++)
            {
                f.Down -= up;
                Move(cape, f, 1, Sprint);
            }
            return cape.Points[^1].Y;
        }
        double flat = Hem(0), rising = Hem(4);
        Assert.True(rising > flat + 4, $"rising, the hem is at {rising:F1}, on the flat at {flat:F1}");
    }

    [Fact]
    public void ASprintRunsDownItAsAWhipTheShouldersFirst()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 300);
        var still = cape.Points.ToArray();
        Move(cape, f, 300, Sprint);
        var swept = cape.Points.ToArray();
        var (f2, early) = (new Frame(), new Cape());
        Move(early, f2, 300);
        Move(early, f2, 15, Sprint);
        // How far along from still to swept the back edge is, at a third of the way down and at the hem.
        double Along(int i) => (early.Points[i].Y - still[i].Y) / (swept[i].Y - still[i].Y);
        int third = Cape.Rows / 3 * Cape.Cols + Cape.Cols - 1;
        Assert.True(Along(third) > Along(Points - 1) + 0.1, $"a third down it has come {Along(third):F2} of the way, the hem {Along(Points - 1):F2}");
    }

    [Fact]
    public void ItNeverFoldsHoweverTheHeroMoves()
    {
        var (f, cape) = (new Frame(), new Cape());
        void Go(int steps, double by, double up, bool afloat = false)
        {
            for (int i = 0; i < steps; i++)
            {
                (f.Feet, f.Down) = (f.Feet + by, f.Down - up);
                cape.Step(Rope.StepMs, f.ToWorld, afloat);
                Assert.False(Folded(cape), $"folded going {by:F1}, {up:F1} up");
            }
        }
        Go(120, 0, 0);
        Go(90, Sprint, 0);
        Go(30, Sprint, 4);
        Go(40, Sprint, -6);
        f.Facing = -1;
        Go(60, Sprint, 0);
        Go(60, -Sprint, 0);
        Go(60, 0, -6);
        Go(60, Sprint, -6);
        Go(120, 0, 0);
        Go(120, 2, 2, afloat: true);
        Go(120, 0, 0, afloat: true);
    }

    [Fact]
    public void AFallThrowsItUpBehind()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 300);
        double most = 0;
        for (int i = 1; i <= 60; i++)
        {
            f.Down += Math.Min(6, i * 0.25);
            Move(cape, f, 1);
            most = Math.Max(most, Cape.Rest(Points - 1).Y - cape.Points[^1].Y);
        }
        Assert.True(most > 15, $"the hem only rose {most:F1}");
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
            Assert.False(Folded(cape));
            (least, most) = (Math.Min(least, cape.Points[^1].X), Math.Max(most, cape.Points[^1].X));
        }
        Assert.InRange(most - least, 0.3, 6);
    }

    [Fact]
    public void AfloatAPushOffSweepsItBackAndAStopLetsItOpenAgain()
    {
        var (f, cape) = (new Frame(), new Cape());
        Move(cape, f, 600, afloat: true);
        double start = cape.Points[^1].X;
        double Off() => cape.Points[^1].X - start;
        double behind = 0;
        for (int step = 0; step < 60; step++)
        {
            Move(cape, f, 1, 2, afloat: true);
            behind = Math.Min(behind, Off());
        }
        Assert.True(behind < -2, $"the hem only swept {-behind:F1} back");
        Move(cape, f, 600, afloat: true);
        Assert.InRange(Off(), -2, 2);
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
