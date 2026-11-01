using Stillwake.Game.Features.Creatures;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The creatures in ink (2026-11-01-creatures-in-ink): each design whole, its gait and pictures there at their size, its feet on the
/// ground as it walks, each individual its own by a tint and its burst read from its picture.
/// </summary>
public class CreatureInkTests
{
    private static string Art => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art", "hd", "creatures");

    public static TheoryData<string> Looks() => [.. CreatureArt.InkDesigns.Keys];

    [Theory]
    [MemberData(nameof(Looks))]
    public void AnInkDesignStandsForALookAndEveryPartOfItsRigIsDrawnAtFourTimes(string look)
    {
        Assert.True(CreatureArt.Designs.ContainsKey(look), look);
        var design = CreatureArt.InkDesigns[look];
        Assert.True(design.Ink);
        Assert.True(CreatureLooks.Runs.ContainsKey(design.Id), $"{design.Id} has no gait");
        Assert.All(design.Rig, n => Assert.True(n.Part == "eye" || design.Parts.ContainsKey(n.Part), $"{n.Id}'s part {n.Part}"));
        foreach (var (name, part) in design.Parts)
        {
            using var png = new BinaryReader(File.OpenRead(Path.Combine(Art, design.Id, name + ".png")));
            png.BaseStream.Seek(16, SeekOrigin.Begin);
            Assert.Equal((part.W * 4, part.H * 4), (BigEndian(png), BigEndian(png)));
        }
    }

    [Theory]
    [MemberData(nameof(Looks))]
    public void WalkingItsLowestFootStaysOnTheGround(string look)
    {
        var design = CreatureArt.InkDesigns[look];
        var (low, high) = LowestFootThroughAStride(design);
        Assert.InRange(low, -0.01, 0.01);
        Assert.InRange(high, -0.01, 0.01);
    }

    [Fact]
    public void WithTheFixedBobALongLeggedGaitLiftsItsFeetOffTheGround()
    {
        // Proves the test above bites: the same design without its legs' length is left to the fixed bob, and floats.
        var design = CreatureArt.InkDesigns["classic"];
        var bobbing = design with { Pose = design.Pose with { LegLen = 0 } };
        var (low, _) = LowestFootThroughAStride(bobbing, design.Pose.LegLen, design.Pose.FootW);
        Assert.True(low < -0.5, $"{low}");
    }

    /// <summary>Where the lowest foot rests against where it was drawn to through a stride's walk: 0 on the ground, below 0 in the air.</summary>
    private static (double Low, double High) LowestFootThroughAStride(CreatureArt.Design design, double legLen = -1, double footW = -1)
    {
        legLen = legLen < 0 ? design.Pose.LegLen : legLen;
        footW = footW < 0 ? design.Pose.FootW : footW;
        var pose = new CreaturePose(design, CreatureLooks.Runs[design.Id]);
        int body = design.Rig.ToList().FindIndex(n => n.Drive == CreatureArt.Drive.Body);
        var (low, high) = (double.PositiveInfinity, double.NegativeInfinity);
        for (int k = 0; k < 40; k++)
        {
            pose.Set(new CreaturePose.Doing(k / 40.0, 1, 0, 0, 0, 0, 0, 0, 0, 1, null));
            double foot = double.NegativeInfinity;
            for (int i = 0; i < design.Rig.Count; i++)
            {
                if (design.Rig[i].Drive != CreatureArt.Drive.Leg) continue;
                var j = pose.Joints[i];
                foot = Math.Max(foot, j.Y + legLen * (Math.Cos(j.A) - 1) + footW * Math.Abs(Math.Sin(j.A)));
            }
            double rest = pose.Joints[body].Y + foot;
            (low, high) = (Math.Min(low, rest), Math.Max(high, rest));
        }
        return (low, high);
    }

    [Fact]
    public void APlainIndividualKeepsThePictureAndNeighboursDiffer()
    {
        Assert.Equal((1f, 1f, 1f), CreatureVariety.InkTint(CreatureVariety.Plain));
        var v = CreatureLooks.Varieties["loper"];
        var tints = Enumerable.Range(1, 4).Select(id => CreatureVariety.InkTint(CreatureVariety.Of(v, 7, id))).ToList();
        Assert.Equal(tints.Count, tints.Distinct().Count());
    }

    [Fact]
    public void AWarmTintIsWarmAndADarkShadeDark()
    {
        var warm = CreatureVariety.InkTint(CreatureVariety.Plain with { Hue = 2 });
        Assert.True(warm.R > warm.G && warm.G > warm.B);
        var dark = CreatureVariety.InkTint(CreatureVariety.Plain with { Shade = -4 });
        Assert.True(dark.R < 1 && dark.R == dark.G && dark.G == dark.B);
    }

    [Fact]
    public void AReducedPictureIsSolidWhereHalfCoveredItsColourWeighedByCover()
    {
        // Two squares of 2 × 2: three of four covered, one red with half cover and two blue solid; then one of four.
        byte[] rgba =
        [
            255, 0, 0, 128, 0, 0, 255, 255, 9, 9, 9, 0, 9, 9, 9, 0,
            0, 0, 255, 255, 0, 0, 0, 0, 9, 9, 9, 255, 9, 9, 9, 0,
        ];
        var reduced = CreatureVariety.Reduced(rgba, 4, 2, 2);
        Assert.Equal(8, reduced.Length);
        Assert.Equal(255, reduced[3]);
        Assert.True(reduced[2] > reduced[0] * 3, "blue outweighs the half-covered red");
        Assert.Equal(0, reduced[7]);
    }

    private static int BigEndian(BinaryReader r)
    {
        var b = r.ReadBytes(4);
        return b[0] << 24 | b[1] << 16 | b[2] << 8 | b[3];
    }
}
