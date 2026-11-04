using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Pipes;

namespace Stillwake.Game.Features.Pipes;

/// <summary>
/// The hands at work on pipes, the plan piping (0109): a left drag lays a span from the cell pressed to the cell let go,
/// a machine's cell standing for its nearest port, jointed on the way past a span's reach and cut short of the tubes in hand; a
/// right press takes the span nearest it away; with the menu's valve card
/// (0110) the buttons make and take valves instead. While dragging, the span runs from its start to the pointer in the
/// colour of what the sim would say, its reason beside it, and the ports it may end at are lit (2026-10-29-typed-ports). Plain C#:
/// <c>PipesOverlay</c> draws what it leaves here.
/// </summary>
public sealed class PipeHands
{
    /// <summary>How near a right press must be to a span, in cells, to take it away.</summary>
    public const double CutReach = 0.7;

    private int? _from, _joint;
    private (int Start, int Pipes, int Grid)? _litAt;

    /// <summary>The span being laid, its ends' cells, and why it may not be laid as it is; null while none is.</summary>
    public (int From, int To, string? Why)? Laying { get; private set; }

    /// <summary>
    /// While a span is laid, the ports it may end at: every port the sim's <c>FluidProblem</c> lets it reach from its start, but the
    /// start's machine's own. Length and cost light nothing off. Found as the drag starts and again when the pipes or the grid move.
    /// </summary>
    public IReadOnlyList<int> Lit { get; private set; } = [];

