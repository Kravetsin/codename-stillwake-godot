using System.Text.Json.Serialization;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Pipes;

/// <summary>A pipe network: its smallest node cell, its node cells (ports and carrying joints) in cell order, the machines on it by id.</summary>
public sealed record Network(int Id, IReadOnlyList<int> Cells, IReadOnlyList<int> Machines);

/// <summary>A straight pipe between node cells <c>A</c> and <c>B</c>, the lower first; <c>Paid</c> when its tubes were taken.</summary>
public sealed record PipeSpan(int A, int B, bool Paid);

/// <summary>A joint at <c>Cell</c> that opens and closes; <c>Paid</c> when its cost was taken.</summary>
public sealed record Valve(int Cell, bool Open, bool Paid);

/// <summary>Lays a straight pipe from cell <c>A</c> to cell <c>B</c>, paid by the sender: each end a machine's port or a joint, on any cell.</summary>
public sealed record LayPipe(int A, int B) : Command;

/// <summary>Takes the pipe between cells <c>A</c> and <c>B</c> away, giving back what was paid; a valve left on no pipe goes with it.</summary>
public sealed record CutPipe(int A, int B) : Command;

/// <summary>Turns the joint at <c>Cell</c> into a valve, open, or (<c>Valve</c> false) the valve there back into a joint, refunded.</summary>
public sealed record MakeValve(int Cell, bool Valve) : Command;

/// <summary>Opens (<c>Open</c>) or shuts the valve at <c>Cell</c>: what its sender saw it should become, as a door's <c>SetDoor</c>.</summary>
public sealed record SetValve(int Cell, bool Open) : Command;

/// <summary>The pipes' slice: spans and valves saved, the networks derived.</summary>
public sealed class PipesState
{
    /// <summary>Every span, ordered by its ends.</summary>
    [JsonInclude] internal List<PipeSpan> Spans { get; set; } = [];
    /// <summary>Every valve, by cell.</summary>
    [JsonInclude] internal List<Valve> Valves { get; set; } = [];
    /// <summary>Derived: per node cell, its network's id; closed valves and cells with no span are in none.</summary>
    [JsonIgnore] internal Dictionary<int, int> NetworkOf { get; set; } = new();
    [JsonIgnore] internal List<Network> Networks { get; set; } = [];
    /// <summary>Derived: the grid's version last looked at: its log names the cells changed since.</summary>
    [JsonIgnore] internal int GridVersion { get; set; }
    /// <summary>Derived: bumped each time the networks are found again.</summary>
    [JsonIgnore] internal int Version { get; set; }
    /// <summary>Derived: whether a span or a valve changed since the networks were found.</summary>
    [JsonIgnore] internal bool Stale { get; set; } = true;
}

/// <summary>What any module may read of the pipes. Read networks at or after derivation.</summary>
public interface IPipesQueries
{
    /// <summary>The network of the node at <paramref name="cell"/>; null for no span there, a closed valve, or off the grid.</summary>
    int? NetworkAt(int cell);
    /// <summary>The networks machine <paramref name="machineId"/> has a port on, in id order.</summary>
    IReadOnlyList<int> NetworksOf(int machineId);
    Network? Network(int id);
    /// <summary>Every network, in id order.</summary>
    IReadOnlyList<Network> Networks { get; }
    /// <summary>Bumped each time the networks are found again, on any change of a span, a valve or a machine; never saved.</summary>
    int Version { get; }
    /// <summary>Every span, ordered by its ends: live and read-only.</summary>
    IReadOnlyList<PipeSpan> Spans { get; }
    /// <summary>Every valve, by cell: live and read-only.</summary>
    IReadOnlyList<Valve> Valves { get; }
    /// <summary>The valve at <paramref name="cell"/>, or null.</summary>
    Valve? ValveAt(int cell);
    /// <summary>Why peer <paramref name="peer"/>'s <see cref="LayPipe"/> from <paramref name="a"/> to <paramref name="b"/> would be refused; null if it would not.</summary>
    string? LayProblem(int peer, int a, int b);
    /// <summary>Why peer <paramref name="peer"/>'s <see cref="MakeValve"/> would be refused; null if it would not.</summary>
    string? ValveProblem(int peer, int cell, bool valve);
    /// <summary>
    /// Whether each valve the <see cref="SetValve"/>s among <paramref name="toggles"/> name would be open, in order, skipping
    /// those refused: what a client shows of its own toggles before their tick runs (0014). The world is untouched.
    /// </summary>
    IReadOnlyDictionary<int, bool> AfterToggles(IReadOnlyList<Command> toggles);
}

