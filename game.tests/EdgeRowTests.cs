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
        Assert.Equal(bright, EdgeRow.Bright(y, rowTop: 700, overTablet: false));

    [Fact]
    public void OverATabletThatIsUpTheyAreBrightHoweverHigh() => Assert.True(EdgeRow.Bright(10, rowTop: 700, overTablet: true));
}
