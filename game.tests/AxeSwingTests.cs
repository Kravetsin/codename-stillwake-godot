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
    private static readonly double HitMs = Overhand.HitAt * AxeSwing.Ms;

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
            if (played[i].Phase is double now && now >= Overhand.HitAt
                && (played[i - 1].Phase is not double was || was < Overhand.HitAt || was > now))
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
        // Three seconds: the first hit part way into the first swing, then one a swing.
        Assert.Equal((int)((180 * Frame - HitMs) / AxeSwing.Ms) + 1, hits.Count);
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
        Assert.InRange(whooshes[0], Strokes.Axe.WhooshMs - Frame, Strokes.Axe.WhooshMs + Frame);
        // Its peak falls in the downstroke, before the blade crosses the line.
        double peak = (Strokes.Axe.WhooshMs + Sound.SwingPeakS * 1000) / AxeSwing.Ms;
        Assert.InRange(peak, Overhand.WindShare, Overhand.HitAt);
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
        Assert.Equal([played.First(p => p.Phase >= Overhand.HitAt).At], Hits(played));
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
        // Put away and taken back, it still rests out its own pace; wanted after it, a new swing begins from the top.
        Assert.False(swing.Advance(Frame, true).Begun);
        swing.Advance(AxeSwing.Ms, false);
        Assert.True(swing.Advance(Frame, true).Begun);
        Assert.Equal((2, 0.0), (swing.Count, swing.Phase));
    }

    /// <summary>The overhand's turn from the aim line at <paramref name="phase"/>, shaped <paramref name="shape"/> (the whole swing by default).</summary>
    private static double Lift(double phase, (double Raised, double Through)? shape = null) =>
        MotionPlay.ShoulderAt(Overhand.KeysFor(shape ?? (Overhand.Raised, Overhand.Through), 0), phase);

    /// <summary>The arm's angle at <paramref name="phase"/> of <paramref name="stroke"/> pressed and played aimed <paramref name="aim"/>.</summary>
    private static double AngleAt(Stroke stroke, double aim, double phase) => MotionPlay.Of(stroke, aim).AngleAt(aim, phase);

    [Fact]
    public void TheBladeMeetsTheWreckWhenTheHitGoes()
    {
        Assert.Equal(0, Lift(Overhand.HitAt), 9);
        Assert.Equal(-Overhand.Raised, Lift(Overhand.WindShare), 9);
        Assert.True(Lift(Overhand.HitAt - 0.01) < 0);
        Assert.True(Lift(Overhand.HitAt + 0.01) > 0);
        // A swing begins and ends on the line to the wreck, so one held runs on into the next unbroken.
        Assert.Equal(0, Lift(0), 9);
        Assert.Equal(0, Lift(1), 9);
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(-60)]
    [InlineData(-30)]
    [InlineData(0)]
    [InlineData(45)]
    [InlineData(90)]
    public void ASwingAimedAnywhereAheadKeepsTheArmWithinTheShoulderAndStillMeetsItsLine(int degrees)
    {
        foreach (var (look, stroke) in new[] { (HeldLook.FireAxe, Strokes.Axe), (HeldLook.Sledgehammer, Strokes.Hammer), (HeldLook.Pickaxe, Strokes.Pick) })
        {
            double line = degrees * Math.PI / 180 + AxeSwing.Lead(look), highest = double.MinValue, lowest = double.MaxValue;
            for (double phase = 0; phase <= 1; phase += 0.002)
            {
                double upper = AxeSwing.ArmAt(AngleAt(stroke, degrees * Math.PI / 180, phase)).Upper;
                (highest, lowest) = (Math.Max(highest, upper), Math.Min(lowest, upper));
            }
            Assert.True(highest <= AxeSwing.Overhead + 1e-9, $"{degrees}°: raised to {highest * 180 / Math.PI:F0}°");
            Assert.True(lowest >= -AxeSwing.Behind - 1e-9, $"{degrees}°: brought through to {lowest * 180 / Math.PI:F0}°");
            // Raised less, it is still raised, and its blade crosses the line when the hit goes.
            Assert.True(highest > AxeSwing.ArmAt(line).Upper + 0.5);
            Assert.Equal(line, AngleAt(stroke, degrees * Math.PI / 180, Overhand.HitAt), 9);
        }
    }

    [Theory]
    [InlineData(-30)]
    [InlineData(0)]
    [InlineData(30)]
    public void ASwingAimedAheadSweepsItsWeaponsWholeFanTheHammersWiderThanTheAxes(int degrees)
    {
        foreach (var (look, stroke) in new[] { (HeldLook.FireAxe, Strokes.Axe), (HeldLook.Sledgehammer, Strokes.Hammer), (HeldLook.Pickaxe, Strokes.Pick) })
        {
            double line = degrees * Math.PI / 180 + AxeSwing.Lead(look), back = 0, through = 0;
            for (double phase = 0; phase <= 1; phase += 0.002)
            {
                double turn = AngleAt(stroke, degrees * Math.PI / 180, phase) - line;
                (back, through) = (Math.Min(back, turn), Math.Max(through, turn));
            }
            Assert.True(back <= -stroke.Half && through >= stroke.Half - 0.02, $"{look.Picture} at {degrees}°: {back:F2}..{through:F2}");
        }
        Assert.True(Strokes.Hammer.Half > Strokes.Axe.Half);
    }

    [Fact]
    public void TheWholeSwingStillFallsAtItsOwnSpeedAndAimedPlainlyIsTheAxesOld()
    {
        // The default shape is the swing as it always was: down at one speed across the line, through 0.3 by the downstroke's end.
        Assert.Equal(Overhand.Through, Lift(Overhand.WindShare + Overhand.StrikeShare), 9);
        // A longer follow-through carries on at that speed, not faster.
        var wide = (Overhand.Raised, 1.2);
        double dt = 0.001, after = Lift(Overhand.HitAt + dt, wide) / dt;
        Assert.Equal(Overhand.DownSpeed, after, 3);
    }

    [Fact]
    public void ASwingBegunOnTheRunEasesFromTheArmCarriedNotFromWhereItWouldRest()
    {
        var arm = new SwingArm();
        var rest = new Bend(0.1, 0.3);
        arm.Next(rest, null, Frame);
        var carried = Rig.Carried(0);
        arm.Drew(carried);
        var first = arm.Next(rest, (0.0, 0.0), Frame);
        // The first frame of the swing is the arm as it was drawn trailing, not a jump to the swing or the rest.
        Assert.Equal(carried.Upper, first.Upper, 1);
    }

    [Fact]
    public void EachToolKeepsItsOwnPaceSoTheAxeSwingsAtOnceAfterTheHammersBlowAndTheHammerStillWaitsItsOwn()
    {
        var swing = new AxeSwing();
        Assert.True(swing.Advance(Frame, true, stroke: Strokes.Hammer).Begun);
        double ms = Frame;
        while (!swing.Advance(Frame, false, stroke: Strokes.Hammer).Hits) ms += Frame;
        // The blow has landed; the hammer rests out its three seconds. The axe taken now swings the next frame.
        Assert.True(swing.Advance(Frame, true, stroke: Strokes.Axe).Begun);
        ms += 2 * Frame;
        Assert.Same(Strokes.Axe, swing.Stroke);
        // The axe swung out and the hammer taken back before its own pace is out: it waits, as it would have.
        while (swing.Phase != null) { swing.Advance(Frame, false, stroke: Strokes.Axe); ms += Frame; }
        Assert.True(ms < Strokes.Hammer.PaceMs);
        Assert.False(swing.Advance(Frame, true, stroke: Strokes.Hammer).Begun);
        ms += Frame;
        while (ms < Strokes.Hammer.PaceMs - Frame) { Assert.False(swing.Advance(Frame, true, stroke: Strokes.Hammer).Begun); ms += Frame; }
        Assert.True(swing.Advance(2 * Frame, true, stroke: Strokes.Hammer).Begun);
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
                Assert.True(swing.Phase >= Overhand.HitAt);
                Assert.True(before is not double was || was < Overhand.HitAt);
            }
        // Every swing begun has hit, but one still winding up.
        int unstruck = swing.Phase is double phase && phase < Overhand.HitAt ? 1 : 0;
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

    // The aim travels with the hero (swing-motions): a stroke keeps its offset from the hero, not its point of the world.

    private static readonly Hero Pressed = new(20_000, 30_000, 0, 0, false, 1);

    /// <summary>Wants a stroke toward <paramref name="at"/> by <paramref name="hero"/> for a frame.</summary>
    private static SwingStep Swing(ref AxeSwing swing, bool wanted, Point at, Hero hero) => swing.Advance(Frame, wanted, at, new Point(hero.X, hero.Y));

    /// <summary>The point <paramref name="hero"/>, where it is now, draws and strikes its stroke toward.</summary>
    private static Point Toward(AxeSwing swing, Hero hero) => swing.Toward(hero);

    /// <summary>The arm's angle at <paramref name="phase"/> of the axe's stroke along <paramref name="line"/> from the shoulder.</summary>
    private static double ArmAngle(AxeSwing swing, double line, double phase) => swing.Play!.AngleAt(line, phase);

    private static double LineOf(Point toward, Hero hero)
    {
        var hand = Aiming.HandOf(hero);
        return Math.Atan2(toward.Y - hand.Y, toward.X - hand.X);
    }

    [Fact]
    public void AHeroCarriedAwayMidSwingStrikesWithTheArmAndFanItWasPressedWith()
    {
        var swing = new AxeSwing();
        Swing(ref swing, true, new Point(Pressed.X + 2_500, Pressed.Y - 3_000), Pressed);
        double line = LineOf(Toward(swing, Pressed), Pressed);
        int heading = ToolRays.HeadingOf(Pressed, (Toward(swing, Pressed).X, Toward(swing, Pressed).Y));
        // Knocked three cells along and one down before the blade comes down.
        var hero = Pressed with { X = Pressed.X + 3_000, Y = Pressed.Y + 1_000 };
        while (!Swing(ref swing, false, default, hero).Hits) { }
        double phase = swing.Phase!.Value, now = LineOf(Toward(swing, hero), hero);
        Assert.Equal(ArmAngle(swing, line, phase), ArmAngle(swing, now, phase), 9);
        Assert.Equal(heading, ToolRays.HeadingOf(hero, (Toward(swing, hero).X, Toward(swing, hero).Y)));
    }

    [Fact]
    public void HeldEachStrokeTakesItsOffsetAfreshFromWhereTheHeroIsThen()
    {
        var swing = new AxeSwing();
        var pointer = new Point(Pressed.X + 4_000, Pressed.Y - 1_000);
        Swing(ref swing, true, pointer, Pressed);
        // Carried on while the pointer stays on one point of the world: the next stroke aims at it again, from there.
        var hero = Pressed with { X = Pressed.X + 3_000 };
        while (swing.Count < 2) Swing(ref swing, true, pointer, hero);
        Assert.Equal(pointer, Toward(swing, hero));
    }

    [Fact]
    public void AStrokeAimedAtTheHerosOwnPlaceGoesStraightAheadTheWayItFaces()
    {
        var swing = new AxeSwing();
        var left = Pressed with { Facing = -1 };
        Swing(ref swing, true, new Point(left.X, left.Y), left);
        Assert.Equal(default, swing.Aim);
        var hand = Aiming.HandOf(left);
        Assert.Equal(new Point(hand.X - Hero.Unit, hand.Y), swing.Toward(left));
        Assert.Equal(0, AxeSwing.AngleOf(swing.Aim));
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
        var drawn = new List<ArmFrame>();
        for (double t = 0; t < ms; t += frame)
        {
            bool begun = swing.Advance(frame, wanted(t), toward(t)).Begun;
            (double, double)? swung = swing.Phase is double phase ? (swing.Play!.AngleAt(AxeSwing.AngleOf(swing.Aim), phase), phase) : null;
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
            if (phase is >= Overhand.WindShare and < Overhand.WindShare + Overhand.StrikeShare)
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
