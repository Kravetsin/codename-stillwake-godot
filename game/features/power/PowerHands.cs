using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Power;

namespace Stillwake.Game.Features.Power;

/// <summary>A powered machine's socket lit by whether the cable dragged reaches it, in cells.</summary>
public readonly record struct SocketLight((double X, double Y) At, Light Light);

/// <summary>
/// The hands at work on cables on the power tab (features/power/mode.ts, 0021, 0030): with the cable in
/// hand, a press on a node lays a cable from it, led on past its reach through supports at the anchors on
/// its way; with no card, a press on a support, or on a cable's end at a machine, carries it; a release
/// lays or moves there. A middle press near a cable cuts it. While dragging, each powered machine's socket is
/// lit by whether the cable reaches it, and the cable runs to the pointer in the colour of what the sim
/// would say, its reason beside it. Plain C#: <c>PowerView</c> draws what it leaves in its properties.
/// </summary>
public sealed class PowerHands
{
    /// <summary>How near a middle press must be to a cable, in cells, to cut it.</summary>
    public const double CutReach = 0.35;

    private Grip? _grip;
    /// <summary>The node the cable being laid runs from now: the last support placed, or where it began.</summary>
    private int? _from;
    /// <summary>The cable being laid, as the rope it will be.</summary>
    private Rope? _rope;
    private List<Laid> _laid = [];
    private readonly List<(double X, double Y)> _laying = [];
    private (int Node, Held End)? _landing;

    /// <summary>The cable's end carried now, and whether it may be let go where it is; null if none.</summary>
    public (Held End, bool Fits)? Carried { get; private set; }

    /// <summary>The cable being laid as it hangs now, in cells, and why it may not be let go where it is; empty if none.</summary>
    public IReadOnlyList<(double X, double Y)> Laying { get; private set; } = [];

    /// <summary>Why the cable being laid or carried may not be let go where the pointer is, an i18n key; null if it may.</summary>
    public string? Why { get; private set; }

    /// <summary>The reason shown beside the pointer, an i18n key: <see cref="Why"/>, but for a cable pointed back at its own node.</summary>
    public string? Reason => Why == "power.sameNode" ? null : Why;

    /// <summary>Each powered machine's socket, lit while a cable is dragged.</summary>
    public IReadOnlyList<SocketLight> Sockets { get; private set; } = [];

    /// <summary>The cable nearest <paramref name="at"/> within <see cref="CutReach"/>, if any: what a middle press there cuts.</summary>
    public static Line? Nearest(IReadOnlyList<Line> lines, (double X, double Y) at)
    {
        (Line Line, double D)? best = null;
        foreach (var line in lines)
        {
            double d = CableShape.DistanceTo(line.Points, at);
            if (d <= CutReach && (best is not { } b || d < b.D)) best = (line, d);
        }
        return best?.Line;
    }

