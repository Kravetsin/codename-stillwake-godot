using System;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The luminous effects' arithmetic (0075): colours cooling, brightness fading, paths curling, streaks, flares.</summary>
public class LuminousTests
{
    private const double Frame = 1000.0 / 60;

    /// <summary>Halfway through every range: the same particle each time.</summary>
    private static Glims Middle() => new(100, () => 0.5);

    [Fact]
    public void AColourRunsAlongItsRampFromTheFirstToTheLast()
    {
        (int, int, int)[] ramp = [(255, 255, 255), (255, 128, 0), (100, 0, 0)];
        Assert.Equal((255d, 255d, 255d), LuminousMath.RampAt(ramp, 0));
        Assert.Equal((255d, 128d, 0d), LuminousMath.RampAt(ramp, 0.5));
        Assert.Equal((255d, 191.5d, 127.5d), LuminousMath.RampAt(ramp, 0.25));
        var end = LuminousMath.RampAt(ramp, 1);
        Assert.Equal(100, end.R, 0);
        Assert.Equal((255d, 255d, 255d), LuminousMath.RampAt([], 0.7));
    }

    [Fact]
    public void AParticleDimsAsItAgesAndIsGoneAtTheEndOfItsLife()
    {
        var glims = Middle();
        var g = glims.Add(Luminous.HitSpark, 0, 0, 100, 0)!;
        double last = LuminousMath.Brightness(g);
        Assert.Equal(1, last, 6);
        for (int i = 0; i < 20; i++)
        {
            glims.Step(Frame);
            if (glims.Live.Count == 0) break;
            double now = LuminousMath.Brightness(g);
            Assert.True(now < last);
            last = now;
        }
        // A hit's spark lives 250 to 650 ms: 450 at the middle.
        glims.Step(450);
        Assert.Empty(glims.Live);
    }

    [Fact]
    public void CurlingTurnsAStreakWithoutSpeedingItUpOrSlowingIt()
    {
        // Spin alone, no drag, no pull: its way turns, its speed stays.
        var look = new Luminous(Glimmer.Streak, [(255, 255, 255)], (10_000, 10_000)) { Spin = 6, NoiseScale = 0.3 };
        var glims = Middle();
        var g = glims.Add(look, 0, 0, 80, 0)!;
        double turned = 0;
        for (int i = 0; i < 120; i++)
        {
            glims.Step(Frame);
            Assert.Equal(80, Math.Sqrt(g.Vx * g.Vx + g.Vy * g.Vy), 6);
            turned = Math.Max(turned, Math.Abs(Math.Atan2(g.Vy, g.Vx)));
        }
        Assert.True(turned > 0.05, $"turned {turned}");
    }

    [Fact]
    public void WithoutSpinTurbulenceDragOrPullAStreakFliesStraight()
    {
        var look = new Luminous(Glimmer.Streak, [(255, 255, 255)], (10_000, 10_000));
        var glims = Middle();
        var g = glims.Add(look, 0, 0, 60, -30)!;
        for (int i = 0; i < 60; i++) glims.Step(Frame);
        Assert.Equal(60, g.X, 6);
        Assert.Equal(-30, g.Y, 6);
    }

    [Fact]
    public void TheCurlFieldSwirlsAlongTheNoiseNeverUpIt()
    {
        // The curl is the noise's slope, over the curl's own step, turned a quarter: square to it everywhere, so it never
        // gathers motes in a hollow; and it is not nothing.
        const double e = 0.35;
        double most = 0;
        for (double x = 0.1; x < 6; x += 0.73)
            for (double y = 0.2; y < 6; y += 0.61)
            {
                double gx = (LuminousMath.Noise(x + e, y) - LuminousMath.Noise(x - e, y)) / (2 * e);
                double gy = (LuminousMath.Noise(x, y + e) - LuminousMath.Noise(x, y - e)) / (2 * e);
                var (cx, cy) = LuminousMath.Curl(x, y, 0);
                Assert.Equal(0, cx * gx + cy * gy, 9);
                Assert.Equal(Math.Sqrt(gx * gx + gy * gy), Math.Sqrt(cx * cx + cy * cy), 9);
                most = Math.Max(most, Math.Abs(cx) + Math.Abs(cy));
                Assert.InRange(LuminousMath.Noise(x, y), 0, 1);
            }
        Assert.True(most > 0.1);
    }

    [Fact]
    public void AStreakIsDrawnAlongItsOwnPathAsLongAsItsSpeedSaysHeadFirst()
    {
        var look = new Luminous(Glimmer.Streak, [(255, 255, 255)], (10_000, 10_000)) { Stretch = 0.1, Shortest = 0.5, Longest = 40 };
        var glims = Middle();
        var g = glims.Add(look, 0, 0, 120, 0)!;
        Span<double> xs = stackalloc double[Glim.Kept + 2], ys = stackalloc double[Glim.Kept + 2];
        // Young, it has no path yet: drawn back along its way a little, never a dot.
        int young = LuminousMath.StreakPoints(g, LuminousMath.LengthOf(g), xs, ys);
        Assert.Equal(2, young);
        Assert.True(xs[1] < xs[0]);
        for (int i = 0; i < 30; i++) glims.Step(Frame);
        double length = LuminousMath.LengthOf(g);
        Assert.Equal(12, length, 6);
        int n = LuminousMath.StreakPoints(g, length, xs, ys);
        Assert.Equal(g.X, xs[0]);
        double along = 0;
        for (int i = 1; i < n; i++) along += Math.Abs(xs[i] - xs[i - 1]);
        Assert.Equal(length, along, 6);
    }

