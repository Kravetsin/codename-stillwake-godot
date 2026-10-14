using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Furnace;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Furnace;

/// <summary>
/// A furnace at work without a word (features/furnace): fire in its mouth and embers rising from it
/// while it burns, neither while it is out, for want of fuel, air or room. Its panel says why, on
/// demand. The fire is drawn unlit, for it gives light (<c>LightView.Burns</c>).
/// </summary>
public partial class FurnaceView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The fire's pictures, <c>art/fire_*.png</c>, shown in turn.</summary>
    private const int Frames = 3;
    /// <summary>Where the fire goes in the furnace's picture, in pixels from its top left: the inside of its mouth (FIRE_AT).</summary>
    private static readonly Vector2 FireAt = new(14, 17);
    /// <summary>Embers a furnace at work gives off a second.</summary>
    private const double EmbersPerSecond = 7;
    /// <summary>How long the fire shows one picture before the next.</summary>
    private const double FlickerMs = 110;

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly IGridQueries _grid;
    private readonly FurnaceModule _furnace;
    private readonly Content.MachineSpec _spec = Content.Machines.Get("furnace");
    private readonly Embers _embers = new() { ZIndex = 7 };
    /// <summary>The fire in each furnace at work, its picture, and the embers owed to it, carried over between frames, by machine id.</summary>
    private readonly Dictionary<int, (Sprite2D Sprite, int Frame, double Owed)> _fires = new();
    private readonly HashSet<int> _burning = new();
    private readonly List<int> _out = new();
    private readonly Random _random = new();
    private double _flicker;

    public FurnaceView(IGridQueries grid, FurnaceModule furnace)
    {
        _grid = grid;
        _furnace = furnace;
        AddChild(_embers);
    }

    /// <summary>The fires shown, and the embers flying: what a recording checks.</summary>
    public int Fires => _fires.Count;
    public int EmberCount => _embers.Count;

    public override void _Process(double delta)
    {
        double elapsedMs = delta * 1000;
        _flicker += elapsedMs;
        bool turn = _flicker >= FlickerMs;
        if (turn) _flicker %= FlickerMs;
        _burning.Clear();
        foreach (var m in _grid.Machines)
        {
            if (m.Kind != "furnace" || !_furnace.At(m.Id).Burning) continue;
            _burning.Add(m.Id);
            if (!_fires.TryGetValue(m.Id, out var fire))
            {
                fire = (new Sprite2D { Centered = false, ZIndex = 5, Material = Unlit, Texture = Picture(m.Id) }, m.Id, 0);
                AddChild(fire.Sprite);
            }
            if (turn)
            {
                // Never the same picture twice running, so the fire never stands still.
                fire.Frame += 1 + _random.Next(Frames - 1);
                fire.Sprite.Texture = Picture(fire.Frame);
            }
            var corner = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * Cell;
            fire.Sprite.Position = corner + FireAt;
            double due = fire.Owed + EmbersPerSecond * elapsedMs / 1000;
            int count = (int)Math.Floor(due);
            fire.Owed = due - count;
            // From the fire in its middle, a little to either side.
            var from = corner + new Vector2(_spec.Width / 2f, _spec.Height * 0.45f) * Cell;
            for (int i = 0; i < count; i++) _embers.Emit(from + new Vector2((_random.NextSingle() - 0.5f) * Cell, 0));
            _fires[m.Id] = fire;
        }
        _out.Clear();
        foreach (var id in _fires.Keys)
            if (!_burning.Contains(id)) _out.Add(id);
        foreach (var id in _out)
        {
            _fires[id].Sprite.QueueFree();
            _fires.Remove(id);
        }
    }

    private static Texture2D Picture(int frame) => PixelArt.Picture($"fire_{frame % Frames}");
}
