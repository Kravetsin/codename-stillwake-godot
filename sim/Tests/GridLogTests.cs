using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Rooms;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The grid's log of changed cells (0051): what it names, how far back it reaches, and readers that fall behind it.</summary>
public class GridLogTests
{
    private static readonly GridLayers[] Layers =
        [GridLayers.Structure, GridLayers.Material, GridLayers.Conduit, GridLayers.Back, GridLayers.BackMaterial, GridLayers.Machine, GridLayers.Shape];

    /// <summary>Every layer of every cell as it stands, a machine by its id.</summary>
    private static int[][] Snapshot(GridModule grid)
    {
        int size = grid.Width * grid.Height;
        return
        [
            grid.StructureLayer.ToArray(), grid.MaterialLayer.ToArray(), grid.ConduitLayer.ToArray(), grid.BackLayer.ToArray(),
            grid.BackMaterialLayer.ToArray(), [.. Enumerable.Range(0, size).Select(c => grid.MachineAt(c)?.Id ?? 0)], grid.ShapeLayer.ToArray(),
        ];
    }

    /// <summary>The cells where two snapshots differ, ascending, with the layers that do.</summary>
    private static List<CellChange> Between(int[][] was, int[][] now)
    {
        var changes = new List<CellChange>();
        for (int c = 0; c < was[0].Length; c++)
        {
            var layers = GridLayers.None;
            for (int k = 0; k < Layers.Length; k++)
                if (was[k][c] != now[k][c]) layers |= Layers[k];
            if (layers != GridLayers.None) changes.Add(new CellChange(c, layers));
        }
        return changes;
    }

    private static readonly string[] Machines = ["vent", "lamp", "tank", "furnace"];

    /// <summary>Anything a player or a hazard does to a cell: build in any layer, take apart, open and close; refused ones too.</summary>
    private static Command Edit(GridModule grid, int cell, int kind) => kind switch
    {
        0 => new Place(cell, Placement.Wall()),
        1 => new Place(cell, Placement.Wall("iron")),
        2 => new Place(cell, new Placement("structure", "door")),
        3 => new Place(cell, new Placement("structure", "floor")),
        4 => new Place(cell, new Placement("conduit", "pipe")),
        5 => new Place(cell, new Placement("conduit", "valve")),
        6 => new Place(cell, Placement.BackWall()),
        7 => new Place(cell, Placement.BackWall("iron")),
        8 => new Place(cell, new Placement("machine", Machines[cell % Machines.Length])),
        9 => new ToggleDoor(cell),
        10 => new ToggleValve(cell),
        11 => new ClearAt(cell, "structure"),
        12 => new ClearAt(cell, "conduit"),
        13 => new ClearAt(cell, "back"),
        _ => new ClearAt(cell, "machine"),
    };

