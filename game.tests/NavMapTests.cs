using System;
using Stillwake.Content;
using Stillwake.Game.Features.Computer;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Where the radar's map puts the sun, the planets, the ship, the contacts and a callout.</summary>
public class NavMapTests
{
    private const float W = 860, H = 480;

    [Fact]
    public void TheSystemFitsTheScreenWithTheSunInTheMiddle()
    {
        Assert.Equal((W / 2, H / 2), NavMap.ToScreen((0, 0), NavMap.Planet(3), 0, W, H, (0, 0)));
        for (int i = 0; i < Landing.Planets.Count; i++)
        {
            var (x, y) = NavMap.ToScreen(NavMap.Planet(i), (0, 0), 0, W, H, (0, 0));
            Assert.InRange(x, 0, W);
            Assert.InRange(y, 0, H);
        }
    }

    [Fact]
    public void FullyZoomedTheFocusIsInTheMiddleAndTheShipsOrbitFillsTheScreen()
    {
        var focus = NavMap.Planet(2);
        var (x, y) = NavMap.ToScreen(focus, focus, 1, W, H, (0, 0));
        Assert.Equal(W / 2, x, 2);
        Assert.Equal(H / 2, y, 2);
        var ship = NavMap.ToScreen(NavMap.Ship(2), focus, 1, W, H, (0, 0));
        Assert.InRange(H / 2 - ship.Y, H / 5, H / 2 - 30);
        Assert.Equal(1, NavMap.Scale(0));
        Assert.Equal(NavMap.Deep, NavMap.Scale(1), 3);
    }

    [Fact]
    public void ContactsLieOnTheShipsOrbitLeftAndRightOfIt()
    {
        var planet = NavMap.Planet(0);
        var ship = NavMap.Ship(0);
        var here = NavMap.Contact(0, 0);
        Assert.Equal(ship.X, here.X, 3);
        Assert.Equal(ship.Y, here.Y, 3);
        Assert.True(NavMap.Contact(0, -Contacts.Reach).X < planet.X);
        Assert.True(NavMap.Contact(0, Contacts.Reach).X > planet.X);
        foreach (int offset in new[] { -Contacts.Reach, -Contacts.Reach / 3, Contacts.Reach })
        {
            var c = NavMap.Contact(0, offset);
            Assert.Equal(NavMap.PlanetRadius + NavMap.OrbitGap, MathF.Sqrt(MathF.Pow(c.X - planet.X, 2) + MathF.Pow(c.Y - planet.Y, 2)), 3);
        }
    }

    [Fact]
    public void ACourseRunsFromPlanetToPlanetBowedAwayFromTheSun()
    {
        var (a, b) = (NavMap.Planet(0), NavMap.Planet(3));
        Assert.Equal(a, NavMap.Course(a, b, (0, 0), 0));
        var end = NavMap.Course(a, b, (0, 0), 1);
        Assert.Equal(b.X, end.X, 3);
        var (mx, my) = NavMap.Course(a, b, (0, 0), 0.5f);
        float straight = MathF.Sqrt(MathF.Pow((a.X + b.X) / 2, 2) + MathF.Pow((a.Y + b.Y) / 2, 2));
        Assert.True(MathF.Sqrt(mx * mx + my * my) > straight);
    }

    [Fact]
    public void ACalloutStandsBesideItsThingAndInsideTheScreen()
    {
        var right = NavMap.Callout(200, 200, 20, 270, 200, W, H);
        Assert.True(right.X > 220);
        var left = NavMap.Callout(800, 200, 20, 270, 200, W, H);
        Assert.True(left.X + 270 < 780);
        var outward = NavMap.Callout(380, 200, 20, 270, 200, W, H);
        Assert.True(outward.X + 270 < 340);
        var low = NavMap.Callout(400, 470, 20, 270, 200, W, H);
        Assert.True(low.Y + 200 <= H - 16);
    }
}
