using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Sound;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The machines heard at work (0059): the drill's motor and its hold on a piece, the Former's strokes in step with its head and its motor, the smelter's random pops, and the suit louder in space.</summary>
public class MachineSoundTests
{
    private const double Frame = 1.0 / 60;

    /// <summary>The motor after <paramref name="frames"/> frames of 1/60 s, running and loaded as told frame by frame.</summary>
    private static MotorSound Run(MotorSound motor, int frames, bool runs, bool loaded, Sound.Motor tune)
    {
        for (int i = 0; i < frames; i++) motor.Step(runs, loaded, Frame, tune);
        return motor;
    }

    [Fact]
    public void TheDrillsMotorSpinsUpWithItsPictureFromAThirdOfItsPitchAndIsFullAtTwoThirdsOfItsSpeed()
    {
        var tune = Sound.DrillMotor;
        Assert.Equal((0.12, 0.7), (tune.SpinUpS, tune.SpinDownS));
        Assert.Equal((HeldLook.SpinUpMs, HeldLook.SpinDownMs), (tune.SpinUpS * 1000, tune.SpinDownS * 1000));
        var motor = new MotorSound();
        Assert.False(motor.Playing);
        motor.Step(true, false, 0.03, tune);
        Assert.True(motor.Playing);
        Assert.Equal(0.25, motor.Spin, 6);
        // speed = 0.35 + 0.65·spin; gain = 20·lg(min(1, 1.5·spin)).
        Assert.Equal(0.35 + 0.65 * 0.25, motor.Speed(tune), 6);
        Assert.Equal(20 * Math.Log10(0.375), motor.GainDb(tune), 3);
        motor.Step(true, false, 0.05, tune);
        Assert.Equal(0, motor.GainDb(tune), 3);
        motor = Run(motor, 10, true, false, tune);
        Assert.Equal(1, motor.Speed(tune), 6);
    }

    [Fact]
    public void OnAPieceTheDrillsMotorSagsEightPercentAndQuietensThreeDecibelsEasedInAndOut()
    {
        var tune = Sound.DrillMotor;
        var motor = Run(new MotorSound(), 30, true, false, tune);
        // 60 ms in: a time constant, 63% of the way.
        motor.Step(true, true, 0.06, tune);
        Assert.Equal(1 - Math.Exp(-1), motor.Load, 6);
        motor = Run(motor, 60, true, true, tune);
        Assert.Equal(0.92, motor.Speed(tune), 3);
        Assert.Equal(-3, motor.GainDb(tune), 2);
        // Off the piece, slower back: 150 ms leaves 37% of the load.
        motor.Step(true, false, 0.15, tune);
        Assert.InRange(motor.Load, 0.36, 0.38);
    }

    [Fact]
    public void TheDrillsMotorRunsDownOverSevenTenthsOfASecondAndThenStops()
    {
        var tune = Sound.DrillMotor;
        var motor = Run(new MotorSound(), 30, true, false, tune);
        motor.Step(false, false, 0.35, tune);
        Assert.True(motor.Playing);
        Assert.Equal(0.5, motor.Spin, 6);
        Assert.Equal(0.35 + 0.65 * 0.5, motor.Speed(tune), 6);
        motor.Step(false, false, 0.36, tune);
        Assert.False(motor.Playing);
    }

    [Fact]
    public void TheWhirsGrainsComeNineASecondAtSpeedAndFewerSpinningUp()
    {
        var tune = Sound.DrillMotor;
        var motor = new MotorSound();
        var grains = new StrikeTrain();
        var due = new List<int>();
        for (int i = 0; i < 60; i++)
        {
            motor.Step(true, false, Frame, tune);
            due.Add(grains.Due(Frame * 1000, motor.Spin > 0, Sound.DrillWhirGrainsPerSecond * motor.Speed(tune)));
        }
        Assert.Equal(1, due[0]);
        // A second from a standstill: a little under nine, the first 120 ms slower.
        Assert.InRange(due.Sum(), 8, 9);
        int atSpeed = 0;
        for (int i = 0; i < 60; i++) atSpeed += grains.Due(Frame * 1000, true, Sound.DrillWhirGrainsPerSecond * motor.Speed(tune));
        Assert.InRange(atSpeed, 8, 10);
    }

