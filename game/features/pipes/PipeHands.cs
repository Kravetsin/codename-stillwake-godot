using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Pipes;

namespace Stillwake.Game.Features.Pipes;

/// <summary>
/// The hands at work on pipes with the wrench (0109): a left drag lays a span from the cell pressed to the cell let go,
/// a machine's cell standing for its nearest port; a left click with Ctrl turns the joint there into a valve, or the valve
/// back; a right press takes the span nearest it away. While dragging, the span runs from its start to the pointer in the
/// colour of what the sim would say, its reason beside it. Plain C#: <c>PipesOverlay</c> draws what it leaves here.
/// </summary>
public sealed class PipeHands
{
    /// <summary>How near a right press must be to a span, in cells, to take it away.</summary>
    public const double CutReach = 0.7;

    private int? _from;

    /// <summary>The span being laid, its ends' cells, and why it may not be laid as it is; null while none is.</summary>
    public (int From, int To, string? Why)? Laying { get; private set; }

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
    /// One frame with the wrench held: the left presses and releases since the last, <paramref name="ctrl"/> whether Ctrl is
    /// held, the right presses, <paramref name="cuts"/>, and the pointer, in cells. Commands go out through
    /// <paramref name="send"/> as peer <paramref name="peer"/>.
    /// </summary>
    public void Update(IReadOnlyList<ButtonEvent> left, bool ctrl, IReadOnlyList<ButtonEvent> cuts, (double X, double Y)? pointer,
        IGridQueries grid, IPipesQueries pipes, int peer, Action<Command> send)
    {
        foreach (var button in cuts)
            if (button.Down && Nearest(pipes.Spans, grid.Width, button.At) is { } cut) send(new CutPipe(cut.A, cut.B));
        foreach (var button in left)
        {
            int cell = CellAt(grid, button.At);
            if (button.Down)
            {
                _from = null;
                if (cell < 0) continue;
                // With Ctrl, a click turns a joint into a valve and a valve back; nothing is laid.
                if (ctrl) send(new MakeValve(cell, pipes.ValveAt(cell) == null));
                else _from = cell;
                continue;
            }
            if (_from is int start && cell >= 0 && cell != start && pipes.LayProblem(peer, start, cell) == null) send(new LayPipe(start, cell));
            _from = null;
        }
        Laying = _from is int from && pointer is { } at && CellAt(grid, at) is var to and >= 0
            ? (from, to, to == from ? null : pipes.LayProblem(peer, from, to))
            : null;
    }

    /// <summary>Lets go of whatever is held: the wrench put away mid-drag.</summary>
    public void Release() => (_from, Laying) = (null, null);
}
