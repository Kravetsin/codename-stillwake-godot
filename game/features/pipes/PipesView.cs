using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Pipes;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Pipes;

/// <summary>
/// The pipes as they are built (0109): each span a straight pipe from the middle of one end's cell to the other's, at any
/// angle, the pipe art's riveted run repeated along it (2026-11-02-pipes-in-ink); a joint a small flange, a valve its wheel, open or
/// shut as the world has it, heard turning. Lit as the ship is, behind what stands in front. Redrawn when the spans or
/// valves change, read every frame by a cheap key.
/// </summary>
public partial class PipesView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private readonly Sketch _pipes = new(Layers.Pipes, lit: true), _valves = new(Layers.Valves, lit: true);
    private readonly Heard2D _turns = Cues.Player2D(S.Valve, S.World, voices: 2);
    private List<PipeSpan> _spans = [];
    private List<Valve> _shown = [];
    private List<int> _joints = [];
    private int _width = 1;
    private string _key = "";
    private static Texture2D? _flange, _run;

    public PipesView()
    {

        Lit.Set(_pipes);
        Lit.Set(_valves);
        _pipes.Paint = PaintPipes;
        _valves.Paint = PaintValves;
        foreach (var node in (Node[])[_pipes, _valves, _turns]) AddChild(node);
    }

    /// <summary>The pipe's riveted run, repeated along a span.</summary>
    private static Texture2D Run
    {
        get
        {
            if (_run != null) return _run;
            var texture = HiRes.Texture(HiRes.Load("pipe_run")!);
            if (texture is CanvasTexture canvas) canvas.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;
            return _run = texture;
        }
    }

    /// <summary>A joint's flange: the lone pipe's picture, its middle.</summary>
    private static Texture2D Flange => _flange ??= HiRes.Texture(HiRes.Load("pipe_0")!);

    /// <summary>Where a node's cell has its middle, in pixels of the world.</summary>
    public static Vector2 MiddleOf(int cell, int width) => new((cell % width + 0.5f) * Cell, (cell / width + 0.5f) * Cell);

    public void Frame(Client client, Registry m)
    {
        var pipes = m.Pipes;
        _width = m.Grid.Width;
        string key = $"{m.Grid.Version}:{pipes.Spans.Count}:{pipes.Valves.Count}:" + string.Join(";", pipes.Spans.Select(s => $"{s.A}-{s.B}"))
            + "|" + string.Join(";", pipes.Valves.Select(v => $"{v.Cell}{(v.Open ? 'o' : 'x')}"));
        if (key == _key) return;
        _key = key;
        // A valve turned, not made or taken: heard where it stands.
        foreach (var valve in pipes.Valves)
            if (_shown.Find(v => v.Cell == valve.Cell) is { } was && was.Open != valve.Open) _turns.PlayAt(MiddleOf(valve.Cell, _width));
        _spans = [.. pipes.Spans];
        _shown = [.. pipes.Valves];
        var grid = m.Grid;
        _joints = _spans.SelectMany(s => new[] { s.A, s.B }).Distinct().Where(n => grid.MachineAt(n) is not { } at || !grid.PortsOf(at).Contains(n)).ToList();
        _pipes.QueueRedraw();
        _valves.QueueRedraw();
    }

    private void PaintPipes(Sketch g)
    {
        foreach (var span in _spans)
        {
            Vector2 a = MiddleOf(span.A, _width), b = MiddleOf(span.B, _width);
            if (TubeRun.LitFromEnd(a.X, a.Y, b.X, b.Y)) (a, b) = (b, a);
            float length = a.DistanceTo(b);
            g.DrawSetTransform(a, (b - a).Angle());
            g.DrawTextureRect(Run, new Rect2(0, -Run.GetHeight() / 2f, length, Run.GetHeight()), tile: true);
        }
        g.DrawSetTransform(Vector2.Zero);
        // A flange at every joint, over the runs that meet there; a machine's port has its socket.
        var flange = Flange;
        foreach (int node in _joints)
            g.DrawTexture(flange, MiddleOf(node, _width) - flange.GetSize() / 2);
    }

    private void PaintValves(Sketch g)
    {
        foreach (var valve in _shown)
        {
            var wheel = ShipTiles.PictureOf(valve.Open ? "valve_open" : "valve_closed");
            g.DrawTexture(wheel, MiddleOf(valve.Cell, _width) - wheel.GetSize() / 2);
        }
    }
}
