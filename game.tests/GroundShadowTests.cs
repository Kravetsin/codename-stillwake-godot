using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The soft shadow walls cast on a natural back wall (0088): three tones along their open edges, none under the walls.</summary>
public class GroundShadowTests
{
    private const int Cell = 16, Open = -1, Whole = Shape.None;

    private static int At(byte[] shade, int x, int y) => shade[y * Cell + x];

    [Fact]
    public void NoWallsRoundCastNone() => Assert.Null(GroundShadow.Of([Open, Open, Open, Open, Open, Open, Open, Open, Open]));

    [Fact]
    public void ACellItsOwnWallCoversWholeShowsNone() => Assert.Null(GroundShadow.Of([Whole, Whole, Whole, Whole, Whole, Whole, Whole, Whole, Whole]));

    [Fact]
    public void AWallBesideFadesInThreeTonesOverFourPixels()
    {
        // A wall all down the left: the pick page's profile, its darkest tone at the edge.
        var shade = GroundShadow.Of([Whole, Open, Open, Whole, Open, Open, Whole, Open, Open])!;
        for (int y = 0; y < Cell; y++)
        {
            Assert.Equal([3, 2, 1, 1, 0, 0], new[] { At(shade, 0, y), At(shade, 1, y), At(shade, 2, y), At(shade, 3, y), At(shade, 4, y), At(shade, 15, y) });
        }
    }

    [Fact]
    public void AWallOnlyAtACornerShadowsOnlyThatCorner()
    {
        var shade = GroundShadow.Of([Open, Open, Open, Open, Open, Open, Open, Open, Whole])!;
        Assert.True(At(shade, 15, 15) > 0);
        Assert.Equal(0, At(shade, 0, 0));
        Assert.Equal(0, At(shade, 15, 0));
        Assert.Equal(0, At(shade, 0, 15));
    }

    [Fact]
    public void AFloorSlopeShadowsItsOpenHalfAlongItsDiagonalNotItsSolidOne()
    {
        // A floor slope rising right, standing on whole ground: under its diagonal it is solid, over it the back wall darkens nearest the diagonal.
        var shade = GroundShadow.Of([Open, Open, Open, Open, Shape.BottomRight, Open, Whole, Whole, Whole])!;
        for (int y = 0; y < Cell; y++)
            for (int x = 0; x < Cell; x++)
                if (GroundShadow.Covers(Shape.BottomRight, x, y)) Assert.Equal(0, At(shade, x, y));
        Assert.Equal(GroundLooks.ShadowSteps, At(shade, 7, 7));
        Assert.Equal(0, At(shade, 2, 2));
    }
}
