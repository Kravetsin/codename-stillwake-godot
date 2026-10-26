using Stillwake.Game.Features.Landing;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The white over a drop pod's rider's screen: rising at its flight's end, falling once it has arrived, clear when it ends otherwise.</summary>
public class WhiteoutTests
{
    private const double Frame = 1.0 / 60;

    [Fact]
    public void RisesOverTheFlightsLastSecondFullOnItsLastTick()
    {
        var white = new Whiteout();
        Assert.Equal(0, white.Frame(0, 100, Frame));
        Assert.Equal(0, white.Frame(0, Whiteout.FadeTicks + 1, Frame));
        Assert.InRange(white.Frame(0, Whiteout.FadeTicks / 2, Frame), 0.5f, 0.6f);
        Assert.Equal(1, white.Frame(0, 1, Frame));
        Assert.Equal(1, white.Frame(0, 0, Frame));
    }

    [Fact]
    public void FallsOverHalfASecondOnceTheHeroIsOnTheSurfaceItFlewTo()
    {
        var white = new Whiteout();
        white.Frame(0, 1, Frame);
        // The frame after the switch: the flight is gone, the surface another.
        Assert.Equal(1, white.Frame(1, null, Frame));
        Assert.InRange(white.Frame(1, null, Whiteout.FadeOutS / 2), 0.45f, 0.55f);
        Assert.Equal(0, white.Frame(1, null, Whiteout.FadeOutS));
        Assert.Equal(0, white.Frame(1, null, Frame));
    }

    [Fact]
    public void ClearsAtOnceWhenAFlightEndsWithNoSwitchAndNeverWhitensABeamAcross()
    {
        var white = new Whiteout();
        white.Frame(0, 5, Frame);
        // Docked home, its hero gone or out: the flight is gone and the surface the same.
        Assert.Equal(0, white.Frame(0, null, Frame));
        // A dev transfer while clear: no white.
        Assert.Equal(0, white.Frame(1, null, Frame));
    }
}
