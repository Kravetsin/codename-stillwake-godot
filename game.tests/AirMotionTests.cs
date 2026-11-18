using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using Xunit;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Tests;

/// <summary>
/// The air bar's motion as the air-motion pick E has it: a spring after the air, a breath spread over a breath so nothing steps,
/// a hit's lost air held then drained, a canister's sweep, parts growing and shrinking, every event eased in and out.
/// </summary>
public class AirMotionTests
{
    private const double Read = 0.25, Hit = 3.5;
    private const int Fill = 6;
    private static readonly (string, int)[] Kit = [("lungs", 20), ("helmet", 25), ("food", 15)];
    private static readonly (string, int)[] Bare = [("lungs", 20), ("food", 15)];
    private const float End = Vitals.AirReach;

    private static AirMotion At(int air, IReadOnlyList<(string, int)>? parts = null)
    {
        var motion = new AirMotion();
        parts ??= Kit;
        motion.Read(air, parts.Sum(p => p.Item2), parts, Hit, Fill);
        motion.Step(0, End);
        return motion;
    }

    /// <summary>
    /// Runs <paramref name="seconds"/> at <paramref name="fps"/>, the sim's air <paramref name="air"/> of the time read four times a
    /// second as the suit gauge does; <paramref name="frame"/> sees each frame after its step.
    /// </summary>
    private static void Run(AirMotion motion, double seconds, int fps, Func<double, int> air, Action<double>? frame = null, double from = 0)
    {
        int frames = (int)Math.Round(seconds * fps);
        int reads = (int)Math.Floor(from / Read + 1e-9);
        for (int f = 1; f <= frames; f++)
        {
            double t = from + (double)f / fps;
            if (Math.Floor(t / Read + 1e-9) > reads)
            {
                reads = (int)Math.Floor(t / Read + 1e-9);
                motion.Read(air(t), 60, Kit, Hit, Fill);
            }
            motion.Step(1.0 / fps, End);
            frame?.Invoke(t);
        }
    }

    /// <summary>Out of room air: a unit a breath, a breath a second, all within the helmet's part.</summary>
    private static int Draining(double t) => 44 - (int)Math.Floor(t + 1e-9);

    [Fact]
    public void ShowsTheFirstReadAtOnceInItsParts()
    {
        var motion = At(30);
        Assert.Equal(30, motion.Shown, 9);
        // The lungs full, ten of the helmet's: the edge past the lungs' piece and its gap.
        Assert.Equal(Vitals.RowLeft + 30 * Vitals.AirPerUnit + Vitals.Gap, motion.EdgeX, 3);
        Assert.Equal(motion.XOf(30), motion.EdgeX, 3);
        Assert.Equal([true, true, true], motion.Parts.Select(p => p.Present));
    }

    [Theory]
    [InlineData(410f)]
    [InlineData(250f)]
    public void LaysItsPartsAsLongAsVitalsAirLengthWithAGapBetween(float end)
    {
        var motion = new AirMotion();
        motion.Read(60, 60, Kit, Hit, Fill);
        motion.Step(0, end);
        var parts = motion.Parts;
        Assert.Equal(Vitals.AirLength(60, 3, end), parts.Sum(p => p.Length), 3);
        Assert.Equal(Vitals.RowLeft, parts[0].X, 3);
        for (int i = 1; i < parts.Count; i++) Assert.Equal(parts[i - 1].X + parts[i - 1].Length + Vitals.Gap, parts[i].X, 3);
        Assert.Equal(20f / 25, parts[0].Length / parts[1].Length, 3);
    }

    [Fact]
    public void ADrainingBarGlidesEveryFrameNeverSteppingOnceABreath()
    {
        var motion = At(44);
        var edges = new List<double>();
        Run(motion, 8, 144, Draining, t => { if (t > 2) edges.Add(motion.EdgeX); });
        var moves = edges.Zip(edges.Skip(1), (a, b) => a - b).ToList();
        double mean = moves.Average();
        // Every frame a little further, never resting between breaths nor jumping at one: a steady pace, under a pixel a frame.
        Assert.All(moves, d => Assert.InRange(d, 0.6 * mean, 1.4 * mean));
        Assert.True(mean < 0.1, $"{mean}");
        // Sub-pixel: the edge sits between whole pixels as often as on them.
        Assert.Contains(edges, e => Math.Abs(e * 1.1 - Math.Round(e * 1.1)) > 0.2);
        Assert.InRange(motion.Shown, 35.5, 37.5);
    }

