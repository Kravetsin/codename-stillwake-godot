using Stillwake.Game.Features.Debris;
using Stillwake.Game.Shared;
using Xunit;
using Spec = Stillwake.Content.Harpoon;

namespace Stillwake.Game.Tests;

/// <summary>
/// The host player's bodies (debris-physics): the harpoon's line as a rope with a brake and a drum, the bodies' size and weight, and
/// which of them the feed sends.
/// </summary>
public class PieceRopeTests
{
    private const double Dt = 1.0 / 60;

    /// <summary>A body hooked at its middle, moving along x, its rope run a step: the body moved by the impulse and its motion.</summary>
    private static (double X, double Vx) Pull(PieceRope rope, double x, double vx, double mass, (double X, double Y) anchor = default,
        double anchorVx = 0, bool winds = false)
    {
        var body = new RopeBody(x, 0, vx, 0, 0, mass, mass * 1_000_000);
        if (rope.Step(Dt, anchor, (anchorVx, 0), (x, 0), body, winds) is { } impulse) vx += impulse.X / mass;
        return (x + vx * Dt, vx);
    }

    [Fact]
    public void AHeavyPieceFlyingOffPaysTheLineOutUnderTheBrakeToPayOutThenItHolds()
    {
        var rope = new PieceRope(10_000);
        double x = 10_000, vx = 30_000, farthest = 0;
        for (int i = 0; i < 600; i++)
        {
            (x, vx) = Pull(rope, x, vx, 100);
            farthest = Math.Max(farthest, x);
        }
        Assert.Equal(Spec.PayOut, rope.Paid, 6);
        Assert.Equal(10_000, rope.Wound, 6);
        // Paid out, it stops the piece within a step's flight of its whole length.
        Assert.InRange(farthest, rope.Length, rope.Length + 30_000 * Dt);
    }

    [Fact]
    public void ALightPiecePulledIsHeldWithoutPayingOut()
    {
        var rope = new PieceRope(10_000);
        double x = 10_000, vx = 2_000, farthest = 0;
        for (int i = 0; i < 120; i++)
        {
            (x, vx) = Pull(rope, x, vx, 0.5);
            farthest = Math.Max(farthest, x);
        }
        Assert.Equal(0, rope.Paid);
        Assert.InRange(farthest, 10_000, 10_000 + 2_000 * Dt);
        Assert.True(vx <= 0);
    }

    [Fact]
    public void TheBrakeSlipsAtItsLimitAndHardensAsItPaysOut()
    {
        var rope = new PieceRope(10_000);
        var body = new RopeBody(10_001, 0, 50_000, 0, 0, 100, 1e8);
        var first = rope.Step(Dt, (0, 0), (0, 0), (10_001, 0), body, winds: false)!.Value;
        Assert.Equal(-Spec.Brake * Dt, first.X, 6);
        // Half paid out: the brake holds 1 + BrakeRamp/4 times as hard.
        var half = new PieceRope(10_000);
        double x = 10_000, vx = 30_000;
        while (half.Paid < Spec.PayOut / 2) (x, vx) = Pull(half, x, vx, 100);
        double f = half.Paid / Spec.PayOut;
        var harder = half.Step(Dt, (0, 0), (0, 0), (half.Length + 1, 0), body with { X = half.Length + 1 }, winds: false)!.Value;
        Assert.Equal(-Spec.Brake * (1 + Spec.BrakeRamp * f * f) * Dt, harder.X, 3);
    }

    [Fact]
    public void WindingTakesInWhatWasPaidOutFirstThenTheLineDownToTheStopAndNoFurther()
    {
        var rope = new PieceRope(10_000);
        double x = 10_000, vx = 30_000;
        for (int i = 0; i < 600; i++) (x, vx) = Pull(rope, x, vx, 100);
        Assert.Equal(Spec.PayOut, rope.Paid, 6);
        // A quarter second of winding takes in its speed's worth of what was paid out, the drum's line untouched.
        for (int i = 0; i < 15; i++) rope.Step(Dt, (0, 0), (0, 0), (0, 0), new RopeBody(0, 0, 0, 0, 0, 1, 1), winds: true);
        Assert.Equal(Spec.PayOut - PieceRope.WindSpeed / 4, rope.Paid, 3);
        Assert.Equal(10_000, rope.Wound, 6);
        for (int i = 0; i < 600; i++) rope.Step(Dt, (0, 0), (0, 0), (0, 0), new RopeBody(0, 0, 0, 0, 0, 1, 1), winds: true);
        Assert.Equal((0.0, (double)Spec.Stop), (rope.Paid, rope.Wound));
        // A line hooked shorter than the stop is never let out by winding.
        var short_ = new PieceRope(Spec.Stop - 1_000);
        short_.Step(Dt, (0, 0), (0, 0), (0, 0), new RopeBody(0, 0, 0, 0, 0, 1, 1), winds: true);
        Assert.Equal(Spec.Stop - 1_000, short_.Wound);
    }

