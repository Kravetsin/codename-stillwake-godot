using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;
using F = Stillwake.Game.Features.Crew.DroneFlight;
using T = Stillwake.Game.Features.Crew.DroneTow.Tune;

namespace Stillwake.Game.Tests;

/// <summary>The light drone as a tug: when it takes hold of a flight, where it pulls from and how it lets go.</summary>
public class DroneTowTests
{
    private static readonly double Top = F.Length(F.VelocityOf(Content.Jetpack.Top, 0));

    /// <summary>The helmet's middle and radius from the body's middle, upright.</summary>
    private static readonly Vec Helmet = new(0, Rig.H / 2 - Rig.Neck - 6.2);
    private const double HelmetRadius = 6.4;

    /// <summary>A hero, its drone's flight and its tow, flown frame by frame as the figure does.</summary>
    private sealed class Pair
    {
        public DroneTow Tow { get; } = new();
        public F Flight { get; } = new();
        public Vec Anchor;
        public Vec Perch;
        public bool May = true;
        public int Facing;
        public Func<double, double, bool>? Stops;
        private readonly double _frame;

        public Pair(int fps, int facing = 1)
        {
            (_frame, Facing, Perch) = (1000.0 / fps, facing, new Vec(-15 * facing, 0));
            Flight.Step(Anchor, default, Perch, false, false, _frame);
        }

        public void Fly(Vec hero, double forMs, Action? each = null)
        {
            for (double t = 0; t < forMs; t += _frame)
            {
                Anchor = F.Plus(Anchor, F.Scaled(hero, _frame));
                Tow.Step(Anchor, hero, Facing, Flight.At, May, Stops, _frame);
                if (Tow.Riding) Flight.Ride(Anchor, hero, Tow.Ahead, Tow.Grip, _frame);
                else Flight.Step(Anchor, hero, Tow.Back(Anchor, F.Plus(Anchor, Perch)), false, false, _frame);
                each?.Invoke();
            }
        }

        public Vec ByMiddle => F.Minus(Flight.At, new Vec(Anchor.X, Anchor.Y + DroneTow.MiddleDown));
    }

    public static TheoryData<double, double, int> Ways()
    {
        var ways = new TheoryData<double, double, int>();
        foreach (var (x, y) in new[] { (1.0, 0.0), (-1.0, 0.0), (0.0, -1.0), (0.0, 1.0), (0.707, 0.707), (-0.707, 0.707), (0.707, -0.707), (-0.707, -0.707) })
            foreach (int facing in new[] { 1, -1 })
                ways.Add(x, y, facing);
        return ways;
    }

    [Fact]
    public void ItHooksAfterTwoSecondsOneWayNotSooner()
    {
        var pair = new Pair(60);
        pair.Fly(new Vec(Top, 0), T.WaitMs - 100);
        Assert.False(pair.Tow.Riding);
        pair.Fly(new Vec(Top, 0), 200);
        Assert.True(pair.Tow.Riding);
    }

    [Fact]
    public void ASlowFlightAndOneKeptTurningNeverHook()
    {
        var slow = new Pair(60);
        slow.Fly(new Vec(Top * 0.7, 0), 8000);
        Assert.False(slow.Tow.Riding);
        var turning = new Pair(60);
        for (int i = 0; i < 8; i++)
        {
            turning.Fly(F.Scaled(new Vec(Math.Cos(i), Math.Sin(i)), Top), T.WaitMs - 200);
            Assert.False(turning.Tow.Riding);
        }
    }

    [Theory]
    [MemberData(nameof(Ways))]
    public void ItPullsFromAheadItsThreadClearOfTheHelmet(double x, double y, int facing)
    {
        var pair = new Pair(60, facing);
        var hero = new Vec(x * Top, y * Top);
        var way = new Vec(x, y);
        pair.Fly(hero, T.WaitMs + 1800);
        Assert.True(pair.Tow.Caught);
        pair.Fly(hero, 4000, () =>
        {
            var drone = pair.ByMiddle;
            Assert.True(F.Dot(drone, way) > 18, $"only {F.Dot(drone, way):F1} px ahead");
            Span<Vec> thread = stackalloc Vec[DroneTow.Points];
            pair.Tow.Thread(drone, pair.Tow.Hold, 0, thread);
            foreach (var point in thread)
                Assert.True(F.Length(F.Minus(point, Helmet)) > HelmetRadius, $"the thread in the helmet at {point}");
        });
    }