    [Fact]
    public void GrazingAnEdgeKeepsTheDrillOnThePieceForATenthOfASecond()
    {
        Assert.Equal(0.1, Sound.DrillTouchHoldS);
        var hold = new TouchHold();
        Assert.False(hold.Step(false, Frame, Sound.DrillTouchHoldS));
        // On and off frame by frame along an edge: never off.
        var grazing = Enumerable.Range(0, 30).Select(i => hold.Step(i % 3 == 0, Frame, Sound.DrillTouchHoldS)).ToList();
        Assert.All(grazing, Assert.True);
        // Left for good: off once the hold has run out, six frames later.
        hold.Step(true, Frame, Sound.DrillTouchHoldS);
        var left = Enumerable.Range(0, 10).Select(_ => hold.Step(false, Frame, Sound.DrillTouchHoldS)).ToList();
        Assert.True(left[4]);
        Assert.False(left[6]);
    }

    /// <summary>The strokes struck over <paramref name="ms"/> of the Former working, frames of <paramref name="frameMs"/>, as StationView steps its pictures.</summary>
    private static List<double> Strokes(double ms, double frameMs, int id = 0, double startMs = 0)
    {
        var beat = new FrameBeat();
        var struck = new List<double>();
        for (double clock = startMs; clock < startMs + ms; clock += frameMs)
        {
            long step = (long)(clock / 110) + id;
            if (beat.Struck(true, step, 4, 2)) struck.Add(clock);
        }
        return struck;
    }

    [Fact]
    public void TheFormerStrikesEachTimeItsHeadComesDownEveryFortyFourHundredthsOfASecond()
    {
        var struck = Strokes(4400, 1000.0 / 60);
        Assert.Equal(10, struck.Count);
        // Each as the third picture shows: 220 ms in, then every 440 ms.
        Assert.InRange(struck[0], 220, 220 + 17);
        for (int i = 1; i < struck.Count; i++) Assert.InRange(struck[i] - struck[i - 1], 440 - 17, 440 + 17);
    }

    [Fact]
    public void ASlowFrameThatSkipsTheHeadsPictureStillStrikesOnce()
    {
        // At 8 frames a second the third picture is now and then never shown.
        Assert.Equal(10, Strokes(4400, 125).Count);
    }

    [Fact]
    public void AFormerStartedWithItsHeadDownStrikesAtOnceAndAStopStartsAfresh()
    {
        var beat = new FrameBeat();
        Assert.True(beat.Struck(true, 6, 4, 2));
        Assert.False(beat.Struck(true, 6, 4, 2));
        Assert.False(beat.Struck(true, 7, 4, 2));
        Assert.False(beat.Struck(false, 10, 4, 2));
        Assert.False(beat.Struck(true, 11, 4, 2));
        Assert.True(beat.Struck(true, 14, 4, 2));
    }

    [Fact]
    public void TheFormersMotorDipsThreePercentAtAStrokeAndIsBackWithinAFewTenthsOfASecond()
    {
        var tune = Sound.FormerSpin;
        var motor = Run(new MotorSound(), 60, true, false, tune);
        Assert.Equal(1, motor.Speed(tune), 6);
        Assert.Equal(0, motor.GainDb(tune), 3);
        motor.Kick();
        Assert.Equal(0.97, motor.Speed(tune), 6);
        motor.Step(true, false, 0.12, tune);
        Assert.Equal(1 - 0.03 * Math.Exp(-1), motor.Speed(tune), 6);
        motor = Run(motor, 30, true, false, tune);
        Assert.InRange(motor.Speed(tune), 0.9995, 1);
        // Standing it turns at half its pitch, spun up over 0.6 s and run down over 0.8 s.
        var standing = new MotorSound();
        standing.Step(true, false, 0.3, tune);
        Assert.Equal(0.75, standing.Speed(tune), 6);
        standing = Run(standing, 30, true, false, tune);
        standing = Run(standing, 30, false, false, tune);
        Assert.True(standing.Playing);
        standing = Run(standing, 20, false, false, tune);
        Assert.False(standing.Playing);
    }

