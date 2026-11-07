using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Strokes played as motions chosen by aim sector (swing-motions): today's swing and jab ported exactly.</summary>
public class SwingMotionsTests
{
    /// <summary>Today's swing, copied before it moved into the overhand: the reference the port must equal.</summary>
    private static class Reference
    {
        public const double WindShare = 0.35, StrikeShare = 0.12, Raised = 2.1, Through = 0.3;
        public const double HitAt = WindShare + StrikeShare * Raised / (Raised + Through);
        public const double Overhead = 3.6, Behind = 1.0, LeastRaised = 0.5, LeastThrough = 0.1;
        public const double DownSpeed = (Raised + Through) / StrikeShare;

        public static (double Raised, double Through) ShapeFor(double line, double half)
        {
            double level = Math.PI / 2 - line;
            return (Math.Clamp(Overhead - level, LeastRaised, Raised), Math.Clamp(level + Behind, LeastThrough, Math.Max(Through, half)));
        }

        public static double AngleAt(double line, double phase, double half) => line + Lift(phase, ShapeFor(line, half));

        public static double ShareAt(double turn, (double Raised, double Through) shape) => turn < 0
            ? WindShare + (HitAt - WindShare) * Math.Max(0, 1 + turn / shape.Raised)
            : HitAt + Math.Min(turn, shape.Through) / DownSpeed;

        public static double Lift(double phase, (double Raised, double Through)? shape = null)
        {
            var (raised, through) = shape ?? (Raised, Through);
            double over = HitAt + through / DownSpeed;
            if (phase < WindShare) return -raised * (1 - Math.Cos(phase / WindShare * Math.PI)) / 2;
            if (phase < HitAt) return -raised * (1 - (phase - WindShare) / (HitAt - WindShare));
            if (phase < over) return through * (phase - HitAt) / (over - HitAt);
            return through * (1 + Math.Cos((phase - over) / (1 - over) * Math.PI)) / 2;
        }

        /// <summary>The jab's elbow <paramref name="ms"/> into its 450 ms, as main drew it.</summary>
        public static double JabElbow(double ms)
        {
            const double Cocked = 1.75;
            double rest = HeldLook.Spear.Elbow;
            static double Smooth(double t) => t * t * (3 - 2 * t);
            static double Mix(double a, double b, double t) => a + (b - a) * t;
            if (ms < 150) return Mix(rest, Cocked, Smooth(ms / 150));
            if (ms < 215) return Mix(Cocked, 0, Math.Pow((ms - 150) / 65, 2));
            if (ms < 265) return 0;
            return ms < 450 ? Mix(0, rest, Smooth((ms - 265) / (450 - 265))) : rest;
        }
    }

    private static readonly Stroke[] Swung = [Strokes.Axe, Strokes.Pick, Strokes.Hammer];

    /// <summary>Aims every 5В° over the front half, in radians.</summary>
    private static IEnumerable<double> FrontAims() => Enumerable.Range(-18, 37).Select(d => d * 5 * Math.PI / 180);

    /// <summary>The arm's angle at <paramref name="phase"/> of <paramref name="stroke"/> aimed <paramref name="aim"/>, as played now.</summary>
    private static double ArmAngle(Stroke stroke, double aim, double phase) => MotionPlay.Of(stroke, aim).AngleAt(aim, phase);

    /// <summary>The share at which the edge is <paramref name="turn"/> from the aim line, as played now.</summary>
    private static double ShareAt(Stroke stroke, double aim, double turn) => MotionPlay.Of(stroke, aim).ShareAt(turn);

    /// <summary>The jab's elbow at <paramref name="phase"/>, as played now.</summary>
    private static double JabElbow(double phase) => MotionPlay.Of(Strokes.Spear, 0).ElbowAt(phase);

