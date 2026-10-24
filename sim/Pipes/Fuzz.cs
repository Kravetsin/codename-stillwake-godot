namespace Stillwake.Sim.Pipes;

public sealed partial class PipesModule
{
    // Ends are drawn mostly within what the grid has built, near one another, and now and then anywhere.
    private const int Reach = Content.Piping.MaxSpan + 3;

    /// <summary>
    /// Lays spans between cells a few apart, mostly from a machine's port or a joint already there, a cell anywhere now and then
    /// (refused off the grid, too long, twice, on a machine but its port); cuts one laid or a pair never piped; makes and unmakes
    /// valves on joints, ports and empty cells; opens and shuts valves, and cells with none.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        int Below(int n) => (int)rng.Below((ulong)n);
        int width = _grid.Width, cells = Math.Max(1, width * _grid.Height);
        var built = _grid.Built;
        int Near(int cell) => cell + (Below(Reach) - (Reach >> 1)) + width * (Below(Reach) - (Reach >> 1));
        int InBuilt() => built.IsEmpty ? Below(cells) : (built.Top + Below(built.Height)) * width + built.Left + Below(built.Width);
        int End()
        {
            if (State.Spans.Count > 0 && Below(2) == 0)
            {
                var span = State.Spans[Below(State.Spans.Count)];
                return Below(2) == 0 ? span.A : span.B;
            }
            var machines = _grid.Machines;
            if (machines.Count > 0 && Below(2) == 0 && _grid.PortsOf(machines[Below(machines.Count)]) is { Count: > 0 } ports) return ports[Below(ports.Count)];
            return Below(8) == 0 ? Below(cells + 2) - 1 : InBuilt();
        }
        switch (Below(6))
        {
            case 0 when State.Spans.Count > 0:
                {
                    var span = State.Spans[Below(State.Spans.Count)];
                    var (a, b) = Below(5) == 0 ? (Below(cells), Below(cells)) : (span.A, span.B);
                    return new CutPipe(a, b) { From = from };
                }
            case 1:
                {
                    int cell = State.Valves.Count > 0 && Below(2) == 0 ? State.Valves[Below(State.Valves.Count)].Cell : End();
                    return new MakeValve(cell, Below(3) != 0) { From = from };
                }
            case 2:
                {
                    int cell = State.Valves.Count > 0 && Below(4) != 0 ? State.Valves[Below(State.Valves.Count)].Cell : End();
                    return new SetValve(cell, Below(2) == 0) { From = from };
                }
            default:
                {
                    int start = End();
                    return new LayPipe(start, Below(8) == 0 ? Below(cells) : Below(3) == 0 ? End() : Near(start)) { From = from };
                }
        }
    }
}
