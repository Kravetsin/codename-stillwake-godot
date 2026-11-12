using Stillwake.Game.Shared;
using Stillwake.Sim;
using Xunit;
using CrewSpec = Stillwake.Content.Crew;

namespace Stillwake.Game.Tests;

/// <summary>The canister's mark as picked (canister-mark): the cooldown counted on the HUD, its ready glow, the slot's clock wipe.</summary>
public class CanisterMarkTests
{
    private const double Read = 0.25;

    [Fact]
    public void CountsTheCooldownDownEvenlyBetweenReads()
    {
        var c = new CanisterCooling();
        c.Read(0);
        c.Read(CrewSpec.CanisterCooldown);
        Assert.True(c.Cooling);
        Assert.Equal(0, c.Passed, 6);
        c.Step(Read / 2);
        Assert.Equal(Read / 2 * World.TicksPerSecond / CrewSpec.CanisterCooldown, c.Passed, 6);
    }

    [Fact]
    public void GlowsAsTheCountRunsOutAndOnceOnlyThoughTheSimLagsIt()
    {
        var c = new CanisterCooling();
        c.Read(0);
        c.Read(CrewSpec.CanisterCooldown);
        c.Read(4);
        c.Step(1);
        Assert.False(c.Cooling);
        Assert.Equal(1, c.Ready, 6);
        // The sim a tick behind the count: nothing starts again.
        c.Read(1);
        Assert.False(c.Cooling);
        c.Step(CanisterCooling.ReadyS / 2);
        c.Read(0);
        Assert.Equal(0.25, c.Ready, 6);
        c.Step(CanisterCooling.ReadyS);
        Assert.Equal(0, c.Ready);
    }

    [Fact]
    public void ACooldownClearedByPassingOutGlowsNothing()
    {
        var c = new CanisterCooling();
        c.Read(CrewSpec.CanisterCooldown);
        c.Read(CrewSpec.CanisterCooldown - 5);
        c.Read(0);
        Assert.False(c.Cooling);
        Assert.Equal(0, c.Ready);
    }

    [Fact]
    public void AWorldLoadedMidCooldownShowsItWithoutAGlowFirst()
    {
        var c = new CanisterCooling();
        c.Read(CrewSpec.CanisterCooldown / 2);
        Assert.Equal(0.5, c.Passed, 6);
        Assert.Equal(0, c.Ready);
    }

    [Fact]
    public void WipesAQuarterThroughTheTopRightCorner()
    {
        Assert.Equal([(0.5f, 0.5f), (0.5f, 0f), (1f, 0f), (1f, 0.5f)], Rounded(ClockWipe.Of(0.25)));
    }

    [Fact]
    public void WipesThreeQuartersThroughThreeCornersAndNothingAtEitherEnd()
    {
        Assert.Equal([(0.5f, 0.5f), (0.5f, 0f), (1f, 0f), (1f, 1f), (0f, 1f), (0f, 0.5f)], Rounded(ClockWipe.Of(0.75)));
        Assert.Empty(ClockWipe.Of(0));
        Assert.Empty(ClockWipe.Of(1));
    }

    private static (float, float)[] Rounded(IReadOnlyList<(float X, float Y)> points) =>
        points.Select(p => (MathF.Round(p.X, 4), MathF.Round(p.Y, 4))).ToArray();
}
