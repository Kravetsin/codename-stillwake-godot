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
    public void FullCellsFallingOnFallingWaterAreAllFallingButAPoolOverItsDrainIsNot()
    {
        // A shaft a cell wide, full cells from row 1 to 6 and 7 units in row 4, open air under; beside it a pool two deep over
        // a floor holed under its middle, water falling through the hole.
        int w = 7, h = 10;
        var layer = new byte[w * h];
        for (int y = 1; y < 7; y++) layer[y * w + 1] = L.Of(L.Water, y == 4 ? 7 : 8);
        for (int y = 0; y < 2; y++)
            for (int x = 3; x < 7; x++) layer[y * w + x] = L.Of(L.Water, 8);
        layer[2 * w + 5] = L.Of(L.Water, 8);
        layer[3 * w + 5] = L.Of(L.Water, 3);
        bool Blocks(int cell) => cell % w == 0 || cell % w == 2 || cell / w >= 2 && cell % w > 2 && cell % w != 5 || cell / w == h - 1;
        var texels = new byte[w * h * 4];
        WaterCells.Write(layer, w, h, Blocks, 0, 0, w, h, texels);
        Assert.All(Enumerable.Range(1, 6), y => Assert.Equal(255, texels[(y * w + 1) * 4 + 1]));
        Assert.All(Enumerable.Range(0, 2), y => Assert.Equal(0, texels[(y * w + 5) * 4 + 1]));
        Assert.Equal(255, texels[(2 * w + 5) * 4 + 1]);
    }

    [Fact]
    public void ABoxOfTheWorldIsDrawnAsTheWholeWorldDrawsItsCells()
    {
        // A sheet of full water three cells wide falling down a long shaft, its head far under the box drawn and its sides past
        // the box's: whether it falls is known only from its head, so the box must look past itself to draw it falling.
        int w = 7, h = 80;
        var layer = new byte[w * h];
        for (int y = 1; y < 60; y++)
            for (int x = 2; x < 5; x++) layer[y * w + x] = L.Of(L.Water, 8);
        bool Blocks(int cell) => cell % w is 0 or 6 || cell / w == h - 1;
        var whole = new byte[w * h * 4];
        WaterCells.Write(layer, w, h, Blocks, 0, 0, w, h, whole);
        Assert.Equal(255, whole[(10 * w + 3) * 4 + 1]);
        var box = new byte[2 * 10 * 4];
        WaterCells.Write(layer, w, h, Blocks, 3, 5, 2, 10, box);
        for (int y = 0; y < 10; y++)
            for (int x = 0; x < 2; x++)
                Assert.Equal(whole.AsSpan(((5 + y) * w + 3 + x) * 4, 4).ToArray(), box.AsSpan((y * 2 + x) * 4, 4).ToArray());
    }

    [Fact]
    public void FallingWaterInStripsIsDrawnWhole()
    {
        // A shaft three cells wide: full rows falling every other row, as a pool's water released down it falls; and two
        // trickles with the middle column dry between.
        int w = 5, h = 16;
        var layer = new byte[w * h];
        for (int y = 1; y < 8; y += 2)
            for (int x = 1; x < 4; x++) layer[y * w + x] = L.Of(L.Water, 8);
        for (int y = 9; y < 14; y++) layer[y * w + 1] = layer[y * w + 3] = L.Of(L.Water, 2);
        bool Blocks(int cell) => cell % w == 0 || cell % w == 4;
        var texels = new byte[w * h * 4];
        WaterCells.Write(layer, w, h, Blocks, 0, 0, w, h, texels);
        Assert.All(Enumerable.Range(1, 13), y => Assert.All(Enumerable.Range(1, 3), x =>
        {
            int at = (y * w + x) * 4;
            Assert.True(texels[at + 3] == 255 && texels[at + 1] == 255, $"({x}, {y}) is not drawn falling");
            Assert.True(y == 1 || texels[at] == 255, $"({x}, {y}) is not whole");
        }));
        Assert.Equal(0, texels[(14 * w + 2) * 4 + 3]);
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
    public void TheDrawnAmountGlidesToTheSimsAndADriedCellGoesAtOnce()
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
        // A dried cell goes at once, as in Terraria, rather than sinking to its floor: cells drained together would leave lines.
        var gone = One(0, 0);
        Assert.False(ease.Step(gone, 0, 0, 1, 1, 1.0 / 60));
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
        Assert.Equal(new Splashes.Cues(true, false, false), s.Step(true, false, S.SplashGapS));
        // A stroke due as the hero splashes in is not heard over the splash.
        s.Step(false, false, S.SplashGapS);
        Assert.Equal(new Splashes.Cues(true, false, false), s.Step(true, true, S.SplashGapS + S.StrokeEveryS));
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
