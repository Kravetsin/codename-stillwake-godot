using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A train of one-shots (the drill motor's grains), the jetpack's thrust and the charging station's hum and blip (0054): when they sound, how often, how varied.</summary>
public class EquipSoundTests
{
    private const double Frame = 1000.0 / 60;
    /// <summary>A train's rate and spread, as a buzz of one-shots would have them.</summary>
    private const float Rate = 18, Pitch = 0.12f, VolumeDb = 1.5f;

    /// <summary>The strikes due in each of <paramref name="frames"/> frames of <paramref name="ms"/>, on as <paramref name="on"/> says frame by frame.</summary>
    private static List<int> Strikes(ref StrikeTrain train, int frames, Func<int, bool> on, double ms = Frame)
    {
        var due = new List<int>();
        for (int i = 0; i < frames; i++) due.Add(train.Due(ms, on(i), Rate));
        return due;
    }

    [Fact]
    public void StrikesAtItsRateTheFirstAtOnceWhateverTheFrameRate()
    {
        var train = new StrikeTrain();
        var due = Strikes(ref train, 60, _ => true);
        Assert.Equal(1, due[0]);
        Assert.InRange(due.Sum(), 18, 19);
        // At 144 frames a second and at 20, the same buzz.
        var fast = new StrikeTrain();
        Assert.InRange(Strikes(ref fast, 144, _ => true, 1000.0 / 144).Sum(), 18, 19);
        var slow = new StrikeTrain();
        Assert.InRange(Strikes(ref slow, 20, _ => true, 50).Sum(), 18, 19);
    }

    [Fact]
    public void StopsTheMomentDrillingStopsAndStartsAfreshOnTheNext()
    {
        var train = new StrikeTrain();
        var due = Strikes(ref train, 90, i => i < 30 || i >= 60);
        Assert.All(due.Skip(30).Take(30), n => Assert.Equal(0, n));
        // Begun again: a strike that very frame, not one owed from before.
        Assert.Equal(1, due[60]);
        Assert.InRange(due.Skip(60).Sum(), 9, 10);
    }

    [Fact]
    public void OwesNoBurstAfterAStall()
    {
        var train = new StrikeTrain();
        train.Due(Frame, true, 18);
        Assert.Equal(StrikeTrain.MostPerFrame, train.Due(2000, true, 18));
    }

    [Fact]
    public void VariesEachStrikesPitchAndVolumeWithinTheirSpreadAndAcrossIt()
    {
        var random = new Random(7);
        var strikes = Enumerable.Range(0, 2000).Select(_ => StrikeTrain.Vary(random, Pitch, VolumeDb)).ToList();
        Assert.All(strikes, s => Assert.InRange(s.Pitch, 0.88f, 1.12f));
        Assert.All(strikes, s => Assert.InRange(s.OffsetDb, -1.5f, 1.5f));
        // Spread across the range, not bunched: some near each end.
        Assert.Contains(strikes, s => s.Pitch < 0.9f);
        Assert.Contains(strikes, s => s.Pitch > 1.1f);
        Assert.Contains(strikes, s => s.OffsetDb < -1.3f);
        Assert.Contains(strikes, s => s.OffsetDb > 1.3f);
    }

    [Fact]
    public void ThrustRisesInItsAttackFallsInItsReleaseAndThenStops()
    {
        var thrust = new LoopEnvelope();
        Assert.False(thrust.Playing);
        thrust.Step(true, 0.01, Sound.ThrustAttackS, Sound.ThrustReleaseS);
        Assert.True(thrust.Playing);
        Assert.Equal(0.25, thrust.Share, 6);
        thrust.Step(true, 0.03, Sound.ThrustAttackS, Sound.ThrustReleaseS);
        Assert.Equal(1, thrust.Share, 6);
        Assert.Equal(0, thrust.GainDb, 3);
        // Let go: still heard, falling, for the release's 140 ms, then stopped.
        thrust.Step(false, 0.07, Sound.ThrustAttackS, Sound.ThrustReleaseS);
        Assert.True(thrust.Playing);
        Assert.Equal(0.5, thrust.Share, 6);
        Assert.InRange(thrust.GainDb, -6.1f, -5.9f);
        thrust.Step(false, 0.07, Sound.ThrustAttackS, Sound.ThrustReleaseS);
        Assert.False(thrust.Playing);
        Assert.True(thrust.GainDb < -60);
    }

    [Fact]
    public void BlipsOnceWhenAChargingBatteryComesFullNotWhenOnePutInFull()
    {
        Assert.True(Charges.CameFull(wasCharging: true, 0.99f, 1));
        // Full already, put in full, or filled while it was not charging (swapped by hand): no blip.
        Assert.False(Charges.CameFull(true, 1, 1));
        Assert.False(Charges.CameFull(false, -1, 1));
        Assert.False(Charges.CameFull(false, 0.4f, 1));
        Assert.False(Charges.CameFull(true, 0.5f, 0.6f));
    }
}
