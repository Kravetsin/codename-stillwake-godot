using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Features.Crew;
using Xunit;
using static Stillwake.Game.Tests.PoseTests;

namespace Stillwake.Game.Tests;

/// <summary>The fire axe's swings: one clock for the blade drawn and the hit sent (0034).</summary>
public class AxeSwingTests
{
    private const double Frame = 1000.0 / 60;
    private static readonly double HitMs = AxeSwing.HitAt * AxeSwing.Ms;

    /// <summary>What each frame did, the phase it drew and its time, from frames <paramref name="frames"/> ms long.</summary>
    private static List<(double At, SwingStep Step, double? Phase)> Play(int count, Func<double, bool> wanted, double frame = Frame)
    {
        var swing = new AxeSwing();
        var played = new List<(double, SwingStep, double?)>();
        for (int i = 0; i < count; i++)
        {
            double at = i * frame;
            var step = swing.Advance(frame, wanted(at));
            played.Add((at, step, swing.Phase));
        }
        return played;
    }

    /// <summary>The frames drawing the blade past the wreck that the frame before drew short of it, or drew no swing.</summary>
    private static List<double> Strikes(List<(double At, SwingStep Step, double? Phase)> played)
    {
        var strikes = new List<double>();
        for (int i = 1; i < played.Count; i++)
            if (played[i].Phase is double now && now >= AxeSwing.HitAt
                && (played[i - 1].Phase is not double was || was < AxeSwing.HitAt || was > now))
                strikes.Add(played[i].At);
        return strikes;
    }

    private static List<double> Hits(List<(double At, SwingStep Step, double? Phase)> played) =>
        played.Where(p => p.Step.Hits).Select(p => p.At).ToList();

    [Fact]
    public void ClickedFastItHitsOnceASwingAndStrikesOnlyWhenItHits()
    {
        // Eight clicks a second for two seconds, each held about 60 ms.
        var played = Play(120, at => at % 125 < 60);
        var hits = Hits(played);
        Assert.Equal(4, hits.Count);
        for (int i = 1; i < hits.Count; i++) Assert.True(hits[i] - hits[i - 1] >= AxeSwing.Ms - Frame, $"hits {hits[i - 1]} and {hits[i]}");
        Assert.Equal(hits, Strikes(played));
        // The swing began on the first click, and the clicks during it began none.
        Assert.Equal(4, played.Count(p => p.Step.Begun));
    }

    [Fact]
    public void HeldItSwingsAtTheAxesPaceEachSwingHittingOnce()
    {
        var played = Play(180, _ => true);
        var hits = Hits(played);
        Assert.Equal(6, hits.Count);
        Assert.InRange(hits[0], HitMs - Frame, HitMs + Frame);
        for (int i = 1; i < hits.Count; i++) Assert.InRange(hits[i] - hits[i - 1], AxeSwing.Ms - Frame, AxeSwing.Ms + Frame);
        Assert.Equal(hits, Strikes(played));
        // Held, no frame passes between swings: the axe never rests.
        Assert.All(played, p => Assert.NotNull(p.Phase));
    }

    [Fact]
    public void EverySwingWhooshesOnceItsLoudestInTheDownstrokeBeforeItHits()
    {
        // Held, and a click let go at once: every swing whooshes, whatever it meets, the hit or the sim never asked.
        var held = Play(180, _ => true);
        var whooshes = held.Where(p => p.Step.Whooshes).Select(p => p.At).ToList();
        Assert.Equal(held.Count(p => p.Step.Begun), whooshes.Count);
        Assert.Single(Play(60, at => at < Frame), p => p.Step.Whooshes);
        Assert.InRange(whooshes[0], AxeSwing.WhooshMs - Frame, AxeSwing.WhooshMs + Frame);
        // Its peak falls in the downstroke, before the blade crosses the line.
        double peak = (AxeSwing.WhooshMs + Sound.SwingPeakS * 1000) / AxeSwing.Ms;
        Assert.InRange(peak, AxeSwing.WindShare, AxeSwing.HitAt);
        // Stopped before it, a swing makes none.
        var swing = new AxeSwing();
        swing.Advance(Frame, true);
        swing.Stop();
        Assert.False(swing.Advance(AxeSwing.MostPerFrameMs, false).Whooshes);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(33)]
    public void HeldItKeepsTheAxesPaceOverAMinuteAtAnyFrameRate(double frame)
    {
        int frames = (int)(60_000 / frame);
        var hits = Hits(Play(frames, _ => true, frame));
        Assert.Equal((int)((frames * frame - HitMs) / AxeSwing.Ms) + 1, hits.Count);
    }

