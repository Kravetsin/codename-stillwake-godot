using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The holograms' row closing up round what is shown, and brightening near the screen's foot.</summary>
public class EdgeRowTests
{
    private const float Middle = 640, Wide = 30, Stride = 34;

    [Fact]
    public void AllSixSitAStrideApartCentredAndNeverOverlap()
    {
        var lefts = EdgeRow.Lefts([true, true, true, true, true, true], Middle, Wide, Stride);
        for (int i = 1; i < 6; i++) Assert.Equal(Stride, lefts[i]!.Value - lefts[i - 1]!.Value);
        Assert.Equal(Middle, (lefts[0]!.Value + lefts[5]!.Value + Wide) / 2);
    }

    [Fact]
    public void AHiddenKeyLeavesNoSlotAndTheRowStaysCentred()
    {
        var lefts = EdgeRow.Lefts([true, true, false, false, false, true], Middle, Wide, Stride);
        Assert.Null(lefts[2]);
        Assert.Null(lefts[4]);
        Assert.Equal(Stride, lefts[5]!.Value - lefts[1]!.Value);
        Assert.Equal(Middle, (lefts[0]!.Value + lefts[5]!.Value + Wide) / 2);
    }

    [Fact]
    public void OneKeySitsInTheMiddle() =>
        Assert.Equal(Middle - Wide / 2, EdgeRow.Lefts([false, true, false], Middle, Wide, Stride)[1]);

    [Theory]
    [InlineData(700, true)]
    [InlineData(700 - EdgeRow.NearBand, true)]
    [InlineData(699 - EdgeRow.NearBand, false)]
    [InlineData(100, false)]
    public void ThePointerNearTheFootBrightensThemAnywhereAlongIt(float y, bool bright) =>
        Assert.Equal(bright, EdgeRow.Bright(y, rowTop: 700, overTablet: false, inWindow: true));

    [Fact]
    public void OverATabletThatIsUpTheyAreBrightHoweverHigh() => Assert.True(EdgeRow.Bright(10, rowTop: 700, overTablet: true, inWindow: true));

    /// <summary>A pointer gone under the window's foot, to the taskbar, lights nothing: the keys flickered on, each drawn every frame (planet-frame-rate).</summary>
    [Fact]
    public void APointerOffTheWindowLightsThemNot() => Assert.False(EdgeRow.Bright(900, rowTop: 700, overTablet: true, inWindow: false));

    [Fact]
    public void TheyGrowWithTheInterfacesSize()
    {
        Assert.Equal(1.2f, EdgeRow.Grown(Slots.SizeOf(SlotScale.Normal)), 3);
        Assert.Equal(1.5f, EdgeRow.Grown(Slots.SizeOf(SlotScale.Large)), 3);
    }

    [Fact]
    public void ALargerKeyStandsTallerOnTheSameFootButNeverOffTheScreensTop()
    {
        Assert.Equal(632, EdgeRow.Top(632, 28, 1));
        Assert.Equal(632 + 28 - 28 * 1.4f, EdgeRow.Top(632, 28, 1.4f), 3);
        Assert.Equal(TabletPeek.RowLeast, EdgeRow.Top(TabletPeek.RowLeast, 28, 1.4f));
    }

    [Fact]
    public void TheFullRowOverTheRimStartsWhereAllItsKeysShownStart()
    {
        // At 1280 wide the rim's art is 12 to 1268, the keys' middle 632 (the vitals pick page's measures).
        Assert.Equal((12f, 1256f), TabletPeek.RimArt(1280));
        float grown = EdgeRow.Grown(Slots.SizeOf(SlotScale.Normal));
        float first = EdgeRow.Lefts([true, true, true, true, true, true], 632, TabletPeek.KeySide * grown, TabletPeek.KeyStride * grown)[0]!.Value;
        Assert.Equal(first, EdgeRow.RimLeft(1280, 6, grown), 3);
        Assert.Equal(513.2f, EdgeRow.RimLeft(1280, 6, grown), 3);
        Assert.Equal(483.5f, EdgeRow.RimLeft(1280, 6, EdgeRow.Grown(Slots.SizeOf(SlotScale.Large))), 3);
        // A wider window moves it right with the rim's middle.
        Assert.Equal(EdgeRow.RimLeft(1280, 6, grown) + 320, EdgeRow.RimLeft(1920, 6, grown), 3);
    }
}
