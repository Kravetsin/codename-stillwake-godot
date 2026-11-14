using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A grinder's sparks leave its working circle where the rim comes out of a wreck, along its spin (grinder-contact).</summary>
public class WheelContactTests
{
    private const int Steps = 48;
    private static readonly WheelContact.Circle Right = new((0, 0), 10, 1), Left = new((0, 0), 10, -1);

    private static List<WheelContact.Exit> Exits(WheelContact.Circle wheel, Func<double, double, bool> inside, int most = 3) =>
        WheelContact.Of(wheel, Steps, inside, most);

    [Fact]
    public void FacingRightIntoAPlateAheadTheRimLeavesItLowAndThrowsDownAndBack()
    {
        // The plate's run crosses the walk's start (angle 0): still one stretch, one exit.
        var e = Assert.Single(Exits(Right, (x, _) => x > 6));
        Assert.True(e.At.X > 5 && e.At.Y > 7, $"{e.At}");
        Assert.True(e.Way.X < 0 && e.Way.Y > 0, $"{e.Way}");
        Assert.True(e.Run > 1.5, $"{e.Run}");
    }

    [Fact]
    public void FacingLeftItIsTheMirrorImage()
    {
        var right = Assert.Single(Exits(Right, (x, _) => x > 6));
        var left = Assert.Single(Exits(Left, (x, _) => x < -6));
        Assert.Equal(-right.At.X, left.At.X, 9);
        Assert.Equal(right.At.Y, left.At.Y, 9);
        Assert.Equal(-right.Way.X, left.Way.X, 9);
        Assert.Equal(right.Way.Y, left.Way.Y, 9);
    }

    [Fact]
    public void AWreckBelowIsLeftOnTheBackSideUpAndBack()
    {
        var e = Assert.Single(Exits(Right, (_, y) => y > 6));
        Assert.True(e.At.X < -5 && e.At.Y > 5, $"{e.At}");
        Assert.True(e.Way.X < 0 && e.Way.Y < 0, $"{e.Way}");
    }

    [Fact]
    public void AStripAcrossTheWheelHasTwoExitsAndACombNoMoreThanItsCap()
    {
        Assert.Equal(2, Exits(Right, (_, y) => Math.Abs(y) < 1.5).Count);
        // Four teeth round the rim: only three stretches throw.
        var comb = Exits(Right, (x, y) => Math.Sin(4 * Math.Atan2(y, x) + 0.1) > 0, most: 3);
        Assert.Equal(3, comb.Count);
    }

    [Fact]
    public void ARimAllInOrAllOutHasNoExit()
    {
        Assert.Empty(Exits(Right, (_, _) => true));
        Assert.Empty(Exits(Right, (_, _) => false));
        Assert.Empty(WheelContact.Of(Right with { Radius = 0 }, Steps, (x, _) => x > 0, 3));
    }

    [Fact]
    public void AWheelReachesAsFarAsItsFanAndNoneWhenItsHubIsPastIt()
    {
        Assert.Equal(14, WorkLooks.WheelRadius(40, 26));
        Assert.Null(WorkLooks.WheelRadius(40, 39));
    }

    /// <summary>Any pattern of matter round the rim: every exit is a point inside whose next point along the spin is outside.</summary>
    [Property(MaxTest = 300)]
    public Property EveryExitIsInsideWithTheNextPointAlongTheSpinOutside() =>
        Prop.ForAll(Gen.ArrayOf(Gen.Elements(true, false), Steps).ToArbitrary(), Gen.Elements(1, -1).ToArbitrary(), (pattern, spin) =>
        {
            var wheel = new WheelContact.Circle((3, -2), 10, spin);
            int Index(double x, double y)
            {
                double a = Math.Atan2(y + 2, x - 3) * spin;
                return (int)Math.Round((a < 0 ? a + 2 * Math.PI : a) / (2 * Math.PI) * Steps) % Steps;
            }
            bool Inside(double x, double y) => pattern[Index(x, y)];
            var exits = WheelContact.Of(wheel, Steps, Inside, int.MaxValue);
            int runs = Enumerable.Range(0, Steps).Count(k => pattern[k] && !pattern[(k + 1) % Steps]);
            bool all = pattern.All(p => p) || pattern.All(p => !p);
            return exits.Count == (all ? 0 : runs) && exits.All(e =>
            {
                int k = Index(e.At.X, e.At.Y);
                return pattern[k] && !pattern[(k + 1) % Steps];
            });
        });
}
