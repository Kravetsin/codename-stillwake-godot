using Stillwake.Game.Dev;
using Stillwake.Sim;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary><c>--snap</c>'s moment: seconds of play from the first tick it sees, not the world's age.</summary>
public class SnapClockTests
{
    [Fact]
    public void A_world_started_late_is_not_snapped_on_its_first_frame()
    {
        // The testbed is aired before play (Testbed.AirTicks), so its first frame is some ten seconds in.
        var clock = new SnapClock(6);
        int first = Testbed.AirTicks + 2;
        Assert.False(clock.Due(first));
        Assert.False(clock.Due(first + 6 * World.TicksPerSecond - 1));
        Assert.True(clock.Due(first + 6 * World.TicksPerSecond));
    }

    [Fact]
    public void A_world_from_tick_zero_is_snapped_at_its_age()
    {
        var clock = new SnapClock(4);
        Assert.False(clock.Due(0));
        Assert.False(clock.Due(4 * World.TicksPerSecond - 1));
        Assert.True(clock.Due(4 * World.TicksPerSecond));
    }
}
