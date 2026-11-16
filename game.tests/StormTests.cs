using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Flora;
using Stillwake.Game.Features.Sky;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A planet's weather seen and heard (docs/plans/surface-storms.md): how far it has come, its streaks and wisps, its wind.</summary>
public class StormTests
{
    private static readonly StretchLooks.Weather Dust = StretchLooks.Of(Content.Stretches.DustStorm.Id)!.Weather!;
    private static readonly StretchLooks.Weather Rain = StretchLooks.Of(Content.Stretches.RainStorm.Id)!.Weather!;
    private const int Warning = 800, Second = Sim.World.TicksPerSecond;
    private static readonly Storm Full = new(1, 1);

    [Fact]
    public void BothStormsHaveTheirPickedLooksTheDustItsWindTheRainItsRainAndBothTheWash()
    {
        Assert.Equal(240, Dust.Count);
        Assert.True(Dust.Streaks.Low);
        Assert.NotNull(Dust.Wisps);
        Assert.Same(Sound.StormWind, Dust.Wind);
        Assert.True(Sound.StormWind.Loops);
        Assert.Equal(210, Rain.Count);
        Assert.False(Rain.Streaks.Low);
        Assert.Null(Rain.Wisps);
        Assert.Same(Sound.StormRain, Rain.Wind);
        Assert.True(Sound.StormRain.Loops);
        Assert.Same(StretchLooks.Washed, Dust.Visibility);
        Assert.Same(StretchLooks.Washed, Rain.Visibility);
    }

    [Fact]
    public void NoWeatherIsNoStormAndItsWindIsSilent()
    {
        var shown = new StretchShown();
        Assert.Equal(Storm.None, Storm.Of(shown));
        shown.See(Content.Surfaces.Ship, "iceBelt", null, 0, 0, ran: 100);
        Assert.Equal(Storm.None, Storm.Of(shown));
        Assert.Equal(0, Storm.None.Heard(Dust));
        Assert.Equal(0, Full.Heard(null));
        Assert.Equal((1f, 1f, 1f), Storm.None.Dim(Dust));
        Assert.Equal(0, Storm.None.Streaks(Dust));
    }

    [Fact]
    public void TheWarningRisesFirstThenTheStormWholeSomeSecondsInThenBothEaseToNothing()
    {
        var before = Storm.At(0, 0, Warning, 0);
        Assert.Equal(Storm.None, before);
        var half = Storm.At(0.5f, Warning / 2, Warning, 0);
        Assert.Equal(0.5f, half.Warn, 3);
        Assert.Equal(0, half.Share);
        Assert.True(half.Heard(Dust) > 0);
        Assert.Equal(0, Storm.At(1, Warning - 2 * Second, Warning, 0).Share);
        var started = Storm.At(1, Warning, Warning, 0);
        Assert.InRange(started.Share, 0.01f, 0.5f);
        var whole = Storm.At(1, Warning + 9 * Second, Warning, 0);
        Assert.Equal(new Storm(1, 1), whole);
        Assert.Equal(1, whole.Dimmed, 3);
        var dim = whole.Dim(Dust);
        Assert.Equal(Dust.Dim.R, dim.R, 4);
        Assert.Equal(Dust.Dim.B, dim.B, 4);
        Assert.Equal(Dust.Count, whole.Streaks(Dust));
        var easing = Storm.At(1, Warning + 60 * Second, Warning, 11 * Second);
        Assert.InRange(easing.Share, 0.1f, 0.9f);
        var gone = Storm.At(1, Warning + 60 * Second, Warning, Storm.Ease * Second);
        Assert.Equal(Storm.None, gone);
        Assert.Equal(0, gone.Heard(Dust));
        Assert.Equal(0, gone.Heard(Rain));
        Assert.Equal(0, gone.Washed);
    }

    [Fact]
    public void ARunningStormIsReadFromTheSkyTheSameForAGuestJoiningMidStorm()
    {
        var shown = new StretchShown();
        shown.See(Content.PlanetTypes.First, Content.Stretches.DustStorm.Id, null, 0, 5000, ran: 30 * Second);
        var storm = Storm.Of(shown);
        Assert.Equal(new Storm(1, 1), storm);
        Assert.Equal(1, storm.Heard(Dust), 3);
        shown.See(Content.PlanetTypes.First, null, null, 0, 5000);
        shown.See(Content.PlanetTypes.First, null, null, 0, 5000 + Storm.Ease * Second);
        Assert.Equal(Storm.None, Storm.Of(shown));
    }

