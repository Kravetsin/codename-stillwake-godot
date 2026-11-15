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
    public void ASteadyArcKeepsItsShapeEatenFromBothEndsAndFadesOnlyAtTheLast()
    {
        var bolts = new Render.Bolts(new System.Random(3).NextDouble);
        var steady = bolts.Add(() => (0, 0, 20, 0, 0.2), 12, 1, steady: true)!;
        var plain = bolts.Add(() => (0, 0, 20, 0, 0.2), 12, 1)!;
        (double, double)[] Shape(Render.Bolt b)
        {
            var into = new (double X, double Y)[65];
            return into[..b.PointsOf(0, 0, 0, 20, 0, into)];
        }
        var (was, plainWas) = (Shape(steady), Shape(plain));
        Assert.Equal((0.0, 1.0), steady.Shown(0));
        bolts.Step(1000.0 / 60 * 6);
        Assert.Equal(was, Shape(steady));
        Assert.NotEqual(plainWas, Shape(plain));
        // Half its life on: still whole bright, its line eaten a share in from either end, its forks shrunk from their tips.
        Assert.Equal(1, steady.Brightness, 9);
        var (from, to) = steady.Shown(0);
        Assert.True(from > 0.15 && from < 0.3 && to == 1 - from, $"{from}..{to}");
        Assert.Equal(0, steady.Shown(1).From);
        Assert.True(steady.Shown(1).To < 0.5);
        Assert.Equal((0.0, 1.0), plain.Shown(0));
        bolts.Step(1000.0 / 60 * 4);
        Assert.True(steady.Brightness is > 0.4 and < 0.7, $"{steady.Brightness}");
    }

    [Fact]
    public void ACutLineKeepsItsCornersBetweenTheShares()
    {
        (double X, double Y)[] line = [(0, 0), (10, 0), (20, 0)], into = new (double, double)[8];
        int n = Render.Bolts.Cut(line, 0.25, 0.75, into);
        Assert.Equal([(5.0, 0.0), (10.0, 0.0), (15.0, 0.0)], into[..n]);
        Assert.Equal(0, Render.Bolts.Cut(line, 0.5, 0.5, into));
    }
}
