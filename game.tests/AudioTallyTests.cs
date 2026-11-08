using Stillwake.Game.Features.Sound;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The count of sound players the log keeps (heard-every-other).</summary>
public class AudioTallyTests
{
    [Fact]
    public void CountsPlayersAndNamesThosePlayingCommonestFirst()
    {
        var tally = new AudioTally();
        tally.Add("HeroView/chop", true);
        tally.Add("HeroView/chop", true);
        tally.Add("DigView/break", true);
        tally.Add("GridView/door", false);
        Assert.Equal("players 4, playing 3: HeroView/chop×2, DigView/break", tally.Line());
    }

    [Fact]
    public void NoneSilentSaysOnlyTheCount()
    {
        var tally = new AudioTally();
        tally.Add("GridView/door", false);
        Assert.Equal("players 1, playing 0", tally.Line());
    }

    [Fact]
    public void NamesOnlyTheCommonest()
    {
        var tally = new AudioTally();
        for (int i = 0; i < AudioTally.Named + 3; i++) tally.Add($"k{i:00}", true);
        Assert.Equal(AudioTally.Named, tally.Line().Split(": ")[1].Split(", ").Length);
    }
}
