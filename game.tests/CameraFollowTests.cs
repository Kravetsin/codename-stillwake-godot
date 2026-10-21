using Stillwake.Game.Features.Crew;
using Xunit;
using Look = Stillwake.Game.Camera;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>The camera after the hero (movement review, item 12): led by its steps, held through a jump, eased at any frame rate.</summary>
public class CameraFollowTests
{
    /// <summary>The farthest zoom's width, in cells, where the full lead is 6.</summary>
    private const double View = 96;
    private const double Reach = View * Look.LeadShare;
    private const double StepS = 1.0 / 60;

    /// <summary>A hero standing still at (<paramref name="x"/>, <paramref name="y"/>), its middle in cells.</summary>
    private static Followed Standing(double x, double y) => new(x, y, 0, 0, Settled: true, Afloat: false);

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

    /// <summary>A run along the deck at <c>Hero.Run</c>, <paramref name="way"/> 1 or -1, from x 0 at height 10.</summary>
    private static Followed Running(double t, int way = 1) => new(way * Spec.Run * 60 * t / Hero.Unit, 10, way * Spec.Run, 0, true, false);

    [Fact]
    public void TheFirstFrameLooksStraightAtTheHero()
    {
        var camera = new CameraFollow();
        camera.Step(new Followed(12, 7, Spec.Run, 0, true, false), View, 16);
        Assert.Equal((12.0, 7.0, 0.0), (camera.X, camera.Y, camera.Lead.X));
    }

    [Fact]
    public void TheLeadIsFullAtARunAndAboutHalfAtAWalk()
    {
        Assert.Equal((1.0, 0.0), CameraFollow.LeadOf(new Followed(0, 0, Spec.Run, 0, true, false)));
        Assert.Equal((-1.0, 0.0), CameraFollow.LeadOf(new Followed(0, 0, -Spec.Run * 3, 0, false, false)));
        Assert.Equal((double)Spec.Walk / Spec.Run, CameraFollow.LeadOf(new Followed(0, 0, Spec.Walk, 0, true, false)).X, 9);
    }

    [Fact]
    public void UnderGravityTheLeadIsAcrossOnlyAndInSpaceItIsBothWaysNoLongerThanFull()
    {
        Assert.Equal((0.0, 0.0), CameraFollow.LeadOf(new Followed(0, 0, 0, Spec.MaxFall, false, false)));
        var (x, y) = CameraFollow.LeadOf(new Followed(0, 0, Spec.Run, -Spec.Run, false, true));
        Assert.Equal(1, Math.Sqrt(x * x + y * y), 9);
        Assert.True(x > 0 && y < 0);
        Assert.Equal((0.5, 0.0), CameraFollow.LeadOf(new Followed(0, 0, Spec.Run / 2, 0, false, true)));
    }

    [Fact]
    public void RunningTheCameraLooksAheadTheWayTheHeroRuns()
    {
        var camera = At(Running(0));
        Run(camera, 2, 60, t => Running(t));
        var hero = Running(2);
        // Ahead by the lead less what easing trails behind a moving aim: the run's speed for SmoothMs.
        double trail = Spec.Run * 60.0 / Hero.Unit * Look.SmoothMs / 1000;
        Assert.InRange(camera.X - hero.X, Reach - trail - 0.15, Reach - trail + 0.15);
        Assert.Equal(1, camera.Lead.X, 2);
        Assert.Equal(hero.Y, camera.Y, 9);
    }

