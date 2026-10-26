using Stillwake.Game.Features.Creatures;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The tick, the sentry's look (its page's candidate 3): its sprite drawn from its pose as the page draws it, and its motion read
/// off what the sim says, its moments coming once each.
/// </summary>
public class TickTests
{
    private static readonly TickArt.Pose Standing = new(0, 0, 0, 0, TickMotion.Watching, 1, 0);

    /// <summary>The opaque pixels of <paramref name="rgba"/>, in the tick's own pixels (origin the middle of its feet).</summary>
    private static List<(int X, int Y)> Opaque(byte[] rgba)
    {
        var found = new List<(int, int)>();
        for (int i = 0; i < TickArt.Width * TickArt.Height; i++)
            if (rgba[i * 4 + 3] > 0) found.Add((i % TickArt.Width - TickArt.OriginX, i / TickArt.Width - TickArt.OriginY));
        return found;
    }

    [Fact]
    public void StandingItFillsItsThreeByTwoCellsOnItsFeet()
    {
        var art = new TickArt();
        art.Draw(Standing);
        var drawn = Opaque(art.Albedo);
        Assert.InRange(drawn.Min(p => p.X), -14, -12);
        Assert.InRange(drawn.Max(p => p.X), 8, 12);
        Assert.InRange(drawn.Min(p => p.Y), -16, -10);
        Assert.Equal(0, drawn.Max(p => p.Y));
        Assert.Equal(drawn, Opaque(art.Silhouette));
    }

    [Fact]
    public void ItsEyeGlowsItsColourAndNothingElseDoesAlive()
    {
        var art = new TickArt();
        var at = art.Draw(Standing with { Eye = TickMotion.Attacking });
        var lit = Opaque(art.Emission);
        Assert.Equal(3, lit.Count);
        Assert.All(lit, p => Assert.True(Math.Abs(p.X - at.Eye.X) <= 1.5 && Math.Abs(p.Y - at.Eye.Y) <= 1.5, $"{p} glows away from its eye"));
        int i = ((int)at.Eye.Y + TickArt.OriginY) * TickArt.Width + (int)at.Eye.X + TickArt.OriginX;
        Assert.True(art.Emission[i * 4] > art.Emission[i * 4 + 1], "a red eye glows red");
    }

    [Fact]
    public void FlyingItFoldsItsLegsUpOffTheGround()
    {
        var art = new TickArt();
        art.Draw(Standing with { Fly = 1 });
        Assert.True(Opaque(art.Albedo).Max(p => p.Y) < 0, "its feet still reach the ground in flight");
    }