    [Fact]
    public void ASwingBegunPlaysThroughAndHitsThoughTheButtonIsLetGo()
    {
        // A click shorter than the wind-up: the swing is committed.
        var played = Play(60, at => at < Frame);
        Assert.Equal([played.First(p => p.Phase >= AxeSwing.HitAt).At], Hits(played));
        Assert.Equal(0, played[0].Phase);
        Assert.Null(played.First(p => p.At >= AxeSwing.Ms).Phase);
        Assert.Single(played, p => p.Step.Begun);
    }

    [Fact]
    public void StoppedBeforeItsStrikeItNeverHits()
    {
        var swing = new AxeSwing();
        swing.Advance(Frame, true);
        swing.Advance(HitMs - 2 * Frame, false);
        swing.Stop();
        Assert.Null(swing.Phase);
        Assert.False(swing.Advance(Frame, false).Hits);
        Assert.Null(swing.Phase);
        // Wanted again, a new swing begins from the top.
        Assert.True(swing.Advance(Frame, true).Begun);
        Assert.Equal((2, 0.0), (swing.Count, swing.Phase));
    }

    [Fact]
    public void TheBladeMeetsTheWreckWhenTheHitGoes()
    {
        Assert.Equal(0, AxeSwing.Lift(AxeSwing.HitAt), 9);
        Assert.Equal(-AxeSwing.Raised, AxeSwing.Lift(AxeSwing.WindShare), 9);
        Assert.True(AxeSwing.Lift(AxeSwing.HitAt - 0.01) < 0);
        Assert.True(AxeSwing.Lift(AxeSwing.HitAt + 0.01) > 0);
        // A swing begins and ends on the line to the wreck, so one held runs on into the next unbroken.
        Assert.Equal(0, AxeSwing.Lift(0), 9);
        Assert.Equal(0, AxeSwing.Lift(1), 9);
    }

    [Fact]
    public void AStallOwesNoBurstOfHits()
    {
        var swing = new AxeSwing();
        swing.Advance(Frame, true);
        // Five seconds lost to a stall move the swing on as one long frame would, and no further.
        Assert.False(swing.Advance(5000, true).Hits);
        Assert.Equal(AxeSwing.MostPerFrameMs / AxeSwing.Ms, swing.Phase);
        Assert.Equal(1, swing.Count);
    }

    /// <summary>
    /// Runs of frames, each a frame's length (past the longest counted, 100 ms) and how many, the button
    /// wanted through the run or not: clicks of one frame to holds of seconds, at any frame rate.
    /// </summary>
    private static readonly Gen<List<(int Ms, int Frames, bool Wanted)>> Runs =
        (from ms in Gen.Choose(1, 150) from frames in Gen.Choose(1, 40) from wanted in Gen.Elements(true, false) select (ms, frames, wanted))
        .ListOf().Select(l => l.Take(40).ToList());

