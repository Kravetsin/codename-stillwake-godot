using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>What turns with the arm eases as its hand-over does, never jumping or running on its own clock (the spear's rest).</summary>
public class HandOverTests
{
    [Fact]
    public void EasesToANewTargetAsTheArmsHandOverDoesAndTurnsBackFromWhereItIs()
    {
        var turn = new HandOver();
        Assert.Equal(0, turn.Next(0, 16, SwingArm.BlendMs));
        // Laid across the fist as the arm drops: the arm's ease, half way at half its time, done with it.
        Assert.Equal(0, turn.Next(1, 16, SwingArm.BlendMs));
        Assert.Equal(1 - SwingArm.Left(SwingArm.BlendMs / 2, SwingArm.BlendMs), turn.Next(1, SwingArm.BlendMs / 2, SwingArm.BlendMs), 12);
        Assert.Equal(0.5, turn.Share, 12);
        // Raised again half way down: back from there over the raise, no jump.
        double was = turn.Share;
        Assert.Equal(was, turn.Next(0, 16, ArmUp.RaiseMs), 12);
        double last = was;
        for (int i = 0; i < 9; i++)
        {
            double now = turn.Next(0, ArmUp.RaiseMs / 9, ArmUp.RaiseMs);
            Assert.True(now <= last && last - now < 0.25, $"step {i}: {last} to {now}");
            last = now;
        }
        Assert.Equal(0, turn.Share, 12);
        Assert.Equal(0, turn.Next(1, 0, SwingArm.BlendMs), 12);
        Assert.Equal(1, turn.Next(1, SwingArm.BlendMs, SwingArm.BlendMs), 12);
    }
}