    [Fact]
    public void WalkingItsFeetStepAndStandingTheyDoNot()
    {
        var art = new TickArt();
        var still = art.Draw(Standing).Feet.ToList();
        Assert.Equal(still, art.Draw(Standing with { Gait = 2 }).Feet);
        var a = art.Draw(Standing with { Walk = 1, Gait = 0 }).Feet.ToList();
        var b = art.Draw(Standing with { Walk = 1, Gait = 2 }).Feet.ToList();
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ItsCracksSpreadAsItDies()
    {
        var art = new TickArt();
        art.Draw(Standing);
        int alive = Opaque(art.Emission).Count;
        art.Draw(Standing with { Cracks = 0.5 });
        int half = Opaque(art.Emission).Count;
        art.Draw(Standing with { Cracks = 1 });
        Assert.True(alive < half && half < Opaque(art.Emission).Count, "its cracks do not spread");
    }

    private static TickMotion.Seen Watching => new(true, false, false, 0, false, 0, 0, 0, 0, (40, 0));

    private static TickMotion.Moment Run(TickMotion motion, TickMotion.Seen seen, double seconds)
    {
        var all = TickMotion.Moment.None;
        for (double t = 0; t < seconds; t += 1 / 60.0) all |= motion.Step(seen, 1 / 60.0);
        return all;
    }

    [Fact]
    public void ItsMomentsComeOnceEachAsTheSimMovesIt()
    {
        var motion = new TickMotion(() => 0.5);
        Assert.Equal(TickMotion.Moment.None, Run(motion, Watching, 0.5));
        Assert.Equal(TickMotion.Moment.Notice, Run(motion, Watching with { Alerted = true }, 0.5));
        Assert.Equal(TickMotion.Moment.WindUp, Run(motion, Watching with { WindingUp = true, WindUpS = 0.4 }, 0.5));
        var flight = Watching with { Clamped = false, Pouncing = true, FlightLeft = 9, Vx = 190, Vy = -40 };
        Assert.Equal(TickMotion.Moment.Unclamp, Run(motion, flight, 0.3));
        Assert.Equal(TickMotion.Moment.Brake | TickMotion.Moment.HeroHit, Run(motion, flight with { Pouncing = false, Vx = 0, Vy = 0 }, 0.2));
        Assert.Equal(TickMotion.Moment.Clamp, Run(motion, Watching, 0.5));
        Run(motion, flight with { FlightLeft = 1 }, 0.3);
        Assert.Equal(TickMotion.Moment.Brake, Run(motion, flight with { Pouncing = false, FlightLeft = 0 }, 0.2));
    }

    [Fact]
    public void ItsEyeIsTealWatchingYellowAlertedRedActing()
    {
        var motion = new TickMotion(() => 0.5);
        Run(motion, Watching, 1);
        Assert.Equal(TickMotion.Watching, motion.Pose.Eye, Near);
        Run(motion, Watching with { Alerted = true }, 1);
        Assert.Equal(TickMotion.Alerted, motion.Pose.Eye, Near);
        Run(motion, Watching with { WindingUp = true, WindUpS = 0.6 }, 1);
        Assert.Equal(TickMotion.Attacking, motion.Pose.Eye, Near);
        Assert.True(motion.Hot);
        Assert.InRange(motion.Pose.Crouch, 0.95, 1.05);
    }

    [Fact]
    public void FlyingItFacesAndNosesAlongItsFlightUnderItsBurner()
    {
        var motion = new TickMotion(() => 0.5);
        Run(motion, Watching, 0.2);
        Run(motion, Watching with { Clamped = false, Vx = -100, Vy = 100 }, 0.5);
        Assert.Equal(-1, motion.Face);
        // Its nose (its own +x, mirrored) along its flight, down and left.
        Assert.InRange(motion.Angle, -Math.PI / 4 - 0.05, -Math.PI / 4 + 0.05);
        Assert.InRange(motion.Thrust, 0.7, 0.8);
        Assert.InRange(motion.Pose.Fly, 0.95, 1);
        Assert.Equal(0, motion.Mags);
        Assert.True(motion.Plume.X > 0 && motion.Plume.Y < 0, "its plume does not trail behind it");
    }

    [Fact]
    public void ClampedItsLegsGripAndItsBurnerDies()
    {
        var motion = new TickMotion(() => 0.5);
        Run(motion, Watching with { Clamped = false, Vx = 100 }, 0.5);
        Run(motion, Watching, 0.6);
        Assert.InRange(motion.Pose.Fly, 0, 0.05);
        Assert.InRange(motion.Thrust, 0, 0.05);
        Assert.Equal(0, motion.Angle, 2);
        Assert.Equal(1, motion.Mags);
    }

    [Fact]
    public void CrawlingAlongItsWreckItsLegsWalkAndItFacesItsWay()
    {
        var motion = new TickMotion(() => 0.5);
        Run(motion, Watching with { Hero = null, Crawl = -5 }, 1);
        Assert.Equal(-1, motion.Face);
        Assert.True(motion.Pose.Walk > 0.9, $"walks {motion.Pose.Walk:F2}");
        double gait = motion.Pose.Gait;
        Run(motion, Watching with { Hero = null, Crawl = -5 }, 1);
        Assert.InRange(motion.Pose.Gait - gait, 7.9, 8.2);
        Run(motion, Watching with { Hero = null }, 1);
        Assert.True(motion.Pose.Walk < 0.01);
    }

    [Fact]
    public void StruckItFlashesWhiteAMomentAndKilledItsCracksSpreadTillItBursts()
    {
        var motion = new TickMotion(() => 0.5);
        Run(motion, Watching, 0.2);
        motion.Struck();
        Run(motion, Watching, 1 / 60.0);
        Assert.True(motion.Flash > 0.5);
        Run(motion, Watching, 0.2);
        Assert.Equal(0, motion.Flash);
        motion.Die();
        Run(motion, default, TickMotion.CracksS / 2);
        Assert.InRange(motion.Pose.Cracks, 0.4, 0.6);
        Run(motion, default, TickMotion.CracksS);
        Assert.Equal(1, motion.Pose.Cracks);
        Assert.True(motion.Dying >= TickMotion.CracksS);
    }

    [Fact]
    public void CuttingALineItFacesItRearsAndSawsAtItThenRecoilsAsItSnaps()
    {
        var motion = new TickMotion(() => 0.5);
        Run(motion, Watching, 0.5);
        Assert.Equal(1, motion.Face);
        var cutting = Watching with { Cutting = true, CutDone = 0.5, Line = (-60, -10) };
        Run(motion, cutting, 0.4);
        Assert.Equal(-1, motion.Face);
        Assert.Equal(0.5, motion.Cut);
        Assert.True(motion.Hot);
        Assert.InRange(motion.Pose.Crouch, -0.55, -0.45);
        Assert.True(motion.Pose.Saw > 0.6, $"saws {motion.Pose.Saw:F2}");
        Assert.True(motion.Angle > 0.1, "it does not rear its head up toward the line");
        Assert.Equal(TickMotion.Attacking, motion.Pose.Eye, Near);
        motion.Snipped();
        Run(motion, Watching, 1 / 60.0);
        Assert.Null(motion.Cut);
        Assert.True(motion.Shake > 1, $"no recoil: {motion.Shake:F2}");
        Run(motion, Watching, 0.6);
        Assert.InRange(motion.Pose.Saw, 0, 0.01);
        Assert.Equal(0, motion.Shake, 2);
    }

    [Fact]
    public void ItsClawRisesToTheLineItSaws()
    {
        var art = new TickArt();
        var standing = art.Draw(Standing);
        var sawing = art.Draw(Standing with { Saw = 1, Jaw = 1 });
        Assert.Equal((10, -1), standing.Cut);
        Assert.True(sawing.Cut.Y < -3 && sawing.Cut.X >= 9, $"its claw at {sawing.Cut}");
    }

    private static readonly IEqualityComparer<TickArt.Rgb> Near = EqualityComparer<TickArt.Rgb>.Create(
        (a, b) => Math.Abs(a.R - b.R) < 2 && Math.Abs(a.G - b.G) < 2 && Math.Abs(a.B - b.B) < 2, _ => 0);
}
