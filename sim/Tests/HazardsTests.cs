using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// packages/sim/src/modules/hazards/hazards.test.ts, ported one for one.
/// </summary>
public class HazardsTests
{
    /// <summary>Nine wide: a sealed room with a vent above a tank and a valve between them.</summary>
    private const string Room = """
        #########
        #V......#
        #TT.....#
        #TT.....#
        #########
        """;

    /// <summary><see cref="Room"/> in creative mode, piped through a valve, its tank loaded and the room aired.</summary>
    private static (Scenario W, int Tank, int Room) Aired()
    {
        var w = new Scenario(Room).Do(new Items.SetCreative(true));
        w.Do(new Place(w.At(1, 1), new Placement("conduit", "pipe")))
            .Do(new Place(w.At(1, 2), new Placement("conduit", "valve")))
            .Do(new Place(w.At(1, 3), new Placement("conduit", "pipe")))
            .Run();
        int tank = w.M.Grid.Machines.FirstOrDefault(m => m.Kind == "tank")?.Id ?? 0;
        for (int i = 0; i < 3; i++) w.Do(new LoadCanister(tank));
        w.Run();
        int room = w.M.Rooms.RoomAt(w.At(4, 2)) ?? throw new InvalidOperationException("room");
        for (int i = 0; i < 1000 && w.M.Atmosphere.StatusOf(room) != RoomStatus.Aired; i++) w.Run();
        Assert.Equal(RoomStatus.Aired, w.M.Atmosphere.StatusOf(room));
        return (w, tank, room);
    }

    [Fact]
    public void BreakAHullWallSoTheRoomVentsAndDrainsItsNetwork()
    {
        var (w, tank, room) = Aired();
        Assert.Empty(w.M.Rooms.RoomById(room)!.Openings);
        int before = w.M.Atmosphere.OxygenIn(tank);
        w.Do(new MeteorHit(w.At(8, 2))).Run();
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(w.At(8, 2)));
        Assert.Equal(room, w.M.Rooms.RoomAt(w.At(4, 2)));
        Assert.Equal(new[] { w.At(8, 2) }, w.M.Rooms.RoomById(room)!.Openings);
        Assert.Equal(RoomStatus.Venting, w.M.Atmosphere.StatusOf(room));
        Assert.Equal(before - Content.Air.VentDrain, w.M.Atmosphere.OxygenIn(tank));
    }

    [Fact]
    public void StopDrainingTheNetworkOnceAValveCutsTheVentOff()
    {
        var (w, tank, _) = Aired();
        w.Do(new MeteorHit(w.At(8, 2))).Run(2);
        w.Do(new ToggleValve(w.At(1, 2))).Run();
        int cut = w.M.Atmosphere.OxygenIn(tank);
        w.Run(10);
        Assert.Equal(cut, w.M.Atmosphere.OxygenIn(tank));
    }

    /// <summary>"stop venting once a wall planned on the hole is built", in creative mode as there.</summary>
    [Fact]
    public void StopVentingOnceAWallPlannedOnTheHoleIsBuilt()
    {
        var (w, tank, room) = Aired();
        w.Do(new SetCreative(true)).Run();
        w.Do(new MeteorHit(w.At(8, 2))).Run();
        w.Do(new PlacePlan(w.At(8, 2), Placement.Wall("scrap"))).Run();
        int plan = w.M.Building.Plans[0].Id;
        for (int i = 0; i <= Content.Materials.Get("scrap").BuildTicks; i++) w.Do(new Work(plan)).Run();
        Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(w.At(8, 2)));
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

    /// <summary>Cell (x, y) of the nine-wide <see cref="Room"/>, before a scenario is there to ask.</summary>
    private static int RoomCell(int x, int y) => y * 9 + x;

    [Fact]
    public void BreakThePipeRunningThroughTheWallToo()
    {
        var w = new Scenario(Room);
        w.Do(new Place(w.At(0, 2), new Placement("conduit", "pipe"))).Run();
        Assert.Equal(Conduit.Pipe, w.M.Grid.ConduitAt(w.At(0, 2)));
        w.Do(new MeteorHit(w.At(0, 2))).Run();
        Assert.Equal(Conduit.None, w.M.Grid.ConduitAt(w.At(0, 2)));
    }

    [Fact]
    public void HitOnlyWallsNotAnOpenCellADoorOrACellOffTheGrid()
    {
        var w = new Scenario("#D#\n#.#\n###")
            .Do(new MeteorHit(4))
            .Do(new MeteorHit(1))
            .Do(new MeteorHit(-1))
            .Run();
        Assert.Equal(new[] { "hazards.noWall", "hazards.noWall", "hazards.noWall" }, w.Refusals.Select(r => r.Reason));
        Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(1));
    }

    /// <summary>grid.test.ts "reads pipes and valves": the query the pipe test above reads.</summary>
    [Fact]
    public void GridReadsPipesAndValves()
    {
        var w = new Scenario("-vx.");
        Assert.Equal(new[] { Conduit.Pipe, Conduit.ValveOpen, Conduit.ValveClosed, Conduit.None },
            Enumerable.Range(0, 4).Select(w.M.Grid.ConduitAt));
        Assert.Equal(Conduit.None, w.M.Grid.ConduitAt(-1));
        Assert.Equal(Conduit.None, w.M.Grid.ConduitAt(4));
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
            var conduit = Enumerable.Range(0, size).Select(grid.ConduitAt).ToArray();
            var back = grid.BackLayer.ToArray();
            var machines = grid.Machines.ToArray();
            foreach (int cell in t.hits) w.Do(new MeteorHit(cell)).Run();
            for (int c = 0; c < size; c++)
            {
                bool broken = structure[c] == Structure.Wall && t.hits.Contains(c);
                if (grid.StructureAt(c) != (broken ? Structure.Empty : structure[c])) return false;
                if (grid.ConduitAt(c) != (broken ? Conduit.None : conduit[c])) return false;
            }
            // Each wall breaks once, on its first hit; every other hit is refused.
            int refused = t.hits.Count - t.hits.Distinct().Count(c => c >= 0 && c < size && structure[c] == Structure.Wall);
            return grid.BackLayer.SequenceEqual(back) && grid.Machines.SequenceEqual(machines)
                && w.Refusals.Count == refused && w.Refusals.All(r => r.Reason == "hazards.noWall");
        });
    }
}
