namespace Stillwake.Sim.Power;

public sealed partial class PowerModule
{
    // Ends are drawn mostly within what the grid has built, near one another, and now and then anywhere.
    private const int Span = Content.Power.MaxSpan + 3;

    /// <summary>
    /// Lays cables between built cells a few apart, a cell anywhere now and then (refused as no node or
    /// too long); moves the ends at a node, or one cable's, to a cell near or anywhere (refused as taut,
    /// no node, no cable there or twice); and cuts one already laid or a pair never cabled.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        int Below(int n) => (int)rng.Below((ulong)n);
        int width = _grid.Width, cells = Math.Max(1, width * _grid.Height);
        var built = _grid.Built;
        int Near(int cell) => cell + (Below(Span) - (Span >> 1)) + width * (Below(Span) - (Span >> 1));
        int InBuilt() => built.IsEmpty ? Below(cells) : (built.Top + Below(built.Height)) * width + built.Left + Below(built.Width);

        if (State.Cables.Count > 0 && Below(3) == 0)
        {
            var cable = State.Cables[Below(State.Cables.Count)];
            int at = Below(6) == 0 ? Below(cells) : Below(2) == 0 ? cable.A : cable.B;
            int to = Near(at);
            var one = Below(2) == 0 ? new Ends(cable.A, cable.B) : null;
            return new Move(at, Below(8) == 0 ? Below(cells) : to, one) { From = from };
        }
        if (State.Cables.Count > 0 && Below(3) == 0)
        {
            var cable = State.Cables[Below(State.Cables.Count)];
            var (a, b) = Below(5) == 0 ? (Below(cells), Below(cells)) : (cable.A, cable.B);
            return new Cut(a, b) { From = from };
        }
        int start = Below(8) == 0 ? Below(cells) : InBuilt();
        int near = Near(start);
        return new Lay(start, Below(8) == 0 ? Below(cells) : near) { From = from };
    }
}
