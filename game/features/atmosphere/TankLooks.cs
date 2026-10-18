using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>
/// What each oxygen tank holds, without a word: pale oxygen up its sight glass by fifths
/// (<see cref="TankFill"/>), the glass dark and a red lamp lit when it holds none. Drawn unlit over its
/// picture (<c>machine_tank.png</c>), for the glass is backlit, and lighting the room round it a little in
/// the colour it shows (a <see cref="Glow"/>), the fuller the brighter.
/// </summary>
public partial class TankLooks : Node2D
{
    /// <summary>What the tanks hold is read this often, as the air is surveyed: a tank fills and drains slowly.</summary>
    private const double RefreshMs = 250;

    /// <summary>Where each frame's light comes from, in pixels from the picture's top-left, and how much of its colour it gives.</summary>
    private static readonly (Vector2 At, Color Colour, float Strength)[] Lights =
    [
        // Empty: the red lamp above the glass.
        (new Vector2(22, 10), new Color("e03030"), 0.12f),
        // The middle of the glass, brighter the more oxygen it shows.
        (new Vector2(16, 16), new Color("6fd3ff"), 0.18f),
        (new Vector2(16, 16), new Color("6fd3ff"), 0.24f),
        (new Vector2(16, 16), new Color("6fd3ff"), 0.30f),
        (new Vector2(16, 16), new Color("6fd3ff"), 0.36f),
        (new Vector2(16, 16), new Color("6fd3ff"), 0.42f),
    ];

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly IGridQueries _grid;
    private readonly IAtmosphereQueries _atmosphere;
    /// <summary>Each tank's glass and its light, by machine id.</summary>
    private readonly Dictionary<int, (Sprite2D Glass, Glow Light)> _tanks = new();
    private readonly HashSet<int> _seen = new();
    private readonly List<int> _gone = new();
    private double _sinceRead = RefreshMs;

    public TankLooks(IGridQueries grid, IAtmosphereQueries atmosphere)
    {
        _grid = grid;
        _atmosphere = atmosphere;
        ZIndex = Layers.Screens;
    }

    public override void _Process(double delta)
    {
        _sinceRead += delta * 1000;
        if (_sinceRead < RefreshMs) return;
        _sinceRead = 0;
        Read();
    }

    private void Read()
    {
        _seen.Clear();
        foreach (var m in _grid.MachinesOf("tank"))
        {
            _seen.Add(m.Id);
            var corner = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * ShipCamera.Cell;
            int frame = TankFill.Frame(_atmosphere.OxygenIn(m.Id));
            if (!_tanks.TryGetValue(m.Id, out var tank))
            {
                _tanks[m.Id] = tank = (new Sprite2D { Centered = false, Material = Unlit }, new Glow());
                AddChild(tank.Glass);
                AddChild(tank.Light);
            }
            var (at, colour, strength) = Lights[frame];
            tank.Glass.Position = corner;
            tank.Glass.Texture = PixelArt.Picture($"tank_fill_{frame}");
            tank.Light.Position = corner + at;
            tank.Light.Colour = colour * strength;
        }
        _gone.Clear();
        foreach (int id in _tanks.Keys)
            if (!_seen.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            _tanks[id].Glass.QueueFree();
            _tanks[id].Light.QueueFree();
            _tanks.Remove(id);
        }
    }
}
