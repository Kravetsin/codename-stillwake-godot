using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>From packages/sim/src/modules/grid/grid.test.ts: what walls and back walls are made of, as atmosphere reads it.</summary>
public class GridTests
{
    /// <summary>"backs a cell in a material, says which", and the material of "places every layer where it fits".</summary>
    [Fact]
    public void SaysWhatAWallAndABackWallAreMadeOf()
    {
        var w = new Scenario("#..\n...", cells: true);
        Assert.Equal("scrap", w.M.Grid.WallAt(0)?.Id);
        Assert.Equal("scrap", w.M.Grid.BackAt(1)?.Id);
        Assert.Null(w.M.Grid.WallAt(1));
        w.Do(new ClearAt(1, "back")).Do(new Place(2, Placement.Wall("iron"))).Run();
        Assert.Null(w.M.Grid.BackAt(1));
        Assert.Equal("iron", w.M.Grid.WallAt(2)?.Id);
        w.Do(new Place(1, Placement.BackWall("gold"))).Do(new Place(1, Placement.BackWall("iron"))).Run();
        Assert.Equal(new[] { "grid.noSuchMaterial" }, w.Refusals.Select(r => r.Reason));
        Assert.Equal("iron", w.M.Grid.BackAt(1)?.Id);
        Assert.Null(w.M.Grid.WallAt(-1));
        Assert.Null(w.M.Grid.BackAt(6));
    }

    /// <summary>A hatch is a doorway lying in a deck (0067): opened or shut from any of its cells, it moves whole.</summary>
    [Fact]
    public void OpensAndShutsAHatchThreeWideWholeFromAnyOfItsCells()
    {
        var w = new Scenario("""
            #######
            #.....#
            ##DDD##
            #.....#
            #######
            """);
        int[] Hatch() => [.. Enumerable.Range(2, 3).Select(x => w.M.Grid.StructureAt(w.At(x, 2)))];
        w.Do(new SetDoor(w.At(3, 2), true)).Run();
        Assert.Equal(new[] { Structure.DoorOpen, Structure.DoorOpen, Structure.DoorOpen }, Hatch());
        w.Do(new SetDoor(w.At(4, 2), false)).Run();
        Assert.Equal(new[] { Structure.DoorClosed, Structure.DoorClosed, Structure.DoorClosed }, Hatch());
    }

    /// <summary>Two peers who both saw a door shut open it in one tick, or one a round trip after the other: it stays open.</summary>
    [Fact]
    public void LeavesADoorOpenWhenTwoPeersOpenItAtOnceOrOneLate()
    {
        var w = new Scenario("#D#\n#D#\n#.#", cells: true);
        int before = w.M.Grid.Version;
        w.Do(new SetDoor(1, true) { From = 0 }).Do(new SetDoor(4, true) { From = 1 }).Run();
        Assert.Equal((Structure.DoorOpen, Structure.DoorOpen), (w.M.Grid.StructureAt(1), w.M.Grid.StructureAt(4)));
        Assert.Equal(before + 1, w.M.Grid.Version);
        w.Run(3).Do(new SetDoor(1, true) { From = 1 }).Run();
        Assert.Equal(Structure.DoorOpen, w.M.Grid.StructureAt(1));
        Assert.Empty(w.Refusals);
    }

    /// <summary>A door or valve already as asked is left alone, the grid's version too; a doorway open in part opens whole.</summary>
    [Fact]
    public void LeavesADoorOrValveAlreadyAsAskedAloneAndOpensAMixedDoorwayWhole()
    {
        var w = new Scenario("#D#v\n###x\n#d#.\n#D#.", cells: true);
        int before = w.M.Grid.Version;
        w.Do(new SetDoor(1, false)).Do(new SetValve(3, true)).Do(new SetValve(7, false)).Run();
        Assert.Equal(before, w.M.Grid.Version);
        Assert.Empty(w.Refusals);
        w.Do(new SetDoor(13, true)).Run();
        Assert.Equal((Structure.DoorOpen, Structure.DoorOpen), (w.M.Grid.StructureAt(9), w.M.Grid.StructureAt(13)));
        w.Do(new SetValve(7, true)).Do(new SetValve(3, false)).Run();
        Assert.Equal((Conduit.ValveClosed, Conduit.ValveOpen), (w.M.Grid.ConduitAt(3), w.M.Grid.ConduitAt(7)));
    }

