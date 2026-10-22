using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Power;

namespace Stillwake.Game.Features.Power;

/// <summary>
/// Power cables (features/power/index.ts): out of the overlay, thin ropes hanging between their nodes
/// inside the ship, swinging out of a hero's way and drifting loose out in space, drawn behind the ship's
/// structure, with a peg where one ends on a wall, a plug in each socket and a grommet where one goes
/// through the hull; a bolt over each machine short of power, blinking with none. With L, the power
/// overlay: the ship as it is, every cable over it in its network's state, the very ropes the cables
/// hang as, a loose plug over each powered machine with no cable, and beside the pointer on a node what
/// its network makes against what it would draw. Cables are laid, moved and cut there, with the cable or
/// the plan in hand (<see cref="PowerHands"/>, 0022): a cable carried is the real rope, highlighted.
/// </summary>
public partial class PowerView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private const double RefreshMs = 250;
    /// <summary>How fast a bolt over a machine with no power at all blinks, in blinks a second.</summary>
    private const double Blink = 1.5;
    private const float CableWidth = 1.25f;
    /// <summary>A cable out of the overlay: thin and dark, a line against the back wall.</summary>
    private static readonly Color CableColour = AsciiArt.Colour(0x7c8794);
    /// <summary>Where a cable goes through the hull: a dark fitting round it.</summary>
    private static readonly Color Fitting = AsciiArt.Colour(0x1a1e25);
    /// <summary>In the overlay, cables by their network's state: fed in full, short, dark; on none, loose.</summary>
    private static readonly Color Fed = AsciiArt.Colour(0x7fe08a), Short = AsciiArt.Colour(0xf0b44c), Dark = AsciiArt.Colour(0xe05555), Loose = AsciiArt.Colour(0x9aa3ad);
    /// <summary>A cable carried by hand, bright and thicker than the rest, red where it may not be let go.</summary>
    private static readonly Color Carried = Colors.White;
    private const float CarriedWidth = 2.5f;
    /// <summary>What the overlay lays over the ship, so the cables stand out from it.</summary>
    private static readonly Color Fits = AsciiArt.Colour(0x7fe08a), Refused = AsciiArt.Colour(0xe05555), OutOfReach = AsciiArt.Colour(0x9aa3ad);

    private readonly Ui _ui;
    private readonly CableRopes _ropes = new();
    private readonly PowerHands _hands = new();
    // Behind the walls and machines, in front of back walls and pipes; fittings on the wall's face.
    private readonly Sketch _lines = new(Layers.Cables, lit: true), _grommets = new(Layers.Fittings, lit: true);
    private readonly Node2D _ends = new() { ZIndex = Layers.Fittings }, _bolts = new() { ZIndex = Layers.Bolts }, _unplugged = new() { ZIndex = Layers.OverlayMarks };
    private readonly Sketch _overlay = new(Layers.Overlay), _ghost = new(Layers.OverlayHands);
    /// <summary>Each powered machine's socket outlined while a cable is carried: where it would plug in.</summary>
    private readonly Outline _sockets = new(Layers.OverlayHands);
    private IReadOnlyList<CableLine> _drawn = [];
    /// <summary>The bolts over machines with no power at all, which blink: kept, not looked up each frame.</summary>
    private readonly List<Sprite2D> _blinking = [];
    private Dictionary<int, PowerNetwork> _byNode = new();
    private double _since = RefreshMs, _timeMs;
    private int _shownTick = -1;
    private (int W, int H) _size;

    public PowerView(Ui ui)
    {
        _ui = ui;
        Lit.Set(_lines);
        Lit.Set(_grommets);
        foreach (var node in (Node2D[])[_lines, _grommets, _ends, _bolts, _overlay, _unplugged, _ghost, _sockets]) AddChild(node);
        _lines.Paint = g =>
        {
            foreach (var line in _drawn) g.Rope(line.Points, CableColour, CableWidth);
        };
        _grommets.Paint = g =>
        {
            foreach (var line in _drawn)
                foreach (var feed in line.Feeds)
                {
                    var at = new Vector2((float)feed.X, (float)feed.Y) * Cell;
                    g.DrawCircle(at, 2, Fitting);
                    g.DrawCircle(at, 1, CableColour);
                }
        };
        _overlay.Paint = PaintOverlay;
        _ghost.Paint = PaintGhost;
    }

    /// <summary>The power overlay's id: the feature's.</summary>
    public const string Id = "power";

    private bool On => _ui.OverlayOn == Id;

    /// <summary>Whether a middle press at <paramref name="at"/>, in cells, would cut a cable as they hang now.</summary>
    public bool NearCable((double X, double Y) at) =>
        PowerHands.Nearest(_ropes.Lines().Select(l => new Line(l.A, l.B, l.Points)).ToList(), at) != null;

    /// <summary>
    /// One frame: the cables brought up to the world at most every <see cref="RefreshMs"/> once it has run a
    /// tick, the hands on them while the overlay is on, <paramref name="cuts"/> the middle presses that cut,
    /// every rope moved on, brushed by <paramref name="bodies"/>.
    /// </summary>
    public void Frame(Client client, Registry m, Buttons buttons, IReadOnlyList<ButtonEvent> cuts, (double X, double Y)? pointer, IReadOnlyList<Body> bodies, double elapsedMs)
    {
        var grid = m.Grid;
        _size = (grid.Width, grid.Height);
        if ((_since += elapsedMs) >= RefreshMs && client.World.Tick != _shownTick)
        {
            _since = 0;
            _shownTick = client.World.Tick;
            _ropes.Sync(grid, m.Rooms, m.Power.Cables);
            _byNode = m.Power.Networks.SelectMany(n => n.Nodes.Select(node => (node, n))).ToDictionary(p => p.node, p => p.n);
            // Made anew only when what they show changes: nodes made four times a second are garbage for the GC to finalize.
            if (Changed(_boltsShown, BoltsOf(m))) PlaceBolts(m);
            if (Changed(_endsShown, EndsOf())) PlaceEnds(m);
            if (Changed(_unpluggedShown, UnpluggedOf(m))) PlaceUnplugged(m);
        }
        _timeMs += elapsedMs;
        _overlay.Visible = _unplugged.Visible = _ghost.Visible = On;
        // Out of the power overlay, nothing is held; in it, a cable's end may be carried by hand.
        if (!On) _hands.Release();
        else
        {
            var lines = _ropes.Lines().Select(l => new Line(l.A, l.B, l.Points)).ToList();
            _hands.Update(_ui.Hand.Get(), buttons, cuts, pointer, lines, grid, m.Power, client.Id, client.Send, elapsedMs);
            if (_hands.Reason != null) _ui.Reason.Set(_hands.Reason);
            Tell(m, pointer);
        }
        OutlineSockets();
        _ropes.Hold(_hands.Carried?.End);
        if (_hands.TakeLanding() is { } landing) _ropes.Land(landing.Node, landing.End);
        foreach (var laid in _hands.TakeLaid()) _ropes.Adopt(laid);
        _ropes.Update(elapsedMs, bodies);
        _drawn = _ropes.Lines();
        bool blinkOn = (int)Math.Floor(_timeMs / 1000 * Blink * 2) % 2 == 0;
        foreach (var bolt in _blinking) bolt.Modulate = new Color(1, 1, 1, blinkOn ? 1 : 0.25f);
        _lines.QueueRedraw();
        _grommets.QueueRedraw();
        if (On)
        {
            _overlay.QueueRedraw();
            _ghost.QueueRedraw();
        }
    }

    /// <summary>
    /// Beside the pointer on a node, what its network makes against what it would draw, and with stores on it what they
    /// hold of full (0101), the tick's settling counted in.
    /// </summary>
    private void Tell(Registry m, (double X, double Y)? pointer)
    {
        string[] said = [];
        if (pointer is { } p && p.X >= 0 && p.Y >= 0 && p.X < _size.W && p.Y < _size.H
            && m.Power.NodeAt((int)p.Y * _size.W + (int)p.X) is int node && m.Power.NetworkOf(node) is { } network)
        {
            var supply = En.T("power.supply", ("supply", network.Supply), ("demand", network.Demand));
            said = network.Capacity > 0
                ? [supply, En.T("power.stored", ("stored", network.Stored + network.Flow), ("capacity", network.Capacity))]
                : [supply];
        }
        _ui.WorldTip.SetLines(said);
    }

    private readonly List<int> _key = [], _boltsShown = [], _endsShown = [], _unpluggedShown = [];

    /// <summary>Whether <paramref name="now"/> differs from <paramref name="shown"/>, which then takes it.</summary>
    private static bool Changed(List<int> shown, List<int> now)
    {
        if (shown.SequenceEqual(now)) return false;
        shown.Clear();
        shown.AddRange(now);
        return true;
    }

    /// <summary>What the bolts show: each machine short of power, where, and whether it has none.</summary>
    private List<int> BoltsOf(Registry m)
    {
        _key.Clear();
        foreach (var machine in m.Grid.Machines)
            if (Machines.Get(machine.Kind).Draws != null && m.Power.Satisfaction(machine.Id) is var s and < 1000)
            {
                _key.Add(machine.Id);
                _key.Add(machine.Cell);
                _key.Add(s == 0 ? 0 : 1);
            }
        return _key;
    }

    /// <summary>What the pegs and plugs show: the nodes cables end at.</summary>
    private List<int> EndsOf()
    {
        _key.Clear();
        foreach (var line in _ropes.Lines())
        {
            _key.Add(line.A);
            _key.Add(line.B);
        }
        return _key;
    }

    /// <summary>What the loose plugs show: the machines making or drawing power with no cable.</summary>
    private List<int> UnpluggedOf(Registry m)
    {
        _key.Clear();
        foreach (var machine in m.Grid.Machines)
        {
            var spec = Machines.Get(machine.Kind);
            if (spec.Powered && !_byNode.ContainsKey(machine.Cell)) _key.Add(machine.Cell);
        }
        return _key;
    }

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    /// <summary>A bolt over each machine that draws power and gets less than it would: red with none, amber with some.</summary>
    private void PlaceBolts(Registry m)
    {
        foreach (var child in _bolts.GetChildren()) child.QueueFree();
        _blinking.Clear();
        foreach (var machine in m.Grid.Machines)
        {
            var spec = Machines.Get(machine.Kind);
            if (spec.Draws == null) continue;
            int satisfaction = m.Power.Satisfaction(machine.Id);
            if (satisfaction >= 1000) continue;
            var at = CableShape.MiddleOf(m.Grid, machine.Cell);
            var bolt = new Sprite2D
            {
                Texture = AsciiArt.Bolt(satisfaction == 0),
                Offset = new Vector2(0, -AsciiArt.Bolt(true).GetHeight() / 2f),
                Position = new Vector2((float)at.X * Cell, (float)(at.Y - spec.Height / 2.0) * Cell - 1),
                Material = Unlit,
            };
            if (satisfaction == 0) _blinking.Add(bolt);
            _bolts.AddChild(bolt);
        }
    }

    /// <summary>At each end of every cable, a plug in a machine's socket or a peg on a wall, one for every node however many cables end there.</summary>
    private void PlaceEnds(Registry m)
    {
        foreach (var child in _ends.GetChildren()) child.QueueFree();
        var nodes = _ropes.Lines().SelectMany(l => new[] { l.A, l.B }).Distinct();
        foreach (int node in nodes)
        {
            bool machine = m.Grid.MachineAt(node)?.Cell == node;
            var at = CableShape.EndOf(m.Grid, node);
            var end = new Sprite2D { Texture = machine ? AsciiArt.Plug() : AsciiArt.Peg(), Position = new Vector2((float)at.X, (float)at.Y) * Cell };
            Lit.Set(end);
            _ends.AddChild(end);
        }
    }

    /// <summary>A loose plug over each machine that makes or draws power with no cable to it: in the overlay.</summary>
    private void PlaceUnplugged(Registry m)
    {
        foreach (var child in _unplugged.GetChildren()) child.QueueFree();
        foreach (var machine in m.Grid.Machines)
        {
            var spec = Machines.Get(machine.Kind);
            if (!spec.Powered || _byNode.ContainsKey(machine.Cell)) continue;
            var at = CableShape.MiddleOf(m.Grid, machine.Cell);
            _unplugged.AddChild(new Sprite2D
            {
                Texture = AsciiArt.Unplugged(),
                Position = new Vector2((float)at.X, (float)at.Y) * Cell,
                Material = Unlit,
            });
        }
    }

    private Color StateOf(int a, int b)
    {
        var network = _byNode.GetValueOrDefault(a) ?? _byNode.GetValueOrDefault(b);
        return network == null ? Loose : network.Satisfaction >= 1000 ? Fed : network.Satisfaction > 0 ? Short : Dark;
    }

    private void PaintOverlay(Sketch g)
    {
        var carried = _hands.Carried;
        bool Holds(int a, int b) => carried is { } c && (c.End.Cable is { } cable ? cable.A == a && cable.B == b : c.End.Node == a || c.End.Node == b);
        foreach (var line in _drawn)
        {
            bool held = Holds(line.A, line.B);
            var colour = held ? (carried!.Value.Fits ? Carried : Dark) : StateOf(line.A, line.B);
            float width = held ? CarriedWidth : CableWidth;
            g.Rope(line.Points, colour with { A = 0.25f }, width * 2.4f);
            g.Rope(line.Points, colour, width);
            foreach (var end in (IEnumerable<(double X, double Y)>)[line.Points[0], line.Points[^1]])
                g.DrawCircle(new Vector2((float)end.X, (float)end.Y) * Cell, 1.5f, colour);
        }
    }

    /// <summary>Each powered machine's socket outlined in whether the cable reaches it, while the overlay is on.</summary>
    private void OutlineSockets()
    {
        _sockets.Begin();
        if (On)
            foreach (var socket in _hands.Sockets)
                _sockets.Add(PixelArt.Picture("power-socket"), new Vector2((float)socket.At.X, (float)socket.At.Y) * Cell, 0, 1, ColourOf(socket.Light));
        _sockets.End();
    }

    private static Color ColourOf(Light light) => light switch { Light.Fits => Fits, Light.Far => OutOfReach, _ => Refused };

    private void PaintGhost(Sketch g)
    {
        // Each powered machine's socket glowing by whether the cable reaches it, outlined by OutlineSockets.
        foreach (var socket in _hands.Sockets)
        {
            var at = new Vector2((float)socket.At.X, (float)socket.At.Y) * Cell;
            g.DrawCircle(at, 6, ColourOf(socket.Light) with { A = 0.25f });
        }
        // The cable laid is a rope already, hanging from where it runs to the hand.
        if (_hands.Laying.Count > 0) g.Rope(_hands.Laying, (_hands.Why != null ? Refused : Fits) with { A = 0.9f }, 1.5f);
    }
}
