using Stillwake.Game.Features.Creatures;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The breakers of painted pieces (breakers-look): every picture drawn, a pair at work and the rest out from under the piece, their moments once each.</summary>
public class BreakerLooksTests
{
    private static readonly BreakerKind Knot = CreatureLooks.Breakers[Spec.Creatures.Breaker.Look];

    public static TheoryData<string> Looks() => [.. CreatureLooks.Breakers.Keys];

    [Theory]
    [MemberData(nameof(Looks))]
    public void EveryPictureItsRigPlacesIsDrawnAtFourTimes(string look)
    {
        var kind = CreatureLooks.Breakers[look];
        var rig = new BreakerRig(kind.Design, kind.Moves);
        rig.Place(new BreakerPose(1));
        Assert.All(rig.Slots, s => Assert.True(kind.Design.Parts.ContainsKey(s.Part), s.Part));
        foreach (var (name, part) in kind.Design.Parts)
        {
            var png = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art", "hd", "creatures", kind.Design.Id, name + ".png"));
            Assert.Equal((part.W * 4, part.H * 4), (png[18] << 8 | png[19], png[22] << 8 | png[23]));
        }
    }

    [Fact]
    public void ItsBallIsDrawnItsWidthWithinItsHitBox()
    {
        Assert.Equal(Knot.Across, Knot.Design.Parts[Knot.Body].W * Knot.Design.Sizes[Knot.Body] * Knot.Fit, 6);
        Assert.InRange(Knot.Across, Camera.Cell, Spec.Creatures.Breaker.Body.Height * Camera.Cell);
    }

    [Fact]
    public void ItTurnsWholeToItsHeroNeverFlipping()
    {
        var motion = new BreakerMotion(112, 0);
        double was = motion.Angle;
        for (int i = 0; i < 60; i++)
        {
            motion.Step(new(false, false, 0, 0, 0, (-30, 0)), 0.02);
            Assert.True(Math.Abs(Math.IEEERemainder(motion.Angle - was, Math.Tau)) <= BreakerMotion.TurnRate * 0.02 + 1e-9, $"a jump at frame {i}");
            was = motion.Angle;
        }
        Assert.True(Math.Cos(motion.Angle) < -0.95, "its arms at its hero");
    }

    [Fact]
    public void AtWorkItNeverSleeps()
    {
        var rig = new BreakerRig(Knot.Design, Knot.Moves);
        bool open = false, shut = false;
        var core = new List<double>();
        for (double t = 0; t < KnotMoves.BiteS; t += 0.05)
        {
            rig.Place(new BreakerPose(t, Work: 1));
            Assert.Equal(1, rig.Slots.Single(s => s.Part == "lamp").Alpha);
            Assert.Equal(0, rig.Slots.Single(s => s.Part == "dead").Alpha);
            open |= rig.Slots.Any(s => s.Part == "open" && s.Alpha > 0);
            shut |= rig.Slots.Any(s => s.Part == "shut" && s.Alpha > 0);
            core.Add(rig.Slots.Single(s => s.Part == "core").X);
        }
        Assert.True(open && shut);
        Assert.True(core.Max() - core.Min() > 2, "it yanks itself back");
    }

    [Fact]
    public void APairWorksApartOnTheRimWhateverThePiece()
    {
        const double HalfW = 24, HalfH = 16;
        Assert.Equal(2, BreakerPack.AtWork);
        for (int piece = 0; piece < 500; piece++)
        {
            var (a, b) = (BreakerPack.WorkerOf(piece, 0, HalfW, HalfH), BreakerPack.WorkerOf(piece, 1, HalfW, HalfH));
            Assert.True(Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y)) > 16, $"{piece}: one on the other");
            foreach (var one in new[] { a, b })
            {
                Assert.True(Math.Abs(one.X) > HalfW / 2 || Math.Abs(one.Y) > HalfH / 2, $"{piece}: over the picture's middle");
                double reachX = Math.Cos(one.Angle), reachY = Math.Sin(one.Angle);
                Assert.True(reachX * one.X + reachY * one.Y < 0, $"{piece}: its hands off the piece");
            }
        }
    }

    [Fact]
    public void TheRestComeOutOneAfterAnotherWithinASecond()
    {
        int rest = Spec.Creatures.Dismantlers.Most - BreakerPack.AtWork;
        for (int after = 1; after < rest; after++) Assert.Equal(0, BreakerPack.Out(BreakerPack.EachS * after, after));
        Assert.True(BreakerPack.Out(BreakerPack.EachS * 2, 0) > BreakerPack.Out(BreakerPack.EachS * 2, 1));
        Assert.Equal(1, BreakerPack.Out(1, rest - 1));
    }

    [Fact]
    public void AWindUpAndAHeroStruckAreEachOneMoment()
    {
        var motion = new BreakerMotion(112, 0);
        motion.Step(default, 0.02);
        var winding = new BreakerMotion.Seen(true, false, 0, 0, 0, (20, 0));
        Assert.Equal(BreakerMotion.Moment.WindUp, motion.Step(winding, 0.02));
        Assert.Equal(BreakerMotion.Moment.None, motion.Step(winding, 0.02));
        motion.Step(new(false, true, 4, 240, 0, (20, 0)), 0.02);
        Assert.Equal(BreakerMotion.Moment.HeroHit, motion.Step(new(false, false, 0, 0, 0, (8, 0)), 0.02));
        Assert.Equal(BreakerMotion.Moment.None, motion.Step(new(false, false, 0, 0, 0, (8, 0)), 0.02));
    }

    [Fact]
    public void ADartABlowCutStruckNoHero()
    {
        var motion = new BreakerMotion(112, 0);
        motion.Step(new(false, true, 4, 240, 0, (20, 0)), 0.02);
        Assert.Equal(BreakerMotion.Moment.None, motion.Step(new(false, false, 0, 0, 0, (20, 0), Blown: true), 0.02));
    }

    [Fact]
    public void KilledItComesApartToNothingWithItsLightOut()
    {
        var motion = new BreakerMotion(112, 0);
        motion.Step(default, 0.02);
        motion.Die((1, 0));
        var rig = new BreakerRig(Knot.Design, Knot.Moves);
        motion.Step(default, 0.02);
        rig.Place(motion.Pose);
        Assert.Equal(0, rig.Slots.Single(s => s.Part == "lamp").Alpha);
        Assert.True(rig.Slots.Single(s => s.Part == "dead").Alpha > 0);
        for (double t = 0; t < BreakerMotion.ApartS; t += 0.05) motion.Step(default, 0.05);
        rig.Place(motion.Pose);
        Assert.True(motion.Gone);
        Assert.All(rig.Slots, s => Assert.True(s.Alpha < 0.01, s.Part));
    }
}
