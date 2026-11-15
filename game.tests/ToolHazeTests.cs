using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>One rule for every tool's sparks (spark-rule): what is hit throws from where the tool's haze lies in it, along the haze.</summary>
public class ToolHazeTests
{
    private static List<HazePoint> Of(Action<List<HazePoint>> make)
    {
        var haze = new List<HazePoint>();
        make(haze);
        return haze;
    }

    [Fact]
    public void ABladesHazeIsItsEdgeAndTheMiddleOfItsSweepMovingTheWayItWent()
    {
        (double X, double Y)[] was = [(0, 0), (0, 4), (0, 8)], now = [(4, 0), (4, 4), (4, 8)];
        var haze = Of(h => ToolHaze.OfEdge(was, now, h));
        Assert.Equal(6, haze.Count);
        var middle = Assert.Single(haze, p => (p.X, p.Y, p.Dx, p.Dy) == (2, 4, 1, 0));
        Assert.Equal((4.0, 4.0), (haze[middle.Next].X, haze[middle.Next].Y));
        Assert.Contains(new HazePoint(4, 8, 1, 0), haze);
        Assert.Empty(Of(h => ToolHaze.OfEdge(now, now, h)));
    }

    [Fact]
    public void AWheelsHazeMovesAlongItsSpin()
    {
        var haze = Of(h => ToolHaze.OfCircle(new((0, 0), 10, 1), 4, h));
        Assert.Equal(4, haze.Count);
        // At angle 0, the rightmost point, a clockwise spin on screen (+y down) moves it down.
        Assert.Equal(10, haze[0].X, 9);
        Assert.Equal(1, haze[0].Dy, 9);
    }

    [Fact]
    public void ADrillsHazeIsItsHelixsLoopWithItsEndsOnTheAxis()
    {
        var helix = new ToolHaze.Helix((0, 0), (1, 0), (0, 1), 20, 0.7);
        var haze = Of(h => ToolHaze.OfHelix(helix, 96, h));
        Assert.Equal(96, haze.Count);
        Assert.Equal(0, haze[0].Y, 9);
        Assert.Equal(20, haze[48].X, 9);
        Assert.Equal(0, haze[48].Y, 9);
        Assert.All(haze, p => Assert.True(Math.Abs(p.Y) <= WorkLooks.HelixRadius && p.X is >= 0 and <= 20));
    }

    [Fact]
    public void ACuttersHazeIsItsJetToItsEnd()
    {
        var haze = Of(h => ToolHaze.OfLine((0, 0), (10, 0), 1, h));
        Assert.Equal(10, haze[^1].X, 9);
        Assert.All(haze, p => Assert.Equal((1.0, 0.0), (p.Dx, p.Dy)));
    }

    [Fact]
    public void WhatIsHitThrowsFromWhereTheHazeLiesInIt()
    {
        var haze = Of(h => ToolHaze.OfLine((0, 0), (20, 0), 1, h));
        var contacts = ToolHaze.Contacts(haze, (x, _) => x is > 12 and < 15, (13, 0));
        Assert.Equal([13.0, 14.0], contacts.Select(p => p.X));
    }

    // Ground below y = 0.
    private static bool Ground(double _, double y) => y > 0;

    [Fact]
    public void AWayIntoWhatWasHitGlancesOffItsSurface()
    {
        // A blade struck down and to the right into the ground: thrown up and to the right off it.
        double d = Math.Sqrt(0.5);
        var p = ToolHaze.Glance(new HazePoint(5, 1, d, d), Ground);
        Assert.Equal(d, p.Dx, 6);
        Assert.Equal(-d, p.Dy, 6);
    }

    [Fact]
    public void AWayOutOfItIsKept()
    {
        var up = new HazePoint(5, 1, 0.6, -0.8);
        Assert.Equal(up, ToolHaze.Glance(up, Ground));
    }

    [Fact]
    public void DeepInsideItGlancesOffTheNearestSurfaceAndAllRoundMatterTurnsItBack()
    {
        // Eight pixels down, the surface found at the widest probe: still thrown up.
        Assert.True(ToolHaze.Glance(new HazePoint(5, 8, 0, 1), Ground).Dy < 0);
        var back = ToolHaze.Glance(new HazePoint(5, 1, 1, 0), (_, _) => true);
        Assert.Equal((-1.0, 0.0), (back.Dx, back.Dy));
    }

    [Fact]
    public void ContactsGlanceOffTheMatterTheyAreIn()
    {
        var haze = new List<HazePoint> { new(0, -2, 0, 1), new(0, 2, 0, 1) };
        var contact = Assert.Single(ToolHaze.Contacts(haze, Ground, (0, 2)));
        Assert.Equal(-1, contact.Dy, 6);
    }

    [Fact]
    public void AWheelInAWallThrowsRoundItsRimToWhereItComesOut()
    {
        // Spun clockwise, the rim's arc in the wall runs down and out at about 60 degrees, moving down and to the left there.
        var haze = Of(h => ToolHaze.OfCircle(new((0, 0), 6, 1), 48, h));
        var contacts = ToolHaze.Contacts(haze, (x, _) => x > 3, (5, 0));
        Assert.True(contacts.Count > 5);
        Assert.All(contacts, p =>
        {
            var (x, y) = Assert.IsAssignableFrom<IReadOnlyList<(double X, double Y)>>(p.Ride)[^1];
            Assert.True(x <= 3 && y > 4, $"out at ({x}, {y})");
            Assert.True(p.Dx < 0 && p.Dy > 0);
        });
    }

    [Fact]
    public void ADrillsHelixInAWallThrowsAlongItsStrandsBackOutToTheChuck()
    {
        var haze = Of(h => ToolHaze.OfHelix(new((0, 0), (1, 0), (0, 1), 20, 0.7), 96, h));
        var contacts = ToolHaze.Contacts(haze, (x, _) => x > 10, (12, 0));
        Assert.NotEmpty(contacts);
        Assert.All(contacts, p =>
        {
            var ride = Assert.IsAssignableFrom<IReadOnlyList<(double X, double Y)>>(p.Ride);
            Assert.True(ride[^1].X <= 10 && p.Dx < 0);
        });
        // Round the axis, not along it: from the reach, the strand turns across it on the way out.
        var longest = contacts.Select(p => p.Ride!).MaxBy(r => r.Count)!;
        Assert.True(longest.Max(q => q.Y) - longest.Min(q => q.Y) > 1);
    }

    [Fact]
    public void AHazeThatDoesNotReachWhatIsHitThrowsFromItsPointNearest()
    {
        var haze = Of(h => ToolHaze.OfLine((0, 0), (10, 0), 1, h));
        var nearest = Assert.Single(ToolHaze.Contacts(haze, (_, _) => false, (30, 5)));
        Assert.Equal(10, nearest.X, 9);
        Assert.Empty(ToolHaze.Contacts([], (_, _) => true, (0, 0)));
    }
}