    [Fact]
    public void TheSmeltersMeltPopsTwiceASecondOnAverageAtRandomAndNotTheMomentItStarts()
    {
        var random = new Random(41);
        var pops = new RandomTrain();
        Assert.Equal(0, pops.Due(Frame, true, Sound.SmelterCracklesPerSecond, random));
        int total = 0;
        var perSecond = new List<int>();
        for (int s = 0; s < 300; s++)
        {
            int n = 0;
            for (int i = 0; i < 60; i++) n += pops.Due(Frame, true, Sound.SmelterCracklesPerSecond, random);
            perSecond.Add(n);
            total += n;
        }
        Assert.InRange(total / 300.0, 1.8, 2.2);
        // At random, not on a clock: some seconds have none, some four or more.
        Assert.Contains(0, perSecond);
        Assert.Contains(perSecond, n => n >= 4);
        Assert.Equal(0, pops.Due(Frame, false, Sound.SmelterCracklesPerSecond, random));
    }

    [Fact]
    public void ASmelterThatStallsOwesNoBurstOfPops()
    {
        var random = new Random(3);
        var pops = new RandomTrain();
        pops.Due(Frame, true, 2, random);
        Assert.Equal(StrikeTrain.MostPerFrame, pops.Due(30, true, 2, random));
        Assert.True(pops.Due(Frame, true, 2, random) <= 1);
    }

    [Fact]
    public void EveryStationCarriesAlikeAndTheFormersStrokeNeverPounds()
    {
        // Every sound a station makes at work fades the same way, so a station sounds as loud as the next at any distance.
        var stations = new[] { Sound.FormerStroke, Sound.FormerMotor, Sound.SmelterHum, Sound.SmelterCrackle, Sound.Fire, Sound.ChargerHum };
        Assert.All(stations, cue => Assert.Same(Sound.StationReach, cue.Heard));
        Assert.Equal(0.25f, Sound.StationReach.Share(6), 6);
        // The stroke only a little over its motor, and quieter than a door or a step.
        Assert.InRange(Sound.FormerStroke.VolumeDb - Sound.FormerMotor.VolumeDb, 0, 4);
        Assert.True(Sound.FormerStroke.VolumeDb < Sound.DoorOpen.VolumeDb && Sound.FormerStroke.VolumeDb < Sound.Step.VolumeDb);
        // The drill's motor at the level it was auditioned at, under the old strikes it was matched to.
        Assert.Equal(-8, Sound.DrillWhir.VolumeDb);
        Assert.Equal((0.15f, 0.1f, 0.02f), (Sound.FormerStrokePitch, Sound.SmelterCracklePitch, Sound.DrillWhirPitch));
        Assert.True(Sound.FormerMotor.Loops && Sound.SmelterHum.Loops);
        Assert.False(Sound.DrillWhir.Loops || Sound.SmelterCrackle.Loops || Sound.FormerStroke.Loops);
    }

    [Fact]
    public void IdleToolSoundsShareOneVolume()
    {
        // The drill running free and the axe's swing, their files levelled alike: one knob turns both.
        Assert.All(new[] { Sound.DrillWhir, Sound.Swing }, cue => Assert.Equal(Sound.ToolIdleDb, cue.VolumeDb));
        // The swing a one-shot of four files, varied in pitch every time, a hit or a miss.
        Assert.Equal((4, false), (Sound.Swing.Files.Count, Sound.Swing.Loops));
        Assert.True(Sound.Swing.Pitch > 0);
    }

    [Fact]
    public void FloatingTheSuitIsRaisedAFewDecibelsAndAboardItIsAsItWas()
    {
        Assert.Equal(0, Hearing.At(0).SuitDb);
        Assert.Equal(Sound.SuitFloatingDb, Hearing.At(1).SuitDb, 6);
        Assert.InRange(Sound.SuitFloatingDb, 3, 9);
        Assert.Equal(Sound.SuitFloatingDb / 2, Hearing.At(0.5).SuitDb, 6);
    }
}
