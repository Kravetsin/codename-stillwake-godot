using System;
using System.Collections.Generic;
using System.Linq;
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
/// room round it in its glow's colour; the circuit assembler's arm dips to its board under the glass, its rotor joint
/// turning, while its screen runs green, drawn unlit over it (<c>art/{kind}_screen_*.png</c>, in step), lighting the
/// room green. Idle, waiting full or without power, it stands still and dark. A fed station too cold for what its
/// input holds shows a cold-blue mark where it would work it, pulsing, its own faint light. Heard where it stands as it works
/// (<see cref="StationSounds"/>): the Former's strokes in step with its head, its motor; the smelter's hum and its melt.
/// </summary>
public partial class StationView : Node2D
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>
    /// How a station shows it works: <paramref name="Frames"/> pictures, each shown <paramref name="StepMs"/>, lit as the
    /// machine is, or unlit when they are what <paramref name="Glows"/>; the lights it gives, where they stand in its
    /// picture, in <paramref name="Light"/>, as much of it as <paramref name="Levels"/> says a picture, each light a
    /// picture apart; with <paramref name="Screen"/>, a screen's pictures drawn unlit over the moving parts, in step; with
    /// <paramref name="Down"/>, the picture its press head comes down on, where its stroke is heard.
    /// </summary>
    private sealed record Motion(int Frames, double StepMs, bool Glows = false, Vector2[]? Lights = null, Color Light = default, float[]? Levels = null, bool Screen = false, int? Down = null);

    /// <summary>The stations whose parts move or glow, by kind.</summary>
    private static readonly Dictionary<string, Motion> Moving = new()
    {
        ["former"] = new(4, 110, Down: 2),
        // The coils' glow: a hot orange, a little brighter on each picture as the heat runs up them.
        ["smelter"] = new(3, 160, Glows: true, Lights: [new(11.5f, 18), new(19.5f, 18)], Light: new(1f, 0.5f, 0.18f), Levels: [0.5f, 0.6f, 0.55f]),
        // The screen's trace, green as it is, flickering a little as it runs.
        ["assembler"] = new(4, 140, Lights: [new(22, 7.5f)], Light: new("6fbf73"), Levels: [0.4f, 0.35f, 0.45f, 0.35f], Screen: true),
        // The grow lamp under its cap, a steady pink while it grows (0060).
        ["growCapsule"] = new(1, 1000, Glows: true, Lights: [new(16, 9)], Light: new(1f, 0.42f, 0.8f), Levels: [0.6f]),
    };

    /// <summary>How many pictures a plant grows through, a quarter of its crop each (<c>art/{kind}_plant_*.png</c>).</summary>
    private const int Stages = 4;

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
    /// <summary>The pictures at work shown, a screen's over them, and their lights, by machine id.</summary>
    private readonly Dictionary<int, (Sprite2D Sprite, Sprite2D? Screen, Glow[] Glows)> _shown = new();
    private readonly HashSet<int> _working = new();
    private readonly List<int> _stopped = new();
    /// <summary>The plants shown in grow capsules, by machine id, and those seen this frame.</summary>
    private readonly Dictionary<int, Sprite2D> _plants = new();
    private readonly HashSet<int> _planted = new();
    /// <summary>The fed stations too cold for their input now: where their mark goes, and its light.</summary>
    private readonly List<Vector2> _cold = [];
    private readonly List<Glow> _coldGlows = [];
    private readonly Node2D _marks = new() { ZIndex = Layers.Screens, Material = Unlit };
    private readonly StationSounds _sounds = new();
    /// <summary>Each electric smelter's heat, by machine id, and those seen this frame.</summary>
    private readonly Dictionary<int, SmelterHeat> _heat = new();
    private readonly HashSet<int> _heated = new();
    private double _clock;

    public StationView(IGridQueries grid, CraftingModule crafting)
    {
        _grid = grid;
        _crafting = crafting;
        AddChild(_marks);
        AddChild(_sounds);
        _marks.Draw += DrawMarks;
    }

    /// <summary>The stations shown at work, the plants shown and the marks of those too cold: what a recording checks.</summary>
    public int Working => _shown.Count;
    public int Plants => _plants.Count;
    public int TooCold => _cold.Count;

    public override void _Process(double delta)
    {
        _clock += delta * 1000;
        _working.Clear();
        _planted.Clear();
        _cold.Clear();
        foreach (var m in _grid.Machines)
        {
            var corner = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * Cell;
            if (ColdAt.TryGetValue(m.Kind, out var cold) && _crafting.TooCold(m.Id)) _cold.Add(corner + cold);
            ShowPlant(m.Id, m.Kind, corner);
            if (!Moving.TryGetValue(m.Kind, out var motion)) continue;
            bool works = _crafting.Works(m.Id);
            if (m.Kind == "smelter") Heat(m.Id, corner, works, delta);
            // Each machine a step apart from the next, so two never beat in step.
            long step = (long)(_clock / motion.StepMs) + m.Id;
            var middle = corner + PixelArt.Picture($"{m.Kind}_work_0").GetSize() / 2;
            _sounds.Hear(m.Id, m.Kind, middle, works, step, motion.Frames, motion.Down, delta);
            if (!works) continue;
            _working.Add(m.Id);
            if (!_shown.TryGetValue(m.Id, out var shown)) _shown[m.Id] = shown = Shown(motion);
            shown.Sprite.Position = corner;
            int frame = (int)(step % motion.Frames);
            shown.Sprite.Texture = PixelArt.Picture($"{m.Kind}_work_{frame}");
            if (shown.Screen is { } screen)
            {
                screen.Position = corner;
                screen.Texture = PixelArt.Picture($"{m.Kind}_screen_{frame}");
            }
            for (int i = 0; i < shown.Glows.Length; i++)
            {
                shown.Glows[i].Position = corner + motion.Lights![i];
                shown.Glows[i].Colour = motion.Light * motion.Levels![(frame + i) % motion.Levels.Length];
            }
        }
        _sounds.Forget();
        foreach (var id in _heat.Keys.Where(id => !_heated.Contains(id)).ToList())
        {
            _heat[id].QueueFree();
            _heat.Remove(id);
        }
        _heated.Clear();
        _stopped.Clear();
        foreach (var id in _shown.Keys)
            if (!_working.Contains(id)) _stopped.Add(id);
        foreach (var id in _stopped)
        {
            _shown[id].Sprite.QueueFree();
            _shown[id].Screen?.QueueFree();
            foreach (var glow in _shown[id].Glows) glow.QueueFree();
            _shown.Remove(id);
        }
        foreach (var id in _plants.Keys.Where(id => !_planted.Contains(id)).ToList())
        {
            _plants[id].QueueFree();
            _plants.Remove(id);
        }
        ShowCold();
    }

    /// <summary>An electric smelter's heat in light (<see cref="SmelterHeat"/>, 0075), made the first time it is seen.</summary>
    private void Heat(int id, Vector2 corner, bool works, double delta)
    {
        _heated.Add(id);
        if (!_heat.TryGetValue(id, out var heat))
        {
            if (!works) return;
            AddChild(_heat[id] = heat = new SmelterHeat());
        }
        heat.Step(corner, works, delta * 1000);
    }

    /// <summary>
    /// What grows in a grow capsule (0060), lit as the machine is: whatever its input holds of its process, planted, at the
    /// quarter of its crop it has grown to, working or paused; nothing for an empty capsule or one holding what it does
    /// not grow.
    /// </summary>
    private void ShowPlant(int id, string kind, Vector2 corner)
    {
        if (!Content.Stations.TryGet(kind, out var station) || station.Process != "grow" || _crafting.WorkOf(id) is not int work) return;
        _planted.Add(id);
        if (!_plants.TryGetValue(id, out var plant))
        {
            plant = new Sprite2D { Centered = false, ZIndex = Layers.Sockets };
            Lit.Set(plant);
            AddChild(plant);
            _plants[id] = plant;
        }
        plant.Position = corner;
        int stage = (int)Math.Min(Stages - 1, (long)_crafting.Progress(id) * Stages / work);
        plant.Texture = PixelArt.Picture($"{kind}_plant_{stage}");
    }

    /// <summary>A station's pictures at work over its own: lit as the machine is, or unlit when they glow; a screen's unlit over them; its lights.</summary>
    private (Sprite2D, Sprite2D?, Glow[]) Shown(Motion motion)
    {
        var sprite = new Sprite2D { Centered = false, ZIndex = Layers.Sockets };
        if (motion.Glows) sprite.Material = Unlit;
        else Lit.Set(sprite);
        AddChild(sprite);
        Sprite2D? screen = null;
        if (motion.Screen) AddChild(screen = new Sprite2D { Centered = false, ZIndex = Layers.Sockets, Material = Unlit });
        var lights = new Glow[motion.Lights?.Length ?? 0];
        for (int i = 0; i < lights.Length; i++) AddChild(lights[i] = new Glow());
        return (sprite, screen, lights);
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
