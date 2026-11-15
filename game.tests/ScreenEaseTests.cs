using System;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How the frost and the haze follow what is felt (2026-11-14-frost-film): smoothly, at any frame rate, landing on 0.</summary>
public class ScreenEaseTests
{
    [Fact]
    public void ClosesTwoThirdsOfAJumpInItsEaseTimeNeverOvershooting()
    {
        double shown = 0;
        for (int i = 0; i < 36; i++)
        {
            double next = ScreenEase.Toward(shown, 1, 1 / 60.0);
            Assert.True(next > shown && next <= 1);
            shown = next;
        }
        Assert.Equal(1 - Math.Exp(-1), shown, 3);
    }

    [Fact]
    public void IsTheSameAtThirtyFramesASecondAsAtSixty()
    {
        double slow = 0.9, fast = 0.9;
        for (int i = 0; i < 30; i++) slow = ScreenEase.Toward(slow, 0.2, 1 / 30.0);
        for (int i = 0; i < 60; i++) fast = ScreenEase.Toward(fast, 0.2, 1 / 60.0);
        Assert.Equal(fast, slow, 9);
    }

    [Fact]
    public void LandsOnNothingSoTheLayerHides()
    {
        double shown = 1;
        int frames = 0;
        while (shown > 0 && frames < 1000) (shown, frames) = (ScreenEase.Toward(shown, 0, 1 / 60.0), frames + 1);
        Assert.Equal(0, shown);
        Assert.InRange(frames, 200, 400);
    }
}
