using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The markers at the screen's edge: their list, where they land, which way they turn, when they show.</summary>
public class MarkersTests
{
    private const double W = 1920, H = 1080, Reach = 17, Margin = 29;
    private static readonly Box Inner = new(Margin, Margin, W - Margin, H - Margin);
    /// <summary>The hotbar and the suit's gauge at the top left, roughly where 0058 lays them.</summary>
    private static readonly Box[] Hud = [new(12, 12, 416, 60), new(12, 68, 236, 98)];

    private static Placed At(double x, double y, params Box[] keepOut) => Markers.Place(x, y, W, H, Inner, Reach, keepOut);

    private static bool OnEdge(Placed p) =>
        p.X >= Inner.Left - 1e-9 && p.X <= Inner.Right + 1e-9 && p.Y >= Inner.Top - 1e-9 && p.Y <= Inner.Bottom + 1e-9
        && (Near(p.X, Inner.Left) || Near(p.X, Inner.Right) || Near(p.Y, Inner.Top) || Near(p.Y, Inner.Bottom));

    private static bool Near(double a, double b) => Math.Abs(a - b) < 1e-6;

    [Fact]
    public void ListsHomeAtTheSpawnFirstThenEachFriendAtItsMiddleInItsSuit()
    {
        var friend = new Hero(12_500, 7_000, 0, 0, false, 1);
        var list = Markers.Of(3 * 20 + 4, 20, [(2, friend, Suits.All[1])]);
        Assert.Equal(new Marker(MarkerIcon.Home, 4.5, 3.5, Markers.HomeColour), list[0]);
        Assert.Equal(new Marker(MarkerIcon.Friend, 12.5, (7_000 - Hero.MiddleUp) / 1000.0, Suits.All[1], 2), list[1]);
        Assert.DoesNotContain(Markers.HomeColour, Suits.All);
    }

    [Fact]
    public void PutsAWorldPointOnTheScreenAroundTheCameraAtItsZoom()
    {
        Assert.Equal((W / 2, H / 2), Markers.ToScreen(10, 5, 10, 5, 32, W, H));
        Assert.Equal((W / 2 + 64, H / 2 - 96), Markers.ToScreen(12, 2, 10, 5, 32, W, H));
    }

    [Fact]
    public void HidesWhileTheTargetIsOnTheScreenItsEdgesIncluded()
    {
        Assert.True(At(W / 2, H / 2).OnScreen);
        Assert.True(At(0, 0).OnScreen);
        Assert.True(At(W, H).OnScreen);
        Assert.False(At(W + 1, H / 2).OnScreen);
        Assert.False(At(W / 2, -1).OnScreen);
    }

    [Fact]
    public void ClampsToTheEdgeWhereTheLineFromTheMiddleCrossesIt()
    {
        var right = At(W * 10, H / 2);
        Assert.Equal((Inner.Right, H / 2), (right.X, right.Y));
        var below = At(W / 2, H * 3);
        Assert.Equal((W / 2, Inner.Bottom), (below.X, below.Y));
        // Up and to the right at 45°: the top edge comes first on a wide screen.
        var diagonal = At(W / 2 + 2000, H / 2 - 2000);
        Assert.Equal(Inner.Top, diagonal.Y, 6);
        Assert.Equal(W / 2 + (H / 2 - Margin), diagonal.X, 6);
    }

    [Fact]
    public void TurnsTowardTheTarget()
    {
        Assert.Equal(0, At(W * 10, H / 2).Angle, 6);
        Assert.Equal(Math.PI / 2, At(W / 2, H * 3).Angle, 6);
        Assert.Equal(Math.PI, Math.Abs(At(-W, H / 2).Angle), 6);
        Assert.Equal(-Math.PI / 4, At(W / 2 + 2000, H / 2 - 2000).Angle, 6);
    }

    [Fact]
    public void SlidesAlongItsEdgeOutOfTheHudAndStillTurnsTowardTheTarget()
    {
        // Far up-left: the line meets the top edge over the hotbar, so the marker slides right of it.
        var upLeft = At(W / 2 - 3000, -2000, Hud);
        Assert.Equal((Hud[0].Right + Reach, Inner.Top), (upLeft.X, upLeft.Y));
        Assert.Equal(Math.Atan2(-2000 - upLeft.Y, W / 2 - 3000 - upLeft.X), upLeft.Angle, 9);
        // Left and up: the left edge over the gauge, just under the hotbar, so it slides below both.
        var left = At(W / 2 - 10_000, H / 2 - 4940, Hud);
        Assert.Equal(Inner.Left, left.X);
        Assert.Equal(Hud[1].Bottom + Reach, left.Y);
    }

    [Fact]
    public void FadesInOffTheScreenAndIsGoneAtOnceOnIt()
    {
        Assert.Equal(0.4, Markers.Fade(0, false, Markers.FadeInS * 0.4), 9);
        Assert.Equal(1, Markers.Fade(0.9, false, 1));
        Assert.Equal(0, Markers.Fade(1, true, 0.001));
    }

    [Fact]
    public void CountsTheCellsBetweenRounded()
    {
        Assert.Equal(5, Markers.CellsBetween(0, 0, 3, 4));
        Assert.Equal(3, Markers.CellsBetween(1, 1, 3.5, 1));
    }

    [Property(MaxTest = 500)]
    public Property PlacesEveryTargetOffTheScreenOnTheEdgeAndClearOfTheHud() => Prop.ForAll(
        Gen.Choose(-20_000, 20_000).Two().ToArbitrary(),
        target =>
        {
            var (x, y) = target;
            var placed = At(x, y, Hud);
            return placed.OnScreen == Markers.OnScreen(x, y, W, H)
                && (placed.OnScreen || (OnEdge(placed) && !Hud.Any(b => b.Grown(Reach).Holds(placed.X, placed.Y))));
        });
}
