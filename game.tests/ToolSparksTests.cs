using System;
using System.Collections.Generic;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A grinder's sparks off its rim (grinder-contact): ridden along the spin to an exit, then thrown on along the rim there.</summary>
public class ToolSparksTests
{
    private static readonly string Grinder = Content.Weapons.Grinder.Item;
    private static readonly WheelContact.Circle Wheel = new((0, 0), 14, 1);

    [Fact]
    public void ASparkLeavesFromTheFirstExitItMeetsAlongTheSpin()
    {
        var top = new WheelContact.Exit(Wheel.At(-Math.PI / 2), Wheel.Along(-Math.PI / 2), -Math.PI / 2, 0.5);
        var sparks = Ground([Low, top], ms: 200);
        int low = 0, high = 0;
        for (int n = 0; n < sparks.Count; n++)
        {
            if (sparks.At(n) is not var (_, head, _)) continue;
            double r = Math.Sqrt(head.X * head.X + head.Y * head.Y);
            if (r <= Wheel.Radius + 1e-6) continue;
            // Off the low exit they go back along -x, off the top one along +x; each spark from the exit it met.
            if (head.Y > 0) low++;
            else high++;
            Assert.True(head.Y > 0 ? head.X < 0 : head.X > 0, $"spark {n} at {head}");
        }
        Assert.True(low > 0 && high > 0, $"{low} off the low exit, {high} off the top");
    }

    // The rim's lowest point: it moves back, along -x, there.
    private static readonly WheelContact.Exit Low = new(Wheel.At(Math.PI / 2), Wheel.Along(Math.PI / 2), Math.PI / 2, 1);

    private static ToolSparks Ground(IReadOnlyList<WheelContact.Exit>? exits, double ms = 1000)
    {
        var sparks = new ToolSparks();
        sparks.Begin(0);
        sparks.Touch((1, 7), Grinder, (30, 0), (-1, 0), 0, Wheel, exits);
        sparks.End();
        sparks.Begin(ms);
        sparks.Touch((1, 7), Grinder, (30, 0), (-1, 0), ms, Wheel, exits);
        sparks.End();
        return sparks;
    }

    [Fact]
    public void ASparkRidesTheWholeRimToItsExitThenFliesOnAlongIt()
    {
        var sparks = Ground([Low], ms: 200);
        int riding = 0, flying = 0, over = 0;
        for (int n = 0; n < sparks.Count; n++)
        {
            if (sparks.At(n) is not var (_, head, _)) continue;
            double r = Math.Sqrt(head.X * head.X + head.Y * head.Y), angle = Math.Atan2(head.Y, head.X);
            if (Math.Abs(r - Wheel.Radius) < 1e-6)
            {
                riding++;
                // The rim's top half, far from the low exit: riding sparks run round all of it.
                if (angle < 0) over++;
                continue;
            }
            flying++;
            double dx = head.X - Low.At.X, dy = head.Y - Low.At.Y;
            double off = Math.Abs(Math.IEEERemainder(Math.Atan2(dy, dx) - Math.Atan2(Low.Way.Y, Low.Way.X), 2 * Math.PI));
            Assert.True(off <= WorkLooks.GrinderSpread + 1e-6, $"spark {n} at {head} is {off} off the rim's way");
        }
        Assert.True(riding > 0 && flying > 0 && over > 0, $"{riding} riding ({over} over the top), {flying} flying");
    }

    [Fact]
    public void AWreckThrowsAsManyFromTwoExitsAsFromOneAndAsWithNone()
    {
        var other = new WheelContact.Exit(Wheel.At(-Math.PI / 2), Wheel.Along(-Math.PI / 2), -Math.PI / 2, 0.5);
        Assert.Equal(Ground([Low]).Count, Ground([Low, other]).Count);
        Assert.Equal(Ground([Low]).Count, Ground([]).Count);
    }

    [Fact]
    public void WithNoExitItThrowsFromTheTouchAsBefore()
    {
        var sparks = Ground([], ms: 60);
        for (int n = 0; n < sparks.Count; n++)
            if (sparks.At(n) is var (_, head, _))
                Assert.True(Math.Abs(head.X - 30) + Math.Abs(head.Y) < 15, $"spark {n} at {head}");
        Assert.True(sparks.Count > 0);
    }
}
