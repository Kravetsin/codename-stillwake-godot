using System;
using System.Collections.Generic;
using Stillwake.Game.Features.Logistics;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Where a flying drone is drawn between ticks (<see cref="Trail"/>): an even speed, and a corner turned along a curve.</summary>
public class TrailTests
{
    private const int Speed = 400, Frames = 3;

    /// <summary>
    /// A drone the sim flies <see cref="Speed"/> a tick through <paramref name="centres"/>, centre to centre as it does, read
    /// <see cref="Frames"/> times a tick: where it was drawn each frame, and its speed as drawn, thousandths of a cell.
    /// </summary>
    private static List<(double X, double Y, double Vx, double Vy)> Fly(params (int X, int Y)[] centres)
    {
        var trail = new Trail();
        var drawn = new List<(double, double, double, double)>();
        int next = 1, x = centres[0].X, y = centres[0].Y;
        for (int tick = 0; tick < 40; tick++)
        {
            for (int f = 0; f < Frames; f++)
            {
                trail.See(x, y, (double)f / Frames);
                var (ax, ay) = trail.At;
                var (vx, vy) = trail.PerTick;
                drawn.Add((ax, ay, vx, vy));
            }
            for (int budget = Speed; budget > 0 && next < centres.Length;)
            {
                var (tx, ty) = centres[next];
                int move = Math.Min(budget, Math.Abs(tx - x) + Math.Abs(ty - y));
                (x, y, budget) = (x + Math.Sign(tx - x) * move, y + Math.Sign(ty - y) * move, budget - move);
                if ((x, y) == (tx, ty)) next++;
            }
        }
        return drawn;
    }

    [Fact]
    public void FirstSeenItIsDrawnWhereTheSimHasItStill()
    {
        var trail = new Trail();
        trail.See(1500, 2500, 0.4);
        Assert.Equal(1500, trail.At.X, 6);
        Assert.Equal(2500, trail.At.Y, 6);
        Assert.Equal((0.0, 0.0), trail.PerTick);
    }

    [Fact]
    public void FlyingStraightItKeepsAnEvenSpeedThroughEachCentre()
    {
        var drawn = Fly((500, 500), (10500, 500));
        // Once under way and before it slows to stop, every frame covers a third of a tick's flight.
        for (int i = 18; i < 60; i++)
        {
            Assert.Equal(Speed / (double)Frames, drawn[i].X - drawn[i - 1].X, 6);
            Assert.Equal(Speed, drawn[i].Vx, 6);
        }
    }

    [Fact]
    public void TurningACornerItsSpeedSwingsRoundSmoothlyAndItStaysNearThePath()
    {
        var drawn = Fly((500, 6500), (6500, 6500), (6500, 500));
        double most = 0;
        for (int i = 1; i < drawn.Count; i++)
        {
            // No frame turns its speed by more than a quarter of it: the sim's turn at once spread over ticks.
            most = Math.Max(most, Math.Sqrt(Math.Pow(drawn[i].Vx - drawn[i - 1].Vx, 2) + Math.Pow(drawn[i].Vy - drawn[i - 1].Vy, 2)));
            // It cuts the corner by under a third of a cell.
            Assert.True(Math.Min(Math.Abs(drawn[i].Y - 6500), Math.Abs(drawn[i].X - 6500)) < 333);
        }
        Assert.InRange(most, 1, Speed / 4.0);
    }
}
