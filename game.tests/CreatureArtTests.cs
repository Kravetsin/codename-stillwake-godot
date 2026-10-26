using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// The creatures' looks (0102): every species drawn in a design the user picked, every part its rig names painted, and every
/// palette a planet recolours them by covering every ramp they are painted in.
/// </summary>
public class CreatureArtTests
{
    private static string Art => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art");

    public static TheoryData<string> Species() => [.. Spec.Creatures.All.Where(s => CreatureLooks.Varieties.ContainsKey(s.Id)).Select(s => s.Id)];

    [Theory]
    [MemberData(nameof(Species))]
    public void EverySpeciesHasALookWithItsGaitAndEveryPartItsRigNamesIsPaintedAtItsSize(string species)
    {
        string look = Spec.Creatures.Get(species).Look;
        Assert.True(CreatureArt.Designs.TryGetValue(look, out var design), $"{species}: no design {look}");
        Assert.True(CreatureLooks.Runs.ContainsKey(look), $"{look} has no gait");
        var ids = design.Rig.Select(n => n.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Single(design.Rig, n => n.Parent == null);
        Assert.All(design.Rig, n =>
        {
            Assert.True(n.Part == "eye" ? n.Drive == CreatureArt.Drive.Eye : design.Parts.ContainsKey(n.Part), $"{look}: {n.Id}'s part {n.Part}");
            // A parent is posed before its children are drawn from it: it comes in the rig, never the node itself.
            Assert.True(n.Parent == null || ids.Contains(n.Parent) && n.Parent != n.Id, $"{look}: {n.Id}'s parent {n.Parent}");
        });
        Assert.All(CreatureArt.EyeParts, p => Assert.True(design.Parts.ContainsKey(p), $"{look}: no {p}"));
        foreach (var (name, part) in design.Parts)
        {
            Assert.Equal((part.W, part.H), Size(Path.Combine(Art, "creatures", look, name + ".png")));
            Assert.Equal(part.Glow, File.Exists(Path.Combine(Art, "creatures", look, name + "_glow.png")));
        }
    }

    [Fact]
    public void EveryPaletteAPlanetUsesCoversEveryRampOfEveryPartInSixTones()
    {
        var palettes = CreatureLooks.PaletteOf.Values.Append(CreatureLooks.Palette).Distinct().ToList();
        Assert.All(Spec.PlanetTypes.All.Where(t => t.Creatures.Count > 0), t => Assert.True(CreatureLooks.PaletteOf.ContainsKey(t.Id), $"{t.Id} has no palette"));
        foreach (var key in palettes)
        {
            var palette = CreatureArt.Palettes[key];
            foreach (var design in CreatureArt.Designs.Values)
            {
                Assert.True(palette.ContainsKey(design.Glow));
                Assert.All(design.Parts, p => Assert.All(p.Value.Ramps, r => Assert.True(palette.TryGetValue(r, out var tones) && tones.Length == 6, $"{key} lacks ramp {r} of {design.Id}/{p.Key}")));
            }
        }
    }

    [Fact]
    public void EveryIndexMapNamesOnlyRampsItsPartListsAndTonesOfThem()
    {
        foreach (var design in CreatureArt.Designs.Values)
            foreach (var (name, part) in design.Parts)
            {
                var (w, h, pixels) = Pixels(Path.Combine(Art, "creatures", design.Id, name + ".png"));
                Assert.Equal((part.W, part.H), (w, h));
                for (int q = 0; q < w * h; q++)
                {
                    if (pixels[q * 4 + 3] == 0) continue;
                    Assert.Contains(pixels[q * 4], part.Ramps);
                    Assert.InRange(pixels[q * 4 + 1], 0, 5);
                }
            }
    }

    [Fact]
    public void EveryDesignsMarkingsAreToldApartAndAtLeastOneOfThemIsThere()
    {
        foreach (var design in CreatureArt.Designs.Values)
        {
            var (w, h, pixels) = Pixels(Path.Combine(Art, "creatures", design.Id, "marks.png"));
            int count = Features.Creatures.CreatureVariety.Markings(pixels, w, h).Max() + 1;
            Assert.InRange(count, 1, Features.Creatures.CreatureVariety.MostMarkings);
        }
    }
    private static (int W, int H) Size(string png)
    {
        Assert.True(File.Exists(png), png);
        var head = File.ReadAllBytes(png);
        return (head[16] << 24 | head[17] << 16 | head[18] << 8 | head[19], head[20] << 24 | head[21] << 16 | head[22] << 8 | head[23]);
    }

    /// <summary>An RGBA, 8-bit, unfiltered or filtered PNG's pixels, as the painter writes them.</summary>
    private static (int W, int H, byte[] Pixels) Pixels(string png)
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
