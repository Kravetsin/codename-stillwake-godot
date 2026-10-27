using System;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A rope hanging from one end (<see cref="Rope.Hang"/>) and the cape laid along one (<see cref="Cape"/>).</summary>
public class CapeTests
{
    private static readonly RopeStyle Heavy = new(Links: 8, Keep: 0.96, Gravity: 0.6, Drift: 0, DriftSpeed: 0, Wave: 0);

    [Fact]
    public void ARopeHungFromOneEndComesToRestStraightBelowItStretchedALittle()
    {
        var rope = new Rope(Heavy, (0, 0), (20, 0));
        for (int i = 0; i < 600; i++) rope.Hang((0, 0), Rope.StepMs, 20);
        var end = rope.Points[^1];
        Assert.InRange(end.X, -0.2, 0.2);
        // A rope only pulls, so its own weight stretches it a little, as a cable sags.
        Assert.InRange(end.Y, 19, 21.6);
    }

    [Fact]
    public void WhatARopeIsKeptFromItNeverEnters()
    {
        // Hung beside a wall at x = 0 and swung into it, every point stays on its side.
        var rope = new Rope(Heavy, (-1, 0), (-21, 0));
        double most = double.MinValue;
        for (int i = 0; i < 300; i++)
        {
            rope.Hang((-1, 0), Rope.StepMs, 20, k => k.X = Math.Min(k.X, 0));
            foreach (var k in rope.Points) most = Math.Max(most, k.X);
        }
        Assert.True(most <= 0, $"a point reached {most}");
    }

    [Fact]
    public void ACapeLiesAgainstTheBackAndOnARunOnlyTrailsALittle()
    {
        // The figure's frame is the world's, moved by its feet: standing, then running at a sprint (211 px a second).
        double feet = 0;
        (double X, double Y) ToWorld((double X, double Y) p) => (p.X + feet, p.Y);
        (double X, double Y) ToFrame((double X, double Y) p) => (p.X - feet, p.Y);
        var cape = new Cape();
        for (int i = 0; i < 300; i++) cape.Step(Rope.StepMs, ToWorld, ToFrame, _ => 9.6);
        // The top piece slants from the neck back over the air pack; the rest hang behind it.
        foreach (var piece in cape.Pieces[1..])
        {
            // Against the back: neither in it nor hanging off it.
            Assert.InRange(piece.X, -9.6 - 0.5, -9.6 + 0.01);
            Assert.InRange(piece.Turn, -0.35, 0.35);
        }
        for (int i = 0; i < 120; i++)
        {
            feet += 211.0 / 60;
            cape.Step(Rope.StepMs, ToWorld, ToFrame, _ => 9.6);
        }
        // Trailing: the hem's piece turns back, away from the way it runs (+x), but a heavy cape not past 45°.
        double hem = cape.Pieces[^1].Turn;
        Assert.InRange(hem, 0.05, Math.PI / 4);
    }
}
