using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

public class PlumeWayTests
{
    [Fact]
    public void BlowsAPlumeOppositeTheThrust()
    {
        var (x, y) = PlumeWay.Of(3, 4);
        Assert.Equal(-0.6f, x, 5);
        Assert.Equal(-0.8f, y, 5);
    }

    [Fact]
    public void BlowsItDownWithNoWay() => Assert.Equal((0f, 1f), PlumeWay.Of(0, 0));

    [Theory]
    [InlineData(1e-25, 0)]
    [InlineData(0, -3e-30)]
    [InlineData(1e-40, 1e-40)]
    public void KeepsAWholeWayWhenTheThrustIsTiny(double tx, double ty)
    {
        // A float's squared length underflows to nothing here, and a plume with no way folds into a point Godot cannot fill.
        var (x, y) = PlumeWay.Of(tx, ty);
        Assert.Equal(1f, x * x + y * y, 4);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(4090.3f, 2040.7f)] // space
    [InlineData(16384.6f, 6144.2f)] // a planet's middle
    [InlineData(32760.4f, 12280.9f)] // a planet's far corner
    public void KeepsTheCoresWindingWhereverTheHeroIs(float x, float y)
    {
        var outline = new (float X, float Y)[PlumeWay.Corners];
        for (int turn = 0; turn < 360; turn++)
            for (float length = 0.5f; length <= 11; length += 0.25f)
            {
                double angle = turn * Math.PI / 180;
                PlumeWay.Core(outline, (x, y), PlumeWay.Of(Math.Cos(angle), Math.Sin(angle)), length);
                // Godot's Triangulate::get_area, in floats as there: the wrong sign turns every ear over and it fills nothing.
                float area = 0;
                for (int p = outline.Length - 1, q = 0; q < outline.Length; p = q++)
                    area += outline[p].X * outline[q].Y - outline[p].Y * outline[q].X;
                Assert.True(area < 0, $"{turn} degrees, {length} px long: area {area}");
            }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(32760.5f, 12280.25f)]
    public void DrawsTheCoreFromTheNozzleToItsTip(float x, float y)
    {
        var outline = new (float X, float Y)[PlumeWay.Corners];
        var from = PlumeWay.Core(outline, (x, y), (0, 1), 10);
        (float X, float Y)[] shape =
        [
            (-0.9f, 0), (-1.128f, 2), (-1.126f, 4.5f), (-0.8055f, 7), (0, 10),
            (0.8055f, 7), (1.126f, 4.5f), (1.128f, 2), (0.9f, 0),
        ];
        for (int i = 0; i < shape.Length; i++)
        {
            Assert.Equal(x + shape[i].X, from.X + outline[i].X, 0.01f);
            Assert.Equal(y + shape[i].Y, from.Y + outline[i].Y, 0.01f);
        }
    }
}
