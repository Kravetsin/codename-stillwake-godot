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
    public void APieceIsCutAtItsBlocksEdgeWhereARunGoesOnAndKeepsItsEndsWhereItStops()
    {
        var (open, joins) = (Planting.End.Open, Planting.End.Joins);
        Assert.Equal(((0, 0, 24, 24), (0, 0, 24, 24)), Planting.Piece(Faces.Up, open, open));
        Assert.Equal(((4, 0, 20, 24), (4, 0, 20, 24)), Planting.Piece(Faces.Up, joins, open));
        Assert.Equal(((0, 0, 20, 24), (0, 0, 20, 24)), Planting.Piece(Faces.Down, open, joins));
        Assert.Equal(((4, 0, 16, 24), (4, 0, 16, 24)), Planting.Piece(Faces.Down, joins, joins));
        Assert.Equal(((0, 4, 24, 20), (0, 4, 24, 20)), Planting.Piece(Faces.Left, joins, open));
        Assert.Equal(((0, 4, 24, 16), (0, 4, 24, 16)), Planting.Piece(Faces.Right, joins, joins));
    }

    [Fact]
    public void APieceStoppingAtAWallOrAlongOneOfItsCellsKeepsItsTaperedEndSqueezedInNotOverTheStone()
    {
        var (open, joins, wall) = (Planting.End.Open, Planting.End.Joins, Planting.End.Wall);
        // Into a wall at its block's end: its whole band, its end within the block.
        Assert.Equal(((0, 0, 24, 24), (0, 0, 20, 24)), Planting.Piece(Faces.Up, open, wall));
        // A stair's step: the block's right face open beside its top cell only, then its bottom cell only.
        Assert.Equal(((0, 0, 24, 24), (0, 0, 24, 12)), Planting.Piece(Faces.Right, open, open, first: true, second: false));
        Assert.Equal(((0, 0, 24, 24), (0, 12, 24, 12)), Planting.Piece(Faces.Right, open, open, first: false, second: true));
        // Joining the run before it over its first cell, tapering into the step's riser over its second.
        Assert.Equal(((4, 0, 20, 24), (4, 0, 8, 24)), Planting.Piece(Faces.Up, joins, joins, first: true, second: false));
    }

    [Fact]
    public void AFaceOnAStairsStepIsOpenAlongItsCellOverTheStepAndMeetsTheRiserPastIt()
    {
        // Stone '#' stepping down to the left; moss at (2, 1), its block (2..3, 1..2): the step's top over its left cell only.
        string[] map =
        [
            "...##",
            "..###",
            ".####",
            "#####",
        ];
        const int width = 5;
        bool Open(int c) => c >= 0 && c < width * map.Length && map[c / width][c % width] == '.';
        int moss = 1 * width + 2;
        Assert.Equal((true, false, true, false), Planting.Along(Faces.Up, moss, width, Open));
        // Its left face open beside its top cell only, the step's floor under that.
        Assert.Equal((true, false, true, false), Planting.Along(Faces.Left, moss, width, Open));
        Assert.Equal((false, false, false, false), Planting.Along(Faces.Right, moss, width, Open));
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
