using Stillwake.Game.Features.Scanner;
using Xunit;

namespace Stillwake.Game.Tests;

// The picked readout's digits and box (2026-11-07-the-scanner), as the page drew them.
public class PhosphorTests
{
    [Fact]
    public void TheBoxSitsCentredOverThePointWithItsGap()
    {
        // Room for 3 digits and "/200": 11 and 15 pixels, a pixel between, 3 inside each side: 33, its left 17 (16.5 away from zero) left.
        Assert.Equal((100 - 17, 200 - 4 - 11, 33, 11), Phosphor.Box(100, 200, "/200"));
    }

    [Fact]
    public void AnOddWidthLeansLeftByHalfAPixelRoundedAway()
    {
        // Room for a digit and "/9": 3 and 7, plus 1 and 6: 17 wide, its left 9 (8.5 away from zero) left of the point.
        Assert.Equal(10 - 9, Phosphor.Box(10, 50, "/9").X);
    }

    [Fact]
    public void TheValueLeftHasRoomForEveryDigitOfTheMaximum()
    {
        // 9 of 120 takes the room of three digits, so the box does not jump as the value loses one.
        Assert.Equal(Phosphor.Width("120"), Phosphor.LeftWidth("/120"));
    }

    [Theory]
    [InlineData(0.4, 1)]
    [InlineData(2.4, 2)]
    [InlineData(2.5, 3)]
    [InlineData(3.7, 4)]
    public void APixelTakesWholeScreenPixels(double zoom, int scale) => Assert.Equal(scale, Phosphor.Scale(zoom));

    [Fact]
    public void DigitsAndTheSlashAreTheirPictures()
    {
        // A one's stem is its middle column; a zero is hollow; the slash runs from top right to bottom left.
        Assert.True(Phosphor.Lit('1', 1, 4) && !Phosphor.Lit('1', 0, 2));
        Assert.True(Phosphor.Lit('0', 0, 2) && !Phosphor.Lit('0', 1, 2));
        Assert.True(Phosphor.Lit('/', 2, 0) && Phosphor.Lit('/', 0, 4) && !Phosphor.Lit('/', 0, 0));
    }
}
