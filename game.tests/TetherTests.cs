using Stillwake.Game.Features.Crew;
using Xunit;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>A tether as drawn: apps/game/src/features/crew/tether.test.ts, one for one.</summary>
public class TetherTests
{
    private static readonly Point Anchor = new(0, 0);

    private static void Run(Tether line, (double X, double Y) clip, double seconds)
    {
        for (int i = 0; i < seconds * 60; i++) line.Update((Anchor.X, Anchor.Y), clip, Hero.StepMs);
    }

    private static double Hypot(double x, double y) => Math.Sqrt(x * x + y * y);

    /// <summary>How far a point of the line is off the straight way between its ends.</summary>
    private static double OffLine(Tether line, (double X, double Y) clip)
    {
        double length = Hypot(clip.X, clip.Y);
        return line.Points.Max(p => Math.Abs(p.X * clip.Y - p.Y * clip.X) / length);
    }

    private static double Span(Tether line) =>
        line.Points.Zip(line.Points.Skip(1), (q, p) => Hypot(p.X - q.X, p.Y - q.Y)).Sum();

    [Fact]
    public void RunsFromWhereItIsTiedToTheClipEndsPinnedInLinksOfItsLength()
    {
        var clip = (10000.0, 4000.0);
        var line = new Tether(Anchor, clip);
        Run(line, clip, 3);
        Assert.Equal(Tether.Links + 1, line.Points.Count);
        Assert.Equal((0.0, 0.0), (line.Points[0].X, line.Points[0].Y));
        Assert.Equal(clip, (line.Points[Tether.Links].X, line.Points[Tether.Links].Y));
        Assert.True(Span(line) <= line.Length * 1.02);
    }

    [Fact]
    public void IsLetOutALittleLongerThanTheWayItSpansAndReeledInAsTheHeroComesBack()
    {
        var line = new Tether(Anchor, (16000, 0));
        Run(line, (16000, 0), 5);
        Assert.True(line.Length > 16000);
        Assert.True(line.Length < 16000 * 1.3);
        Run(line, (4000, 0), 5);
        Assert.True(line.Length < 6000);
    }

    [Fact]
    public void RunsStraightAtTheLimit()
    {
        var clip = ((double)Spec.Tether, 0.0);
        var line = new Tether(Anchor, clip);
        Run(line, clip, 5);
        Assert.Equal(Spec.Tether, line.Length);
        Assert.True(OffLine(line, clip) < 300);
    }

    [Fact]
    public void DriftsWhenLooseNeverRestingAndNeverFarOff()
    {
        var clip = (8000.0, 0.0);
        var line = new Tether(Anchor, clip);
        Run(line, clip, 2);
        var before = line.Points.Select(p => (p.X, p.Y)).ToArray();
        Run(line, clip, 2);
        Assert.Contains(line.Points.Select((p, i) => Hypot(p.X - before[i].X, p.Y - before[i].Y)), d => d > 100);
        Run(line, clip, 30);
        Assert.All(line.Points, p => Assert.True(Hypot(p.X, p.Y) <= line.Length));
    }

    [Fact]
    public void DoesNotFlipAsTheHeroCrossesBelowWhereItIsTied()
    {
        var line = new Tether(Anchor, (1200, 12000));
        Run(line, (1200, 12000), 3);
        var middle = () => line.Points[Tether.Links / 2];
        var before = (middle().X, middle().Y);
        // Across the vertical through the anchor at the swimming speed: the line's middle follows
        // less far than the hero went, where a line drawn by which side the hero is on would flip.
        for (int x = 1200; x >= -1200; x -= Spec.Swim) line.Update((0, 0), (x, 12000), Hero.StepMs);
        Assert.True(Hypot(middle().X - before.X, middle().Y - before.Item2) < 2400);
    }
}
