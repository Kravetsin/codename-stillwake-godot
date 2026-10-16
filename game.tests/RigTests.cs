using Stillwake.Game;
using Stillwake.Game.Features.Crew;
using Xunit;
using static Stillwake.Game.Tests.PoseTests;

namespace Stillwake.Game.Tests;

/// <summary>A limb in two segments: apps/game/src/features/crew/rig.test.ts, one for one.</summary>
public class RigTests
{
    [Fact]
    public void EndsWhereItIsToldToReachAnElbowFoldingForwardAndAKneeBack()
    {
        foreach (var target in new Vec[] { new(8, 4), new(3, -9), new(-4, 6) })
        {
            var arm = Rig.Reach(target, Rig.Arm, 1);
            Close(target.X, Rig.EndOf(arm, Rig.Arm).X, 5);
            Close(target.Y, Rig.EndOf(arm, Rig.Arm).Y, 5);
            Assert.True(arm.Lower > 0);
        }
        var knee = Rig.Reach(new Vec(1, Rig.Leg.Upper + Rig.Leg.Lower - 4), Rig.Leg, -1);
        Assert.True(knee.Lower < 0);
        // The knee comes forward of the line from hip to foot.
        Assert.True(Math.Sin(knee.Upper) * Rig.Leg.Upper > 0.5);
    }

    [Fact]
    public void StretchesStraightTowardWhatIsOutOfReachAndNeverFoldsThroughItself()
    {
        var far = Rig.Reach(new Vec(100, 0), Rig.Arm, 1);
        Close(Math.PI / 2, far.Upper, 1);
        Close(0, far.Lower, 1);
        var near = Rig.Reach(new Vec(0, 0), Rig.Arm, 1);
        Assert.True(double.IsFinite(near.Upper) && double.IsFinite(near.Lower));
    }

    // Held items (0040).

    [Fact]
    public void HoldsAPictureByItsGripInTheFistSquareToTheForearm()
    {
        // The arm straight out ahead: the fist a pixel short of its end, the axe's haft standing up from it,
        // its blade toward the arm's way.
        var axe = HeldLook.FireAxe;
        var arm = new Bend(Math.PI / 2, 0);
        double fist = Rig.Arm.Upper + Rig.Arm.Lower - Rig.FistBack;
        Close(fist, Rig.FistOf(arm).X, 9);
        Close(0, Rig.FistOf(arm).Y, 9);
        Close(-Math.PI / 2, Rig.HeldTurn(arm, axe), 9);
        Close(fist, Rig.HeldAt(arm, axe, axe.Grip).X, 9);
        Close(0, Rig.HeldAt(arm, axe, axe.Grip).Y, 9);
        // Ten pixels on along the haft is ten up; three toward the blade, three on.
        var up = Rig.HeldAt(arm, axe, (axe.Grip.X + 10, axe.Grip.Y));
        var on = Rig.HeldAt(arm, axe, (axe.Grip.X, axe.Grip.Y + 3));
        Close(fist, up.X, 9);
        Close(-10, up.Y, 9);
        Close(fist + 3, on.X, 9);
        Close(0, on.Y, 9);
        // Hanging, the haft points ahead and the blade down.
        var hanging = new Bend(0, 0);
        var head = Rig.HeldAt(hanging, axe, (axe.Grip.X + 10, axe.Grip.Y));
        Close(10, head.X, 9);
        Close(Rig.Arm.Upper + Rig.Arm.Lower - Rig.FistBack, head.Y, 9);
    }

    [Fact]
    public void SwingsTheBladeNotTheHandAcrossTheLineToItsPoint()
    {
        var axe = HeldLook.FireAxe;
        double lead = AxeSwing.Lead(axe);
        // At the strike the edge's middle is on the line ahead, the hand below it, at the swing's reach.
        var (from, to) = AxeSwing.EdgeAt(axe, lead);
        Close(0, Math.Atan2((from.Y + to.Y) / 2, (from.X + to.X) / 2), 9);
        Assert.True(lead > 0);
        var hand = Rig.EndOf(AxeSwing.ArmAt(lead), Rig.Arm);
        Close(lead, Math.Atan2(hand.Y, hand.X), 9);
        Close(AxeSwing.ArmReach * (Rig.Arm.Upper + Rig.Arm.Lower), Math.Sqrt(hand.X * hand.X + hand.Y * hand.Y), 9);
        // The edge is where the picture's is, held in the fist of that arm.
        var arm = AxeSwing.ArmAt(lead);
        Assert.Equal((Rig.HeldAt(arm, axe, axe.EdgeFrom), Rig.HeldAt(arm, axe, axe.EdgeTo)), (from, to));
    }

    [Fact]
    public void HangsStraightDownAtNoBend() =>
        Assert.Equal(new Vec(0, Rig.Leg.Upper + Rig.Leg.Lower), Rig.EndOf(new Bend(0, 0), Rig.Leg));
}
