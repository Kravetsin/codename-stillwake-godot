using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// Light without a word (0019, 0025): a dark ship, the ambient light of <see cref="L.Ambient"/> in
/// every corner, lit by its lamps — Godot lights casting soft shadows from the walls, falling off as the
/// TypeScript game's did — and by starlight through the holes in its back walls. A lit lamp glows,
/// and short of power it flickers; sparks and embers light what is around them. <see cref="Sync"/>
/// follows the grid after each tick; the shadows and the starlight change only when walls, doors, back
/// walls or machines do.
/// </summary>
public partial class LightView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>
    /// How far a lamp's light reaches, in cells: the TypeScript game's still gave a fifth there, but each
    /// light draws the shadows of every wall in its reach, and a third more reach cost half the frame rate.
    /// </summary>
    private const int LampReach = 16;
    /// <summary>A lit lamp's warm glow, and how far across it reaches, in cells.</summary>
    private static readonly Color GlowColour = new(0xffd9a0ff);
    private const float GlowCells = 3;
    /// <summary>
    /// How many glowing particles light their surroundings at once, and how far, in cells: each light draws
    /// its walls' shadows, and a furnace at work keeps a dozen embers in the air.
    /// </summary>
    private const int GlowLights = 48, GlowReach = 8;

    private readonly IGridQueries _grid;
    private readonly Occluders _occluders = new();
    private readonly Starlight _starlight = new();
    private readonly Node2D _lamps = new();
    private readonly Node2D _glowing = new();
    private readonly List<PointLight2D> _glows = new();
    private readonly List<(Machine Lamp, PointLight2D Light, Sprite2D Glow)> _shown = new();
    private readonly List<(Machine Furnace, PointLight2D Light)> _fires = new();
    private int[] _structure = [], _back = [];
    private Machine[] _machines = [];
    private bool _drawn;
    private double _timeMs;

    /// <summary>
    /// The share of the power each lamp gets, 0 to 1: its brightness, flickering when short. Full
    /// until the power module is ported (G-04), which answers it from a lamp's satisfaction.
    /// </summary>
    public Func<Machine, float> LampShare { get; set; } = _ => 1f;

    /// <summary>Whether a furnace's fire burns: then it lights its room in <see cref="L.Fire"/>. None burn until the furnace answers it.</summary>
    public Func<Machine, bool> Burns { get; set; } = _ => false;

    public int OccluderCount => _occluders.Count;
    public int LampCount => _shown.Count;

    public LightView(IGridQueries grid)
    {
        _grid = grid;
        AddChild(new CanvasModulate { Color = new Color(L.Ambient.R, L.Ambient.G, L.Ambient.B) });
        AddChild(_occluders);
        AddChild(_starlight);
        AddChild(_lamps);
        AddChild(_glowing);
    }

    /// <summary>Brings shadows, starlight and lamps up to the grid: called after each tick, cheap when nothing changed.</summary>
    public void Sync()
    {
        bool walls = !_drawn || !_grid.StructureLayer.SequenceEqual(_structure);
        bool backs = !_drawn || !_grid.BackLayer.SequenceEqual(_back);
        bool machines = !_drawn || !_machines.SequenceEqual(_grid.Machines);
        _drawn = true;
        if (walls)
        {
            _structure = _grid.StructureLayer.ToArray();
            _occluders.Show(_grid.StructureLayer, _grid.Width);
        }
        if (backs) _back = _grid.BackLayer.ToArray();
        if (machines)
        {
            _machines = _grid.Machines.ToArray();
            ShowLamps();
        }
        if (walls || backs || machines) _starlight.Show(_grid);
    }

    private void ShowLamps()
    {
        foreach (var node in _lamps.GetChildren()) node.QueueFree();
        _shown.Clear();
        foreach (var lamp in _machines.Where(m => m.Kind == "lamp"))
        {
            var at = new Vector2(lamp.Cell % _grid.Width + 0.5f, lamp.Cell / _grid.Width + 0.5f) * Cell;
            var light = new PointLight2D
            {
                Position = at,
                Texture = LampTexture,
                TextureScale = LampReach * 2f * Cell / LampTexture.GetWidth(),
                RangeItemCullMask = Lit.LampMask,
                ShadowEnabled = true,
                ShadowFilter = Light2D.ShadowFilterEnum.Pcf5,
                Height = 32,
            };
            var glow = new Sprite2D
            {
                Texture = PixelArt.Glow,
                Position = at,
                Modulate = GlowColour,
                Scale = Vector2.One * GlowCells * Cell / PixelArt.Glow.GetWidth(),
                ZIndex = 6,
                Material = new CanvasItemMaterial
                {
                    BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
                    LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
                },
            };
            _lamps.AddChild(light);
            _lamps.AddChild(glow);
            _shown.Add((lamp, light, glow));
        }
        _fires.Clear();
        var furnace = Content.Machines.Get("furnace");
        foreach (var m in _machines.Where(m => m.Kind == "furnace"))
        {
            // From the middle of its bottom row, where the TypeScript game's fire gave its light.
            var at = new Vector2(m.Cell % _grid.Width + furnace.Width / 2f, m.Cell / _grid.Width + furnace.Height - 0.5f) * Cell;
            var light = new PointLight2D
            {
                Position = at,
                Texture = FireTexture,
                TextureScale = LampReach * 2f * Cell / FireTexture.GetWidth(),
                RangeItemCullMask = Lit.LampMask,
                ShadowEnabled = true,
                ShadowFilter = Light2D.ShadowFilterEnum.Pcf5,
                Height = 32,
                Visible = false,
            };
            _lamps.AddChild(light);
            _fires.Add((m, light));
        }
    }

    public override void _Process(double delta)
    {
        _timeMs += delta * 1000;
        foreach (var (lamp, light, glow) in _shown)
        {
            float shines = Flicker.BrightnessOf(LampShare(lamp), _timeMs, lamp.Id);
            light.Energy = shines;
            light.Visible = shines > 0;
            glow.Visible = shines > 0;
            glow.Modulate = GlowColour with { A = shines };
        }
        foreach (var (furnace, light) in _fires) light.Visible = Burns(furnace);
        ShowGlows();
    }

    /// <summary>
    /// A light under each glowing particle (sparks, embers), <see cref="L.Particle"/> of its glow's colour
    /// as it fades, shadowed as a lamp's; at most <see cref="GlowLights"/>, the rest dark.
    /// </summary>
    private void ShowGlows()
    {
        int shown = 0;
        foreach (var p in Particles.Shared.Live)
        {
            if (p.Effect.Glow is not var (colour, _) || shown == GlowLights) continue;
            if (shown == _glows.Count)
            {
                var light = new PointLight2D
                {
                    Texture = GlowTexture,
                    TextureScale = GlowReach * 2f * Cell / GlowTexture.GetWidth(),
                    RangeItemCullMask = Lit.LampMask,
                    ShadowEnabled = true,
                    Height = 32,
                };
                _glowing.AddChild(light);
                _glows.Add(light);
            }
            var glow = _glows[shown++];
            glow.Visible = true;
            glow.Position = new Vector2((float)p.X, (float)p.Y);
            glow.Color = new Color(((colour >> 16) & 0xff) / 255f, ((colour >> 8) & 0xff) / 255f, (colour & 0xff) / 255f);
            glow.Energy = (float)(L.Particle * p.Alpha);
        }
        for (int i = shown; i < _glows.Count; i++) _glows[i].Visible = false;
    }

    private static ImageTexture? _lampTexture, _fireTexture, _glowTexture;

    /// <summary>
    /// A lamp's light as the TypeScript game spread it: <see cref="L.Lamp"/> fading by <see cref="L.Air"/>
    /// a cell, each colour clamped at whole, so it is white by the bulb and warm further off; faded to
    /// nothing over the last cells of <see cref="LampReach"/>.
    /// </summary>
    private static ImageTexture LampTexture => _lampTexture ??= MakeTexture(L.Lamp, LampReach);

    /// <summary>A fire's light, spread as a lamp's is, in <see cref="L.Fire"/>.</summary>
    private static ImageTexture FireTexture => _fireTexture ??= MakeTexture(L.Fire, LampReach);

    /// <summary>A glowing particle's light, white to be tinted by its glow, spread as a lamp's out to <see cref="GlowReach"/>.</summary>
    private static ImageTexture GlowTexture => _glowTexture ??= MakeTexture((1, 1, 1), GlowReach);

    private static ImageTexture MakeTexture((float R, float G, float B) colour, int reach)
    {
        // Two pixels of the world a texel: a light is sampled as unfiltered as the pixel art it falls on.
        int size = reach * Cell;
        float cellsPerTexel = reach * 2f / size;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgb8);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).Length() * cellsPerTexel;
                float f = Mathf.Pow(L.Air, d) * Mathf.Clamp((reach - d) / 4f, 0, 1);
                image.SetPixel(x, y, new Color(Math.Min(1, colour.R * f), Math.Min(1, colour.G * f), Math.Min(1, colour.B * f)));
            }
        return ImageTexture.CreateFromImage(image);
    }
}
