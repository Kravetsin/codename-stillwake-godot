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
        // Falling, it is a stream: its 4 units over the 3 open rows of its column, falling Fall a tick.
        Assert.Equal(new byte[] { 4 * L.Fall / 3 * 255 / L.Full, 255, 0, 255 }, texels[(2 * 4)..(2 * 4 + 4)]);
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, texels[0..4]);
        Assert.False(WaterCells.Write(new byte[w * h], w, h, Blocks, 0, 0, w, h, texels));
    }

    [Fact]
    public void AStreamFallingInLumpsIsDrawnWholeAndEven()
    {
        // A shaft a cell wide: a lump of 8 every Fall rows, as a stream of 8 a tick falls, over a pool at the bottom.
        int w = 3, h = 16;
        var layer = new byte[w * h];
        for (int y = 0; y < 12; y += L.Fall) layer[y * w + 1] = L.Of(L.Water, 8);
        for (int y = 12; y < h; y++) layer[y * w + 1] = L.Of(L.Water, 8);
        bool Blocks(int cell) => cell % w != 1;
        var texels = new byte[w * h * 4];
        WaterCells.Write(layer, w, h, Blocks, 0, 0, w, h, texels);
        // Where the window lies wholly in the stream, its width is the same at every cell.
        var widths = Enumerable.Range(WaterCells.Window, 12 - 2 * WaterCells.Window + 1).Select(y => texels[(y * w + 1) * 4]).ToArray();
        Assert.All(Enumerable.Range(0, 12), y => Assert.Equal(255, texels[(y * w + 1) * 4 + 1]));
        Assert.InRange(widths.Max() - widths.Min(), 0, 255 / L.Full);
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
