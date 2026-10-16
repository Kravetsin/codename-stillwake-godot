using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How far a sound in the world carries (ADR 0033): full nearby, fading to silence, none past it.</summary>
public class SoundReachTests
{
    [Fact]
    public void IsFullUpToItsFullCellsThenFadesToSilence()
    {
        var reach = new Sound.Reach(3, 10);
        Assert.Equal(1, reach.Share(0));
        Assert.Equal(1, reach.Share(3));
        Assert.InRange(reach.Share(6.5f), 0.24f, 0.26f);
        Assert.True(reach.Share(5) > reach.Share(8));
        Assert.Equal(0, reach.Share(10));
        Assert.Equal(0, reach.Share(40));
    }

    [Fact]
    public void GivesEverySoundPlacedInTheWorldAReachThatEndsPastItsFullVolume()
    {
        foreach (var cue in Sound.All)
            if (cue.Heard is { } reach) Assert.True(reach.Silent > reach.Full, $"{cue.Files[0]} goes silent before it fades");
    }
}
