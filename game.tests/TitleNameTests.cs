using System;
using System.Linq;
using Stillwake.Game.Menu;
using Xunit;
using Ink = Stillwake.Game.Menu.TitleNameInk;

namespace Stillwake.Game.Tests;

/// <summary>The game's name on the title, «Отражение»: whole most of the loop, to dust and back the same way, no jump at the seam.</summary>
public class TitleNameTests
{
    private const double Frame = 1.0 / 60;

    [Fact]
    public void TheWordIsWholeAtBothEndsOfTheLoopForMostOfItAndApartBetween()
    {
        double whole = 0;
        for (double t = 0; t < Ink.Cycle; t += Frame)
            if (TitleName.Whole(t)) whole += Frame;
        Assert.InRange(whole, 8, Ink.Cycle - 6);
        Assert.True(TitleName.Whole(0) && TitleName.Whole(Ink.Cycle - Frame));
        Assert.False(TitleName.Whole(Ink.Mid));
    }

    [Fact]
    public void EveryGrainIsHomeWhileTheWordIsWholeAndLeavesAtTheFarthest()
    {
        Assert.InRange(TitleName.Grains.Length, 2000, 5000);
        for (double t = 0; t < Ink.Cycle; t += 0.1)
            if (TitleName.Whole(t)) Assert.All(TitleName.Grains, g => Assert.Null(TitleName.At(g, t)));
        Assert.All(TitleName.Grains, g => Assert.NotNull(TitleName.At(g, Ink.Mid)));
    }

    [Fact]
    public void TheDustComesBackTheWayItWentAndNeverJumps()
    {
        foreach (var g in TitleName.Grains.Where((_, i) => i % 7 == 0))
        {
            for (double d = 0; d < Ink.Mid; d += 0.25)
                Assert.Equal(TitleName.At(g, Ink.Mid - d), TitleName.At(g, Ink.Mid + d));
            var last = Home(g, 0);
            for (double t = Frame; t < Ink.Cycle + Frame; t += Frame)
            {
                var now = Home(g, t % Ink.Cycle);
                Assert.True(Math.Abs(now.X - last.X) + Math.Abs(now.Y - last.Y) < 4, $"a grain jumped at {t:F2} s");
                Assert.True(Math.Abs(now.A - last.A) < 0.1, $"a grain blinked at {t:F2} s");
                last = now;
            }
        }
    }

    [Fact]
    public void TheDustStaysInTheFrameAndOffTheSun()
    {
        foreach (var g in TitleName.Grains)
            for (double t = Ink.D0; t < Ink.Ge; t += 0.1)
                if (TitleName.At(g, t) is { } p && p.Alpha > 0.01)
                {
                    Assert.InRange(p.X, 0, 1280);
                    Assert.InRange(p.Y, 0, 720);
                    Assert.True(Math.Abs(p.X - Ink.SunX) + Math.Abs(p.Y - Ink.SunY) > 40);
                }
    }

    /// <summary>Where a grain shows at <paramref name="t"/>: at home, unseen, while it rests in its letter.</summary>
    private static (double X, double Y, double A) Home(in Grain g, double t) => TitleName.At(g, t) is { } p ? (p.X, p.Y, p.Alpha) : (g.X, g.Y, 0);
}