    [Fact]
    public void ASparkBouncesOffTheFloorItWasGiven()
    {
        var glims = Middle();
        var g = glims.Add(Luminous.CrackSpark, 0, 0, 0, 200, floor: 10)!;
        for (int i = 0; i < 30; i++)
        {
            glims.Step(Frame);
            Assert.True(g.Y <= 10);
        }
    }

    [Fact]
    public void NeverMoreThanTheLimitFlyAtOnce()
    {
        var glims = new Glims(5, () => 0.5);
        glims.Spray(Luminous.Ember, 0, 0, 8, 0, (0, 0), (1, 1));
        Assert.Equal(5, glims.Live.Count);
    }

    [Fact]
    public void AGlintIsWholeOnItsFirstFrameAndShowsOnceWhateverTheFrameRate()
    {
        var flares = new Flares();
        flares.Glint(0, 0, 6, (255, 255, 255));
        var glint = flares.Glints[0];
        // A slow frame ages it past its hold before any draw: it still shows, whole, once.
        flares.Step(50);
        Assert.Single(flares.Glints);
        Assert.Equal(1, glint.Strength);
        glint.Drawn = true;
        flares.Step(Frame);
        Assert.Empty(flares.Glints);
    }

    [Fact]
    public void AShockLineSpreadsFastAtFirstAndFadesAsItGoes()
    {
        var flares = new Flares();
        flares.Shock(0, 0, 2, 17, 11 * Frame, (255, 255, 255), 0.35);
        var shock = flares.Shocks[0];
        flares.Step(11 * Frame / 2);
        Assert.True(shock.Radius > 2 + 15 / 2.0);
        Assert.Equal(0.35 / 2, shock.Alpha, 6);
        flares.Step(11 * Frame / 2);
        Assert.Empty(flares.Shocks);
    }

    [Fact]
    public void AFlashDiesAwayAlikeAtAnyFrameRateAndAHeldLightStaysWhileItIsSet()
    {
        var a = new Flares();
        var b = new Flares();
        a.Flash(0, 0, (255, 255, 255), 1, 2, 0.5f, 0.84);
        b.Flash(0, 0, (255, 255, 255), 1, 2, 0.5f, 0.84);
        a.Step(Frame);
        b.Step(Frame / 2);
        b.Step(Frame / 2);
        Assert.Equal(0.84, a.Flashes[0].Strength, 6);
        Assert.Equal(a.Flashes[0].Strength, b.Flashes[0].Strength, 6);
        for (int i = 0; i < 60; i++) a.Step(Frame);
        Assert.Empty(a.Flashes);

        var key = new object();
        for (int i = 0; i < 30; i++)
        {
            a.Hold(key, 1, 2, (255, 120, 40), 0.5, 2, 0.3f, 0.6);
            a.Step(Frame);
        }
        Assert.Single(a.Flashes);
        for (int i = 0; i < 30; i++) a.Step(Frame);
        Assert.Empty(a.Flashes);
    }

    [Fact]
    public void ASwingThatBitIsDrawnStoppedOnContactAndCatchesUpByItsEnd()
    {
        double stop = AxeSwing.StopMs / AxeSwing.Ms;
        Assert.Equal(0.2, AxeSwing.Shown(0.2), 9);
        Assert.Equal(AxeSwing.HitAt, AxeSwing.Shown(AxeSwing.HitAt + stop / 2), 9);
        Assert.Equal(AxeSwing.HitAt, AxeSwing.Shown(AxeSwing.HitAt + stop), 9);
        Assert.Equal(1, AxeSwing.Shown(1), 9);
        // Never backwards, never a jump.
        double last = 0;
        for (double p = 0; p <= 1; p += 0.001)
        {
            double shown = AxeSwing.Shown(p);
            Assert.True(shown >= last - 1e-12);
            Assert.True(shown - last < 0.01);
            last = shown;
        }
    }

    [Fact]
    public void ATrailOrABloomStampedIsDrawnTheFrameItWasStampedForAndGoneTheNext()
    {
        var flares = new Flares();
        flares.Trail([(0, 0), (4, 0), (8, 2)], (255, 255, 255), 0.5, 0.8, 0.3);
        flares.Trail(0, 0, 1, 1, (255, 255, 255), 0.5, 0.003, 0.3);
        flares.Bloom(1, 2, 6, (255, 200, 140), 0.5, 0.3);
        Assert.Empty(flares.Trails);
        flares.TakeFrame();
        // The faint one is nothing; the other's points in order, head first.
        var trail = Assert.Single(flares.Trails);
        Assert.Equal([(0d, 0d), (4d, 0d), (8d, 2d)], flares.TrailPoints.Skip(trail.From).Take(trail.Count));
        Assert.Equal(new Bloom(1, 2, 6, (255, 200, 140), 0.5, 0.3), Assert.Single(flares.Blooms));
        flares.TakeFrame();
        Assert.Empty(flares.Trails);
        Assert.Empty(flares.TrailPoints);
        Assert.Empty(flares.Blooms);
    }