    [Fact]
    public void EverySwungWeaponPlaysTodaysSwingExactlyInTheSectorsItKeepsTheOverhand()
    {
        foreach (var stroke in Swung)
            foreach (double aim in FrontAims().Where(a => ReferenceEquals(stroke.Motions[Sectors.Of(a)], Overhand.Motion)))
            {
                double line = aim + AxeSwing.Lead(stroke.Look);
                for (int i = 0; i <= 100; i++)
                {
                    double phase = i / 100.0;
                    Assert.True(Math.Abs(Reference.AngleAt(line, phase, stroke.Half) - ArmAngle(stroke, aim, phase)) < 1e-9,
                        $"{stroke.Look.Picture} at {aim * 180 / Math.PI:F0}В°, phase {phase}");
                }
                var shape = Reference.ShapeFor(line, stroke.Half);
                foreach (double turn in new[] { -stroke.Half, stroke.Half, -0.3, 0.3, -2.0, 2.0 })
                    Assert.True(Math.Abs(Reference.ShareAt(turn, shape) - ShareAt(stroke, aim, turn)) < 1e-9,
                        $"{stroke.Look.Picture} at {aim * 180 / Math.PI:F0}В°, turn {turn}");
            }
    }

    [Fact]
    public void TheSpearsJabKeepsItsElbowCurve()
    {
        for (double ms = 0; ms <= Strokes.Spear.Ms; ms += 5)
            Assert.Equal(Reference.JabElbow(ms), JabElbow(ms / Strokes.Spear.Ms), 12);
    }

    // Rows: every weapon's motions.

    /// <summary>The strike key: the first after the arm is furthest back at which it is on the aim line again.</summary>
    private static MotionKey StrikeOf(IReadOnlyList<MotionKey> keys)
    {
        int back = 0;
        for (int i = 1; i < keys.Count; i++)
            if (keys[i].Shoulder < keys[back].Shoulder) back = i;
        return keys.Skip(back).First(k => k.Shoulder == 0);
    }

    /// <summary>Aims every half degree over the front half, in radians: every sector, its bounds and many between.</summary>
    private static IEnumerable<double> FineAims() => Enumerable.Range(-180, 361).Select(d => d * 0.5 * Math.PI / 180);

    /// <summary>
    /// Every row of every weapon, aimed anywhere ahead: at rest at shares 0 and 1, its elbow and wrist there the strike's; struck
    /// at its stroke's hit with the shoulder on the line, the elbow as held and the wrist straight; shares rising; the upper arm
    /// never past <see cref="Overhand.Overhead"/> nor <see cref="Overhand.Behind"/>; the edge's middle on the aim at the strike.
    /// </summary>
    [Fact]
    public void EveryRowRestsAtItsEndsStrikesOnTheAimAtItsHitAndKeepsTheArmWithinTheShoulder()
    {
        foreach (var stroke in new[] { Strokes.Axe, Strokes.Pick, Strokes.Hammer, Strokes.Spear })
        {
            foreach (double aim in FineAims())
            {
                var play = MotionPlay.Of(stroke, aim);
                string at = $"{stroke.Look.Picture} at {aim * 180 / Math.PI:F1}°, {play.Sector}";
                if (play.Motion.Kind == MotionKind.Thrust)
                {
                    Assert.Equal(play.ElbowAt(0), play.ElbowAt(1), 9);
                    continue;
                }
                var (keys, look) = (play.Keys, stroke.Look);
                var (first, last, strike) = (keys[0], keys[^1], StrikeOf(keys));
                Assert.True((first.Share, first.Shoulder, last.Share, last.Shoulder) == (0.0, 0.0, 1.0, 0.0), at);
                Assert.True((first.Elbow, last.Elbow, first.Wrist, last.Wrist) == (strike.Elbow, strike.Elbow, strike.Wrist, strike.Wrist), at);
                Assert.True(Math.Abs(stroke.HitShare - strike.Share) < 1e-12, at);
                Assert.True((strike.Elbow, strike.Wrist) == (look.Elbow, 0.0), at);
                for (int i = 1; i < keys.Count; i++) Assert.True(keys[i].Share > keys[i - 1].Share, at);
                foreach (double phase in Enumerable.Range(0, 1001).Select(i => i / 1000.0).Concat(keys.Select(k => k.Share)))
                {
                    double upper = AxeSwing.ArmAt(play.AngleAt(aim, phase)).Upper;
                    Assert.True(upper <= Overhand.Overhead + 1e-9 && upper >= -Overhand.Behind - 1e-9, $"{at}, phase {phase}: upper arm {upper:F3}");
                }
                double hit = stroke.HitShare;
                var (from, to) = AxeSwing.EdgeAt(look, play.AngleAt(aim, hit), play.ElbowAt(hit), play.WristAt(hit));
                double off = Math.IEEERemainder(Math.Atan2((from.Y + to.Y) / 2, (from.X + to.X) / 2) - aim, 2 * Math.PI);
                Assert.True(Math.Abs(off) < 1e-6, $"{at}: the edge off the aim by {off}");
            }
        }
    }

