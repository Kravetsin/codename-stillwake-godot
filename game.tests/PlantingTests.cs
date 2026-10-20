using Stillwake.Game.Features.Flora;
using Stillwake.Sim.Grid;
using Xunit;
using Anchor = Stillwake.Content.Flora.Anchor;
using Faces = Stillwake.Game.Features.Flora.Planting.Faces;

namespace Stillwake.Game.Tests;

/// <summary>Where what grows is drawn against what holds it: planted, its light in the open, its drips landing.</summary>
public class PlantingTests
{
    private const int Cell = 16;

    [Fact]
    public void AGrowthsLightInTheRockShinesFromJustPastItsOpenFaceNearestItsGlow()
    {
        // Moss in the stone at (2, 3), its glow on its bottom face, a hair inside the stone: its light comes out under it.
        var below = Planting.Light(Anchor.Rock, 2, 3, Cell, 40, 63.9, Faces.Down | Faces.Left);
        Assert.Equal((40.0, 64 + Planting.LightOff), below);
        // A heat crack's glow in the middle of the stone, only its top open: from over the floor.
        Assert.Equal((40.0, 48 - Planting.LightOff), Planting.Light(Anchor.Rock, 2, 3, Cell, 40, 56, Faces.Up));
        // Nearer the left face, both open: out to the left, held off the cell's corners.
        Assert.Equal((32 - Planting.LightOff, 48 + Planting.LightOff), Planting.Light(Anchor.Rock, 2, 3, Cell, 33, 50, Faces.Left | Faces.Up | Faces.Down));
        Assert.Null(Planting.Light(Anchor.Rock, 2, 3, Cell, 40, 56, Faces.None));
    }

    [Fact]
    public void AGrowthsLightOnAFloorOrFromACeilingIsHeldOffItsFace()
    {
        Assert.Equal((40.0, 64 - Planting.LightOff), Planting.Light(Anchor.Floor, 2, 3, Cell, 40, 63.5, Faces.None));
        Assert.Equal((40.0, 55.0), Planting.Light(Anchor.Floor, 2, 3, Cell, 40, 55, Faces.None));
        Assert.Equal((40.0, 48 + Planting.LightOff), Planting.Light(Anchor.Ceiling, 2, 3, Cell, 40, 48.2, Faces.None));
    }

    [Fact]
    public void APlantsBaseSinksIntoTheGroundAndTheRowsInItAreCutOff()
    {
        // A cap 20 × 19 whose last opaque row is 17, on the floor of cell (2, 3): the ground's top at 64.
        var (x, y, from, rows) = Planting.Picture(Anchor.Floor, 2, 3, Cell, 20, 19, 5, 17, sink: 2);
        Assert.Equal(40 - 10, x);
        // Row 17 at 65, a second row of the ground's: drawn down to 63, the ground's top edge over the rest.
        Assert.Equal(65, y + 17);
        Assert.Equal((0, 64 - y), (from, rows));
    }

    [Fact]
    public void AHangingGrowthsTopSinksIntoTheCeilingMirrored()
    {
        // Hanging from the wall over cell (2, 3), its first opaque row 1: the ceiling's last row at 47.
        var (_, y, from, rows) = Planting.Picture(Anchor.Ceiling, 2, 3, Cell, 20, 21, 1, 18, sink: 2);
        Assert.Equal(46, y + 1);
        Assert.Equal(48, y + from);
        Assert.Equal(21 - from, rows);
    }

    [Fact]
    public void AMossBandRunsTheWholeEdgeOfItsCellAlongARunAndALonePieceKeepsItsEnds()
    {
        Assert.Null(Planting.Strip(Faces.Down, 2, 3, Cell, 2, before: false, after: false));
        Assert.Equal((32, 63, 16, 2), Planting.Strip(Faces.Down, 2, 3, Cell, 2, before: true, after: false));
        Assert.Equal((32, 48, 16, 1), Planting.Strip(Faces.Up, 2, 3, Cell, 1, before: false, after: true));
        Assert.Equal((32, 48, 1, 16), Planting.Strip(Faces.Left, 2, 3, Cell, 1, before: true, after: true));
        Assert.Equal((47, 48, 1, 16), Planting.Strip(Faces.Right, 2, 3, Cell, 1, before: true, after: false));
    }

    /// <summary>A column 1 wide: open, open, open, a floor slope ◢ at row 3, then a wall.</summary>
    private static double? Drip(double y, int shape = Shape.BottomRight) =>
        Planting.DripFloor(c => c < 3 ? Structure.Empty : Structure.Wall, c => c == 3 ? shape : Shape.None, 1, 5, Cell, 4, y, 20);

    [Fact]
    public void ADripLandsOnTheFirstSolidUnderItDownASlopesDiagonal()
    {
        // ◢ at column 4 of 16: its diagonal 11.75 px down the cell, a quarter of a cell in from its right.
        Assert.Equal(3 * Cell + (Shape.Unit - 1 - 250) * Cell / (double)Shape.Unit, Drip(10));
        Assert.Equal(3 * Cell, Drip(10, Shape.None));
        // Its tip on the floor already: nowhere to fall, no drip.
        Assert.Null(Drip(3 * Cell - 1, Shape.None));
        Assert.Null(Planting.DripFloor(_ => Structure.Empty, _ => Shape.None, 1, 5, Cell, 4, 10, 20));
    }
}
