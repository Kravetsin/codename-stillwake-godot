using Stillwake.Game.Features.Debris;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// A burst's push on the host player's bodies (debris-bursts): by its power, falling off to none at its reach, along the line from its
/// middle to the box's nearest point and put on there, so a light piece flies far, a heavy one barely moves and an off-centre one turns.
/// </summary>
public class BurstPushTests
{
    private static readonly Spec.Blast Charge = Spec.Blasting.Charge;

    /// <summary>A long flat box, 4 cells by 1, at the origin.</summary>
    private static readonly PieceShape Plank = PieceShape.Sized(2000, 500);

    private static RopeImpulse? Push(double cx, double cy, double angle = 0, PieceShape? shape = null, int reach = 8) =>
        BurstPush.Of(cx, cy, Charge.Power, reach, 0, 0, angle, shape ?? Plank);

    private static double Strength(RopeImpulse i) => Math.Sqrt(i.X * i.X + i.Y * i.Y);

    /// <summary>The impulse's turn about the body's middle: its point across it.</summary>
    private static double Torque(RopeImpulse i) => i.AtX * i.Y - i.AtY * i.X;

    [Fact]
    public void ItFallsOffToNoneAtItsReachAndPushesNothingBeyondIt()
    {
        double full = Charge.Power * (double)Spec.Debris.BurstPush;
        // Straight above the middle, gaps of 0, half the reach, all of it, and more: from the box's top edge, 500 up.
        Assert.Equal(full, Strength(Push(0, -500)!.Value), 6);
        Assert.Equal(full / 2, Strength(Push(0, -500 - 4000)!.Value), 6);
        Assert.Null(Push(0, -500 - 8000));
        Assert.Null(Push(0, -500 - 9000));
        Assert.True(Strength(Push(0, -500 - 7990)!.Value) is > 0 and < 0.01 * 500 * 100);
        // A burst of no reach pushes nothing, even on the box.
        Assert.Null(Push(0, -500, reach: 0));
    }

    [Fact]
    public void ItPushesAwayFromTheBurstAtTheNearestPointSoOffItsMiddleItTurns()
    {
        // Over the middle: straight down at the top edge, no turn.
        var centred = Push(0, -3000)!.Value;
        Assert.Equal((0, -500), (centred.AtX, centred.AtY));
        Assert.True(centred.Y > 0);
        Assert.Equal(0, centred.X, 6);
        Assert.Equal(0, Torque(centred), 6);
        // Over the right end: down at the top edge there, turning it clockwise as the screen shows it.
        var off = Push(1500, -3000)!.Value;
        Assert.Equal((1500, -500), (off.AtX, off.AtY));
        Assert.Equal(Strength(centred), Strength(off), 6);
        Assert.True(Torque(off) > 0);
        // Past its corner: along the line to the corner.
        var corner = Push(3000, -1500)!.Value;
        Assert.Equal((2000, -500), (corner.AtX, corner.AtY));
        Assert.Equal(-corner.X, corner.Y, 6);
        Assert.True(corner.X < 0 && corner.Y > 0);
    }

    [Fact]
    public void ATurnedBoxIsMetWhereItReallyIs()
    {
        // Turned a quarter, the plank stands upright: a burst 3 cells right meets its side half a cell from the middle.
        var turned = Push(3000, 0, Math.PI / 2)!.Value;
        Assert.Equal(500, turned.AtX, 6);
        Assert.Equal(0, turned.AtY, 6);
        Assert.True(turned.X < 0);
        Assert.Equal(Charge.Power * Spec.Debris.BurstPush * (1 - 2500 / 8000.0), Strength(turned), 6);
    }

    [Fact]
    public void ABurstInsideTheBoxPushesItsMiddleAwayAndOneAtTheMiddleUpNeverNaN()
    {
        var inside = Push(1000, 0)!.Value;
        Assert.Equal((1000, 0), (inside.AtX, inside.AtY));
        Assert.True(inside.X < 0);
        Assert.Equal(0, Torque(inside), 6);
        var middle = Push(0, 0)!.Value;
        Assert.Equal((0, 0), (middle.AtX, middle.AtY));
        Assert.Equal(0, middle.X, 6);
        Assert.Equal(-Charge.Power * (double)Spec.Debris.BurstPush, middle.Y, 6);
    }

    [Fact]
    public void AChargeTwoCellsOffThrowsASmallPieceTenToTwentyCellsASecondAndACargoContainerFarLess()
    {
        double Speed(PieceShape shape) => Strength(Push(0, -shape.HalfH - 2000, shape: shape)!.Value) / shape.Mass / 1000;
        double small = Speed(PieceShape.Of("scrapChunk")), container = Speed(PieceShape.Of("cargoContainer"));
        Assert.InRange(small, 10, 20);
        Assert.True(container < small / 4, $"{container} of {small}");
    }
}
