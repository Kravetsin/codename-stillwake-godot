using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The hero's looks (<see cref="HeroLooks"/>): as many pictures in <c>game/art/hd</c> as the counts say.</summary>
public class HeroLooksTests
{
    [Theory]
    [InlineData("hair", HeroLooks.HairStyles)]
    [InlineData("face", HeroLooks.Faces)]
    [InlineData("beard", HeroLooks.Beards)]
    public void EveryPictureCountedIsPainted(string kind, int count)
    {
        bool Painted(int i) => Png.Has($"hero_{kind}_{i}");
        Assert.All(Enumerable.Range(0, count), i => Assert.True(Painted(i), $"hero_{kind}_{i}.png"));
        Assert.False(Painted(count), $"hero_{kind}_{count}.png is painted but not counted");
    }

    [Theory]
    [InlineData(0, 6, 0)]
    [InlineData(5, 6, 5)]
    [InlineData(6, 6, 0)]
    [InlineData(-1, 6, 5)]
    [InlineData(-13, 6, 5)]
    public void AnIndexOutOfThePaletteIsWrappedNeverNegative(int i, int count, int drawn) => Assert.Equal(drawn, HeroLooks.Wrap(i, count));
}
