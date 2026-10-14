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

    [Fact]
    public void HangsStraightDownAtNoBend() =>
        Assert.Equal(new Vec(0, Rig.Leg.Upper + Rig.Leg.Lower), Rig.EndOf(new Bend(0, 0), Rig.Leg));
}
