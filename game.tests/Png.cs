using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Pictures in <c>game/art</c> read straight from their files, as the tools write them.</summary>
internal static class Png
{
    /// <summary>A PNG's width and height, from its header.</summary>
    public static (int W, int H) Size(string png)
    {
        Assert.True(File.Exists(png), png);
        var head = File.ReadAllBytes(png);
        return (head[16] << 24 | head[17] << 16 | head[18] << 8 | head[19], head[20] << 24 | head[21] << 16 | head[22] << 8 | head[23]);
    }

    /// <summary>An RGBA, 8-bit, unfiltered or filtered PNG's pixels.</summary>
    public static (int W, int H, byte[] Pixels) Pixels(string png)
    {
        var (w, h) = Size(png);
        var bytes = File.ReadAllBytes(png);
        Assert.Equal((8, 6), (bytes[24], bytes[25]));
        using var data = new MemoryStream();
        for (int p = 8; p < bytes.Length;)
        {
            int length = bytes[p] << 24 | bytes[p + 1] << 16 | bytes[p + 2] << 8 | bytes[p + 3];
            if (System.Text.Encoding.ASCII.GetString(bytes, p + 4, 4) == "IDAT") data.Write(bytes, p + 8, length);
            p += 12 + length;
        }
        data.Position = 0;
        using var z = new System.IO.Compression.ZLibStream(data, System.IO.Compression.CompressionMode.Decompress);
        using var raw = new MemoryStream();
        z.CopyTo(raw);
        var rows = raw.ToArray();
        var pixels = new byte[w * h * 4];
        int stride = w * 4;
        for (int y = 0; y < h; y++)
        {
            int filter = rows[y * (stride + 1)];
            for (int i = 0; i < stride; i++)
            {
                int at = y * (stride + 1) + 1 + i;
                int a = i >= 4 ? pixels[y * stride + i - 4] : 0, b = y > 0 ? pixels[(y - 1) * stride + i] : 0, c = i >= 4 && y > 0 ? pixels[(y - 1) * stride + i - 4] : 0;
                int pa = Math.Abs(b - c), pb = Math.Abs(a - c), pc = Math.Abs(a + b - 2 * c);
                int predicted = filter switch { 0 => 0, 1 => a, 2 => b, 3 => (a + b) / 2, _ => pa <= pb && pa <= pc ? a : pb <= pc ? b : c };
                pixels[y * stride + i] = (byte)(rows[at] + predicted);
            }
        }
        return (w, h, pixels);
    }
}
