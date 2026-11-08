using FsCheck;
using FsCheck.Xunit;
using Stillwake.Game.Features.Effects;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A chip's triangle placed by its own corners, as the transform it was drawn under placed it (planet-frame-rate).</summary>
public class ChipShapeTests
{
    private static (float X, float Y)[] Placed(double x, double y, double turn, double scale)
    {
        var into = new (float X, float Y)[3];
        ChipShape.Place(x, y, turn, scale, into);
        return into;
    }

    [Fact]
    public void AnUnturnedChipOfSizeOneIsItsShapeMovedToItsPlace()
    {
        var corners = Placed(10, 20, 0, 1);
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(ChipShape.Corners[i].X + 10, corners[i].X, 4);
            Assert.Equal(ChipShape.Corners[i].Y + 20, corners[i].Y, 4);
        }
    }

    [Fact]
    public void AQuarterTurnTakesItsRightToItsDownAsTheScreensYDoes()
    {
        var corners = Placed(0, 0, Math.PI / 2, 2);
        // (1.2, -0.3) turned clockwise on a screen whose y grows down, then doubled.
        Assert.Equal(0.6f, corners[1].X, 4);
        Assert.Equal(2.4f, corners[1].Y, 4);
    }

    /// <summary>Turned and sized, never bent or mirrored: each corner as far from its place as the shape's, times the scale, the triangle's winding kept.</summary>
    [Property(MaxTest = 300)]
    public void TurningAndSizingKeepTheShape(NormalFloat turn, PositiveInt size)
    {
        double scale = size.Get % 40 / 4.0 + 0.25, t = turn.Get % 100;
        var corners = Placed(5, -7, t, scale);
        for (int i = 0; i < 3; i++)
        {
            var (sx, sy) = ChipShape.Corners[i];
            double was = Math.Sqrt(sx * sx + sy * sy) * scale, now = Math.Sqrt(Math.Pow(corners[i].X - 5, 2) + Math.Pow(corners[i].Y + 7, 2));
            // Within a thousandth, not rounded to three places: two values a hair apart may round either side of a step.
            Assert.Equal(was, now, 1e-3);
        }
        Assert.Equal(Area(ChipShape.Corners) * scale * scale, Area(corners), 1e-3);
    }

    private static double Area((float X, float Y)[] c) =>
        ((c[1].X - c[0].X) * (double)(c[2].Y - c[0].Y) - (c[2].X - c[0].X) * (double)(c[1].Y - c[0].Y)) / 2;
}
