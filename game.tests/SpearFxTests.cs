using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The spear's effects follow its thrust and its blade (spear-effects): nothing between thrusts, nothing off a blade at rest.</summary>
public class SpearFxTests
{
    [Fact]
    public void TheLensSwellsToTheHitThenTearsOffAsEddiesRollingBack()
    {
        Assert.Null(SpearFx.HazeAt(0));
        Assert.Null(SpearFx.HazeAt(Strokes.JabCockMs - 1));
        Assert.Equal(1, SpearFx.HazeAt(Strokes.JabHitMs)!.Value.Lens, 9);
        var early = SpearFx.HazeAt(Strokes.JabHitMs + 30)!.Value;
        var late = SpearFx.HazeAt(Strokes.JabHitMs + 150)!.Value;
        Assert.True(early.Eddy > 0 && late.EddyX < early.EddyX && late.EddyRadius > early.EddyRadius);
        Assert.Equal(0, late.Lens);
        Assert.Null(SpearFx.HazeAt(Strokes.JabHitMs + SpearFx.EddyMs + 1));
    }

    [Fact]
    public void TheArcRunsDownTheCableToThePointOnTheThrust()
    {
        Assert.False(SpearFx.JabAt(10)!.Value.Running);
        double head = 0;
        for (double ms = Strokes.JabCockMs; ms < Strokes.JabHitMs; ms += 5)
        {
            var jab = SpearFx.JabAt(ms)!.Value;
            Assert.True(jab.Running && jab.Head >= head);
            head = jab.Head;
        }
        var hit = SpearFx.JabAt(Strokes.JabHitMs)!.Value;
        Assert.True(hit.Head == 1 && !hit.Running && hit.Spits == 1);
        Assert.Null(SpearFx.JabAt(Strokes.JabHitMs + SpearFx.LitFadeMs));
        Assert.Equal(SpearFx.Cable[^1].X, SpearFx.Along(1).X, 9);
        Assert.Equal(SpearFx.Cable[^1].Y, SpearFx.Along(1).Y, 9);
        Assert.Equal(SpearFx.Cable[0], SpearFx.Along(0));
    }

    [Fact]
    public void ChargesRollDownTheCableFasterOnARunNeverJumpingAsThePaceChanges()
    {
        // Lane 0 over a pass at a stand, then at a run: the run's passes are the quicker.
        Assert.True(SpearFx.PassesIn(100, 1) > SpearFx.PassesIn(100, 0));
        double passes = 0, last = -1;
        int seen = 0;
        for (int i = 0; i < 200; i++)
        {
            passes += SpearFx.PassesIn(8, i < 100 ? 0 : 1);
            if (SpearFx.ChargeAt(1, passes) is not var (head, alpha)) continue;
            seen++;
            Assert.True(alpha > 0 && alpha <= 1);
            // A head moves on down the cable, or starts a new pass from the coil.
            Assert.True(head >= last || head < 0.2, $"{head} after {last}");
            last = head;
        }
        Assert.True(seen > 100);
        Assert.Equal(SpearFx.ChargeAt(2, 7.3), SpearFx.ChargeAt(2, 7.3));
    }

    [Fact]
    public void AMovingBladeThrowsLightningBackAndShedsMotesAndAStillOneNothing()
    {
        var wake = new SpearWake(() => 0.5);
        for (int i = 0; i < 30; i++) wake.Step(16, p => (p.X, p.Y));
        Assert.Equal(0, wake.Strength);
        Assert.Null(wake.Behind(0, 55));
        Assert.Empty(wake.Motes);
        double x = 0;
        for (int i = 0; i < 30; i++)
        {
            x += 2;
            wake.Step(16, p => (p.X + x, p.Y));
        }
        Assert.True(wake.Strength > 0.9);
        Assert.NotEmpty(wake.Motes);
        // 125 px a second: 55 ms back is about 7 px behind the point.
        var was = wake.Behind(0, 55)!.Value;
        Assert.InRange(63.1 + x - was.X, 6, 8);
        wake.Step(16, null);
        Assert.Null(wake.Behind(0, 55));
        Assert.Equal(0, wake.Strength);
    }

    [Fact]
    public void AJumpOfTheBladeLetsTheWakeGoSoItNeverSpansIt()
    {
        var wake = new SpearWake(() => 0.5);
        double x = 0;
        for (int i = 0; i < 30; i++)
        {
            x += 2;
            wake.Step(16, p => (p.X + x, p.Y));
        }
        Assert.NotNull(wake.Behind(0, 55));
        // Set down a little lower in one frame (a hard landing): near enough to draw to, yet a jump.
        wake.Step(16, p => (p.X + x, p.Y + 12));
        Assert.Null(wake.Behind(0, 55));
        Assert.Null(wake.Behind(0, 110));
    }

    [Fact]
    public void ABladeRaisedOrLoweredThrowsFainterLightningThanOneThrustAlong()
    {
        double Strength(bool level)
        {
            var wake = new SpearWake(() => 0.5);
            double d = 0;
            for (int i = 0; i < 20; i++)
            {
                d += 2;
                wake.Step(16, p => level ? (p.X + d, p.Y) : (p.X + d * 0.3, p.Y + d));
            }
            return wake.Strength;
        }
        Assert.True(Strength(level: false) < Strength(level: true) / 2);
    }
}
