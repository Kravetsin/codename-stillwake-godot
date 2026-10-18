using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Logistics;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Game.Features.Logistics;

/// <summary>
/// Drones in the world without a word (0062): each flying drone a small body with two spinning rotors, drawn by code,
/// its cargo's icon hanging under it, carried on between ticks; a lamp on each provider crate in red and each
/// requester in sky, storage none, each lighting what is round it a little; a bay's pads lit sky where drones dock;
/// and a bay's zone outlined, dashed sky, while its frame is open or a bay's card is in hand, where it would stand.
/// </summary>
public partial class LogisticsView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private const double RefreshMs = 100;
    private const float Unit = 1000;
    /// <summary>Where a crate's lamp sits in its picture, in pixels from its top-left, and its side.</summary>
    private static readonly Vector2 LampAt = new(27, 2);
    private const float LampSide = 2;
    /// <summary>A bay's pads in its picture (<c>machine_droneBay.png</c>), slot by slot.</summary>
    private static readonly Rect2[] Pads = [new(5, 15, 8, 2), new(19, 15, 8, 2), new(5, 27, 8, 2), new(19, 27, 8, 2)];
    private const float LampLight = 0.35f, PadLight = 0.22f;
    /// <summary>A rotor's turns a second, as it looks: the blade's width beats at this.</summary>
    private const double Spin = 12;

    private static readonly Color Body = new("cbd5e1"), Shade = new("8fa0b2"), Rotor = new("e2e8f0"), Lens = new("60a5fa"), Outline = new("1a1e25");
    private static readonly Color Pad = new("7dd3fc"), Zone = new("60a5fa");
    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly LogisticsModule _logistics;
    private readonly Func<double> _progress;
    private readonly Func<int?> _openBay;
    private readonly Func<(double X, double Y)?> _planning;
    private readonly Node2D _glowing = new() { ZIndex = Layers.Screens, Material = Unlit };
    private readonly Node2D _flying = new() { ZIndex = Layers.Drones };
    private readonly Node2D _zones = new() { ZIndex = Layers.Overlay, Material = Unlit };
    private readonly List<(Vector2 At, Color Colour)> _lamps = [], _pads = [];
    private readonly List<Glow> _glows = [];
    private readonly List<Rect2> _outlined = [];
    private double _sinceRead = RefreshMs, _clock;

    /// <param name="progress">How far the world is between its last tick and the next, 0 to 1.</param>
    /// <param name="openBay">The bay whose frame is open, if one is.</param>
    /// <param name="planning">Where the pointer is, in cells, while a bay's card is in hand.</param>
    public LogisticsView(IGridQueries grid, IItemsQueries items, LogisticsModule logistics, Func<double> progress, Func<int?> openBay, Func<(double X, double Y)?> planning)
    {
        (_grid, _items, _logistics, _progress, _openBay, _planning) = (grid, items, logistics, progress, openBay, planning);
        Lit.Set(_flying);
        AddChild(_glowing);
        AddChild(_flying);
        AddChild(_zones);
        _glowing.Draw += DrawGlowing;
        _flying.Draw += DrawFlying;
        _zones.Draw += DrawZones;
    }

    public override void _Process(double delta)
    {
        _clock += delta;
        _sinceRead += delta * 1000;
        if (_sinceRead >= RefreshMs)
        {
            _sinceRead = 0;
            Read();
            _glowing.QueueRedraw();
        }
        Zones();
        _flying.QueueRedraw();
        _zones.QueueRedraw();
    }

    private Vector2 CornerOf(Machine m) => new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * Cell;

    /// <summary>The crates' lamps and the bays' pads as the sim has them now, and their lights.</summary>
    private void Read()
    {
        _lamps.Clear();
        _pads.Clear();
        foreach (var crate in _grid.MachinesOf("crate"))
        {
            var role = _logistics.RoleOf(crate.Id);
            if (role == Role.Storage) continue;
            _lamps.Add((CornerOf(crate) + LampAt, role == Role.Provider ? RoleToggle.Provider : RoleToggle.Requester));
        }
        foreach (var bay in _grid.MachinesOf(Tune.Bay))
        {
            var slots = _items.Crate(bay.Id);
            for (int i = 0; i < slots.Count && i < Pads.Length; i++)
                if (slots[i]?.Item == Tune.Drone) _pads.Add((CornerOf(bay) + Pads[i].Position, Pad));
        }
        int lights = _lamps.Count + _pads.Count;
        while (_glows.Count < lights)
        {
            var glow = new Glow();
            AddChild(glow);
            _glows.Add(glow);
        }
        for (int i = 0; i < _glows.Count; i++)
        {
            _glows[i].Visible = i < lights;
            if (i >= lights) continue;
            bool lamp = i < _lamps.Count;
            var (at, colour) = lamp ? _lamps[i] : _pads[i - _lamps.Count];
            _glows[i].Position = at + (lamp ? new Vector2(LampSide, LampSide) / 2 : Pads[0].Size / 2);
            _glows[i].Colour = colour * (lamp ? LampLight : PadLight);
        }
    }

    /// <summary>The zones outlined now: the open bay's, and while a bay's card is in hand every bay's and the one it would make.</summary>
    private void Zones()
    {
        _outlined.Clear();
        if (_openBay() is int open && _logistics.ZoneOf(open) is { } zone) _outlined.Add(RectOf(zone));
        if (_planning() is not { } pointer) return;
        foreach (var bay in _grid.MachinesOf(Tune.Bay))
            if (bay.Id != _openBay() && _logistics.ZoneOf(bay.Id) is { } each) _outlined.Add(RectOf(each));
        var spec = Content.Machines.Get(Tune.Bay);
        int x = (int)Math.Floor(pointer.X), y = (int)Math.Floor(pointer.Y);
        // Clipped at the grid's edge, as the sim clips a bay's.
        _outlined.Add(RectOf(new Box(Math.Max(0, x - Tune.Reach), Math.Max(0, y - Tune.Reach),
            Math.Min(_grid.Width - 1, x + spec.Width - 1 + Tune.Reach), Math.Min(_grid.Height - 1, y + spec.Height - 1 + Tune.Reach))));
    }

    private static Rect2 RectOf(Box b) => new(b.Left * Cell, b.Top * Cell, b.Width * Cell, b.Height * Cell);

    private void DrawGlowing()
    {
        foreach (var (at, colour) in _lamps) _glowing.DrawRect(new Rect2(at, new Vector2(LampSide, LampSide)), colour);
        foreach (var (at, colour) in _pads) _glowing.DrawRect(new Rect2(at, Pads[0].Size), colour);
    }

    private void DrawZones()
    {
        foreach (var r in _outlined)
        {
            _zones.DrawRect(r, Zone with { A = 0.05f });
            var (a, b, c, d) = (r.Position, r.Position + new Vector2(r.Size.X, 0), r.End, r.Position + new Vector2(0, r.Size.Y));
            foreach (var (from, to) in new[] { (a, b), (b, c), (c, d), (d, a) }) _zones.DrawDashedLine(from, to, Zone with { A = 0.55f }, 1, 4);
        }
    }

    /// <summary>Each drone in flight at where it is between ticks: two rotors beating over its body, its cargo under it.</summary>
    private void DrawFlying()
    {
        double progress = _progress();
        int width = _grid.Width;
        foreach (var d in _logistics.Drones)
        {
            float toX = d.To % width * Unit + Unit / 2, toY = d.To / width * Unit + Unit / 2;
            var (x, y) = Requests.Smoothed(d.X, d.Y, (int)toX, (int)toY, Tune.Speed, progress);
            var at = new Vector2((float)(x / Unit * Cell), (float)(y / Unit * Cell));
            DrawDrone(at, d.Id);
            if (d.Cargo is { } cargo)
                _flying.DrawTextureRect(ItemIcons.Icon(cargo.Item), new Rect2(at + new Vector2(-4, 3), new Vector2(8, 8)), false);
        }
    }

    /// <summary>A drone about its middle <paramref name="at"/>: a body ten pixels wide, its lens, two arms and their rotors.</summary>
    private void DrawDrone(Vector2 at, int id)
    {
        var g = _flying;
        g.DrawRect(new Rect2(at + new Vector2(-5, -3), new Vector2(10, 5)), Outline);
        g.DrawRect(new Rect2(at + new Vector2(-4, -2), new Vector2(8, 3)), Body);
        g.DrawRect(new Rect2(at + new Vector2(-4, 0), new Vector2(8, 1)), Shade);
        g.DrawRect(new Rect2(at + new Vector2(1, -1), new Vector2(2, 1)), Lens);
        // Each rotor's blade beats wide and narrow as it turns; the two out of step, as each drone is with others.
        for (int side = -1; side <= 1; side += 2)
        {
            var hub = at + new Vector2(side * 5, -4);
            g.DrawRect(new Rect2(hub + new Vector2(-0.5f, 0), new Vector2(1, 2)), Shade);
            float blade = 2 + 2 * (float)Math.Abs(Math.Sin((_clock * Spin + id * 0.37 + (side + 1) * 0.25) * Math.PI));
            g.DrawRect(new Rect2(hub + new Vector2(-blade, -1), new Vector2(2 * blade, 1)), Rotor);
        }
    }
}