    /// <summary>
    /// A doorway is one straight run (0067): where a door meets a hatch, the cell they share is the door's and the hatch
    /// stops beside it, so each opens alone; a door and a hatch touching only at a corner never were one.
    /// </summary>
    [Theory]
    [InlineData("DDD.|..D.|..D.", 0, 0, "ddD.|..D.|..D.")]
    [InlineData("DDD.|..D.|..D.", 2, 2, "DDd.|..d.|..d.")]
    [InlineData("D..|DDD|D..", 1, 1, "D..|Ddd|D..")]
    [InlineData("D..|DDD|D..", 0, 0, "d..|dDD|d..")]
    [InlineData("DDDDD|..D..|..D..", 0, 0, "ddDDD|..D..|..D..")]
    [InlineData("DDDDD|..D..|..D..", 2, 1, "DDdDD|..d..|..d..")]
    [InlineData(".D.|DDD|.D.", 0, 1, ".D.|dDD|.D.")]
    [InlineData(".D.|DDD|.D.", 1, 0, ".d.|DdD|.d.")]
    [InlineData("D..|D..|.DD", 0, 0, "d..|d..|.DD")]
    [InlineData("D..|D..|.DD", 1, 2, "D..|D..|.dd")]
    public void SetsOnlyTheStraightRunTheCellIsIn(string map, int x, int y, string after)
    {
        var w = new Scenario(map.Replace('|', '\n'), cells: true);
        w.Do(new SetDoor(w.At(x, y), true)).Run();
        int width = map.IndexOf('|');
        string Shown(int c) => w.M.Grid.StructureAt(c) switch { Structure.DoorClosed => "D", Structure.DoorOpen => "d", _ => "." };
        var rows = Enumerable.Range(0, after.Split('|').Length).Select(r => string.Concat(Enumerable.Range(0, width).Select(c => Shown(r * width + c))));
        Assert.Equal(after, string.Join("|", rows));
    }

    private static readonly string[] Kinds = [.. Content.Machines.All.Select(m => m.Kind)];