    [Fact]
    public void TheWristTurnsTheDrawnItemAndTheEdgeItHitsWithAlike()
    {
        // The axe aimed straight plays the chain; a third through, it is laid back, the wrist turned 0.55 up.
        double aim = 0.2, phase = 0.33;
        var play = MotionPlay.Of(Strokes.Axe, aim);
        var look = Strokes.Axe.Look;
        Assert.Same(FixedMotions.Chain, play.Motion);
        double wrist = play.WristAt(phase), angle = play.AngleAt(aim, phase), elbow = play.ElbowAt(phase);
        Assert.Equal(-0.55, wrist, 12);
        var arm = AxeSwing.ArmAt(angle, elbow);
        double drawn = Rig.HeldTurn(arm, look, wrist);
        Assert.Equal(wrist, Math.IEEERemainder(drawn - Rig.HeldTurn(arm, look), 2 * Math.PI), 12);
        // The fire axe's edge lies level in its picture: the edge hit with runs as the drawn item is turned, from where it is drawn.
        var (from, to) = AxeSwing.EdgeAt(look, angle, elbow, wrist);
        Assert.Equal(0, Math.IEEERemainder(Math.Atan2(to.Y - from.Y, to.X - from.X) - drawn, 2 * Math.PI), 9);
        Assert.Equal(Rig.HeldAt(arm, look, look.EdgeFrom, wrist), from);
        // And the edge sampled through the swing for the fan's sides is that one.
        double middle = Math.Atan2((from.Y + to.Y) / 2, (from.X + to.X) / 2);
        Assert.Equal(Math.IEEERemainder(middle - aim, 2 * Math.PI), MotionPlay.EdgeTurnAt(play.Keys, look, play.Lead, phase), 9);
    }

    [Fact]
    public void EaseInIsSlowOffTheKeyBeforeAndFastestIntoItsOwn()
    {
        Assert.Equal((0.0, 0.25, 1.0), (MotionPlay.Eased(Ease.In, 0), MotionPlay.Eased(Ease.In, 0.5), MotionPlay.Eased(Ease.In, 1)));
        MotionKey[] keys = [new(0, 0, 0), new(1, -2, 0, Ease: Ease.In)];
        Assert.Equal(-0.5, MotionPlay.ShoulderAt(keys, 0.5), 12);
    }

    // Sectors.

    private static readonly (Sector Sector, double From, double To)[] Table =
        [(Sector.Up, -90, -45), (Sector.Straight, -45, 20), (Sector.Down, 20, 60), (Sector.Under, 60, 90)];

    /// <summary>Any angle, many turns round either way.</summary>
    private static readonly Arbitrary<double> Angles = Gen.Choose(-200_000, 200_000).Select(i => i / 10_000.0).ToArbitrary();

    [Property(MaxTest = 500)]
    public Property AnyAngleFallsInTheSectorItsMirrorInTheFrontLiesIn() => Prop.ForAll(Angles, aim =>
    {
        double front = Math.Atan2(Math.Sin(aim), Math.Abs(Math.Cos(aim))) * 180 / Math.PI;
        var row = Table.Single(r => (r.From < front || r.Sector == Sector.Up) && front <= r.To);
        Assert.Equal(row.Sector, Sectors.Of(aim));
    });

    [Theory]
    [InlineData(-90, Sector.Up)]
    [InlineData(-45, Sector.Up)]
    [InlineData(-44.9, Sector.Straight)]
    [InlineData(20, Sector.Straight)]
    [InlineData(60, Sector.Down)]
    [InlineData(90, Sector.Under)]
    [InlineData(180 + 45, Sector.Up)]
    [InlineData(180 - 30, Sector.Down)]
    [InlineData(160, Sector.Straight)]
    [InlineData(120, Sector.Down)]
    [InlineData(-180, Sector.Straight)]
    public void ABoundBelongsToTheSmallerAngleAndTheBackHalfIsMirrored(double degrees, Sector sector) =>
        Assert.Equal(sector, Sectors.Of(degrees * Math.PI / 180));

