using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Xunit;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Sim.Tests;

/// <summary>
/// packages/sim/src/modules/hazards/hazards.test.ts, ported one for one.
/// </summary>
public partial class HazardsTests
{
    /// <summary>Nine wide: a sealed room with a vent above a tank, the vaporizer <see cref="Aired"/> makes it, and a valve between them.</summary>
    private const string Room = """
        #########
        #V......#
        #TT.....#
        #TT.....#
        #########
        """;

    /// <summary>
    /// <see cref="Room"/> made the chain (<see cref="HandPiped.Chained"/>) in creative mode, its vent piped through a valve to
    /// the vaporizer's output, the tank under the hull loaded, the room aired and the vaporizer's store full, so a drain is
    /// paid in full.
    /// </summary>
    private static (Scenario W, int Tank, int Room) Aired()
    {
        var w = new Scenario(HandPiped.Chained(Room), doubled: true).Do(new Items.SetCreative(true));
        // From the vent's port, its top-left, through a valve on the vaporizer's top-left to its output, the right of its top row.
        w.Pipe(w.At(1, 1), w.At(1, 2), w.At(1, 2) + 3).Do(new MakeValve(w.At(1, 2), true)).Run();
        int tank = w.M.Grid.Machines.FirstOrDefault(m => m.Kind == "tank")?.Id ?? 0;
        int vaporizer = w.M.Grid.Machines.FirstOrDefault(m => m.Kind == "generator")?.Id ?? 0;
        for (int i = 0; i < 3; i++) w.Do(new LoadCanister(tank));
        w.Run();
        int room = w.M.Rooms.RoomAt(w.At(4, 2)) ?? throw new InvalidOperationException("room");
        for (int i = 0; i < 1000 && w.M.Atmosphere.StatusOf(room) != RoomStatus.Aired; i++) w.Run();
        Assert.Equal(RoomStatus.Aired, w.M.Atmosphere.StatusOf(room));
        for (int i = 0; i < 1000 && w.M.Atmosphere.OxygenIn(vaporizer) < Content.Air.GeneratorStore; i++) w.Run();
        Assert.Equal(Content.Air.GeneratorStore, w.M.Atmosphere.OxygenIn(vaporizer));
        return (w, tank, room);
    }

    /// <summary>Meteors on both cells of the hull's thickness at the drawn cell (8, 2): a breach (0104).</summary>
    private static Scenario Breach(Scenario w) => w.Do(new MeteorHit(w.At(8, 2))).Do(new MeteorHit(w.At(8, 2) + 1));

    [Fact]
    public void BreakAHullWallSoTheRoomVentsAndDrainsItsNetwork()
    {
        var (w, _, room) = Aired();
        Assert.Empty(w.M.Rooms.RoomById(room)!.Openings);
        int before = HandPiped.Held(w);
        Breach(w).Run();
        Assert.Equal((Structure.Empty, Structure.Empty), (w.M.Grid.StructureAt(w.At(8, 2)), w.M.Grid.StructureAt(w.At(8, 2) + 1)));
        Assert.Equal(room, w.M.Rooms.RoomAt(w.At(4, 2)));
        Assert.Equal(new[] { w.At(8, 2) + 1 }, w.M.Rooms.RoomById(room)!.Openings);
        Assert.Equal(RoomStatus.Venting, w.M.Atmosphere.StatusOf(room));
        Assert.Equal(before - Content.Air.VentDrain, HandPiped.Held(w));
    }

    [Fact]
    public void StopDrainingTheNetworkOnceAValveCutsTheVentOff()
    {
        var (w, _, _) = Aired();
        Breach(w).Run(2);
        w.Do(new SetValve(w.At(1, 2), false)).Run();
        // The vaporizer refills its store from the tank behind the valve: the chain holds what it held.
        int cut = HandPiped.Held(w);
        w.Run(10);
        Assert.Equal(cut, HandPiped.Held(w));
    }

    /// <summary>"stop venting once a wall planned on the hole is built", in creative mode as there.</summary>
    [Fact]
    public void StopVentingOnceAWallPlannedOnTheHoleIsBuilt()
    {
        var (w, tank, room) = Aired();
        w.Do(new SetCreative(true)).Run();
        Breach(w).Run();
        Assert.Empty(w.M.Building.Plans);
        w.Do(new PlacePlan(w.At(8, 2), Placement.Wall("scrap"))).Do(new PlacePlan(w.At(8, 2) + 1, Placement.Wall("scrap"))).Run();
        foreach (int plan in w.M.Building.Plans.Select(p => p.Id).ToArray())
            for (int i = 0; i <= Content.Materials.Get("scrap").BuildTicks; i++) w.Do(new Work(plan)).Run();
        Assert.Equal((Structure.Wall, Structure.Wall), (w.M.Grid.StructureAt(w.At(8, 2)), w.M.Grid.StructureAt(w.At(8, 2) + 1)));
        Assert.Empty(Assert.Single(w.M.Rooms.All).Openings);
        // Built at once, the tank is far from dry; a fresh canister fills the room again all the same.
        w.Do(new LoadCanister(tank)).Run();
        Assert.Equal(RoomStatus.Filling, w.M.Atmosphere.StatusOf(room));
    }

