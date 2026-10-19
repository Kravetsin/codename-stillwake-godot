using Stillwake.Game.Features.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A door's panel sliding open or shut over a fifth of a second: only the look.</summary>
public class DoorSlidesTests
{
    private const double Frame = DoorSlides.Seconds / 4;

    [Fact]
    public void SlidesOpenFromShutAndComesToRestAfterAFifthOfASecond()
    {
        var slides = new DoorSlides();
        slides.Moved(7, open: true);
        Assert.Equal(0, slides.OpenOf(7));
        var open = new List<double?>();
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal([7], slides.Advance(Frame));
            open.Add(slides.OpenOf(7));
        }
        Assert.Equal([0.25, 0.5, 0.75], open.Select(o => Math.Round(o!.Value, 6)));
        Assert.Equal([7], slides.Advance(Frame));
        Assert.Null(slides.OpenOf(7));
        Assert.False(slides.Any);
    }

    [Fact]
    public void ShutsFromOpenAndTurnedBackPartWayGoesBackFromWhereItIs()
    {
        var slides = new DoorSlides();
        slides.Moved(3, open: false);
        Assert.Equal(1, slides.OpenOf(3));
        slides.Advance(Frame);
        Assert.Equal(0.75, slides.OpenOf(3)!.Value, 6);
        slides.Moved(3, open: true);
        Assert.Equal(0.75, slides.OpenOf(3)!.Value, 6);
        slides.Advance(Frame / 2);
        Assert.Equal(0.875, slides.OpenOf(3)!.Value, 6);
        // Back where it began a frame ago, it rests.
        Assert.Equal([3], slides.Advance(Frame / 2));
        Assert.Null(slides.OpenOf(3));
    }

    [Fact]
    public void MovesTheCellsOfADoorwayTogetherAndAnswersNothingAtRest()
    {
        var slides = new DoorSlides();
        Assert.Empty(slides.Advance(1));
        foreach (int cell in new[] { 1, 11, 21 }) slides.Moved(cell, open: true);
        Assert.Equal([1, 11, 21], slides.Advance(Frame).Order());
        Assert.Single(new[] { 1, 11, 21 }.Select(slides.OpenOf).Distinct());
    }
}
