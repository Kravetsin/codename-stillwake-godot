using System;
using System.Collections.Generic;
using Stillwake.Game.Features.Sound;
using Xunit;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Tests;

/// <summary>The ship's music: a minute off a planet, then a track, a silence of minutes after it ends, the next in turn.</summary>
public class ShipMusicTests
{
    private const double Frame = 0.1;

    /// <summary>Runs <paramref name="seconds"/> in frames; each track plays <paramref name="trackS"/>; returns when each began and which.</summary>
    private static List<(double At, int Track)> Run(ShipMusic music, double seconds, Func<double, bool> offPlanet, double trackS = 220)
    {
        var begun = new List<(double, int)>();
        double endsAt = -1;
        for (double t = 0; t < seconds; t += Frame)
            if (music.Step(Frame, offPlanet(t), t < endsAt) is int track)
            {
                begun.Add((t, track));
                endsAt = t + trackS;
            }
        return begun;
    }

    [Fact]
    public void TheFirstTrackBeginsAfterAMinuteOffAPlanetAndNotBefore()
    {
        var begun = Run(new ShipMusic(new Random(1), 2), 120, _ => true);
        Assert.InRange(begun[0].At, S.ShipFirstS - 0.2, S.ShipFirstS + 0.2);
    }

    [Fact]
    public void TheWaitCountsOnlyOffAPlanetButATrackBegunPlaysOn()
    {
        // Down on a planet from 30 s to 90 s: the first minute off it ends at 120 s.
        var begun = Run(new ShipMusic(new Random(1), 2), 130, t => t < 30 || t >= 90);
        Assert.Single(begun);
        Assert.InRange(begun[0].At, 119.8, 120.2);
        Assert.Empty(Run(new ShipMusic(new Random(1), 2), 3600, _ => false));
    }

    [Fact]
    public void ItIsHeardInSpaceByAVesselAsByTheShipAndNeverOnAPlanet()
    {
        Assert.True(ShipMusic.HeardOn(Content.Surfaces.Ship));
        foreach (var vessel in Content.Vessels.Kinds) Assert.True(ShipMusic.HeardOn(vessel), vessel.Id);
        Assert.False(ShipMusic.HeardOn(Content.Surfaces.TestStrip));
        foreach (var planet in Content.PlanetTypes.All) Assert.False(ShipMusic.HeardOn(planet), planet.Id);
    }

    [Fact]
    public void ATrackPlaysAtThePitchItWasMade() => Assert.All(S.ShipTracks, track => Assert.Equal(0, track.Pitch));

    [Fact]
    public void TracksTakeTurnsWithMinutesOfSilenceBetween()
    {
        const double trackS = 220;
        var begun = Run(new ShipMusic(new Random(7), 2), 4 * 3600, _ => true, trackS);
        Assert.True(begun.Count > 10);
        for (int i = 1; i < begun.Count; i++)
        {
            Assert.NotEqual(begun[i - 1].Track, begun[i].Track);
            double silence = begun[i].At - begun[i - 1].At - trackS;
            Assert.InRange(silence, S.ShipGapLeastS - 0.2, S.ShipGapMostS + 0.2);
        }
    }
}
