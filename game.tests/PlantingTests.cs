using Stillwake.Game.Features.Flora;
using Stillwake.Sim.Grid;
using Xunit;
using Anchor = Stillwake.Content.Flora.Anchor;
using Faces = Stillwake.Game.Features.Flora.Planting.Faces;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Tests;

/// <summary>
/// Where what grows is drawn against what holds it: planted, its light in the open, its drips landing. At the 8 px cell each
/// growth is two by two cells (0104), one on a floor at its block's bottom-left, any other at its top-left.
/// </summary>
public class PlantingTests
{
    private const int Cell = Camera.Cell;

    [Fact]
    public void AGrowthsLightInTheRockShinesFromJustPastItsOpenFaceNearestItsGlow()
    {
        // Moss in the stone at (4, 6), its glow on its block's bottom face, a hair inside the stone: its light comes out under it.
        var below = Planting.Light(Anchor.Rock, 4, 6, Cell, 2, 2, 40, 63.9, Faces.Down | Faces.Left);
        Assert.Equal((40.0, 64 + Planting.LightOff), below);
        // A heat crack's glow in the middle of the stone, only its top open: from over the floor.
        Assert.Equal((40.0, 48 - Planting.LightOff), Planting.Light(Anchor.Rock, 4, 6, Cell, 2, 2, 40, 56, Faces.Up));
        // Nearer the left face, both open: out to the left, held off the block's corners.
        Assert.Equal((32 - Planting.LightOff, 48 + Planting.LightOff), Planting.Light(Anchor.Rock, 4, 6, Cell, 2, 2, 33, 50, Faces.Left | Faces.Up | Faces.Down));
        Assert.Null(Planting.Light(Anchor.Rock, 4, 6, Cell, 2, 2, 40, 56, Faces.None));
    }

    [Fact]
    public void AGrowthsLightOnAFloorOrFromACeilingIsHeldOffItsFace()
    {
        Assert.Equal((40.0, 64 - Planting.LightOff), Planting.Light(Anchor.Floor, 4, 7, Cell, 2, 2, 40, 63.5, Faces.None));
        Assert.Equal((40.0, 55.0), Planting.Light(Anchor.Floor, 4, 7, Cell, 2, 2, 40, 55, Faces.None));
        Assert.Equal((40.0, 48 + Planting.LightOff), Planting.Light(Anchor.Ceiling, 4, 6, Cell, 2, 2, 40, 48.2, Faces.None));
    }

    [Fact]
    public void APlantsBaseSinksIntoTheGroundAndTheRowsInItAreCutOff()
    {
        // A cap 20 × 19 whose last opaque row is 17, on the floor in its two columns from cell (4, 7): the ground's top at 64.
        var (x, y, from, rows) = Planting.Picture(Anchor.Floor, 4, 7, Cell, 2, 20, 19, 5, 17, sink: 2);
        Assert.Equal(40 - 10, x);
        // Row 17 at 65, a second row of the ground's: drawn down to 63, the ground's top edge over the rest.
        Assert.Equal(65, y + 17);
        Assert.Equal((0, 64 - y), (from, rows));
    }

    [Fact]
    public void AHangingGrowthsTopSinksIntoTheCeilingMirrored()
    {
        // Hanging from the wall over cell (4, 6), its first opaque row 1: the ceiling's last row at 47.
        var (_, y, from, rows) = Planting.Picture(Anchor.Ceiling, 4, 6, Cell, 2, 20, 21, 1, 18, sink: 2);
        Assert.Equal(46, y + 1);
        Assert.Equal(48, y + from);
        Assert.Equal(21 - from, rows);
    }

    [Fact]
    public void AMossBandRunsTheWholeEdgeOfItsBlockAlongARunAndALonePieceKeepsItsEnds()
    {
        Assert.Null(Planting.Strip(Faces.Down, 4, 6, Cell, 2, 2, 2, before: false, after: false));
        Assert.Equal((32, 63, 16, 2), Planting.Strip(Faces.Down, 4, 6, Cell, 2, 2, 2, before: true, after: false));
        Assert.Equal((32, 48, 16, 1), Planting.Strip(Faces.Up, 4, 6, Cell, 2, 2, 1, before: false, after: true));
        Assert.Equal((32, 48, 1, 16), Planting.Strip(Faces.Left, 4, 6, Cell, 2, 2, 1, before: true, after: true));
        Assert.Equal((47, 48, 1, 16), Planting.Strip(Faces.Right, 4, 6, Cell, 2, 2, 1, before: true, after: false));
    }

    [Fact]
    public void InInkAPieceIsCutAtItsBlocksEdgeWhereARunGoesOnAndKeepsItsEndsWhereItStops()
    {
        Assert.Equal((0, 0, 24, 24), Planting.Piece(Faces.Up, before: false, after: false));
        Assert.Equal((4, 0, 20, 24), Planting.Piece(Faces.Up, before: true, after: false));
        Assert.Equal((0, 0, 20, 24), Planting.Piece(Faces.Down, before: false, after: true));
        Assert.Equal((4, 0, 16, 24), Planting.Piece(Faces.Down, before: true, after: true));
        Assert.Equal((0, 4, 24, 20), Planting.Piece(Faces.Left, before: true, after: false));
        Assert.Equal((0, 4, 24, 16), Planting.Piece(Faces.Right, before: true, after: true));
    }

    /// <summary>A column 1 wide: open, open, open, then walls from row 3.</summary>
    private static double? Drip(double y) =>
        Planting.DripFloor(c => c < 3 ? Structure.Empty : Structure.Wall, 1, 5, Cell, Cell / 4.0, y, 20);

    [Fact]
    public void ADripLandsOnTheFirstSolidUnderIt()
    {
        Assert.Equal(3 * Cell, Drip(10));
        // Its tip on the floor already: nowhere to fall, no drip.
        Assert.Null(Drip(3 * Cell - 1));
        Assert.Null(Planting.DripFloor(_ => Structure.Empty, 1, 5, Cell, Cell / 4.0, 10, 20));
    }
}
