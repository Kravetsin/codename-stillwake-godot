using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/building/building.test.ts, ported.</summary>
public class BuildingTests
{
    private static readonly Placement Wall = Placement.Wall("iron");
    private static readonly Placement Pipe = new("conduit", "pipe");
    private static readonly Placement Tank = new("machine", "tank");

    /// <summary>Nine cells wide: cell (x, y) is y * 9 + x.</summary>
    private const string Open = """
        #########
        #.......#
        #.......#
        #.......#
        #########
        """;
    private static int At(int x, int y) => y * 9 + x;

    /// <summary>A plan as a value, its deliveries as text, so plans compare by what they hold.</summary>
    internal static (int Id, int Cell, int Progress, Placement? Place, string? Clear, string Delivered) Of(Plan p) =>
        (p.Id, p.Cell, p.Progress, p.Place, p.Clear, string.Join(" ", p.Delivered.Select(s => $"{s.Item}:{s.Count}")));

    /// <summary>A blueprint as <c>Plans</c> shows it before anything is delivered.</summary>
    private static (int, int, int, Placement?, string?, string) Placed(int id, int cell, Placement place) => Of(Plan.Blueprint(id, cell, place));

    /// <summary>A mark as <c>Plans</c> shows it.</summary>
    private static (int, int, int, Placement?, string?, string) Marked(int id, int cell, string layer) => Of(Plan.Mark(id, cell, layer));

    private static (int, int, int, Placement?, string?, string)[] PlansOf(Scenario w) => w.M.Building.Plans.Select(Of).ToArray();
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int[] Ids(IEnumerable<Plan> plans) => plans.Select(p => p.Id).ToArray();

    [Fact]
    public void PlansEveryKindOfThingTheToolbarOffers()
    {
        Placement[] placements =
        [
            Placement.Wall("scrap"),
            Wall,
            new("structure", "floor"),
            new("structure", "ladder"),
            new("structure", "door"),
            Pipe,
            new("conduit", "valve"),
            new("machine", "vent"),
        ];
        var w = new Scenario(Open);
        for (int i = 0; i < placements.Length; i++)
            w.Do(new PlacePlan(i < 7 ? At(1 + i, 1) : At(1, 2), placements[i]));
        w.Do(new PlacePlan(At(3, 2), Tank)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal([.. placements, Tank], w.M.Building.Plans.Select(p => p.Place));
    }

    [Fact]
    public void KeepsWhatItPlannedInItsSliceAndLeavesTheGridAlone()
    {
        var w = new Scenario(Open).Do(new PlacePlan(At(1, 1), Wall)).Run();
        Assert.Equal([Placed(1, At(1, 1), Wall)], PlansOf(w));
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(At(1, 1)));
    }

    [Fact]
    public void PassesTheGridsReasonOn()
    {
        var w = new Scenario(Open)
            .Do(new PlacePlan(At(0, 0), Wall))
            .Do(new PlacePlan(-1, Pipe))
            .Run();
        Assert.Equal(["grid.occupied", "grid.outOfBounds"], Reasons(w));
        Assert.Empty(w.M.Building.Plans);
    }

    [Fact]
    public void RefusesASecondPlanForASlotAlreadyPlannedEvenInTheSameTick()
    {
        var w = new Scenario(Open)
            .Do(new PlacePlan(At(2, 1), Wall))
            .Do(new PlacePlan(At(2, 1), new Placement("structure", "floor")))
            .Run();
        Assert.Equal(["building.planned"], Reasons(w));
        Assert.Equal([Placed(1, At(2, 1), Wall)], PlansOf(w));
    }

    [Fact]
    public void AsksTheGridFirstSoItsReasonWinsOverAClashWithAPlan()
    {
        var w = new Scenario(Open)
            .Do(new PlacePlan(At(2, 1), Tank))
            .Do(new PlacePlan(At(1, 0), Tank))
            .Run();
        Assert.Equal(["grid.blocked"], Reasons(w));
    }

    [Fact]
    public void NeverGivesACancelledPlansIdToANewOne()
    {
        var w = new Scenario(Open)
            .Do(new PlacePlan(At(1, 1), Wall))
            .Do(new PlacePlan(At(2, 1), Wall))
            .Run()
            .Do(new CancelPlan(2))
            .Run()
            .Do(new PlacePlan(At(3, 1), Wall))
            .Run();
        Assert.Equal([1, 3], Ids(w.M.Building.Plans));
    }