    [Fact]
    public void WindingPullsAPieceIn()
    {
        var rope = new PieceRope(20_000);
        double x = 20_000, vx = 0;
        for (int i = 0; i < 60; i++) (x, vx) = Pull(rope, x, vx, 1, winds: true);
        Assert.InRange(x, 20_000 - PieceRope.WindSpeed - 500, 20_000 - PieceRope.WindSpeed + 500);
    }

    [Fact]
    public void AHeavyPieceLagsALightOneTowedTheSameWay()
    {
        var light = new PieceRope(5_000);
        var heavy = new PieceRope(5_000);
        double lx = 5_000, lv = 0, hx = 5_000, hv = 0, anchor = 0;
        for (int i = 0; i < 60; i++)
        {
            anchor -= 6_000 * Dt;
            (lx, lv) = Pull(light, lx, lv, 0.5, (anchor, 0), -6_000);
            (hx, hv) = Pull(heavy, hx, hv, 400, (anchor, 0), -6_000);
        }
        Assert.True(5_000 - hx < (5_000 - lx) / 2, $"heavy moved {5_000 - hx}, light {5_000 - lx}");
        Assert.True(heavy.Paid > 0);
        Assert.Equal(0, light.Paid);
    }

    [Fact]
    public void ABodysWeightGrowsWithItsBoxAndItsDriftHoldsAgainstItsDrag()
    {
        var small = PieceShape.Sized(1_000, 1_000);
        var big = PieceShape.Sized(4_000, 2_000);
        Assert.Equal(4 * Stillwake.Content.Debris.Density / 1000.0, small.Mass, 6);
        Assert.Equal(8 * small.Mass, big.Mass, 6);
        Assert.True(big.Inertia > big.Mass * 1_000_000);
        // Damped toward still at the drag's rate and pushed steadily, a body settles at its drift.
        double v = 0, mass = big.Mass, drift = -8_000;
        for (int i = 0; i < 6_000; i++) v = v * (1 - PieceShape.DragPerSecond * Dt) + PieceShape.Push(mass, drift) / mass * Dt;
        Assert.Equal(drift, v, 3);
    }

    [Fact]
    public void AStrayIsHowFarTheWorstCornerOfTheBoxLies()
    {
        var at = new PiecePose(10_000, 5_000, 0);
        Assert.Equal(300, BodyFeed.Stray(at, at with { X = 10_300 }, 2_000, 1_000), 6);
        // Turned half round: every corner swapped with its opposite, the box's diagonal off.
        Assert.Equal(2 * Math.Sqrt(2_000.0 * 2_000 + 1_000.0 * 1_000), BodyFeed.Stray(at, at with { Angle = 32_768 }, 2_000, 1_000), 6);
    }

    [Fact]
    public void TheFeedSendsOnlyStraysPastTheSlackTheFarthestFirstAtMostFeedMost()
    {
        const double Slack = Stillwake.Content.Debris.FeedSlack;
        Assert.Equal([3, 1], BodyFeed.Picked([(1, Slack + 1), (2, Slack), (3, Slack + 50), (4, 10)]));
        var many = Enumerable.Range(1, 3 * Stillwake.Content.Debris.FeedMost).Select(id => (id, Slack + id)).ToList();
        var picked = BodyFeed.Picked(many);
        Assert.Equal(Stillwake.Content.Debris.FeedMost, picked.Count);
        Assert.Equal(3 * Stillwake.Content.Debris.FeedMost, picked[0]);
        Assert.Empty(BodyFeed.Picked([(1, 0), (2, Slack)]));
    }
}
