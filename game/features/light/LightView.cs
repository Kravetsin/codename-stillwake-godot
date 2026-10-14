using System;
using System.Collections.Generic;
using System.Diagnostics;
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
/// and short of power it flickers; a furnace's fire, sparks and embers light what is around them. Where
/// lights meet they add, each colour at most whole: the lit shader shares the sum out by the
/// <see cref="LightField"/> this keeps. <see cref="Sync"/> follows the grid after each tick; the
/// shadows and the starlight change only when walls, doors, back walls or machines do.
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
    /// A burning fire's glow at the furnace's mouth, how far across it reaches, in cells, and how opaque it
    /// is at the fire's full light: it shows the fire as the source of its light where a lamp already
    /// lights the furnace nearly whole, and jumps with it.
    /// </summary>
    private static readonly Color FireGlowColour = new(0xff8a3cff);
    private const float FireGlowCells = 3, FireGlowAlpha = 0.55f;
    /// <summary>
    /// How many glowing particles light their surroundings at once, and how far, in cells: each light draws
    /// its walls' shadows, and a furnace at work keeps a dozen embers in the air. A cell or two round each,
    /// so an ember lights what it passes and not its room.
    /// </summary>
    private const int GlowLights = 48, GlowReach = 3;
    /// <summary>
    /// How far a furnace's fire reaches, in cells: falling off by <see cref="L.FireKeep"/> a cell, it gives
    /// under a seventh of its light six cells off, where a lamp still gives near half of its own.
    /// </summary>
    private const int FireReach = 8;

    private readonly IGridQueries _grid;
    private readonly Occluders _occluders = new();
    private readonly Starlight _starlight = new();
    private readonly Node2D _lamps = new();
    private readonly Node2D _glowing = new();
    private readonly List<PointLight2D> _glows = new();
    private readonly List<(Machine Lamp, PointLight2D Light, Sprite2D Glow, Emitter Bulb)> _shown = new();
    private readonly List<(Machine Furnace, PointLight2D Light, Sprite2D Glow)> _fires = new();
    private int[] _structure = [], _back = [];
    private Machine[] _machines = [];
    private bool _drawn;
    private double _timeMs;
    // All the lamps' light together, for the lit shader to share out: the lamps (steady) worked out again
    // only when one of them changes, wavering fires and glowing particles added each frame some burn or glow.
    private LightField? _steady, _field;
    private Image? _fieldImage;
    private ImageTexture? _fieldTexture;
    private byte[] _fieldBytes = [];
    private readonly List<float> _steadyKey = new();
    private bool _moved;
    // The cells mode (0026): the light over the screen and round it, worked out each frame, and its texture.
    private readonly CellLight _cells = new(Cell);
    private Image? _cellImage;
    private ImageTexture? _cellTexture;
    private double _cellsMs, _cellsWorstMs;
    private long _cellsBytes;
    private int _cellsFrames, _cellsArea, _cellsKept;

    /// <summary>
    /// The share of the power each lamp gets, 0 to 1: its brightness, flickering when short. Full
    /// until the power module is ported (G-04), which answers it from a lamp's satisfaction.
    /// </summary>
    public Func<Machine, float> LampShare { get; set; } = _ => 1f;

    /// <summary>
    /// How brightly a furnace's fire burns, as a share of <see cref="L.Fire"/>: 0 while it is out, past 1
    /// as a coal cracks. None burn until the furnace feature answers it.
    /// </summary>
    public Func<Machine, float> Fire { get; set; } = _ => 0;

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
        Lit.CellsChanged += Switched;
    }

    public override void _ExitTree() => Lit.CellsChanged -= Switched;

    /// <summary>The lamps' field is worked out anew on coming back from the cells mode: their flicker went on meanwhile.</summary>
    private void Switched() => _steadyKey.Clear();

    /// <summary>
    /// What working out the cells' light has cost a frame in the cells mode, in ms, its worst, and the bytes
    /// it allocated a frame: for the dev readouts.
    /// </summary>
    public string CellsCost => _cellsFrames == 0 ? "cells=off"
        : $"cells={_cellsMs / _cellsFrames:F3}/{_cellsWorstMs:F2}ms over {_cellsFrames} frames of {_cellsArea} cells, {(_cellsKept == 0 ? 0 : (double)_cellsBytes / _cellsKept):F0} B a frame allocated once sized";

    /// <summary>How many of Godot's lights shine now: none in the cells mode.</summary>
    public int GodotLightsShown =>
        _lamps.GetChildren().OfType<Light2D>().Count(l => l.Visible) + _glows.Count(l => l.Visible) + (_starlight.Visible ? 1 : 0);

    /// <summary>Brings shadows, starlight and lamps up to the grid: called after each tick, cheap when nothing changed.</summary>
    public void Sync()
    {
        bool walls = !_drawn || !_grid.StructureLayer.SequenceEqual(_structure);
        bool backs = !_drawn || !_grid.BackLayer.SequenceEqual(_back);
        bool machines = !_drawn || !_machines.SequenceEqual(_grid.Machines);
        _drawn = true;
        if (_field is null) ShowField();
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
        _steadyKey.Clear();
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
                ZIndex = Layers.Glows,
                Material = GlowMaterial,
            };
            // Its bulb, as the cells mode lights it: the lamp's picture is a tile, laid from its cell's corner.
            var bulb = new Emitter(L.Emits.Lamp)
            {
                Picture = PixelArt.Picture("machine_lamp"),
                Position = new Vector2(lamp.Cell % _grid.Width, lamp.Cell / _grid.Width) * Cell,
            };
            _lamps.AddChild(light);
            _lamps.AddChild(glow);
            _lamps.AddChild(bulb);
            _shown.Add((lamp, light, glow, bulb));
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
                TextureScale = FireReach * 2f * Cell / FireTexture.GetWidth(),
                RangeItemCullMask = Lit.LampMask,
                ShadowEnabled = true,
                ShadowFilter = Light2D.ShadowFilterEnum.Pcf5,
                Height = 32,
                Visible = false,
            };
            var glow = new Sprite2D
            {
                Texture = PixelArt.Glow,
                Position = at,
                Scale = Vector2.One * FireGlowCells * Cell / PixelArt.Glow.GetWidth(),
                ZIndex = Layers.Glows,
                Material = GlowMaterial,
                Visible = false,
            };
            _lamps.AddChild(light);
            _lamps.AddChild(glow);
            _fires.Add((m, light, glow));
        }
    }

    public override void _Process(double delta)
    {
        _timeMs += delta * 1000;
        int key = 0;
        bool steady = false;
        void Keep(float value)
        {
            if (key == _steadyKey.Count) _steadyKey.Add(float.NaN);
            steady |= _steadyKey[key] != value;
            _steadyKey[key++] = value;
        }
        bool cells = Lit.Cells;
        _starlight.Visible = !cells;
        foreach (var (lamp, light, glow, bulb) in _shown)
        {
            float shines = Flicker.BrightnessOf(LampShare(lamp), _timeMs, lamp.Id);
            // Its strength in its colour, not its energy: the lit shader reads a light's colour as what it gives.
            light.Color = new Color(shines, shines, shines);
            light.Visible = shines > 0 && !cells;
            bulb.Strength = shines;
            glow.Visible = shines > 0;
            glow.Modulate = GlowColour with { A = shines };
            Keep(shines);
        }
        steady |= key != _steadyKey.Count;
        _steadyKey.RemoveRange(key, _steadyKey.Count - key);
        int fires = 0;
        foreach (var (furnace, light, glow) in _fires)
        {
            float burns = Fire(furnace);
            // The cells mode lights the fire from its picture's flames, and the TypeScript game's had no glow.
            light.Visible = burns > 0 && !cells;
            glow.Visible = light.Visible;
            if (!light.Visible) continue;
            light.Color = new Color(burns, burns, burns);
            glow.Modulate = FireGlowColour with { A = Math.Min(1, FireGlowAlpha * burns) };
            fires++;
        }
        int glows = cells ? HideGlows() : ShowGlows();
        if (cells)
        {
            ShowCells();
            return;
        }
        if (_steady is null || _field is null) return;
        if (steady)
        {
            _steady.Clear();
            foreach (var (_, light, _, _) in _shown)
                if (light.Visible) _steady.Add(light.Position.X / Cell, light.Position.Y / Cell, L.Lamp, L.LampMost, (light.Color.R, light.Color.G, light.Color.B), LampReach, L.Air);
        }
        bool moving = fires + glows > 0;
        if (!steady && !moving && !_moved) return;
        _moved = moving;
        _field.CopyFrom(_steady);
        foreach (var (_, light, _) in _fires)
            if (light.Visible) _field.Add(light.Position.X / Cell, light.Position.Y / Cell, L.Fire, 1, (light.Color.R, light.Color.G, light.Color.B), FireReach, L.FireKeep);
        for (int i = 0; i < glows; i++)
        {
            var g = _glows[i];
            _field.Add(g.Position.X / Cell, g.Position.Y / Cell, (1, 1, 1), 1, (g.Color.R, g.Color.G, g.Color.B), GlowReach, L.ParticleKeep);
        }
        ShowFieldLight();
    }

    /// <summary>
    /// The cells mode (0026): the light of the screen and <see cref="L.Margin"/> cells round it (half a
    /// screen at most, area.ts), worked out as the TypeScript game did: the stars, every emitter's glowing
    /// pixels and every glowing particle, spread by sweeps, handed to <see cref="Lit"/> as floats.
    /// </summary>
    private void ShowCells()
    {
        long started = Stopwatch.GetTimestamp(), allocated = GC.GetAllocatedBytesForCurrentThread();
        var view = GetCanvasTransform().AffineInverse() * GetViewportRect();
        float vx = view.Position.X / Cell, vy = view.Position.Y / Cell, vw = view.Size.X / Cell, vh = view.Size.Y / Cell;
        int across = Math.Min(L.Margin, (int)Math.Ceiling(vw / 2)), down = Math.Min(L.Margin, (int)Math.Ceiling(vh / 2));
        // As many cells as the screen can span, wherever it lies: the texture keeps its size while the camera moves.
        int x = (int)Math.Floor(vx) - across, y = (int)Math.Floor(vy) - down;
        int w = (int)Math.Ceiling(vw) + 1 + across * 2, h = (int)Math.Ceiling(vh) + 1 + down * 2;
        _cells.Begin(_grid, x, y, w, h);
        var emitters = Emitter.All;
        for (int i = 0; i < emitters.Count; i++)
        {
            var e = emitters[i];
            if (!e.Shows(out var mask, out var toWorld)) continue;
            float s = e.Emits.Strength * e.Strength;
            var (r, g, b) = e.Emits.Colour;
            _cells.Give(mask, toWorld, new System.Numerics.Vector4(r, g, b, 0) * s);
        }
        var live = Particles.Shared.Live;
        for (int i = 0; i < live.Count; i++)
        {
            var p = live[i];
            if (p.Effect.Glow is null) continue;
            var emits = p.Effect == Effect.Ember ? L.Emits.Ember : L.Emits.Spark;
            float s = emits.Strength * (float)p.Alpha;
            _cells.Give((float)p.X, (float)p.Y, new System.Numerics.Vector4(emits.Colour.R, emits.Colour.G, emits.Colour.B, 0) * s);
        }
        _cells.Spread();
        _cells.Finish();
        if (_cellImage == null || _cellTexture == null || _cellImage.GetWidth() != w || _cellImage.GetHeight() != h)
        {
            _cellImage = Image.CreateFromData(w, h, false, Image.Format.Rgbaf, _cells.Texels);
            _cellTexture = ImageTexture.CreateFromImage(_cellImage);
        }
        else
        {
            _cellImage.SetData(w, h, false, Image.Format.Rgbaf, _cells.Texels);
            _cellTexture.Update(_cellImage);
        }
        Lit.ShowCells(_cellTexture, new Rect2(x * Cell, y * Cell, w * Cell, h * Cell));
        double ms = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        // Allocation counted only once the buffers fit the screen: a zoom makes them anew.
        if (_cellsArea == w * h)
        {
            _cellsKept++;
            _cellsBytes += GC.GetAllocatedBytesForCurrentThread() - allocated;
            _cellsWorstMs = Math.Max(_cellsWorstMs, ms);
        }
        _cellsArea = w * h;
        _cellsFrames++;
        _cellsMs += ms;
    }

    /// <summary>The cells mode lights round glowing particles itself: Godot's lights under them are hidden.</summary>
    private int HideGlows()
    {
        for (int i = 0; i < _glows.Count; i++) _glows[i].Visible = false;
        return 0;
    }

    /// <summary>The field of all the lamps' light, over the grid and as far round it as a lamp reaches, handed to <see cref="Lit"/>.</summary>
    private void ShowField()
    {
        int w = _grid.Width + LampReach * 2, h = _grid.Height + LampReach * 2;
        _steady = new LightField(-LampReach, -LampReach, w, h);
        _field = new LightField(-LampReach, -LampReach, w, h);
        _fieldBytes = new byte[w * h * 16];
        _fieldImage = Image.CreateFromData(w, h, false, Image.Format.Rgbaf, _fieldBytes);
        _fieldTexture = ImageTexture.CreateFromImage(_fieldImage);
        Lit.ShowLamplight(_fieldTexture, new Rect2(-LampReach * Cell, -LampReach * Cell, w * Cell, h * Cell));
    }

    /// <summary>The field as it stands into its texture, as floats: light passes whole where lights meet.</summary>
    private void ShowFieldLight()
    {
        Buffer.BlockCopy(_field!.Light, 0, _fieldBytes, 0, _fieldBytes.Length);
        _fieldImage!.SetData(_field.Width, _field.Height, false, Image.Format.Rgbaf, _fieldBytes);
        _fieldTexture!.Update(_fieldImage);
    }

    /// <summary>
    /// A light under each glowing particle (sparks, embers), <see cref="L.Particle"/> of its glow's colour
    /// as it fades, shadowed as a lamp's; at most <see cref="GlowLights"/>, the rest dark. Answers how many glow.
    /// </summary>
    private int ShowGlows()
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
            float shines = (float)(L.Particle * p.Alpha);
            glow.Color = new Color(((colour >> 16) & 0xff) / 255f * shines, ((colour >> 8) & 0xff) / 255f * shines, (colour & 0xff) / 255f * shines);
        }
        for (int i = shown; i < _glows.Count; i++) _glows[i].Visible = false;
        return shown;
    }

    /// <summary>A glow's: added over what lies under it, unlit, for it gives light.</summary>
    private static readonly CanvasItemMaterial GlowMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    private static ImageTexture? _lampTexture, _fireTexture, _glowTexture;

    /// <summary>
    /// A lamp's light as the TypeScript game spread it: <see cref="L.Lamp"/> fading by <see cref="L.Air"/>
    /// a cell, each colour clamped at <see cref="L.LampMost"/>, so it is white by the bulb and warm further
    /// off; faded to nothing over the last cells of <see cref="LampReach"/>.
    /// </summary>
    private static ImageTexture LampTexture => _lampTexture ??= MakeTexture(L.Lamp, L.LampMost, LampReach, L.Air);

    /// <summary>A fire's light in <see cref="L.Fire"/>, falling off by <see cref="L.FireKeep"/> a cell out to <see cref="FireReach"/>.</summary>
    private static ImageTexture FireTexture => _fireTexture ??= MakeTexture(L.Fire, 1, FireReach, L.FireKeep);

    /// <summary>A glowing particle's light, white to be tinted by its glow, by <see cref="L.ParticleKeep"/> a cell out to <see cref="GlowReach"/>.</summary>
    private static ImageTexture GlowTexture => _glowTexture ??= MakeTexture((1, 1, 1), 1, GlowReach, L.ParticleKeep);

    private static ImageTexture MakeTexture((float R, float G, float B) colour, float most, int reach, float keep)
    {
        // Two pixels of the world a texel: a light is sampled as unfiltered as the pixel art it falls on.
        int size = reach * Cell;
        float cellsPerTexel = reach * 2f / size;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgb8);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).Length() * cellsPerTexel;
                var (r, g, b) = LightField.At(colour, most, d, reach, keep);
                image.SetPixel(x, y, new Color(r, g, b));
            }
        return ImageTexture.CreateFromImage(image);
    }
}
