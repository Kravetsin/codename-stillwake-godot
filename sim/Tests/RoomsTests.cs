using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/rooms/rooms.test.ts, ported.</summary>
public class RoomsTests
{
    private static (int, int)[] Summary(Scenario w) => w.Rooms.All.Select(r => (r.Id, r.Volume)).ToArray();

    [Fact]
    public void FindsASealedRoomWithTheHullItLeaksThroughAndNoOpenings()
    {
        var w = new Scenario("""
            .......
            .#####.
            .#...#.
            .#####.
            .......
            """);
        var room = Assert.Single(w.Rooms.All);
        Assert.Equal(w.At(2, 2), room.Id);
        Assert.Equal(3, room.Volume);
        Assert.Equal(new[] { w.At(2, 1), w.At(3, 1), w.At(4, 1), w.At(1, 2), w.At(5, 2), w.At(2, 3), w.At(3, 3), w.At(4, 3) }, room.Hull);
        Assert.Empty(room.Openings);
        Assert.Equal(w.At(2, 2), w.Rooms.RoomAt(w.At(3, 2)));
        Assert.Equal(RoomsModule.Space, w.Rooms.RoomAt(w.At(0, 0)));
        Assert.Null(w.Rooms.RoomAt(w.At(1, 1)));
        Assert.Null(w.Rooms.RoomAt(-1));
    }

    [Fact]
    public void JoinsTwoRoomsThroughAnOpenDoorAndSplitsThemWhenItCloses()
    {
        var w = new Scenario("""
            .........
            .#######.
            .#..D..#.
            .#######.
            .........
            """);
        Assert.Equal(new[] { (w.At(2, 2), 2), (w.At(5, 2), 2) }, Summary(w));
        w.Do(new ToggleDoor(w.At(4, 2))).Run();
        Assert.Equal(new[] { (w.At(2, 2), 5) }, Summary(w));
        w.Do(new ToggleDoor(w.At(4, 2))).Run();
        Assert.Equal(new[] { (w.At(2, 2), 2), (w.At(5, 2), 2) }, Summary(w));
    }

    [Fact]
    public void KeepsARoomWithAHoleInItsWallARoomOpenToSpaceThroughThatCell()
    {
        var w = new Scenario("""
            .......
            .#####.
            .#.....
            .#####.
            .......
            """);
        var room = Assert.Single(w.Rooms.All);
        Assert.Equal(4, room.Volume);
        Assert.Equal(new[] { w.At(5, 2) }, room.Openings);
        Assert.Equal(RoomsModule.Space, w.Rooms.RoomAt(w.At(6, 2)));
    }

    [Fact]
    public void LetsARoomFloodIntoSpaceThroughAHoleTwoCellsWide()
    {
        var w = new Scenario("""
            .......
            .#####.
            .#.....
            .#.....
            .#####.
            .......
            """);
        Assert.Empty(w.Rooms.All);
        Assert.Equal(RoomsModule.Space, w.Rooms.RoomAt(w.At(3, 2)));
    }

    [Fact]
    public void FindsASealedClosetAndKeepsFloorsLaddersAndMachinesInside()
    {
        var w = new Scenario("""
            ..........
            .########.
            .#.#=HV.#.
            .#.#TT..#.
            .#.#TT..#.
            .########.
            ..........
            """);
        Assert.Equal(new[] { (w.At(2, 2), 3), (w.At(4, 2), 12) }, Summary(w));
    }

    [Fact]
    public void OpensARoomBehindWhereACellHasNoBackWallAndClosesItOnceOneIsBuilt()
    {
        var w = new Scenario("""
            #######
            #.. ..#
            #.....#
            #######
            """);
        Assert.Equal(w.At(1, 1), w.Rooms.RoomAt(w.At(3, 1)));
        Assert.Equal(new[] { w.At(3, 1) }, w.Rooms.RoomById(w.At(1, 1))!.Openings);
        w.Do(new Place(w.At(3, 1), Placement.BackWall())).Run();
        Assert.Empty(w.Rooms.RoomById(w.At(1, 1))!.Openings);
        w.Do(new ClearAt(w.At(5, 2), "back")).Run();
        Assert.Equal(new[] { w.At(5, 2) }, w.Rooms.RoomById(w.At(1, 1))!.Openings);
    }

