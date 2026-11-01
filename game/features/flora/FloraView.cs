using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content.Flora;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Features.Flora;

/// <summary>
/// What grows on a planet's surface, seen (0079): each plant the sim keeps standing (<see cref="BuildingModule.Plants"/>)
/// drawn as a picture of its kind lit as the world is, what of it glows added over it unlit, and its glowing parts' light
/// handed to the light feature (<see cref="Lights"/>). A plant felled is taken away in a fall of leaves. While glowing plants
/// are in view, motes drift about them and in the open air over the ground, a few at a time, none made off the screen.
/// </summary>
public partial class FloraView : Node2D, ILightSource
{
    private const int Cell = ShipCamera.Cell;

    private readonly IGridQueries _grid;
    private readonly BuildingModule _building;
    private readonly Dictionary<int, (string Kind, Sprite2D Picture, Sprite2D? Glow, (Vector2 At, Color Colour)[] Lights)> _shown = new();
    private readonly List<(Vector2 At, Color Colour)> _lights = [];
    /// <summary>Scratch for <see cref="Motes"/>: the lights on screen.</summary>
    private readonly List<(Vector2 At, Color Colour)> _lit = [];
    private readonly Queue<double> _motes = new();
    private double _seconds, _moteDue;
    private int _count = -1;

    private readonly List<LightSource> _sources = [];

    /// <summary>The light of every glowing part of a plant near the view this frame, a small pool round each (0079).</summary>
    public IReadOnlyList<LightSource> Lights => _sources;

    /// <summary>A plant felled heard where it stood, by its kind, whoever felled it (0093).</summary>
    private readonly Voices _falls = new(Game.Sound.World, voices: 3);

    public FloraView(IGridQueries grid, BuildingModule building)
    {
        _grid = grid;
        _building = building;
        AddChild(_falls);
    }

    public override void _Process(double delta)
    {
        long started = Game.Dev.FrameLog.Begin();
        _seconds += delta;
        if (_building.Plants.Count != _count) Follow();
        Hits(delta);
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewport().GetVisibleRect();
        var near = view.Grow(Cell * FloraLooks.LightReach);
        _lights.Clear();
        _sources.Clear();
        foreach (var (_, (_, picture, _, lights)) in _shown)
            if (lights.Length > 0 && near.Intersects(new Rect2(picture.Position, picture.Texture.GetSize())))
                foreach (var (at, colour) in lights)
                {
                    _lights.Add((at, colour));
                    _sources.Add(new LightSource(at.X, at.Y, (colour.R, colour.G, colour.B), 1, FloraLooks.LightReach, FloraLooks.LightKeep, 1));
                }
        Motes(view, delta);
        Game.Dev.FrameLog.End("flora", started);
    }

    /// <summary>Brings the pictures up to the plants standing: a new one drawn, a felled one taken away in a fall of leaves.</summary>
    private void Follow()
    {
        bool first = _count < 0;
        _count = _building.Plants.Count;
        var standing = new HashSet<int>();
        foreach (var plant in _building.Plants)
        {
            // The caves' growth is drawn by chunks (CaveView, 0080).
            if (!FloraLooks.Of.ContainsKey(plant.Kind)) continue;
            standing.Add(plant.Cell);
            if (!_shown.ContainsKey(plant.Cell)) Show(plant);
        }
        foreach (int cell in _shown.Keys.Where(c => !standing.Contains(c)).ToList())
        {
            var (kind, picture, glow, _) = _shown[cell];
            picture.QueueFree();
            glow?.QueueFree();
            _shown.Remove(cell);
            if (!first) Felled(cell, kind, picture.Texture.GetSize().Y);
        }
    }

    private void Show(Plant plant)
    {
        var look = FloraLooks.Of[plant.Kind];
        int variant = (int)(Hash(plant.Cell) % (uint)look.Variants);
        string name = $"planet/{plant.Kind}_{variant}";
        var texture = PixelArt.Picture(name);
        var size = texture.GetSize();
        // Planted: its lowest drawn row Sink pixels under the top of the ground it grows from, the rows there not drawn.
        int x = plant.Cell % _grid.Width, y = plant.Cell / _grid.Width;
        var (top, bottom) = UsedRows(PixelArt.Logical(name));
        var (px, py, from, rows) = Planting.Picture(Spec.Anchor.Floor, x, y, Cell, Spec.Get(plant.Kind).Across, (int)size.X, (int)size.Y, top, bottom, look.Sink);
        var corner = new Vector2(px, py);
        var picture = new Sprite2D
        {
            Texture = texture,
            Centered = false,
            Position = corner + new Vector2(0, from),
            RegionEnabled = true,
            RegionRect = new Rect2(0, from, size.X, rows),
            ZIndex = Layers.Flora,
        };
        Lit.Set(picture);
        AddChild(picture);
        Sprite2D? glow = null;
        var lights = Array.Empty<(Vector2, Color)>();
        if (Glows(name) is { } shine)
        {
            glow = new Sprite2D { Texture = shine.Texture, Centered = false, Position = corner, ZIndex = Layers.Flora, Material = GlowMaterial };
            AddChild(glow);
            lights = shine.Points.Select(p => (corner + p.At, p.Colour)).ToArray();
        }
        _shown[plant.Cell] = (plant.Kind, picture, glow, lights);
    }