    /// <summary>A motion of fixed keys, the overhand's own as aimed straight, for every sector: none such is played yet.</summary>
    private static readonly SwingMotion Fixed = new(MotionKind.Swing, 0.41, Overhand.KeysFor((Overhand.Raised, Overhand.Through), 0));

    private static readonly Stroke FixedAxe = Strokes.Axe with { Motions = SectorMotions.All(Fixed) };

    [Property(MaxTest = 300)]
    public Property AFixedMotionIsTurnedAtMostHalfItsSectorAndExactlyToAnAimAhead() => Prop.ForAll(Angles, aim =>
    {
        var play = MotionPlay.Of(FixedAxe, aim);
        foreach (double phase in new[] { 0, 0.2, Overhand.HitAt, 0.8 })
        {
            double line = play.AngleAt(aim, phase) - play.Lead - MotionPlay.ShoulderAt(play.Keys, phase);
            Assert.True(Math.Abs(line - Sectors.Middle(play.Sector)) <= Sectors.HalfSector(play.Sector) + 1e-12);
            if (Math.Abs(aim) <= Math.PI / 2) Assert.Equal(aim, line, 9);
        }
    });

    [Fact]
    public void AFixedMotionSamplesWhereItsEdgeCrossesTheFansSidesOnceAtThePress()
    {
        var play = MotionPlay.Of(FixedAxe, 0);
        var shape = (Overhand.Raised, Overhand.Through);
        foreach (double turn in new[] { -FixedAxe.Half, FixedAxe.Half })
            Assert.InRange(play.ShareAt(turn) - Overhand.ShareAt(turn, shape), 0, 0.01 + 1e-9);
    }

    [Fact]
    public void TheSectorAndItsMotionAreChosenAtThePressAndKeptWhereverTheAimGoesAfter()
    {
        var up = Fixed with { WhooshShare = 0.4 };
        var stroke = Strokes.Axe with { Motions = new SectorMotions(up, Overhand.Motion, Overhand.Motion, Overhand.Motion) };
        var swing = new AxeSwing();
        swing.Advance(Frame, true, new Point(1_000, -3_000), default, stroke, -1.2);
        Assert.Equal(Sector.Up, swing.Play!.Sector);
        Assert.Same(up, swing.Play.Motion);
        // The pointer brought level mid-stroke: the stroke plays on as it began.
        swing.Advance(Frame, true, new Point(3_000, 0), default, stroke, 0);
        Assert.Same(up, swing.Play!.Motion);
        // Held into the next, it is chosen afresh.
        while (swing.Count < 2) swing.Advance(Frame, true, new Point(3_000, 0), default, stroke, 0);
        Assert.Equal(Sector.Straight, swing.Play!.Sector);
        Assert.Same(Overhand.Motion, swing.Play.Motion);
    }

    [Fact]
    public void TheAngleGivenAtThePressChoosesTheMotionNotTheOffsetsOwn()
    {
        // Level behind by the offset, a straight chain; high by the figure's shoulder: the figure's angle picks the overhand and shapes it.
        var swing = new AxeSwing();
        swing.Advance(Frame, true, new Point(-3_000, 0), default, Strokes.Axe, -1.2);
        Assert.Equal(new Point(-3_000, 0), swing.Aim);
        Assert.Same(Overhand.Motion, swing.Play!.Motion);
        Assert.Equal(Overhand.ShapeFor(-1.2 + AxeSwing.Lead(Strokes.Axe.Look), Strokes.Axe.Half), swing.Play.Shape);
        Assert.NotEqual(Overhand.ShapeFor(AxeSwing.AngleOf(swing.Aim) + AxeSwing.Lead(Strokes.Axe.Look), Strokes.Axe.Half), swing.Play.Shape);
    }

    // Others' heroes: each swing aimed from the hero sent with it.

    private const double Frame = 1000.0 / 60;

