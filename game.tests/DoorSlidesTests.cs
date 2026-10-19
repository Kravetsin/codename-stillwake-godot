using Stillwake.Game.Features.Grid;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A door's panel sliding open or shut over a fifth of a second, a step at a time: only the look.</summary>
public class DoorSlidesTests
{
    private const double Frame = DoorSlides.Seconds / 4;

    [Fact]
    public void SlidesOpenFromShutAStepAtATimeAndComesToRestAfterAFifthOfASecond()
    {
        var slides = new DoorSlides();
        slides.Moved(7, open: true);
        Assert.Equal(0, slides.StepOf(7));
        var steps = new List<int?>();
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal([7], slides.Advance(Frame));
            steps.Add(slides.StepOf(7));
        }
        Assert.Equal([2, 4, 6], steps);
        Assert.Equal([7], slides.Advance(Frame));
        Assert.Null(slides.StepOf(7));
        Assert.False(slides.Any);
    }

    [Fact]
    public void ShutsFromOpenAndTurnedBackPartWayGoesBackFromWhereItIs()
    {
        var slides = new DoorSlides();
        slides.Moved(3, open: false);
        Assert.Equal(DoorShape.Steps, slides.StepOf(3));
        slides.Advance(Frame);
        Assert.Equal(6, slides.StepOf(3));
        slides.Moved(3, open: true);
        Assert.Equal(6, slides.StepOf(3));
        slides.Advance(Frame / 2);
        Assert.Equal(7, slides.StepOf(3));
        // Back where it began a frame ago, it rests.
        Assert.Equal([3], slides.Advance(Frame / 2));
        Assert.Null(slides.StepOf(3));
    }

    [Fact]
    public void AnswersOnlyTheCellsWhoseStepChangedAndNothingAtRest()
    {
        var slides = new DoorSlides();
        Assert.Empty(slides.Advance(1));
        slides.Moved(1, open: true);
        slides.Moved(2, open: true);
        Assert.Empty(slides.Advance(DoorSlides.Seconds / 100));
        Assert.Equal([1, 2], slides.Advance(Frame).Order());
    }
}