    /// <summary>
    /// The first and last rows of <paramref name="texture"/> with anything drawn in them, found once a picture: reading a texture
    /// back is slow, and a planet's first look shows hundreds of plants of a few pictures.
    /// </summary>
    private static (int Top, int Bottom) UsedRows(Texture2D texture)
    {
        if (Used.TryGetValue(texture, out var rows)) return rows;
        var image = texture.GetImage();
        if (image.IsCompressed()) image.Decompress();
        var used = image.GetUsedRect();
        return Used[texture] = (used.Position.Y, used.End.Y - 1);
    }

    private static readonly Dictionary<Texture2D, (int Top, int Bottom)> Used = new();

    /// <summary>The depth of each plant's crack last seen, by its cell, and the plants shaking: their picture's resting place and the seconds left.</summary>
    private readonly Dictionary<int, int> _cracks = new();
    private readonly Dictionary<int, (Vector2 Home, double Left)> _shaking = new();
    private readonly List<int> _scratch = [];

    /// <summary>
    /// A hit that deepens a plant's crack (it has strength, 0079) and leaves it standing: a short shake, so a tree taking several
    /// swings answers each; what it knocks off is thrown where it struck (<c>HitBurst</c>, 0094). Reads the cracks, a short list,
    /// each frame.
    /// </summary>
    private void Hits(double delta)
    {
        _scratch.Clear();
        foreach (var crack in _building.Cracks)
        {
            if (!_shown.TryGetValue(crack.Cell, out var shown)) continue;
            _scratch.Add(crack.Cell);
            if (_cracks.TryGetValue(crack.Cell, out int was) && was >= crack.Damage) continue;
            _cracks[crack.Cell] = crack.Damage;
            Shake(crack.Cell, shown.Picture);
        }
        foreach (int cell in _cracks.Keys.Where(c => !_scratch.Contains(c)).ToList()) _cracks.Remove(cell);
        foreach (var (cell, (rest, left)) in _shaking.ToList())
        {
            bool gone = !_shown.TryGetValue(cell, out var shown);
            if (gone || left - delta <= 0)
            {
                if (!gone) shown.Picture.Position = rest;
                _shaking.Remove(cell);
                continue;
            }
            shown.Picture.Position = rest + new Vector2((float)Math.Round(Math.Sin((left - delta) * 90) * FloraLooks.Shake), 0);
            _shaking[cell] = (rest, left - delta);
        }
    }

    private void Shake(int cell, Sprite2D picture)
    {
        var rest = _shaking.TryGetValue(cell, out var shaking) ? shaking.Home : picture.Position;
        _shaking[cell] = (rest, FloraLooks.ShakeSeconds);
    }

    /// <summary>A felled plant's leaves, or a rock's chips, falling where it stood.</summary>
    private void Felled(int cell, string kind, float tall)
    {
        int w = _grid.Width;
        double x = (cell % w + Spec.Get(kind).Across / 2.0) * Cell, ground = (cell / w + 1) * Cell, y = ground - Math.Min(tall, 6 * Cell) / 2;
        var look = kind == "rock" ? FloraLooks.Pebble : FloraLooks.Leaf;
        Stages.Of(Layers.Effects).Glims.Spray(look, x, y, (int)Math.Round(FloraLooks.Fallen * Fx.Count), -Math.PI / 2, (-1.4, 1.4), (20, 60), ground);
        if (GroundSounds.FallOf(kind) is { } fall) _falls.Play(fall, new Vector2((float)x, (float)y));
    }

