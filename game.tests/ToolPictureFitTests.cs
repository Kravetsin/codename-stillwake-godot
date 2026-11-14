using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

public class ToolPictureFitTests
{
    [Fact]
    public void ALongToolFillsTheWindowsWidthAndKeepsItsShape()
    {
        // A spear-long tool, 60 × 10: as wide as the window inside its margin lets it, its height after it.
        var (w, h) = ToolPictureFit.Of(60, 10);
        Assert.Equal(ToolPictureFit.Width - 2 * ToolPictureFit.Margin, w, 6);
        Assert.Equal(10 * w / 60, h, 6);
    }

    [Fact]
    public void ATallToolFillsTheWindowsHeight()
    {
        // The pickaxe, 34 × 19: its height is what binds.
        var (w, h) = ToolPictureFit.Of(34, 19);
        Assert.Equal(ToolPictureFit.Height - 2 * ToolPictureFit.Margin, h, 6);
        Assert.True(w <= ToolPictureFit.Width - 2 * ToolPictureFit.Margin);
    }

    [Fact]
    public void ASmallToolGrowsNoMoreThanTheMost()
    {
        // The screwdriver, 10 × 3, would fit seventeen times over: it stops at the most.
        Assert.Equal((10 * ToolPictureFit.Most, 3 * ToolPictureFit.Most), ToolPictureFit.Of(10, 3));
    }
}