    [Fact]
    public void ItMovesAlikeAt30And144FramesASecond()
    {
        var slow = At(44);
        var fast = At(44);
        var atSlow = new List<double>();
        var atFast = new List<double>();
        Run(slow, 6, 30, Draining, t => { if (Math.Abs(t - Math.Round(t)) < 1e-9) atSlow.Add(slow.Shown); });
        Run(fast, 6, 144, Draining, t => { if (Math.Abs(t - Math.Round(t)) < 1e-9) atFast.Add(fast.Shown); });
        Assert.Equal(6, atSlow.Count);
        // Read a frame later at 30, the bar lags by at most that frame's share of a breath.
        for (int i = 0; i < 6; i++) Assert.InRange(atSlow[i] - atFast[i], -0.08, 0.08);
        // At rest both settle on the same air exactly.
        Run(slow, 3, 30, _ => 38, from: 6);
        Run(fast, 3, 144, _ => 38, from: 6);
        Assert.Equal(slow.EdgeX, fast.EdgeX, 3);
    }

    [Fact]
    public void AHitsLostAirIsHeldWhiteThenDrainsSmoothlyAsTheEdgeEasesDownWithoutOvershoot()
    {
        var motion = At(44);
        motion.Read(36, 60, Kit, Hit, Fill);
        var hit = Assert.IsType<(double, double From, double To, bool Hurt)>(motion.Hit);
        Assert.Equal((44.0, 36.0, false), (hit.From, hit.To, hit.Hurt));
        double lost = 44, shown = motion.Shown;
        for (double t = 0; t < Look.HitS + 0.1; t += 1.0 / 60)
        {
            motion.Step(1.0 / 60, End);
            if (motion.Lost is not { } now) break;
            // Lit whole while it holds, then drained back, never up again and never by a jump.
            if (t < Look.HitHoldS - 0.02) Assert.Equal(44, now, 6);
            Assert.True(now <= lost + 1e-9 && lost - now < 0.6, $"{lost} -> {now}");
            Assert.True(motion.Shown <= shown + 1e-9 && motion.Shown >= 36 - 1e-6);
            (lost, shown) = (now, motion.Shown);
        }
        Assert.Null(motion.Hit);
        Assert.Equal(36, motion.Shown, 2);
    }

    [Fact]
    public void AHurtMakesEvenABreathsLossAHitInItsColourAndABreathAloneGlides()
    {
        var hurt = At(44);
        hurt.Hurt();
        hurt.Read(43, 60, Kit, Hit, Fill);
        Assert.Equal(true, hurt.Hit?.Hurt);
        // The next read's loss is a breath again.
        var breath = At(44);
        breath.Read(43, 60, Kit, Hit, Fill);
        Assert.Null(breath.Hit);
        breath.Step(0.5, End);
        Assert.Equal(43.5, breath.Aim, 6);
    }

    [Fact]
    public void ACanistersAirComesAtOnceEasedInItsSweepRunningAndNoRefillLight()
    {
        var motion = At(18);
        motion.Surge();
        Assert.Null(motion.Canister);
        motion.Step(0.1, End);
        motion.Read(38, 60, Kit, Hit, Fill);
        Assert.Equal((0.0, 18.0, 38.0), motion.Canister);
        Assert.Equal(38, motion.Aim);
        double shown = motion.Shown;
        Run(motion, 1.5, 60, _ => 38, _ =>
        {
            Assert.True(motion.Shown >= shown - 1e-9 && motion.Shown <= 38 + 1e-9);
            shown = motion.Shown;
        });
        Assert.Equal(38, motion.Shown, 1);
        Assert.Equal(0, motion.Gain, 6);
        Run(motion, 1, 60, _ => 38, from: 1.5);
        Assert.Null(motion.Canister);
    }

    [Fact]
    public void ACanisterWhoseAirIsNeverReadStillRunsItsLight()
    {
        var motion = At(60);
        motion.Surge();
        motion.Step(Look.CanisterWaitS + 0.01, End);
        Assert.Equal((0.0, 60.0, 60.0), motion.Canister);
    }