    /// <summary>
    /// One frame: the buttons pressed since the last (the left one only with the cable or the plan in
    /// hand), the middle presses meant for the cables, <paramref name="cuts"/>, the pointer, in cells, and
    /// the cables as drawn, <paramref name="lines"/>. Commands go out through <paramref name="send"/> as
    /// peer <paramref name="peer"/>.
    /// </summary>
    public void Update(Holding holds, Buttons buttons, IReadOnlyList<ButtonEvent> cuts, (double X, double Y)? pointer, IReadOnlyList<Line> lines,
        IGridQueries grid, IPowerQueries power, int peer, Action<Command> send, double elapsedMs)
    {
        int width = grid.Width, height = grid.Height;
        int CellAt((double X, double Y) p)
        {
            int x = (int)Math.Floor(p.X), y = (int)Math.Floor(p.Y);
            return x < 0 || y < 0 || x >= width || y >= height ? -1 : y * width + x;
        }
        bool IsMachine(int node) => grid.MachineAt(node)?.Cell == node;
        // Laid, the cable goes on as the rope it was laid as.
        void Lay(int a, int b)
        {
            send(new Lay(a, b));
            if (_rope != null && power.NodeAt(b) is int to)
                _laid.Add(new Laid(a, to, CableShape.EndOf(grid, a), CableShape.EndOf(grid, to), _rope));
            _rope = null;
        }

        // A middle press cuts the nearest cable within reach.
        foreach (var button in cuts)
            if (button.Down && Nearest(lines, button.At) is { } cut) send(new Cut(cut.A, cut.B));

        // The left button is the cable's, and the plan's with no card: it carries pegs and plugs.
        // Another card's, or an empty hand's, is the building's.
        bool wiring = holds is Holding.Cable or Holding.Plan;
        if (!wiring) (_grip, _from, _rope) = (null, null, null);
        foreach (var button in wiring ? buttons.TakeLeft() : [])
        {
            int cell = CellAt(button.At);
            if (button.Down)
            {
                int? node = cell < 0 ? null : power.NodeAt(cell);
                // With the cable, laid from whatever node is pressed, a peg too; with no card, a peg or a
                // plug is carried and nothing is laid.
                var held = Wiring.GripAt(button.At, lines, node, IsMachine);
                _grip = holds == Holding.Cable
                    ? node is int n ? new LayGrip(n) : null
                    : held is LayGrip ? null : held;
                _from = _grip?.Node;
                continue;
            }
            if (_grip != null && _from is int start && cell >= 0 && power.NodeAt(cell) != start)
            {
                if (_grip is LayGrip)
                {
                    if (power.LayProblem(peer, start, cell) == null) Lay(start, cell);
                }
                else
                {
                    var cable = (_grip as EndGrip)?.Cable;
                    if (power.MoveProblem(_grip.Node, cell, cable) == null)
                    {
                        send(new Move(_grip.Node, cell, cable));
                        if (power.NodeAt(cell) is int landed) _landing = (landed, new Held(_grip.Node, cable, CableShape.EndOf(grid, landed)));
                    }
                }
            }
            (_grip, _from, _rope) = (null, null, null);
        }

        Carried = null;
        Laying = [];
        Why = null;
        Sockets = [];
        if (_grip == null || _from is not int from0 || pointer is not { } at) return;
        int target = CellAt(at);

        // Led past one cable's reach, the cable is held on the way and goes on from there.
        if (_grip is LayGrip)
        {
            var middle = CableShape.MiddleOf(grid, from0);
            if (Math.Sqrt((at.X - middle.X) * (at.X - middle.X) + (at.Y - middle.Y) * (at.Y - middle.Y)) > Content.Power.MaxSpan)
            {
                int start = from0;
                var support = Wiring.SupportOnWay(middle, at, width,
                    c => power.NodeAt(c) is int n && n != start && power.LayProblem(peer, start, c) == null);
                if (support is int s && power.NodeAt(s) is int node)
                {
                    Lay(start, s);
                    _from = node;
                }
            }
        }
        int from = _from!.Value;
        var grip = _grip;
        string? ProblemAt(int cell) =>
            cell < 0 ? "power.noNode"
            : grip is LayGrip ? power.LayProblem(peer, from, cell)
            : power.MoveProblem(grip.Node, cell, (grip as EndGrip)?.Cable);

        // Each powered machine's socket lit by whether the cable reaches it: where it would plug in.
        Sockets = grid.Machines
            .Where(m => Machines.Get(m.Kind).Plug != null && m.Cell != from)
            .Select(m => new SocketLight(CableShape.EndOf(grid, m.Cell), Wiring.LightOf(ProblemAt(m.Cell))))
            .ToList();

        Why = ProblemAt(target);
        // Where it would go, it runs to where it would end: a machine's socket, an anchor's middle.
        int? goesTo = Why != null ? null : power.NodeAt(target);
        var to = goesTo is int g ? CableShape.EndOf(grid, g) : at;
        if (grip is LayGrip)
        {
            // The cable laid is a rope already, hanging from where it runs to the hand.
            var p = CableShape.EndOf(grid, from);
            _rope ??= CableRopes.LayingRope(p);
            _rope.Update(p, to, elapsedMs, CableRopes.HangFor);
            _laying.Clear();
            _rope.DrawnInto(_laying);
            Laying = _laying;
        }
        else
        {
            // A cable carried is the cable itself, its end in hand.
            Carried = (new Held(grip.Node, (grip as EndGrip)?.Cable, to), Why == null);
        }
    }

    /// <summary>The node a cable was last carried to and its end there, once: the cables' ropes land it.</summary>
    public (int Node, Held End)? TakeLanding()
    {
        var taken = _landing;
        _landing = null;
        return taken;
    }

    /// <summary>The cables laid since asked, with the ropes they were laid as: the cables' ropes take them.</summary>
    public IReadOnlyList<Laid> TakeLaid()
    {
        var taken = _laid;
        _laid = [];
        return taken;
    }

    /// <summary>Lets go of whatever is held: the overlay turned off mid-drag.</summary>
    public void Release()
    {
        (_grip, _from, _rope) = (null, null, null);
        Carried = null;
        Laying = [];
        Why = null;
        Sockets = [];
    }
}
