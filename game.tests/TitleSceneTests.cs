using System;
using System.Linq;
using Stillwake.Game.Menu;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The scene behind the title, «Против солнца»: the hero by the sun, its motion, its tether, the lamp, the stars.</summary>
public class TitleSceneTests
{
    private const double Frame = 1.0 / 60, Minutes = 10 * 60;

    private static double Distance(Vec a, Vec b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    [Fact]
    public void TheHeroReachesIntoTheSunsHaloNeverCoversItsDiscAndKeepsOutOfTheMenusThirdAndTheEdges()
    {
        for (double s = 0; s < Minutes; s += 0.25)
        {
            var at = TitleScene.Motion(s);
            var outline = TitleHero.Outline(TitleHero.At(s)).Select(o => (At: TitleScene.ToScene(at, o.At), R: o.R * TitleScene.HeroScale)).ToList();
            double near = outline.Min(o => Distance(o.At, TitleScene.Sun) - o.R);
            Assert.True(near <= TitleScene.HaloR - 8, $"{near:F0} px from the sun at {s} s: outside its halo");
            Assert.True(near >= TitleScene.CoreR + 6, $"{near:F0} px from the sun at {s} s: over its disc");
            foreach (var (p, r) in outline)
            {
                Assert.True(p.X - r >= TitleScene.Third + 20, $"in the menu's third at {s} s");
                Assert.True(p.X - r >= 16 && p.Y - r >= 16 && p.X + r <= TitleScene.Wide - 16 && p.Y + r <= TitleScene.High - 16, $"at the frame's edge at {s} s");
            }
        }
    }

    [Fact]
    public void TheHeroDriftsAndTurnsSlowlyAndTurnsWholeAgainstTheClock()
    {
        var before = TitleScene.Motion(0);
        double turned = 0;
        for (double s = Frame; s < Minutes; s += Frame)
        {
            var now = TitleScene.Motion(s);
            Assert.True(Distance(now.At, before.At) / Frame <= 5, $"the hero moves {Distance(now.At, before.At) / Frame:F1} px/s at {s} s");
            Assert.True(Math.Abs(now.Turn - before.Turn) / Frame <= 0.2, $"the hero turns {Math.Abs(now.Turn - before.Turn) / Frame:F2} rad/s at {s} s");
            turned += now.Turn - before.Turn;
            before = now;
        }
        Assert.InRange(-turned / (2 * Math.PI), Minutes / TitleScene.TurnSeconds - 1, Minutes / TitleScene.TurnSeconds + 1);
    }

    [Fact]
    public void TheTetherRunsFromTheBeltToItsPinOffTheFrameSlackAndCalmNeverAcrossTheMenu()
    {
        var tether = new TitleTether();
        double rest = tether.Link * (TitleTether.Count - 1);
        var before = Points(tether);
        for (double s = Frame; s < Minutes; s += Frame)
        {
            tether.RunTo(s);
            var now = Points(tether);
            Assert.Equal(TitleTether.Anchor, now[^1]);
            Assert.True(now[^1].Y > TitleScene.High, "the pin is in the frame");
            Assert.True(Distance(now[0], TitleTether.Attach(tether.Seconds)) < 1e-9, $"the tether left the belt at {s} s");
            double length = 0;
            for (int i = 1; i < now.Length; i++) length += Distance(now[i - 1], now[i]);
            double straight = Distance(now[0], now[^1]);
            Assert.True(length >= straight && length <= rest * 1.01, $"the tether is {length:F1} long at {s} s, its rest {rest:F1}");
            Assert.True(length > straight * 1.005, $"the tether is taut at {s} s");
            Assert.All(now, p => Assert.False(p.X < TitleScene.Third && p.Y > 0 && p.Y < TitleScene.High, $"the tether crosses the menu at {s} s"));
            double fastest = now.Zip(before, Distance).Max() / Frame;
            Assert.True(fastest <= 30, $"the tether moves {fastest:F1} px/s at {s} s");
            before = now;
        }
    }

    [Fact]
    public void TheTetherSmoothedKeepsItsEnds()
    {
        var tether = new TitleTether();
        var smooth = new Vec[TitleTether.Smoothed];
        tether.Smooth(smooth);
        Assert.Equal(tether[0], smooth[0]);
        Assert.Equal(TitleTether.Anchor, smooth[^1]);
    }

    [Fact]
    public void TheLampIsLitAThirdOfASecondInEveryBlink()
    {
        int lit = 0, frames = (int)(TitleScene.LampSeconds * 10 / 0.001);
        for (int i = 0; i < frames; i++) if (TitleScene.LampOn(i * 0.001)) lit++;
        Assert.InRange(lit * 0.001, 10 * TitleScene.LampLit - 0.01, 10 * TitleScene.LampLit + 0.01);
    }

    [Fact]
    public void TheSunsLightFallsOffFromItsMiddleToNothing()
    {
        Assert.Equal(0.95, TitleScene.SunAlpha(0), 6);
        Assert.Equal(0.24, TitleScene.SunAlpha(0.08 * TitleScene.GlowR), 6);
        Assert.Equal(0, TitleScene.SunAlpha(TitleScene.GlowR));
        Assert.Equal(0, TitleScene.SunAlpha(2 * TitleScene.GlowR));
        for (double d = 1; d < TitleScene.GlowR; d++) Assert.True(TitleScene.SunAlpha(d) <= TitleScene.SunAlpha(d - 1));
    }

    [Fact]
    public void TheStarsAreThePagesFaintFarAndBrighterNear()
    {
        var page = TitleStars.Rng(7);
        Assert.Equal([0.8990055415779352, 0.42729625664651394, 0.5560492090880871], new[] { page(), page(), page() });
        var far = TitleStars.Far(1280, 720, 1);
        var near = TitleStars.Near(1280, 720, 1);
        Assert.Equal(far, TitleStars.Far(1280, 720, 1));
        Assert.InRange(far.Max(), 60, (int)Math.Round(0.45 * 255) + 2);
        Assert.InRange(far.Count(a => a > 0), 300, 330);
        Assert.True(near.Count(a => a >= 0.95 * 255 - 1) >= 12 * 4 - 8, "too few bright near stars");
    }

    private static Vec[] Points(TitleTether tether) => Enumerable.Range(0, TitleTether.Count).Select(i => tether[i]).ToArray();
}
