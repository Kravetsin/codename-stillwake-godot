using Stillwake.Game.Menu;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The volume sliders' steps (settings-menu): the top one today's mix, even steps in dB below, 0 silent.</summary>
public class VolumeTests
{
    [Fact]
    public void TheTopStepIsTodaysMix()
    {
        Assert.Equal(0f, Volume.Db(Sound.VolumeSteps));
        Assert.False(Volume.Muted(Sound.VolumeSteps));
    }

    [Fact]
    public void EachStepDownIsTheSameDbQuieter()
    {
        Assert.Equal(-Sound.VolumeStepDb, Volume.Db(Sound.VolumeSteps - 1));
        Assert.Equal(-(Sound.VolumeSteps - 1) * Sound.VolumeStepDb, Volume.Db(1));
        Assert.False(Volume.Muted(1));
    }

    [Fact]
    public void StepZeroMutesTheBus()
    {
        Assert.True(Volume.Muted(0));
        Assert.Equal(Volume.Db(1), Volume.Db(0));
    }

    [Fact]
    public void AKeptStepOutOfRangePlaysAtTheNearest()
    {
        Assert.Equal(Sound.VolumeSteps, Volume.Step(99));
        Assert.Equal(0, Volume.Step(-3));
        Assert.True(Volume.Muted(-3));
    }
}