    [Fact]
    public void RoomAirRefillsBreathByBreathGlidingItsLightEasingInAndOut()
    {
        var motion = At(20);
        double gain = 0, aim = motion.Aim;
        Run(motion, 3, 60, t => 20 + 4 * (int)Math.Floor(t), _ =>
        {
            Assert.True(Math.Abs(motion.Gain - gain) < 0.1 && motion.Aim >= aim);
            (gain, aim) = (motion.Gain, motion.Aim);
        });
        Assert.True(motion.Gain > 0.9, $"{motion.Gain}");
        Run(motion, 3, 60, _ => 32, _ =>
        {
            Assert.True(Math.Abs(motion.Gain - gain) < 0.1);
            gain = motion.Gain;
        }, from: 3);
        Assert.True(motion.Gain < 0.01, $"{motion.Gain}");
    }

    [Fact]
    public void AHelmetTakenOffShrinksAwayAndPutOnGrowsBackInItsPlaceWithNoHit()
    {
        var motion = At(60);
        float end = motion.End;
        motion.Read(35, 35, Bare, Hit, Fill);
        motion.Step(0, End);
        Assert.Null(motion.Hit);
        Assert.Equal(["lungs", "helmet", "food"], Keys(motion));
        Assert.False(motion.Parts[1].Present);
        RunFrames(motion, 1.5, () =>
        {
            Assert.True(end - motion.End is >= 0 and < 8, $"{end} -> {motion.End}");
            end = motion.End;
        });
        Assert.Equal(2, motion.Parts.Count);
        Assert.Equal(Vitals.RowLeft + Vitals.AirLength(35, 2, End) + Vitals.Gap, motion.End, 2);
        motion.Read(35, 60, Kit, Hit, Fill);
        motion.Step(0, End);
        Assert.Equal(["lungs", "helmet", "food"], Keys(motion));
        Assert.Equal(0, motion.Parts[1].Length, 3);
        RunFrames(motion, 2, () =>
        {
            Assert.True(motion.End - end is >= 0 and < 8, $"{end} -> {motion.End}");
            end = motion.End;
        });
        Assert.Equal(Vitals.RowLeft + Vitals.AirLength(60, 3, End) + 2 * Vitals.Gap, motion.End, 2);
        Assert.Null(motion.Hit);
    }

    [Fact]
    public void TurningRedEasesInAndOut()
    {
        var motion = At(10);
        motion.Short = true;
        double red = 0;
        RunFrames(motion, 1.5, () =>
        {
            Assert.True(motion.Red >= red && motion.Red - red < 0.1);
            red = motion.Red;
        });
        Assert.True(red > 0.99);
        motion.Short = false;
        RunFrames(motion, 1.5, () =>
        {
            Assert.True(motion.Red <= red && red - motion.Red < 0.1);
            red = motion.Red;
        });
        Assert.True(red < 0.01);
    }

    [Fact]
    public void TheEdgesHighlightShowsWhileItMovesAndFadesAtRest()
    {
        var motion = At(44);
        Assert.Equal(0, motion.Moving);
        Run(motion, 4, 60, Draining);
        Assert.True(motion.Moving > 0.5, $"{motion.Moving}");
        Run(motion, 3, 60, _ => 40, from: 4);
        Assert.True(motion.Moving < 0.01, $"{motion.Moving}");
    }

    [Fact]
    public void AirBreathedAgainRunsOneWave()
    {
        var motion = At(30);
        motion.Wave();
        motion.Step(Look.WaveS / 2, End);
        Assert.Equal(Look.WaveS / 2, motion.Again);
        motion.Step(Look.WaveS / 2 + 0.01, End);
        Assert.Null(motion.Again);
    }

    [Fact]
    public void PartsThatDoNotAddUpToTheCapacityAreOnePiece()
    {
        var motion = new AirMotion();
        motion.Read(30, 45, Kit, Hit, Fill);
        motion.Step(0, End);
        var part = Assert.Single(motion.Parts);
        Assert.Equal((45, true), (part.Size, part.Present));
        Assert.Equal(Vitals.AirLength(45, 1, End), part.Length, 3);
    }

    private static string[] Keys(AirMotion motion) => [.. Enumerable.Range(0, motion.Parts.Count).Select(i => KeyOf(motion, i))];

    /// <summary>A laid part's key by its size, the kit's sizes all differing.</summary>
    private static string KeyOf(AirMotion motion, int i) => Kit.First(k => k.Item2 == motion.Parts[i].Size).Item1;

    private static void RunFrames(AirMotion motion, double seconds, Action frame)
    {
        for (int f = 0; f < (int)(seconds * 60); f++)
        {
            motion.Step(1.0 / 60, End);
            frame();
        }
    }
}
