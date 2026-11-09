using System;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The catcher's fight as its picks draw it (catcher-combat): the page's hash, the aim's tell, the blink's bands, the salvo's columns.</summary>
public class CatcherFxTests
{
    [Fact]
    public void TheHashIsThePagesOwn()
    {
        // As the page's JavaScript gives them.
        Assert.Equal(0.3050170976202935, CatcherFx.Hash(0), 12);
        Assert.Equal(0.5077388833742589, CatcherFx.Hash(1), 12);
        Assert.Equal(0.611978464294225, CatcherFx.Hash(12345), 12);
        Assert.Equal(0.04437401262111962, CatcherFx.Hash(-3), 12);
    }

    [Fact]
    public void TheTellStillsTheLineAndBrightensItWithAHalo()
    {
        var tracking = CatcherFx.AimOf(null, 1, 0);
        Assert.Equal(1, tracking.Sway);
        Assert.Equal(0, tracking.Halo);
        var start = CatcherFx.AimOf(0, 1, 0);
        var end = CatcherFx.AimOf(1, 1, 0);
        Assert.True(end.Alpha > start.Alpha && start.Alpha > tracking.Alpha);
        Assert.True(end.Width > start.Width && end.DotRadius > start.DotRadius && end.MuzzleRadius > start.MuzzleRadius);
        Assert.Equal(0, end.Sway);
        Assert.Equal((0.0, 0.0), CatcherFx.Wobble(1234, end.Sway));
        Assert.True(end.Halo > 0);
    }

    [Fact]
    public void AfterABoltTheLineIsGoneWhileTheBoltShowsThenComesBack()
    {
        Assert.Equal(0, CatcherFx.BackOf(0));
        Assert.Equal(0, CatcherFx.BackOf(CatcherFx.BoltMs + CatcherFx.AfterMs));
        Assert.Equal(1, CatcherFx.BackOf(CatcherFx.BoltMs + CatcherFx.AfterMs + CatcherFx.BackMs));
        Assert.Equal(0, CatcherFx.AimOf(null, CatcherFx.BackOf(100), 0).Alpha);
    }

    [Fact]
    public void GatheringJittersMoreAsItGoesAndHalfTheBandsHoldStill()
    {
        double Most(double gather)
        {
            double most = 0;
            Span<float> shift = stackalloc float[CatcherFx.Bands];
            for (int frame = 0; frame < 200; frame++)
            {
                CatcherFx.Body(frame, gather, null, shift);
                foreach (float s in shift) most = Math.Max(most, Math.Abs(s));
            }
            return most;
        }
        Assert.True(Most(1) > 2 * Most(0));
        Assert.True(Most(1) <= 0.4 + 2.8 + 1e-6);
        Span<float> one = stackalloc float[CatcherFx.Bands];
        int still = 0;
        for (int frame = 0; frame < 200; frame++)
        {
            CatcherFx.Body(frame, 1, null, one);
            foreach (float s in one) still += s == 0 ? 1 : 0;
        }
        Assert.InRange(still / (200.0 * CatcherFx.Bands), 0.4, 0.7);
    }

    [Fact]
    public void SpentItsBandsStillTwitchThenSettle()
    {
        Span<float> shift = stackalloc float[CatcherFx.Bands];
        bool moved = false;
        for (int frame = 0; frame < 50; frame++)
        {
            CatcherFx.Body(frame, 0, 10, shift);
            foreach (float s in shift) moved |= s != 0;
        }
        Assert.True(moved);
        CatcherFx.Body(3, 0, CatcherFx.TwitchMs, shift);
        Assert.All(shift.ToArray(), s => Assert.Equal(0, s));
    }

    [Fact]
    public void TheRedCopyFadesInAsItGathersAndOutAfterItArrives()
    {
        Span<float> shift = stackalloc float[CatcherFx.Bands], alpha = stackalloc float[CatcherFx.Bands];
        CatcherFx.RedCopy(5, 0, null, shift, alpha);
        float faint = alpha.ToArray().Max();
        CatcherFx.RedCopy(5, 1, null, shift, alpha);
        Assert.True(alpha.ToArray().Max() > faint);
        CatcherFx.RedCopy(5, 0, CatcherFx.ArriveMs, shift, alpha);
        Assert.All(alpha.ToArray(), a => Assert.Equal(0, a));
    }

    [Fact]
    public void TheGhostFlickersWithAboutAQuarterOfItsBandsGone()
    {
        Span<float> shift = stackalloc float[CatcherFx.GhostBands], alpha = stackalloc float[CatcherFx.GhostBands];
        int gone = 0;
        for (int frame = 0; frame < 300; frame++)
        {
            CatcherFx.Ghosting(frame, 0.5, shift, alpha);
            foreach (float a in alpha) gone += a == 0 ? 1 : 0;
        }
        Assert.InRange(gone / (300.0 * CatcherFx.GhostBands), 0.15, 0.35);
    }

    [Fact]
    public void AColumnNarrowsToAThreadAndBrightensAsItsShellNears()
    {
        var (w0, a0, t0, g0) = CatcherFx.PillarOf(0);
        var (w1, a1, t1, g1) = CatcherFx.PillarOf(1);
        Assert.Equal(6, w0, 9);
        Assert.Equal(1.4, w1, 9);
        Assert.True(a1 > a0 && t1 > t0 && g1 > g0);
        Assert.Equal(0, t0);
        double last = double.MaxValue;
        for (double k = 0; k <= 1; k += 0.05)
        {
            Assert.True(CatcherFx.PillarOf(k).Width <= last);
            last = CatcherFx.PillarOf(k).Width;
        }
    }

    [Fact]
    public void ACrookedLineRunsEndToEndAndStraysMostInTheMiddle()
    {
        Span<(double X, double Y)> points = stackalloc (double, double)[72];
        int n = CatcherFx.Crooked(0, 0, 80, 0, 7, 1.2, points);
        Assert.Equal(11, n);
        Assert.Equal((0, 0), (points[0].X, Math.Round(points[0].Y, 9)));
        Assert.Equal(80, points[n - 1].X, 9);
        Assert.Equal(0, points[n - 1].Y, 9);
        for (int i = 0; i < n; i++) Assert.True(Math.Abs(points[i].Y) <= 1.2 + 1e-9);
    }
}
