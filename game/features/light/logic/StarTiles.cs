using System;
using System.Collections.Generic;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// The starlight's texels (<see cref="StarField"/>) cut into square tiles of <see cref="Size"/> cells, row by row from the
/// region's top-left, a layer each of one texture array: a change sends the few tiles it wrote, not the whole region, which
/// on a planet is 25 MB. A tile past the region's right or bottom edge is padded with nothing; the shader never reads there.
/// </summary>
public static class StarTiles
{
    /// <summary>A tile's side, in cells: a planet's region is some 17 by 7 of them, far under a texture array's layers.</summary>
    public const int Size = 256;

    /// <summary>How many tiles across and down cover <paramref name="region"/>.</summary>
    public static (int Across, int Down) Of(Box region) => ((region.Width + Size - 1) / Size, (region.Height + Size - 1) / Size);

    /// <summary>The tiles of <paramref name="region"/> any of <paramref name="written"/> overlaps, by layer, ascending, each once.</summary>
    public static List<int> Touched(Box region, IReadOnlyList<Box> written)
    {
        var (across, _) = Of(region);
        var layers = new SortedSet<int>();
        foreach (var b in written)
        {
            if (b.IsEmpty) continue;
            int left = (Math.Max(b.Left, region.Left) - region.Left) / Size, right = (Math.Min(b.Right, region.Right) - region.Left) / Size;
            int top = (Math.Max(b.Top, region.Top) - region.Top) / Size, bottom = (Math.Min(b.Bottom, region.Bottom) - region.Top) / Size;
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++) layers.Add(y * across + x);
        }
        return [.. layers];
    }

    /// <summary>Copies tile <paramref name="layer"/> of <paramref name="texels"/>, four bytes a cell of <paramref name="region"/>, into <paramref name="tile"/>, four bytes a cell of the tile.</summary>
    public static void Copy(byte[] texels, Box region, int layer, byte[] tile)
    {
        var (across, _) = Of(region);
        int x0 = layer % across * Size, y0 = layer / across * Size;
        int width = Math.Min(Size, region.Width - x0), height = Math.Min(Size, region.Height - y0);
        if (width < Size || height < Size) Array.Clear(tile);
        for (int y = 0; y < height; y++)
            Array.Copy(texels, ((y0 + y) * region.Width + x0) * 4, tile, y * Size * 4, width * 4);
    }
}
