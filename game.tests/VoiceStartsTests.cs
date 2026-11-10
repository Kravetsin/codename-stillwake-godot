using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Stillwake.Game.Features.Sound;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// Which voice of a placed sound starts it. One played again before Godot's next physics tick (20 a second) — a drill's hits, the
/// cells it breaks — on a voice of its own, not on the one still waiting to start, which the second play would replace
/// (heard-every-other); one played while another still rings, on a voice of its own too, never moving the ringing one off.
/// </summary>
public class VoiceStartsTests
{
    private static bool[] Quiet(int voices) => new bool[voices];

    /// <summary>
    /// How many of <paramref name="plays"/> are heard, each played in the physics tick its value says, by a player of
    /// <paramref name="voices"/> voices, none still sounding: as Godot has it, each voice starts only the last sound played on it in a tick.
    /// </summary>
    private static int Heard(int voices, params ulong[] plays)
    {
        var starts = new VoiceStarts();
        var started = new HashSet<(ulong Tick, int Voice)>();
        foreach (ulong tick in plays) started.Add((tick, starts.Take(tick, Quiet(voices))));
        return started.Count;
    }

    [Fact]
    public void TwoPlaysInOneTickAreBothHeard()
    {
        // A drill at 30 a second against physics at 20: every other tick takes two. One voice alone kept 4 of the 6.
        Assert.Equal(6, Heard(3, 0, 0, 1, 2, 2, 3));
    }

    [Fact]
    public void EachTickStartsAgainOnThePlayerItselfOnceItIsQuiet()
    {
        var starts = new VoiceStarts();
        Assert.Equal(0, starts.Take(5, Quiet(3)));
        Assert.Equal(1, starts.Take(5, Quiet(3)));
        Assert.Equal(0, starts.Take(6, Quiet(3)));
        Assert.Equal(0, starts.Take(7, Quiet(3)));
    }

    [Fact]
    public void PastItsVoicesTheLatestReplacesTheOneWaiting()
    {
        var starts = new VoiceStarts();
        Assert.Equal([0, 1, 0, 0], new[] { starts.Take(1, Quiet(2)), starts.Take(1, Quiet(2)), starts.Take(1, Quiet(2)), starts.Take(1, Quiet(2)) });
        Assert.Equal(2, Heard(2, 1, 1, 1, 1));
    }

    [Fact]
    public void OneVoiceKeepsToItself()
    {
        var starts = new VoiceStarts();
        Assert.Equal(0, starts.Take(0, Quiet(1)));
        Assert.Equal(0, starts.Take(0, Quiet(1)));
        Assert.Equal(0, starts.Take(1, [true]));
    }

    /// <summary>A charge's burst still ringing near, another going off far later: the far one on a voice of its own, so the near one is not moved off and silenced.</summary>
    [Fact]
    public void AVoiceStillSoundingIsLeftWhileAnotherIsFree()
    {
        var starts = new VoiceStarts();
        Assert.Equal(0, starts.Take(10, Quiet(3)));
        Assert.Equal(1, starts.Take(30, [true, false, false]));
        Assert.Equal(2, starts.Take(31, [true, true, false]));
    }

    [Fact]
    public void WithEveryVoiceSoundingTheOneStartedLongestAgoIsCutShort()
    {
        var starts = new VoiceStarts();
        Assert.Equal(0, starts.Take(1, Quiet(2)));
        Assert.Equal(1, starts.Take(2, [true, false]));
        Assert.Equal(0, starts.Take(3, [true, true]));
        Assert.Equal(1, starts.Take(4, [true, true]));
    }

    /// <summary>
    /// Physics ticks at least once a frame at 60 fps: a placed sound waits for the next one to start, so at 20 a second a drill's
    /// hits came quantised to 50 ms and half of them lost (heard-every-other). The client runs no Godot physics of its own.
    /// </summary>
    [Fact]
    public void PlacedSoundsStartWithinAFrame()
    {
        string project = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Here())!, "..", "game", "project.godot"));
        var tick = Regex.Match(project, @"^common/physics_ticks_per_second=(\d+)", RegexOptions.Multiline);
        Assert.True(tick.Success && int.Parse(tick.Groups[1].Value) >= 60, "physics must tick at least 60 times a second");
    }

    private static string Here([CallerFilePath] string path = "") => path;
}