    /// <summary>The cell a press at <paramref name="at"/>, in cells, works on: a machine's nearest port on its cells, else the cell; -1 off the grid.</summary>
    public static int CellAt(IGridQueries grid, (double X, double Y) at)
    {
        int x = (int)Math.Floor(at.X), y = (int)Math.Floor(at.Y);
        if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height) return -1;
        int cell = y * grid.Width + x;
        if (grid.MachineAt(cell) is not { } machine || grid.PortsOf(machine) is not { Count: > 0 } ports) return cell;
        int best = ports[0];
        double Far(int c) => (c % grid.Width + 0.5 - at.X) * (c % grid.Width + 0.5 - at.X) + (c / grid.Width + 0.5 - at.Y) * (c / grid.Width + 0.5 - at.Y);
        foreach (int port in ports)
            if (Far(port) < Far(best)) best = port;
        return best;
    }

    /// <summary>How far <paramref name="at"/> is from the span between cells <paramref name="a"/> and <paramref name="b"/>' middles, in cells.</summary>
    public static double DistanceTo(int a, int b, int width, (double X, double Y) at)
    {
        double ax = a % width + 0.5, ay = a / width + 0.5, bx = b % width + 0.5, by = b / width + 0.5;
        double dx = bx - ax, dy = by - ay, t = dx * dx + dy * dy == 0 ? 0 : Math.Clamp(((at.X - ax) * dx + (at.Y - ay) * dy) / (dx * dx + dy * dy), 0, 1);
        double px = ax + t * dx - at.X, py = ay + t * dy - at.Y;
        return Math.Sqrt(px * px + py * py);
    }

    /// <summary>The span nearest <paramref name="at"/> within <see cref="CutReach"/>, if any: what a right press there takes away.</summary>
    public static PipeSpan? Nearest(IReadOnlyList<PipeSpan> spans, int width, (double X, double Y) at)
    {
        (PipeSpan Span, double D)? best = null;
        foreach (var span in spans)
        {
            double d = DistanceTo(span.A, span.B, width, at);
            if (d <= CutReach && (best is not { } b || d < b.D)) best = (span, d);
        }
        return best?.Span;
    }

    /// <summary>
    /// One frame of the plan piping: the left presses and releases since the last, <paramref name="valves"/> whether its menu's
    /// valve card is picked (0110), the right presses, <paramref name="cuts"/>, and the pointer, in cells. With the pipe card the left
    /// lays and the right takes the nearest span away; with the valve card the left makes a valve on the joint pressed and the
    /// right takes one off. Commands go out through <paramref name="send"/> as peer <paramref name="peer"/>.
    /// </summary>
    public void Update(IReadOnlyList<ButtonEvent> left, bool valves, IReadOnlyList<ButtonEvent> cuts, (double X, double Y)? pointer,
        IGridQueries grid, IPipesQueries pipes, int peer, Action<Command> send)
    {
        foreach (var button in cuts)
        {
            if (!button.Down) continue;
            if (!valves && Nearest(pipes.Spans, grid.Width, button.At) is { } cut) send(new CutPipe(cut.A, cut.B));
            else if (valves && CellAt(grid, button.At) is >= 0 and int at && pipes.ValveAt(at) != null) send(new MakeValve(at, false));
        }
        foreach (var button in left)
        {
            int cell = CellAt(grid, button.At);
            if (button.Down)
            {
                _from = null;
                if (cell < 0) continue;
                if (valves)
                {
                    if (pipes.ValveAt(cell) == null) send(new MakeValve(cell, true));
                }
                else _from = cell;
                continue;
            }
            if (_from is int start && cell >= 0 && EndOf(start, cell, button.At, grid, pipes, peer) is (int stop, null) && stop != start)
                send(new LayPipe(start, stop));
            (_from, _joint) = (null, null);
        }
        // Led past a span's reach, the pipe is jointed on the way and goes on from there, as a cable is held (0108); the next joint
        // waits for the sim to lay the last, so the tubes it is reckoned from have been paid.
        if (_joint is int j && pipes.Spans.Any(s => s.A == j || s.B == j)) _joint = null;
        if (!valves && _joint == null && _from is int from0 && pointer is { } led && CellAt(grid, led) is >= 0 and int far
            && PipesModule.LengthOf(grid.Width, from0, far) > Stillwake.Content.Piping.MaxSpan
            && WayBack.Nearest(Middle(from0, grid.Width), led, grid.Width, c => Fits(from0, c, grid, pipes, peer)) is int joint)
        {
            send(new LayPipe(from0, joint));
            _from = _joint = joint;
        }
        if (_from is int lit) Lit = LitFrom(lit, grid, pipes);
        else (Lit, _litAt) = ([], null);
        Laying = _from is int from && pointer is { } p && CellAt(grid, p) is var to and >= 0
            ? to == from ? (from, to, null) : EndOf(from, to, p, grid, pipes, peer) is var (until, why) ? (from, until, why) : null
            : null;
        Hovered = !valves && Laying == null && pointer is { } over ? Nearest(pipes.Spans, grid.Width, over) : null;
    }

    /// <summary>The refusals a span is cut short for, to the farthest cell on its way that takes it: too long, short of tubes, out of the zone.</summary>
    private static readonly string[] CutShort = ["pipes.tooLong", "items.notEnough", "pipes.outsideZone"];

    /// <summary>
    /// Where a span from <paramref name="start"/> led to <paramref name="to"/> (the pointer at <paramref name="at"/>) ends, and why it
    /// is refused: at <paramref name="to"/> if the sim takes it; refused only for its reach or the tubes in hand, at the cell on the
    /// way back nearest the pointer that takes it; else at <paramref name="to"/> with the sim's reason.
    /// </summary>
    private static (int End, string? Why) EndOf(int start, int to, (double X, double Y) at, IGridQueries grid, IPipesQueries pipes, int peer)
    {
        string? why = pipes.LayProblem(peer, start, to);
        if (why == null) return (to, null);
        if (Array.IndexOf(CutShort, why) >= 0
            && WayBack.Nearest(Middle(start, grid.Width), at, grid.Width, c => Fits(start, c, grid, pipes, peer)) is int end)
            return (end, null);
        return (to, why);
    }

    /// <summary>Whether a span from <paramref name="start"/> may end at <paramref name="cell"/> as a joint or a port, never inside a machine elsewhere.</summary>
    private static bool Fits(int start, int cell, IGridQueries grid, IPipesQueries pipes, int peer) =>
        cell != start && cell >= 0 && cell < grid.Width * grid.Height
        && PipesModule.LengthOf(grid.Width, start, cell) <= Stillwake.Content.Piping.MaxSpan
        && (grid.MachineAt(cell) is not { } machine || grid.PortsOf(machine).Contains(cell))
        && pipes.LayProblem(peer, start, cell) == null;

    private static (double X, double Y) Middle(int cell, int width) => (cell % width + 0.5, cell / width + 0.5);

    /// <summary>The span the right button would take away now, with the pipe card in hand and nothing being laid: drawn lit.</summary>
    public PipeSpan? Hovered { get; private set; }

    /// <summary>Lets go of whatever is held: piping left mid-drag.</summary>
    public void Release() => (_from, _joint, Laying, Hovered, Lit, _litAt) = (null, null, null, null, [], null);

    private IReadOnlyList<int> LitFrom(int start, IGridQueries grid, IPipesQueries pipes)
    {
        if (_litAt == (start, pipes.Version, grid.Version)) return Lit;
        _litAt = (start, pipes.Version, grid.Version);
        int? own = grid.MachineAt(start)?.Id;
        var lit = new List<int>();
        foreach (var machine in grid.Machines)
            if (machine.Id != own)
                foreach (int port in grid.PortsOf(machine))
                    if (pipes.FluidProblem(start, port) == null) lit.Add(port);
        return lit;
    }
}
