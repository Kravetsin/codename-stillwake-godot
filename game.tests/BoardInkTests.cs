using System.Linq;
using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

public class BoardInkTests
{
    [Fact]
    public void TheSameThingGetsTheSameStrokes()
    {
        var one = BoardInk.Box(0, 0, 148, 180, BoardInk.Seed("plate"));
        var two = BoardInk.Box(0, 0, 148, 180, BoardInk.Seed("plate"));
        Assert.Equal(one.SelectMany(s => s), two.SelectMany(s => s));
        Assert.NotEqual(one.SelectMany(s => s), BoardInk.Box(0, 0, 148, 180, BoardInk.Seed("wire")).SelectMany(s => s));
    }

    [Fact]
    public void AStrokeWandersButKeepsItsEnds()
    {
        var stroke = BoardInk.Stroke(0, 0, 200, 0, 3, sag: 10);
        Assert.Equal((0f, 0f), (stroke[0].X, stroke[0].Y));
        Assert.Equal(200, stroke[^1].X, 3);
        Assert.Equal(0, stroke[^1].Y, 3);
        Assert.Contains(stroke, p => p.Y != 0);
    }

    [Fact]
    public void ABoxRunsPastItsCorners()
    {
        var box = BoardInk.Box(0, 0, 100, 100, 1, over: 4);
        Assert.Equal(4, box.Length);
        Assert.Equal(-4, box[0][0].X, 3);
        Assert.Equal(104, box[0][^1].X, 3);
    }

    [Fact]
    public void AnEarlyShapeIsOnlyItsCorners()
    {
        var corners = BoardInk.Corners(0, 0, 100, 100, BoardInk.SketchOf(0).Side, 1);
        Assert.Equal(8, corners.Length);
        Assert.All(corners.SelectMany(s => s), p => Assert.True(p.X < 20 || p.X > 80 || p.Y < 20 || p.Y > 80));
    }

    [Fact]
    public void AThreadGoesFromEdgeToEdge()
    {
        var thread = BoardInk.Thread("a|b", 0, 0, 200, 0, 36, 42);
        Assert.Equal(36, thread[0].X, 3);
        Assert.Equal(164, thread[^1].X, 3);
    }

    [Fact]
    public void ThreadsTouchingBoxesStayInsideThem()
    {
        var thread = BoardInk.Thread("a|b", 0, 0, 10, 0, 36, 42);
        Assert.Equal(10, thread[0].X, 3);
        Assert.Equal(0, thread[^1].X, 3);
    }

    [Theory]
    [InlineData(0, 0.14f)]
    [InlineData(0.25, 0.26f)]
    [InlineData(0.5, 0.42f)]
    [InlineData(0.75, 0.68f)]
    [InlineData(0.95, 1f)]
    public void APencilSketchesInSteps(double pencil, float side) => Assert.Equal(side, BoardInk.SketchOf(pencil).Side);

    [Fact]
    public void TheBoxAroundNothingIsNone() => Assert.Null(BoardInk.Around([], 1, 1));

    [Fact]
    public void TheBoxAroundHoldsEveryThingWhole() =>
        Assert.Equal((-1d, -2d, 21d, 12d), BoardInk.Around([(0, 0), (20, 10)], 1, 2));

    [Fact]
    public void ALookComesAsCloseAsTheMapAllows()
    {
        var camera = new TechCamera();
        camera.Fit(600, 400, 1200, 800);
        camera.Look(600, 400, 0.75);
        Assert.Equal(0.75, camera.Zoom);
        Assert.Equal((600d, 400d), (camera.ToMap(300, 200).X, camera.ToMap(300, 200).Y));
        camera.Look(600, 400, 0.1);
        Assert.Equal(camera.Least, camera.Zoom);
    }

    [Fact]
    public void AFitNeverGoesAboveLifeSize()
    {
        Assert.Equal(1, TechCamera.Fitting(600, 400, 100, 100));
        Assert.Equal(0.5, TechCamera.Fitting(600, 400, 1200, 100));
    }
}
