using System;
using Stillwake.Game.Features.Crew;
using Xunit;
using CrewSpec = Stillwake.Content.Crew;

namespace Stillwake.Game.Tests;

/// <summary>The last breath as the tunnel (2026-11-05-the-last-breath): closing as the sim runs it down, opening as air comes back.</summary>
public class TunnelTests
{
    private const double Frame = 1.0 / 60;
    private static readonly double LastBreathS = (double)CrewSpec.LastBreath / Stillwake.Sim.World.TicksPerSecond;

    /// <summary>Runs <paramref name="tunnel"/> <paramref name="seconds"/> on in frames.</summary>
    private static double Run(Tunnel tunnel, double seconds)
    {
        double shown = tunnel.Shown;
        for (double t = 0; t < seconds - 1e-9; t += Frame) shown = tunnel.Advance(Frame);
        return shown;
    }

    [Fact]
    public void StaysOpenWithAirAndClosesOnTheSimsClockBetweenReads()
    {
        var tunnel = new Tunnel();
        tunnel.Read(0, 0);
        Assert.Equal(0, Run(tunnel, 1));
        // Read once as the last breath starts: half of it gone half its time later, no read between.
        tunnel.Read(CrewSpec.LastBreath, 0);
        Assert.InRange(Run(tunnel, LastBreathS / 2), 0.48, 0.52);
    }

    [Fact]
    public void CatchesUpAHitsCutWithinCatchSAndLiftsWithinLiftSOnceAirIsBack()
    {
        var tunnel = new Tunnel();
        tunnel.Read(CrewSpec.LastBreath / 4, 0);
        Assert.InRange(Run(tunnel, Tunnel.CatchS), 0.75, 0.8);
        tunnel.Read(0, 0);
        Assert.Equal(0, Run(tunnel, Tunnel.LiftS + Frame));
    }

    [Fact]
    public void IsInkWhilePassedOutAndOpensAsTheHeroWakes()
    {
        var tunnel = new Tunnel();
        tunnel.Read(0, CrewSpec.PassOut);
        Assert.Equal(1, Tunnel.Of(Run(tunnel, Tunnel.CatchS + Frame), 0).Ink);
        tunnel.Read(0, 0);
        Assert.Equal(0, Run(tunnel, Tunnel.LiftS + Frame));
    }

    [Fact]
    public void BlursBrownsAndDoublesMoreAsItGoesSinkingIntoInkOnlyAtTheEnd()
    {
        var clear = Tunnel.Of(0, 3);
        Assert.Equal((0.0, 0.0, 0.0, 0.0, 0.0, 0.0), (clear.Blur, clear.Dark, clear.Sway, clear.GhostX, clear.Swim, clear.Ink));
        // At clock 0 the blur's breathing is still; a quarter of the ghost's swing on, the ghost is at its widest.
        double widest = Math.PI / 2 / 1.9, blur = -1, dark = -1, ghost = -1;
        for (double p = 0.1; p < 0.75; p += 0.1)
        {
            var look = Tunnel.Of(p, 0);
            double reach = Tunnel.Of(p, widest).GhostX;
            Assert.True(look.Blur > blur && look.Dark > dark && reach > ghost);
            (blur, dark, ghost) = (look.Blur, look.Dark, reach);
            Assert.Equal(0, look.Ink);
        }
        Assert.Equal((0, true), (Tunnel.Of(Tunnel.Sinks, 0).Ink, Tunnel.Of(Tunnel.Sinks + 0.05, 0).Ink > 0));
        Assert.Equal(1, Tunnel.Of(1, 0).Ink);
    }
}