    [Fact]
    public void TreatsAPlannedMachineAsFillingItsWholeFootprint()
    {
        var w = new Scenario(Open)
            .Do(new PlacePlan(At(2, 1), Tank))
            .Do(new PlacePlan(At(3, 2), Tank))
            .Do(new PlacePlan(At(3, 2), Wall))
            .Do(new PlacePlan(At(4, 1), Tank))
            .Run();
        Assert.Equal(["building.planned", "building.planned"], Reasons(w));
        Assert.Equal([1], Ids(w.M.Building.PlansAt(At(3, 2))));
        Assert.Equal([2], Ids(w.M.Building.PlansAt(At(4, 2))));
        Assert.Empty(w.M.Building.PlansAt(At(6, 1)));
    }

    [Fact]
    public void LetsAConduitBePlannedUnderAPlannedWallOrMachineButNotASecondConduit()
    {
        var w = new Scenario(Open)
            .Do(new PlacePlan(At(1, 1), Wall))
            .Do(new PlacePlan(At(1, 1), Pipe))
            .Do(new PlacePlan(At(3, 1), Tank))
            .Do(new PlacePlan(At(4, 2), Pipe))
            .Do(new PlacePlan(At(4, 2), new Placement("conduit", "valve")))
            .Run();
        Assert.Equal(["building.planned"], Reasons(w));
        Assert.Equal(
            [Placed(1, At(1, 1), Wall), Placed(2, At(1, 1), Pipe), Placed(3, At(3, 1), Tank), Placed(4, At(4, 2), Pipe)],
            PlansOf(w));
    }

    /// <summary>
    /// Plans claim their slots, so only another plan's build could fill one, and a plan refuses that:
    /// the tests' instant builds, standing in for builds, claim nothing planned either. A lamp built at
    /// once on a floor's blueprint left the floor's last tick of work throwing (GridTests' zone walk).
    /// </summary>
    [Fact]
    public void TestBuildsAtOnceFillNoSlotABlueprintClaimsSoItsWorkStillBuildsIt()
    {
        var floor = new Placement("structure", "floor");
        var w = new Scenario(Open)
            .Do(new Items.SetCreative(true))
            .Do(new PlacePlan(At(1, 1), floor))
            .Do(new PlacePlan(At(3, 1), Tank))
            .Run()
            .Do(new Place(At(1, 1), new Placement("machine", "lamp")))
            .Do(new Place(At(4, 2), Wall))
            .Do(new Place(At(1, 1), Pipe))
            .Run();
        Assert.Equal(["building.planned", "building.planned"], Reasons(w));
        Assert.Empty(w.M.Grid.Machines);
        Assert.Equal(Conduit.Pipe, w.M.Grid.ConduitAt(At(1, 1)));
        for (int i = 0; i < Builds.Get("floor").BuildTicks; i++) w.Do(new Work(1)).Run();
        Assert.Equal(Structure.Floor, w.M.Grid.StructureAt(At(1, 1)));
    }

    [Fact]
    public void PlansABackWallBehindAPlannedWallAndInSpaceButNotWhereOneStandsOrIsPlanned()
    {
        var backWall = Placement.BackWall("scrap");
        // Space, a wall, two backed cells, one with no back wall, one backed, a wall, space.
        var w = new Scenario(Layout.Parse(["   #.. .#   "]))
            .Do(new PlacePlan(6, Wall))
            .Do(new PlacePlan(6, backWall))
            .Do(new PlacePlan(0, backWall))
            .Do(new PlacePlan(4, backWall))
            .Do(new PlacePlan(0, backWall))
            .Run();
        Assert.Equal(["grid.occupied", "building.planned"], Reasons(w));
        Assert.Equal([Placed(1, 6, Wall), Placed(2, 6, backWall), Placed(3, 0, backWall)], PlansOf(w));
    }

