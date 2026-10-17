using System.Text.Json.Serialization;
using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Pipes;

/// <summary>A pipe network: its smallest cell, its pipes and open valves in cell order, the machines on it by id.</summary>
public sealed record Network(int Id, IReadOnlyList<int> Cells, IReadOnlyList<int> Machines);

/// <summary>Everything pipes keep is derived: found again from the grid, never saved.</summary>
public sealed class PipesState
{
    /// <summary>Per cell, the id of its network, or <see cref="PipeNetworks.None"/>.</summary>
    [JsonIgnore] internal int[] NetworkOf { get; set; } = [];
    [JsonIgnore] internal List<Network> Networks { get; set; } = [];
    /// <summary>The grid's version last looked at: its log names the cells changed since.</summary>
    [JsonIgnore] internal int GridVersion { get; set; }
    /// <summary>Bumped each time the networks are found again.</summary>
    [JsonIgnore] internal int Version { get; set; }
}

/// <summary>What any module may read of the pipe networks. Read them at or after derivation.</summary>
public interface IPipesQueries
{
    /// <summary>The network <paramref name="cell"/>'s conduit belongs to; null for no conduit, a closed valve, or off the grid.</summary>
    int? NetworkAt(int cell);
    /// <summary>The networks machine <paramref name="machineId"/> has a port on, in id order.</summary>
    IReadOnlyList<int> NetworksOf(int machineId);
    Network? Network(int id);
    /// <summary>Every network, in id order.</summary>
    IReadOnlyList<Network> Networks { get; }
    /// <summary>
    /// Bumped each time the networks are found again, on any change of a conduit or a machine; never
    /// saved. A module keeping what it read of the networks keeps it while this stays.
    /// </summary>
    int Version { get; }
}

/// <summary>
/// Pipe networks: the runs of pipes that carry oxygen between tanks and vents, found from the grid's
/// conduit layer and machines (packages/sim/src/modules/pipes).
/// </summary>
public sealed class PipesModule : Module<PipesState>, IPipesQueries
{
    private readonly IGridQueries _grid;

    public override string Id => "pipes";

    public PipesModule(IGridQueries grid)
    {
        _grid = grid;
        System(Phase.Derivation, () =>
        {
            if (_grid.Version == State.GridVersion) return;
            var log = _grid.ChangedSince(State.GridVersion);
            State.GridVersion = _grid.Version;
            if (log == null || Moved(log)) Rebuild();
        });
    }

    protected override PipesState Init(Layout layout) => new();

    public override void Rebuild()
    {
        var machines = _grid.Machines.Select(m => (m.Id, _grid.PortsOf(m))).ToList();
        (State.NetworkOf, State.Networks) = PipeNetworks.Find(_grid.Width, _grid.Height, _grid.ConduitLayer, machines);
        State.GridVersion = _grid.Version;
        State.Version++;
    }

    /// <summary>Whether a conduit or a machine is among the <paramref name="changes"/>: a machine came or went where its cells did.</summary>
    private static bool Moved(IReadOnlyList<CellChange> changes)
    {
        foreach (var c in changes)
            if ((c.Layers & (GridLayers.Conduit | GridLayers.Machine)) != 0) return true;
        return false;
    }

    public int? NetworkAt(int cell)
    {
        if (cell < 0 || cell >= State.NetworkOf.Length || State.NetworkOf[cell] == PipeNetworks.None) return null;
        return State.NetworkOf[cell];
    }

    public IReadOnlyList<int> NetworksOf(int machineId) =>
        State.Networks.Where(n => n.Machines.Contains(machineId)).Select(n => n.Id).ToList();

    public Network? Network(int id) => State.Networks.Find(n => n.Id == id);
    public IReadOnlyList<Network> Networks => State.Networks;
    public int Version => State.Version;
}
