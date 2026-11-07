using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Liquids;
using Xunit;
using L = Stillwake.Content.Liquids;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Tests;

/// <summary>Water drawn and heard (liquids): the cells the shader reads, the splashes and strokes a swimmer sets off.</summary>
public class WaterDrawnTests
{
    [Fact]
    public void TheTextureMarksAmountsStreamsAndCoveredCells()
    {
        // A 3 × 3 box: a full cell over a full one in the middle column, a falling 4 beside them over open air, dry elsewhere.
        int w = 3, h = 3;
        var layer = new byte[w * h];
        layer[1] = L.Of(L.Water, 8);
        layer[4] = L.Of(L.Water, 8);
        layer[2] = L.Of(L.Water, 4);
        bool Blocks(int cell) => cell / w == 2 && cell % w != 2;
        var texels = new byte[w * h * 4];
        Assert.True(WaterCells.Write(layer, w, h, Blocks, 0, 0, w, h, texels));
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, texels[(1 * 4)..(1 * 4 + 4)]);
        Assert.Equal(new byte[] { 255, 0, 255, 255 }, texels[(4 * 4)..(4 * 4 + 4)]);
        Assert.Equal(new byte[] { 127, 255, 0, 255 }, texels[(2 * 4)..(2 * 4 + 4)]);
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, texels[0..4]);
        Assert.False(WaterCells.Write(new byte[w * h], w, h, Blocks, 0, 0, w, h, texels));
    }

    [Fact]
    public void ASwimmerSplashesInAndOutButNotTwiceWithinTheGap()
    {
        var s = new Splashes();
        Assert.Equal(default, s.Step(true, false, 0.1));
        Assert.True(s.Step(false, false, 0.1).Out);
        Assert.False(s.Step(true, false, S.SplashGapS / 2).In);
        Assert.True(s.Step(false, false, S.SplashGapS).Out);
    }

    [Fact]
    public void AStrokeIsHeardEveryStrokeWhileMovingOnly()
    {
        var s = new Splashes();
        s.Step(true, false, 0.1);
        int strokes = 0;
        for (int i = 0; i < 100; i++) strokes += s.Step(true, true, S.StrokeEveryS / 10).Stroke ? 1 : 0;
        Assert.InRange(strokes, 9, 11);
        for (int i = 0; i < 100; i++) Assert.False(s.Step(true, false, S.StrokeEveryS / 10).Stroke);
    }
}
