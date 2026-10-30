using Stillwake.Game.Features.Hazards;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How a magnetic flare is seen: its warning's run, the aurora, the lamps, the cables and the flash (magnetic-flare).</summary>
public class FlareLookTests
{
    [Fact]
    public void TheWarningRunsFromNoneToTheStrike()
    {
        Assert.Equal(0, FlareLook.Near(0, 0.5, 200));
        Assert.Equal(0, FlareLook.Near(200, 0, 200));
        Assert.Equal(0.5, FlareLook.Near(100, 0, 200));
        Assert.Equal(1, FlareLook.Near(1, 1, 200));
        // A flare started by hand farther off than its warning is not yet near.
        Assert.Equal(0, FlareLook.Near(500, 0, 200));
    }

    [Fact]
    public void TheAuroraComesInFromNothingSmoothlyAndFadesAfterTheStrike()
    {
        Assert.Equal(0, FlareLook.Aurora(0));
        Assert.Equal(1, FlareLook.Aurora(1), 6);
        // Barely there at first: no pop as the warning begins.
        Assert.True(FlareLook.Aurora(0.02) < 0.01);
        double was = 0;
        for (double near = 0.01; near <= 1; near += 0.01)
        {
            Assert.True(FlareLook.Aurora(near) >= was);
            was = FlareLook.Aurora(near);
        }
        Assert.Equal(1, FlareLook.AuroraLeft(0));
        Assert.Equal(0, FlareLook.AuroraLeft(FlareLook.AuroraFadeMs));
        Assert.True(FlareLook.AuroraLeft(100) > 0.99);
    }

    [Fact]
    public void LampsFalterMoreTowardTheStrikeAndShineWholeWithNoneComing()
    {
        Assert.Equal(1f, FlareLook.LampShare(0));
        Assert.True(FlareLook.LampShare(0.01) < 1f);
        Assert.True(FlareLook.LampShare(1) < FlareLook.LampShare(0.5));
        Assert.True(FlareLook.LampShare(1) > 0f);
    }

    [Fact]
    public void CablesGlowOnlyWhileOneIsComingBrighterNearer()
    {
        Assert.Equal(0, FlareLook.CableGlow(0, 1234));
        for (double t = 0; t < 2000; t += 37)
        {
            Assert.InRange(FlareLook.CableGlow(0.2, t), 0.2 * 0.3, 0.2);
            Assert.InRange(FlareLook.CableGlow(1, t), 0.3, 1);
        }
    }

    [Fact]
    public void TheFlashEasesUpToWholeThenOutToNothing()
    {
        Assert.Equal(0, FlareLook.Flash(0));
        Assert.Equal(1, FlareLook.Flash(FlareLook.FlashRiseMs), 6);
        Assert.Equal(0, FlareLook.Flash(FlareLook.FlashMs));
        Assert.Equal(0, FlareLook.Flash(-1));
        Assert.Equal(0, FlareLook.Flash(double.MaxValue));
        double was = 1;
        for (double ms = FlareLook.FlashRiseMs; ms < FlareLook.FlashMs; ms += 20)
        {
            Assert.True(FlareLook.Flash(ms) <= was);
            was = FlareLook.Flash(ms);
        }
        // Its tail is soft: little left to drop over its last tenth.
        Assert.True(FlareLook.Flash(FlareLook.FlashMs * 0.9) < 0.05);
    }
}
