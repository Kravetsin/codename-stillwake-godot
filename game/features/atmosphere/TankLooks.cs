using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>
/// What each oxygen tank holds, without a word: pale oxygen up its sight glass by any level (<see cref="GlassLevel"/>), the glass
/// dark and a red lamp lit when it holds none (<c>tank_empty</c>). Drawn unlit over its picture (<c>machine_tank.png</c>), for the
/// glass is backlit, and lighting the room round it a little in the colour it shows (a <see cref="Glow"/>), the fuller the brighter.
/// </summary>
public partial class TankLooks : Node2D
{
    /// <summary>What the tanks hold is read this often, as the air is surveyed: a tank fills and drains slowly.</summary>
    private const double RefreshMs = 250;

    /// <summary>The red lamp's light, above the glass, while empty; the glass's, from its middle, from its least to its most.</summary>
    private static readonly (Vector2 At, Color Colour, float Strength) Empty = (new Vector2(22, 10), new Color("e03030"), 0.12f);
    private static readonly Vector2 GlassMiddle = new(16, 16);
    private static readonly Color Oxygen = new("6fd3ff");
    private const float Least = 0.18f, Most = 0.42f;

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly IGridQueries _grid;
    private readonly IAtmosphereQueries _atmosphere;
    /// <summary>Each tank's glass, its red lamp and its light, by machine id.</summary>
    private readonly Dictionary<int, (GlassLevel Glass, Sprite2D Lamp, Glow Light)> _tanks = new();
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
        foreach (var m in _grid.MachinesWith(Stillwake.Content.MachineTrait.Gas))
        {
            _seen.Add(m.Id);
            var corner = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * ShipCamera.Cell;
            int oxygen = _atmosphere.OxygenIn(m.Id);
            if (!_tanks.TryGetValue(m.Id, out var tank))
            {
                _tanks[m.Id] = tank = (new GlassLevel("tank"), new Sprite2D { Centered = false, Material = Unlit, Texture = PixelArt.Picture("tank_empty") }, new Glow());
                AddChild(tank.Glass);
                AddChild(tank.Lamp);
                AddChild(tank.Light);
            }
            tank.Glass.Position = tank.Lamp.Position = corner;
            tank.Glass.Show(oxygen, Stillwake.Content.Air.TankCapacity);
            tank.Lamp.Visible = oxygen == 0;
            float full = Mathf.Clamp(oxygen / (float)Stillwake.Content.Air.TankCapacity, 0, 1);
            tank.Light.Position = corner + (oxygen == 0 ? Empty.At : GlassMiddle);
            tank.Light.Colour = oxygen == 0 ? Empty.Colour * Empty.Strength : Oxygen * Mathf.Lerp(Least, Most, full);
        }
        _gone.Clear();
        foreach (int id in _tanks.Keys)
            if (!_seen.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            _tanks[id].Glass.QueueFree();
            _tanks[id].Lamp.QueueFree();
            _tanks[id].Light.QueueFree();
            _tanks.Remove(id);
        }
    }
}
