using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Pipes;

namespace Stillwake.Game.Features.Pipes;

/// <summary>
/// Every pipe network brought out of the ship while the plan is piping (features/pipes/index.ts, 0109): each network's spans
/// over the ship, slim, in a colour of their own, so a player sees which tanks and generators reach which vents. Where
/// oxygen comes into a network, from a tank holding some or a powered generator, dots run along its spans away from it; a
/// network none comes into is faded and still. A valve shows its state in colour; a socket shows where each machine takes
/// a pipe. The piping's hands work here (<see cref="PipeHands"/>): the span being laid runs to the pointer, green where it
/// may go, red where not, its length and cost by the pointer.
/// </summary>
public partial class PipesOverlay : Node2D
{
    /// <summary>The overlay's id: the feature's.</summary>
    public const string Id = "pipes";
    private const int Cell = ShipCamera.Cell;
    private const double RefreshMs = 250;
    /// <summary>A network with no oxygen coming into it, against one with: faded.</summary>
    private const float DryAlpha = 0.45f;
    /// <summary>Oxygen's dots: how fast they run, in cells a second, and how many cells apart.</summary>
    private const double FlowSpeed = 4;
    private const int DotEvery = 6;
    private static readonly Color ValveOpen = AsciiArt.Colour(0x4caf6a), ValveClosed = AsciiArt.Colour(0xd05050), ValveEdge = AsciiArt.Colour(0x10161d),
        Loose = new(0.6f, 0.65f, 0.7f, DryAlpha), Fits = new(0.45f, 0.9f, 0.55f), Refused = new(0.95f, 0.35f, 0.35f), Hovered = new(1f, 0.85f, 0.45f);

    private readonly Ui _ui;
    private readonly Sketch _view = new(Layers.Overlay), _ghost = new(Layers.OverlayHands);
    private readonly PipeHands _hands = new();
    private readonly List<(Vector2 A, Vector2 B, Color Colour)> _spans = [];
    private readonly List<(Rect2 Box, Color Colour)> _valves = [];
    private readonly List<Vector2> _sockets = [];
    private List<(Run Run, Color Colour)> _runs = [];
    private double _since = RefreshMs, _along;
    private int _shownTick = -1, _width = 1;

    public PipesOverlay(Ui ui)
    {
        _ui = ui;
        AddChild(_view);
        AddChild(_ghost);
        _view.Paint = Paint;
        _ghost.Paint = PaintGhost;
    }

    /// <summary>
    /// One frame while the overlay is on: the piping's hands with the left buttons, <paramref name="valves"/> (the valve card) and the right
    /// presses, <paramref name="cuts"/>; the networks read again at most every <see cref="RefreshMs"/>, the dots moved on.
    /// </summary>
    public void Frame(Client client, Registry m, IReadOnlyList<ButtonEvent> left, bool valves, IReadOnlyList<ButtonEvent> cuts,
        (double X, double Y)? pointer, double elapsedMs)
    {
        _hands.Update(left, valves, cuts, pointer, m.Grid, m.Pipes, client.Id, client.Send);
        if (_hands.Laying is { Why: { } why }) _ui.Reason.Set(why);
        else if (_hands.Laying is var (from, to, _) && from != to)
        {
            int length = PipesModule.LengthOf(m.Grid.Width, from, to);
            _ui.Reason.Set(null);
            _ui.WorldTip.Set([$"{length} · {En.T($"item.{Stillwake.Content.Piping.Item}")} ×{PipesModule.CostOf(length)[0].Count}"]);
        }
        else _ui.WorldTip.Set(null);
        _ghost.QueueRedraw();
        _along += elapsedMs / 1000 * FlowSpeed;
        _view.QueueRedraw();
        if ((_since += elapsedMs) < RefreshMs || client.World.Tick == _shownTick) return;
        _since = 0;
        _shownTick = client.World.Tick;
        Read(m);
    }

    /// <summary>Lets go of a span being laid: piping left.</summary>
    public void Release() => _hands.Release();