    [Property(MaxTest = 300)]
    public Property HitsOnlyAsTheBladeMeetsTheWreckAndNeverFasterThanTheAxesPace() => Prop.ForAll(Runs.ToArbitrary(), runs =>
    {
        var swing = new AxeSwing();
        double sinceHit = double.PositiveInfinity;
        int hits = 0;
        foreach (var (ms, frames, wanted) in runs)
            for (int i = 0; i < frames; i++)
            {
                double? before = swing.Phase;
                var step = swing.Advance(ms, wanted);
                sinceHit += Math.Min(ms, AxeSwing.MostPerFrameMs);
                if (!step.Hits) continue;
                hits++;
                Assert.True(sinceHit > AxeSwing.Ms - AxeSwing.MostPerFrameMs, $"two hits {sinceHit} ms apart");
                sinceHit = 0;
                // Drawn this frame with the blade through the line to the wreck; the frame before, short of it.
                Assert.True(swing.Phase >= AxeSwing.HitAt);
                Assert.True(before is not double was || was < AxeSwing.HitAt);
            }
        // Every swing begun has hit, but one still winding up.
        int unstruck = swing.Phase is double phase && phase < AxeSwing.HitAt ? 1 : 0;
        Assert.Equal(swing.Count - unstruck, hits);
    });

    // Others' heroes: each swing sent is swung once.

    [Fact]
    public void FollowsAnothersHeroSwingingOnceForEachSwingSent()
    {
        var swing = new AxeSwing();
        int begun = 0;
        void Run(double ms, int? sent)
        {
            for (double t = 0; t < ms; t += Frame)
                if (swing.Follow(Frame, sent is int n ? new Busy(Handwork.Chop, default, n) : null).Begun) begun++;
        }
        Run(100, null);
        Assert.Equal(0, begun);
        Run(Frame, 1);
        Assert.Equal((1, 0.0), (begun, swing.Phase));
        // Still sent as the swing ends, the same swing is not swung again.
        Run(AxeSwing.Ms + 100, 1);
        Assert.Equal(1, begun);
        Assert.Null(swing.Phase);
        // The next sent while one plays waits for it to end; two passed meanwhile are swung as one.
        Run(Frame, 2);
        Run(100, 3);
        Assert.Equal(2, begun);
        Run(AxeSwing.Ms, 4);
        Assert.Equal(3, begun);
        Run(AxeSwing.Ms + 100, null);
        Assert.Null(swing.Phase);
        Assert.Equal(3, begun);
    }

    // Where it swings (0039).

    private static readonly Point Ahead = new(9_000, 7_000), Above = new(6_500, 4_000), Below = new(6_000, 10_000);

    [Fact]
    public void ASwingIsAimedAtThePointerAsItBeganFixedTillItEnds()
    {
        var swing = new AxeSwing();
        Assert.True(swing.Advance(Frame, true, Ahead).Begun);
        Assert.Equal(Ahead, swing.At);
        // The pointer moved, the button held or pressed again: the swing playing keeps its point.
        for (double t = Frame; t < AxeSwing.Ms - 2 * Frame; t += Frame) swing.Advance(Frame, true, Above);
        Assert.Equal(Ahead, swing.At);
        // Held as it ends, the next is aimed where the pointer is then.
        swing.Advance(3 * Frame, true, Below);
        Assert.Equal((2, Below), (swing.Count, swing.At));
    }

    // The arm (0040): straight through a swing, eased into and out of it.

    /// <summary>The axe's front arm resting afloat: bent a little at the elbow.</summary>
    private static readonly Bend Resting = new(-0.05, 0.35);

    /// <summary>One frame of a figure's front arm: the arm drawn, the swing's phase, the arm the swing alone would draw, and whether a swing began.</summary>
    private readonly record struct ArmFrame(Bend Drawn, double? Phase, Bend? Swung, bool Begun);

    /// <summary>
    /// The front arm drawn every <paramref name="frame"/> ms for <paramref name="ms"/>, as a figure draws it:
    /// the button wanted when <paramref name="wanted"/> says at that time, the pointer toward
    /// <paramref name="toward"/>'s point from the shoulder.
    /// </summary>
    private static List<ArmFrame> Arm(double ms, Func<double, bool> wanted, Func<double, Point> toward, double frame = Frame)
    {
        var swing = new AxeSwing();
        var arm = new SwingArm();
        double lead = AxeSwing.Lead(HeldLook.FireAxe);
        var drawn = new List<ArmFrame>();
        for (double t = 0; t < ms; t += frame)
        {
            bool begun = swing.Advance(frame, wanted(t), toward(t)).Begun;
            (double, double)? swung = swing.Phase is double phase ? (Math.Atan2(swing.At.Y, swing.At.X) + lead + AxeSwing.Lift(phase), phase) : null;
            drawn.Add(new(arm.Next(Resting, swung, frame), swing.Phase, swung is var (angle, _) ? AxeSwing.ArmAt(angle) : null, begun));
        }
        return drawn;
    }