    [Fact]
    public void TheLandsFarthestLayerTakesAllTheHazeAndTheNearestThePicksLeast()
    {
        Assert.Equal(1, Storm.Veil(0, 3));
        Assert.Equal(0.85f, Storm.Veil(1, 3), 4);
        Assert.Equal(0.65f, Storm.Veil(2, 3), 4);
        Assert.Equal(0.65f, Storm.Veil(4, 5), 4);
        Assert.Equal(1, Storm.Veil(0, 1));
    }

    [Fact]
    public void AFullStormLaysItsCountOfStreaksInEachBoxAndHalfAStormHalf()
    {
        Assert.Equal(Rain.Count, InFirstBox(Lines(Rain, Full)).Count);
        Assert.Equal((int)MathF.Round(Rain.Count * 0.5f), InFirstBox(Lines(Rain, new Storm(1, 0.5f))).Count);
        Assert.Empty(Lines(Rain, new Storm(1, 0)));
    }

    [Fact]
    public void StreaksLieOnlyWhereBothEndsAreOpenSky()
    {
        Assert.Empty(Lines(Rain, Full, open: (_, _) => false));
        var lines = Lines(Rain, Full, open: (_, y) => y < 60);
        Assert.NotEmpty(lines);
        Assert.All(lines, l => Assert.True(l.HeadY < 60 && l.TailY < 60));
    }

    [Fact]
    public void StreaksAreTheSameForTheSameMomentAndFlyLeftAndDown()
    {
        Assert.Equal(Lines(Rain, Full, seconds: 3600.25), Lines(Rain, Full, seconds: 3600.25));
        Assert.NotEqual(Lines(Rain, Full, seconds: 1), Lines(Rain, Full, seconds: 1.1));
        // Their heads lead: the tail is up and to the right of each.
        Assert.All(Lines(Rain, Full), l => Assert.True(l.TailX > l.HeadX && l.TailY < l.HeadY));
        Assert.All(Lines(Dust, Full, ground: _ => 140), l => Assert.True(l.TailX > l.HeadX && l.TailY < l.HeadY));
    }

    [Fact]
    public void DustKeepsLowThickestByTheGroundAndNoneHigherThanItsReach()
    {
        const float ground = 400;
        var view = (0f, 0f, 4 * Streaks.Wide, ground);
        var lines = Lines(Dust, Full, view, ground: _ => ground);
        Assert.All(lines, l => Assert.True(ground - l.HeadY <= Streaks.LowReach + 1));
        int low = lines.Count(l => ground - l.HeadY < Streaks.LowReach / 2), high = lines.Count - low;
        Assert.True(low > 2 * high, $"{low} low, {high} high");
        Assert.Contains(lines, l => l.Speck != null);
        Assert.DoesNotContain(Lines(Rain, Full), l => l.Speck != null);
    }

    [Fact]
    public void OnlyDustBlowsWispsAlongTheGroundAsHighAsTheStorm()
    {
        const float ground = 100;
        var view = (0f, 0f, 4 * Streaks.Wide, 200f);
        Assert.Empty(Puffs(Rain, Full, view, ground));
        var puffs = Puffs(Dust, Full, view, ground);
        Assert.InRange(puffs.Count, 1, 6 * Dust.Wisps!.Count);
        Assert.All(puffs, p => Assert.InRange(p.Y, ground - 35.5f, ground - 2.5f));
        Assert.All(puffs, p => Assert.InRange(p.Alpha, 0.6f * Dust.Wisps.Alpha - 1e-4f, Dust.Wisps.Alpha + 1e-4f));
        Assert.Empty(Puffs(Dust, Storm.None, view, ground));
    }

    [Fact]
    public void AStormBlowsThePlantsItsWayInTheCalmAndDropsThemWhenItEases()
    {
        var gusts = new Gusts(FloraLooks.Breeze, () => 0.5) { Storm = 0.9 * StretchLooks.Weather.SwayGain };
        gusts.Advance(1 / 60.0);
        Assert.True(gusts.Blowing);
        Assert.Equal(Gusts.StormWay, gusts.Way);
        Assert.InRange(gusts.Strength, -2.25f * 1.01f, -2.25f * 0.69f);
        gusts.Storm = 0;
        gusts.Advance(1 / 60.0);
        Assert.False(gusts.Blowing);
        Assert.Equal(0, gusts.Strength);
    }