    /// <summary>
    /// For every version the grid has had, what the log names since it is exactly the cells that differ
    /// between then and now, with the layers that do, ascending; a cell changed and changed back is not
    /// named. Null exactly when the log, whatever its size, has let go of a write made after that version.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property NamesTheCellsThatDifferSinceAnyVersionItStillReaches()
    {
        var gen =
            from width in Gen.Choose(1, 9)
            from height in Gen.Choose(1, 7)
            from cells in Gen.Elements(' ', ' ', '.', '#', '/', '\\', 'r', '7', 'D', 'd', '-', 'v', 'x', 'V').ArrayOf(width * height)
            from capacity in Gen.Elements(1, 2, 4, 8, 32, 1 << 16)
            from edits in (from cell in Gen.Choose(0, 1 << 20) from kind in Gen.Choose(0, 15) select (cell, kind)).ListOf()
            select (rows: Enumerable.Range(0, height).Select(y => new string(cells, y * width, width)).ToArray(), capacity, edits: edits.Take(50).ToList());
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var grid = new GridModule { LogCapacity = t.capacity };
            var world = World.Create(1, Layout.Parse(t.rows), [grid, new Builder(grid, grid)]);
            int size = grid.Width * grid.Height;
            if (size == 0) return true.Label("no cells");
            // Each version's layers, and the writes made by then: a command writes each of a cell's layers at most once.
            var seen = new List<(int Version, int[][] Layers, long Writes)> { (grid.Version, Snapshot(grid), 0) };
            long writes = 0;
            foreach (var (cell, kind) in t.edits)
            {
                world.Step([Edit(grid, cell % size, kind)]);
                var now = Snapshot(grid);
                if (grid.Version == seen[^1].Version) continue;
                writes += Between(seen[^1].Layers, now).Sum(c => Layers.Count(l => (c.Layers & l) != 0));
                seen.Add((grid.Version, now, writes));
                long dropped = Math.Max(0, writes - t.capacity);
                foreach (var (version, layers, by) in seen)
                {
                    var said = grid.ChangedSince(version);
                    bool reaches = by >= dropped;
                    if (!reaches)
                    {
                        if (said != null) return false.Label($"version {version} answered past the log ({by} writes by then, {dropped} dropped)");
                        continue;
                    }
                    if (said == null) return false.Label($"version {version} not answered ({by} writes by then, {dropped} dropped)");
                    var expected = Between(layers, now);
                    if (!said.SequenceEqual(expected))
                        return false.Label($"since {version}: [{string.Join(" ", said)}], not [{string.Join(" ", expected)}]");
                }
                if (grid.ChangedSince(grid.Version + 1) != null || grid.ChangedSince(-1) != null) return false.Label("a version the grid never had answered");
            }
            return true.ToProperty();
        });
    }

    [Fact]
    public void NamesTheLayersOfACellBuiltAndNotOneOpenedAndClosedAgain()
    {
        var w = new Scenario(Layout.Parse(["#D#  ", "#..V "]));
        int before = w.M.Grid.Version;
        w.Do(new ToggleDoor(1)).Run().Do(new ToggleDoor(1)).Do(new Place(3, Placement.Wall("iron"))).Run();
        Assert.Equal([new CellChange(3, GridLayers.Structure | GridLayers.Material)], w.M.Grid.ChangedSince(before));
        int built = w.M.Grid.Version;
        w.Do(new ClearAt(8, "machine")).Do(new Place(4, Placement.BackWall("iron"))).Do(new ClearAt(7, "machine")).Run();
        Assert.Equal([new CellChange(4, GridLayers.Back | GridLayers.BackMaterial), new CellChange(8, GridLayers.Machine)], w.M.Grid.ChangedSince(built));
        Assert.Empty(w.M.Grid.ChangedSince(w.M.Grid.Version)!);
    }

    [Fact]
    public void KnowsNoVersionFromBeforeALoad()
    {
        var w = new Scenario("#D#\n#.#");
        w.Do(new ToggleDoor(1)).Run().Do(new ToggleDoor(1)).Run();
        int version = w.M.Grid.Version;
        Assert.Equal(2, version);
        w.Reload();
        Assert.Equal(0, w.M.Grid.Version);
        Assert.Null(w.M.Grid.ChangedSince(version));
        Assert.Empty(w.M.Grid.ChangedSince(0)!);
    }

    /// <summary>
    /// A tick writing more than the log holds leaves rooms and pipes behind it: they find everything again,
    /// as after a load, and say so; what they find is what a search from scratch finds.
    /// </summary>
    [Fact]
    public void ReadersLeftBehindByTheLogFindEverythingAgain()
    {
        var layout = Layout.Parse(["        ", "        ", "        ", "        ", "        "]);
        var grid = new GridModule { LogCapacity = 8 };
        var rooms = new RoomsModule(grid);
        var pipes = new PipesModule(grid);
        var world = World.Create(1, layout, [grid, rooms, pipes, new Builder(grid, grid)]);
        int roomsAt = rooms.Version, pipesAt = pipes.Version, gridAt = grid.Version;
        // A closed room of 4×3 walls round a 2×1 floor, and a pipe through it: 16 writes in one tick.
        var walls = new[] { 0, 1, 2, 3, 8, 11, 16, 17, 18, 19 }.Select(c => (Command)new Place(c + 9, Placement.Wall()));
        world.Step([.. walls, new Place(18, new Placement("conduit", "pipe")), new Place(19, new Placement("conduit", "pipe"))]);
        Assert.Null(grid.ChangedSince(gridAt));
        Assert.Null(rooms.ChangedSince(roomsAt));
        var (roomOf, found) = RoomsModule.Find(grid.Width, grid.Height, grid.StructureLayer, grid.BackLayer);
        Assert.Equal(roomOf, rooms.RoomOfCells.ToArray());
        Assert.Equal(found.Select(r => (r.Id, r.Volume)), rooms.All.Select(r => (r.Id, r.Volume)));
        Assert.Equal([18], rooms.All.Select(r => r.Id));
        Assert.True(pipes.Version > pipesAt);
        Assert.Equal([new[] { 18, 19 }], pipes.Networks.Select(n => n.Cells.ToArray()));
        // Within what the log holds again: around the change only.
        roomsAt = rooms.Version;
        world.Step([new ClearAt(18, "conduit")]);
        Assert.Equal([new[] { 19 }], pipes.Networks.Select(n => n.Cells.ToArray()));
        world.Step([new ClearAt(10, "structure")]);
        Assert.Equal([10, 18], rooms.ChangedSince(roomsAt));
        Assert.Equal(RoomsModule.Find(grid.Width, grid.Height, grid.StructureLayer, grid.BackLayer).RoomOf, rooms.RoomOfCells.ToArray());
    }
}
