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

    /// <summary>Aboard, and on the first planet, whose gravity is two thirds of the ship's.</summary>
    [Theory]
    [InlineData(Spec.Gravity, 3.8)]
    [InlineData(Spec.Gravity * 2 / 3, 5.9)]
    public void AJumpIsFollowedUpAndDownNeverFarBehind(int gravity, double rises)
    {
        var camera = At(Standing(0, 10));
        double highest = 10, cameraHighest = 10, worst = 0;
        foreach (var hero in Jump(10, gravity))
        {
            camera.Step(hero, View, StepMs);
            highest = Math.Min(highest, hero.Y);
            cameraHighest = Math.Min(cameraHighest, camera.Y);
            worst = Math.Max(worst, Math.Abs(camera.Y - hero.Y));
        }
        Assert.True(10 - highest > rises, $"the jump rose only {10 - highest} cells");
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
        Assert.Equal(9, camera.Y, 2);
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
}