    [Fact]
    public void AnswersAGhostWithTheReasonTheCommandWouldGet()
    {
        var w = new Scenario(Open);
        Assert.Null(w.M.Building.Problem(At(1, 1), Wall));
        w.Do(new PlacePlan(At(1, 1), Wall)).Run();
        Assert.Equal("building.planned", w.M.Building.Problem(At(1, 1), Wall));
        Assert.Equal("grid.occupied", w.M.Building.Problem(At(0, 0), Wall));
        Assert.Equal("building.nothingThere", w.M.Building.Problem(At(1, 2), "structure"));
    }

    /// <summary>grid.test.ts "tells which cells a machine would cover, and none past the grid's edge": the query plans cover cells by.</summary>
    [Fact]
    public void GridTellsWhichCellsAMachineWouldCoverAndNonePastTheGridsEdge()
    {
        var w = new Scenario("""
            ######
            #.TT.#
            #HTT=D
            ######
            """);
        Assert.Equal([8, 9, 14, 15], w.M.Grid.Footprint("tank", 8));
        Assert.Null(w.M.Grid.Footprint("tank", 5));
        Assert.Null(w.M.Grid.Footprint("tank", 19));
        Assert.Equal([23], w.M.Grid.Footprint("vent", 23));
        Assert.Null(w.M.Grid.Footprint("nothing", 8));
    }

    // Deconstruction.

    /// <summary>Seven wide: a tank, a pipe, a floor and a ladder in a room.</summary>
    private const string Room = """
        #######
        #TT.-.#
        #TT=H.#
        #######
        """;
    private static int Cell(int x, int y) => y * 7 + x;

    [Fact]
    public void MarksWhatALayerOfACellHoldsOnce()
    {
        var w = new Scenario(Room)
            .Do(new Deconstruct(Cell(0, 0), "structure"))
            .Do(new Deconstruct(Cell(4, 1), "conduit"))
            .Do(new Deconstruct(Cell(0, 0), "structure"))
            .Run();
        Assert.Equal(["building.planned"], Reasons(w));
        Assert.Equal([Marked(1, Cell(0, 0), "structure"), Marked(2, Cell(4, 1), "conduit")], PlansOf(w));
    }

    [Fact]
    public void RefusesAMarkWhereThatLayerHoldsNothing()
    {
        var w = new Scenario(Room);
        foreach (var layer in new[] { "structure", "conduit", "machine" }) w.Do(new Deconstruct(Cell(5, 1), layer));
        w.Do(new Deconstruct(-3, "structure")).Run();
        Assert.Equal(Enumerable.Repeat("building.nothingThere", 4), Reasons(w));
    }

    [Fact]
    public void RefusesTakingApartOrPlanningTheLifepodBuiltIntoTheShipCreativeModeOrNot()
    {
        var w = new Scenario("""
            #######
            #OOO..#
            #OOO..#
            #OOO..#
            #######
            """);
        int pod = w.M.Grid.Machines.Single().Id;
        w.Do(new Deconstruct(8, "machine")).Do(new Deconstruct(23, "machine")).Run();
        w.Do(new Items.SetCreative(true)).Run();
        w.Do(new Deconstruct(16, "machine")).Do(new PlacePlan(8, new Placement("machine", "lifepod"))).Run();
        w.Do(new PlacePlan(12, new Placement("machine", "lifepod"))).Run();
        Assert.Equal(Enumerable.Repeat("building.fixed", 5), Reasons(w));
        Assert.Empty(w.M.Building.Plans);
        Assert.Equal(pod, w.M.Grid.MachineAt(16)?.Id);
        // What it stands on and behind it are still the ship's to change.
        w.Do(new Deconstruct(8, "back")).Run();
        Assert.Equal(5, w.Refusals.Count);
    }

    [Fact]
    public void KeepsAMachinesMarkAtItsTopLeftFromWhicheverCellItWasMarked()
    {
        var w = new Scenario(Room)
            .Do(new Deconstruct(Cell(2, 2), "machine"))
            .Do(new Deconstruct(Cell(1, 1), "machine"))
            .Run();
        Assert.Equal(["building.planned"], Reasons(w));
        Assert.Equal([Marked(1, Cell(1, 1), "machine")], PlansOf(w));
        Assert.Equal([1], Ids(w.M.Building.PlansAt(Cell(2, 2))));
    }

