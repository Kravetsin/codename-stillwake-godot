using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Grid;
using Xunit;
using Structure = Stillwake.Content.Structure;

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
        // One straight span down the left column of each block from the vent's port to the tank's (0109).
        int width = layout.Width;
        Assert.Equal([2 * width + 2, 8 * width + 2], network.Cells);
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
    public void ADoorwayDoublesToTwoCellsThickTwiceAsLongAndAHatchAcrossTheSame()
    {
        // A door fills its blocks, two cells thick as its 16 px door was one (0104).
        Assert.Equal([".##.", ".##.", ".DD.", ".DD.", ".DD.", ".DD.", ".DD.", ".DD.", ".##.", ".##."], Doubled(".#.", ".D.", ".D.", ".D.", ".#.").Rows.Select(r => r[1..5]));
        Assert.Equal(["##DD##", "##DD##", "##HH##"], Doubled("#D#", "#H#").Rows.Take(3));
        Assert.Equal(["##DDDDDD##", "##DDDDDD##", ".........."], Doubled("#DDD#", ".....").Rows.Take(3));
    }

    [Fact]
    public void ADoubledDoorwayTogglesWholeTwoCellsThickAndSixTall()
    {
        var w = new Scenario("""
            ###
            .D.
            .D.
            .D.
            ###
            """);
        w.Do(new SetDoor(w.At(1, 2) + 1, true)).Run();
        var door = Enumerable.Range(2, 6).SelectMany(y => new[] { y * w.M.Grid.Width + 2, y * w.M.Grid.Width + 3 }).ToList();
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
            // The spans join their ends exactly as the doubled drawing's pipes run (0109): two span ends share a network when the
            // cells that carry, a closed valve not, join them side by side; every end not a closed valve is in one.
            int w = doubled.Width, size = w * doubled.Height;
            var carries = new bool[size];
            for (int c = 0; c < size; c++) carries[c] = doubled.At(c) is '-' or 'v';
            foreach (int c in doubled.CellsOf('-', under: true)) carries[c] = true;
            var component = new int[size];
            Array.Fill(component, -1);
            for (int start = 0; start < size; start++)
            {
                if (!carries[start] || component[start] >= 0) continue;
                var queue = new Queue<int>([start]);
                component[start] = start;
                while (queue.TryDequeue(out int c))
                    foreach (int n in (int[])[c % w > 0 ? c - 1 : -1, c % w < w - 1 ? c + 1 : -1, c - w, c + w])
                        if (n >= 0 && n < size && carries[n] && component[n] < 0) { component[n] = start; queue.Enqueue(n); }
            }
            var ends = big.M.Pipes.Spans.SelectMany(p => new[] { p.A, p.B }).Distinct().Where(c => big.M.Pipes.ValveAt(c) is not { Open: false }).ToList();
            foreach (int u in ends)
            {
                if (big.M.Pipes.NetworkAt(u) == null || !carries[u]) return false;
                foreach (int v in ends)
                    if ((big.M.Pipes.NetworkAt(u) == big.M.Pipes.NetworkAt(v)) != (component[u] == component[v])) return false;
            }
            return true;
        });
    }
}