/// <summary>
/// Pipes as spans (0109): straight pipes between machines' ports and joints, laid and taken away by hand with the wrench,
/// and the networks they join, along which air flows between the machines on them.
/// </summary>
public sealed partial class PipesModule : Module<PipesState>, IPipesQueries
{
    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private static readonly Comparison<PipeSpan> ByEnds = (p, q) => p.A != q.A ? p.A.CompareTo(q.A) : p.B.CompareTo(q.B);

    public override string Id => "pipes";

    public PipesModule(IGridQueries grid, IItemsQueries items, IItemsOps itemsOps)
    {
        _grid = grid;
        _items = items;
        _itemsOps = itemsOps;
        Handles<LayPipe>(c => LayProblem(c.From, c.A, c.B), ApplyLay);
        Handles<CutPipe>(CutProblem, ApplyCut);
        Handles<MakeValve>(c => ValveProblem(c.From, c.Cell, c.Valve), ApplyMakeValve);
        Handles<SetValve>(c => ValveAt(c.Cell) == null ? "pipes.noValve" : null, ApplySetValve);
        System(Phase.Derivation, () =>
        {
            bool moved = false;
            if (_grid.Version != State.GridVersion)
            {
                var log = _grid.ChangedSince(State.GridVersion);
                State.GridVersion = _grid.Version;
                moved = log == null || Moved(log);
            }
            if (moved || State.Stale) Rebuild();
        });
    }

    protected override PipesState Init(Layout layout)
    {
        var (spans, valves) = PipeNetworks.FromLayout(layout);
        return new PipesState { Spans = spans, Valves = valves };
    }

    public override void Rebuild()
    {
        var machines = _grid.Machines.Select(m => (m.Id, _grid.PortsOf(m))).ToList();
        (State.NetworkOf, State.Networks) = PipeNetworks.Find(State.Spans, State.Valves, machines);
        State.GridVersion = _grid.Version;
        State.Stale = false;
        State.Version++;
    }

    /// <summary>Whether a machine is among the <paramref name="changes"/>: one came or went where its cells did.</summary>
    private static bool Moved(IReadOnlyList<CellChange> changes)
    {
        foreach (var c in changes)
            if ((c.Layers & GridLayers.Machine) != 0) return true;
        return false;
    }

    /// <summary>
    /// A span's length in cells between cells <paramref name="a"/> and <paramref name="b"/> of a grid <paramref name="width"/>
    /// wide: the square root of their squared distance, an integer, rounded up, so a diagonal of 3 by 4 is 5 and of 1 by 1 is 2.
    /// </summary>
    public static int LengthOf(int width, int a, int b)
    {
        long dx = a % width - b % width, dy = a / width - b / width, squared = dx * dx + dy * dy;
        long root = 0;
        while ((root + 1) * (root + 1) <= squared) root++;
        return (int)(root * root == squared ? root : root + 1);
    }

    /// <summary>What a span <paramref name="length"/> cells long costs: an item for every started <see cref="Content.Piping.CellsPerItem"/>.</summary>
    public static IReadOnlyList<(string Item, int Count)> CostOf(int length) =>
        [(Content.Piping.Item, (length + Content.Piping.CellsPerItem - 1) / Content.Piping.CellsPerItem)];

    // Queries.

    public int? NetworkAt(int cell) => State.NetworkOf.TryGetValue(cell, out int id) ? id : null;

    public IReadOnlyList<int> NetworksOf(int machineId) =>
        State.Networks.Where(n => n.Machines.Contains(machineId)).Select(n => n.Id).ToList();

    public Network? Network(int id) => State.Networks.Find(n => n.Id == id);
    public IReadOnlyList<Network> Networks => State.Networks;
    public int Version => State.Version;
    public IReadOnlyList<PipeSpan> Spans => State.Spans;
    public IReadOnlyList<Valve> Valves => State.Valves;

    public Valve? ValveAt(int cell)
    {
        int at = ValveIndex(cell);
        return at >= 0 ? State.Valves[at] : null;
    }

