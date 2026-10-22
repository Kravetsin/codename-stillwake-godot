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
/// Light without a word (0019, 0029, 0070): a dark ship, the ambient light of <see cref="L.Ambient"/> in
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
    /// <summary>A lit lamp's warm glow, how far across it reaches, in cells, and how opaque at full light: a faint halo.</summary>
    private static readonly Color GlowColour = new(0xffd9a0ff), ArcGlowColour = new(0xb4dcffff);

    private static Color GlowOf(Machine lamp) => lamp.Kind == ArcLampLooks.Kind ? ArcGlowColour : GlowColour;
    private const float GlowCells = 3.5f, GlowAlpha = 0.4f;
    /// <summary>
    /// A burning fire's glow at its machine's mouth, how far across it reaches, in cells, and how opaque it
    /// is at the fire's full light: it shows the fire as the source of its light where a lamp already
    /// lights the furnace nearly whole, and jumps with it.
    /// </summary>
    private static readonly Color FireGlowColour = new(0xff8a3cff);
    private const float FireGlowCells = 6, FireGlowAlpha = 0.55f;
    /// <summary>How far a glowing particle and a planner's screen light, in cells: what they pass, not their room.</summary>
    private const int ParticleReach = 4, ScreenReach = 6;
    /// <summary>
    /// How far a furnace's fire reaches, in cells: falling off by <see cref="L.FireKeep"/> a cell, it gives
    /// under a seventh of its light twelve cells off, where a lamp still gives a third of its own.
    /// </summary>
    private const int FireReach = 16;

    /// <summary>The columns of its cell a closed door's picture fills and the rows a closed hatch's does, in pixels: what of them stops light.</summary>
    private static readonly Lightmap.Panels DoorPanels = new(
        PanelOf(ShipTiles.ImageOf("door_closed_0"), across: false),
        PanelOf(ShipTiles.ImageOf(DoorShape.Picture(closed: true, 0)), across: true));

    private readonly IGridQueries _grid;
    private readonly Lightmap _lightmap = new();
    private readonly Starlight _starlight;
    private readonly Node2D _lamps = new();
    private readonly List<(Machine Lamp, Vector2 At, Sprite2D Glow, double Floor, (Sprite2D Alight, Sprite2D Glow)? Glass)> _shown = new();
    /// <summary>Rolls an arc lamp's crackle (0091).</summary>
    private readonly Random _random = new();
    /// <summary>The sparks, glints, ember and arcs of lamps short of power (0075).</summary>
    private readonly LampSparks _sparks = new();
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

    /// <summary>
    /// Where each helmet lamp worn on this surface shines from this frame, in pixels of the world, and the way it points, a
    /// unit vector (0077): none till the crew answers it.
    /// </summary>
    public Func<IReadOnlyList<(Vector2 At, Vector2 Toward, Vector2 Head)>> HeadLamps { get; set; } = () => [];

    /// <summary>Where each glowing part of a plant near the view shines from this frame, in pixels of the world, and its colour (0079).</summary>
    public Func<IReadOnlyList<(Vector2 At, Color Colour)>> Glows { get; set; } = () => [];

    /// <summary>The light of the glowing growths in a planet's caves near the view this frame, each a small lamp of its own (0080).</summary>
    public Func<IReadOnlyList<LightSource>> Caves { get; set; } = () => [];

    /// <summary>The light of a planet's lava near the view this frame, where the open is over its surface (<c>LavaView</c>).</summary>
    public Func<IReadOnlyList<LightSource>> Lava { get; set; } = () => [];

    /// <summary>The light of the arc makers' lamps and hearths near the view this frame (0091: <c>HomesView</c>).</summary>
    public Func<IReadOnlyList<LightSource>> Homes { get; set; } = () => [];

    /// <summary>The light of the spires round their relics near the view this frame (0096: <c>SpiresView</c>).</summary>
    public Func<IReadOnlyList<LightSource>> Spires { get; set; } = () => [];

    /// <summary>The light of the arc benches' shards this frame (0099: <c>BenchView</c>).</summary>
    public Func<IReadOnlyList<LightSource>> Benches { get; set; } = () => [];

    /// <summary>Where each glowing part of a creature drawn shines from this frame and its colour (0102: <c>FaunaView</c>).</summary>
    public Func<IReadOnlyList<(Vector2 At, Color Colour)>> Creatures { get; set; } = () => [];

    /// <summary>The lights drawn into the lightmap the last frame.</summary>
    public int LightCount => _lightmap.Drawn;
    public int LampCount => _shown.Count;

    /// <param name="sky">The light through the open sky of its surface, by <see cref="L.SkyOf"/> its kind: the stars' when not given.</param>
    /// <param name="ground">Whether it is a planet's ground, where light reaches past solid rock, fading (<see cref="Lightmap.SoakThrough"/>).</param>
    public LightView(IGridQueries grid, (float R, float G, float B)? sky = null, bool ground = false)
    {
        _grid = grid;
        _lightmap.SoakThrough = ground;
        _starlight = new Starlight(sky);
        // Under a planet's sky no ambient light: what no light reaches is black (0077).
        Lit.Ambient(sky == null ? L.Ambient : (0f, 0f, 0f));
        AddChild(new CanvasModulate { Color = new Color(L.Ambient.R, L.Ambient.G, L.Ambient.B) });
        AddChild(_lightmap);
        AddChild(_starlight);
        AddChild(_lamps);
    }

    /// <summary>
    /// Brings what stops light, starlight and lamps up to the grid: called after each tick, nothing done
    /// while the grid's version stands, and then only for the cells its log names (0051).
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
            _lightmap.Occupy(_grid.StructureAt, w, w * _grid.Height, _grid.Built, DoorPanels);
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
        if (_walls.Count > 0) _lightmap.Follow(_grid.StructureAt, w, w * _grid.Height, _grid.Built, _walls, DoorPanels);
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
        foreach (var lamp in _machines.Where(m => m.Kind is "lamp" or ArcLampLooks.Kind))
        {
            // The middle of its picture, its footprint's (two by two cells, 0104).
            var spec = Stillwake.Content.Machines.Get(lamp.Kind);
            var at = new Vector2(lamp.Cell % _grid.Width + spec.Width / 2f, lamp.Cell / _grid.Width + spec.Height / 2f) * Cell;
            var glow = new Sprite2D
            {
                Texture = PixelArt.Glow,
                Position = at,
                Modulate = GlowOf(lamp),
                Scale = Vector2.One * GlowCells * Cell / PixelArt.Glow.GetWidth(),
                // An arc lamp's halo under its glass, which is added after it in the same layer: it never washes the rings to white.
                ZIndex = lamp.Kind == ArcLampLooks.Kind ? Layers.Screens : Layers.Glows,
                Material = GlowMaterial,
            };
            _lamps.AddChild(glow);
            _shown.Add((lamp, at, glow, FloorUnder(lamp.Cell), lamp.Kind == ArcLampLooks.Kind ? Glass(at) : null));
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

    /// <summary>
    /// An arc lamp's glass alight, unlit for it gives the light, and its glow added round it (<see cref="ArcLampLooks"/>), over its
    /// body in the cell whose middle is <paramref name="at"/>; hidden till it shines.
    /// </summary>
    private (Sprite2D Alight, Sprite2D Glow) Glass(Vector2 at)
    {
        var alight = new Sprite2D { Texture = PixelArt.Picture(ArcLampLooks.Alight), Position = at, ZIndex = Layers.Screens, Material = Unlit, Visible = false };
        var shine = new Sprite2D { Texture = PixelArt.Picture(ArcLampLooks.Glow), Position = at, ZIndex = Layers.Screens, Material = GlowMaterial, Visible = false };
        _lamps.AddChild(alight);
        _lamps.AddChild(shine);
        return (alight, shine);
    }

    /// <summary>An arc lamp's glass as bright as it <paramref name="shines"/>; at full power its arc crackling across it now and then.</summary>
    private void ShowGlass(Sprite2D alight, Sprite2D shine, Vector2 at, float share, float shines, double delta)
    {
        var look = ArcLampLooks.Drawn(shines);
        (alight.Visible, alight.Modulate) = (look.Alight > 0, new Color(1, 1, 1, look.Alight));
        (shine.Visible, shine.Modulate) = (look.Glow > 0, new Color(1, 1, 1, look.Glow));
        var arc = ArcLampLooks.Arc;
        if (!ArcLampLooks.Crackles(share) || _random.NextDouble() >= Crackle.Chance(arc, 0) * delta * Crackle.StepsPerSecond) return;
        var corner = at - new Vector2(ShipTiles.Drawn, ShipTiles.Drawn) / 2;
        Crackle.Strike(_random, arc, corner + new Vector2(arc.X, arc.Y), corner + new Vector2(arc.X2, arc.Y2));
    }

    public override void _Process(double delta)
    {
        long started = Game.Dev.FrameLog.Begin();
        _timeMs += delta * 1000;
        _main.Clear();
        _spots.Clear();
        _sparks.Begin();
        foreach (var (lamp, at, glow, floor, glass) in _shown)
        {
            float share = LampShare(lamp), shines = Flicker.BrightnessOf(share, _timeMs, lamp.Id);
            if (glass is { } g) ShowGlass(g.Alight, g.Glow, at, share, shines, delta);
            if (share > 0 && share < 1) _sparks.Lamp(lamp.Id, at, shines, floor, delta * 1000);
            glow.Visible = shines > 0;
            glow.Modulate = GlowOf(lamp) with { A = shines * GlowAlpha };
            // An arc makers' lamp (0091): their cold light, brighter and further.
            _main.Add(lamp.Kind == "arcLamp"
                ? new LightSource(at.X, at.Y, L.ArcLamp, shines * L.ArcLampStrength, L.ArcLampReach, L.ArcLampKeep, L.LampMost, L.LampSize)
                : new LightSource(at.X, at.Y, L.Lamp, shines, L.LampReach, L.LampKeep, L.LampMost, L.LampSize));
        }
        _sparks.End();
        foreach (var (machine, at, glow) in _fires)
        {
            float burns = Fire(machine);
            glow.Visible = burns > 0;
            if (burns <= 0) continue;
            glow.Modulate = FireGlowColour with { A = Math.Min(1, FireGlowAlpha * burns) };
            _main.Add(new LightSource(at.X, at.Y, L.Fire, burns, FireReach, L.FireKeep, 1, L.FireSize));
        }
        // A helmet lamp: its beam the way the head looks, and a faint glow round the head.
        foreach (var (at, toward, head) in HeadLamps())
        {
            var (beam, glow) = Sources.HeadLamp((at.X, at.Y), (toward.X, toward.Y), (head.X, head.Y), _blocks ??= Blocks);
            _main.Add(beam);
            _main.Add(glow);
        }
        // A plant's glowing parts: a seed pod, a berry, a bell, a small pool of light round each.
        foreach (var (at, colour) in Glows())
            _main.Add(new LightSource(at.X, at.Y, (colour.R, colour.G, colour.B), 1, FloraLooks.LightReach, FloraLooks.LightKeep, 1));
        // A creature's glowing parts: the fluff's spots and antennae, the tapir's pod, the sixleg's berries; faded as it dies.
        var creatures = Creatures();
        for (int i = 0; i < creatures.Count; i++)
        {
            var (at, colour) = creatures[i];
            _main.Add(new LightSource(at.X, at.Y, (colour.R, colour.G, colour.B), FaunaLooks.LightStrength * colour.A, FaunaLooks.LightReach,
                FaunaLooks.LightKeep, 1));
        }
        var caves = Caves();
        for (int i = 0; i < caves.Count; i++) _main.Add(caves[i]);
        var lava = Lava();
        for (int i = 0; i < lava.Count; i++) _main.Add(lava[i]);
        var homes = Homes();
        for (int i = 0; i < homes.Count; i++) _main.Add(homes[i]);
        var spires = Spires();
        for (int i = 0; i < spires.Count; i++) _main.Add(spires[i]);
        var benches = Benches();
        for (int i = 0; i < benches.Count; i++) _main.Add(benches[i]);
        // Flashes first, the brightest moments; then the particles that give light, while the cap lasts (Fx.MostLights).
        foreach (var f in Flares.Shared.Flashes)
        {
            if (_spots.Count >= Fx.MostLights) break;
            _spots.Add(new LightSource(f.X, f.Y, Rgb(f.Colour), (float)f.Strength, f.Reach, f.Keep, 1));
        }
        // Each arc lights round its middle in its cyan, the light changing only as its shape does.
        foreach (var bolt in Bolts.Shared.Live)
        {
            if (_spots.Count >= Fx.MostLights) break;
            if (bolt.Fine) continue;
            var (x0, y0, x1, y1, _) = bolt.Ends();
            // A short arc lights less than a long one: a crawl over a coil barely, a leap to a wall the room round it.
            double length = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            float strength = (float)(Fx.BoltLight * bolt.Intensity * Math.Min(1, length / 10 + 0.3) * Fx.Glow);
            _spots.Add(new LightSource((float)(x0 + x1) / 2, (float)(y0 + y1) / 2, Rgb(bolt.Tint.Glow), strength, ParticleReach + 1, L.ParticleKeep, 1));
        }
        foreach (var g in Glims.Shared.Live)
        {
            if (_spots.Count >= Fx.MostLights) break;
            if (!g.Look.Light) continue;
            var c = LuminousMath.RampAt(g.Look.Ramp, g.Age);
            float strength = L.Particle * Fx.ParticleLight * (float)(LuminousMath.Brightness(g) * Fx.Glow);
            _spots.Add(new LightSource(g.X, g.Y, ((float)c.R / 255, (float)c.G / 255, (float)c.B / 255), strength, ParticleReach, L.ParticleKeep, 1));
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
        Game.Dev.FrameLog.End("lightmap", started);
    }

    private Func<double, double, bool>? _blocks;

    /// <summary>Whether a point of the world, in pixels, lies where light stops.</summary>
    private bool Blocks(double x, double y) => Sources.Blocks(_grid.StructureAt, _grid.Width, _grid.Height, Cell, x, y);

    /// <summary>The top of the first solid cell under <paramref name="cell"/>, in pixels, within twenty; else twelve cells under it.</summary>
    private double FloorUnder(int cell)
    {
        int w = _grid.Width, x = cell % w, y = cell / w;
        for (int row = y + 1; row <= y + 20 && row < _grid.Height; row++)
            if (_grid.StructureAt(row * w + x) is Structure.Wall or Structure.Floor or Structure.DoorClosed) return row * Cell;
        return (y + 14) * Cell;
    }

    private static (float R, float G, float B) Rgb((int R, int G, int B) c) => (c.R / 255f, c.G / 255f, c.B / 255f);

    /// <summary>What gives light drawn as it is, the world's light not falling on it: an arc lamp's glass.</summary>
    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    /// <summary>A glow's: added over what lies under it, unlit, for it gives light.</summary>
    private static readonly CanvasItemMaterial GlowMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    /// <summary>The columns of a cell a picture's opaque pixels span, from the first to past the last.</summary>
    private static (int From, int To) PanelOf(Image picture, bool across)
    {
        if (picture.IsCompressed()) picture.Decompress();
        var used = picture.GetUsedRect();
        return across ? (used.Position.Y, used.End.Y) : (used.Position.X, used.End.X);
    }
}