    [Fact]
    public void TheRainsLoopRisesWithTheStormAndReachesSilenceAsTheDustsWindDoes()
    {
        Assert.Equal(0, Storm.None.Heard(Rain));
        Assert.True(Storm.At(0.5f, Warning / 2, Warning, 0).Heard(Rain) > 0);
        Assert.Equal(1, Full.Heard(Rain), 3);
        Assert.Equal(0, Storm.At(1, Warning + 60 * Second, Warning, Storm.Ease * Second).Heard(Rain));
    }

    [Fact]
    public void TheWashIsAQuarterInTheWarningWholeInTheStormAndGoneAsItEases()
    {
        Assert.Equal(0, Storm.None.Washed);
        Assert.Equal(0.125f, Storm.At(0.5f, Warning / 2, Warning, 0).Washed, 3);
        Assert.Equal(0.25f, Storm.At(1, Warning - 2 * Second, Warning, 0).Washed, 3);
        Assert.Equal(1, Full.Washed, 3);
        Assert.Equal(0.5625f, Storm.At(1, Warning + 60 * Second, Warning, 11 * Second).Washed, 3);
    }

    [Fact]
    public void TheSmoothedGroundNeverStepsWithALedgeAndChangesSlowlyAcrossIt()
    {
        const int cell = 8, reach = Streaks.GroundReach;
        int[] tops = [.. Enumerable.Range(0, 200).Select(x => x < 100 ? 40 : 60)];
        var line = Streaks.Smoothed(tops, reach, cell);
        Assert.Equal(40 * cell, line[50], 3);
        Assert.Equal(60 * cell, line[150], 3);
        float step = 20f * cell / (2 * reach + 1);
        for (int x = 0; x < tops.Length; x++)
            Assert.True(MathF.Abs(line[(x + 1) % tops.Length] - line[x]) <= step + 1e-3f, $"column {x}");
        // Between columns it slides, never holding a column's height and jumping at its edge.
        float a = Streaks.GroundAt(line, 100 * cell, cell), b = Streaks.GroundAt(line, 100 * cell + 1, cell);
        Assert.InRange(b - a, 1e-4f, step / cell + 1e-3f);
        Assert.Equal(line[0], Streaks.GroundAt(line, 200 * cell + cell / 2f, cell), 3);
    }

    [Fact]
    public void AWispFadesByTheShareOfItsWidthDriftingIntoGroundAndIsGoneOnceAllIn()
    {
        const float ground = 100;
        var view = (0f, 0f, 4 * Streaks.Wide, 200f);
        var open = Puffs(Dust, Full, view, ground);
        var crossing = open[0];
        float wall = crossing.X + 5;
        var walled = new List<Streaks.Puff>();
        Streaks.Puffs(Dust, Full, view, 7.5, (x, y) => y < ground && x < wall, _ => ground, walled);
        Assert.All(walled, p => Assert.True(p.X < wall + 14));
        Assert.Equal(crossing.Alpha * 5 / 7, walled.Single(p => p.X == crossing.X).Alpha, 4);
        Assert.All(walled.Where(p => p.X < wall - 14), p => Assert.Equal(open.First(o => o.X == p.X).Alpha, p.Alpha));
    }

    private static List<Streaks.Line> InFirstBox(List<Streaks.Line> lines) =>
        [.. lines.Where(l => l.HeadX >= 0 && l.HeadX < Streaks.Wide && l.HeadY >= 0 && l.HeadY < Streaks.Tall)];

    private static List<Streaks.Line> Lines(StretchLooks.Weather weather, Storm storm, (float, float, float, float)? view = null,
        double seconds = 12.5, Func<float, float, bool>? open = null, Func<float, float>? ground = null)
    {
        var into = new List<Streaks.Line>();
        Streaks.Fill(weather, storm, view ?? (0, 0, Streaks.Wide, Streaks.Tall), seconds, open ?? ((_, _) => true), ground ?? (_ => 10_000), into);
        return into;
    }

    private static List<Streaks.Puff> Puffs(StretchLooks.Weather weather, Storm storm, (float, float, float, float) view, float ground)
    {
        var into = new List<Streaks.Puff>();
        Streaks.Puffs(weather, storm, view, 7.5, (_, y) => y < ground, _ => ground, into);
        return into;
    }
}
