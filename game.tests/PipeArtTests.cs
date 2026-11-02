using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Stillwake.Game.Features.Pipes;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The pipes in ink: every machine's fitting stands on its port, so a span from the port meets it, and a run's lit edge faces the light.</summary>
public class PipeArtTests
{
    private const int Fine = 4, Cell = 8;

    private static string Hd => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art", "hd");

    public static TheoryData<string> Ported => new(Spec.Machines.All.Where(m => m.Ports.Count > 0).Select(m => m.Kind));

    [Theory]
    [MemberData(nameof(Ported))]
    public void ItsFittingReachesThePicturesEdgeLevelWithEveryPort(string kind)
    {
        var spec = Spec.Machines.Get(kind);
        var (width, height, alpha) = Alpha(Path.Combine(Hd, $"machine_{kind}.png"));
        foreach (var port in spec.Ports)
        {
            int column = port.X * 2 + 1 < spec.Width ? 1 : width - 2;
            int y = (port.Y * Cell + Cell / 2) * Fine, top = y, bottom = y;
            Assert.True(alpha[y * width + column] > 128, $"{kind}: nothing at the edge level with its port at {port.X}, {port.Y}");
            while (top > 0 && alpha[(top - 1) * width + column] > 128) top--;
            while (bottom < height - 1 && alpha[(bottom + 1) * width + column] > 128) bottom++;
            Assert.InRange((top + bottom) / 2.0, y - Fine, y + Fine);
        }
    }

    [Theory]
    [InlineData(0, 0, 10, 0)]
    [InlineData(10, 0, 0, 0)]
    [InlineData(0, 0, 0, 10)]
    [InlineData(0, 10, 0, 0)]
    [InlineData(0, 0, 10, 10)]
    [InlineData(10, 10, 0, 0)]
    [InlineData(0, 10, 10, 0)]
    [InlineData(10, 0, 0, 10)]
    public void ARunsLitEdgeFacesTheLightWhicheverWayItWasLaid(float ax, float ay, float bx, float by)
    {
        if (TubeRun.LitFromEnd(ax, ay, bx, by)) (ax, ay, bx, by) = (bx, by, ax, ay);
        // Its picture's top, turned with its direction, against a light from the upper left.
        float upX = by - ay, upY = ax - bx;
        Assert.True(upX * -0.6f + upY * -0.8f >= 0, $"lit edge along ({upX}, {upY})");
    }

    /// <summary>A browser's PNG, 8-bit RGBA with any row filter: its width, height and each pixel's alpha.</summary>
    private static (int Width, int Height, byte[] Alpha) Alpha(string file)
    {
        var png = File.ReadAllBytes(file);
        int width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)), height = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20));
        using var idat = new MemoryStream();
        for (int at = 8; at < png.Length;)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(at));
            if (Encoding.ASCII.GetString(png, at + 4, 4) == "IDAT") idat.Write(png, at + 8, length);
            at += 12 + length;
        }
        idat.Position = 0;
        using var raw = new MemoryStream();
        using (var z = new ZLibStream(idat, CompressionMode.Decompress)) z.CopyTo(raw);
        var bytes = raw.ToArray();
        int stride = width * 4;
        var alpha = new byte[width * height];
        var above = new byte[stride];
        var row = new byte[stride];
        for (int y = 0; y < height; y++)
        {
            int start = y * (stride + 1), filter = bytes[start];
            for (int i = 0; i < stride; i++)
            {
                int a = i >= 4 ? row[i - 4] : 0, b = above[i], c = i >= 4 ? above[i - 4] : 0;
                int guess = filter switch
                {
                    1 => a,
                    2 => b,
                    3 => (a + b) / 2,
                    4 => Paeth(a, b, c),
                    _ => 0,
                };
                row[i] = (byte)(bytes[start + 1 + i] + guess);
            }
            for (int x = 0; x < width; x++) alpha[y * width + x] = row[x * 4 + 3];
            (above, row) = (row, above);
        }
        return (width, height, alpha);
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }
}
