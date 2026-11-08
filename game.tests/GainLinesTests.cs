using Stillwake.Game.Features.Items;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A place's lines of gains (2026-11-06-gain-spots): merging, flaring, fading and rows that keep still.</summary>
public class GainLinesTests
{
    private const double Frame = 1000.0 / 60;

    [Fact]
    public void ARepeatWhileALineShowsAddsToItAndFlaresPastItsTimeANewOneOpens()
    {
        var lines = new GainLines();
        lines.Add("ore", 1);
        Assert.Equal(0, lines.Pop(lines.Lines[0]));
        lines.Update(GainLines.HoldMs + GainLines.FadeMs / 2);
        lines.Add("ore", 2);
        var line = Assert.Single(lines.Lines);
        Assert.Equal(3, line.Count);
        lines.Update(GainLines.PopRiseMs);
        Assert.Equal(1, lines.Pop(line));
        Assert.Equal(1, lines.Alpha(line));
        lines.Update(GainLines.HoldMs + GainLines.FadeMs);
        Assert.Empty(lines.Lines);
        lines.Add("ore", 1);
        Assert.Equal(1, Assert.Single(lines.Lines).Count);
    }

    [Fact]
    public void FadesInHoldsThenFadesOut()
    {
        var lines = new GainLines();
        lines.Add("ore", 1);
        var line = lines.Lines[0];
        Assert.Equal(0, lines.Alpha(line));
        lines.Update(GainLines.HoldMs - 1);
        Assert.Equal(1, lines.Alpha(line));
        lines.Update(1 + GainLines.FadeMs / 2);
        Assert.Equal(0.5, lines.Alpha(line), 3);
    }

    [Fact]
    public void TheOldestIsLowestAndAGrowingLineKeepsItsRow()
    {
        var lines = new GainLines();
        lines.Add("ore", 1);
        lines.Add("scrap", 3);
        Settle(lines);
        lines.Add("ore", 1);
        Settle(lines);
        Assert.Equal(["ore", "scrap"], lines.Lines.Select(l => l.Key));
        Assert.Equal([0, 1], lines.Lines.Select(l => Math.Round(l.Row, 3)));
    }

    [Fact]
    public void ANewItemPastTheMostPushesTheOldestOff()
    {
        var lines = new GainLines();
        for (int i = 0; i <= GainLines.Most; i++) lines.Add("item" + i, 1);
        Assert.Equal(GainLines.Most, lines.Lines.Count);
        Assert.Equal("item1", lines.Lines[0].Key);
    }

    private static void Settle(GainLines lines)
    {
        for (int i = 0; i < 60; i++) lines.Update(Frame);
    }
}
