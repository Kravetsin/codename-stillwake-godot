using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The hero's looks (<see cref="HeroLooks"/>) match what <c>tools/hero-art</c> paints: its ramps, the ramps the pictures are
/// drawn in, and as many pictures in <c>game/art</c> as the counts say.
/// </summary>
public class HeroLooksTests
{
    private static string Here([CallerFilePath] string path = "") => path;

    private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, ".."));

    private static readonly string Parts = File.ReadAllText(Path.Combine(Root, "tools/hero-art/parts.js"));

    /// <summary>The ramps of <c>const <paramref name="name"/> = [...]</c> in <c>parts.js</c>, each its four colours.</summary>
    private static int[][] RampsOf(string name)
    {
        var block = Regex.Match(Parts, $@"const {name} = \[(.*?)\];", RegexOptions.Singleline).Groups[1].Value;
        return Regex.Matches(block, @"\[([^\]]*)\]")
            .Select(r => Regex.Matches(r.Groups[1].Value, "#([0-9a-f]{6})").Select(c => Convert.ToInt32(c.Groups[1].Value, 16)).ToArray())
            .ToArray();
    }

    private static int ConstOf(string name) => int.Parse(Regex.Match(Parts, $@"const {name} = (\d+);").Groups[1].Value);

    [Fact]
    public void TheRampsAreThePaintersOwn()
    {
        Assert.Equal(RampsOf("SKINS"), HeroLooks.Skins);
        Assert.Equal(RampsOf("HAIRS"), HeroLooks.Hairs);
        Assert.Equal(HeroLooks.SkinDrawn, ConstOf("SKIN_DRAWN"));
        Assert.Equal(HeroLooks.HairDrawn, ConstOf("HAIR_DRAWN"));
    }

    [Theory]
    [InlineData("hair", HeroLooks.HairStyles)]
    [InlineData("face", HeroLooks.Faces)]
    [InlineData("beard", HeroLooks.Beards)]
    public void EveryPictureCountedIsPainted(string kind, int count)
    {
        bool Painted(int i) => File.Exists(Path.Combine(Root, $"game/art/hero_{kind}_{i}.png"));
        Assert.All(Enumerable.Range(0, count), i => Assert.True(Painted(i), $"hero_{kind}_{i}.png"));
        Assert.False(Painted(count), $"hero_{kind}_{count}.png is painted but not counted");
    }

    /// <summary>The bare shin's shoe ends on the boot's row: both hang from one knee, so a shorter one stands above the floor.</summary>
    [Fact]
    public void TheBareFootStandsWhereTheBootDoes()
    {
        static int Lowest(string part)
        {
            var (w, h, pixels) = CreatureArtTests.Pixels(Path.Combine(Root, $"game/art/hero_{part}.png"));
            return Enumerable.Range(0, h).Last(y => Enumerable.Range(0, w).Any(x => pixels[(y * w + x) * 4 + 3] > 0));
        }
        Assert.Equal(Lowest("shin"), Lowest("jumpsuit_shin"));
    }

    [Theory]
    [InlineData(0, 6, 0)]
    [InlineData(5, 6, 5)]
    [InlineData(6, 6, 0)]
    [InlineData(-1, 6, 5)]
    [InlineData(-13, 6, 5)]
    public void AnIndexOutOfThePaletteIsWrappedNeverNegative(int i, int count, int drawn) => Assert.Equal(drawn, HeroLooks.Wrap(i, count));
}
