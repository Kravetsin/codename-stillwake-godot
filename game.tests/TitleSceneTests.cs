using System;
using Stillwake.Game.Menu;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The scene behind the title: where its hero sits, where the camera looks, what drifts past and how.</summary>
public class TitleSceneTests
{
    /// <summary>A map from rows, '#' solid: the wreck's shape small, a roof over a torn deck that reaches out past it.</summary>
    private static Func<int, int, bool> Map(params string[] rows) =>
        (x, y) => y < 0 || y >= rows.Length || x < 0 || x >= rows[y].Length || rows[y][x] == '#';

    private static readonly string[] Wreck =
    [
        "          ",
        "    ######",
        "    #     ",
        "          ",
        "  ########",
        "  ########",
        "          ",
    ];

    [Fact]
    public void SitsOnTheLowestEdgeOpenAboveAndOutWithDeckBehindNotTheRoof()
    {
        Assert.Equal((2, 4), TitleScene.Ledge(Map(Wreck), 10, Wreck.Length));
        Assert.Equal((2 + TitleScene.SitIn, 4.0), TitleScene.Seat((2, 4)));
    }

    [Fact]
    public void FindsNoLedgeOnAMapWithNone()
    {
        string[] none = ["     ", "     ", "#####", "#####"];
        Assert.Null(TitleScene.Ledge(Map(none), 5, none.Length));
    }

    [Fact]
    public void TheCameraKeepsTheHeroWhereItSitsOnScreenWanderingLittle()
    {
        var seat = (100.0, 50.0);
        var screen = (40.0, 22.5);
        for (double s = 0; s < 120; s += 3.7)
        {
            var (x, y) = TitleScene.Camera(seat, screen, s);
            double across = 0.5 - (x - seat.Item1) / screen.Item1, down = 0.5 - (y - seat.Item2) / screen.Item2;
            Assert.InRange(across, TitleScene.HeroAcross - TitleScene.SwayX / screen.Item1 - 1e-9, TitleScene.HeroAcross + TitleScene.SwayX / screen.Item1 + 1e-9);
            Assert.InRange(down, TitleScene.HeroDown - TitleScene.SwayY / screen.Item2 - 1e-9, TitleScene.HeroDown + TitleScene.SwayY / screen.Item2 + 1e-9);
        }
    }

    [Fact]
    public void EverythingDriftsLeftToRightFromOffTheViewToOffItAndIsGoneOnceAcross()
    {
        var rng = new Random(7);
        var view = (10.0, 20.0, 40.0, 22.5);
        for (int i = 0; i < 200; i++)
        {
            foreach (var drift in new[] { TitleScene.NextDrifter(rng, view), TitleScene.NextPiece(rng, view) })
            {
                Assert.True(drift.FromX < view.Item1 && drift.ToX > view.Item1 + view.Item3);
                Assert.InRange(drift.Depth, 0.3, 1.2);
                Assert.True(drift.Seconds > 0);
                Assert.Equal((drift.FromX, drift.FromY, 0.0), drift.At(0));
                Assert.Null(drift.At(drift.Seconds + 0.01));
                Assert.Null(drift.At(-0.01));
            }
        }
    }

    [Fact]
    public void AstronautsDriftAboveOrBelowTheWreckWhereSpaceIsOpenNeverThroughIt()
    {
        var rng = new Random(3);
        var view = (0.0, 0.0, 40.0, 100.0);
        for (int i = 0; i < 200; i++)
        {
            double y = TitleScene.NextDrifter(rng, view).FromY;
            Assert.True(y <= 22 || y >= 66, $"{y} crosses the wreck's band");
        }
    }

    [Fact]
    public void GapsFallBetweenTheirBounds()
    {
        var rng = new Random(1);
        for (int i = 0; i < 100; i++)
            Assert.InRange(TitleScene.Gap(rng, TitleScene.DrifterGapLeast, TitleScene.DrifterGapMost), TitleScene.DrifterGapLeast, TitleScene.DrifterGapMost);
    }
}
