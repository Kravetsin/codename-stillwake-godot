using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Pipes;

/// <summary>
/// Every pipe network brought out of the ship (features/pipes/index.ts), on the Air tab: the ship as it is and each
/// network's pipes over it, slim, in a colour of their own, so a player sees which tanks and generators
/// reach which vents. Where oxygen comes into a network, from a tank holding some or a powered generator,
/// dots run along its pipes away from it; a network none comes into is faded and still. A valve shows its
/// state in colour; a socket shows where each machine takes a pipe, which only matters here. Out of the
/// overlay, the grid draws the pipes themselves.
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
    private const double FlowSpeed = 2;
    private const int DotEvery = 3;
    private static readonly Color ValveOpen = AsciiArt.Colour(0x4caf6a), ValveClosed = AsciiArt.Colour(0xd05050), ValveEdge = AsciiArt.Colour(0x10161d);

    private readonly Sketch _view = new(Layers.Overlay);
    private readonly List<(Rect2 Box, Color Colour)> _pipes = [];
    private readonly List<(Rect2 Box, Color Colour)> _valves = [];
    private readonly List<Vector2> _sockets = [];
    private List<(Run Run, Color Colour)> _runs = [];
    private double _since = RefreshMs, _along;
    private int _shownTick = -1;

    public PipesOverlay()
    {
        AddChild(_view);
        _view.Paint = Paint;
    }

    /// <summary>One frame while the overlay is on: the networks read again at most every <see cref="RefreshMs"/>, the dots moved on.</summary>
    public void Frame(Client client, Registry m, double elapsedMs)
    {
        _along += elapsedMs / 1000 * FlowSpeed;
        _view.QueueRedraw();
        if ((_since += elapsedMs) < RefreshMs || client.World.Tick == _shownTick) return;
        _since = 0;
        _shownTick = client.World.Tick;
        var grid = m.Grid;
        int width = grid.Width;
        var byId = grid.Machines.ToDictionary(x => x.Id);
        // A tank holding oxygen feeds its pipes; a powered generator does, its store empty or not.
        bool Feeds(int id) => byId.TryGetValue(id, out var x) && (x.Kind == "generator"
            ? m.Power.Satisfaction(id) > 0
            : x.Kind == "tank" && m.Atmosphere.OxygenIn(id) > 0);
        _pipes.Clear();
        _runs = [];
        for (int i = 0; i < m.Pipes.Networks.Count; i++)
        {
            var network = m.Pipes.Networks[i];
            var own = network.Cells.ToHashSet();
            var sources = network.Machines.Where(Feeds).SelectMany(id => grid.PortsOf(byId[id])).ToList();
            var colour = AsciiArt.Colour(Flow.ColourOf(i), sources.Count > 0 ? 1 : DryAlpha);
            // Slim, as the pipe drawn under it, reaching toward the neighbours of its own network.
            foreach (int cell in network.Cells)
            {
                float cx = cell % width * Cell + Cell / 2f, cy = cell / width * Cell + Cell / 2f;
                int x = cell % width;
                _pipes.Add((new Rect2(cx - 2, cy - 2, 4, 4), colour));
                if (x < width - 1 && own.Contains(cell + 1)) _pipes.Add((new Rect2(cx, cy - 1, Cell / 2f, 2), colour));
                if (x > 0 && own.Contains(cell - 1)) _pipes.Add((new Rect2(cx - Cell / 2f, cy - 1, Cell / 2f, 2), colour));
                if (own.Contains(cell + width)) _pipes.Add((new Rect2(cx - 1, cy, 2, Cell / 2f), colour));
                if (own.Contains(cell - width)) _pipes.Add((new Rect2(cx - 1, cy - Cell / 2f, 2, Cell / 2f), colour));
            }
            foreach (var run in Flow.RunsOf(Flow.StepsFrom(network.Cells, width, sources), width)) _runs.Add((run, colour with { A = 1 }));
        }
        _valves.Clear();
        // Every valve is within what is built.
        var conduit = grid.ConduitLayer;
        var built = grid.Built;
        for (int y = built.Top; y <= built.Bottom; y++)
            for (int cell = y * width + built.Left, end = cell + built.Width; cell < end; cell++)
            {
                if (conduit[cell] is not (Conduit.ValveOpen or Conduit.ValveClosed)) continue;
                float cx = cell % width * Cell + Cell / 2f, cy = cell / width * Cell + Cell / 2f;
                _valves.Add((new Rect2(cx - 3, cy - 3, 6, 6), conduit[cell] == Conduit.ValveOpen ? ValveOpen : ValveClosed));
            }
        _sockets.Clear();
        foreach (var machine in grid.Machines)
            foreach (int port in grid.PortsOf(machine))
                _sockets.Add(new Vector2(port % width * Cell + Cell / 2f - 4, port / width * Cell + Cell / 2f - 4));
    }

    private void Paint(Sketch g)
    {
        foreach (var (box, colour) in _pipes) g.DrawRect(box, colour);
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
}