    [Fact]
    public void AnArcRunsEndToEndHoldsItsShapeTwoFramesFollowsItsEndsAndIsGoneAfterItsLife()
    {
        int calls = 0;
        var bolts = new Bolts(() => (calls++ * 0.37) % 1);
        double x1 = 20;
        var bolt = bolts.Add(() => (0, 0, x1, 0, 0.2), 6)!;
        var points = new (double X, double Y)[65];
        int n = bolt.PointsOf(0, 0, 0, x1, 0, points);
        // Folded by its length: 20 pixels, four folds, seventeen points from end to end.
        Assert.Equal(17, n);
        Assert.Equal((0d, 0d), points[0]);
        Assert.Equal((20d, 0d), points[n - 1]);
        Assert.Contains(points.Take(n), p => Math.Abs(p.Y) > 0.5);
        var held = points.Take(n).ToArray();
        bolts.Step(Frame);
        bolt.PointsOf(0, 0, 0, x1, 0, points);
        Assert.Equal(held, points.Take(n));
        // Its ends moved, the held shape moves with them; rolled on the second frame, a new one.
        bolt.PointsOf(0, 10, 10, 10 + x1, 10, points);
        Assert.Equal(held.Select(p => (p.X + 10, p.Y + 10)), points.Take(n));
        bolts.Step(Frame);
        bolt.PointsOf(0, 0, 0, x1, 0, points);
        Assert.NotEqual(held, points.Take(n));
        for (int i = 0; i < 3; i++) bolts.Step(Frame);
        Assert.Single(bolts.Live);
        bolts.Step(Frame);
        Assert.Empty(bolts.Live);
    }

    [Fact]
    public void AnArcKeepsItsLastShapesNewestFirstForItsAfterimageAtMostItsGhosts()
    {
        int calls = 0;
        var bolts = new Bolts(() => (calls++ * 0.37) % 1);
        var bolt = bolts.Add(() => (0, 0, 20, 0, 0), 100, branchy: true)!;
        var points = new (double X, double Y)[65];
        Assert.Equal(0, bolt.Ghosts);
        var first = points.Take(bolt.PointsOf(0, 0, 0, 20, 0, points)).ToArray();
        bolts.Step(Frame * Bolts.Hold);
        Assert.Equal(1, bolt.Ghosts);
        // Its own line, not its branches, as it was before the roll.
        Assert.Equal(first, points.Take(bolt.GhostOf(1, 0, 0, 20, 0, points)));
        for (int i = 0; i < 10; i++) bolts.Step(Frame * Bolts.Hold);
        Assert.Equal(Bolts.Ghosts, bolt.Ghosts);
    }

    [Fact]
    public void AFineArcIsFourSegmentsHoweverShortAndRolledAnewEveryFrame()
    {
        int calls = 0;
        var bolts = new Bolts(() => (calls++ * 0.37) % 1);
        var fine = bolts.Add(() => (0, 0, 2, 0, -0.3), 6, fine: true)!;
        var plain = bolts.Add(() => (0, 0, 2, 0, -0.3), 6)!;
        var points = new (double X, double Y)[65];
        // Two pixels long: a plain arc is folded once, a fine one twice.
        Assert.Equal(3, plain.PointsOf(0, 0, 0, 2, 0, points));
        int n = fine.PointsOf(0, 0, 0, 2, 0, points);
        Assert.Equal(5, n);
        var held = points.Take(n).ToArray();
        bolts.Step(Frame);
        fine.PointsOf(0, 0, 0, 2, 0, points);
        Assert.NotEqual(held, points.Take(n));
    }

    [Fact]
    public void ArcsPastTheLimitAreNotMadeAndAStallRollsAFewFramesNotAllOfIt()
    {
        var bolts = new Bolts(() => 0.5);
        for (int i = 0; i < Bolts.Limit; i++) Assert.NotNull(bolts.Add(() => (0, 0, 5, 0, 0), 100));
        Assert.Null(bolts.Add(() => (0, 0, 5, 0, 0), 100));
        bolts.Step(10_000);
        Assert.All(bolts.Live, b => Assert.Equal(6, b.Age));
    }

    [Fact]
    public void AFloorRingIsSquashedFlatAndAnyOtherIsRound()
    {
        var flares = new Flares();
        flares.Shock(0, 0, 2, 10, 100, (255, 255, 255), 0.3);
        flares.Shock(0, 0, 2, 10, 100, (255, 255, 255), 0.3, squash: 0.16);
        Assert.Equal([1, 0.16], flares.Shocks.Select(s => s.Squash));
    }
}