    /// <summary>
    /// A machine found by its id, its kind or a cell of it is the one a scan of every machine finds, after
    /// any builds and removals and after a load; and the machines stay in id order.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property FindsEveryMachineByIdKindAndCellAsAScanOfThemAll()
    {
        var gen =
            from width in Gen.Choose(2, 12)
            from height in Gen.Choose(2, 8)
            from edits in (from cell in Gen.Choose(0, 1 << 20) from kind in Gen.Choose(0, 2 * Kinds.Length - 1) select (cell, kind)).ListOf()
            from reload in Gen.Choose(0, 40)
            select (width, height, edits: edits.Take(60).ToList(), reload);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var w = new Scenario(Layout.Parse(Enumerable.Repeat(new string('.', t.width), t.height).ToArray()));
            int size = t.width * t.height;
            for (int i = 0; i < t.edits.Count; i++)
            {
                var (cell, kind) = t.edits[i];
                // Half of the edits take a machine apart, so builds and removals interleave.
                w.Do(kind >= Kinds.Length ? new ClearAt(cell % size, "machine") : new Place(cell % size, new Placement("machine", Kinds[kind]))).Run();
                if (i == t.reload) w.Reload();
                if (!AsAScanFinds(w.M.Grid, size, t.edits.Count + 2)) return false;
            }
            return true;
        });
    }

    /// <summary>
    /// The built box is the smallest one a scan of every layer and machine finds, after any builds and
    /// removals in any layer, out in space or on the edge of what stands, and after a load.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property KeepsTheBuiltBoxTheOneAScanOfEveryLayerFinds()
    {
        var gen =
            from width in Gen.Choose(1, 12)
            from height in Gen.Choose(1, 10)
            from cells in Gen.Elements(' ', ' ', ' ', '#', '.', '-', 'D', 'V').ArrayOf(width * height)
            from edits in (from cell in Gen.Choose(0, 1 << 20) from kind in Gen.Choose(0, 7) select (cell, kind)).ListOf()
            from reload in Gen.Choose(0, 40)
            select (rows: Enumerable.Range(0, height).Select(y => new string(cells, y * width, width)).ToArray(), edits: edits.Take(60).ToList(), reload);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var w = new Scenario(Layout.Parse(WholeVents(t.rows)));
            var grid = w.M.Grid;
            int size = grid.Width * grid.Height;
            for (int i = 0; i < t.edits.Count; i++)
            {
                var (at, kind) = t.edits[i];
                int cell = at % size;
                // Half of the edits take something apart, so the box grows and shrinks.
                w.Do(kind switch
                {
                    0 => new Place(cell, Placement.Wall()),
                    1 => new Place(cell, new Placement("conduit", "pipe")),
                    2 => new Place(cell, Placement.BackWall()),
                    3 => new Place(cell, new Placement("machine", "vent")),
                    4 => new ClearAt(cell, "structure"),
                    5 => new ClearAt(cell, "conduit"),
                    6 => new ClearAt(cell, "back"),
                    _ => new ClearAt(cell, "machine"),
                }).Run();
                if (i == t.reload) { w.Reload(); grid = w.M.Grid; }
                var scanned = Box.None;
                for (int c = 0; c < size; c++)
                    if (grid.StructureAt(c) != Structure.Empty || grid.ConduitAt(c) != Conduit.None || grid.BackLayer[c] != Back.None || grid.MachineAt(c) != null)
                        scanned = scanned.Union(new Box(c % grid.Width, c / grid.Width, c % grid.Width, c / grid.Width));
                if (grid.Built != scanned) return false.Label($"edit {i}: the box is {grid.Built}, a scan finds {scanned}");
            }
            return true.ToProperty();
        });
    }

    /// <summary>
    /// <paramref name="rows"/> with each vent drawn whole (0104): in reading order, a <c>V</c> not yet covered starts a vent's footprint
    /// where it fits, else it becomes open ship, <c>.</c>; as <see cref="Layout.Machines"/> reads them.
    /// </summary>
    internal static string[] WholeVents(string[] rows)
    {
        var vent = Content.Machines.Get("vent");
        var cells = rows.Select(r => r.ToCharArray()).ToArray();
        var claimed = cells.Select(r => new bool[r.Length]).ToArray();
        for (int y = 0; y < cells.Length; y++)
            for (int x = 0; x < cells[y].Length; x++)
            {
                if (cells[y][x] != 'V' || claimed[y][x]) continue;
                bool fits = y + vent.Height <= cells.Length && x + vent.Width <= cells[y].Length
                    && Enumerable.Range(0, vent.Height).All(dy => Enumerable.Range(0, vent.Width).All(dx => !claimed[y + dy][x + dx]));
                if (!fits) { cells[y][x] = '.'; continue; }
                for (int dy = 0; dy < vent.Height; dy++)
                    for (int dx = 0; dx < vent.Width; dx++)
                        (cells[y + dy][x + dx], claimed[y + dy][x + dx]) = ('V', true);
            }
        return [.. cells.Select(r => new string(r))];
    }

    private static bool AsAScanFinds(GridModule grid, int size, int ids)
    {
        var all = grid.Machines;
        for (int i = 1; i < all.Count; i++) if (all[i - 1].Id >= all[i].Id) return false;
        foreach (var kind in Kinds)
            if (!grid.MachinesOf(kind).SequenceEqual(all.Where(m => m.Kind == kind))) return false;
        for (int id = 0; id <= ids; id++)
            if (grid.MachineById(id) != all.FirstOrDefault(m => m.Id == id)) return false;
        for (int cell = -1; cell <= size; cell++)
            if (grid.MachineAt(cell) != all.FirstOrDefault(m => grid.Footprint(m.Kind, m.Cell)!.Contains(cell))) return false;
        return ((GridState)grid.Slice).ById.Count == all.Count;
    }

    // The build zone (0048).

    /// <summary>The zone of <see cref="Zoned"/>'s 10×6 map of space: columns 2 to 7, rows 1 to 4.</summary>
    private static readonly Box SmallZone = new(2, 1, 7, 4);

    /// <param name="doubled">Whether the map and its zone are drawn a character a 16 px cell, for a machine's footprint (0104).</param>
    private static Scenario Zoned(bool doubled = false) =>
        new(Layout.Parse(Enumerable.Repeat(new string(' ', 10), 6).ToArray()) with { Zone = SmallZone }, doubled: doubled);

    private static readonly Placement[] EveryLayer = [Placement.Wall(), new("conduit", "pipe"), Placement.BackWall()];

    [Fact]
    public void RefusesAWallAPipeOrABackWallJustOutsideTheZoneOnEverySideAndKeepsItsZoneAcrossALoad()
    {
        var w = Zoned();
        foreach (var (x, y) in new[] { (1, 2), (8, 2), (4, 0), (4, 5), (1, 0), (8, 5) })
            foreach (var p in EveryLayer)
                w.Do(new Place(w.At(x, y), p));
        w.Run();
        Assert.Equal(Enumerable.Repeat("grid.outsideZone", 18), w.Refusals.Select(r => r.Reason));
        Assert.Equal(Box.None, w.M.Grid.Built);
        w.Reload();
        Assert.Equal(SmallZone, w.M.Grid.Zone);
        Assert.Equal("grid.outsideZone", w.M.Grid.PlacementProblem(w.At(1, 2), Placement.Wall()));
    }

    [Fact]
    public void BuildsEveryLayerOnTheZonesEdgeCells()
    {
        var w = Zoned();
        foreach (var (x, y) in new[] { (2, 1), (7, 1), (2, 4), (7, 4) })
            foreach (var p in EveryLayer)
                w.Do(new Place(w.At(x, y), p));
        w.Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(SmallZone, w.M.Grid.Built);
    }

    [Fact]
    public void RefusesAMachineWhoseFootprintStraddlesTheZonesEdgeAndBuildsOneJustInside()
    {
        // A furnace is drawn 2×2: its top-left at (6, 3) covers the zone's last two columns and rows.
        var w = Zoned(doubled: true);
        foreach (var (x, y) in new[] { (7, 3), (6, 4), (1, 1), (2, 0), (7, 4) })
            w.Do(new Place(w.At(x, y), new Placement("machine", "furnace")));
        w.Run();
        Assert.Equal(Enumerable.Repeat("grid.outsideZone", 5), w.Refusals.Select(r => r.Reason));
        w.Do(new Place(w.At(6, 3), new Placement("machine", "furnace"))).Run();
        Assert.Equal(5, w.Refusals.Count);
        Assert.Equal(w.At(6, 3), Assert.Single(w.M.Grid.Machines).Cell);
    }

    [Fact]
    public void TakesTheWholeMapAsItsZoneWhenTheMapSetsNone()
    {
        var w = new Scenario(Layout.Parse(["   ", "   "]));
        Assert.Equal(new Box(0, 0, 2, 1), w.M.Grid.Zone);
        w.Do(new Place(w.At(0, 0), Placement.Wall())).Do(new Place(w.At(2, 1), Placement.BackWall())).Run();
        Assert.Empty(w.Refusals);
    }

    /// <summary>
    /// Whatever every module's commands and builds anywhere on the grid, and off it, ask for, nothing is
    /// ever built outside the zone: what is built lies within it after every tick.
    /// </summary>
    [Property(MaxTest = 40)]
    public Property BuildsNothingOutsideTheZoneWhateverIsAsked()
    {
        var placements = new[] { Placement.Wall(), new Placement("structure", "door"), new Placement("conduit", "pipe"), Placement.BackWall() }
            .Concat(Kinds.Select(k => new Placement("machine", k))).ToArray();
        var gen =
            from width in Gen.Choose(3, 24)
            from height in Gen.Choose(3, 14)
            from left in Gen.Choose(0, width - 1)
            from top in Gen.Choose(0, height - 1)
            from right in Gen.Choose(left, width - 1)
            from bottom in Gen.Choose(top, height - 1)
            from seed in Gen.Choose(1, 100_000)
            from ticks in Gen.Choose(20, 200)
            select (width, height, zone: new Box(left, top, right, bottom), seed, ticks);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var layout = Layout.Parse(Enumerable.Repeat(new string(' ', t.width), t.height).ToArray()) with { Zone = t.zone };
            var w = new Scenario(layout, (uint)t.seed);
            var rng = new Rng(new RngState { S = (uint)t.seed });
            for (int i = 0; i < t.ticks; i++)
            {
                foreach (var c in ModulesTests.RandomCommands(w.World, rng)) w.Do(c);
                int cell = (int)rng.Below((uint)(t.width * t.height + 2)) - 1;
                // From a peer the random commands never move off the ship, where the test builder is.
                w.Do(new Place(cell, placements[rng.Below((uint)placements.Length)]) { From = 5 }).Run();
                var built = w.M.Grid.Built;
                if (!built.IsEmpty && !(t.zone.Contains(built.Left, built.Top) && t.zone.Contains(built.Right, built.Bottom)))
                    return false.Label($"tick {i}: built {built} outside the zone {t.zone}");
            }
            return true.ToProperty();
        });
    }
}

