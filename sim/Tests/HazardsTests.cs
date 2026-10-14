using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// packages/sim/src/modules/hazards/hazards.test.ts, ported as far as grid and rooms can show it; the
/// rest waits for atmosphere, building and items (see sim/Hazards/README.md).
/// </summary>
public class HazardsTests
{
    /// <summary>Nine wide: a sealed room with a vent above a tank.</summary>
    private const string Room = """
        #########
        #V......#
        #TT.....#
        #TT.....#
        #########
        """;

    /// <summary>"break a hull wall, so the room vents": the hole is the room's opening to space.</summary>
    [Fact]
    public void BreakAHullWallSoTheRoomOpensToSpace()
    {
        var w = new Scenario(Room);
        int room = w.M.Rooms.RoomAt(w.At(4, 2)) ?? throw new InvalidOperationException("room");
        Assert.Empty(w.M.Rooms.RoomById(room)!.Openings);
        w.Do(new MeteorHit(w.At(8, 2))).Run();
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(w.At(8, 2)));
        Assert.Equal(room, w.M.Rooms.RoomAt(w.At(4, 2)));
        Assert.Equal(new[] { w.At(8, 2) }, w.M.Rooms.RoomById(room)!.Openings);
    }

    /// <summary>"stop venting once a wall planned on the hole is built", with the test builder for building.</summary>
    [Fact]
    public void CloseTheRoomAgainOnceAWallIsBuiltOnTheHole()
    {
        var w = new Scenario(Room);
        w.Do(new MeteorHit(w.At(8, 2))).Run();
        w.Do(new Place(w.At(8, 2), Placement.Wall())).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(w.At(8, 2)));
        Assert.Empty(Assert.Single(w.M.Rooms.All).Openings);
    }

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
