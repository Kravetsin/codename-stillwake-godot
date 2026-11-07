using Stillwake.Game.Features.Crew;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The salvage tools struck by hand (salvage-tools): the sledgehammer's slow blow, the spear's jab, on the axe's clock.</summary>
public class StrokesTests
{
    private const double Frame = 1000.0 / 60;

    /// <summary>The times of the frames a stroke of <paramref name="stroke"/> held for <paramref name="ms"/> hit on, and the phases drawn.</summary>
    private static (List<double> Hits, List<double?> Phases) Held(Stroke stroke, double ms)
    {
        var swing = new AxeSwing();
        var (hits, phases) = (new List<double>(), new List<double?>());
        for (double at = 0; at < ms; at += Frame)
        {
            if (swing.Advance(Frame, true, default, stroke).Hits) hits.Add(at);
            phases.Add(swing.Phase);
        }
        return (hits, phases);
    }

    [Fact]
    public void OnlyTheAxeTheSledgehammerTheSpearAndThePickaxeAreStruckWith()
    {
        Assert.Same(Strokes.Axe, Strokes.Of(Spec.Weapons.Axe.Item));
        Assert.Same(Strokes.Hammer, Strokes.Of(Spec.Weapons.Sledgehammer.Item));
        Assert.Same(Strokes.Spear, Strokes.Of(Spec.Weapons.Spear.Item));
        Assert.Same(Strokes.Pick, Strokes.Of(Spec.Weapons.Pickaxe.Item));
        Assert.All(new[] { Spec.Weapons.Drill, Spec.Weapons.Grinder, Spec.Weapons.PlasmaCutter }, w => Assert.Null(Strokes.Of(w.Item)));
        Assert.Null(Strokes.Of(null));
    }

    [Fact]
    public void TheAxeIsAsItWasAStrokeItsPaceLong()
    {
        Assert.Equal((AxeSwing.Ms, AxeSwing.Ms, AxeSwing.HitAt), (Strokes.Axe.Ms, Strokes.Axe.PaceMs, Strokes.Axe.HitShare));
        Assert.Equal(AxeSwing.WhooshMs, Strokes.Axe.WhooshMs, 9);
    }

    [Fact]
    public void HeldThePickaxeSwingsAsTheAxeAtItsOwnQuickerPaceABlowEveryThreeTenthsOfASecond()
    {
        var (hits, _) = Held(Strokes.Pick, 3000);
        Assert.Equal(10, hits.Count);
        Assert.InRange(hits[1] - hits[0], 300 - Frame, 300 + Frame);
        Assert.Equal((StrokeKind.Swing, AxeSwing.HitAt), (Strokes.Pick.Kind, Strokes.Pick.HitShare));
        Assert.Equal(Strokes.Pick.Ms, Strokes.Pick.PaceMs);
        Assert.True(Strokes.Pick.PaceMs < Strokes.Axe.PaceMs);
    }

    [Fact]
    public void HeldTheSledgehammerBlowsEveryThreeSecondsEightTenthsIntoItsSwingAndRestsBetween()
    {
        var (hits, phases) = Held(Strokes.Hammer, 9000);
        Assert.Equal(3, hits.Count);
        Assert.InRange(hits[0], Strokes.HammerBlowMs - Frame, Strokes.HammerBlowMs + Frame);
        Assert.InRange(hits[1] - hits[0], 3000 - Frame, 3000 + Frame);
        // Swung for its stroke, then at rest, no phase, till its pace is out.
        Assert.Contains(null, phases.Take((int)(3000 / Frame) - 1));
        Assert.True(Strokes.Hammer.Ms < Strokes.Hammer.PaceMs);
    }

    [Fact]
    public void HeldTheSpearJabsAtItsPaceStraightAtTheHitAndAtRestBetween()
    {
        var (hits, phases) = Held(Strokes.Spear, 3000);
        Assert.Equal(5, hits.Count);
        Assert.InRange(hits[0], 215 - Frame, 215 + Frame);
        // Its whole pace is the stroke: the arm stays aimed between jabs.
        Assert.DoesNotContain(null, phases);
        Assert.Equal(Strokes.RestElbow, Strokes.ElbowAt(0), 9);
        Assert.Equal(Strokes.Cocked, Strokes.ElbowAt(150 / Strokes.Spear.Ms), 9);
        Assert.Equal(0, Strokes.ElbowAt(Strokes.Spear.HitShare), 9);
        Assert.Equal(Strokes.RestElbow, Strokes.ElbowAt(Strokes.JabMs / Strokes.Spear.Ms), 9);
        Assert.Equal(Strokes.RestElbow, Strokes.ElbowAt(0.99), 9);
    }

    [Fact]
    public void ABiteStopsTheDrawnStrokeAtItsOwnHit()
    {
        double hit = Strokes.Spear.HitShare;
        Assert.Equal(hit, AxeSwing.Shown(hit + 0.01, Strokes.Spear), 9);
        Assert.Equal(1, AxeSwing.Shown(1, Strokes.Hammer), 9);
    }
}
