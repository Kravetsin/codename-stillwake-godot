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
            if (swing.Advance(Frame, true, stroke: stroke).Hits) hits.Add(at);
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
    public void EachWeaponPlaysTheMotionsPickedForItsSectors()
    {
        var o = Overhand.Motion;
        Assert.Equal(new SectorMotions(o, FixedMotions.Chain, FixedMotions.Chain, FixedMotions.Chop), Strokes.Axe.Motions);
        Assert.Equal(new SectorMotions(FixedMotions.Peck, FixedMotions.Peck, FixedMotions.Peck, FixedMotions.Chop), Strokes.Pick.Motions);
        Assert.Equal(new SectorMotions(o, FixedMotions.HeaveStraight, FixedMotions.HeaveDown, o), Strokes.Hammer.Motions);
        Assert.Equal(SectorMotions.All(Jab.Motion), Strokes.Spear.Motions);
    }

    [Fact]
    public void TheAxeIsAsItWasAStrokeItsPaceLong()
    {
        Assert.Equal((Strokes.Axe.Ms, Strokes.Axe.Ms, Overhand.HitAt), (Strokes.Axe.Ms, Strokes.Axe.PaceMs, Strokes.Axe.HitShare));
        // Its whoosh read from its row: loudest in the middle of the downstroke, as it always was.
        Assert.Equal((Overhand.WindShare + Overhand.StrikeShare / 2) * Strokes.Axe.Ms - Sound.SwingPeakS * 1000, Strokes.Axe.WhooshMsOf(MotionPlay.Of(Strokes.Axe, -1.2)), 9);
    }

    [Fact]
    public void HeldThePickaxeSwingsAsTheAxeAtItsOwnQuickerPaceABlowEveryThreeTenthsOfASecond()
    {
        var (hits, _) = Held(Strokes.Pick, 3000);
        Assert.Equal(10, hits.Count);
        Assert.InRange(hits[1] - hits[0], 300 - Frame, 300 + Frame);
        Assert.Equal(Overhand.HitAt, Strokes.Pick.HitShare);
        Assert.Equal(Strokes.Pick.Ms, Strokes.Pick.PaceMs);
        Assert.True(Strokes.Pick.PaceMs < Strokes.Axe.PaceMs);
    }

    [Fact]
    public void ThePickaxesCutRunsAlongItsPointsPathAThinBandWithinACellOutsideIt()
    {
        // A swing's dust lies from the blade's reach out to its weapon's range (AirCuts): the pick's range is its point's, a cell at most past it.
        var point = Rig.HeldAt(AxeSwing.ArmAt(0), HeldLook.Pickaxe, HeldLook.Pickaxe.EdgeTo);
        double reach = Math.Sqrt(point.X * point.X + point.Y * point.Y), range = Spec.Weapons.Pickaxe.Range * Rig.Cell / 1000.0;
        Assert.InRange(range - reach, 0, Rig.Cell);
    }

    [Fact]
    public void HeldTheSledgehammerBlowsEveryOnePointEightSecondsItsBlowAtItsHitShareAndNeverRests()
    {
        var (hits, phases) = Held(Strokes.Hammer, 9000);
        Assert.Equal(5, hits.Count);
        double blow = Overhand.HitAt * 1800;
        Assert.InRange(hits[0], blow - Frame, blow + Frame);
        Assert.InRange(hits[1] - hits[0], 1800 - Frame, 1800 + Frame);
        // A stroke its pace long, as the axe's: the arm swings on from one blow into the next, never idle between.
        Assert.Equal((1800.0, 1800.0, Overhand.HitAt), (Strokes.Hammer.Ms, Strokes.Hammer.PaceMs, Strokes.Hammer.HitShare));
        Assert.DoesNotContain(null, phases);
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

    [Theory]
    [InlineData("fireAxe")]
    [InlineData("pickaxe")]
    [InlineData("sledgehammer")]
    [InlineData("spear")]
    public void EveryStrokeIsDrawnWithTheLookItsItemIsHeldIn(string item) => Assert.Same(HeldLook.Of(item), Strokes.Of(item)!.Look);

    [Fact]
    public void ABiteStopsTheDrawnStrokeAtItsOwnHit()
    {
        double hit = Strokes.Spear.HitShare;
        Assert.Equal(hit, AxeSwing.Shown(hit + 0.01, MotionPlay.Of(Strokes.Spear, 0)), 9);
        Assert.Equal(1, AxeSwing.Shown(1, MotionPlay.Of(Strokes.Hammer, 0)), 9);
    }
}