/// <summary>From grid.test.ts: "toggles a client has in flight", folded by <c>AfterToggles</c> (0014).</summary>
public class GridAfterTogglesTests
{
    /// <summary><paramref name="layer"/> with the cells <c>AfterToggles</c> listed laid over it.</summary>
    private static int[] Over(ReadOnlySpan<int> layer, IReadOnlyDictionary<int, int> cells)
    {
        int[] shown = layer.ToArray();
        foreach (var (cell, code) in cells) shown[cell] = code;
        return shown;
    }

    [Fact]
    public void ShowTheLayersAsTheirCommandsWouldLeaveThemSkippingWhatTheyWouldRefuse()
    {
        var w = new Scenario("#D#v\n#D#-", cells: true);
        Command[] toggles = [new SetDoor(5, true), new SetValve(3, false), new SetDoor(0, true), new SetValve(7, true)];
        var (structure, conduit) = w.M.Grid.AfterToggles(toggles);
        int[] shownStructure = Over(w.M.Grid.StructureLayer, structure), shownConduit = Over(w.M.Grid.ConduitLayer, conduit);
        foreach (var t in toggles) w.Do(t);
        w.Run();
        Assert.Equal(new[] { "grid.noDoor", "grid.noValve" }, w.Refusals.Select(r => r.Reason));
        Assert.Equal(w.M.Grid.StructureLayer.ToArray(), shownStructure);
        Assert.Equal(w.M.Grid.ConduitLayer.ToArray(), shownConduit);
    }

    [Fact]
    public void ListOnlyTheCellsTheToggledDoorwaysAndValvesCover()
    {
        var w = new Scenario("#D#v\n#D#-\n#.#.", cells: true);
        var (structure, conduit) = w.M.Grid.AfterToggles([new SetDoor(5, true), new SetValve(3, false), new SetDoor(0, true)]);
        Assert.Equal(new[] { 1, 5 }, structure.Keys.Order());
        Assert.Equal(new[] { 3 }, conduit.Keys);
    }

    [Fact]
    public void FoldInOrderSoEachToggleSeesTheOnesBeforeItAndTheWorldIsLeftBe()
    {
        var w = new Scenario("#d#\n#D#", cells: true);
        ulong hash = w.World.Hash();
        int[] live = w.M.Grid.StructureLayer.ToArray();
        var (structure, _) = w.M.Grid.AfterToggles([new SetDoor(1, false), new SetDoor(4, true)]);
        // A mixed doorway: the first shuts it all, the second opens it all again.
        Assert.Equal(new[] { live[1], live[1] }, new[] { structure[1], structure[4] });
        Assert.Equal(hash, w.World.Hash());
        Assert.Equal(live, w.M.Grid.StructureLayer.ToArray());
    }
}