    private int ValveIndex(int cell)
    {
        int lo = 0, hi = State.Valves.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int at = State.Valves[mid].Cell;
            if (at == cell) return mid;
            if (at < cell) lo = mid + 1;
            else hi = mid - 1;
        }
        return ~lo;
    }

    public IReadOnlyDictionary<int, bool> AfterToggles(IReadOnlyList<Command> toggles)
    {
        var open = new Dictionary<int, bool>();
        foreach (var t in toggles)
            if (t is SetValve v && ValveAt(v.Cell) != null) open[v.Cell] = v.Open;
        return open;
    }

    /// <summary>Whether <paramref name="cell"/> is a port of the machine standing on it.</summary>
    private bool IsPort(int cell) => _grid.MachineAt(cell) is { } m && _grid.PortsOf(m).Contains(cell);

    /// <summary>Whether a span ends at <paramref name="cell"/>, a port or not: a joint if it is no port.</summary>
    private bool Piped(int cell) => State.Spans.Exists(s => s.A == cell || s.B == cell);

    private PipeSpan? SpanAt(int a, int b)
    {
        var (lo, hi) = a < b ? (a, b) : (b, a);
        return State.Spans.Find(s => s.A == lo && s.B == hi);
    }

    /// <summary>
    /// <c>pipes.offGrid</c> for an end off the grid, <c>pipes.sameCell</c>, <c>pipes.outsideZone</c> for an end outside the build
    /// zone, <c>pipes.tooLong</c> past
    /// <see cref="Content.Piping.MaxSpan"/>, <c>pipes.twice</c> for a span already there, then what the items say of its cost.
    /// </summary>
    public string? LayProblem(int peer, int a, int b)
    {
        int size = _grid.Width * _grid.Height;
        if (a < 0 || b < 0 || a >= size || b >= size) return "pipes.offGrid";
        if (a == b) return "pipes.sameCell";
        var zone = _grid.Zone;
        if (!zone.Contains(a % _grid.Width, a / _grid.Width) || !zone.Contains(b % _grid.Width, b / _grid.Width)) return "pipes.outsideZone";
        int length = LengthOf(_grid.Width, a, b);
        if (length > Content.Piping.MaxSpan) return "pipes.tooLong";
        if (SpanAt(a, b) != null) return "pipes.twice";
        return _items.ShortOf(peer, CostOf(length));
    }

    /// <summary>
    /// Making a valve: <c>pipes.noJoint</c> where no span ends or at a machine's port, <c>pipes.already</c> on a valve, then
    /// what the items say of its cost; unmaking one: <c>pipes.noValve</c>, then whether the refund fits.
    /// </summary>
    public string? ValveProblem(int peer, int cell, bool valve)
    {
        var there = ValveAt(cell);
        if (!valve) return there == null ? "pipes.noValve" : there.Paid ? _items.FullFor(peer, Content.Piping.ValveCost) : null;
        if (!Piped(cell) || IsPort(cell)) return "pipes.noJoint";
        if (there != null) return "pipes.already";
        return _items.ShortOf(peer, Content.Piping.ValveCost);
    }

    /// <summary>What cutting <paramref name="span"/> gives back: its tubes if paid, and the valves left on no span, if paid.</summary>
    private List<(string Item, int Count)> RefundOf(PipeSpan span)
    {
        var refund = new List<(string Item, int Count)>();
        if (span.Paid) refund.AddRange(CostOf(LengthOf(_grid.Width, span.A, span.B)));
        foreach (int end in (int[])[span.A, span.B])
            if (ValveAt(end) is { Paid: true } && State.Spans.Count(s => s.A == end || s.B == end) == 1) refund.AddRange(Content.Piping.ValveCost);
        return refund;
    }

    private string? CutProblem(CutPipe c) =>
        SpanAt(c.A, c.B) is not { } span ? "pipes.noPipe" : RefundOf(span) is { Count: > 0 } refund ? _items.FullFor(c.From, refund) : null;

    // Commands.

    private void ApplyLay(LayPipe c)
    {
        var taken = _itemsOps.Take(c.From, CostOf(LengthOf(_grid.Width, c.A, c.B)));
        State.Spans.Add(new PipeSpan(Math.Min(c.A, c.B), Math.Max(c.A, c.B), taken.Count > 0));
        State.Spans.Sort(ByEnds);
        State.Stale = true;
    }

    private void ApplyCut(CutPipe c)
    {
        var span = SpanAt(c.A, c.B)!;
        var refund = RefundOf(span);
        if (refund.Count > 0) _itemsOps.Give(c.From, refund);
        State.Spans.Remove(span);
        State.Valves.RemoveAll(v => !Piped(v.Cell));
        State.Stale = true;
    }

    private void ApplyMakeValve(MakeValve c)
    {
        if (c.Valve)
        {
            var taken = _itemsOps.Take(c.From, Content.Piping.ValveCost);
            State.Valves.Insert(~ValveIndex(c.Cell), new Valve(c.Cell, true, taken.Count > 0));
        }
        else
        {
            int at = ValveIndex(c.Cell);
            if (State.Valves[at].Paid) _itemsOps.Give(c.From, Content.Piping.ValveCost);
            State.Valves.RemoveAt(at);
        }
        State.Stale = true;
    }

    private void ApplySetValve(SetValve c)
    {
        int at = ValveIndex(c.Cell);
        // Already as asked, it is left alone: nothing derived is redone.
        if (State.Valves[at].Open == c.Open) return;
        State.Valves[at] = State.Valves[at] with { Open = c.Open };
        State.Stale = true;
    }
}
