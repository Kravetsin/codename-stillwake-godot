using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// The arc bench's look (0099): any tool with a held picture sits on it by its own clamp, within the bench or overhanging
/// it no more than <see cref="BenchLook.Overhang"/> and <see cref="BenchLook.Hang"/> say; the drill as the picked design drew it.
/// </summary>
public class BenchLookTests
{
    private static string Art => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art");

    private static (int W, int H) SizeOf(string resource)
    {
        // A PNG's width and height are the two big-endian words after its signature and IHDR's header.
        var head = File.ReadAllBytes(Path.Combine(Art, "..", resource["res://".Length..]))[16..24];
        return (BinaryPrimitives.ReadInt32BigEndian(head), BinaryPrimitives.ReadInt32BigEndian(head.AsSpan(4)));
    }

    /// <summary>Every item held as a picture: every tool a hero wields, and any to come.</summary>
    public static TheoryData<string> Held => new(Spec.Items.All.Select(i => i.Id).Where(id => HeldLook.Of(id) != null));

    [Theory]
    [MemberData(nameof(Held))]
    public void EveryToolWithAHeldPictureIsClampedWhereItSaysAndLiesOnTheBenchOrOverhangsItGracefully(string item)
    {
        var look = HeldLook.Of(item)!;
        Assert.True(look.Clamp is { } clamp && clamp.X >= 0 && clamp.Y >= 0, $"{item} declares no clamp");
        var (w, h) = SizeOf(look.Picture);
        var (cx, cy) = look.Clamp!.Value;
        Assert.True(cx < w && cy < h, $"{item}'s clamp lies outside its picture");
        var at = BenchLook.ToolAt(look);
        Assert.True(BenchLook.Fits(at, w, h), $"{item} at {at}, {w}×{h}, sticks out past the bench");
        // Every frame it is drawn in is its picture's size, so a spun or loaded one lies alike (only the still one is drawn there).
        foreach (var frame in look.Spin ?? []) Assert.Equal((w, h), SizeOf(frame));
    }

    [Fact]
    public void AnOverhangIsBoundedOnEverySideAndNeverAboveThePicture()
    {
        Assert.True(BenchLook.Fits((0, 4), 33, 15));
        Assert.True(BenchLook.Fits((-BenchLook.Overhang, 0), BenchLook.Width + 2 * BenchLook.Overhang, BenchLook.TableTop + BenchLook.Hang));
        Assert.False(BenchLook.Fits((-BenchLook.Overhang - 1, 0), 8, 8));
        Assert.False(BenchLook.Fits((BenchLook.Width + BenchLook.Overhang - 7, 0), 8, 8));
        Assert.False(BenchLook.Fits((0, -1), 8, 8));
        Assert.False(BenchLook.Fits((0, BenchLook.TableTop + BenchLook.Hang - 7), 8, 8));
    }

    [Fact]
    public void TheDrillLiesAsThePickedDesignDrewItItsCoilWhereTheBurstStrikes()
    {
        // The vise's manifest: the drill's top-left at (0, 4), its coil from (3, 7.5) to (11, 10.5).
        var drill = HeldLook.Drill;
        Assert.Equal((0, 4), BenchLook.ToolAt(drill));
        var box = drill.Mounts![0].Box;
        Assert.Equal((3, 7.5, 11, 10.5), (box.X0, box.Y0 + 4, box.X1, box.Y1 + 4));
    }

    [Fact]
    public void ItsPicturesAreThereTheJawsCloseWhereToolsAreClampedAndItsArcsLeaveTheShardAlight()
    {
        foreach (var name in (string[])[BenchLook.Body, BenchLook.Alight, BenchLook.Front])
            Assert.Equal((BenchLook.Width, BenchLook.Height), SizeOf($"res://art/{name}.png"));
        foreach (var name in (string[])[BenchLook.Glow, BenchLook.FitGlow])
            Assert.Equal((BenchLook.Width + 2 * BenchLook.GlowMargin, BenchLook.Height + 2 * BenchLook.GlowMargin), SizeOf($"res://art/{name}.png"));
        Assert.Equal($"machine_{BenchLook.Kind}", BenchLook.Body);
        // Two cells across, one tall: its picture rises a cell over its footprint, the vise over the bench.
        var spec = Spec.Machines.Get(BenchLook.Kind);
        Assert.Equal((2, 1), (spec.Width, spec.Height));
        Assert.Equal(BenchLook.Height - 16, BenchLook.TableTop - 1);

        var front = Pixels(Path.Combine(Art, $"{BenchLook.Front}.png"));
        var jaws = Enumerable.Range(0, 32 * 32).Where(i => front[i] >> 24 != 0).ToArray();
        Assert.Equal(BenchLook.Clamp.X, (jaws.Min(i => i % 32) + jaws.Max(i => i % 32) + 1) / 2.0, 3);
        Assert.Equal(BenchLook.Clamp.Y, (jaws.Min(i => i / 32) + jaws.Max(i => i / 32) + 1) / 2.0, 3);

        var alight = Pixels(Path.Combine(Art, $"{BenchLook.Alight}.png"));
        bool Lit(float x, float y) => alight[(int)y * 32 + (int)x] >> 24 != 0;
        Assert.True(Lit(BenchLook.Idle.X, BenchLook.Idle.Y));
        Assert.All(BenchLook.Sources, s => Assert.True(Lit(s.X, s.Y), $"{s}"));
        // Nothing of the body but the shard is drawn alight.
        var body = Pixels(Path.Combine(Art, $"{BenchLook.Body}.png"));
        for (int i = 0; i < 32 * 32; i++)
            if (alight[i] >> 24 != 0) Assert.True(body[i] >> 24 != 0, $"alight over nothing at {i % 32}, {i / 32}");
    }

    /// <summary>A square picture <paramref name="size"/> wide as the painter writes it (8-bit RGBA, each row unfiltered): each pixel's ARGB.</summary>
    private static uint[] Pixels(string file, int size = 32)
    {
        var png = File.ReadAllBytes(file);
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
        int row = 1 + size * 4;
        var pixels = new uint[size * size];
        for (int y = 0; y < size; y++)
        {
            Assert.Equal(0, bytes[y * row]);
            for (int x = 0; x < size; x++)
            {
                int o = y * row + 1 + x * 4;
                pixels[y * size + x] = (uint)bytes[o + 3] << 24 | (uint)bytes[o] << 16 | (uint)bytes[o + 1] << 8 | bytes[o + 2];
            }
        }
        return pixels;
    }
}
