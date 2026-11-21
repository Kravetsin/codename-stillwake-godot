using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The motion «Отставание»: hologram windows trail the hero's starts, stops and jumps, never a steady run.</summary>
public class HoloSwayTests
{
    private const double Frame = 1 / 60.0;

    [Fact]
    public void AStartSwingsTheWindowBehindTheHero()
    {
        var sway = new HoloSway();
        Run(sway, 0.15, 300, 0);
        Assert.True(sway.Offset.X < -1, $"trails a run to the right: {sway.Offset}");
        var jump = new HoloSway();
        // Up is less y: the window drops behind a jump.
        Run(jump, 0.15, 0, -240);
        Assert.True(jump.Offset.Y > 1, $"drops behind a jump: {jump.Offset}");
    }

    [Fact]
    public void ASteadyRunSettlesBackToItsPlace()
    {
        var sway = new HoloSway();
        Run(sway, 5, 300, 0);
        Assert.True(System.Math.Abs(sway.Offset.X) < 0.01f && System.Math.Abs(sway.Offset.Y) < 0.01f, $"still off by {sway.Offset}");
        // A stop swings it the other way.
        Run(sway, 0.15, 0, 0);
        Assert.True(sway.Offset.X > 1, $"swings on past a stop: {sway.Offset}");
    }

    [Fact]
    public void PointedAtOrCarryingItCalmsToItsShareAndBack()
    {
        var sway = new HoloSway();
        Run(sway, 0.5, 0, 0, calm: true);
        Assert.Equal(HoloLook.SwayCalm, sway.Calm, 4);
        Run(sway, 0.15, 300, 0, calm: true);
        Assert.True(System.Math.Abs(sway.Offset.X) <= HoloLook.SwayReachX * HoloLook.SwayCalm, $"calmed, yet off by {sway.Offset}");
        Run(sway, 2, 300, 0);
        Assert.Equal(1, sway.Calm, 4);
    }

    [Fact]
    public void ALongHitchStaysWithinReach()
    {
        var sway = new HoloSway();
        sway.Step(3, 5000, -5000, false);
        Assert.True(System.Math.Abs(sway.Offset.X) <= HoloLook.SwayReachX && System.Math.Abs(sway.Offset.Y) <= HoloLook.SwayReachY);
        sway.Step(0, 0, 0, false);
        sway.Step(double.NaN, 0, 0, false);
        Assert.False(float.IsNaN(sway.Offset.X) || float.IsNaN(sway.Offset.Y));
    }

    private static Gen<(double Seconds, int Vx, int Vy, bool Calm)[]> Runs =>
        Gen.ArrayOf(
            from seconds in Gen.Choose(1, 100)
            from vx in Gen.Choose(-3000, 3000)
            from vy in Gen.Choose(-3000, 3000)
            from calm in Gen.Elements(true, false)
            select (seconds / 1000.0, vx, vy, calm));

    [Property(MaxTest = 300)]
    public Property TheWindowNeverGoesPastItsReach() => Prop.ForAll(Runs.ToArbitrary(), steps =>
    {
        var sway = new HoloSway();
        foreach (var (seconds, vx, vy, calm) in steps)
        {
            sway.Step(seconds, vx, vy, calm);
            var (x, y) = sway.Offset;
            Assert.True(System.Math.Abs(x) <= HoloLook.SwayReachX && System.Math.Abs(y) <= HoloLook.SwayReachY, $"off by ({x}, {y})");
        }
    });

    private static void Run(HoloSway sway, double seconds, double vx, double vy, bool calm = false)
    {
        for (double t = 0; t < seconds; t += Frame) sway.Step(Frame, vx, vy, calm);
    }
}
