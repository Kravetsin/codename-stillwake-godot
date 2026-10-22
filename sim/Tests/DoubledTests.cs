using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Maps drawn a character a 16 px cell doubled to the 8 px cells (0104, <see cref="Layout.Doubled"/>): machines, pipes, floors,
/// doorways and the rest of a map, and the invariant that a doubled map keeps every machine whole and every network as its
/// drawing has it.
/// </summary>
public class DoubledTests
{
    private static Layout Doubled(params string[] rows) => Layout.Parse(rows).Doubled();

    [Fact]
    public void ADoubledTankIsFourByFourItsIdKeptAndItsPortOnTheTopLeftOfItsBlock()
    {
        var w = new Scenario("""
            #####
            #TTV#
            #TT.#
            #####
            """);
        var tank = Assert.Single(w.M.Grid.MachinesOf("tank"));
        Assert.Equal((1, w.At(1, 1)), (tank.Id, tank.Cell));
        Assert.Equal(16, w.M.Grid.Footprint("tank", tank.Cell)!.Count);
        Assert.Equal(2, w.M.Grid.MachinesOf("vent").Single().Id);
        Assert.Equal([w.At(1, 2)], w.M.Grid.PortsOf(tank));
    }

    [Fact]
    public void DoubledPipesStayOneCellWideJoinAndMeetThePortsOfDoubledMachines()
    {
        // A vent piped to a tank under it, the run drawn under the vent, on the map, and under the tank to its port.
        var layout = Layout.Parse(["#####", "#V..#", "#-..#", "#TT.#", "#TT.#", "#####"], ["", " -", "", " -", " -", ""]).Doubled();
        var w = new Scenario(layout);
        var network = Assert.Single(w.M.Pipes.Networks);
        Assert.Equal(["tank", "vent"], network.Machines.Select(id => w.M.Grid.MachineById(id)!.Kind).Order(StringComparer.Ordinal));
        // One cell wide: the left column of each block from the vent's port down to the tank's.
        int width = layout.Width;
        Assert.Equal(Enumerable.Range(2, 7).Select(y => y * width + 2), network.Cells);
    }

    [Fact]
    public void ADoubledFloorFillsTheTopRowOfItsBlockAndOpenGroundWithItsBackWallUnder()
    {
        var l = Doubled("=");
        Assert.Equal(["==", ".."], l.Rows);
        var w = new Scenario(l);
        Assert.Equal((Structure.Floor, Structure.Empty), (w.M.Grid.StructureAt(0), w.M.Grid.StructureAt(2)));
        Assert.NotNull(w.M.Grid.BackAt(2));
    }

    [Fact]
    public void ADoorwayDoublesToOneCellThickTwiceAsLongAndAHatchAcrossTheSame()
    {
        // A door stands in its blocks' left column, the right one open to what lies beside it: the room, or space.
        Assert.Equal([".##.", ".##.", ".D..", ".D..", ".D..", ".D..", ".D..", ".D..", ".##.", ".##."], Doubled(".#.", ".D.", ".D.", ".D.", ".#.").Rows.Select(r => r[1..5]));
        Assert.Equal(["D ", "D ", "D ", "D "], Doubled("D ", "D ").Rows.Select(r => r[..2]));
        // A hatch lies in their top row, the row under it open; one alone is a hatch, as Doorways has it, the ladder it is in kept.
        Assert.Equal(["##DD##", "##HH##"], Doubled("#D#", "#H#").Rows.Take(2));
        Assert.Equal(["##DDDDDD##", "##......##"], Doubled("#DDD#", ".....").Rows.Take(2));
    }

