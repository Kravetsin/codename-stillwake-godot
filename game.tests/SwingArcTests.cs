using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A swing's light kept on its blade when the stroke crosses the angle seam behind the shoulder (aimed up).</summary>
public class SwingArcTests
{
    [Fact]
    public void AStrokeAcrossTheSeamKeepsRising()
    {
        // A blade over the top of a stroke aimed up: Atan2 jumps from just under +π to just over −π.
        double[] raw = [2.6, 2.85, 3.05, -3.05, -2.85, -2.6];
        double last = raw[0];
        foreach (double angle in raw[1..])
        {
            double next = SwingArc.Near(last, angle);
            Assert.InRange(next - last, 0.15, 0.35);
            last = next;
        }
        Assert.Equal(-2.6 + Math.Tau, last, 9);
    }

    [Fact]
    public void AnAngleNearTheLastIsLeftAsItIs()
    {
        Assert.Equal(0.4, SwingArc.Near(0.1, 0.4), 9);
        Assert.Equal(-0.4, SwingArc.Near(-0.1, -0.4), 9);
    }
}
