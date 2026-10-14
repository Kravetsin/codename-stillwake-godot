using Stillwake.Game.Features.Atmosphere;
using Stillwake.Sim.Atmosphere;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>features/atmosphere/streams.test.ts, ported.</summary>
public class StreamsTests
{
    private static RoomAir Room(RoomStatus status = RoomStatus.Venting, int aired = 10, int made = 0, int? lasts = null) =>
        new(0, status, aired, 0, 0, made, lasts, null);

    // A five-wide strip; cells 0..4 on row 0 are space, the room's openings are on row 1.
    private const int W = 5, H = 3;
    private static bool Space(int cell) => cell < W;
    private static bool Never(int cell) => false;

    [Fact]
    public void StreamOutOfAVentingRoomHoldingAirHeadingIntoTheSpaceBesideEachOpening()
    {
        var up = AirStream.Of([Room()], _ => [W + 2], W, H, Space).Single();
        Assert.Equal((2.5, 1.5, AirStream.PerOpening), (up.X, up.Y, up.PerSecond));
        Assert.Equal(-Math.PI / 2, up.Heading!.Value, 5);
        // Out over the grid's edge, to the left.
        var left = AirStream.Of([Room()], _ => [W], W, H, Never).Single();
        Assert.Equal(Math.PI, left.Heading!.Value, 5);
    }

    [Fact]
    public void LeaveARoomOpenToSpaceWithNoAirInItQuietAndOneAiredAndSealedToo()
    {
        Assert.Empty(AirStream.Of([Room(aired: 0)], _ => [W + 2], W, H, Space));
        // Its vents still pouring oxygen in, from tanks or a generator, it streams with no air held.
        Assert.Single(AirStream.Of([Room(aired: 0, lasts: 600)], _ => [W + 2], W, H, Space));
        Assert.Single(AirStream.Of([Room(aired: 0, made: 200)], _ => [W + 2], W, H, Space));
        Assert.Empty(AirStream.Of([Room(RoomStatus.Aired)], _ => [W + 2], W, H, Space));
    }

    [Fact]
    public void ThinOutOverManyOpeningsARoomGivingOffNoMoreThanItsMost()
    {
        var openings = Enumerable.Range(0, 40).Select(i => W + i % W).ToList();
        var streams = AirStream.Of([Room()], _ => openings, W, H, Space);
        Assert.Equal(AirStream.PerRoom, streams.Sum(s => s.PerSecond), 5);
    }

    [Fact]
    public void HaveNoHeadingThroughAMissingBackWallWithNoSpaceBeside()
    {
        Assert.Null(AirStream.Of([Room()], _ => [W + 2], W, H, Never).Single().Heading);
    }
}
