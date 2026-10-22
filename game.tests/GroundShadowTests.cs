using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The soft shadow walls cast on a natural back wall (0088): three tones along their open edges, none under the walls.</summary>
public class GroundShadowTests
{
    private const int Cell = Camera.Cell;
    private const bool Open = false, Wall = true;

    private static int At(byte[] shade, int x, int y) => shade[y * Cell + x];

    [Fact]
    public void NoWallsRoundCastNone() => Assert.Null(GroundShadow.Of([Open, Open, Open, Open, Open, Open, Open, Open, Open]));

    [Fact]
    public void ACellItsOwnWallCoversShowsNone() => Assert.Null(GroundShadow.Of([Open, Wall, Open, Open, Wall, Open, Open, Open, Open]));

    [Fact]
    public void AWallBesideFadesInThreeTonesOverFourPixels()
    {
        // A wall all down the left: the pick page's profile, its darkest tone at the edge.
        var shade = GroundShadow.Of([Wall, Open, Open, Wall, Open, Open, Wall, Open, Open])!;
        for (int y = 0; y < Cell; y++)
        {
            Assert.Equal([3, 2, 1, 1, 0, 0], new[] { At(shade, 0, y), At(shade, 1, y), At(shade, 2, y), At(shade, 3, y), At(shade, 4, y), At(shade, Cell - 1, y) });
        }
    }

    [Fact]
    public void AWallOnlyAtACornerShadowsOnlyThatCorner()
    {
        var shade = GroundShadow.Of([Open, Open, Open, Open, Open, Open, Open, Open, Wall])!;
        Assert.True(At(shade, Cell - 1, Cell - 1) > 0);
        Assert.Equal(0, At(shade, 0, 0));
        Assert.Equal(0, At(shade, Cell - 1, 0));
        Assert.Equal(0, At(shade, 0, Cell - 1));
    }
}
