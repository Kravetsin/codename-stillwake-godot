namespace Stillwake.Sim.Liquids;

/// <summary>
/// What a save holds of the liquid: the cells that differ from its base, worked out from the cells written since (as the
/// grid's edits, 0077), never by looking at every cell, for a save is hashed once a second.
/// </summary>
public sealed partial class LiquidsModule
{
    /// <summary>Derived: what each cell written since the base held then, and those cells in the order first written.</summary>
    private readonly Dictionary<int, byte> _base = new();
    private readonly List<int> _touched = [];

    /// <summary>Notes that <paramref name="cell"/> held <paramref name="was"/> before its first write.</summary>
    private void Touch(int cell, byte was)
    {
        if (_base.TryAdd(cell, was)) _touched.Add(cell);
    }

    /// <summary>The cells differing from the base, ascending, each with its byte.</summary>
    private int[] Edits()
    {
        if (State.Layer is not { } layer) return [];
        var cells = _touched.ToArray();
        Array.Sort(cells);
        var edits = new List<int>();
        foreach (int cell in cells)
        {
            if (layer[cell] == _base[cell]) continue;
            edits.Add(cell);
            edits.Add(layer[cell]);
        }
        return [.. edits];
    }
}
