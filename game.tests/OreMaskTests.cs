using Stillwake.Content;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Ore shines in lamp light by a mask (2026-11-07-ore-pockets): what a vein's picture adds to its host's sheet, in every cell of it.</summary>
public class OreMaskTests
{
    [Fact]
    public void APixelLikeTheHostsIsNoOreAndOneClearlyApartIsWholeInTheFlecksColour()
    {
        byte[] host = [100, 100, 100, 255, 100, 100, 100, 255, 100, 100, 100, 255];
        byte[] vein = [104, 100, 97, 255, 200, 120, 60, 255, 124, 100, 100, 255];

        var mask = OreMask.Of(vein, host, (255, 186, 120));

        Assert.Equal([0, 0, 0, 255], mask[..4]);
        Assert.Equal([255, 186, 120, 255], mask[4..8]);
        // 24 apart, half way from the fringe to a clear difference: half the fleck.
        Assert.Equal([127, 93, 60, 255], mask[8..]);
    }

    public static TheoryData<string> Veins() => [.. Materials.All.Where(m => m.In != null).Select(m => m.Id)];

    [Theory]
    [MemberData(nameof(Veins))]
    public void EveryCellOfAVeinsSheetHoldsOreThatShines(string vein)
    {
        var host = Materials.Get(vein).In!;
        var (w, h, drawn) = Png.Pixels(Png.Of($"wall_{vein}"));
        var (hw, hh, under) = Png.Pixels(Png.Of($"wall_{host}"));
        Assert.Equal((w, h), (hw, hh));

        var mask = OreMask.Of(drawn, under, GroundLooks.Of[vein].Fleck);

        int side = 8 * Png.Scale;
        var bare = new List<(int, int)>();
        for (int cy = 0; cy < h / side; cy++)
            for (int cx = 0; cx < w / side; cx++)
            {
                int whole = 0;
                for (int y = cy * side; y < (cy + 1) * side; y++)
                    for (int x = cx * side; x < (cx + 1) * side; x++)
                        if (mask[(y * w + x) * 4] + mask[(y * w + x) * 4 + 1] + mask[(y * w + x) * 4 + 2] > 0) whole++;
                // At least a world pixel's worth of ore in every cell.
                if (whole < Png.Scale * Png.Scale) bare.Add((cx, cy));
            }
        Assert.True(bare.Count == 0, $"{vein}: cells with no ore {string.Join(" ", bare)}");
    }
}
