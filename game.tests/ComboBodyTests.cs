using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The body laid over a pose in a combo's strike (axe-combos), and the far hand on the haft.</summary>
public class ComboBodyTests
{
    private static readonly Limbs Shown = Poses.LimbsOf(Pose.Stand, new Hero(11000, 20000, 0, 0, false, 1), new Moment(0, 0));

    /// <summary>The third step at its heavy hit: both feet well ahead, the hip pushed after them.</summary>
    private static readonly BodyState Lunging =
        ComboBody.At(MotionPlay.Of(Strokes.Axe, 0, 2, MotionPlay.Of(Strokes.Axe, 0, 1).SpliceAt(450)), 0.52 * 600)!.Value;

    /// <summary>How far ahead of where it stands at rest the layer puts each ankle and the hip.</summary>
    private static (double Hip, double Front, double Back) AheadOf(BodyLayer layer)
    {
        var rest = ComboBody.Stance(BodyState.Rest, double.PositiveInfinity, 0);
        double Ankle(Bend leg, Bend by) => Rig.EndOf(new Bend(leg.Upper + by.Upper, leg.Lower + by.Lower), Rig.ToAnkle).X - Rig.EndOf(leg, Rig.ToAnkle).X;
        return (layer.HipX, layer.HipX + Ankle(rest.FrontLeg, layer.FrontLeg), layer.HipX + Ankle(rest.BackLeg, layer.BackLeg));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void AgainstAWallNothingGoesFurtherAheadThanTheRoomThereIs(double room)
    {
        Assert.True(Lunging.Front > 10 && Lunging.Back > 5, $"{Lunging}");
        var (hip, front, back) = AheadOf(ComboBody.Over(Shown, Lunging, Pose.Stand, 0, room)!.Value);
        Assert.True(hip <= room + 1e-9 && front <= room + 1e-6 && back <= room + 1e-6, $"hip {hip}, front {front}, back {back}");
        // With room, it goes all the way.
        var (_, free, _) = AheadOf(ComboBody.Over(Shown, Lunging, Pose.Stand, 0, double.PositiveInfinity)!.Value);
        Assert.Equal(Lunging.Front, free, 6);
    }

    [Fact]
    public void StridingTheLegsKeepTheirGaitAndOnlyTheTorsoAndTheFarArmMove()
    {
        var layer = ComboBody.Over(Shown, Lunging, Pose.Walk, 1, double.PositiveInfinity)!.Value;
        Assert.Equal((0.0, 0.0, default(Bend), default(Bend), 0.0, 0.0), (layer.HipX, layer.HipY, layer.FrontLeg, layer.BackLeg, layer.FrontFoot, layer.BackFoot));
        Assert.Equal(Lunging.Lean, layer.Lean);
        Assert.NotEqual(default, layer.FarArm);
        // Slowing to a stand, the legs come in as the stride goes.
        var half = ComboBody.Over(Shown, Lunging, Pose.Walk, 0.5, double.PositiveInfinity)!.Value;
        var full = ComboBody.Over(Shown, Lunging, Pose.Stand, 0, double.PositiveInfinity)!.Value;
        Assert.Equal(full.HipX / 2, half.HipX, 9);
    }

    [Theory]
    [InlineData(Pose.Air)]
    [InlineData(Pose.Float)]
    [InlineData(Pose.Climb)]
    [InlineData(Pose.Lie)]
    public void OffTheFloorOnlyTheArmStrikes(Pose pose) => Assert.Null(ComboBody.Over(Shown, Lunging, pose, 0, double.PositiveInfinity));

    [Fact]
    public void FacingLeftTheStanceIsFacingRightsMirroredAndTurningRoundItSwapsSidesWithoutAJump()
    {
        var layer = ComboBody.Over(Shown, Lunging, Pose.Stand, 0, double.PositiveInfinity)!.Value;
        var (right, left) = (new BodyBlend(), new BodyBlend());
        Assert.Equal(right.Next(layer, 1, true, 0), left.Next(layer, -1, true, 0));
        Assert.Equal(layer.Mirrored(-1).HipX, -layer.Mirrored(1).HipX);
        // Turned round: the first frame is drawn where the last was in the world, the stance swapped sides by the blend's end.
        var was = right.Next(layer, 1, false, 16).Mirrored(1);
        var turned = right.Next(layer, -1, false, 0).Mirrored(-1);
        Assert.Equal(was.HipX, turned.HipX, 9);
        Assert.Equal(was.Lean, turned.Lean, 9);
        var done = right.Next(layer, -1, false, Combos.LayerMs);
        Assert.Equal(layer.HipX, done.HipX, 9);
        // Lying down it snaps.
        Assert.Equal(BodyLayer.None, right.Next(null, -1, true, 0));
    }

    [Fact]
    public void LetGoTheLayerEasesOutOverItsTime()
    {
        var layer = ComboBody.Over(Shown, Lunging, Pose.Stand, 0, double.PositiveInfinity)!.Value;
        var blend = new BodyBlend();
        blend.Next(layer, 1, true, 0);
        Assert.Equal(layer.HipX, blend.Next(null, 1, false, 16).HipX, 9);
        Assert.InRange(blend.Next(null, 1, false, Combos.LayerMs / 2).HipX, 1e-3, layer.HipX - 1e-3);
        Assert.Equal(0, blend.Next(null, 1, false, Combos.LayerMs).HipX, 9);
    }

    [Fact]
    public void GrippingTheFarFistHoldsTheHaftWithinItsRange()
    {
        int held = 0;
        foreach (int step in new[] { 1, 2 })
        {
            var play = MotionPlay.Of(Strokes.Axe, 0, step);
            for (double ms = 0; ms <= play.Ms; ms += 2)
            {
                if (ComboBody.At(play, ms)!.Value.Grip < 1) continue;
                held++;
                double shown = AxeSwing.Shown(ms / play.Ms, play);
                var arm = AxeSwing.ArmAt(play.AngleAt(0, shown), play.ElbowAt(shown));
                var hold = HaftGrip.ArmFor(arm, HeldLook.FireAxe, play.WristAt(shown));
                var fist = Rig.FistOf(hold.Arm);
                Assert.True(Math.Sqrt((fist.X - hold.At.X) * (fist.X - hold.At.X) + (fist.Y - hold.At.Y) * (fist.Y - hold.At.Y)) < 0.3,
                    $"step {step} at {ms} ms: fist {fist}, haft {hold.At}");
                Assert.InRange(hold.Along, Combos.HaftFrom, Combos.HaftTo);
            }
        }
        Assert.True(held > 50);
    }

    [Fact]
    public void WithoutALayerTheUpperBodyIsWhereItWasSoEveryHeldItemBeamLightAndCapeIsToo()
    {
        var (at, turn) = ComboBody.WaistOf(BodyLayer.None);
        Assert.Equal((0.0, 0.0, 0.0), (at.X, at.Y, turn));
        // Leaned, it turns about the hip, which stays put; moved, it moves with the hip.
        var leaned = ComboBody.WaistOf(BodyLayer.None with { Lean = 0.45 });
        var hip = new Vec(0, -Rig.Hip);
        double c = Math.Cos(leaned.Turn), s = Math.Sin(leaned.Turn);
        Assert.Equal(hip.X, leaned.At.X + hip.X * c - hip.Y * s, 9);
        Assert.Equal(hip.Y, leaned.At.Y + hip.X * s + hip.Y * c, 9);
        Assert.Equal((2.0, 1.0), (ComboBody.WaistOf(BodyLayer.None with { HipX = 2, HipY = 1 }).At.X, ComboBody.WaistOf(BodyLayer.None with { HipX = 2, HipY = 1 }).At.Y));
    }

    [Fact]
    public void TheFloorIsLookedAtAsFarAheadAsAnyStepCarriesAFoot()
    {
        double furthest = Combos.FireAxe.SelectMany(s => s.Body.Feet).Max(f => f.To);
        Assert.Equal(Combos.MostAheadPx, furthest);
    }
}
