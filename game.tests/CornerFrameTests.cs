using System;
using System.Linq;
using Stillwake.Game.Shared;
using Xunit;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Tests;

/// <summary>The drone's corners round its windows (the pick «Уголки»): the clock, the sparks laying them, the threads behind a dev key.</summary>
public class CornerFrameTests
{
    private const double Frame = 1 / 60.0;
    private static readonly (float X, float Y, float W, float H) Window = (300, 200, 240, 160);
    private static readonly (float X, float Y) Eye = (100, 500);

    private static CornerClock Run(CornerClock clock, bool open, double seconds)
    {
        for (double t = 0; t < seconds - 1e-9; t += Frame) clock.Step(Math.Min(Frame, seconds - t), open);
        return clock;
    }

    private static EmberMarks Drawn(CornerClock clock, (float X, float Y)? eye = null, float seconds = 0.4f)
    {
        var marks = new EmberMarks();
        CornerFrame.Draw(marks, clock, Window, eye ?? Eye, seconds);
        return marks;
    }

    [Fact]
    public void TheWindowIsReadableWithinATenthOfASecondAndWholeSoonAfter()
    {
        var clock = Run(new CornerClock(), true, 0.1);
        Assert.InRange(clock.Screen, 0.7f, 1);
        Run(clock, true, 0.035);
        Assert.Equal(1, clock.Screen);
        Assert.Equal(1, clock.Flight(3));
    }

    [Fact]
    public void ShutTheScreenGoesFirstThenTheCornersFlyHomeAndNothingShows()
    {
        var clock = Run(new CornerClock(), true, 0.5);
        Run(clock, false, I.ScreenOut + 0.001);
        Assert.Equal(0, clock.Screen);
        Assert.True(clock.Shown);
        Assert.InRange(clock.Flight(0), 0.01f, 0.9f);
        Run(clock, false, I.Home);
        Assert.False(clock.Shown);
        Assert.Empty(Drawn(clock).Squares);
    }

    [Fact]
    public void OpenedAgainMidwayTheCornersGoOnFromWhereTheyAre()
    {
        var clock = Run(new CornerClock(), true, 0.5);
        Run(clock, false, I.Home / 2);
        float was = clock.Flight(0);
        clock.Step(0, true);
        Assert.Equal(was, clock.Flight(0), 3);
        Run(clock, true, I.Reach);
        Assert.Equal(1, clock.Flight(0));
    }

    [Fact]
    public void EachCornerIsLaidByASparkShotFromTheEye()
    {
        var clock = Run(new CornerClock(), true, I.Reach / 2);
        var flying = Drawn(clock);
        // On its way: a spark between the eye and its corner, no bracket yet.
        Assert.Empty(flying.Lines);
        var corner = CornerFrame.Corner(Window, 0);
        Assert.Contains(flying.Squares, s => s.Dark && OnLine(Eye, corner, (s.X, s.Y)) && s.X > Eye.X && s.X < corner.X);
        Run(clock, true, 0.4);
        var landed = Drawn(clock);
        // Landed: each corner's bracket, a wide line and a fine one, bent at the corner and nothing across the window.
        Assert.Equal(8, landed.Lines.Count);
        for (int k = 0; k < 4; k++)
        {
            var (x, y) = CornerFrame.Corner(Window, k);
            Assert.Equal(2, landed.Lines.Count(l => l.Count == 3 && landed.Points[l.From + 2] == x && landed.Points[l.From + 3] == y));
            Assert.Contains(landed.Squares, s => !s.Dark && s.X == x && s.Y == y);
        }
        Assert.All(landed.Lines, l => Assert.True(Length(landed, l) <= 2 * I.Arm + 0.01f));
    }

    [Fact]
    public void TheCornersStandJustOutsideTheWindowTheirArmsAlongItsEdges()
    {
        var (x, y, w, h) = Window;
        Assert.Equal((x - I.CornerOut, y - I.CornerOut), CornerFrame.Corner(Window, 0));
        Assert.Equal((x + w + I.CornerOut, y + h + I.CornerOut), CornerFrame.Corner(Window, 2));
        var (across, along) = CornerFrame.Arms(CornerFrame.Corner(Window, 2), 2);
        Assert.Equal((x + w + I.CornerOut - I.Arm, y + h + I.CornerOut), across);
        Assert.Equal((x + w + I.CornerOut, y + h + I.CornerOut - I.Arm), along);
    }

    [Fact]
    public void TheThreadsFromTheEyeShowOnlyWithTheDevKey()
    {
        var clock = Run(new CornerClock(), true, 0.5);
        try
        {
            CornerFrame.Threads = false;
            Assert.DoesNotContain(Drawn(clock).Lines, l => StartsAtEye(Drawn(clock), l));
            var halfway = Run(new CornerClock(), true, I.Reach / 2);
            Assert.Empty(Drawn(halfway).Lines);
            CornerFrame.Threads = true;
            var marks = Drawn(clock);
            Assert.Equal(4, marks.Lines.Count(l => StartsAtEye(marks, l)));
            Assert.Equal(4, Drawn(halfway).Lines.Count(l => StartsAtEye(Drawn(halfway), l)));
        }
        finally
        {
            CornerFrame.Threads = false;
        }
    }

    [Fact]
    public void WithNoDroneShownTheCornersStandAtOnceAndGoWithTheScreen()
    {
        var clock = Run(new CornerClock(), true, I.Reach / 2);
        var marks = new EmberMarks();
        CornerFrame.Draw(marks, clock, Window, null, 0);
        Assert.Equal(8, marks.Lines.Count);
        Run(clock, false, 0.01);
        marks.Clear();
        CornerFrame.Draw(marks, clock, Window, null, 0);
        Assert.Empty(marks.Lines);
    }

    private static bool StartsAtEye(EmberMarks marks, EmberMarks.Line line) => marks.Points[line.From] == Eye.X && marks.Points[line.From + 1] == Eye.Y;

    private static float Length(EmberMarks marks, EmberMarks.Line line)
    {
        float sum = 0;
        for (int i = 1; i < line.Count; i++)
        {
            float dx = marks.Points[line.From + 2 * i] - marks.Points[line.From + 2 * i - 2], dy = marks.Points[line.From + 2 * i + 1] - marks.Points[line.From + 2 * i - 1];
            sum += MathF.Sqrt(dx * dx + dy * dy);
        }
        return sum;
    }

    private static bool OnLine((float X, float Y) a, (float X, float Y) b, (float X, float Y) p)
    {
        float cross = (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
        return MathF.Abs(cross) / MathF.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y)) < 0.01f;
    }
}
