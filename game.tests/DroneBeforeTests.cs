using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Projecting a drone window, the drone flies before the hero and back (drone-holo): eased, never jerked by its leash.</summary>
public class DroneBeforeTests
{
    [Fact]
    public void SentBeforeTheHeroItFliesThereSmoothlyAndSettles()
    {
        var drone = new DroneFloat();
        for (int i = 0; i < 60; i++) drone.Follow((0, 0), 1, 16, 0);
        var head = drone.At!.Value;
        var before = (X: 14.0, Y: 28.0);
        var at = drone.Follow((0, 0), 1, 16, 0, before);
        // One frame on it has gone part of the way, not jumped to its leash.
        double moved = System.Math.Sqrt((at.X - head.X) * (at.X - head.X) + (at.Y - head.Y) * (at.Y - head.Y));
        Assert.InRange(moved, 1, 15);
        for (int ms = 16; ms < 300; ms += 16) drone.Follow((0, 0), 1, 16, 0, before);
        Assert.True(drone.Off < SheetHoloLook.OverArrived);
    }
}