    [Fact]
    public void TakeADeconstructionMarkWithTheWallTheyBreak()
    {
        var w = new Scenario(Room).Do(new Deconstruct(RoomCell(8, 2), "structure")).Run();
        w.Do(new MeteorHit(w.At(8, 2))).Run();
        Assert.Empty(w.M.Building.Plans);
    }

    [Fact]
    public void LeaveNothingToRefundForAMarkWorkedOnTheTickItsWallBreaks()
    {
        var w = new Scenario(Room).Do(new Deconstruct(RoomCell(8, 2), "structure")).Run();
        int plan = w.M.Building.Plans[0].Id;
        var held = w.M.Items.Inventory(0).ToArray();
        w.Do(new MeteorHit(w.At(8, 2))).Do(new Work(plan)).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Building.Plans);
        Assert.Equal(held, w.M.Items.Inventory(0));
    }

    /// <summary>Cell (x, y) of the nine-wide <see cref="Room"/> as drawn, doubled, before a scenario is there to ask.</summary>
    private static int RoomCell(int x, int y) => AirTesting.Cell(9, x, y);

    [Fact]
    public void LeaveAPipeRunningThroughTheWallWhole()
    {
        var w = new Scenario(Room).Do(new Items.SetCreative(true));
        w.Pipe(w.At(0, 1), w.At(0, 3)).Run();
        w.Do(new MeteorHit(w.At(0, 2))).Run();
        Assert.Null(w.M.Grid.WallAt(w.At(0, 2)));
        Assert.Single(w.M.Pipes.Spans);
    }

    [Fact]
    public void HitOnlyWallsNotAnOpenCellADoorOrACellOffTheGrid()
    {
        var w = new Scenario("#D#\n#.#\n###", cells: true)
            .Do(new MeteorHit(4))
            .Do(new MeteorHit(1))
            .Do(new MeteorHit(-1))
            .Run();
        Assert.Equal(new[] { "hazards.noWall", "hazards.noWall", "hazards.noWall" }, w.Refusals.Select(r => r.Reason));
        Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(1));
    }

    private static readonly char[] Chars = [' ', '.', '#', '#', '#', '=', 'D', 'd', '-', 'v', 'x'];

    /// <summary>The README's invariant: a meteor only ever turns a wall into an empty cell, adding nothing.</summary>
    [Property(MaxTest = 200)]
    public Property OnlyEverTurnAWallIntoAnEmptyCell()
    {
        var gen =
            from width in Gen.Choose(1, 10)
            from height in Gen.Choose(1, 8)
            from cells in Gen.Elements(Chars).ArrayOf(width * height)
            from under in Gen.Elements(' ', '-').ArrayOf(width * height)
            from hits in Gen.Choose(-1, width * height).ListOf()
            select (width, height, cells, under, hits: hits.Take(12).ToList());
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            string[] Rows(char[] chars) =>
                Enumerable.Range(0, t.height).Select(y => new string(chars, y * t.width, t.width)).ToArray();
            var w = new Scenario(Layout.Parse(Rows(t.cells), Rows(t.under)));
            var grid = w.M.Grid;
            int size = t.width * t.height;
            var structure = grid.StructureLayer.ToArray();
            var spans = w.M.Pipes.Spans.ToArray();
            var back = grid.BackLayer.ToArray();
            var machines = grid.Machines.ToArray();
            foreach (int cell in t.hits) w.Do(new MeteorHit(cell)).Run();
            for (int c = 0; c < size; c++)
            {
                bool broken = structure[c] == Structure.Wall && t.hits.Contains(c);
                if (grid.StructureAt(c) != (broken ? Structure.Empty : structure[c])) return false;
            }
            // Each wall breaks once, on its first hit; every other hit is refused.
            int refused = t.hits.Count - t.hits.Distinct().Count(c => c >= 0 && c < size && structure[c] == Structure.Wall);
            return grid.BackLayer.SequenceEqual(back) && grid.Machines.SequenceEqual(machines) && w.M.Pipes.Spans.SequenceEqual(spans)
                && w.Refusals.Count == refused && w.Refusals.All(r => r.Reason == "hazards.noWall");
        });
    }

    [Fact]
    public void AMeteorBreaksNoBedrockNorAnythingInAPlanetsFogButSoilInTheZone()
    {
        // Three walls, a cell a character: soil in the fog, bedrock and soil in the zone (columns 1 to 2).
        var layout = Layout.Parse("###\n...").WithMaterials("sbs\n...") with { Zone = new Box(1, 0, 2, 1) };
        var w = new Scenario(layout, doubled: false);
        w.Do(new MeteorHit(0)).Do(new MeteorHit(1)).Do(new MeteorHit(2)).Do(new MeteorHit(3)).Run();
        Assert.Equal(["hazards.neverBreaks", "hazards.neverBreaks", "hazards.noWall"], w.Refusals.Select(r => r.Reason));
        Assert.Equal((Structure.Wall, Structure.Wall, Structure.Empty), (w.M.Grid.StructureAt(0), w.M.Grid.StructureAt(1), w.M.Grid.StructureAt(2)));
    }
}
