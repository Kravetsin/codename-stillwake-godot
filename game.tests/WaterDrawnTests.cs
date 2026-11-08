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
    public void TheTextureMarksLevelsFallingAndCoveredCells()
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
        Assert.Equal(new byte[] { 4 * 255 / L.Full, 255, 0, 255 }, texels[(2 * 4)..(2 * 4 + 4)]);
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, texels[0..4]);
        Assert.False(WaterCells.Write(new byte[w * h], w, h, Blocks, 0, 0, w, h, texels));
    }

    [Fact]
    public void AFallingColumnIsDrawnFullDownToItsHeadAndItsTopToItsLevel()
    {
        // A shaft a cell wide: 2 units in each of rows 2 to 9, open air under them.
        int w = 3, h = 14;
        var layer = new byte[w * h];
        for (int y = 2; y < 10; y++) layer[y * w + 1] = L.Of(L.Water, 2);
        bool Blocks(int cell) => cell % w != 1;
        var texels = new byte[w * h * 4];
        WaterCells.Write(layer, w, h, Blocks, 0, 0, w, h, texels);
        byte[] At(int y) => texels[((y * w + 1) * 4)..((y * w + 1) * 4 + 4)];
        Assert.Equal(new byte[] { 2 * 255 / L.Full, 255, 0, 255 }, At(2));
        Assert.All(Enumerable.Range(3, 7), y => Assert.Equal(new byte[] { 255, 255, 255, 255 }, At(y)));
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, At(10));
    }

    [Fact]
    public void ACellNoLongerCoveredSinksToItsOwnLevel()
    {
        var ease = new WaterEase();
        var t = new byte[] { 255, 255, 255, 255 };
        ease.Step(t, 0, 0, 1, 1, 1.0 / 60);
        ease.Step(t = [63, 255, 0, 255], 0, 0, 1, 1, 1.0 / 60);
        Assert.InRange(t[0], 64, 254);
        for (int i = 0; i < 60; i++) ease.Step(t = [63, 255, 0, 255], 0, 0, 1, 1, 1.0 / 60);
        Assert.Equal(63, t[0]);
    }

    [Fact]
    public void TheDrawnAmountGlidesToTheSimsAndADriedCellFadesOut()
    {
        var ease = new WaterEase();
        byte[] One(byte r, byte a) => [r, 0, 0, a];
        var t = One(0, 0);
        ease.Step(t, 0, 0, 1, 1, 0.05);
        int last = 0;
        for (int i = 0; i < 30; i++)
        {
            t = One(255, 255);
            ease.Step(t, 0, 0, 1, 1, 1.0 / 60);
            Assert.True(t[0] >= last, "it only rises");
            last = t[0];
        }
        Assert.Equal(255, last);
        Assert.True(t[0] > 0 && One(0, 0) is var fading && ease.Step(fading, 0, 0, 1, 1, 1.0 / 60) && fading[0] is > 0 and < 255, "a dried cell still shows");
        var gone = One(0, 0);
        for (int i = 0; i < 120; i++) ease.Step(gone = One(0, 0), 0, 0, 1, 1, 1.0 / 60);
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, gone);
        var entering = One(200, 255);
        ease.Step(entering, 5, 0, 1, 1, 1.0 / 60);
        Assert.Equal(200, entering[0]);
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
