using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Walls planned cut to a shape (0069, part 2): what they are planned with, built into, cost and give back.</summary>
public class BuildingShapeTests
{
    /// <summary>Nine cells wide: cell (x, y) is y * 9 + x.</summary>
    private const string Open = """
        #########
        #.......#
        #.......#
        #########
        """;
    private static int At(int x, int y) => y * 9 + x;

    private static Placement Cut(int shape, string material = "scrap") => Placement.Wall(material) with { Shape = shape };

    private static Scenario Stocked()
    {
        var w = new Scenario(Open, cells: true);
        w.GiveItems(0, ("scrap", 100), ("plate", 100));
        return w;
    }

    /// <summary>Works plan <paramref name="id"/> to its end, a press a tick.</summary>
    private static void Finish(Scenario w, int id)
    {
        while (w.M.Building.Plans.Any(p => p.Id == id)) w.Do(new Work(id)).Run();
    }

    [Fact]
    public void PlansAWallOfEachShapeAndBuildsItCutSoMaterialAndAll()
    {
        var w = Stocked();
        int[] shapes = [.. Shape.Cut, Shape.None];
        for (int i = 0; i < shapes.Length; i++) w.Do(new PlacePlan(At(1 + i, 1), Cut(shapes[i], "iron")));
        w.Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(shapes, w.M.Building.Plans.Select(p => p.Place!.Shape));
        foreach (var plan in w.M.Building.Plans.ToList()) Finish(w, plan.Id);
        Assert.Equal(shapes, shapes.Select((_, i) => w.M.Grid.ShapeAt(At(1 + i, 1))));
        Assert.All(shapes.Select((_, i) => w.M.Grid.WallAt(At(1 + i, 1))?.Id), id => Assert.Equal("iron", id));
    }

    [Fact]
    public void RefusesAShapeOnAnythingButAWall()
    {
        var w = Stocked();
        Placement[] refused =
        [
            new("structure", "floor", Shape: Shape.BottomRight),
            new("structure", "ladder", Shape: Shape.BottomLeft),
            new("structure", "door", Shape: Shape.TopLeft),
            Placement.BackWall() with { Shape = Shape.TopRight },
            new("conduit", "pipe", Shape: Shape.BottomRight),
            new("machine", "crate", Shape: Shape.BottomRight),
            // No such shape: a code saves never had.
            Cut(Shape.TopRight + 1),
        ];
        foreach (var place in refused) w.Do(new PlacePlan(At(2, 1), place));
        w.Run();
        Assert.Equal(Enumerable.Repeat("grid.notShapeable", refused.Length), w.Refusals.Select(r => r.Reason));
        Assert.Empty(w.M.Building.Plans);
        Assert.Equal("grid.notShapeable", w.M.Building.Problem(At(2, 1), refused[0]));
    }

    [Fact]
    public void AShapedWallCostsTakesAndGivesBackWhatAWholeOneDoes()
    {
        var scrap = Materials.Get("scrap");
        foreach (int shape in Shape.Cut)
        {
            var w = Stocked();
            Assert.Equal(w.M.Building.CostFor(Placement.Wall()), w.M.Building.CostFor(Cut(shape)));
            w.Do(new PlacePlan(At(1, 1), Cut(shape))).Run();
            Assert.Equal(scrap.BuildTicks, w.M.Building.TicksFor(1));
            Assert.Equal<(string, int)>(scrap.Cost, w.M.Building.CostOf(1)!);
            int before = Count(w, "scrap");
            Finish(w, 1);
            Assert.Equal(before - scrap.Cost.Single().Count, Count(w, "scrap"));
            // Taken apart, it gives back a whole wall's cost, and its shape goes with it.
            w.Do(new Deconstruct(At(1, 1), "structure")).Run();
            Assert.Equal<(string, int)>(scrap.Cost, w.M.Building.RefundOf(2)!);
            Finish(w, 2);
            Assert.Equal(before, Count(w, "scrap"));
            Assert.Equal((Structure.Empty, Shape.None), (w.M.Grid.StructureAt(At(1, 1)), w.M.Grid.ShapeAt(At(1, 1))));
        }
    }

    [Fact]
    public void KeepsAShapedPlanThroughASaveAndALoad()
    {
        var w = Stocked().Do(new PlacePlan(At(3, 2), Cut(Shape.TopLeft))).Run().Reload();
        Assert.Equal(Cut(Shape.TopLeft), w.M.Building.Plans.Single().Place);
    }

    [Fact]
    public void FuzzDrawsWallsOfEveryShapeAndShapesWhereNoneMayBe()
    {
        var w = Stocked();
        var rng = new Rng(new RngState { S = 69 });
        var placed = new List<Placement>();
        for (int i = 0; i < 4000; i++)
            if (w.M.Building.Fuzz(rng, 0) is PlacePlan p) placed.Add(p.Placement);
        Assert.All(Shape.Cut, shape => Assert.Contains(placed, p => p.Kind == "wall" && p.Shape == shape));
        Assert.Contains(placed, p => p.Kind != "wall" && p.Shape != Shape.None);
    }

    private static int Count(Scenario w, string item) => w.M.Items.Inventory(0).Sum(s => s?.Item == item ? s.Count : 0);
}