    private void Read(Registry m)
    {
        var grid = m.Grid;
        var pipes = m.Pipes;
        int width = _width = grid.Width;
        var byId = grid.Machines.ToDictionary(x => x.Id);
        // An air source feeds its pipes: a tank while it holds oxygen, a generator while powered, its store empty or not.
        bool Feeds(int id) => byId.TryGetValue(id, out var x) && Stillwake.Content.Machines.Get(x.Kind) is var spec
            && spec.Is(Stillwake.Content.MachineTrait.AirSource)
            && (spec.Is(Stillwake.Content.MachineTrait.Gas) ? m.Atmosphere.OxygenIn(id) > 0 : m.Power.Satisfaction(id) > 0);
        var colourOf = new Dictionary<int, Color>();
        _runs = [];
        var all = pipes.Spans.Select(s => (s.A, s.B)).ToList();
        for (int i = 0; i < pipes.Networks.Count; i++)
        {
            var network = pipes.Networks[i];
            var sources = network.Machines.Where(Feeds).SelectMany(id => grid.PortsOf(byId[id])).ToList();
            var colour = AsciiArt.Colour(Flow.ColourOf(i), sources.Count > 0 ? 1 : DryAlpha);
            colourOf[network.Id] = colour;
            var own = network.Cells.ToHashSet();
            foreach (var run in Flow.RunsOf(Flow.SpansIn(all, own), width, sources, own.Contains)) _runs.Add((run, colour with { A = 1 }));
        }
        _spans.Clear();
        foreach (var span in pipes.Spans)
        {
            // A span to a shut valve is its other end's network's.
            int? network = pipes.NetworkAt(span.A) ?? pipes.NetworkAt(span.B);
            _spans.Add((PipesView.MiddleOf(span.A, width), PipesView.MiddleOf(span.B, width), network is int n ? colourOf[n] : Loose));
        }
        _valves.Clear();
        foreach (var valve in pipes.Valves)
        {
            var at = PipesView.MiddleOf(valve.Cell, width);
            _valves.Add((new Rect2(at - new Vector2(3, 3), new Vector2(6, 6)), valve.Open ? ValveOpen : ValveClosed));
        }
        _sockets.Clear();
        foreach (var machine in grid.Machines)
            foreach (int port in grid.PortsOf(machine))
                _sockets.Add(PipesView.MiddleOf(port, width) - new Vector2(4, 4));
    }

    private void Paint(Sketch g)
    {
        foreach (var (a, b, colour) in _spans) g.DrawLine(a, b, colour, 2);
        foreach (var (box, colour) in _valves)
        {
            g.DrawRect(box, colour);
            g.DrawRect(box, ValveEdge, false, 1);
        }
        var socket = PixelArt.Picture("socket");
        foreach (var at in _sockets) g.DrawTexture(socket, at);
        foreach (var (run, colour) in _runs)
            foreach (var (x, y) in Flow.DotsOf([run], _along, DotEvery))
            {
                var at = new Vector2((float)x, (float)y) * Cell;
                g.DrawRect(new Rect2(at - Vector2.One, new Vector2(2, 2)), new Color(1, 1, 1, 0.9f));
                g.DrawRect(new Rect2(at - 2 * Vector2.One, new Vector2(4, 4)), colour with { A = 0.35f });
            }
    }

    private void PaintGhost(Sketch g)
    {
        // The whole span the right button would take away, lit along its length and its two ends (0110).
        if (_hands.Hovered is { } span)
        {
            Vector2 a = PipesView.MiddleOf(span.A, _width), b = PipesView.MiddleOf(span.B, _width);
            g.DrawLine(a, b, Hovered with { A = 0.35f }, 7);
            g.DrawLine(a, b, Hovered, 1);
            foreach (var end in (Vector2[])[a, b]) g.DrawRect(new Rect2(end - new Vector2(3, 3), new Vector2(6, 6)), Hovered, false, 1);
        }
        if (_hands.Laying is not var (from, to, why) || from == to) return;
        g.DrawLine(PipesView.MiddleOf(from, _width), PipesView.MiddleOf(to, _width), why == null ? Fits : Refused, 3);
    }
}
