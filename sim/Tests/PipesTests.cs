using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/pipes/pipes.test.ts, ported; and the grid queries pipes reads.</summary>
public class PipesTests
{
    private static readonly Placement Pipe = new("conduit", "pipe");
    private static readonly Placement Vent = new("machine", "vent");

    private static (int, int)[] Summary(Scenario w) => w.M.Pipes.Networks.Select(n => (n.Id, n.Cells.Count)).ToArray();

    [Fact]
    public void MakesOneNetworkOfEachRunOfPipesNamedByItsSmallestCell()
    {
        var w = new Scenario("""
            ---..
            ..-..
            .....
            -....
            --...
            """);
        var networks = w.M.Pipes.Networks;
        Assert.Equal(2, networks.Count);
        Assert.Equal(w.At(0, 0), networks[0].Id);
        Assert.Equal(new[] { w.At(0, 0), w.At(1, 0), w.At(2, 0), w.At(2, 1) }, networks[0].Cells);
        Assert.Empty(networks[0].Machines);
        Assert.Equal(w.At(0, 3), networks[1].Id);
        Assert.Equal(new[] { w.At(0, 3), w.At(0, 4), w.At(1, 4) }, networks[1].Cells);
        Assert.Empty(networks[1].Machines);
        Assert.Equal(w.At(0, 0), w.M.Pipes.NetworkAt(w.At(2, 1)));
        Assert.Null(w.M.Pipes.NetworkAt(w.At(3, 3)));
        Assert.Null(w.M.Pipes.NetworkAt(-1));
    }

    /// <summary>A pipe laid out in space, then taken up: what is built shrinks past it, and the network still loses it.</summary>
    [Fact]
    public void FindsANetworkAgainWhenAPipeAtTheEdgeOfWhatIsBuiltIsTakenUp()
    {
        var w = new Scenario(Layout.Parse(["---     "]));
        w.Do(new Place(w.At(3, 0), Pipe)).Do(new Place(w.At(4, 0), Pipe)).Run();
        Assert.Equal(new[] { (0, 5) }, Summary(w));
        w.Do(new ClearAt(w.At(4, 0), "conduit")).Run();
        Assert.Equal(new Box(0, 0, 3, 0), w.M.Grid.Built);
        Assert.Equal(new[] { (0, 4) }, Summary(w));
    }

    [Fact]
    public void JoinsPipesSideBySideOnlyNotCornerToCorner()
    {
        var w = new Scenario("""
            -.
            .-
            """);
        Assert.Equal(new[] { (0, 1), (3, 1) }, Summary(w));
    }

    [Fact]
    public void SplitsANetworkAtAClosedValveAndJoinsItAgainWhenTheValveOpens()
    {
        var w = new Scenario("--v--");
        Assert.Equal(new[] { (0, 5) }, Summary(w));
        w.Do(new ToggleValve(2)).Run();
        Assert.Equal(new[] { (0, 2), (3, 2) }, Summary(w));
        // A closed valve carries nothing, so it belongs to no network.
        Assert.Null(w.M.Pipes.NetworkAt(2));
        w.Do(new ToggleValve(2)).Run();
        Assert.Equal(new[] { (0, 5) }, Summary(w));
    }

    [Fact]
    public void JoinsTwoNetworksWhenThePipeBetweenThemIsBuilt()
    {
        var w = new Scenario("--.--");
        Assert.Equal(new[] { (0, 2), (3, 2) }, Summary(w));
        w.Do(new Place(2, Pipe)).Run();
        Assert.Equal(new[] { (0, 5) }, Summary(w));
        w.Do(new ClearAt(2, "conduit")).Run();
        Assert.Equal(new[] { (0, 2), (3, 2) }, Summary(w));
    }

    [Fact]
    public void PutsAMachineOnTheNetworkItsPortStandsOnAndOnlyThroughItsPort()
    {
        // A vent's port is its own cell; a tank's is its bottom-left cell.
        var w = new Scenario("""
            V---
            ....
            TT..
            TT..
            """);
        var tank = w.M.Grid.Machines.Single(m => m.Kind == "tank");
        var vent = w.M.Grid.Machines.Single(m => m.Kind == "vent");
        Assert.Empty(w.M.Pipes.NetworksOf(vent.Id));
        w.Do(new Place(w.At(0, 0), Pipe)).Do(new Place(w.At(1, 2), Pipe)).Run();
        Assert.Equal(new[] { w.At(0, 0) }, w.M.Pipes.NetworksOf(vent.Id));
        Assert.Equal(new[] { vent.Id }, w.M.Pipes.Network(w.At(0, 0))?.Machines);
        // Under the tank but not its port: no connection.
        Assert.Empty(w.M.Pipes.NetworksOf(tank.Id));
        w.Do(new Place(w.At(0, 3), Pipe)).Run();
        Assert.Equal(new[] { w.At(0, 3) }, w.M.Pipes.NetworksOf(tank.Id));
    }

