using Stillwake.Game.Features.Crew;
using Xunit;
using Look = Stillwake.Game.Camera;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>The camera after the hero (movement review, item 12): led by its steps, its height eased after it, alike at any frame rate.</summary>
public class CameraFollowTests
{
    /// <summary>The farthest zoom's width, in cells, where the full lead is 6.</summary>
    private const double View = 96;
    private const double Reach = View * Look.LeadShare;
    private const double StepMs = 1000.0 / 60;

    /// <summary>A hero standing still at (<paramref name="x"/>, <paramref name="y"/>), its middle in cells.</summary>
    private static Followed Standing(double x, double y) => new(x, y, 0, 0, Afloat: false);

    /// <summary>
    /// Runs <paramref name="seconds"/> at <paramref name="fps"/>, the hero at each frame where <paramref name="at"/> puts
    /// it after that many seconds; <paramref name="check"/>, if given, sees the camera and the hero each frame.
    /// </summary>
    private static void Run(CameraFollow camera, double seconds, double fps, Func<double, Followed> at,
        Action<CameraFollow, Followed>? check = null)
    {
        int frames = (int)Math.Round(seconds * fps);
        for (int i = 1; i <= frames; i++)
        {
            var hero = at(i / fps);
            camera.Step(hero, View, 1000 / fps);
            check?.Invoke(camera, hero);
        }
    }

    private static CameraFollow At(Followed hero)
    {
        var camera = new CameraFollow();
        camera.Snap(hero);
        return camera;
    }

    /// <summary>A run along the deck at <c>Hero.Run</c>, <paramref name="way"/> 1 or -1, from x <paramref name="from"/> at height 10.</summary>
    private static Followed Running(double t, int way = 1, double from = 0) =>
        new(from + way * Spec.Run * 60 * t / Hero.Unit, 10, way * Spec.Run, 0, false);

    [Fact]
    public void TheFirstFrameLooksStraightAtTheHero()
    {
        var camera = new CameraFollow();
        camera.Step(new Followed(12, 7, Spec.Run, 0, false), View, 16);
        Assert.Equal((12.0, 7.0, 0.0), (camera.X, camera.Y, camera.Lead.X));
    }

    [Fact]
    public void TheLeadIsFullAtARunAndAboutHalfAtAWalk()
    {
        Assert.Equal((1.0, 0.0), CameraFollow.LeadOf(new Followed(0, 0, Spec.Run, 0, false)));
        Assert.Equal((-1.0, 0.0), CameraFollow.LeadOf(new Followed(0, 0, -Spec.Run * 3, 0, false)));
        Assert.Equal((double)Spec.Walk / Spec.Run, CameraFollow.LeadOf(new Followed(0, 0, Spec.Walk, 0, false)).X, 9);
    }