    [Fact]
    public void MarksEachLayerOfOneCellApartAPipeInAWallTakesTwoMarks()
    {
        var w = new Scenario(Room)
            .Do(new Place(Cell(0, 0), Pipe))
            .Run()
            .Do(new Deconstruct(Cell(0, 0), "structure"))
            .Do(new Deconstruct(Cell(0, 0), "conduit"))
            .Run();
        Assert.Empty(w.Refusals);
        Assert.Equal([1, 2], Ids(w.M.Building.Plans));
    }

    [Fact]
    public void TakesAMachineFirstThenAConduitTheStructureAndTheBackPassingOverTheMarked()
    {
        var w = new Scenario(Room)
            .Do(new Place(Cell(1, 1), Pipe))
            .Do(new Place(Cell(0, 1), Pipe))
            .Run();
        (string, int)? Next(int c) => w.M.Building.ClearAt(c);
        // A tank with a pipe under it, at a cell of the tank that is not its corner.
        Assert.Equal(("machine", Cell(1, 1)), Next(Cell(2, 2)));
        Assert.Equal(("machine", Cell(1, 1)), Next(Cell(1, 1)));
        w.Do(new Deconstruct(Cell(1, 1), "machine")).Run();
        Assert.Equal(("conduit", Cell(1, 1)), Next(Cell(1, 1)));
        // A pipe in a wall: the pipe, the wall, the wall behind, then the pipe again, already marked.
        Assert.Equal(("conduit", Cell(0, 1)), Next(Cell(0, 1)));
        w.Do(new Deconstruct(Cell(0, 1), "conduit")).Run();
        Assert.Equal(("structure", Cell(0, 1)), Next(Cell(0, 1)));
        w.Do(new Deconstruct(Cell(0, 1), "structure")).Run();
        Assert.Equal(("back", Cell(0, 1)), Next(Cell(0, 1)));
        w.Do(new Deconstruct(Cell(0, 1), "back")).Run();
        Assert.Equal(("conduit", Cell(0, 1)), Next(Cell(0, 1)));
        // An empty cell of the room has only the wall behind it.
        Assert.Equal(("back", Cell(5, 1)), Next(Cell(5, 1)));
        w.Do(new ClearAt(Cell(5, 1), "back")).Run();
        Assert.Null(Next(Cell(5, 1)));
    }

    [Fact]
    public void LooksOnlyInTheLayersAskedForInTheirOrder()
    {
        var w = new Scenario(Room).Do(new Place(Cell(0, 1), Pipe)).Do(new Place(Cell(1, 1), Pipe)).Run();
        (string, int)? Next(int c, params string[] layers) => w.M.Building.ClearAt(c, layers);
        // A pipe in a wall, with the wall behind.
        Assert.Equal(("structure", Cell(0, 1)), Next(Cell(0, 1), "structure", "back"));
        Assert.Equal(("back", Cell(0, 1)), Next(Cell(0, 1), "back", "conduit"));
        // The pipes overlay asks for a pipe before the machine on it: a full crate or a tank on a pipe
        // does not stand in the way of taking the pipe.
        Assert.Equal(("conduit", Cell(1, 1)), Next(Cell(1, 1), "conduit", "machine"));
        Assert.Equal(("machine", Cell(1, 1)), Next(Cell(1, 1), "machine", "conduit"));
        // A machine's cell, asked for its conduit only, has none.
        Assert.Null(Next(Cell(2, 2), "conduit"));
        w.Do(new Deconstruct(Cell(0, 1), "structure")).Run();
        Assert.Equal(("back", Cell(0, 1)), Next(Cell(0, 1), "structure", "back"));
    }

    [Fact]
    public void LeavesTheGridRefusingAPlanInAMarkedSlotUntilTheThingIsGone()
    {
        var w = new Scenario(Room)
            .Do(new Deconstruct(Cell(3, 2), "structure"))
            .Do(new PlacePlan(Cell(3, 2), Wall))
            .Run();
        Assert.Equal(["grid.occupied"], Reasons(w));
    }

