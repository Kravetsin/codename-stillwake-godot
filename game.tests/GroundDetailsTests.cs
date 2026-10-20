using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The pictures over a planet's ground (0082): singles in some whole cells, features only over whole cells of one material.</summary>
public class GroundDetailsTests
{
    private const int Size = 64, Soil = 3, Stone = 4;

    private static GroundDetails.Detail[,] Over(Func<int, int, int> ground)
    {
        var shown = new GroundDetails.Detail[Size, Size];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                shown[x, y] = GroundDetails.At(x, y, ground);
        return shown;
    }

    private static int AllSoil(int x, int y) => x is >= 0 and < Size && y is >= 0 and < Size ? Soil : -1;

    [Fact]
    public void AboutFortyPercentOfCellsShowASingleAndSomeBlocksAFeature()
    {
        var shown = Over(AllSoil);
        int singles = 0, features = 0;
        foreach (var d in shown)
        {
            if (d.Picture is >= 0 and < GroundDetails.Singles) singles++;
            if (d.Picture >= GroundDetails.Singles) features++;
        }
        Assert.InRange(singles * 100 / (Size * Size), 28, 42);
        Assert.True(features > 0);
    }

    [Fact]
    public void AFeatureIsWholeEveryPieceOnceWithinItsBlock()
    {
        var shown = Over(AllSoil);
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                var d = shown[x, y];
                if (d.Picture < GroundDetails.Singles) continue;
                int side = GroundDetails.Features[d.Picture - GroundDetails.Singles], fx = x - d.Dx, fy = y - d.Dy;
                Assert.Equal(fx / GroundDetails.Block, (fx + side - 1) / GroundDetails.Block);
                Assert.Equal(fy / GroundDetails.Block, (fy + side - 1) / GroundDetails.Block);
                for (int j = 0; j < side; j++)
                    for (int i = 0; i < side; i++)
                        Assert.Equal(new GroundDetails.Detail(d.Picture, i, j), shown[fx + i, fy + j]);
            }
    }

    [Fact]
    public void NoFeatureCrossesAnotherMaterialOrAnOpenCell()
    {
        // Stone in every third column, a hole in every fifth row: no feature may lie over a cell of either.
        int Mixed(int x, int y) => AllSoil(x, y) < 0 || y % 5 == 0 ? -1 : x % 3 == 0 ? Stone : Soil;
        var shown = Over(Mixed);
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                var d = shown[x, y];
                if (Mixed(x, y) < 0) Assert.Equal(GroundDetails.Detail.None, d);
                if (d.Picture < GroundDetails.Singles) continue;
                int side = GroundDetails.Features[d.Picture - GroundDetails.Singles];
                for (int j = 0; j < side; j++)
                    for (int i = 0; i < side; i++)
                        Assert.Equal(Mixed(x, y), Mixed(x - d.Dx + i, y - d.Dy + j));
            }
    }

    [Fact]
    public void ACellShowsTheSameEveryTime() =>
        Assert.Equal(GroundDetails.At(17, 41, AllSoil), GroundDetails.At(17, 41, AllSoil));
}
