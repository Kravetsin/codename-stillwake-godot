using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>A wall's shape (0069): the geometry the sim and the hero share, and the grid's layer of it; stacks on slopes are in <see cref="ItemsFloorTests"/>.</summary>
public class ShapeTests
{
    private const int Unit = Shape.Unit;

    private static int Count(int shape)
    {
        int n = 0;
        for (int fx = 0; fx < Unit; fx++)
            for (int fy = 0; fy < Unit; fy++)
                if (Shape.Solid(shape, fx, fy)) n++;
        return n;
    }

    [Fact]
    public void CutsHalfACellAt45DegreesEachShapeTheMirrorOfAnother()
    {
        Assert.All(Shape.Cut, shape => Assert.Equal(Unit * (Unit + 1) / 2, Count(shape)));
        for (int fx = 0; fx < Unit; fx += 7)
            for (int fy = 0; fy < Unit; fy += 3)
            {
                // ◣ is ◢ seen in a mirror; ◤ and ◥ are ◣ and ◢ upside down.
                Assert.Equal(Shape.Solid(Shape.BottomRight, fx, fy), Shape.Solid(Shape.BottomLeft, Unit - 1 - fx, fy));
                Assert.Equal(Shape.Solid(Shape.BottomLeft, fx, fy), Shape.Solid(Shape.TopLeft, fx, Unit - 1 - fy));
                Assert.Equal(Shape.Solid(Shape.BottomRight, fx, fy), Shape.Solid(Shape.TopRight, fx, Unit - 1 - fy));
                Assert.True(Shape.Solid(Shape.None, fx, fy));
            }
    }

    [Fact]
    public void ItsTopIsTheFirstSolidThousandthDownEachColumnAndClosedSidesAreSolidAllAlong()
    {
        foreach (int shape in Shape.Cut.Append(Shape.None))
        {
            for (int fx = 0; fx < Unit; fx++)
            {
                int top = Enumerable.Range(0, Unit).First(fy => Shape.Solid(shape, fx, fy));
                Assert.Equal(top, Shape.TopAt(shape, fx));
                Assert.All(Enumerable.Range(top, Unit - top), fy => Assert.True(Shape.Solid(shape, fx, fy) || !Shape.IsFloor(shape)));
            }
            bool All(Func<int, (int, int)> along) => Enumerable.Range(0, Unit).All(i => Shape.Solid(shape, along(i).Item1, along(i).Item2));
            int closed = (All(i => (i, 0)) ? Doorways.Up : 0) | (All(i => (Unit - 1, i)) ? Doorways.Right : 0)
                | (All(i => (i, Unit - 1)) ? Doorways.Down : 0) | (All(i => (0, i)) ? Doorways.Left : 0);
            Assert.Equal(closed, Shape.ClosedSides(shape));
        }
    }

    [Fact]
    public void ARampOfSlopesRisesAThousandthAThousandthWithNoStepBetweenItsCellsOrOntoAPlateau()
    {
        // ◢ cells up and to the right, a whole wall after them: the ground across them, a thousandth at a time.
        int Ground(int x) => (x / Unit) switch
        {
            0 => 2 * Unit + Shape.TopAt(Shape.BottomRight, x % Unit),
            1 => 1 * Unit + Shape.TopAt(Shape.BottomRight, x % Unit),
            _ => 1 * Unit,
        };
        for (int x = 1; x < 3 * Unit; x++) Assert.InRange(Ground(x - 1) - Ground(x), 0, 1);
        // And down a ◣ off the plateau the same.
        Assert.Equal(1 * Unit, 1 * Unit + Shape.TopAt(Shape.BottomLeft, 0));
    }

    private const string Shapes = """
        #####
        #/\r7
        #...#
        #####
        """;

