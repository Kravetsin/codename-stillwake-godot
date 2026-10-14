namespace Stillwake.Sim.Power;

public sealed partial class PowerModule
{
    // The module cannot see the grid. The generic suites fuzz it on the wreck, so ends are drawn mostly
    // from the cells where it has something built, near one another, and now and then anywhere.
    private static readonly int FuzzWidth = Content.Wreck.Rows[0].Length;
    private static readonly int FuzzCells = FuzzWidth * Content.Wreck.Rows.Count;
    private static readonly int[] Built = Content.Wreck.Rows
        .SelectMany((row, y) => row.Select((c, x) => (c, cell: y * FuzzWidth + x)))
        .Where(t => t.c != ' ').Select(t => t.cell).ToArray();
    private const int Span = Content.Power.MaxSpan + 3;

    /// <summary>
    /// Lays cables between built cells a few apart, a cell anywhere now and then (refused as no node or
    /// too long); moves the ends at a node, or one cable's, to a cell near or anywhere (refused as taut,
    /// no node, no cable there or twice); and cuts one already laid or a pair never cabled.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        int Below(int n) => (int)rng.Below((ulong)n);
        int Near(int cell) => cell + (Below(Span) - (Span >> 1)) + FuzzWidth * (Below(Span) - (Span >> 1));

        if (State.Cables.Count > 0 && Below(3) == 0)
        {
            var cable = State.Cables[Below(State.Cables.Count)];
            int at = Below(6) == 0 ? Below(FuzzCells) : Below(2) == 0 ? cable.A : cable.B;
            int to = Near(at);
            var one = Below(2) == 0 ? new Ends(cable.A, cable.B) : null;
            return new Move(at, Below(8) == 0 ? Below(FuzzCells) : to, one) { From = from };
        }
        if (State.Cables.Count > 0 && Below(3) == 0)
        {
            var cable = State.Cables[Below(State.Cables.Count)];
            var (a, b) = Below(5) == 0 ? (Below(FuzzCells), Below(FuzzCells)) : (cable.A, cable.B);
            return new Cut(a, b) { From = from };
        }
        int start = Below(8) == 0 ? Below(FuzzCells) : Built[Below(Built.Length)];
        int near = Near(start);
        return new Lay(start, Below(8) == 0 ? Below(FuzzCells) : near) { From = from };
    }
}
