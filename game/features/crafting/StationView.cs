using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Crafting;

/// <summary>
/// A station at work, without a word (0057): over its picture, which shows it at rest, its pictures at work
/// (<c>art/{kind}_work_*.png</c>, each the whole machine's size) shown in turn while the sim says it works
/// (<c>CraftingModule.Works</c>): the Former's flywheel turns, its gears with it, and its press head beats on the
/// die, lit as the machine is; the electric smelter's coils glow behind their grate, drawn unlit, each lighting the
/// room round it in its glow's colour. Idle, waiting full or without power, it stands still and dark. A fed station
/// too cold for what its input holds shows a cold-blue mark where it would work it, pulsing, its own faint light.
/// </summary>
public partial class StationView : Node2D
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>
    /// The stations whose parts move or glow, by kind: how many pictures, each shown this long; for one that glows,
    /// where its glows' lights stand in its picture and how much of their colour they give.
    /// </summary>
    private static readonly Dictionary<string, (int Frames, double StepMs, Vector2[]? Glows)> Moving = new()
    {
        ["former"] = (4, 110, null),
        ["smelter"] = (3, 160, [new(11.5f, 18), new(19.5f, 18)]),
    };

    /// <summary>The coils' glow: a hot orange, a little brighter on each picture as the heat runs up them.</summary>
    private static readonly Color Coils = new(1f, 0.5f, 0.18f);
    private static readonly float[] CoilLight = [0.5f, 0.6f, 0.55f];

    /// <summary>Where a fed station shows it is too cold, in pixels of its picture: the furnace's mouth, the smelter's grate.</summary>
    private static readonly Dictionary<string, Vector2> ColdAt = new() { ["furnace"] = new(18, 21), ["smelter"] = new(16, 18) };
    private static readonly Color ColdBlue = new("7ec8ff"), ColdDim = new("2a5f8f");
    /// <summary>The cold mark, five pixels square: a star of ice.</summary>
    private static readonly Vector2I[] Flake = [new(2, 0), new(0, 1), new(2, 1), new(4, 1), new(1, 2), new(2, 2), new(3, 2), new(0, 3), new(2, 3), new(4, 3), new(2, 4)];
    private const float ColdLight = 0.25f;
    private const double ColdPulses = 0.7;

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly IGridQueries _grid;
    private readonly CraftingModule _crafting;
    /// <summary>The pictures at work shown, and their glows' lights, by machine id.</summary>
    private readonly Dictionary<int, (Sprite2D Sprite, Glow[] Glows)> _shown = new();
    private readonly HashSet<int> _working = new();
    private readonly List<int> _stopped = new();
    /// <summary>The fed stations too cold for their input now: where their mark goes, and its light.</summary>
    private readonly List<Vector2> _cold = [];
    private readonly List<Glow> _coldGlows = [];
    private readonly Node2D _marks = new() { ZIndex = Layers.Screens, Material = Unlit };
    private double _clock;

    public StationView(IGridQueries grid, CraftingModule crafting)
    {
        _grid = grid;
        _crafting = crafting;
        AddChild(_marks);
        _marks.Draw += DrawMarks;
    }

    /// <summary>The stations shown at work and the marks of those too cold: what a recording checks.</summary>
    public int Working => _shown.Count;
    public int TooCold => _cold.Count;

    public override void _Process(double delta)
    {
        _clock += delta * 1000;
        _working.Clear();
        _cold.Clear();
        foreach (var m in _grid.Machines)
        {
            var corner = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * Cell;
            if (ColdAt.TryGetValue(m.Kind, out var cold) && _crafting.TooCold(m.Id)) _cold.Add(corner + cold);
            if (!Moving.TryGetValue(m.Kind, out var motion) || !_crafting.Works(m.Id)) continue;
            _working.Add(m.Id);
            if (!_shown.TryGetValue(m.Id, out var shown)) _shown[m.Id] = shown = Shown(motion.Glows);
            shown.Sprite.Position = corner;
            // Each machine a step apart from the next, so two never beat in step.
            int frame = ((int)(_clock / motion.StepMs) + m.Id) % motion.Frames;
            shown.Sprite.Texture = PixelArt.Picture($"{m.Kind}_work_{frame}");
            for (int i = 0; i < shown.Glows.Length; i++)
            {
                shown.Glows[i].Position = corner + motion.Glows![i];
                shown.Glows[i].Colour = Coils * CoilLight[(frame + i) % CoilLight.Length];
            }
        }
        _stopped.Clear();
        foreach (var id in _shown.Keys)
            if (!_working.Contains(id)) _stopped.Add(id);
        foreach (var id in _stopped)
        {
            _shown[id].Sprite.QueueFree();
            foreach (var glow in _shown[id].Glows) glow.QueueFree();
            _shown.Remove(id);
        }
        ShowCold();
    }

    /// <summary>A station's pictures at work over its own: lit as the machine is, or unlit with its lights when it glows.</summary>
    private (Sprite2D, Glow[]) Shown(Vector2[]? glows)
    {
        var sprite = new Sprite2D { Centered = false, ZIndex = Layers.Sockets };
        if (glows == null) Lit.Set(sprite);
        else sprite.Material = Unlit;
        AddChild(sprite);
        var lights = new Glow[glows?.Length ?? 0];
        for (int i = 0; i < lights.Length; i++) AddChild(lights[i] = new Glow());
        return (sprite, lights);
    }

    /// <summary>The cold marks' lights, one a mark, pulsing with them.</summary>
    private void ShowCold()
    {
        while (_coldGlows.Count < _cold.Count)
        {
            var glow = new Glow();
            AddChild(glow);
            _coldGlows.Add(glow);
        }
        float pulse = Pulse();
        for (int i = 0; i < _coldGlows.Count; i++)
        {
            _coldGlows[i].Visible = i < _cold.Count;
            if (i >= _cold.Count) continue;
            _coldGlows[i].Position = _cold[i];
            _coldGlows[i].Colour = ColdBlue * (ColdLight * pulse);
        }
        _marks.QueueRedraw();
    }

    /// <summary>How bright the cold marks are now, pulsing slowly.</summary>
    private float Pulse() => 0.6f + 0.4f * (float)Math.Cos(_clock / 1000 * ColdPulses * 2 * Math.PI);

    private void DrawMarks()
    {
        var colour = ColdDim.Lerp(ColdBlue, Pulse());
        foreach (var at in _cold)
            foreach (var p in Flake)
                _marks.DrawRect(new Rect2(at - new Vector2(2, 2) + p, Vector2.One), colour);
    }
}
