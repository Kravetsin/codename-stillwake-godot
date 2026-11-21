using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The soot flying to a window's outline and home, its net and its crackle (drone-frame, the picks «Угли» and «Сеть по краю»).</summary>
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

    /// <summary>How far a landed node may drift from its home: its two swings together.</summary>
    private static readonly float Drifts = SootRun.Tune.Drift * 1.29f + 1e-3f;

    private static bool AtHome(SootRun run, SootSpots spots, (float X, float Y) at, int i)
    {
        var home = spots.Home(i);
        return Apart(run.X(i), run.Y(i), at.X + home.X, at.Y + home.Y) <= Drifts;
    }

    [Fact]
    public void OpeningLandsEveryMoteAndLinksTheNetWithin0_3s()
    {
        var run = New();
        Run(run, 0.2, true, Bag, BagAt);
        Assert.Contains(Enumerable.Range(0, run.Count), i => !run.Landed(i));
        Run(run, 0.1, true, Bag, BagAt);
        for (int i = 0; i < run.Count; i++)
        {
            Assert.True(run.Landed(i), $"mote {i} still flying");
            Assert.True(AtHome(run, Bag, BagAt, i), $"mote {i} off its node's home");
        }
        Assert.Equal(Bag.Links, run.Links);
        Assert.All(Enumerable.Range(0, run.Links), j => Assert.True(run.Strength(j) > 0, $"link {j} not linked"));
        Run(run, SootRun.Tune.LinkIn, true, Bag, BagAt);
        Assert.All(Enumerable.Range(0, run.Links), j => Assert.Equal(1, run.Strength(j)));
    }

    [Fact]
    public void ALinkLightsOnlyAfterBothEndsLand()
    {
        var run = New();
        bool halfLanded = false;
        Run(run, 0.5, true, Bag, BagAt, () =>
        {
            for (int j = 0; j < run.Links; j++)
            {
                var (a, b) = Bag.Link(j);
                if (!(run.Landed(a) && run.Landed(b))) Assert.Equal(0, run.LinkLook(j).Alpha);
                halfLanded |= run.Landed(a) != run.Landed(b);
            }
        });
        Assert.True(halfLanded, "no link ever had one end landed alone");
    }

    [Fact]
    public void AMoteKindlesIntoItsNodeOnceLandedAndGoesOutOnAClose()
    {
        var run = New();
        Run(run, 0.1, true, Bag, BagAt, () =>
        {
            for (int i = 0; i < run.Count; i++)
                if (!run.Landed(i)) Assert.Equal(0, run.Kindled(i));
        });
        Run(run, 0.5, true, Bag, BagAt);
        Assert.All(Enumerable.Range(0, run.Count), i => Assert.Equal(1, run.Kindled(i)));
        Run(run, SootRun.Tune.Douse + Frame, false, Bag, BagAt);
        Assert.All(Enumerable.Range(0, run.Count), i => Assert.Equal(0, run.Kindled(i)));
    }

    [Fact]
    public void AShorterLinkIsHotterAndALongOneGone()
    {
        for (double t = 0; t < 10; t += 0.37)
        {
            var (near, mid, far) = (SootRun.Look(10, 0.3f, t), SootRun.Look(30, 0.3f, t), SootRun.Look(SootRun.Tune.LinkReach, 0.3f, t));
            Assert.True(near.Heat > mid.Heat, "a short link no hotter");
            Assert.True(near.Alpha > 0.3f && mid.Alpha > 0);
            Assert.Equal(0, far.Alpha);
        }
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
        Assert.All(Enumerable.Range(0, run.Links), j => Assert.Equal(0, run.Strength(j)));
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
        Assert.All(Enumerable.Range(0, run.Links), j => Assert.Equal(1, run.Strength(j)));
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
                Assert.InRange(c.Span, 0, run.Links - 1);
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
        for (int i = 0; i < run.Count; i++) Assert.True(AtHome(run, Bag, BagAt, i));
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
