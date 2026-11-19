using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The tablet's top edge along the window's foot: the rim, the way up from it, and the keys on it.</summary>
public class TabletPeekTests
{
    private const float Wide = 1280, Tall = 720;

    [Fact]
    public void TheRimsArtKeepsItsMarginFromBothSides()
    {
        const int s = TabletLayout.ArtScale;
        float artLeft = TabletPeek.ScreenLeft(Wide) - TabletLayout.ScreenLeft * s;
        float artWide = TabletPeek.ScreenWidth(Wide) + TabletLayout.ArtAround * s;
        Assert.Equal(TabletPeek.Margin, artLeft);
        Assert.Equal(TabletPeek.Margin, Wide - artLeft - artWide);
    }

    [Fact]
    public void ALittleOfTheDarkScreenShowsAboveTheFoot()
    {
        Assert.Equal(714, TabletPeek.ScreenTop(Tall));
        Assert.Equal(682, TabletPeek.RimArtTop(Tall));
    }

    /// <summary>The keys stay where they stood over the rim (holo windows); a window rests a margin above them, higher as they grow.</summary>
    [Fact]
    public void TheKeysStayAtTheFootAndAWindowRestsAboveThem()
    {
        Assert.Equal(665, TabletPeek.KeysTop(Tall, 1));
        Assert.Equal(665 - TabletPeek.Margin, TabletPeek.Floor(Tall, 1));
        Assert.True(TabletPeek.Floor(Tall, 1.5f) < TabletPeek.Floor(Tall, 1));
    }

    [Fact]
    public void TheKeysFloatOverTheBezelsTopInTheMiddleOfItsPlate()
    {
        // The art at the rim's place: 12 in, 682 down, 1256 across, what the page the look was picked on drew.
        var (middle, top) = TabletPeek.RowAt(12, 682, 1256);
        Assert.Equal(632, middle);
        Assert.Equal(665, top);
    }

    [Fact]
    public void OverATabletAtTheWindowsTopTheKeysStayOnScreen() => Assert.Equal(TabletPeek.RowLeast, TabletPeek.RowAt(12, 12, 1256).Top);
}