    [Fact]
    public void ReadsTheFourSlopesOffAMapAsWallsOfTheDefaultHullCutToThem()
    {
        var w = new Scenario(Shapes);
        Assert.Equal(new[] { Shape.BottomRight, Shape.BottomLeft, Shape.TopLeft, Shape.TopRight }, Enumerable.Range(1, 4).Select(x => w.M.Grid.ShapeAt(w.At(x, 1))));
        Assert.All(Enumerable.Range(1, 4), x => Assert.Equal(Content.Materials.DefaultHull, w.M.Grid.WallAt(w.At(x, 1))?.Id));
        Assert.All(Enumerable.Range(1, 4), x => Assert.NotNull(w.M.Grid.BackAt(w.At(x, 1))));
        Assert.Equal(Shape.None, w.M.Grid.ShapeAt(w.At(0, 1)));
        Assert.Equal(Shape.None, w.M.Grid.ShapeAt(-1));
        Assert.Equal(Shape.BottomRight, w.M.Grid.ShapeLayer[w.At(1, 1)]);
    }

    [Fact]
    public void TakingASlopedWallApartTakesItsShapeWithItAndTheLogNamesIt()
    {
        var w = new Scenario(Shapes);
        int version = w.M.Grid.Version, cell = w.At(1, 1);
        w.Do(new ClearAt(cell, "structure")).Run();
        Assert.Equal(Shape.None, w.M.Grid.ShapeAt(cell));
        Assert.Contains(new CellChange(cell, GridLayers.Structure | GridLayers.Material | GridLayers.Shape), w.M.Grid.ChangedSince(version)!);
        // Built again, a wall is whole: shapes come from maps alone so far.
        w.Do(new Place(cell, Placement.Wall())).Run();
        Assert.Equal((Structure.Wall, Shape.None), (w.M.Grid.StructureAt(cell), w.M.Grid.ShapeAt(cell)));
    }

    [Fact]
    public void KeepsItsShapesThroughASaveAndALoad()
    {
        var w = new Scenario(Shapes).Reload();
        Assert.Equal(new[] { Shape.BottomRight, Shape.BottomLeft, Shape.TopLeft, Shape.TopRight }, Enumerable.Range(1, 4).Select(x => w.M.Grid.ShapeAt(w.At(x, 1))));
    }

    private static readonly string[] Kinds = ["wall", "floor", "ladder", "door"];

    /// <summary>A shape lies only on a wall: on any map, through any building, taking apart and toggling, refused ones too.</summary>
    [Property(MaxTest = 300)]
    public Property AShapeLiesOnlyOnAWall()
    {
        var gen =
            from width in Gen.Choose(1, 8)
            from height in Gen.Choose(1, 6)
            from cells in Gen.Elements('.', '#', '/', '\\', 'r', '7', '=', 'H', 'D', ' ').ArrayOf(width * height)
            from edits in (from cell in Gen.Choose(0, 1 << 20) from kind in Gen.Choose(0, 6) select (cell, kind)).ListOf()
            select (rows: Enumerable.Range(0, height).Select(y => new string(cells, y * width, width)).ToArray(), edits: edits.Take(40).ToList());
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var w = new Scenario(Layout.Parse(t.rows));
            int size = w.M.Grid.Width * w.M.Grid.Height;
            bool Holds() => Enumerable.Range(0, size).All(c => w.M.Grid.ShapeAt(c) == Shape.None || w.M.Grid.StructureAt(c) == Structure.Wall);
            if (!Holds()) return false.Label("as the map drew it");
            foreach (var (at, kind) in t.edits)
            {
                int cell = at % size;
                w.Do(kind switch
                {
                    < 4 => new Place(cell, new Placement("structure", Kinds[kind], kind == 0 ? "scrap" : null)),
                    4 => new ClearAt(cell, "structure"),
                    5 => new ToggleDoor(cell),
                    _ => new ClearAt(cell, "back"),
                }).Run();
                if (!Holds()) return false.Label($"after {kind} at {cell}");
            }
            return true.ToProperty();
        });
    }
}