    [Fact]
    public void CancelsAPlanOrAMarkByIdAndRefusesOneThatIsNotThere()
    {
        var w = new Scenario(Open)
            .Do(new PlacePlan(At(1, 1), Wall))
            .Do(new Deconstruct(At(0, 0), "structure"))
            .Run()
            .Do(new CancelPlan(1))
            .Do(new CancelPlan(1))
            .Do(new CancelPlan(2))
            .Do(new CancelPlan(7))
            .Run();
        Assert.Equal(["building.noPlan", "building.noPlan"], Reasons(w));
        Assert.Empty(w.M.Building.Plans);
    }

    /// <summary>A placePlan (with a placement), a deconstruct (with a layer) or a cancelPlan (with an id).</summary>
    private sealed record Step(int Cell, Placement? Placement, string? Layer, int Id);

    /// <summary>Nine wide, with room for a tank in the open: cell (x, y) is y * 9 + x.</summary>
    private const string Crowded = """
        #########
        #TT.-...#
        #TT=H...#
        #.......#
        #########
        """;

    /// <summary>Cells -1 to 46, a third of them from a few, so plans clash as fast-check's small values make them.</summary>
    private static readonly Gen<int> Cells = Gen.Frequency(
        (2, Gen.Choose(-1, 46)),
        (1, Gen.Choose(-1, 4)),
        (1, Gen.Elements(At(1, 1), At(2, 2), At(4, 1), At(3, 2), At(4, 3))));

    [Property(MaxTest = 100)]
    public Property NeverLetsTwoPlansShareASlotOrTwoMarksOneThingAndNumbersPlansInOrder()
    {
        var placement = Gen.Elements(
            Wall, new Placement("structure", "floor"), new Placement("structure", "door"),
            Pipe, new Placement("conduit", "valve"), Tank, new Placement("machine", "vent"));
        var step = Gen.OneOf(
            from cell in Cells from p in placement select new Step(cell, p, null, 0),
            from cell in Cells from layer in Gen.Elements("structure", "conduit", "machine") select new Step(cell, null, layer, 0),
            from id in Gen.Frequency((2, Gen.Choose(0, 160)), (3, Gen.Choose(0, 8))) select new Step(0, null, null, id));
        var tick = from n in Gen.Choose(0, 4) from s in step.ArrayOf(n) select s;
        var ticks = from n in Gen.Choose(0, 40) from t in tick.ArrayOf(n) select t;
        return Prop.ForAll(ticks.ToArbitrary(), Holds);
    }

    /// <summary>Whether, after <paramref name="ticks"/> of steps, plans are numbered in order and claim each slot and thing once.</summary>
    private static bool Holds(Step[][] ticks)
    {
        var w = new Scenario(Crowded);
        foreach (var tick in ticks)
        {
            foreach (var s in tick)
            {
                if (s.Placement != null) w.Do(new PlacePlan(s.Cell, s.Placement));
                else if (s.Layer != null) w.Do(new Deconstruct(s.Cell, s.Layer));
                else w.Do(new CancelPlan(s.Id));
            }
            w.Run();
        }
        var plans = w.M.Building.Plans;
        var ids = plans.Select(p => p.Id).ToArray();
        if (!ids.Order().SequenceEqual(ids) || ids.Distinct().Count() != ids.Length) return false;

        var claimed = new List<string>();
        foreach (var p in plans)
        {
            if (p.Clear is { } layer)
            {
                // A machine's marks are told apart by the machine, not by the cell the module kept.
                int? machine = layer == "machine" ? w.M.Grid.MachineAt(p.Cell)?.Id : null;
                string key = $"mark:{layer}:{machine ?? p.Cell}";
                if (claimed.Contains(key)) return false;
                claimed.Add(key);
                continue;
            }
            string slot = p.Place!.Layer == "conduit" ? "conduit" : "solid";
            foreach (int c in FootprintOf(p))
            {
                string key = $"{slot}:{c}";
                if (claimed.Contains(key)) return false;
                claimed.Add(key);
            }
        }
        return true;
    }

    /// <summary>The cells a blueprint covers on the nine-wide map, worked out here, not by the module.</summary>
    private static IEnumerable<int> FootprintOf(Plan p)
    {
        if (p.Place is not { Layer: "machine" } place) return [p.Cell];
        var spec = Machines.Get(place.Kind);
        return Enumerable.Range(0, spec.Height).SelectMany(dy => Enumerable.Range(0, spec.Width).Select(dx => p.Cell + dy * 9 + dx));
    }
}
