using Stillwake.Content;
using Stillwake.Game.Features.Computer;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Where the radar draws a contact: on the ship's orbit, the farthest at <see cref="RadarOrbit.Span"/> of the screen.</summary>
public class RadarOrbitTests
{
    private const float Width = 800, ShipX = 400, ShipY = 225, Radius = 1400;

    [Fact]
    public void NoOffsetIsTheShipItself()
    {
        var (x, y) = RadarOrbit.At(0, Width, ShipX, ShipY, Radius);
        Assert.Equal(ShipX, x, 3);
        Assert.Equal(ShipY, y, 3);
    }

    [Fact]
    public void TheEdgeOfReachLiesOnTheOrbitASpanOfTheScreenAwayEitherSideAndOnScreen()
    {
        foreach (int side in new[] { -1, 1 })
        {
            var (x, y) = RadarOrbit.At(side * Contacts.Reach, Width, ShipX, ShipY, Radius);
            // Along the arc a span of the width; as the crow flies a little less.
            Assert.InRange(side * (x - ShipX), Width * RadarOrbit.Span * 0.97f, Width * RadarOrbit.Span);
            Assert.InRange(x, 0, Width);
            // Below the ship, the orbit bending down round the planet, at the orbit's radius from the core.
            Assert.True(y > ShipY);
            float dx = x - ShipX, dy = y - (ShipY + Radius);
            Assert.Equal(Radius, System.MathF.Sqrt(dx * dx + dy * dy), 1);
        }
    }

    [Fact]
    public void DistanceIsInKilometresWhicheverSide()
    {
        Assert.Equal(640, RadarOrbit.Km(64));
        Assert.Equal(640, RadarOrbit.Km(-64));
        Assert.Equal(Contacts.Reach * Contacts.KmPerUnit, RadarOrbit.Km(-Contacts.Reach));
    }

    [Fact]
    public void NearerIsNearer()
    {
        var (near, _) = RadarOrbit.At(Contacts.Nearest, Width, ShipX, ShipY, Radius);
        var (far, _) = RadarOrbit.At(Contacts.Reach, Width, ShipX, ShipY, Radius);
        Assert.True(ShipX < near && near < far);
    }
}
