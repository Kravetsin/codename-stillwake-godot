using Stillwake.Game.Features.Landing;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How deep in the warp the ship's flight between planets is, and how far along.</summary>
public class WarpTests
{
    private const int Flight = 600;

    [Fact]
    public void StandingStillWithNoCourse()
    {
        Assert.Equal(0, Warp.Depth(null, Flight, 0.5));
        Assert.Equal(0, Warp.Along(null, Flight, 0.5));
    }

    [Fact]
    public void BuildsUpOverItsFirstSecondsAndDiesDownOverItsLast()
    {
        Assert.InRange(Warp.Depth(Flight - 1, Flight, 0), 0, 0.01f);
        Assert.InRange(Warp.Depth(Flight - 40, Flight, 0), 0.4f, 0.6f);
        Assert.Equal(1, Warp.Depth(Flight / 2, Flight, 0));
        Assert.InRange(Warp.Depth(40, Flight, 0), 0.4f, 0.6f);
        Assert.InRange(Warp.Depth(1, Flight, 0.9), 0, 0.01f);
    }

    [Fact]
    public void RisesSmoothlyFrameByFrame()
    {
        float last = 0;
        for (double t = 1; t < Flight / 2; t += 0.25)
        {
            float d = Warp.Depth(Flight - (int)t, Flight, t % 1);
            Assert.True(d >= last && d - last < 0.02f, $"at {t}: {last} to {d}");
            last = d;
        }
    }

    [Fact]
    public void AlongGoesFromTheSetOffToTheArrival()
    {
        Assert.Equal(0.5, Warp.Along(Flight / 2, Flight, 0), 3);
        Assert.Equal(1, Warp.Along(0, Flight, 1));
    }
}