    [Fact]
    public void UnderGravityTheLeadIsAcrossOnlyAndInSpaceItIsBothWaysNoLongerThanFull()
    {
        Assert.Equal((0.0, 0.0), CameraFollow.LeadOf(new Followed(0, 0, 0, Spec.MaxFall, false)));
        var (x, y) = CameraFollow.LeadOf(new Followed(0, 0, Spec.Run, -Spec.Run, true));
        Assert.Equal(1, Math.Sqrt(x * x + y * y), 9);
        Assert.True(x > 0 && y < 0);
        Assert.Equal((0.5, 0.0), CameraFollow.LeadOf(new Followed(0, 0, Spec.Run / 2, 0, true)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void RunningTheCameraLooksTheWholeLeadAheadNothingTrailing(int way)
    {
        var camera = At(Running(0, way));
        Run(camera, 5, 60, t => Running(t, way));
        Assert.Equal(way * Reach, camera.X - Running(5, way).X, 2);
        Assert.Equal(2, Reach, 9);
    }

    [Fact]
    public void TheLeadIsTheSameShareOfTheScreenAtEveryZoom()
    {
        var near = At(Running(0));
        for (int i = 1; i <= 300; i++) near.Step(Running(i / 60.0), View / 2, StepMs);
        Assert.Equal(Reach / 2, near.X - Running(5).X, 2);
    }

    [Fact]
    public void StartingToRunTheLeadGrowsWithoutAJump()
    {
        var camera = At(Standing(0, 10));
        double before = 0, most = 0;
        Run(camera, 1, 60, t => Running(t), (c, _) =>
        {
            most = Math.Max(most, Math.Abs(c.X - before));
            before = c.X;
        });
        // Never faster than twice the run itself, a frame.
        Assert.True(most < 2 * Spec.Run / (double)Hero.Unit, $"moved {most} cells in a frame");
    }

    [Fact]
    public void TurningRoundTheViewDriftsOverGently()
    {
        var camera = At(Running(0));
        Run(camera, 5, 60, t => Running(t));
        double turnedAt = Running(5).X, before = camera.X, most = 0;
        Run(camera, 0.5, 60, t => Running(t, -1, turnedAt));
        // Half a second on, the lead is only just past the middle, far from the full lead the other way.
        Assert.True(camera.Lead.X > -0.3, $"turned to {camera.Lead.X} in half a second");
        before = camera.X;
        Run(camera, 4.5, 60, t => Running(t + 0.5, -1, turnedAt), (c, _) =>
        {
            most = Math.Max(most, Math.Abs(c.X - before));
            before = c.X;
        });
        // The view never moves much faster than the hero runs.
        Assert.True(most < 1.3 * Spec.Run / (double)Hero.Unit, $"swung {most} cells in a frame");
        Assert.Equal(-1, camera.Lead.X, 1);
    }

    [Fact]
    public void AHeroTurningOnTheSpotToAimMovesNothing()
    {
        // Facing is not followed: a still hero, however it faces, leaves the view where it is.
        var camera = At(Standing(5, 10));
        Run(camera, 1, 60, _ => Standing(5, 10));
        Assert.Equal((5.0, 10.0), (camera.X, camera.Y));
    }

    /// <summary>A jump straight up from height <paramref name="ground"/>, as the controller jumps: up at <c>Hero.Jump</c>, gravity a step.</summary>
    private static List<Followed> Jump(double ground, int gravity = Spec.Gravity)
    {
        var path = new List<Followed>();
        int up = 0, vy = -Spec.Jump;
        do
        {
            vy += gravity;
            int was = up;
            up = Math.Min(0, up + vy);
            path.Add(new Followed(0, ground + up / (double)Hero.Unit, 0, up - was, false));
        } while (up < 0);
        for (int i = 0; i < 120; i++) path.Add(Standing(0, ground));
        return path;
    }

    /// <summary>Aboard, and on the first planet, at its own gravity.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AJumpIsFollowedUpAndDownNeverFarBehind(bool planet)
    {
        int gravity = planet ? Stillwake.Content.PlanetTypes.Small.Fall(Spec.Gravity) : Spec.Gravity;
        // The jump's top: each step that still rises, Hero.Jump less gravity a step, summed.
        int rising = (Spec.Jump - 1) / gravity;
        double rises = (rising * Spec.Jump - gravity * rising * (rising + 1) / 2) / (double)Hero.Unit;
        // Higher than the catch, or the view would not need to follow at all.
        Assert.True(rises > Look.Catch, $"a jump of {rises} cells stays within the catch");
        var camera = At(Standing(0, 10));
        double highest = 10, cameraHighest = 10, worst = 0;
        foreach (var hero in Jump(10, gravity))
        {
            camera.Step(hero, View, StepMs);
            highest = Math.Min(highest, hero.Y);
            cameraHighest = Math.Min(cameraHighest, camera.Y);
            worst = Math.Max(worst, Math.Abs(camera.Y - hero.Y));
        }
        Assert.Equal(rises, 10 - highest, 9);
        // The view rose with it most of the way, never further than the catch from it, and is back down with it.
        Assert.True(10 - cameraHighest > (10 - highest) * 0.6, $"the view rose {10 - cameraHighest} of {10 - highest} cells");
        Assert.True(worst <= Look.Catch + 1e-9, $"the hero got {worst} cells from the middle");
        Assert.Equal(10, camera.Y, 2);
    }

    [Fact]
    public void LandingOnAnotherHeightTheCameraGoesThereAtItsEase()
    {
        var camera = At(Standing(0, 10));
        // Up a cell: at SmoothMs it has come 1 - 1/e of the way, and in a second all of it.
        Run(camera, Look.SmoothMs / 1000, 1000, _ => Standing(0, 9));
        Assert.Equal(10 - (1 - Math.Exp(-1)), camera.Y, 3);
        Run(camera, 1, 60, _ => Standing(0, 9));
        Assert.Equal(9, camera.Y);
    }

    [Fact]
    public void FallingDownAShaftTheHeroNeverGetsFarBelowTheMiddle()
    {
        var camera = At(Standing(0, 10));
        double y = 10, worst = 0;
        int vy = 0;
        for (int step = 0; step < 120; step++)
        {
            vy = Math.Min(vy + Spec.Gravity, Spec.MaxFall);
            y += vy / (double)Hero.Unit;
            camera.Step(new Followed(0, y, 0, vy, false), View, StepMs);
            worst = Math.Max(worst, y - camera.Y);
        }
        Assert.True(y - 10 > 30, $"fell only {y - 10} cells");
        Assert.True(worst <= Look.Catch + 1e-9, $"the hero got {worst} cells below the middle");
        Run(camera, 2, 60, _ => Standing(0, y));
        Assert.Equal(y, camera.Y, 2);
    }

    [Fact]
    public void FloatingInSpaceTheLeadIsUpAndDownTooNothingTrailing()
    {
        int top = Stillwake.Content.Jetpack.Top;
        Followed Flying(double t) => new(0, 10 - top * 60 * t / Hero.Unit, 0, -top, true);
        var camera = At(Flying(0));
        Run(camera, 5, 60, Flying);
        Assert.Equal(-Reach, camera.Y - Flying(5).Y, 2);
        Assert.Equal(-1, camera.Lead.Y, 1);
    }

    [Fact]
    public void FromTheDeckIntoSpaceAndBackTheViewEasesNeverSnaps()
    {
        int top = Stillwake.Content.Jetpack.Top;
        var camera = At(Standing(0, 10));
        double before = camera.Y, most = 0;
        void Watch(CameraFollow c, Followed _)
        {
            most = Math.Max(most, Math.Abs(c.Y - before));
            before = c.Y;
        }
        // Out into space, thrusting up for two seconds; then air comes in and it falls back to the deck a cell below.
        Run(camera, 2, 60, t => new Followed(0, 10 - top * 60 * t / Hero.Unit, 0, -top, true), Watch);
        double y = 10 - top * 120 / (double)Hero.Unit;
        Run(camera, 2, 60, t => t < 0.2 ? new Followed(0, y + 5 * t, 0, 83, false) : Standing(0, y + 1), Watch);
        Assert.True(most < 0.6, $"jumped {most} cells in a frame");
        Assert.Equal(y + 1, camera.Y, 2);
    }

    [Fact]
    public void TheEasingIsTheSameAtAnyFrameRate()
    {
        double[] fps = [30, 60, 120, 240];
        var ends = fps.Select(rate =>
        {
            var camera = At(Standing(0, 10));
            Run(camera, 0.3, rate, _ => Standing(4, 9));
            return (camera.X, camera.Y);
        }).ToList();
        foreach (var end in ends)
        {
            Assert.Equal(ends[0].X, end.X, 9);
            Assert.Equal(ends[0].Y, end.Y, 9);
        }
    }

    [Fact]
    public void FollowingARunIsAlikeAtAnyFrameRate()
    {
        double[] fps = [30, 60, 144];
        var ends = fps.Select(rate =>
        {
            var camera = At(Running(0));
            Run(camera, 1.5, rate, t => Running(t));
            return camera.X - Running(1.5).X;
        }).ToList();
        foreach (var end in ends) Assert.InRange(end - ends[0], -0.05, 0.05);
    }

    /// <summary>Screen pixels a cell at the farthest zoom of a screen 1920 wide.</summary>
    private const double PerCell = 16 * 1.25;

    /// <summary>
    /// A run of three seconds, then the hero standing where it stopped, at <paramref name="fps"/>: when, after the stop, the
    /// camera moved a whole screen pixel, in milliseconds, and the camera and the hero at the end.
    /// </summary>
    private static (List<double> Moves, CameraFollow Camera, Followed Hero) Stopping(double fps)
    {
        var camera = At(Running(0));
        Run(camera, 3, fps, t => Running(t));
        var stopped = Standing(Running(3).X, 10);
        var moves = new List<double>();
        double shown = CameraFollow.OnPixels(camera.X, camera.Y, stopped.X, stopped.Y, PerCell).Camera.X;
        int frames = (int)Math.Round(4 * fps);
        for (int i = 1; i <= frames; i++)
        {
            camera.Step(stopped, View, 1000 / fps);
            double now = CameraFollow.OnPixels(camera.X, camera.Y, stopped.X, stopped.Y, PerCell).Camera.X;
            if (now != shown) moves.Add(i * 1000 / fps);
            shown = now;
        }
        return (moves, camera, stopped);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(144)]
    public void AfterARunTheViewSettlesAtASteadyPaceNeverCrawlingInAndEndsOnTheHero(double fps)
    {
        var (moves, camera, hero) = Stopping(fps);
        // At SettleSpeed the lead's two cells go by a pixel every 50 ms here; an eased tail's last pixels came most of a second apart.
        double slowest = 1000 / (Look.SettleSpeed * Reach * PerCell);
        Assert.Equal(50, slowest, 9);
        var gaps = moves.Zip(moves.Skip(1), (a, b) => b - a).ToList();
        Assert.True(gaps.Count > 30, $"only {moves.Count} pixels moved");
        Assert.All(gaps, gap => Assert.True(gap <= slowest + 1000 / fps + 1e-9, $"a pixel {gap} ms after the last: {string.Join(" ", gaps)}"));
        Assert.True(moves[^1] < 2000, $"still settling {moves[^1]} ms after the stop");
        Assert.Equal((hero.X, hero.Y), (camera.X, camera.Y));
        Assert.Equal((0.0, 0.0), camera.Lead);
    }

    [Fact]
    public void TheViewSettlesAlikeAt60And144Fps()
    {
        var (at60, _, _) = Stopping(60);
        var (at144, _, _) = Stopping(144);
        Assert.InRange(at144[^1] - at60[^1], -2000 / 60.0, 2000 / 60.0);
        Assert.InRange(at144.Count - at60.Count, -2, 2);
    }

    [Fact]
    public void AnApproachGoesItsShareButAtLeastItsLeastAndNeverPast()
    {
        Assert.Equal(5, CameraFollow.Approach(0, 10, 0.5, 1));
        Assert.Equal(1, CameraFollow.Approach(0, 1.5, 0.1, 1));
        Assert.Equal(-1.5, CameraFollow.Approach(0, -1.5, 0.1, 2));
        Assert.Equal(3, CameraFollow.Approach(3, 3, 0.5, 1));
    }

    [Fact]
    public void OnPixelsTheWorldMovesByWholePixelsAndASteadyLeadHoldsTheHeroStill()
    {
        double? heroShown = null;
        for (int i = 0; i < 600; i++)
        {
            // A hero running on at no whole number of pixels a frame, the camera a steady 1.937 cells ahead.
            double feet = 3 + i * 0.01234, middle = 10 - 1.312;
            var (look, at) = CameraFollow.OnPixels(feet + 1.937, middle + 0.2, feet, 10, PerCell);
            // Every point of the world a whole pixel apart from frame to frame: the camera an eighth off whole pixels.
            Assert.Equal(-CameraFollow.Phase, look.X * PerCell - Math.Round(look.X * PerCell), 9);
            Assert.Equal(-CameraFollow.Phase, look.Y * PerCell - Math.Round(look.Y * PerCell), 9);
            Assert.Equal(0, at.X * PerCell - Math.Round(at.X * PerCell), 9);
            // The hero within half a pixel of where it stands, and as far from the screen's middle every frame.
            Assert.InRange((at.X - feet) * PerCell, -0.5, 0.5);
            double shown = (at.X - look.X) * PerCell;
            Assert.Equal(heroShown ?? shown, shown, 9);
            heroShown = shown;
        }
    }
}
