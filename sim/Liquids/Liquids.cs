using System.Text.Json;
using System.Text.Json.Serialization;
using Stillwake.Sim.Grid;
using L = Stillwake.Content.Liquids;

namespace Stillwake.Sim.Liquids;

/// <summary>The liquids' slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class LiquidsState
{
    /// <summary>The ticks the liquid has run: the sweep's way along a row turns with it, and modules have no clock of their own.</summary>
    [JsonInclude] internal int Clock { get; set; }

    /// <summary>
    /// What a save holds of the liquid: the cells that differ from its base (the liquid a grown surface grew with, none on a
    /// drawn one), ascending, each as <c>cell, byte</c>. Written only by a save; empty while the world runs.
    /// </summary>
    [JsonInclude] internal int[] Cells { get; set; } = [];

    /// <summary>The liquid, a byte a cell (<c>Content.Liquids.Of</c>); null on a surface that has held none.</summary>
    [JsonIgnore] internal byte[]? Layer { get; set; }
}

/// <summary>What any module or the client may read of the liquid.</summary>
public interface ILiquidsQueries
{
    /// <summary>The units of liquid in <paramref name="cell"/>, 0 to <c>Content.Liquids.Full</c>.</summary>
    int AmountAt(int cell);

    /// <summary>Every cell's byte (<c>Content.Liquids.Of</c>), row by row; empty on a surface that has held no liquid.</summary>
    ReadOnlySpan<byte> Layer { get; }

    /// <summary>
    /// The cell nearest <paramref name="cell"/> (by squared distance, the lower on a tie, then the left) that liquid fell into
    /// last tick, within <paramref name="reach"/> cells either way; null for none: where a stream is heard.
    /// </summary>
    int? FallNear(int cell, int reach);
}

/// <summary>
/// Liquids (liquids): water in the cells of a surface, falling and spreading till it rests (flowing-liquids). Only
/// what can move costs a tick: squares of <c>Content.Liquids.Chunk</c> cells sleep while none of their cells can.
/// </summary>
public sealed partial class LiquidsModule : Module<LiquidsState>, ILiquidsQueries
{
    private readonly IGridQueries _grid;
    private int _width, _height;

    public override string Id => "liquids";

    public LiquidsModule(IGridQueries grid)
    {
        _grid = grid;
        System(Phase.Simulation, Tick);
    }

    protected override LiquidsState Init(Layout layout)
    {
        (_width, _height) = (layout.Width, layout.Height);
        Forget();
        var s = new LiquidsState();
        if (layout.Liquids is not { } drawn || !Wet(drawn)) return s;
        s.Layer = [.. drawn];
        // A grown surface's base is its ground's liquid, grown again at a load; a drawn one's is none, so a save holds it all.
        if (!layout.Grown)
            for (int cell = 0; cell < s.Layer.Length; cell++)
                if (s.Layer[cell] != L.None) Touch(cell, L.None);
        return s;
    }

    protected override object Save() => new LiquidsState { Clock = State.Clock, Cells = Edits() };

    protected override LiquidsState? Load(JsonElement saved, JsonSerializerOptions options, Layout? ground)
    {
        var s = saved.Deserialize<LiquidsState>(options);
        if (s == null) return null;
        // The grid restores first: its size is the surface's.
        (_width, _height) = (_grid.Width, _grid.Height);
        Forget();
        if (ground?.Liquids is { } grown && Wet(grown)) s.Layer = [.. grown];
        var cells = s.Cells;
        s.Cells = [];
        for (int i = 0; i + 1 < cells.Length; i += 2)
        {
            s.Layer ??= new byte[_width * _height];
            int cell = cells[i];
            Touch(cell, s.Layer[cell]);
            s.Layer[cell] = (byte)cells[i + 1];
        }
        return s;
    }

    public override void Rebuild()
    {
        _version = _grid.Version;
        _falls.Clear();
        if (State.Layer == null) return;
        _chunksAcross = (_width + L.Chunk - 1) / L.Chunk;
        _awake = new bool[_chunksAcross * ((_height + L.Chunk - 1) / L.Chunk)];
        _stirred = new bool[_awake.Length];
        _awakeInRow = new int[(_height + L.Chunk - 1) / L.Chunk];
        Recheck();
    }

    private static bool Wet(IReadOnlyList<byte> cells)
    {
        for (int cell = 0; cell < cells.Count; cell++)
            if (cells[cell] != L.None) return true;
        return false;
    }

    public int AmountAt(int cell) => State.Layer is { } l && (uint)cell < (uint)l.Length ? L.AmountOf(l[cell]) : 0;

    public ReadOnlySpan<byte> Layer => State.Layer;

    public int? FallNear(int cell, int reach)
    {
        int x = cell % _width, y = cell / _width;
        int? best = null;
        long bestDistance = long.MaxValue;
        foreach (int fell in _falls)
        {
            int dx = fell % _width - x, dy = fell / _width - y;
            if (Math.Abs(dx) > reach || Math.Abs(dy) > reach) continue;
            long distance = (long)dx * dx + (long)dy * dy;
            // The lower cell on a tie, then the left: a larger index is lower, a smaller one in the row further left.
            if (distance < bestDistance || distance == bestDistance && (fell / _width > best!.Value / _width
                || fell / _width == best.Value / _width && fell < best.Value))
                (best, bestDistance) = (fell, distance);
        }
        return best;
    }
}