    [Fact]
    public void SwingsTheArmStraightAboutTheShoulderAndStrikesWithTheSwingsOwnArm()
    {
        // Held from rest, three swings, toward a point ahead and a little below.
        var frames = Arm(3 * AxeSwing.Ms, _ => true, _ => new Point(3_000, 500));
        int downstroke = 0;
        for (int i = 0; i < frames.Count; i++)
        {
            var (drawn, phase, swung, _) = frames[i];
            // Through the downstroke the arm is the swing's own, so the blade drawn is the one that hits.
            if (phase is >= AxeSwing.WindShare and < AxeSwing.WindShare + AxeSwing.StrikeShare)
            {
                Assert.Equal(swung, drawn);
                downstroke++;
            }
            if (i * Frame < SwingArm.BlendMs) continue;
            // Once it has left the rest, the elbow is on the line from the shoulder to the hand, between them.
            var elbow = Rig.EndOf(drawn with { Lower = 0 }, Rig.Arm with { Lower = 0 });
            var hand = Rig.EndOf(drawn, Rig.Arm);
            Assert.Equal(0, elbow.X * hand.Y - elbow.Y * hand.X, 9);
            Assert.True(elbow.X * hand.X + elbow.Y * hand.Y > 0, $"frame {i}");
            Close(Rig.Arm.Upper + Rig.Arm.Lower, Math.Sqrt(hand.X * hand.X + hand.Y * hand.Y), 9);
        }
        Assert.InRange(downstroke, 9, 12);
    }

    /// <summary>
    /// Resting, a click, resting again; then held through a swing toward a point above and on into one
    /// toward a point below, let go during it, and resting: the arm drawn every <paramref name="frame"/> ms.
    /// </summary>
    private static List<ArmFrame> ClickThenHold(double frame)
    {
        Point above = new(2_000, -2_500), below = new(2_000, 2_500);
        return Arm(4_000, t => t is >= 300 and < 320 || t is >= 2_000 and < 2_700, t => t < 2_300 ? above : below, frame);
    }

    [Fact]
    public void NeverJumpsAsASwingBeginsEndsOrFollowsAnotherTowardANewPoint()
    {
        // At 60 fps, the frame one hands over to the other draws the arm where it was.
        var frames = ClickThenHold(Frame);
        int handOvers = 0;
        for (int i = 1; i < frames.Count; i++)
        {
            var (was, now) = (frames[i - 1].Drawn, frames[i]);
            if ((frames[i - 1].Phase is null) == (now.Phase is null) && !now.Begun) continue;
            handOvers++;
            Assert.True(Math.Abs(Poses.Apart(was.Upper, now.Drawn.Upper)) < 0.02 && Math.Abs(now.Drawn.Lower - was.Lower) < 0.02, $"frame {i}: {was} to {now.Drawn}");
        }
        // Begun from rest, ended, begun from rest, followed by another, ended.
        Assert.Equal(5, handOvers);
        Assert.Equal(3, frames.Count(f => f.Begun));
        Assert.Equal(Resting, frames[^1].Drawn);
        // And at a thousand frames a second it moves smoothly throughout, the downstroke too: never a jump.
        var fine = ClickThenHold(1);
        for (int i = 1; i < fine.Count; i++)
        {
            var (was, now) = (fine[i - 1].Drawn, fine[i].Drawn);
            Assert.True(Math.Abs(Poses.Apart(was.Upper, now.Upper)) < 0.05 && Math.Abs(now.Lower - was.Lower) < 0.05, $"at {i} ms: {was} to {now}");
        }
    }
}
