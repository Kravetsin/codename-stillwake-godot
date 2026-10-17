namespace Stillwake.Sim.Grid;

/// <summary>A grid's layers, a bit each: which of a cell's a change moved.</summary>
[Flags]
public enum GridLayers : byte
{
    None = 0,
    Structure = 1,
    /// <summary>A wall's material.</summary>
    Material = 2,
    Conduit = 4,
    Back = 8,
    BackMaterial = 16,
    /// <summary>The machine covering the cell: one came, went or took another's place.</summary>
    Machine = 32,
}

/// <summary>A cell whose layers differ from what they were at a version, and which of them do.</summary>
public readonly record struct CellChange(int Cell, GridLayers Layers);

/// <summary>
/// Every write to the grid's layers, oldest first, as the version it was made under, the cell, the layer and
/// what the cell held before (0050). A ring: once full, the oldest write goes, and the versions it covered
/// can no longer be answered. Derived: never saved, empty after a load.
/// </summary>
internal sealed class ChangeLog
{
    /// <summary>How many writes the log holds: a door, a breach or a hero's build is a handful.</summary>
    internal const int DefaultCapacity = 1 << 16;

    private readonly int[] _version, _cell, _old;
    private readonly byte[] _layer;
    private readonly int _mask;
    /// <summary>The writes ever added, and those dropped: the ring holds those between.</summary>
    private long _added, _dropped;
    /// <summary>The log answers for every version from this one on: the writes after it are all still held.</summary>
    internal int Floor { get; private set; }
    /// <summary>Scratch for <see cref="Since"/>: a key a write, sorted.</summary>
    private long[] _keys = [];

    /// <param name="capacity">A power of two, below 2²⁴.</param>
    internal ChangeLog(int capacity, int floor)
    {
        if (capacity <= 0 || (capacity & (capacity - 1)) != 0 || capacity >= 1 << 24)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "a power of two below 2^24");
        (_version, _cell, _old, _layer, _mask, Floor) = (new int[capacity], new int[capacity], new int[capacity], new byte[capacity], capacity - 1, floor);
    }

    /// <summary>Records that <paramref name="cell"/>'s <paramref name="layer"/> held <paramref name="old"/> before a write under <paramref name="version"/>.</summary>
    internal void Add(int version, int cell, GridLayers layer, int old)
    {
        if (_added - _dropped == _version.Length)
        {
            // The oldest write goes: a reader from before it can no longer be told what changed.
            Floor = _version[(int)(_dropped & _mask)];
            _dropped++;
        }
        int at = (int)(_added++ & _mask);
        (_version[at], _cell[at], _layer[at], _old[at]) = (version, cell, (byte)layer, old);
    }

    /// <summary>
    /// The cells whose layers differ now, as <paramref name="now"/> reads them, from what they held at
    /// <paramref name="since"/>, ascending, each with the layers that do; null when the log no longer reaches back to it.
    /// </summary>
    internal List<CellChange>? Since(int since, Func<int, GridLayers, int> now)
    {
        if (since < Floor) return null;
        // The first write made after the version: versions only grow along the log.
        long low = _dropped, high = _added;
        while (low < high)
        {
            long mid = low + ((high - low) >> 1);
            if (_version[(int)(mid & _mask)] <= since) low = mid + 1;
            else high = mid;
        }
        int n = (int)(_added - low);
        var changes = new List<CellChange>();
        if (n == 0) return changes;
        if (_keys.Length < n) _keys = new long[Math.Max(n, _keys.Length * 2)];
        // By cell, then layer, then write: the first write of each cell's layer holds what it was then.
        for (int i = 0; i < n; i++)
        {
            int at = (int)((low + i) & _mask);
            _keys[i] = ((long)_cell[at] << 32) | ((long)LayerIndex(_layer[at]) << 24) | (uint)i;
        }
        Array.Sort(_keys, 0, n);
        int cell = -1;
        var layers = GridLayers.None;
        for (int i = 0; i < n; i++)
        {
            long key = _keys[i];
            if (i > 0 && key >> 24 == _keys[i - 1] >> 24) continue;
            int c = (int)(key >> 32), at = (int)((low + (key & 0xffffff)) & _mask);
            var layer = (GridLayers)_layer[at];
            if (c != cell)
            {
                if (layers != GridLayers.None) changes.Add(new CellChange(cell, layers));
                (cell, layers) = (c, GridLayers.None);
            }
            if (now(c, layer) != _old[at]) layers |= layer;
        }
        if (layers != GridLayers.None) changes.Add(new CellChange(cell, layers));
        return changes;
    }

    private static int LayerIndex(byte layer)
    {
        int index = 0;
        while ((layer >> index) != 1) index++;
        return index;
    }
}