    [Fact]
    public void LetsGoOfAMachineThatIsTakenAwayAndTakesUpOnePutBack()
    {
        var w = new Scenario("-V-");
        var vent = w.M.Grid.Machines[0];
        w.Do(new Place(1, Pipe)).Run();
        Assert.Equal(new[] { vent.Id }, w.M.Pipes.Network(0)?.Machines);
        w.Do(new ClearAt(1, "machine")).Run();
        Assert.Empty(w.M.Pipes.Network(0)!.Machines);
        w.Do(new Place(1, Vent)).Run();
        Assert.Equal(new[] { vent.Id + 1 }, w.M.Pipes.Network(0)?.Machines);
        // Replaced in one tick, so as many machines stand as before: still noticed.
        w.Do(new ClearAt(1, "machine")).Do(new Place(1, Vent)).Run();
        Assert.Equal(new[] { vent.Id + 2 }, w.M.Pipes.Network(0)?.Machines);
    }

    [Fact]
    public void KeepsWhatItFoundWhileConduitsAndMachinesStayAsTheyAre()
    {
        var w = new Scenario("-.D.-");
        var found = w.M.Pipes.Networks;
        w.Run(20).Do(new ToggleDoor(2)).Run();
        Assert.Same(found, w.M.Pipes.Networks);
    }

    private static readonly char[] Chars = [' ', '-', '-', '-', 'v', 'x', '#'];

    /// <summary>
    /// Every pipe and open valve is in one network shared with its neighbours, whatever valves do, and
    /// a load finds the same: the invariant test of pipes.test.ts.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property PutsEveryPipeAndOpenValveInOneNetworkSharedWithItsNeighboursAsAfterALoad()
    {
        var gen =
            from width in Gen.Choose(1, 12)
            from height in Gen.Choose(1, 10)
            from cells in Gen.Elements(Chars).ArrayOf(width * height)
            from count in Gen.Choose(0, 8)
            from toggles in Gen.Choose(0, width * height - 1).ArrayOf(count)
            select (width, height, cells, toggles);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var rows = Enumerable.Range(0, t.height).Select(y => new string(t.cells, y * t.width, t.width)).ToArray();
            var w = new Scenario(Layout.Parse(rows));
            int size = t.width * t.height;
            foreach (int cell in t.toggles)
                if (w.M.Grid.ConduitLayer[cell] is Conduit.ValveOpen or Conduit.ValveClosed)
                    w.Do(new ToggleValve(cell)).Run();

            bool Carries(int c) => w.M.Grid.ConduitLayer[c] is Conduit.Pipe or Conduit.ValveOpen;
            var counts = new SortedDictionary<int, int>();
            for (int cell = 0; cell < size; cell++)
            {
                var id = w.M.Pipes.NetworkAt(cell);
                if ((id != null) != Carries(cell)) return false;
                if (id == null) continue;
                // Cells come in order, so a network's first cell is its smallest: its id.
                if (!counts.ContainsKey(id.Value) && cell != id) return false;
                counts[id.Value] = counts.GetValueOrDefault(id.Value) + 1;
                int x = cell % t.width;
                foreach (int n in new[] { x > 0 ? cell - 1 : -1, x < t.width - 1 ? cell + 1 : -1, cell - t.width, cell + t.width })
                    if (n >= 0 && n < size && Carries(n) && w.M.Pipes.NetworkAt(n) != id) return false;
            }
            var networks = w.M.Pipes.Networks;
            if (!networks.Select(n => (n.Id, n.Cells.Count)).SequenceEqual(counts.Select(kv => (kv.Key, kv.Value)))) return false;
            if (networks.Any(n => n.Cells.Where((c, i) => i > 0 && c <= n.Cells[i - 1]).Any())) return false;
            static string Text(IReadOnlyList<Pipes.Network> ns) =>
                string.Join(";", ns.Select(n => $"{n.Id}:{string.Join(",", n.Cells)}:{string.Join(",", n.Machines)}"));
            string before = Text(networks);
            w.Reload();
            return before == Text(w.M.Pipes.Networks);
        });
    }

    /// <summary>grid.test.ts, the queries pipes reads: a machine's ports and the conduit layer.</summary>
    [Fact]
    public void GridReadsAMachinesPortsAndItsConduitLayer()
    {
        var w = new Scenario("""
            V...
            TT-.
            TTvx
            """);
        var vent = w.M.Grid.Machines.Single(m => m.Kind == "vent");
        var tank = w.M.Grid.Machines.Single(m => m.Kind == "tank");
        Assert.Equal(new[] { w.At(0, 0) }, w.M.Grid.PortsOf(vent));
        Assert.Equal(new[] { w.At(0, 2) }, w.M.Grid.PortsOf(tank));
        Assert.Equal(new[] { Conduit.Pipe, Conduit.None, Conduit.ValveOpen, Conduit.ValveClosed },
            new[] { w.At(2, 1), w.At(3, 1), w.At(2, 2), w.At(3, 2) }.Select(c => w.M.Grid.ConduitLayer[c]));
    }
}
