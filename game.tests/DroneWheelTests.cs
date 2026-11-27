using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The drone's two-branch wheel (drone-abilities): the right button switches actives and modes, and a pick sets its own branch's choice.</summary>
public class DroneWheelTests
{
    private static bool None(DroneActive _) => false;

    [Fact]
    public void ItStartsOnTheModesTheLampAndTheThrow()
    {
        var d = new DroneWheel();
        Assert.Equal(DroneBranch.Modes, d.Branch);
        Assert.Equal([DroneSide.Of(DroneMode.Lamp), DroneSide.Of(DroneMode.Scan)], d.SidesOf(None));
        Assert.Equal((DroneMode.Lamp, DroneActive.Throw), (d.Mode, d.Active));
    }

    [Fact]
    public void TheActivesShowOnlyTheThrowUntilAFormIsLearned()
    {
        var d = new DroneWheel();
        d.Switch();
        Assert.Equal(DroneBranch.Actives, d.Branch);
        Assert.Equal([DroneSide.Of(DroneActive.Throw)], d.SidesOf(None));
        Assert.Equal([DroneSide.Of(DroneActive.Throw), DroneSide.Of(DroneActive.Nails)], d.SidesOf(_ => true));
        d.Pick(DroneSide.Of(DroneActive.Nails));
        d.SidesOf(_ => true);
        Assert.Equal(DroneActive.Nails, d.Active);
        // A form no longer known falls back to the throw.
        d.SidesOf(None);
        Assert.Equal(DroneActive.Throw, d.Active);
        d.Switch();
        Assert.Equal(DroneBranch.Modes, d.Branch);
    }

    [Fact]
    public void APickSetsItsOwnBranchsChoiceAndLeavesTheOther()
    {
        var d = new DroneWheel();
        d.Pick(DroneSide.Of(DroneMode.Scan));
        Assert.Equal((DroneMode.Scan, DroneActive.Throw), (d.Mode, d.Active));
        d.Pick(DroneSide.Of(DroneActive.Throw));
        Assert.Equal((DroneMode.Scan, DroneActive.Throw), (d.Mode, d.Active));
        Assert.True(d.Chosen(DroneSide.Of(DroneMode.Scan)) && d.Chosen(DroneSide.Of(DroneActive.Throw)));
        Assert.False(d.Chosen(DroneSide.Of(DroneMode.Lamp)));
    }

    [Fact]
    public void SwitchedWhileHeldTheWheelPicksFromTheNewBranch()
    {
        var d = new DroneWheel();
        var wheel = new Wheel<DroneSide>(d.SidesOf(None));
        wheel.Step(true, 0, (100, 100), Wheel.Middle, null);
        wheel.Step(true, 300, (100, 100), Wheel.Middle, null);
        Assert.True(wheel.Open);
        d.Switch();
        wheel.Sides = d.SidesOf(None);
        wheel.Step(true, 16, (100, 40), Wheel.Middle, null);
        Assert.Equal(DroneSide.Of(DroneActive.Throw), wheel.Step(false, 16, (100, 40), Wheel.Middle, null));
    }

    [Fact]
    public void TheWheelsIconsAreWithinTheirBox()
    {
        foreach (var icon in new[] { DroneWheel.ThrowIcon, DroneWheel.NailsIcon, DroneWheel.RightButton })
            foreach (var step in icon.One.Steps)
                Assert.True(System.Math.Abs(step.Ax) <= 1 && System.Math.Abs(step.Ay) <= 1 && System.Math.Abs(step.Bx) <= 1 && System.Math.Abs(step.By) <= 1);
    }
}
