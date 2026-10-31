using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Crafting;

/// <summary>
/// The lifepod at work without a word, over its picture (<c>machine_lifepod.png</c>): the Fabricator's
/// screen runs green while it works or waits full and shows a steady green READY light idle; something
/// in its tray while its outputs hold what it made. While a hero is being made, passed out, its glass
/// glows from inside with a hero's shape, brighter as the hero comes round; as one is made, a puff of air
/// at the hatch and the glow fades. Drawn unlit, for a screen and a glowing glass give their own light,
/// and each lights the room round it a little in its colour (a <see cref="Glow"/>).
/// </summary>
public partial class PodView : Node2D, ILightSource
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>What the pods are doing is read this often; the screens run every frame.</summary>
    private const double RefreshMs = 100;
    /// <summary>The screen and the tray in the pod's picture, in pixels from its top-left.</summary>
    private static readonly Rect2I Screen = new(37, 19, 6, 5), Tray = new(37, 29, 6, 3);
    /// <summary>Seconds the glass glows on once a hero is made.</summary>
    private const double Afterglow = 1.2;
    /// <summary>How much of its colour a screen's light gives, ready and at work, and the glass's at its brightest.</summary>
    private static readonly (float R, float G, float B) Pale = Stillwake.Game.Light.Pod;
    private const float ReadyLight = 0.22f, WorkingLight = 0.4f, GlassLight = 0.6f;

    private static readonly Color Dark = new("04130d"), Green = new("4ade80"), GreenLo = new("167a40"), Amber = new("fbbf24"),
        Made = new("e0a030"), Warm = new(1f, 0.85f, 0.55f);

    private readonly IGridQueries _grid;
    private readonly CraftingModule _crafting;
    private readonly IItemsQueries _items;
    private readonly ICrewQueries _crew;
    private readonly List<(Vector2 Corner, Doing Doing, bool Holds)> _pods = [];
    private readonly List<LightSource> _lights = [];
    /// <summary>The glass lit from inside, and its glow on the room, over the pod heroes are made at.</summary>
    private readonly Sprite2D _glass = new()
    {
        Texture = PixelArt.Picture("lifepod_glass"),
        Centered = false,
        ZIndex = Layers.Screens,
        ZAsRelative = false,
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded },
        Visible = false,
    };
    private readonly Sprite2D _halo = new()
    {
        Texture = PixelArt.Glow,
        ZIndex = Layers.Glows,
        ZAsRelative = false,
        Scale = new Vector2(1.5f, 1.5f),
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded, BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
        Visible = false,
    };
    private readonly List<Glow> _screens = [];
    private readonly Glow _glassLight = new();
    /// <summary>Each hero's ticks out at the last read, by peer; null before the first read, which makes no one.</summary>
    private Dictionary<int, int>? _outs;
    private double _sinceRead = RefreshMs, _clock, _afterglow;
    /// <summary>How far the hero being made has come, 0 to 1; null while none is.</summary>
    private double? _making;

    public PodView(IGridQueries grid, CraftingModule crafting, IItemsQueries items, ICrewQueries crew)
    {
        _grid = grid;
        _crafting = crafting;
        _items = items;
        _crew = crew;
        ZIndex = Layers.Screens;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        AddChild(_glass);
        AddChild(_halo);
        AddChild(_glassLight);
    }

    /// <summary>How many screens run and whether the glass glows: what a recording checks.</summary>
    public (int Pods, int Working, bool Glowing) Shown => (_pods.Count, _pods.Count(p => p.Doing != Doing.Ready), _glass.Visible);

    public override void _Process(double delta)
    {
        _clock += delta;
        _afterglow = Math.Max(0, _afterglow - delta);
        _sinceRead += delta * 1000;
        if (_sinceRead >= RefreshMs)
        {
            _sinceRead = 0;
            Read();
        }
        float glow = _making is double share ? (float)(0.35 + 0.65 * share) : (float)(_afterglow / Afterglow);
        _glass.Visible = _halo.Visible = glow > 0 && _pods.Count > 0;
        _glass.Modulate = new Color(1, 1, 1, glow);
        _halo.Modulate = Warm with { A = 0.35f * glow };
        _glassLight.Visible = _glass.Visible;
        _glassLight.Position = _halo.Position;
        _glassLight.Colour = Warm * (GlassLight * glow);
        Light();
        QueueRedraw();
    }

    /// <summary>Each pod's own light, for the light feature.</summary>
    public IReadOnlyList<LightSource> Lights => _lights;

    /// <summary>Each pod's screen lighting the room: steady green ready, flickering with the dots at work.</summary>
    private void Light()
    {
        while (_screens.Count < _pods.Count)
        {
            var glow = new Glow();
            AddChild(glow);
            _screens.Add(glow);
        }
        _lights.Clear();
        foreach (var (corner, _, _) in _pods)
        {
            var middle = corner + new Vector2(24, 12);
            _lights.Add(new LightSource(middle.X, middle.Y, Pale, 1, Stillwake.Game.Light.LampReach, Stillwake.Game.Light.LampKeep, Stillwake.Game.Light.LampMost));
        }
        for (int i = 0; i < _screens.Count; i++)
        {
            var glow = _screens[i];
            glow.Visible = i < _pods.Count;
            if (!glow.Visible) continue;
            var (corner, doing, _) = _pods[i];
            glow.Position = corner + (Vector2)Screen.Position + (Vector2)Screen.Size / 2;
            glow.Colour = Green * (doing == Doing.Ready ? ReadyLight : WorkingLight * (float)(0.8 + 0.2 * Math.Sin(_clock * 8)));
        }
    }

    private void Read()
    {
        _pods.Clear();
        Vector2? maker = null;
        foreach (var m in _grid.Machines)
        {
            if (!Stillwake.Content.Machines.Get(m.Kind).Fixed) continue;
            var corner = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * Cell;
            var doing = Crafts.DoingOf(_crafting.Queue(m.Id), _crafting.Progress(m.Id));
            _pods.Add((corner, doing, _items.Crate(m.Id).Any(s => s != null)));
            maker ??= corner;
        }
        if (maker is not { } at) return;
        _glass.Position = at;
        _halo.Position = at + new Vector2(22, 22);
        // Passed out, a hero is being made in the pod; coming round, or coming aboard, it has been.
        var heroes = _crew.Heroes;
        var outs = heroes.ToDictionary(h => h.Id, h => h.Out);
        bool made = _outs != null && heroes.Any(h => h.Out == 0 && (!_outs.TryGetValue(h.Id, out int was) || was > 0));
        _outs = outs;
        var out_ = heroes.Where(h => h.Out > 0).Select(h => h.Out).DefaultIfEmpty(0).Min();
        _making = out_ > 0 ? 1 - (double)out_ / Content.Crew.PassOut : null;
        if (!made) return;
        _afterglow = Afterglow;
        int hatch = _crew.Spawn;
        // The hatch breathes out: a few puffs of vapour rolling up and out (0075), over the waking cell's right edge.
        var glims = Stages.Of(Layers.Machines).Glims;
        double hx = (hatch % _grid.Width + 1) * Cell, hy = (hatch / _grid.Width + 0.2) * Cell;
        for (int i = 0; i < 5; i++)
        {
            double a = -Math.PI / 2 + glims.Between(-1.1, 1.1), s = glims.Between(14, 34);
            glims.Add(Luminous.SealHiss, hx + glims.Between(-3, 3), hy, Math.Cos(a) * s, Math.Sin(a) * s);
        }
    }

    public override void _Draw()
    {
        foreach (var (corner, doing, holds) in _pods)
        {
            var origin = corner + Screen.Position;
            DrawRect(new Rect2(origin, Screen.Size), Dark);
            if (holds) DrawRect(new Rect2(corner + Tray.Position + new Vector2(1, 1), new Vector2(Tray.Size.X - 2, 1)), Made);
            if (doing == Doing.Ready)
            {
                // READY: a steady light in the corner and a still line, nothing running.
                DrawRect(new Rect2(origin + new Vector2(Screen.Size.X - 2, 1), Vector2.One), Green);
                DrawRect(new Rect2(origin + new Vector2(0, 3), new Vector2(Screen.Size.X - 2, 1)), GreenLo);
                continue;
            }
            // The mockup's screen(): dots running across, a wave down the rows, the corner amber.
            for (int i = 0; i < Screen.Size.X; i++)
            {
                if (Math.Sin((i + _clock * 8) / 1.6) <= 0.2) continue;
                int row = 1 + (i + (int)Math.Floor(_clock * 6)) % (Screen.Size.Y - 2);
                DrawRect(new Rect2(origin + new Vector2(i, row), Vector2.One), Green);
            }
            DrawRect(new Rect2(origin + new Vector2(Screen.Size.X - 2, 1), Vector2.One), Amber);
        }
    }
}