    private static Hero RunnerAt(double ms) => new((int)(10_000 + ms * 8), 30_000, 8_000, 0, false, 1);

    /// <summary>
    /// Another's hero running right, sent every 50 ms and heard at once, pressing at <paramref name="presses"/>' times toward its
    /// offsets, each held its pace: every replay begun in <paramref name="ms"/>, when, its offset, the hero drawn then and the one last sent.
    /// </summary>
    private static List<(double At, Point Aim, Hero Shown, Hero Sent)> Replay(double ms, params (double At, Point Aim)[] presses)
    {
        var remote = new RemoteHeroes();
        var swing = new AxeSwing();
        HeardChop heard = default;
        var begun = new List<(double, Point, Hero, Hero)>();
        double nextSend = 0;
        Busy? busy = null;
        Hero sent = RunnerAt(0);
        for (double t = 0; t < ms; t += Frame)
        {
            while (nextSend <= t)
            {
                sent = RunnerAt(nextSend);
                int pressed = presses.Count(p => p.At <= nextSend);
                if (pressed > 0 && (busy is not { Swings: var n } || n < pressed))
                {
                    var aim = presses[pressed - 1].Aim;
                    busy = new Busy(Handwork.Chop, new Point(sent.X + aim.X, sent.Y + aim.Y), pressed);
                }
                var seen = new Seen(sent, Wields: Stillwake.Content.Weapons.Axe.Item, Busy: busy);
                remote.Add(2, new SentHero(seen, nextSend), nextSend);
                heard = Busy.Hear(heard, seen);
                nextSend += RemoteHeroes.SendMs;
            }
            if (swing.Follow(Frame, heard.Last, heard.From).Begun) begun.Add((t, swing.Aim, remote.At(t)[0].Seen.Hero, sent));
        }
        return begun;
    }

    [Fact]
    public void AReplayTakesItsOffsetFromTheHeroSentWithTheSwingNotTheOneDrawnBehind()
    {
        var aim = new Point(3_000, -500);
        var (_, played, shown, sent) = Replay(1000, (300, aim)).Single();
        Assert.Equal(aim, played);
        // The body drawn lags that packet by 150 ms, more than a cell back at a run: an offset from it would aim short.
        Assert.True(sent.X - shown.X >= 1_000, $"drawn at {shown.X}, sent at {sent.X}");
    }

    [Fact]
    public void AHeldRunOfThreeSwingsReplaysBackToBackEachWithItsOwnOffset()
    {
        Point a = new(3_000, -500), b = new(-2_500, 1_000), c = new(1_500, -4_000);
        var begun = Replay(2500, (300, a), (300 + Strokes.Axe.Ms, b), (300 + 2 * Strokes.Axe.Ms, c));
        Assert.Equal([a, b, c], begun.Select(x => x.Aim));
        for (int i = 1; i < begun.Count; i++) Assert.InRange(begun[i].At - begun[i - 1].At, Strokes.Axe.Ms - 2 * Frame, Strokes.Axe.Ms + 2 * Frame);
    }

    [Fact]
    public void ASwingHeardWhileTheLastStillPlaysIsAimedFromThePacketThatBroughtItNotTheOnesAfter()
    {
        // The second arrives 400 ms into the first's 500 and waits: packets meanwhile carry the hero on, not its swing.
        Point a = new(3_000, -500), b = new(-2_500, 1_000);
        var begun = Replay(1500, (300, a), (700, b));
        Assert.Equal([a, b], begun.Select(x => x.Aim));
        Assert.True(begun[1].At - 700 >= RemoteHeroes.SendMs, $"begun at {begun[1].At}");
    }

    [Fact]
    public void AReplayWhoseBodyFacesAwaySwingsTowardItsOffsetsSide()
    {
        var right = new Hero(10_000, 30_000, 0, 0, false, 1);
        Assert.Equal(-1, AxeSwing.FaceOf(new Point(-2_000, 0), right));
        Assert.Equal(1, AxeSwing.FaceOf(new Point(2_000, 500), right with { Facing = -1 }));
        // Nearly straight above or below, it keeps the way it faces.
        Assert.Equal(-1, AxeSwing.FaceOf(new Point(200, -3_000), right with { Facing = -1 }));
    }
}
