using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The part of the board the camera keeps in bounds (docs/plans/board.md).</summary>
public class BoardBoxTests
{
    [Fact]
    public void TheFirstNoteStandsInTheMiddleOfTheView()
    {
        var box = BoardBox.Round(800, 600).Holding(0, 0);
        Assert.Equal((400d, 300d), (-box.Left, -box.Top));
        Assert.Equal((800d, 600d), (box.Wide, box.Tall));
    }

    [Fact]
    public void WhatIsShownBeyondTheViewGrowsItByAMargin()
    {
        var box = BoardBox.Round(800, 600).Holding(-500, 700);
        Assert.Equal(-500 - BoardBox.Margin, box.Left);
        Assert.Equal(700 + BoardBox.Margin, box.Bottom);
        Assert.Equal((-300d, 400d), (box.Top, box.Right));
    }

    [Fact]
    public void ItNeverShrinks()
    {
        var box = BoardBox.Round(800, 600).Holding(-500, 700);
        Assert.Equal(box, box.Holding(0, 0));
        Assert.Equal(box, box.Holding(BoardBox.Round(400, 300)));
    }
}
