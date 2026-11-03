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
        // Laid past the foot, so no bottom edge of the rim ever shows.
        Assert.True(TabletPeek.ScreenTop(Tall) + TabletPeek.RimTall > Tall);
    }

    [Fact]
    public void ATabletOfAnySizeStartsAsTheRimAndEndsWhereItRests()
    {
        var rest = (Left: 329f, Top: 516f, Width: 622f);
        var rim = TabletPeek.Pose(0, rest, Wide, Tall);
        Assert.Equal(TabletPeek.ScreenLeft(Wide), rim.Left);
        Assert.Equal(TabletPeek.ScreenTop(Tall), rim.Top);
        Assert.Equal(TabletPeek.ScreenWidth(Wide), rim.Width);
        Assert.Equal(rest, TabletPeek.Pose(1, rest, Wide, Tall));
    }

    [Fact]
    public void HalfWayUpItIsHalfWayToItsRest()
    {
        var at = TabletPeek.Pose(0.5, (329, 516, 622), Wide, Tall);
        Assert.Equal((TabletPeek.ScreenLeft(Wide) + 329) / 2, at.Left, 0.5f);
        Assert.Equal((714 + 516) / 2f, at.Top, 0.5f);
        Assert.Equal((TabletPeek.ScreenWidth(Wide) + 622) / 2, at.Width, 0.5f);
    }

    [Fact]
    public void TheKeysSitOnTheBezelsTopInTheMiddleOfItsMetal()
    {
        // The art at the rim's place: 12 in, 682 down, 1256 across, what the page the look was picked on drew.
        var (x, y) = TabletPeek.SaddleAt(12, 682, 1256);
        Assert.Equal(526, x);
        Assert.Equal(686, y);
        Assert.True(y + TabletPeek.SaddleTall >= Tall, "the metal runs off the foot, with no edge showing under the keys");
    }

    [Fact]
    public void SixKeysFitTheMetalTheySitOn()
    {
        int last = TabletPeek.KeyIn + 5 * TabletPeek.KeyStride + 30;
        Assert.Equal(TabletPeek.SaddleWide, last + TabletPeek.KeyIn);
    }
    [Fact]
    public void TheCreditsCoverTheChargeOnTheBezelsTop()
    {
        // The art's signal bars and battery: 76 to 47 art pixels in from its right, 9 to 16 down, at two screen pixels each.
        var (x, y) = TabletPeek.DisplayAt(1268, 682);
        Assert.True(x <= 1268 - 2 * 76 && x + TabletPeek.DisplayWide >= 1268 - 2 * 47, "the charge shows beside the credits");
        Assert.True(y <= 682 + 2 * 9 && y + TabletPeek.DisplayTall >= 682 + 2 * 16, "the charge shows over or under them");
        Assert.True(y + TabletPeek.DisplayTall <= 682 + TabletLayout.ScreenTop * TabletLayout.ArtScale, "they run onto the screen");
    }
}