    /// <summary>
    /// Motes in the daylight: made at <see cref="FloraLooks.MotesASecond"/> while a glowing plant is in view, never more than
    /// <see cref="FloraLooks.MostMotes"/> alive; most by a glowing part, the rest over the ground in the open air.
    /// </summary>
    private void Motes(Rect2 view, double delta)
    {
        while (_motes.Count > 0 && _motes.Peek() <= _seconds) _motes.Dequeue();
        var lit = _lit;
        lit.Clear();
        foreach (var light in _lights)
            if (view.HasPoint(light.At)) lit.Add(light);
        if (lit.Count == 0) return;
        var glims = Stages.Of(Layers.Flora).Glims;
        _moteDue += delta * FloraLooks.MotesASecond;
        for (; _moteDue >= 1; _moteDue--)
        {
            if (_motes.Count >= FloraLooks.MostMotes) continue;
            double x, y;
            Luminous look;
            if (glims.Between(0, 1) < FloraLooks.NearShare)
            {
                var (at, colour) = lit[(int)glims.Between(0, lit.Count - 0.001)];
                (x, y) = (at.X + glims.Between(-14, 14), at.Y + glims.Between(-16, 6));
                var c = ((int)(colour.R * 255), (int)(colour.G * 255), (int)(colour.B * 255));
                look = FloraLooks.Mote with { Ramp = [(255, 255, 255), c] };
            }
            else
            {
                x = glims.Between(view.Position.X, view.End.X);
                if (GroundUnder(x, view) is not double ground) continue;
                y = ground - glims.Between(4, 12 * Cell);
                look = FloraLooks.Mote with { Ramp = FloraLooks.AirMote };
            }
            var mote = glims.Add(look, x, y, glims.Between(-3, 3), glims.Between(-4, 0), bright: glims.Between(0.4, 0.9), size: glims.Between(0.5, 0.9));
            if (mote != null) _motes.Enqueue(_seconds + look.LifeMs.To / 1000);
        }
    }

    /// <summary>The top of the first wall down column <paramref name="x"/> (pixels) from the top of the view, under open sky; null for none in it.</summary>
    private double? GroundUnder(double x, Rect2 view)
    {
        int column = (int)(x / Cell), w = _grid.Width;
        if (column < 0 || column >= w) return null;
        for (int row = Math.Max(0, (int)(view.Position.Y / Cell)); row < Math.Min(_grid.Height, (int)(view.End.Y / Cell) + 1); row++)
        {
            int cell = row * w + column;
            if (_grid.StructureAt(cell) != Structure.Empty) return _grid.WallAt(cell) != null ? row * Cell : null;
            if (_grid.BackAt(cell) != null) return null;
        }
        return null;
    }

    /// <summary>What of picture <paramref name="name"/> glows, and the points it lights from with their colours; null for none.</summary>
    private static (Texture2D Texture, (Vector2 At, Color Colour)[] Points)? Glows(string name)
    {
        if (GlowsOf.TryGetValue(name, out var known)) return known;
        string path = $"res://art/{name}_glow.png";
        if (!ResourceLoader.Exists(path)) return GlowsOf[name] = null;
        return GlowsOf[name] = (PixelArt.Picture($"{name}_glow"), PointsOf(PixelArt.Logical($"{name}_glow")));
    }

    private static readonly Dictionary<string, (Texture2D, (Vector2, Color)[])?> GlowsOf = new();

    /// <summary>
    /// Where a glow picture lights from: its bright pixels gathered by six-pixel squares, the brightest squares first, at most
    /// <see cref="FloraLooks.MostLights"/>, each at its pixels' middle in their colour.
    /// </summary>
    private static (Vector2 At, Color Colour)[] PointsOf(Texture2D texture)
    {
        var image = texture.GetImage();
        if (image.IsCompressed()) image.Decompress();
        var squares = new Dictionary<(int, int), (Vector2 Sum, Vector3 Colour, int Count)>();
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
            {
                var c = image.GetPixel(x, y);
                if (c.A < 0.6f) continue;
                var key = (x / 6, y / 6);
                var (sum, colour, count) = squares.GetValueOrDefault(key);
                squares[key] = (sum + new Vector2(x + 0.5f, y + 0.5f), colour + new Vector3(c.R, c.G, c.B), count + 1);
            }
        return squares.Values.OrderByDescending(s => s.Count).ThenBy(s => s.Sum.Y).Take(FloraLooks.MostLights)
            .Select(s => (s.Sum / s.Count, new Color(s.Colour.X / s.Count, s.Colour.Y / s.Count, s.Colour.Z / s.Count) * FloraLooks.LightStrength)).ToArray();
    }

    /// <summary>A glow's: added over what lies under it, unlit, for it gives light.</summary>
    private static readonly CanvasItemMaterial GlowMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    private static uint Hash(int cell)
    {
        uint h = unchecked((uint)cell * 0x85EBCA77u);
        h ^= h >> 13;
        h = unchecked(h * 0xC2B2AE3Du);
        return h ^ (h >> 16);
    }
}