    [Fact]
    public void ADoubledDoorwayTogglesWholeSixCellsTall()
    {
        var w = new Scenario("""
            ###
            .D.
            .D.
            .D.
            ###
            """);
        w.Do(new SetDoor(w.At(1, 2), true)).Run();
        var door = Enumerable.Range(2, 6).Select(y => y * w.M.Grid.Width + 2).ToList();
        Assert.All(door, c => Assert.Equal(Structure.DoorOpen, w.M.Grid.StructureAt(c)));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void TheWakingPlaceIsTheBottomLeftOfItsBlockAndOxygenItsTanksTopLeft()
    {
        Assert.Equal(["..", "S."], Doubled("S").Rows);
        var l = Layout.Parse(["TT", "TT"], ["o"]).Doubled();
        Assert.Equal([0], l.CellsOf('o', under: true));
    }

    [Fact]
    public void DoublesMaterialsCablesTheZoneAndAPlantByItsAnchor()
    {
        var drawn = Layout.Parse(["..", "##"], cables: [((0, 0), (1, 0))]).WithMaterials("""
            ..
            st
            """) with { Zone = new Box(0, 0, 0, 1), Flora = [(0, "bush"), (1, "stalactite")] };
        var l = drawn.Doubled();
        Assert.Equal((4, 4), (l.Width, l.Height));
        int soil = Content.Materials.Get("soil").Code, stone = Content.Materials.Get("stone").Code;
        Assert.Equal([soil, soil, stone, stone], l.Materials!.Skip(8).Take(4));
        Assert.Equal([(0, 2)], l.Cables);
        Assert.Equal(new Box(0, 0, 1, 3), l.Zone);
        // The bush stands on the floor at its block's bottom-left; the stalactite hangs from its top-left.
        Assert.Equal([(2, "stalactite"), (4, "bush")], l.Flora);
    }

    [Fact]
    public void ASlopeDoesNotDouble() => Assert.Throws<FormatException>(() => Doubled("/#"));

    private static readonly char[] Chars = ['.', '.', '#', '-', 'v', 'x', 'D', '=', 'H', ' ', 'V', 'L'];

    /// <summary>
    /// Any drawn map, its vents, lamps and walls among pipes and doors: doubled, every machine is whole and of its kind, ids in the
    /// drawing's order, and the networks are the drawing's, each with the same machines, the same pipes by their blocks.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property KeepsEveryMachineWholeAndEveryNetworkAsTheDrawingHasIt()
    {
        var gen =
            from width in Gen.Choose(1, 10)
            from height in Gen.Choose(1, 8)
            from cells in Gen.Elements(Chars).ArrayOf(width * height)
            select (width, height, cells);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var rows = Enumerable.Range(0, t.height).Select(y => new string(t.cells, y * t.width, t.width)).ToArray();
            var drawn = Layout.Parse(rows);
            var doubled = drawn.Doubled();
            var big = new Scenario(doubled);
            // The drawing's machines, a character each: a vent or a lamp, a 16 px cell's.
            var drawnMachines = Enumerable.Range(0, t.width * t.height).Where(c => t.cells[c] is 'V' or 'L')
                .Select(c => (Kind: t.cells[c] == 'V' ? "vent" : "lamp", Cell: c)).ToList();
            var machines = big.M.Grid.Machines;
            if (machines.Count != drawnMachines.Count) return false;
            for (int i = 0; i < machines.Count; i++)
            {
                var (kind, cell) = drawnMachines[i];
                var m = machines[i];
                if (m.Kind != kind || m.Cell != cell / t.width * 2 * doubled.Width + cell % t.width * 2) return false;
                if (big.M.Grid.Footprint(m.Kind, m.Cell) is not { } cells || cells.Any(c => big.M.Grid.MachineAt(c)?.Id != m.Id)) return false;
            }
            // Each drawn conduit's block's top-left is in a network exactly when the drawn cell carries, and two carrying drawn
            // cells side by side share theirs.
            int Big(int x, int y) => 2 * y * doubled.Width + 2 * x;
            bool Carries(char c) => c is '-' or 'v';
            for (int y = 0; y < t.height; y++)
                for (int x = 0; x < t.width; x++)
                {
                    char c = rows[y][x];
                    int? net = big.M.Pipes.NetworkAt(Big(x, y));
                    if (Carries(c) != (net != null)) return false;
                    if (x + 1 < t.width && Carries(c) && Carries(rows[y][x + 1]) && big.M.Pipes.NetworkAt(Big(x + 1, y)) != net) return false;
                    if (y + 1 < t.height && Carries(c) && Carries(rows[y + 1][x]) && big.M.Pipes.NetworkAt(Big(x, y + 1)) != net) return false;
                }
            // No network joins drawn cells the drawing keeps apart. A closed valve's other half is pipe, alone between two of them.
            var small = new Scenario(drawn with { Rows = [.. rows.Select(r => new string([.. r.Select(c => c is 'V' or 'L' ? '.' : c)]))] });
            var drawnOf = new Dictionary<int, int>();
            for (int c = 0; c < t.width * t.height; c++)
            {
                if (small.M.Pipes.NetworkAt(c) is not int was) continue;
                int now = big.M.Pipes.NetworkAt(Big(c % t.width, c / t.width))!.Value;
                if (drawnOf.TryGetValue(now, out int other) && other != was) return false;
                drawnOf[now] = was;
            }
            return true;
        });
    }
}