    [Fact]
    public void FindsTheWrecksRoomsAsTheTypeScriptSimDoes()
    {
        var w = new Scenario(Layout.Parse(Wreck.Rows, Wreck.Under));
        // As packages/sim/src/modules/wreck.test.ts expects: A and the airlock; torn B is space.
        Assert.Equal(new[] { w.At(46, 19), w.At(55, 20) }, w.Rooms.All.Select(r => r.Id));
        Assert.Equal(RoomsModule.Space, w.Rooms.RoomAt(w.At(41, 21)));
        Assert.Equal(new[] { "solarPanel", "lamp", "analyzer", "generator", "vent" }, w.Grid.Machines.Select(m => m.Kind));
    }

    private static readonly char[] Chars = [' ', '.', '#', '#', '#', '=', 'H', 'D', 'd'];

    /// <summary>
    /// Every open cell is in one closed room or in space, whatever doors and back walls do, and a load
    /// finds the same: the invariant test of rooms.test.ts.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property PutsEveryOpenCellInOneClosedRoomOrSpaceAndFindsTheSameAfterALoad()
    {
        var gen =
            from width in Gen.Choose(1, 12)
            from height in Gen.Choose(1, 10)
            from cells in Gen.Elements(Chars).ArrayOf(width * height)
            from toggles in Gen.Choose(0, width * height - 1).ListOf()
            from backs in Gen.Choose(0, width * height - 1).ListOf()
            select (width, height, cells, toggles: toggles.Take(8).ToList(), backs: backs.Take(8).ToList());
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var rows = Enumerable.Range(0, t.height).Select(y => new string(t.cells, y * t.width, t.width)).ToArray();
            var w = new Scenario(Layout.Parse(rows));
            int size = t.width * t.height;
            foreach (int cell in t.toggles)
                if (w.Grid.StructureAt(cell) is Structure.DoorClosed or Structure.DoorOpen)
                    w.Do(new ToggleDoor(cell)).Run();
            foreach (int cell in t.backs)
                w.Do(w.Grid.BackLayer[cell] == Back.Wall ? new ClearAt(cell, "back") : new Place(cell, Placement.BackWall())).Run();

            bool Solid(int c) => w.Grid.StructureAt(c) is Structure.Wall or Structure.DoorClosed;
            var counts = new SortedDictionary<int, int>();
            for (int cell = 0; cell < size; cell++)
            {
                var at = w.Rooms.RoomAt(cell);
                if ((at == null) != Solid(cell)) return false;
                if (at is not >= 0) continue;
                if (!counts.ContainsKey(at.Value) && cell != at) return false;
                counts[at.Value] = counts.GetValueOrDefault(at.Value) + 1;
                var room = w.Rooms.RoomById(at.Value)!;
                if (w.Grid.BackLayer[cell] != Back.Wall && !room.Openings.Contains(cell)) return false;
                int x = cell % t.width;
                foreach (int n in new[] { x > 0 ? cell - 1 : -1, x < t.width - 1 ? cell + 1 : -1, cell - t.width, cell + t.width })
                {
                    if (n < 0 || n >= size || Solid(n)) continue;
                    var there = w.Rooms.RoomAt(n);
                    if (there == RoomsModule.Space ? !room.Openings.Contains(cell) : there != at) return false;
                }
            }
            var found = w.Rooms.All.Select(r => (r.Id, r.Volume)).ToArray();
            if (!found.SequenceEqual(counts.Select(kv => (kv.Key, kv.Value)))) return false;
            var before = w.Rooms.All.Select(r => (r.Id, r.Volume, string.Join(",", r.Hull), string.Join(",", r.Openings))).ToArray();
            w.Reload();
            return before.SequenceEqual(w.Rooms.All.Select(r => (r.Id, r.Volume, string.Join(",", r.Hull), string.Join(",", r.Openings))));
        });
    }
}