    [Fact]
    public void TurningRoundTheViewSwingsOverWithoutAJump()
    {
        var camera = At(Running(0));
        Run(camera, 2, 60, t => Running(t));
        double turnedAt = Running(2).X, before = camera.X, most = 0;
        Run(camera, 2, 60, t => new Followed(turnedAt - Spec.Run * 60 * t / Hero.Unit, 10, -Spec.Run, 0, true, false), (c, _) =>
        {
            most = Math.Max(most, Math.Abs(c.X - before));
            before = c.X;
        });
        // Never faster than twice the run itself, a frame; and led the other way by the end.
        Assert.True(most < 2 * Spec.Run / (double)Hero.Unit, $"swung {most} cells in a frame");
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
            path.Add(new Followed(0, ground + up / (double)Hero.Unit, 0, up - was, up == 0, false));
        } while (up < 0);
        path.Add(new Followed(0, ground, 0, 0, true, false));
        return path;
    }

    /// <summary>Aboard, and on the first planet, whose gravity is two thirds of the ship's.</summary>
    [Theory]
    [InlineData(Spec.Gravity, 3.9)]
    [InlineData(Spec.Gravity * 2 / 3, 6)]
    public void AJumpLeavesTheHeightExactlyWhereItWas(int gravity, double rises)
    {
        var camera = At(Standing(0, 10));
        double highest = 10;
        foreach (var hero in Jump(10, gravity))
        {
            camera.Step(hero, View, StepS * 1000);
            highest = Math.Min(highest, hero.Y);
            Assert.Equal(10, camera.Y, 9);
        }
        Assert.True(10 - highest > rises, $"the jump rose only {10 - highest} cells");
    }

    [Fact]
    public void LandingOnAnotherHeightTheCameraGoesThereAtItsEase()
    {
        var camera = At(Standing(0, 10));
        // Landed 3 cells higher: at SmoothMs it has come 1 - 1/e of the way, and in a second all of it.
        Run(camera, Look.SmoothMs / 1000, 1000, _ => Standing(0, 7));
        Assert.Equal(10 - 3 * (1 - Math.Exp(-1)), camera.Y, 3);
        Run(camera, 1, 60, _ => Standing(0, 7));
        Assert.Equal(7, camera.Y, 2);
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
            camera.Step(new Followed(0, y, 0, vy, false, false), View, StepS * 1000);
            worst = Math.Max(worst, y - camera.Y);
        }
        Assert.True(y - 10 > 30, $"fell only {y - 10} cells");
        Assert.True(worst <= Look.CatchDown + 1e-9, $"the hero got {worst} cells below the middle");
        Run(camera, 2, 60, _ => Standing(0, y));
        Assert.Equal(y, camera.Y, 2);
    }

    [Fact]
    public void AStepDownALedgeIsFollowedOnLandingNotOnTheWay()
    {
        var camera = At(Standing(0, 10));
        camera.Step(new Followed(0, 10.5, 0, 500, false, false), View, StepS * 1000);
        camera.Step(new Followed(0, 11, 0, 500, false, false), View, StepS * 1000);
        Assert.Equal(10, camera.Y, 9);
        Run(camera, 1, 60, _ => Standing(0, 11));
        Assert.Equal(11, camera.Y, 2);
    }

    [Fact]
    public void FloatingInSpaceNothingIsHeldAndTheLeadIsUpAndDownToo()
    {
        var camera = At(new Followed(0, 10, 0, 0, false, true));
        // Thrusting up at the jetpack's top speed for two seconds.
        int top = Stillwake.Content.Jetpack.Top;
        Followed Flying(double t) => new(0, 10 - top * 60 * t / Hero.Unit, 0, -top, false, true);
        Run(camera, 2, 60, Flying);
        var hero = Flying(2);
        Assert.True(camera.Y < hero.Y - Reach / 2, $"looked at {camera.Y} with the hero at {hero.Y}");
        Assert.Equal(-1, camera.Lead.Y, 2);
    }

    [Fact]
    public void FromSpaceOntoTheDeckTheViewEasesInNotSnaps()
    {
        int top = Stillwake.Content.Jetpack.Top;
        var camera = At(new Followed(0, 10, 0, 0, false, true));
        Run(camera, 2, 60, t => new Followed(0, 10 - top * 60 * t / Hero.Unit, 0, -top, false, true));
        double y = 10 - top * 120 / (double)Hero.Unit, before = camera.Y, most = 0;
        // Air comes in and it falls back to the deck a cell below.
        Run(camera, 1, 60, t => t < 0.2 ? new Followed(0, y + 5 * t, 0, 83, false, false) : Standing(0, y + 1), (c, _) =>
        {
            most = Math.Max(most, Math.Abs(c.Y - before));
            before = c.Y;
        });
        Assert.True(most < 1, $"jumped {most} cells in a frame");
    }

    [Fact]
    public void TheEasingIsTheSameAtAnyFrameRate()
    {
        double[] fps = [30, 60, 120, 240];
        var ends = fps.Select(rate =>
        {
            var camera = At(Standing(0, 10));
            Run(camera, 0.3, rate, _ => Standing(4, 6));
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
            return camera.X;
        }).ToList();
        // A moving aim is met a frame's sampling apart: within a fifth of a cell.
        foreach (var end in ends) Assert.InRange(end - ends[0], -0.2, 0.2);
    }
}
