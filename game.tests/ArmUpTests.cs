using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>An aimed item's arm is up while it is used and a moment after, else it hangs (aim-on-use).</summary>
public class ArmUpTests
{
    [Fact]
    public void IsDownTillUsedUpWhileUsedAndALingerAfter()
    {
        var arm = new ArmUp();
        Assert.False(arm.Step(false, 16));
        Assert.True(arm.Step(true, 16));
        Assert.True(arm.Step(true, 1000));
        Assert.True(arm.Step(false, ArmUp.LingerMs - 1));
        Assert.False(arm.Step(false, 2));
        Assert.False(arm.Step(false, 5000));
    }

    [Fact]
    public void AClickWithinTheLingerKeepsItUpFromThatClick()
    {
        var arm = new ArmUp();
        arm.Step(true, 16);
        arm.Step(false, ArmUp.LingerMs - 50);
        Assert.True(arm.Step(true, 16));
        Assert.True(arm.Step(false, ArmUp.LingerMs - 1));
    }
}
