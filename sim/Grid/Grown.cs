using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stillwake.Sim.Grid;

/// <summary>
/// What a save holds of a grown surface's grid (0077): its size and zone, its machines, and the cells changed since its
/// ground grew from the seed, each as <c>cell, structure, material, back, backMaterial</c>, ascending by
/// cell. The ground itself is grown again from the seed as the save loads.
/// </summary>
public sealed class GrownGrid
{
    [JsonInclude] internal int Width { get; set; }
    [JsonInclude] internal int Height { get; set; }
    [JsonInclude] internal Box Zone { get; set; }
    [JsonInclude] internal List<Machine> Machines { get; set; } = [];
    [JsonInclude] internal int NextMachineId { get; set; } = 1;
    [JsonInclude] internal int[] Edits { get; set; } = [];
}

/// <summary>A grown surface's grid saved as its seed and the cells changed since (0077): its edits.</summary>
public sealed partial class GridModule
{
    /// <summary>The layers a cell's edit holds, in order, after its cell.</summary>
    private const int Layers = 4;

    /// <summary>Whether this grid grew from a seed (<see cref="Layout.Grown"/>): then it saves its edits, not every cell.</summary>
    private bool _grown;

    /// <summary>
    /// Derived: what each cell written since the ground grew held then, and those cells in the order first written: a
    /// save compares them with what they hold now. Never saved: after a load, the cells the save named.
    /// </summary>
    private readonly Dictionary<int, int[]> _base = new();
    private List<int> _touched = [];

    /// <summary>Notes what <paramref name="cell"/> held as grown, on the first write to it.</summary>
    private void Touch(int cell)
    {
        if (_base.ContainsKey(cell)) return;
        _base[cell] = Of(State, cell);
        _touched.Add(cell);
    }

    public bool GrownBack(int cell)
    {
        if (!_grown || BackAt(cell) == null) return false;
        return !_base.TryGetValue(cell, out var was) || was[2] == State.Back[cell] && was[3] == State.BackMaterial[cell];
    }

    private static int[] Of(GridState s, int cell) =>
        [s.Structure[cell], s.Material[cell], s.Back[cell], s.BackMaterial[cell]];

    /// <summary>The cells that differ now from the ground as it grew, ascending, each with its layers: what a save holds.</summary>
    internal int[] Edits()
    {
        var cells = _touched.ToArray();
        Array.Sort(cells);
        var edits = new List<int>();
        foreach (int cell in cells)
        {
            var now = Of(State, cell);
            if (now.AsSpan().SequenceEqual(_base[cell])) continue;
            edits.Add(cell);
            edits.AddRange(now);
        }
        return [.. edits];
    }

    protected override object Save() => !_grown ? State : new GrownGrid
    {
        Width = State.Width,
        Height = State.Height,
        Zone = State.Zone,
        Machines = State.Machines,
        NextMachineId = State.NextMachineId,
        Edits = Edits(),
    };

    protected override GridState? Load(JsonElement saved, JsonSerializerOptions options, Layout? ground)
    {
        if (ground == null) return base.Load(saved, options, ground);
        var grown = saved.Deserialize<GrownGrid>(options) ?? throw new InvalidDataException("No grid in the save.");
        if (grown.Width != ground.Width || grown.Height != ground.Height)
            throw new InvalidDataException($"A grid of {grown.Width}×{grown.Height} saved over ground of {ground.Width}×{ground.Height}.");
        var s = Init(ground);
        (s.Zone, s.Machines, s.NextMachineId) = (grown.Zone, grown.Machines, grown.NextMachineId);
        var edits = grown.Edits;
        for (int i = 0; i + Layers < edits.Length; i += Layers + 1)
        {
            int cell = edits[i];
            _base[cell] = Of(s, cell);
            _touched.Add(cell);
            (s.Structure[cell], s.Material[cell], s.Back[cell], s.BackMaterial[cell]) =
                (edits[i + 1], edits[i + 2], edits[i + 3], edits[i + 4]);
        }
        return s;
    }
}
