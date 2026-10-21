using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How the arc makers' lamp hung aboard looks (0091): the lantern of its icon in every state, its glass alight only while it shines.</summary>
public class ArcLampLooksTests
{
    private static string Art => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art");

    [Fact]
    public void ItsBodyIsDrawnWholeOffOnAndFlickeringItsGlassAlightAndGlowingAsBrightAsItShines()
    {
        // Off, unpowered, or dark in a flicker: the body alone, never nothing.
        Assert.Equal(new ArcLampLooks.Look(1, 0, 0), ArcLampLooks.Drawn(0));
        // On: the body still, the glass alight over it and its glow.
        Assert.Equal(new ArcLampLooks.Look(1, 1, 1), ArcLampLooks.Drawn(1));
        // Short of power, as bright as it shines; past whole its strength's light, the pictures no more than whole.
        Assert.Equal(new ArcLampLooks.Look(1, 0.4f, 0.4f), ArcLampLooks.Drawn(0.4f));
        Assert.Equal(new ArcLampLooks.Look(1, 1, 1), ArcLampLooks.Drawn(1.3f));
        Assert.All((string[])[ArcLampLooks.Body, ArcLampLooks.Alight, ArcLampLooks.Glow], p => Assert.True(File.Exists(Path.Combine(Art, $"{p}.png")), p));
        Assert.Equal($"machine_{ArcLampLooks.Kind}", ArcLampLooks.Body);
    }

    [Fact]
    public void ItIsTheLanternOfItsIconItsGlassAloneAlightAndItsBodyAndGlassTogetherTheIconWhole()
    {
        var icon = Pixels(Path.Combine(Art, "items", "arcLamp.png"));
        var body = Pixels(Path.Combine(Art, $"{ArcLampLooks.Body}.png"));
        var alight = Pixels(Path.Combine(Art, $"{ArcLampLooks.Alight}.png"));
        // Lit, its picture is its icon's, pixel for pixel; off, the same lantern, its glass gone cold.
        for (int i = 0; i < 16 * 16; i++)
        {
            uint lit = alight[i] >> 24 != 0 ? alight[i] : body[i];
            Assert.True(lit == icon[i], $"pixel {i % 16}, {i / 16}");
            Assert.Equal(icon[i] >> 24 != 0, body[i] >> 24 != 0);
        }
        // What lights is its glass, round its middle.
        var glass = Enumerable.Range(0, 256).Where(i => alight[i] >> 24 != 0).ToArray();
        Assert.Equal(ArcLampLooks.Glass.X, (glass.Min(i => i % 16) + glass.Max(i => i % 16) + 1) / 2f, 3);
        Assert.Equal(ArcLampLooks.Glass.Y, (glass.Min(i => i / 16) + glass.Max(i => i / 16) + 1) / 2f, 3);
    }

    [Fact]
    public void ItCracklesOnlyAtFullPowerTheHomesLanternsArcAcrossItsGlass()
    {
        Assert.True(ArcLampLooks.Crackles(1));
        Assert.False(ArcLampLooks.Crackles(0.6f));
        Assert.False(ArcLampLooks.Crackles(0));
        // The homes' lantern's arc, its length and tilt, struck from glass to glass of this one's, never on its cage.
        var arc = ArcLampLooks.Arc;
        var lantern = HomesArt.Of["lantern"].Emitters.First(e => e.Kind == Emits.Arc);
        Assert.Equal((lantern.Rate, lantern.Life, lantern.Width), (arc.Rate, arc.Life, arc.Width));
        Assert.Equal(lantern.X2 - lantern.X, arc.X2 - arc.X, 3);
        Assert.Equal(lantern.Y2 - lantern.Y, arc.Y2 - arc.Y, 3);
        var alight = Pixels(Path.Combine(Art, $"{ArcLampLooks.Alight}.png"));
        bool Lit(float x, float y) => alight[(int)y * 16 + (int)x] >> 24 != 0;
        Assert.True(Lit(arc.X, arc.Y) && Lit(arc.X2, arc.Y2) && Lit((arc.X + arc.X2) / 2, (arc.Y + arc.Y2) / 2));
    }

    /// <summary>A 16 × 16 picture as the export writes it (8-bit RGBA, each row unfiltered): each pixel's ARGB.</summary>
    private static uint[] Pixels(string file)
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
        Assert.Equal(16 * (1 + 16 * 4), bytes.Length);
        var pixels = new uint[16 * 16];
        for (int y = 0; y < 16; y++)
        {
            Assert.Equal(0, bytes[y * 65]);
            for (int x = 0; x < 16; x++)
            {
                int o = y * 65 + 1 + x * 4;
                pixels[y * 16 + x] = (uint)bytes[o + 3] << 24 | (uint)bytes[o] << 16 | (uint)bytes[o + 1] << 8 | bytes[o + 2];
            }
        }
        return pixels;
    }
}
