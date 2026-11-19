using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The soot flying to a window's outline and home, its spans and its crackle (drone-frame, the pick «Угли»).</summary>
public class SootRunTests
{
    private const double Frame = 1 / 60.0;
    private const float Zoom = 4;

    /// <summary>The bag at the screen's top left, the drone by the hero's head in its middle, screen px.</summary>
    private static readonly (float X, float Y) BagAt = (20, 100), Drone = (960, 520);
    private static readonly SootSpots Bag = Shape(520, 250), Menu = Shape(1120, 470);
    private static readonly (float X, float Y) MenuAt = (400, 560);

    private static SootSpots Shape(float w, float h) => SootFrame.Spots([(0, 0), (w, 0), (w, h), (0, h)], 2);

    private static float Apart(float ax, float ay, float bx, float by) => MathF.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));

    private static SootRun New(int seed = 1) => new(window: true, new Random(seed));

    private static void Run(SootRun run, double seconds, bool open, SootSpots spots, (float X, float Y) at, Action? each = null)
    {
        for (int f = 0, frames = (int)Math.Ceiling(seconds / Frame - 1e-6); f < frames; f++)
        {
            run.Step(Frame, open, spots, at, Drone, Zoom);
            each?.Invoke();
        }
    }

    [Fact]
    public void OpeningLandsEveryMoteAndLinksEverySpanWithin0_3s()
    {
        var run = New();
        Run(run, 0.2, true, Bag, BagAt);
        Assert.Contains(Enumerable.Range(0, run.Count), i => !run.Landed(i));
        Run(run, 0.1, true, Bag, BagAt);
        for (int i = 0; i < run.Count; i++)
        {
            Assert.True(run.Landed(i), $"mote {i} still flying");
            var spot = Bag.Spot(i);
            Assert.True(Apart(run.X(i), run.Y(i), BagAt.X + spot.X, BagAt.Y + spot.Y) <= 0.5f + 1e-3f, $"mote {i} off its spot");
            Assert.True(run.Strength(i) > 0, $"span {i} not linked");
        }
        Run(run, SootRun.Tune.LinkIn, true, Bag, BagAt);
        Assert.All(Enumerable.Range(0, run.Count), j => Assert.Equal(1, run.Strength(j)));
    }

    [Fact]
    public void ASpanLightsOnlyAfterBothEndsLand()
    {
        var run = New();
        bool halfLanded = false;
        Run(run, 0.5, true, Bag, BagAt, () =>
        {
            for (int j = 0; j < run.Count; j++)
            {
                bool both = run.Landed(j) && run.Landed((j + 1) % run.Count);
                if (!both) Assert.Equal(0, run.Strength(j));
                halfLanded |= run.Landed(j) != run.Landed((j + 1) % run.Count);
            }
        });
        Assert.True(halfLanded, "no span ever had one end landed alone");
    }

    [Fact]
    public void ClosingIsHomeWithin0_2s()
    {
        var run = New();
        Run(run, 1, true, Bag, BagAt);
        Run(run, 0.15, false, Bag, BagAt);
        Assert.True(run.Busy);
        Run(run, 0.05, false, Bag, BagAt);
        Assert.False(run.Busy);
        Assert.Equal(0, run.Out);
        // Home is the drone's orbits: a few world px round its core.
        for (int i = 0; i < run.Count; i++) Assert.True(Apart(run.X(i), run.Y(i), Drone.X, Drone.Y) < 4 * Zoom, $"mote {i} not home");
        Assert.All(Enumerable.Range(0, run.Count), j => Assert.Equal(0, run.Strength(j)));
    }

    /// <summary>The most any mote moves in one frame of <paramref name="script"/>, counting only motes there both frames.</summary>
    private static float Fastest(Action<SootRun, int> script, int frames)
    {
        var run = New();
        float most = 0;
        float[] x = [], y = [];
        for (int f = 0; f < frames; f++)
        {
            script(run, f);
            for (int i = 0; i < Math.Min(x.Length, run.Count); i++) most = Math.Max(most, Apart(x[i], y[i], run.X(i), run.Y(i)));
            x = Enumerable.Range(0, run.Count).Select(run.X).ToArray();
            y = Enumerable.Range(0, run.Count).Select(run.Y).ToArray();
        }
        return most;
    }

    [Fact]
    public void ASwitchMidRunNeverJumps()
    {
        // The runs uninterrupted, the bag's and the build menu's: open, then close.
        float Plain(SootSpots spots, (float X, float Y) at) =>
            Fastest((run, f) => run.Step(Frame, f < 40, spots, at, Drone, Zoom), 70);
        float bound = 1.5f * Math.Max(Plain(Bag, BagAt), Plain(Menu, MenuAt));
        const int Switch = 6;
        var cases = new (string Name, Action<SootRun, int> Script)[]
        {
            ("the outline changes mid-open", (run, f) => run.Step(Frame, true, f < Switch ? Bag : Menu, f < Switch ? BagAt : MenuAt, Drone, Zoom)),
            ("a close mid-open", (run, f) => run.Step(Frame, f < Switch, Bag, BagAt, Drone, Zoom)),
            ("both on one frame", (run, f) => run.Step(Frame, f < Switch, f < Switch ? Bag : Menu, f < Switch ? BagAt : MenuAt, Drone, Zoom)),
            ("a close mid-open, opened again mid-close", (run, f) => run.Step(Frame, f < Switch || f > Switch + 4, Bag, BagAt, Drone, Zoom)),
        };
        foreach (var (name, script) in cases)
        {
            float fastest = Fastest(script, 70);
            Assert.True(fastest <= bound, $"{name}: a mote moved {fastest:F1} px in a frame, over {bound:F1}");
        }
    }

    [Fact]
    public void ASwitchMidRunStillEndsSettled()
    {
        var run = New();
        Run(run, 0.1, true, Bag, BagAt);
        Run(run, 1, true, Menu, MenuAt);
        Assert.Equal(Menu.Count, run.Count);
        Assert.All(Enumerable.Range(0, run.Count), i => Assert.True(run.Landed(i)));
        Run(run, 0.1, false, Menu, MenuAt);
        Run(run, 1, true, Menu, MenuAt);
        Assert.All(Enumerable.Range(0, run.Count), j => Assert.Equal(1, run.Strength(j)));
    }

    [Fact]
    public void TheWholeFrameGlowsNeverUnderItsBase()
    {
        foreach (var spots in new[] { Bag, Menu })
            for (double t = 0; t < 30; t += 0.07)
                for (int i = 0; i < spots.Count; i++)
                    Assert.InRange(SootRun.Glow(spots, i, t), SootRun.Tune.GlowBase - 1e-4f, SootRun.Tune.GlowBase + SootRun.Tune.GlowPeak * SootRun.Tune.Runners.Length);
    }

    [Fact]
    public void NeighboursGlowAlikeSoTheFrameReadsAsOneLine()
    {
        // Sticks of their own brightness were seen in game: the glow changes smoothly round the frame, never stepping at a mote.
        foreach (var spots in new[] { Bag, Menu, Shape(1800, 1000) })
            for (double t = 0; t < 30; t += 0.07)
                for (int i = 0; i < spots.Count; i++)
                {
                    int next = (i + 1) % spots.Count;
                    float gap = MathF.Abs(spots.Round(next) - spots.Round(i)), apart = MathF.Min(gap, spots.Perimeter - gap);
                    float step = MathF.Abs(SootRun.Glow(spots, i, t) - SootRun.Glow(spots, next, t));
                    Assert.True(step <= 0.006f * apart, $"{i} at {t}: {step} over {apart} px");
                }
    }

    [Fact]
    public void TheEmbersLightRunsRoundTheFrame()
    {
        int Brightest(double t) => Enumerable.Range(0, Bag.Count).MaxBy(i => SootRun.Glow(Bag, i, t));
        var seen = Enumerable.Range(0, 40).Select(k => Brightest(k * 0.5)).Distinct().Count();
        Assert.True(seen > 10, $"the light stood on {seen} spots only");
    }

    [Fact]
    public void NothingShowsOnceTheSootIsHome()
    {
        var run = New();
        Run(run, 1, true, Bag, BagAt);
        Assert.True(run.Shows);
        Run(run, 0.15, false, Bag, BagAt);
        Assert.True(run.Shows);
        Run(run, 0.1, false, Bag, BagAt);
        Assert.False(run.Shows);
    }

    [Fact]
    public void CracklesCome4To8sApartLast0_13sNeverMidRun()
    {
        var run = New(7);
        var starts = new List<double>();
        double t = 0, openedAt = 0;
        (int, int)? last = null;
        for (int f = 0; f < 120 * 60; f++)
        {
            // Closed for a second at 60 s, opened again: a crackle never plays while the soot is out or flying.
            bool open = t < 60 || t >= 61;
            if (open && t >= 61 && openedAt < 61) openedAt = t;
            run.Step(Frame, open, Bag, BagAt, Drone, Zoom);
            t += Frame;
            if (run.Crackle is { } c)
            {
                Assert.True(open && run.Out >= 1, $"a crackle mid-run at {t:F2} s");
                Assert.InRange(c.Age, 0, SootRun.Tune.CrackleLife);
                Assert.InRange(c.Span, 0, run.Count - 1);
                if (last != (c.Span, c.Seed)) starts.Add(t);
                last = (c.Span, c.Seed);
            }
        }
        Assert.True(starts.Count >= 12, $"only {starts.Count} crackles in 120 s");
        // The first after each opening comes 2.4 s on, from when the soot settled.
        Assert.InRange(starts[0], 2.4 + SootRun.Tune.OpenS, 2.4 + SootRun.Tune.OpenS + 0.05);
        var after = starts.First(s => s > 61);
        Assert.InRange(after - openedAt, 2.4 + SootRun.Tune.OpenS - 0.02, 2.4 + SootRun.Tune.OpenS + 0.05);
        foreach (var (a, b) in starts.Zip(starts.Skip(1)).Where(p => p.First > 61 || p.Second < 60))
            Assert.InRange(b - a, 4 - 1e-6, 8 + Frame);
    }

    [Fact]
    public void TheDronesCrackleWaitsWhileAWindowIsOpen()
    {
        var window = New(3);
        var drone = new SootRun(window: false, new Random(4));
        int droneCrackles = 0, windowCrackles = 0;
        for (int f = 0; f < 90 * 60; f++)
        {
            double t = f * Frame;
            bool open = t is < 30 or >= 60;
            window.Step(Frame, open, Bag, BagAt, Drone, Zoom);
            // The drone's flag is the one LightDrone hears: a window open, or its soot still out.
            bool out_ = open || window.Busy;
            drone.Step(Frame, out_);
            if (drone.Crackle is { } d)
            {
                Assert.False(out_, $"the drone crackled with a window open at {t:F2} s");
                Assert.InRange(d.Span, 0, SootRun.Tune.DroneSpans - 1);
                droneCrackles++;
            }
            if (window.Crackle != null)
            {
                Assert.Null(drone.Crackle);
                windowCrackles++;
            }
        }
        Assert.True(droneCrackles > 0 && windowCrackles > 0);
    }

    [Fact]
    public void WithNoDroneDrawnTheSootFadesInOnTheOutline()
    {
        var run = New();
        run.Step(Frame, true, Bag, BagAt, null, Zoom);
        for (int i = 0; i < run.Count; i++)
        {
            var spot = Bag.Spot(i);
            Assert.True(Apart(run.X(i), run.Y(i), BagAt.X + spot.X, BagAt.Y + spot.Y) <= 0.5f + 1e-3f);
        }
        Assert.InRange(run.Alpha, 0, 0.2f);
        Run(run, 0.3, true, Bag, BagAt);
        Assert.Equal(1, run.Alpha);
    }

    [Fact]
    public void TheFadeOpensGlassThenContentsAndTakesClicksAtOnce()
    {
        var fade = new SootFade();
        fade.Step(Frame, true);
        Assert.True(fade.Clicks && fade.Glass == 0 && fade.Contents == 0);
        for (int f = 0; f < 18; f++) fade.Step(Frame, true);
        Assert.Equal((1f, 1f), (fade.Glass, fade.Contents));
        fade.Step(Frame, false);
        Assert.True(fade.Clicks && fade.Shown);
        for (int f = 0; f < 4; f++) fade.Step(Frame, false);
        Assert.False(fade.Clicks);
        for (int f = 0; f < 10; f++) fade.Step(Frame, false);
        Assert.False(fade.Shown);
        Assert.Equal(0, fade.Glass);
    }
}
