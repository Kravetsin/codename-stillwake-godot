using Stillwake.Game.Features.Light;
using Stillwake.Sim;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The starlight cut into tiles: put back together they are the field, and a change names only the tiles it wrote.</summary>
public class StarTilesTests
{
    private const int S = StarTiles.Size;

    [Fact]
    public void TilesPutBackTogetherAreTheFieldTheirPaddingEmpty()
    {
        var region = new Box(-8, -8, S + 40, 2 * S + 3);
        int w = region.Width, h = region.Height;
        var texels = new byte[w * h * 4];
        new Random(4).NextBytes(texels);
        Assert.Equal((2, 3), StarTiles.Of(region));
        var tile = new byte[S * S * 4];
        Array.Fill(tile, (byte)7);
        for (int layer = 0; layer < 6; layer++)
        {
            StarTiles.Copy(texels, region, layer, tile);
            int x0 = layer % 2 * S, y0 = layer / 2 * S;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                    for (int c = 0; c < 4; c++)
                    {
                        byte expected = x0 + x < w && y0 + y < h ? texels[((y0 + y) * w + x0 + x) * 4 + c] : (byte)0;
                        Assert.Equal(expected, tile[(y * S + x) * 4 + c]);
                    }
        }
    }

    [Fact]
    public void AChangeNamesTheTilesItsBoxesOverlapEachOnceWithinTheRegion()
    {
        var region = new Box(-8, -8, 3 * S - 9, 2 * S - 9);
        Assert.Equal([0], StarTiles.Touched(region, [new Box(0, 0, 10, 10)]));
        // Across the line between the first two columns and two rows of tiles, and again, overlapping.
        Assert.Equal([0, 1, 3, 4], StarTiles.Touched(region, [new Box(S - 12, S - 12, S - 4, S - 4), new Box(S - 10, S - 9, S - 8, S - 8)]));
        // Clipped to the region: past its corner, the last tile only.
        Assert.Equal([5], StarTiles.Touched(region, [new Box(3 * S - 20, 2 * S - 20, 3 * S + 50, 2 * S + 50)]));
        Assert.Empty(StarTiles.Touched(region, [Box.None]));
    }
}
