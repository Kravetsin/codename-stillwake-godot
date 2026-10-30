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
    private static (int, int)[] Summary(Scenario w) => w.M.Rooms.All.Select(r => (r.Id, r.Volume)).ToArray();

    [Fact]
    public void TakesTheInnerLayerOfAWallTwoCellsThickIntoTheHullAsAWallOfOneButNoneOfAWallOfThree()
    {
        // A 16 px wall is two 8 px cells (0104): its inner layer is hull, as a one-cell wall is; past a wall of three, nothing.
        // The partition inside, beside the outer wall, has no outside beyond it away from either room.
        var w = new Scenario("""
            ..........
            .########.
            .##..#..#.
            .##..#..#.
            .########.
            ..........
            """, cells: true);
        Assert.Equal(2, w.M.Rooms.All.Count);
        var left = w.M.Rooms.All[0];
        Assert.Equal(
            new[] { w.At(3, 1), w.At(4, 1), w.At(2, 2), w.At(2, 3), w.At(3, 4), w.At(4, 4) },
            left.Hull);
        var right = w.M.Rooms.All[1];
        Assert.Equal(new[] { w.At(6, 1), w.At(7, 1), w.At(8, 2), w.At(8, 3), w.At(6, 4), w.At(7, 4) }, right.Hull);
        var thick = new Scenario("""
            .........
            .#######.
            .#######.
            .#######.
            .###.###.
            .#######.
            .#######.
            .#######.
            .........
            """, cells: true);
        Assert.Empty(Assert.Single(thick.M.Rooms.All).Hull);
    }

    [Fact]
    public void FindsASealedRoomWithTheHullItLeaksThroughAndNoOpenings()
    {
        var w = new Scenario("""
            .......
            .#####.
            .#...#.
            .#####.
            .......
            """, cells: true);
        var room = Assert.Single(w.M.Rooms.All);
        Assert.Equal(w.At(2, 2), room.Id);
        Assert.Equal(3, room.Volume);
        Assert.Equal(new[] { w.At(2, 1), w.At(3, 1), w.At(4, 1), w.At(1, 2), w.At(5, 2), w.At(2, 3), w.At(3, 3), w.At(4, 3) }, room.Hull);
        Assert.Empty(room.Openings);
        Assert.Equal(w.At(2, 2), w.M.Rooms.RoomAt(w.At(3, 2)));
        Assert.Equal(RoomsModule.Space, w.M.Rooms.RoomAt(w.At(0, 0)));
        Assert.Null(w.M.Rooms.RoomAt(w.At(1, 1)));
        Assert.Null(w.M.Rooms.RoomAt(-1));
    }

    /// <summary>roomOfCells, which atmosphere walks: every cell at once, as RoomAt gives them, solid ones negative too.</summary>
    [Fact]
    public void GivesEveryCellsRoomAtOnceSpaceAndSolidAsNegatives()
    {
        var w = new Scenario("""
            .........
            .#######.
            .#..D..#.
            .#######.
            .........
            """, cells: true);
        var all = w.M.Rooms.RoomOfCells.ToArray();
        Assert.Equal(9 * 5, all.Length);
        for (int cell = 0; cell < all.Length; cell++) Assert.Equal(w.M.Rooms.RoomAt(cell) ?? RoomsModule.Solid, all[cell]);
        Assert.Equal(new[] { RoomsModule.Space, RoomsModule.Solid, w.At(2, 2), RoomsModule.Solid, w.At(5, 2) },
            new[] { all[0], all[w.At(1, 1)], all[w.At(3, 2)], all[w.At(4, 2)], all[w.At(6, 2)] });
        w.Do(new SetDoor(w.At(4, 2), true)).Run();
        Assert.Equal(w.At(2, 2), w.M.Rooms.RoomOfCells[w.At(6, 2)]);
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
            """, cells: true);
        Assert.Equal(new[] { (w.At(2, 2), 2), (w.At(5, 2), 2) }, Summary(w));
        w.Do(new SetDoor(w.At(4, 2), true)).Run();
        Assert.Equal(new[] { (w.At(2, 2), 5) }, Summary(w));
        w.Do(new SetDoor(w.At(4, 2), false)).Run();
        Assert.Equal(new[] { (w.At(2, 2), 2), (w.At(5, 2), 2) }, Summary(w));
    }

    [Fact]
    public void AShutHatchSealsTwoRoomsApartAndAnOpenOneJoinsThem()
    {
        var w = new Scenario("""
            ........
            .######.
            .#....#.
            .##hh##.
            .#....#.
            .######.
            ........
            """, cells: true);
        Assert.Equal(new[] { (w.At(2, 2), 4), (w.At(2, 4), 4) }, Summary(w));
        w.Do(new SetDoor(w.At(3, 3), true)).Run();
        Assert.Equal(new[] { (w.At(2, 2), 10) }, Summary(w));
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
            """, cells: true);
        var room = Assert.Single(w.M.Rooms.All);
        Assert.Equal(4, room.Volume);
        Assert.Equal(new[] { w.At(5, 2) }, room.Openings);
        Assert.Equal(RoomsModule.Space, w.M.Rooms.RoomAt(w.At(6, 2)));
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
            """, cells: true);
        Assert.Empty(w.M.Rooms.All);
        Assert.Equal(RoomsModule.Space, w.M.Rooms.RoomAt(w.At(3, 2)));
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
        Assert.Equal(new[] { (w.At(2, 2), 3 * 4), (w.At(4, 2), 12 * 4) }, Summary(w));
    }

    [Fact]
    public void OpensARoomBehindWhereACellHasNoBackWallAndClosesItOnceOneIsBuilt()
    {
        var w = new Scenario("""
            #######
            #.. ..#
            #.....#
            #######
            """, cells: true);
        Assert.Equal(w.At(1, 1), w.M.Rooms.RoomAt(w.At(3, 1)));
        Assert.Equal(new[] { w.At(3, 1) }, w.M.Rooms.RoomById(w.At(1, 1))!.Openings);
        w.Do(new Place(w.At(3, 1), Placement.BackWall())).Run();
        Assert.Empty(w.M.Rooms.RoomById(w.At(1, 1))!.Openings);
        w.Do(new ClearAt(w.At(5, 2), "back")).Run();
        Assert.Equal(new[] { w.At(5, 2) }, w.M.Rooms.RoomById(w.At(1, 1))!.Openings);
    }

    [Fact]
    public void FindsTheWrecksRoomsAsTheTypeScriptSimDoes()
    {
        var w = new Scenario(Layout.Parse(Wreck.Rows, Wreck.Under), doubled: true);
        // As packages/sim/src/modules/wreck.test.ts expects: A and the airlock, named by their first cells; torn B is space.
        Assert.Equal(new[] { w.At(46, 19), w.At(55, 20) }, w.M.Rooms.All.Select(r => r.Id));
        Assert.Equal(RoomsModule.Space, w.M.Rooms.RoomAt(w.At(41, 21)));
        Assert.Equal(new[] { "solarPanel", "lamp", "lifepod", "tradeStation", "generator", "vent" }, w.M.Grid.Machines.Select(m => m.Kind));
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
                if (w.M.Grid.StructureAt(cell) is Structure.DoorClosed or Structure.DoorOpen)
                    w.Do(new SetDoor(cell, w.M.Grid.StructureAt(cell) == Structure.DoorClosed)).Run();
            foreach (int cell in t.backs)
                w.Do(w.M.Grid.BackLayer[cell] == Back.Wall ? new ClearAt(cell, "back") : new Place(cell, Placement.BackWall())).Run();

            bool Solid(int c) => w.M.Grid.StructureAt(c) is Structure.Wall or Structure.DoorClosed;
            var counts = new SortedDictionary<int, int>();
            for (int cell = 0; cell < size; cell++)
            {
                var at = w.M.Rooms.RoomAt(cell);
                if ((at == null) != Solid(cell)) return false;
                if (at is not >= 0) continue;
                if (!counts.ContainsKey(at.Value) && cell != at) return false;
                counts[at.Value] = counts.GetValueOrDefault(at.Value) + 1;
                var room = w.M.Rooms.RoomById(at.Value)!;
                if (w.M.Grid.BackLayer[cell] != Back.Wall && !room.Openings.Contains(cell)) return false;
                int x = cell % t.width;
                foreach (int n in new[] { x > 0 ? cell - 1 : -1, x < t.width - 1 ? cell + 1 : -1, cell - t.width, cell + t.width })
                {
                    if (n < 0 || n >= size || Solid(n)) continue;
                    var there = w.M.Rooms.RoomAt(n);
                    if (there == RoomsModule.Space ? !room.Openings.Contains(cell) : there != at) return false;
                }
            }
            var found = w.M.Rooms.All.Select(r => (r.Id, r.Volume)).ToArray();
            if (!found.SequenceEqual(counts.Select(kv => (kv.Key, kv.Value)))) return false;
            var before = w.M.Rooms.All.Select(r => (r.Id, r.Volume, string.Join(",", r.Hull), string.Join(",", r.Openings))).ToArray();
            w.Reload();
            return before.SequenceEqual(w.M.Rooms.All.Select(r => (r.Id, r.Volume, string.Join(",", r.Hull), string.Join(",", r.Openings))));
        });
    }

    /// <summary>
    /// A breach beside space floods only the ship's box, not the world around it, and names the rooms it
    /// changed: the room breached, under its new id, and no other.
    /// </summary>
    [Fact]
    public void FindsABreachBesideSpaceWithinTheShipAndNamesOnlyTheRoomBreached()
    {
        var w = new Scenario(Layout.Parse([
            "                    ",
            "  #######           ",
            "  #..#..#           ",
            "  #######           ",
            "                    ",
            "                    ",
        ]));
        Assert.Equal(new Box(2, 1, 8, 3), w.M.Grid.Built);
        int version = w.M.Rooms.Version;
        w.Do(new ClearAt(w.At(3, 1), "structure")).Run();
        Assert.Equal(new[] { w.At(3, 1), w.At(3, 2) }, w.M.Rooms.ChangedSince(version));
        Assert.Equal(new[] { (w.At(3, 1), 3), (w.At(6, 2), 2) }, Summary(w));
        Assert.Equal(new[] { w.At(3, 1) }, w.M.Rooms.RoomById(w.At(3, 1))!.Openings);
    }

    /// <summary>
    /// A back wall taken from behind a room's hole on the tick another room is breached: the room keeps its
    /// cells, hull and openings, but is named, as what its air leaks through changed.
    /// </summary>
    [Fact]
    public void NamesARoomWhoseBackWallChangedOnTheTickOfABreach()
    {
        var w = new Scenario(Layout.Parse([
            "                    ",
            "  #.#####           ",
            "  #..#..#           ",
            "  #######           ",
            "                    ",
        ]));
        var leaks = w.M.Atmosphere.RoomsAir().Select(r => (r.Room, r.Leak)).ToList();
        var hole = w.M.Rooms.RoomById(w.At(3, 1))!;
        int version = w.M.Rooms.Version;
        w.Do(new ClearAt(w.At(3, 1), "back")).Do(new ClearAt(w.At(6, 1), "structure")).Run();
        Assert.Equal(new[] { w.At(3, 1), w.At(6, 1), w.At(6, 2) }, w.M.Rooms.ChangedSince(version));
        Assert.Equal(Of(hole), Of(w.M.Rooms.RoomById(w.At(3, 1))!));
        var now = w.M.Atmosphere.RoomsAir().Select(r => (r.Room, r.Leak)).ToList();
        Assert.NotEqual(leaks[0], now[0]);
        Assert.Equal(now, w.Reload().M.Atmosphere.RoomsAir().Select(r => (r.Room, r.Leak)));
    }

    /// <summary>A room built out in space grows the ship's box, and is found in it; taken apart, the box shrinks back.</summary>
    [Fact]
    public void FindsARoomBuiltOutInSpaceAndForgetsItWhenTakenApart()
    {
        var w = new Scenario(Layout.Parse([
            "                    ",
            "  #####             ",
            "  #...#             ",
            "  #####             ",
            "                    ",
            "                    ",
        ]));
        var ring = new[] { (14, 2), (15, 2), (16, 2), (14, 3), (16, 3), (14, 4), (15, 4), (16, 4) }.Select(p => w.At(p.Item1, p.Item2)).ToList();
        foreach (int cell in ring) w.Do(new Place(cell, Placement.Wall()));
        w.Run();
        Assert.Equal(new Box(2, 1, 16, 4), w.M.Grid.Built);
        Assert.Equal(new[] { (w.At(3, 2), 3), (w.At(15, 3), 1) }, Summary(w));
        foreach (int cell in ring) w.Do(new ClearAt(cell, "structure"));
        w.Run();
        Assert.Equal(new Box(2, 1, 6, 3), w.M.Grid.Built);
        Assert.Equal(new[] { (w.At(3, 2), 3) }, Summary(w));
        Assert.Equal(RoomsModule.Space, w.M.Rooms.RoomAt(w.At(15, 3)));
    }

    private static readonly char[] Inside = ['.', '.', '#', '#', '=', 'H', 'D', 'd'];

    /// <summary>A hull <paramref name="width"/> by <paramref name="height"/> filled with <paramref name="inside"/>, in <paramref name="margin"/> cells of space.</summary>
    private static string[] Ship(int width, int height, int margin, char[] inside)
    {
        var rows = new List<string>();
        string space = new(' ', width + 2 * margin);
        for (int i = 0; i < margin; i++) rows.Add(space);
        for (int y = 0; y < height; y++)
        {
            var row = y == 0 || y == height - 1 ? new string('#', width)
                : "#" + new string(inside, y * width + 1, width - 2) + "#";
            rows.Add(new string(' ', margin) + row + new string(' ', margin));
        }
        for (int i = 0; i < margin; i++) rows.Add(space);
        return [.. rows];
    }

    /// <summary>
    /// An edit drawn for a cell: built on if empty, toggled or taken apart if a door, taken apart otherwise;
    /// its back wall built or taken; or the cell left empty, so what is built shrinks.
    /// </summary>
    private static Command[] Edit(GridModule grid, int cell, int kind)
    {
        if (kind >= 10) return [new ClearAt(cell, "structure"), new ClearAt(cell, "back")];
        if (kind >= 7) return [grid.BackLayer[cell] == Back.Wall ? new ClearAt(cell, "back") : new Place(cell, Placement.BackWall())];
        int structure = grid.StructureAt(cell);
        if (structure == Structure.Empty)
            return [new Place(cell, (kind % 4) switch
            {
                0 => Placement.Wall(),
                1 => new Placement("structure", "door"),
                2 => new Placement("structure", "floor"),
                _ => new Placement("structure", "ladder"),
            })];
        if (structure is Structure.DoorClosed or Structure.DoorOpen && kind < 4) return [new SetDoor(cell, structure == Structure.DoorClosed)];
        return [new ClearAt(cell, "structure")];
    }

    private static string Of(Room r) => $"{r.Id}:{r.Volume}:{string.Join(",", r.Hull)}:{string.Join(",", r.Openings)}";

    /// <summary>
    /// The rooms found around each change are those found from scratch, whatever is built, taken apart
    /// or opened, beside space and the grid's edge or far from both, rooms joined or split, the built box
    /// grown by building out in space and shrunk by clearing its edge; and every room that differs from
    /// the tick before is among those the rooms say changed. A tick that writes more than a small log
    /// holds leaves the rooms behind it, and they are found again from scratch.
    /// </summary>
    [Property(MaxTest = 400)]
    public Property FindsTheSameRoomsAroundAChangeAsFromScratch()
    {
        var ships =
            from width in Gen.Choose(3, 20)
            from height in Gen.Choose(3, 16)
            from margin in Gen.Choose(0, 5)
            from inside in Gen.Elements(Inside).ArrayOf(width * height)
            select Ship(width, height, margin, inside);
        var anything =
            from width in Gen.Choose(1, 14)
            from height in Gen.Choose(1, 12)
            from cells in Gen.Elements(Chars).ArrayOf(width * height)
            select Enumerable.Range(0, height).Select(y => new string(cells, y * width, width)).ToArray();
        var edit =
            from cell in Gen.Choose(0, 1 << 20)
            from kind in Gen.Choose(0, 11)
            from together in Gen.Frequency((3, Gen.Constant(false)), (1, Gen.Constant(true)))
            select (cell, kind, together);
        var gen =
            from rows in Gen.Frequency((3, ships), (1, anything))
            from edits in edit.ListOf()
            from capacity in Gen.Frequency((3, Gen.Constant(1 << 16)), (1, Gen.Elements(1, 2, 4)))
            select (rows, edits: edits.Take(60).ToList(), capacity);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var layout = Layout.Parse(t.rows);
            var grid = new GridModule { LogCapacity = t.capacity };
            var rooms = new RoomsModule(grid);
            var world = World.Create(1, layout, [grid, rooms, new Builder(grid, grid)]);
            int size = layout.Width * layout.Height;
            if (size == 0) return true.Label("no cells");
            var pending = new List<Command>();
            foreach (var (cell, kind, together) in t.edits.Append((0, 0, false)))
            {
                if (!together && pending.Count > 0)
                {
                    var before = rooms.All.Select(Of).ToList();
                    int version = rooms.Version;
                    world.Step(pending);
                    pending.Clear();
                    var broken = Differs(grid, rooms, before, version);
                    if (broken != null) return false.Label(broken);
                }
                pending.AddRange(Edit(grid, cell % size, kind));
            }
            return true.ToProperty();
        });
    }

    /// <summary>What differs between the rooms kept and those found from scratch, or the rooms said changed and those that did; null for nothing.</summary>
    private static string? Differs(GridModule grid, RoomsModule rooms, List<string> before, int version)
    {
        var (roomOf, fresh) = RoomsModule.Find(grid.Width, grid.Height, grid.StructureLayer, grid.BackLayer);
        for (int c = 0; c < roomOf.Length; c++)
            if (roomOf[c] != rooms.RoomOfCells[c]) return $"cell {c} is in {rooms.RoomOfCells[c]}, not {roomOf[c]}";
        var now = rooms.All.Select(Of).ToList();
        if (!now.SequenceEqual(fresh.Select(Of))) return $"rooms [{string.Join(" ", now)}], not [{string.Join(" ", fresh.Select(Of))}]";
        var changed = rooms.ChangedSince(version);
        if (changed == null) return null;
        if (!changed.SequenceEqual(changed.Distinct().Order())) return "the changed ids are not ascending and distinct";
        foreach (var r in before.Except(now).Concat(now.Except(before)))
            if (!changed.Contains(int.Parse(r[..r.IndexOf(':')]))) return $"room {r} changed unsaid";
        return null;
    }
}
