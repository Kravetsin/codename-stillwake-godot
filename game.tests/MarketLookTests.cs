using FsCheck;
using FsCheck.Xunit;
using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The market's garbage, its decipher and its rows ([market-decrypt](../docs/decisions/2026-11-02-market-decrypt.md)).</summary>
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

    [Fact]
    public void ALineBreaksAtBlanks() =>
        Assert.Equal(["+1 trade station", "slot"], Cipher.Wrap("+1 trade station slot", 16));

    [Fact]
    public void AWordTooLongIsCutAndWhatIsOverTheLastLineWithIt()
    {
        Assert.Equal(["Unbreakable…"], Cipher.Wrap("Unbreakableword", 12));
        Assert.Equal(["one two", "three four…"], Cipher.Wrap("one two three four five", 7 + 4, 2));
    }

    [Theory]
    [InlineData(false, false, "research.locked", MarketCard.Sealed)]
    [InlineData(false, true, "research.noCredits", MarketCard.Dear)]
    [InlineData(false, true, null, MarketCard.Affordable)]
    [InlineData(true, true, "research.bought", MarketCard.Bought)]
    public void ACardsStateIsWhatTheSimSays(bool done, bool reached, string? problem, MarketCard state) =>
        Assert.Equal(state, MarketRows.StateOf(done, reached, problem));

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
    }

    [Fact]
    public void AsTheMarketComesUpADecipherHoldsOffForTheRise()
    {
        var reveal = new MarketReveal(Rows);
        reveal.Came();
        reveal.Play(0, 1);
        reveal.Step(MarketReveal.Rise / 2);
        Assert.Equal(0, reveal.StepOf(1));
        Assert.Equal(0, reveal.CardShown(1, 0, 20));
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
    public void GarbageIsOnlyALookPickingACardClearsIt()
    {
        var reveal = new MarketReveal(Rows);
        reveal.Play(0, 1);
        Assert.True(reveal.Deciphering(1, 0));
        reveal.ClearCard(1, 0);
        Assert.False(reveal.Deciphering(1, 0));
        Assert.Equal(20, reveal.CardShown(1, 0, 20));
        Assert.True(reveal.Deciphering(1, 1));
    }

    [Fact]
    public void AnOrderDeciphersItsCardsAndTheNextRowsHead()
    {
        var reveal = new MarketReveal(Rows);
        reveal.Play(0, 1);
        Assert.True(reveal.HeadDeciphering(2));
        Assert.False(reveal.HeadDeciphering(1));
        Assert.False(reveal.Deciphering(2, 0));
    }

    [Fact]
    public void ClosedMidDecipherEverythingIsClear()
    {
        var reveal = new MarketReveal(Rows);
        reveal.Play(0, 2);
        reveal.Step(0.3);
        reveal.Clear();
        Assert.False(reveal.Playing);
        Assert.False(reveal.Deciphering(1, 0));
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
            for (int card = 0; card < 5; card++)
            {
                Assert.False(reveal.Deciphering(row, card));
                Assert.Equal(30, reveal.CardShown(row, card, 30));
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
    public void EveryCardFinishesInsideItsDecipher(PositiveInt count)
    {
        var reveal = new MarketReveal(Rows);
        reveal.Play(0, 1);
        reveal.Step(MarketReveal.Takes * 0.999);
        Assert.Equal(count.Get, reveal.CardShown(1, 4, count.Get));
    }

    [Theory]
    [InlineData(1, new double[] { 0, 0, 1, 0, 0, 0 })]
    [InlineData(0, new double[] { 0, 1, 0, 0, 0, 0 })]
    [InlineData(5, new double[] { 0, 0, 0, 0, 0, 0 })]
    public void AtRestTheTallRowIsTheOrderBeingFilled(int met, double[] weights) =>
        Assert.Equal(weights, new MarketReveal(Rows).Weights(met));

    [Fact]
    public void TheTallRowPassesOnAsItsOrderDeciphers()
    {
        var reveal = new MarketReveal(Rows);
        reveal.Play(0, 1);
        var start = reveal.Weights(1);
        Assert.Equal(1, start[1]);
        Assert.Equal(0, start[2]);
        reveal.Step(MarketReveal.Takes);
        var end = reveal.Weights(1);
        Assert.Equal(0, end[1]);
        Assert.Equal(1, end[2]);
    }

    [Theory]
    [InlineData(new double[] { 0, 1, 0, 0, 0, 0 })]
    [InlineData(new double[] { 0, 0, 0, 0, 0, 0 })]
    [InlineData(new double[] { 0, 0.5, 0.5, 0, 0, 0 })]
    public void SixRowsFillTheViewExactlyWithOrWithoutATallOne(double[] weights)
    {
        var rows = MarketRows.Of(472, weights);
        Assert.Equal(0, rows[0].Top);
        Assert.Equal(472, rows[^1].Top + rows[^1].Tall, 2);
        Assert.All(rows, r => Assert.True(r.Tall >= MarketRows.Small, $"{r.Tall}"));
    }

    [Fact]
    public void CardsShareTheRowsWidth()
    {
        var (left, wide) = MarketRows.Card(926, 5, 4);
        Assert.Equal(926, left + wide, 2);
    }
}
