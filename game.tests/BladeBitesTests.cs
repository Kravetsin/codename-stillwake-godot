using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A struck wreck sparks where the blade's trail lies on it, along the swing (blade-sparks).</summary>
public class BladeBitesTests
{
    // An edge from (0, 0) to (0, 8) swept 4 px right.
    private static readonly (double X, double Y)[] Was = [(0, 0), (0, 4), (0, 8)], Now = [(4, 0), (4, 4), (4, 8)];

    [Fact]
    public void ABiteIsWhereTheStretchIsInsideAndGoesTheWayTheBladeWent()
    {
        var (points, dx, dy) = Assert.NotNull(BladeBites.Of(Was, Now, (x, y) => y > 3));
        Assert.Equal([(2.0, 4.0), (4.0, 4.0), (2.0, 8.0), (4.0, 8.0)], points);
        Assert.Equal(1, dx, 9);
        Assert.Equal(0, dy, 9);
    }

    [Fact]
    public void NoBiteWhileTheStretchMissesOrStandsStill()
    {
        Assert.Null(BladeBites.Of(Was, Now, (_, _) => false));
        Assert.Null(BladeBites.Of(Now, Now, (_, _) => true));
    }

    [Fact]
    public void OnlyThePointsInsideTurnTheWay()
    {
        // The tip swept down, the rest right: a wreck at the tip alone is bitten downward.
        (double X, double Y)[] now = [(4, 0), (4, 4), (0, 12)];
        var (_, dx, dy) = Assert.NotNull(BladeBites.Of(Was, now, (_, y) => y > 9));
        Assert.Equal(0, dx, 9);
        Assert.Equal(1, dy, 9);
    }
}
