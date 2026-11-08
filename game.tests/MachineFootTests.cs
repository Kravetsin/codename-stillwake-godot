using System.IO.Compression;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A machine's picture reaches its bottom edge: its corner is laid by its size, so a shorter drawing hangs over the floor.</summary>
public class MachineFootTests
{
    private const int Scale = 4;
    /// <summary>How far above the picture's bottom, in the world's pixels, the wash's soft edge may end.</summary>
    private const double Slack = 0.75;

    private static int BigEndian(byte[] b, int at) => b[at] << 24 | b[at + 1] << 16 | b[at + 2] << 8 | b[at + 3];

    /// <summary>How many rows of an 8-bit RGBA PNG lie under its lowest mostly opaque pixel.</summary>
    private static int RowsUnderFoot(string path)
    {
        byte[] file = File.ReadAllBytes(path);
        int width = BigEndian(file, 16), height = BigEndian(file, 20);
        Assert.True(file[24] == 8 && file[25] == 6 && file[28] == 0, $"{path} is not plain 8-bit RGBA");
        using var idat = new MemoryStream();
        for (int at = 8; at < file.Length;)
        {
            int length = BigEndian(file, at);
            if (file[at + 4] == 'I' && file[at + 5] == 'D') idat.Write(file, at + 8, length);
            at += 12 + length;
        }
        idat.Position = 0;
        using var zlib = new ZLibStream(idat, CompressionMode.Decompress);
        using var raw = new MemoryStream();
        zlib.CopyTo(raw);
        byte[] data = raw.GetBuffer();
        int stride = width * 4, lowest = -1;
        var previous = new byte[stride];
        var row = new byte[stride];
        for (int y = 0; y < height; y++)
        {
            int start = y * (stride + 1) + 1, filter = data[start - 1];
            for (int i = 0; i < stride; i++)
            {
                int left = i >= 4 ? row[i - 4] : 0, up = previous[i], corner = i >= 4 ? previous[i - 4] : 0;
                row[i] = (byte)(data[start + i] + filter switch
                {
                    0 => 0,
                    1 => left,
                    2 => up,
                    3 => (left + up) / 2,
                    _ => Paeth(left, up, corner),
                });
            }
            for (int i = 3; i < stride; i += 4)
                if (row[i] > 128) { lowest = y; break; }
            (previous, row) = (row, previous);
        }
        return height - 1 - lowest;
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    /// <summary>The lamps, hung from the ceiling by their plug at the top, and the debris net, hung on the hull: nothing of theirs stands on the floor.</summary>
    private static readonly string[] Hung = ["arcLamp", "emergencyLamp", "lamp", "net"];

    [Fact]
    public void EveryMachineStandsOnItsPicturesFoot()
    {
        var floating = new List<string>();
        // A vessel's salvage with no picture of its own wears its drifting piece's (plan V-1b): it may float a little.
        foreach (var m in Stillwake.Content.Machines.All.Where(m => !Hung.Contains(m.Kind) && !DebrisLooks.SalvageAsDebris.Contains(m.Kind)))
        {
            double over = RowsUnderFoot(Png.Of($"machine_{m.Kind}")) / (double)Scale;
            if (over > Slack) floating.Add($"{m.Kind} {over:0.##}");
        }
        Assert.True(floating.Count == 0, "Ending above the floor: " + string.Join(", ", floating));
    }
}
