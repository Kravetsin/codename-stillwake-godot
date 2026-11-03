using System;
using System.IO;
using System.Linq;
using Stillwake.Game.Features.Sound;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// The power driver's look and sound (power-driver): its own pictures, to the screwdriver's fist; its chuck turned by its motor
/// while it runs; and its motor heard in three cuts, the bite, the loop and the run-down, the ratchet quiet by it.
/// </summary>
public class PowerDriverLookTests
{
    private const double Frame = 1.0 / 60;

    [Fact]
    public void ItsMotorTurnsItAtWorkTheScrewdriverOnlyEverTwists()
    {
        Assert.True(HeldLook.Drives(Spec.Screwdrivers.PowerDriver.Item));
        Assert.False(HeldLook.Drives(Spec.Screwdrivers.Basic.Item));
        Assert.False(HeldLook.Drives(Spec.Weapons.Drill.Item));
        Assert.False(HeldLook.Drives(null));
    }

    [Fact]
    public void EveryBuildersStripsAreThreeFramesOfItsPictureAndItsGripAndEdgeLieOnIt()
    {
        foreach (var look in new[] { HeldLook.Screwdriver, HeldLook.PowerDriver })
        {
            var (w, h) = Png.SizeOf(look.Picture);
            foreach (string? strip in new[] { look.Twist, look.Driven }.Where(s => s != null))
                Assert.Equal((3 * w, h), Png.SizeOf(strip!));
            Assert.InRange(look.Grip.X, 0, w);
            Assert.InRange(look.Grip.Y, 0, h);
            Assert.InRange(look.EdgeTo.X, 0, w);
        }
    }

    [Fact]
    public void ItIsDrawnToTheSameFistAsTheScrewdriverAsThePickWasBesideIt()
    {
        // The pick's 16 by 13 beside the screwdriver's 17 by 5, both shrunk alike to the fist (hero.js's Hand).
        Assert.Equal((10, 3), Png.SizeOf(HeldLook.Screwdriver.Picture));
        Assert.Equal((10, 8), Png.SizeOf(HeldLook.PowerDriver.Picture));
    }

    /// <summary>What a <see cref="BiteLoop"/> says over <paramref name="frames"/> frames of 1/60 s, at work or not.</summary>
    private static BiteCue[] Run(ref BiteLoop hum, int frames, bool works)
    {
        var said = new BiteCue[frames];
        for (int i = 0; i < frames; i++) said[i] = hum.Step(works, Frame);
        return said;
    }

    [Fact]
    public void ItBitesOnceLoopsOnceTheBiteHasPlayedAndRunsDownWhenItsWorkLapses()
    {
        var hum = new BiteLoop();
        Assert.Equal([BiteCue.None, BiteCue.None], Run(ref hum, 2, works: false));
        var working = Run(ref hum, 60, works: true);
        Assert.Equal(BiteCue.Bite, working[0]);
        // The loop takes over as the 0.8 s bite ends, once.
        int loop = Array.IndexOf(working, BiteCue.Loop);
        Assert.InRange(loop * Frame, Sound.PowerDriverBiteS - 2 * Frame, Sound.PowerDriverBiteS + Frame);
        Assert.Single(working, BiteCue.Loop);
        Assert.Single(working, BiteCue.Bite);
        // A frame or two without a plan does not break it; past the hold it runs down, once.
        Assert.All(Run(ref hum, 3, works: false), c => Assert.Equal(BiteCue.None, c));
        Run(ref hum, 1, works: true);
        var left = Run(ref hum, 30, works: false);
        Assert.Single(left, BiteCue.Off);
        Assert.InRange(Array.IndexOf(left, BiteCue.Off) * Frame, Sound.PowerDriverHoldS - 2 * Frame, Sound.PowerDriverHoldS + Frame);
        // Back at work it bites again.
        Assert.Equal(BiteCue.Bite, Run(ref hum, 1, works: true)[0]);
    }

    [Fact]
    public void LetGoInTheBiteItRunsDownAndNeverLoops()
    {
        var hum = new BiteLoop();
        Run(ref hum, 12, works: true);
        var after = Run(ref hum, 60, works: false);
        Assert.Contains(BiteCue.Off, after);
        Assert.DoesNotContain(BiteCue.Loop, after);
    }

    [Fact]
    public void ItKeepsTheRatchetByItQuietWhileItWorksAndAWhileAfterAsTheLastWorkLands()
    {
        var hum = new BiteLoop();
        Run(ref hum, 1, works: false);
        Assert.False(hum.Quiets);
        Run(ref hum, 30, works: true);
        Assert.True(hum.Quiets);
        Run(ref hum, (int)(Sound.PowerDriverQuietS / Frame) - 2, works: false);
        Assert.True(hum.Quiets);
        Run(ref hum, 4, works: false);
        Assert.False(hum.Quiets);
    }

    [Fact]
    public void ItsBiteIsAsLongAsItsFileSoTheLoopTakesOverAsItEnds()
    {
        string file = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", Sound.PowerDriverBite.Files[0]["res://".Length..]);
        var bytes = File.ReadAllBytes(file);
        // A PCM WAV's byte rate at 28, its data's length after the "data" mark.
        int rate = BitConverter.ToInt32(bytes, 28), data = bytes.AsSpan().IndexOf("data"u8);
        double seconds = (double)BitConverter.ToInt32(bytes, data + 4) / rate;
        Assert.Equal(Sound.PowerDriverBiteS, seconds, 2);
        Assert.True(Sound.PowerDriverLoop.Loops);
    }
}
