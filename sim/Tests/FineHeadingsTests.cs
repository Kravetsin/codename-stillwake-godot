using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Fine headings (catcher-motion): a slowly turning gun kept in whole numbers, 4096 to the turn, its direction and the way to a point.</summary>
public class FineHeadingsTests
{
    [Fact]
    public void TheAxesAreAQuarterTurnApartClockwiseAsTheScreenShowsIt()
    {
        Assert.Equal(0, Rays.FineHeadingTo(5000, 0));
        Assert.Equal(Rays.FineTurn / 4, Rays.FineHeadingTo(0, 5000));
        Assert.Equal(Rays.FineTurn / 2, Rays.FineHeadingTo(-5000, 0));
        Assert.Equal(3 * Rays.FineTurn / 4, Rays.FineHeadingTo(0, -5000));
        Assert.Equal((1000, 0), Rays.FineDirection(0));
        Assert.Equal((0, 1000), Rays.FineDirection(Rays.FineTurn / 4));
    }

    [Fact]
    public void ApartIsTheShortWayRound()
    {
        Assert.Equal(10, Rays.FineApart(Rays.FineTurn - 5, 5));
        Assert.Equal(-10, Rays.FineApart(5, Rays.FineTurn - 5));
        Assert.Equal(Rays.FineTurn / 2, Rays.FineApart(0, Rays.FineTurn / 2));
    }

    /// <summary>The heading to any point runs within a hundredth of a cell a cell of it: a gun on it strikes it.</summary>
    [Property(MaxTest = 500)]
    public Property TheHeadingToAPointRunsThroughIt() => Prop.ForAll(
        Gen.Choose(-200_000, 200_000).Two().ToArbitrary(),
        p =>
        {
            var (dx, dy) = (p.Item1, p.Item2);
            if (dx == 0 && dy == 0) return true.ToProperty();
            var (x, y) = Rays.FineDirection(Rays.FineHeadingTo(dx, dy));
            double length = Math.Sqrt((double)dx * dx + (double)dy * dy), off = Math.Abs((double)x * dy - (double)y * dx) / Math.Sqrt((double)x * x + (double)y * y);
            return (off <= length / 100 && (long)x * dx + (long)y * dy > 0).Label($"({dx},{dy}) off {off:F0} of {length:F0}");
        });
}
