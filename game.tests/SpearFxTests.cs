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
    public void ArcsCreepMoreOftenOnARunAndTheSameHoweverOftenAsked()
    {
        int Creeping(double run) => Enumerable.Range(0, 20000).Count(i => SpearFx.CreepAt(0, i * 10, run) != null);
        Assert.True(Creeping(1) > 2 * Creeping(0) && Creeping(0) > 0);
        Assert.Equal(SpearFx.CreepAt(1, 12345, 0.5), SpearFx.CreepAt(1, 12345, 0.5));
    }

    [Fact]
    public void AMovingBladeLeavesAWakeAndMotesAndAStillOneNothing()
    {
        var wake = new SpearWake(() => 0.5);
        for (int i = 0; i < 30; i++) wake.Step(16, p => (p.X, p.Y));
        Assert.Equal(0, wake.Strength(0));
        Assert.Empty(wake.Motes);
        double x = 0;
        for (int i = 0; i < 30; i++)
        {
            x += 2;
            wake.Step(16, p => (p.X + x, p.Y));
        }
        Assert.True(wake.Strength(0) > 0.5 && wake.Strength(2) < wake.Strength(0));
        Assert.NotEmpty(wake.Motes);
        Assert.True(wake.Trail(0).Count > 2);
        wake.Step(16, null);
        Assert.Empty(wake.Trail(0));
    }

    [Fact]
    public void ABladeRaisedOrLoweredLeavesAFainterWakeThanOneThrustAlong()
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
            return wake.Strength(0);
        }
        Assert.True(Strength(level: false) < Strength(level: true) / 2);
    }
}
