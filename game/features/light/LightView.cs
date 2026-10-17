using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// Light without a word (0019, 0029): a dark ship, the ambient light of <see cref="L.Ambient"/> in
/// every corner, lit by its lamps, its furnaces' and generators' fires and whatever glows — each drawn into the
/// <see cref="Lightmap"/> with its shadows, falling off as the TypeScript game's did — and by starlight
/// through the holes in its back walls. A lit lamp glows, and short of power it flickers; a furnace's
/// fire, sparks, embers and a planner's screen light what is around them. Where lights meet they add,
/// each colour at most whole (<see cref="Lit"/>). <see cref="Sync"/> follows the grid after each tick;
/// what stops light and the starlight change only when walls, doors, back walls or machines do.
/// </summary>
public partial class LightView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>How far a lamp's light reaches, in cells.</summary>
    private const int LampReach = 16;
    /// <summary>A lit lamp's warm glow, and how far across it reaches, in cells.</summary>
    private static readonly Color GlowColour = new(0xffd9a0ff);
    private const float GlowCells = 3;
    /// <summary>
    /// A burning fire's glow at its machine's mouth, how far across it reaches, in cells, and how opaque it
    /// is at the fire's full light: it shows the fire as the source of its light where a lamp already
    /// lights the furnace nearly whole, and jumps with it.
    /// </summary>
    private static readonly Color FireGlowColour = new(0xff8a3cff);
    private const float FireGlowCells = 3, FireGlowAlpha = 0.55f;
    /// <summary>How far a glowing particle and a planner's screen light, in cells: what they pass, not their room.</summary>
    private const int ParticleReach = 2, ScreenReach = 3;
    /// <summary>
    /// How far a furnace's fire reaches, in cells: falling off by <see cref="L.FireKeep"/> a cell, it gives
    /// under a seventh of its light six cells off, where a lamp still gives near half of its own.
    /// </summary>
    private const int FireReach = 8;

    /// <summary>The columns of its cell a closed door's picture fills, in pixels: what of a door stops light.</summary>
    private static readonly (int From, int To) DoorPanel = PanelOf(PixelArt.Picture("door_closed_0").GetImage());

    private readonly IGridQueries _grid;
    private readonly Lightmap _lightmap = new();
    private readonly Starlight _starlight = new();
    private readonly Node2D _lamps = new();
    private readonly List<(Machine Lamp, Vector2 At, Sprite2D Glow)> _shown = new();
    private readonly List<(Machine Machine, Vector2 At, Sprite2D Glow)> _fires = new();
    private readonly List<LightSource> _main = new(), _spots = new();
    private Machine[] _machines = [];
    private bool _drawn;
    private int _version;
    private double _timeMs;

    /// <summary>
    /// The share of the power each lamp gets, 0 to 1: its brightness, flickering when short. Full
    /// until the power module is ported (G-04), which answers it from a lamp's satisfaction.
    /// </summary>
    public Func<Machine, float> LampShare { get; set; } = _ => 1f;

    /// <summary>
    /// How brightly a furnace's or a generator's fire burns, as a share of <see cref="L.Fire"/>: 0 while
    /// it is out, past 1 as a coal cracks. None burn until the furnace feature answers it.
    /// </summary>
    public Func<Machine, float> Fire { get; set; } = _ => 0;

    /// <summary>Where a machine of a kind gives its fire's light, in pixels from its top left; null for one with no fire.</summary>
    public Func<string, Vector2?> FireAt { get; set; } = _ => null;

    /// <summary>The lights drawn into the lightmap the last frame.</summary>
    public int LightCount => _lightmap.Drawn;
    public int LampCount => _shown.Count;

    public LightView(IGridQueries grid)
    {
        _grid = grid;
        AddChild(new CanvasModulate { Color = new Color(L.Ambient.R, L.Ambient.G, L.Ambient.B) });
        AddChild(_lightmap);
        AddChild(_starlight);
        AddChild(_lamps);
    }

    /// <summary>
    /// Brings what stops light, starlight and lamps up to the grid: called after each tick, nothing done
    /// while the grid's version stands, and then only for the cells its log names (0050).
    /// </summary>
    public void Sync()
    {
        if (_drawn && _grid.Version == _version) return;
        var log = _drawn ? _grid.ChangedSince(_version) : null;
        (_drawn, _version) = (true, _grid.Version);
        int w = _grid.Width;
        if (log == null)
        {
            // The first look, or the grid's log let go of what changed: everything laid out anew.
            _lightmap.Occupy(_grid.StructureLayer, w, _grid.Built, DoorPanel);
            _machines = _grid.Machines.ToArray();
            ShowLamps();
            _starlight.Show(_grid);
            return;
        }
        _walls.Clear();
        _lit.Clear();
        bool machines = false;
        foreach (var change in log)
        {
            if ((change.Layers & GridLayers.Structure) != 0) _walls.Add(change.Cell);
            if ((change.Layers & (GridLayers.Structure | GridLayers.Back | GridLayers.Machine)) != 0) _lit.Add(change.Cell);
            machines |= (change.Layers & GridLayers.Machine) != 0;
        }
        if (_walls.Count > 0) _lightmap.Follow(_grid.StructureLayer, w, _grid.Built, _walls, DoorPanel);
        if (machines)
        {
            _machines = _grid.Machines.ToArray();
            ShowLamps();
        }
        if (_lit.Count > 0) _starlight.Follow(_grid, _lit);
    }

    /// <summary>Scratch for <see cref="Sync"/>: the cells changed in structure, and in what stops, dims or lets in starlight.</summary>
    private readonly List<int> _walls = [], _lit = [];

    private void ShowLamps()
    {
        foreach (var node in _lamps.GetChildren()) node.QueueFree();
        _shown.Clear();
        foreach (var lamp in _machines.Where(m => m.Kind == "lamp"))
        {
            var at = new Vector2(lamp.Cell % _grid.Width + 0.5f, lamp.Cell / _grid.Width + 0.5f) * Cell;
            var glow = new Sprite2D
            {
                Texture = PixelArt.Glow,
                Position = at,
                Modulate = GlowColour,
                Scale = Vector2.One * GlowCells * Cell / PixelArt.Glow.GetWidth(),
                ZIndex = Layers.Glows,
                Material = GlowMaterial,
            };
            _lamps.AddChild(glow);
            _shown.Add((lamp, at, glow));
        }
        _fires.Clear();
        foreach (var m in _machines)
        {
            if (FireAt(m.Kind) is not { } fireAt) continue;
            var at = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * Cell + fireAt;
            var glow = new Sprite2D
            {
                Texture = PixelArt.Glow,
                Position = at,
                Scale = Vector2.One * FireGlowCells * Cell / PixelArt.Glow.GetWidth(),
                ZIndex = Layers.Glows,
                Material = GlowMaterial,
                Visible = false,
            };
            _lamps.AddChild(glow);
            _fires.Add((m, at, glow));
        }
    }

    public override void _Process(double delta)
    {
        _timeMs += delta * 1000;
        _main.Clear();
        _spots.Clear();
        foreach (var (lamp, at, glow) in _shown)
        {
            float shines = Flicker.BrightnessOf(LampShare(lamp), _timeMs, lamp.Id);
            glow.Visible = shines > 0;
            glow.Modulate = GlowColour with { A = shines };
            _main.Add(new LightSource(at.X, at.Y, L.Lamp, shines, LampReach, L.Air, L.LampMost, L.LampSize));
        }
        foreach (var (machine, at, glow) in _fires)
        {
            float burns = Fire(machine);
            glow.Visible = burns > 0;
            if (burns <= 0) continue;
            glow.Modulate = FireGlowColour with { A = Math.Min(1, FireGlowAlpha * burns) };
            _main.Add(new LightSource(at.X, at.Y, L.Fire, burns, FireReach, L.FireKeep, 1, L.FireSize));
        }
        foreach (var p in Particles.Shared.Live)
        {
            if (p.Effect.Glow is not var (colour, _)) continue;
            var rgb = (((colour >> 16) & 0xff) / 255f, ((colour >> 8) & 0xff) / 255f, (colour & 0xff) / 255f);
            _spots.Add(new LightSource(p.X, p.Y, rgb, (float)(L.Particle * p.Alpha), ParticleReach, L.ParticleKeep, 1));
        }
        foreach (var g in Glow.All)
        {
            if (!g.IsVisibleInTree()) continue;
            var at = g.GlobalPosition;
            _spots.Add(new LightSource(at.X, at.Y, (g.Colour.R, g.Colour.G, g.Colour.B), 1, ScreenReach, L.ScreenKeep, 1));
        }
        // The view in pixels of the world, as the camera last laid it.
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewport().GetVisibleRect();
        _lightmap.Frame(view, _main, _spots);
    }

    /// <summary>A glow's: added over what lies under it, unlit, for it gives light.</summary>
    private static readonly CanvasItemMaterial GlowMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    /// <summary>The columns of a cell a picture's opaque pixels span, from the first to past the last.</summary>
    private static (int From, int To) PanelOf(Image picture)
    {
        if (picture.IsCompressed()) picture.Decompress();
        var used = picture.GetUsedRect();
        return (used.Position.X, used.End.X);
    }
}