    [Theory]
    [MemberData(nameof(Ways))]
    public void RunningAheadItGoesRoundTheHelmetNotThroughIt(double x, double y, int facing)
    {
        var pair = new Pair(60, facing);
        var hero = new Vec(x * Top, y * Top);
        pair.Fly(hero, T.WaitMs);
        pair.Fly(hero, 2000, () =>
        {
            Assert.True(F.Length(F.Minus(pair.ByMiddle, Helmet)) > HelmetRadius + 3, $"the drone in the helmet at {pair.ByMiddle}");
            if (y == 0) Assert.True(pair.ByMiddle.Y < Helmet.Y + 1, $"the drone went through the body at {pair.ByMiddle}");
        });
    }

    [Theory]
    [InlineData(30)]
    [InlineData(144)]
    public void TakingHoldHeavingAndLettingGoNeverSnap(int fps)
    {
        var pair = new Pair(fps);
        Vec hero = new(Top, 0), was = pair.Flight.At, speed = default;
        double ms = 1000.0 / fps, hardest = 0, furthest = 0;
        void Seen()
        {
            var flew = F.Scaled(F.Minus(pair.Flight.At, was), 1 / ms);
            hardest = Math.Max(hardest, F.Length(F.Minus(flew, speed)) / ms);
            furthest = Math.Max(furthest, F.Length(pair.ByMiddle));
            (was, speed) = (pair.Flight.At, flew);
        }
        pair.Fly(hero, 1000, Seen);
        hardest = 0;
        pair.Fly(hero, 6000, Seen);
        Assert.True(pair.Tow.Caught);
        double gain = F.Length(F.VelocityOf(Content.Jetpack.Accel, 0)) / Hero.StepMs;
        for (double t = 0; t < 3000; t += ms)
        {
            hero = new Vec(Math.Max(-Top, hero.X - gain * ms), 0);
            pair.Fly(hero, ms / 2, Seen);
        }
        Assert.False(pair.Tow.Caught && pair.Tow.Way.X > 0);
        // A heave pulls harder than a jetpack does, a snap would be many times that.
        Assert.True(hardest < gain * 6, $"its speed jumped {hardest / gain:F1} times a jetpack's push");
        Assert.True(furthest < 200, $"flung {furthest:F0} px off");
    }

    [Fact]
    public void ATurnMakesItLetGoFlyOnAloneAndComeBack()
    {
        var pair = new Pair(60);
        pair.Fly(new Vec(Top, 0), T.WaitMs + 2500);
        Assert.True(pair.Tow.Caught);
        double ahead = pair.ByMiddle.X;
        var turned = new Vec(0, -Top * 0.5);
        double furthest = ahead;
        pair.Fly(turned, 3500, () => furthest = Math.Max(furthest, pair.ByMiddle.X));
        Assert.False(pair.Tow.Riding);
        Assert.True(furthest > ahead + 8, $"carried on only {furthest - ahead:F1} px");
        Assert.True(F.Length(F.Minus(pair.Flight.At, F.Plus(pair.Anchor, pair.Perch))) < 3);
    }

    [Fact]
    public void WhatMattersMoreTakesItOffAtOnceAndLeavesItsGoalAlone()
    {
        var pair = new Pair(60);
        var hero = new Vec(Top, 0);
        pair.Fly(hero, T.WaitMs + 2500);
        pair.May = false;
        pair.Fly(hero, 20);
        Assert.False(pair.Tow.Riding);
        var wanted = new Vec(5, 7);
        Assert.Equal(wanted, pair.Tow.Back(pair.Anchor, wanted));
        pair.Fly(hero, T.Reel * 1000 + 100);
        Assert.False(pair.Tow.Threaded);
    }

    [Fact]
    public void AWallAheadKeepsItFromHookingAndMakesItLetGo()
    {
        var pair = new Pair(60);
        var hero = new Vec(Top, 0);
        pair.Stops = (x, _) => x > pair.Anchor.X + 20;
        pair.Fly(hero, T.WaitMs + 1000);
        Assert.False(pair.Tow.Riding);
        pair.Stops = null;
        pair.Fly(hero, T.WaitMs + 1000);
        Assert.True(pair.Tow.Riding);
        pair.Stops = (x, _) => x > pair.Anchor.X + 20;
        pair.Fly(hero, 20);
        Assert.False(pair.Tow.Riding);
    }

    [Fact]
    public void ATwistMidFlightKeepsItsShoulderOnTheSameSide()
    {
        var pair = new Pair(60);
        var hero = new Vec(Top, 0);
        pair.Fly(hero, T.WaitMs + 2500);
        var hold = pair.Tow.Hold;
        pair.Facing = -1;
        pair.Fly(hero, 1000);
        Assert.True(pair.Tow.Caught);
        Assert.Equal(hold, pair.Tow.Hold);
    }
}
