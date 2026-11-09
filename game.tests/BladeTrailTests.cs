using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The blade's trail in a combo (axe-combos): only in a cut, fading with age, fainter where the blade is slow.</summary>
public class BladeTrailTests
{
    /// <summary>An edge moving <paramref name="speed"/> pixels a ms down.</summary>
    private static Func<double, (Vec, Vec)> Moving(double speed) => ms => (new Vec(10, ms * speed), new Vec(19, ms * speed));

    [Fact]
    public void InACutItFadesWithAgeFromItsMostAtTheBlade()
    {
        var trail = BladeTrail.Of(Moving(1), _ => true, 200);
        Assert.True(trail.Live);
        Assert.Equal((int)(Combos.TrailMs / Combos.TrailStepMs) + 1, trail.Edges.Count);
        Assert.Equal((10.0, 200.0), (trail.Edges[0].From.X, trail.Edges[0].From.Y));
        Assert.InRange(trail.Opacity[0], Combos.TrailMost * 0.95, Combos.TrailMost);
        for (int i = 1; i < trail.Opacity.Count; i++) Assert.True(trail.Opacity[i] < trail.Opacity[i - 1]);
        Assert.InRange(trail.Opacity[^1], 0, Combos.TrailMost * 0.05);
    }

    [Fact]
    public void OutsideACutNoneShowsAndAcrossItsEdgeOnlyThePartInIt()
    {
        Assert.False(BladeTrail.Of(Moving(1), _ => false, 200).Live);
        var leaving = BladeTrail.Of(Moving(1), ms => ms < 170, 200);
        Assert.True(leaving.Live);
        for (int i = 0; i < leaving.Opacity.Count; i++)
            Assert.Equal(200 - (i + 0.5) * Combos.TrailStepMs < 170, leaving.Opacity[i] > 0);
    }

    [Fact]
    public void ASlowBladeLeavesAFainterTrailAndItReachesNoFurtherBackThanThePlay()
    {
        double fast = BladeTrail.Of(Moving(1), _ => true, 200).Opacity[0];
        Assert.Equal(fast * 0.03 / Combos.TrailFast, BladeTrail.Of(Moving(0.03), _ => true, 200).Opacity[0], 9);
        Assert.Equal(11, BladeTrail.Of(Moving(1), _ => true, 30).Edges.Count);
    }
}
