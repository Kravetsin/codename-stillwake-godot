using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Power;

namespace Stillwake.Game.Features.Power;

/// <summary>
/// Each launch platform's lamp, the lens in its right end post: green while its network has power enough, red while not
/// (<see cref="PlatformRuns"/>), lighting the deck a little in its colour. Drawn unlit over the platform's picture
/// (<c>platform_*.png</c>, whose lens is dark). Its socket, where every cable to the run plugs in, sits in its left end post, lit
/// as a machine's is (<c>StructureSpec.Plug</c>). The runs are followed through the grid's log (<see cref="KnownPlatforms"/>).
/// </summary>
public partial class PlatformLamps : Node2D
{
    private const double RefreshMs = 250;
    /// <summary>The lens in a run's last cell: its top-left in pixels and its size, as <c>platform_8.png</c> draws its dark lens.</summary>
    private static readonly Vector2 Lens = new(6, 3), LensSize = new(1, 2);
    private static readonly Color Green = new("62d68a"), Red = new("e05555");
    private const float LightOf = 0.25f;

    private readonly IGridQueries _grid;
    private readonly IPowerQueries _power;
    private readonly KnownPlatforms _runs = new();
    private readonly List<(Vector2 At, bool Lit)> _lamps = [];
    private readonly List<Glow> _glows = [];
    private readonly Node2D _sockets = new();
    private double _sinceRead = RefreshMs;

    public PlatformLamps(IGridQueries grid, IPowerQueries power)
    {
        _grid = grid;
        _power = power;
        ZIndex = Layers.Screens;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        AddChild(_sockets);
    }

    public override void _Process(double delta)
    {
        if ((_sinceRead += delta * 1000) < RefreshMs) return;
        _sinceRead = 0;
        if (_runs.Follow(_grid)) PlaceSockets();
        _lamps.Clear();
        foreach (var run in _runs.Runs)
        {
            int last = run.First + run.Length - 1;
            _lamps.Add((new Vector2(last % _grid.Width, last / _grid.Width) * ShipCamera.Cell + Lens, PlatformRuns.Lit(_power, run)));
        }
        Light();
        QueueRedraw();
    }

    /// <summary>A socket in each run's left end post, where its plug is.</summary>
    private void PlaceSockets()
    {
        foreach (var socket in _sockets.GetChildren()) socket.QueueFree();
        foreach (var run in _runs.Runs)
        {
            if (Content.Structures.Of(_grid.StructureAt(run.First)).Plug is not { } plug) continue;
            var socket = new Sprite2D
            {
                Texture = PixelArt.Lit("power-socket"),
                Position = new Vector2(run.First % _grid.Width, run.First / _grid.Width) * ShipCamera.Cell + new Vector2(plug.X, plug.Y),
            };
            Lit.Set(socket);
            _sockets.AddChild(socket);
        }
    }

    private void Light()
    {
        while (_glows.Count < _lamps.Count)
        {
            var glow = new Glow();
            AddChild(glow);
            _glows.Add(glow);
        }
        for (int i = 0; i < _glows.Count; i++)
        {
            _glows[i].Visible = i < _lamps.Count;
            if (i >= _lamps.Count) continue;
            _glows[i].Position = _lamps[i].At + LensSize / 2;
            _glows[i].Colour = (_lamps[i].Lit ? Green : Red) * LightOf;
        }
    }

    public override void _Draw()
    {
        foreach (var (at, lit) in _lamps) DrawRect(new Rect2(at, LensSize), lit ? Green : Red);
    }
}
