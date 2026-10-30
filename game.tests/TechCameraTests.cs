using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

public class TechCameraTests
{
    private static TechCamera Over(double viewWide = 1000, double viewTall = 500, double mapWide = 2000, double mapTall = 2000)
    {
        var camera = new TechCamera();
        camera.Fit(viewWide, viewTall, mapWide, mapTall);
        return camera;
    }

    [Fact]
    public void ZoomedAllTheWayOutHoldsTheWholeMapInView()
    {
        var camera = Over();
        camera.Wheel(-100, 500, 250);
        Assert.Equal(0.25, camera.Zoom, 6);
        Assert.Equal(0, camera.Y, 6);
        // The tall map fills the view's height and stands in the middle of its width.
        Assert.Equal((1000 - 2000 * 0.25) / 2, camera.X, 6);
    }

    [Fact]
    public void ZoomsAboutThePointer()
    {
        var camera = Over();
        camera.Drag(-300, -400);
        var before = camera.ToMap(200, 100);
        camera.Wheel(1, 200, 100);
        var after = camera.ToMap(200, 100);
        Assert.Equal(before.X, after.X, 6);
        Assert.Equal(before.Y, after.Y, 6);
        Assert.Equal(TechCamera.Notch, camera.Zoom, 6);
    }

    [Fact]
    public void StopsAtItsLargest()
    {
        var camera = Over();
        camera.Wheel(100, 0, 0);
        Assert.Equal(TechCamera.Most, camera.Zoom, 6);
    }

    [Fact]
    public void ANotchThatLandsNearLifeSizeIsLifeSize()
    {
        var camera = Over();
        camera.Wheel(-1, 0, 0);
        camera.Wheel(1, 0, 0);
        Assert.Equal(1, camera.Zoom);
    }

    [Theory]
    [InlineData(5000, 5000)]
    [InlineData(-5000, -5000)]
    public void CannotBeDraggedAway(double dx, double dy)
    {
        var camera = Over();
        camera.Drag(dx, dy);
        Assert.InRange(camera.X, 1000 - 2000, 0);
        Assert.InRange(camera.Y, 500 - 2000, 0);
    }

    [Fact]
    public void AMapSmallerThanTheViewStandsInItsMiddleWhereverDragged()
    {
        var camera = Over(mapWide: 400, mapTall: 100);
        camera.Drag(77, -33);
        Assert.Equal(300, camera.X, 6);
        Assert.Equal(200, camera.Y, 6);
    }

    [Fact]
    public void CentresAPointAtLifeSize()
    {
        var camera = Over();
        camera.Wheel(-3, 0, 0);
        camera.Centre(1000, 1000);
        Assert.Equal(1, camera.Zoom);
        var middle = camera.ToMap(500, 250);
        Assert.Equal(1000, middle.X, 6);
        Assert.Equal(1000, middle.Y, 6);
    }

    [Fact]
    public void CentringKeepsTheMapInBounds()
    {
        var camera = Over();
        camera.Centre(0, 0);
        Assert.Equal((0d, 0d), (camera.X, camera.Y));
    }

    [Fact]
    public void AGrownViewBringsTheSmallestZoomUpWithIt()
    {
        var camera = Over();
        camera.Wheel(-100, 0, 0);
        camera.Fit(2000, 1000, 2000, 2000);
        Assert.Equal(0.5, camera.Zoom, 6);
    }
}
