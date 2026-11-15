using System;
using System.Collections.Generic;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A drill's sparks ride its haze helix, a closed loop, out of a wreck and leave where the loop does (drill-contact).</summary>
public class DrillContactTests
{
    // The chuck at the origin, aimed right, its reach 20 px out, the helix as it is at turn 0.
    private const double Length = 20;
    private static readonly ToolContact.Helix Right = new((0, 0), (1, 0), (0, 1), Length, 0);

    private static List<ToolContact.Exit> Exits(Func<double, double, bool> inside) =>
        ToolContact.Exits(Right, WorkLooks.HelixSteps, inside, WorkLooks.WheelExits);

    [Fact]
    public void TheStrandsMeetOnTheAxisAtTheChuckAndTheReach()
    {
        foreach (double phase in new[] { 0, 1.3, Math.PI })
        {
            AssertNear((0, 0), Right.At(0, phase));
            AssertNear((Length, 0), Right.At(0.5, phase));
            AssertNear(Right.At(0, phase), Right.At(1, phase));
        }
    }

    [Fact]
    public void TheStrandsStandOppositeAcrossTheAxis()
    {
        var (ox, oy) = Right.At(0.3, 0.7);
        var (bx, by) = Right.At(0.7, 0.7);
        Assert.Equal(ox, bx, 9);
        Assert.Equal(-oy, by, 9);
        Assert.True(Math.Abs(oy) <= WorkLooks.HelixRadius);
    }

    [Fact]
    public void AWallAcrossTheHelixIsLeftOnTheWayBack()
    {
        // The loop runs into the wall along the first strand, round the reach and back out along the other: one stretch.
        var exit = Assert.Single(Exits((x, _) => x > Length / 2));
        Assert.InRange(exit.Angle, 0.5, 1);
        Assert.InRange(exit.At.X, Length / 2, Length / 2 + 2 * Length / WorkLooks.HelixSteps + 1e-9);
        Assert.True(exit.Way.X < 0, $"leaves along {exit.Way}");
        Assert.InRange(exit.Run, 0.45, 0.5);
    }

    [Fact]
    public void NoExitWhileTheHelixIsAllInOrAllOut()
    {
        Assert.Empty(Exits((_, _) => true));
        Assert.Empty(Exits((_, _) => false));
    }

    [Fact]
    public void AtMostTheLongestStretchesAreExits()
    {
        // Thin slabs across the axis: each crossed twice, once by each strand.
        var exits = Exits((x, _) => x is > 3 and < 4 or > 9 and < 12 or > 15 and < 15.5);
        Assert.Equal(WorkLooks.WheelExits, exits.Count);
        for (int k = 1; k < exits.Count; k++) Assert.True(exits[k - 1].Run >= exits[k].Run);
    }

    [Fact]
    public void ATouchWithNoExitThrowsFromTheLoopsNearestPoint()
    {
        var behind = ToolContact.Nearest(Right, (-5, 0), WorkLooks.HelixSteps);
        Assert.Equal(0, behind.Angle, 9);
        var past = ToolContact.Nearest(Right, (30, 0), WorkLooks.HelixSteps);
        Assert.Equal(0.5, past.Angle, 9);
    }

    private static ToolSparks Drilled(IReadOnlyList<ToolContact.Exit>? exits, double ms, (double X, double Y)? at = null)
    {
        var sparks = new ToolSparks();
        string drill = Content.Weapons.Drill.Item;
        var touch = at ?? (12, 0);
        sparks.Begin(0);
        sparks.Touch((1, 7), drill, touch, (-1, 0), 0, helix: Right, exits: exits);
        sparks.End();
        sparks.Begin(ms);
        sparks.Touch((1, 7), drill, touch, (-1, 0), ms, helix: Right, exits: exits);
        sparks.End();
        return sparks;
    }

    [Fact]
    public void SparksRideTheLoopInsideTheWallThenFlyOutOfIt()
    {
        var exits = Exits((x, _) => x > Length / 2);
        var sparks = Drilled(exits, 200);
        int deep = 0, flying = 0;
        for (int n = 0; n < sparks.Count; n++)
        {
            if (sparks.At(n) is not var (_, head, age)) continue;
            if (age > 0)
            {
                flying++;
                continue;
            }
            // Never off the helix while riding it.
            Assert.True(head.X is >= -1e-6 and <= Length + 1e-6 && Math.Abs(head.Y) <= WorkLooks.HelixRadius + 1e-6, $"spark {n} at {head}");
            if (head.X > Length / 2 + 1) deep++;
        }
        Assert.True(deep > 0 && flying > 0, $"{deep} riding deep in the wall, {flying} flying");
    }

    [Fact]
    public void ARideThrowsAsManyAsATouchDoes()
    {
        Assert.Equal(Drilled(null, 1000).Count, Drilled(Exits((x, _) => x > Length / 2), 1000).Count);
    }

    [Fact]
    public void WithNoExitItStillThrowsOffTheHelixNearestTheTouch()
    {
        var (none, nearest) = (Drilled(null, 200, (30, 0)), Drilled([ToolContact.Nearest(Right, (30, 0), WorkLooks.HelixSteps)], 200, (30, 0)));
        Assert.Equal(nearest.Count, none.Count);
        for (int n = 0; n < none.Count; n++) Assert.Equal(nearest.At(n), none.At(n));
    }

    private static void AssertNear((double X, double Y) want, (double X, double Y) got)
    {
        Assert.Equal(want.X, got.X, 9);
        Assert.Equal(want.Y, got.Y, 9);
    }
}
