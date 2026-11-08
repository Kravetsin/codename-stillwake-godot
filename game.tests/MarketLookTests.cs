using FsCheck;
using FsCheck.Xunit;
using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The market's garbage, its decipher and its grid ([market-grid](../docs/decisions/2026-11-03-market-grid.md)).</summary>
public class MarketLookTests
{
    private const int Rows = 6;

    private static string Words(NonNull<string> text) => text.Get.Replace('\n', ' ');

    [Property]
    public void GarbageKeepsTheLengthAndTheBlanks(NonNull<string> text, uint place, PositiveInt step)
    {
        string words = Words(text), made = Cipher.Of(words, place, 0, step.Get);
        Assert.Equal(words.Length, made.Length);
        for (int i = 0; i < words.Length; i++) Assert.Equal(words[i] == ' ', made[i] == ' ');
    }

    [Property]
    public void TheSamePlaceIsTheSameGarbage(NonNull<string> text, NonNull<string> name, PositiveInt step)
    {
        uint place = Cipher.Place(name.Get);
        Assert.Equal(Cipher.Of(Words(text), place, 0, step.Get), Cipher.Of(Words(text), Cipher.Place(name.Get), 0, step.Get));
    }

    [Fact]
    public void WhatHasClearedShowsAndTheRestDoesNot()
    {
        string made = Cipher.Of("+1 slot", 7, 3);
        Assert.StartsWith("+1 ", made);
        Assert.NotEqual("slot", made[3..]);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 2)]
    [InlineData(3, 4)]
    public void RowsOpenedTogetherStartTogether(int seen, int met)
    {
        var reveal = new MarketReveal(Rows);
        reveal.Play(seen, met);
        reveal.Step(0.4);
        Assert.Equal(reveal.StepOf(met), reveal.StepOf(seen + 1));
        Assert.NotEqual(0, reveal.StepOf(met));
        Assert.Equal(0, reveal.StepOf(seen));
        Assert.False(reveal.Plays(seen));
    }

    [Fact]
    public void AsTheMarketComesUpADecipherHoldsOffForTheRise()
    {
        var reveal = new MarketReveal(Rows);
        reveal.Came();
        reveal.Play(0, 1);
        reveal.Step(MarketReveal.Rise / 2);
        Assert.Equal(0, reveal.StepOf(1));
        Assert.Equal(0, reveal.CardShown("bag", 1, 0, 1, 20));
        reveal.Step(MarketReveal.Rise);
        Assert.NotEqual(0, reveal.StepOf(1));
    }

    [Fact]
    public void AnOrderFilledWithTheMarketOpenPlaysAtOnce()
    {
        var reveal = new MarketReveal(Rows);
        reveal.Came();
        reveal.Step(MarketReveal.Rise);
        reveal.Play(0, 1);
        reveal.Step(0.1);
        Assert.NotEqual(0, reveal.StepOf(1));
    }

    [Fact]
    public void GarbageIsOnlyALookPickingAGoodClearsIt()
    {
        var reveal = new MarketReveal(Rows);
        reveal.Play(0, 1);
        Assert.True(reveal.Deciphering("bag", 1, 0, 2));
        reveal.ClearCard("bag");
        Assert.False(reveal.Deciphering("bag", 1, 0, 2));
        Assert.Equal(20, reveal.CardShown("bag", 1, 0, 2, 20));
        Assert.True(reveal.Deciphering("belt", 1, 1, 2));
    }

    [Fact]
    public void AnOrderDeciphersItsGoodsAndTheNextOrdersHead()
    {
        var reveal = new MarketReveal(Rows);
        reveal.Play(0, 1);
        Assert.True(reveal.HeadDeciphering(2));
        Assert.False(reveal.HeadDeciphering(1));
        Assert.False(reveal.Deciphering("bag", 2, 0, 1));
    }

    [Fact]
    public void ClosedMidDecipherEverythingIsClear()
    {
        var reveal = new MarketReveal(Rows);
        reveal.Play(0, 2);
        reveal.Step(0.3);
        reveal.Clear();
        Assert.False(reveal.Playing);
        Assert.False(reveal.Deciphering("bag", 1, 0, 1));
    }

    [Property]
    public void ADecipherEndsAllClear(NonNegativeInt seen, NonNegativeInt met, PositiveInt frames)
    {
        int count = frames.Get % 200 + 1;
        var reveal = new MarketReveal(Rows);
        reveal.Play(seen.Get % Rows, met.Get % Rows);
        for (int i = 0; i < count; i++) reveal.Step((MarketReveal.Takes + 0.01) / count);
        Assert.False(reveal.Playing);
        for (int row = 0; row < Rows; row++)
        {
            Assert.False(reveal.HeadDeciphering(row));
            Assert.Equal(30, reveal.HeadShown(row, 30));
            for (int tile = 0; tile < 5; tile++)
            {
                Assert.False(reveal.Deciphering("t" + tile, row, tile, 5));
                Assert.Equal(30, reveal.CardShown("t" + tile, row, tile, 5, 30));
            }
        }
    }

    [Property]
    public void SeenAsMetPlaysNothing(NonNegativeInt met)
    {
        var reveal = new MarketReveal(Rows);
        reveal.Play(met.Get, met.Get);
        Assert.False(reveal.Playing);
    }

    [Property]
    public void EveryTileFinishesInsideItsDecipher(PositiveInt count)
    {
        int tiles = count.Get % 100 + 1;
        var reveal = new MarketReveal(Rows);
        reveal.Play(0, 1);
        reveal.Step(MarketReveal.Takes * 0.95);
        Assert.Equal(30, reveal.CardShown("last", 1, tiles - 1, tiles, 30));
    }

    [Fact]
    public void TheReferenceWindowHoldsTwelveByFive()
    {
        var (wide, tall) = MarketGrid.GridIn(1188, 472);
        var (columns, rows, tileWide, tileTall) = MarketGrid.Of(wide, tall);
        Assert.Equal((12, 5), (columns, rows));
        Assert.Equal(75, tileWide, 2);
        Assert.True(tileTall >= MarketGrid.LeastTall);
        Assert.Equal(60, MarketGrid.Capacity(1188, 472));
    }

    [Property]
    public void TilesFillTheGridAndAreNeverUnderTheLeast(PositiveInt wide, PositiveInt tall)
    {
        float w = MarketGrid.LeastWide + wide.Get % 3000, h = MarketGrid.LeastTall + tall.Get % 2000;
        var (columns, rows, tileWide, tileTall) = MarketGrid.Of(w, h);
        Assert.Equal(w, columns * tileWide + MarketGrid.Gap * (columns - 1), 2);
        Assert.Equal(h, rows * tileTall + MarketGrid.Gap * (rows - 1), 2);
        Assert.True(tileWide >= MarketGrid.LeastWide - 0.01f && tileTall >= MarketGrid.LeastTall - 0.01f, $"{tileWide} by {tileTall}");
    }
}
