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
    /// <summary>The glowing plants' lights by their pictures' boxes: those near the view found each frame without walking every plant.</summary>
    private readonly BoxesByLeft<(Vector2 At, Color Colour)[]> _glowing = new();
    private readonly List<(Vector2 At, Color Colour)[]> _near = [];
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

    /// <summary>Every hero on this surface, its feet and speed across: what stirs the plants (plant-stirs).</summary>
    private readonly Func<IReadOnlyList<(Vector2 Feet, float Vx)>> _heroes;
    private readonly Stirs _stirs = new();
    private readonly Shedding _shedding = new();
    /// <summary>The plants a runner shakes leaves off, by their pictures' boxes: each with its crown, where they fall from.</summary>
    private readonly BoxesByLeft<(int Cell, Rect2 Crown)> _leafy = new();
    private readonly List<(int Cell, Rect2 Crown)> _met = [];
    private readonly List<Falling> _falling = [];

    public FloraView(IGridQueries grid, BuildingModule building, Func<IReadOnlyList<(Vector2 Feet, float Vx)>> heroes)
    {
        _grid = grid;
        _building = building;
        _heroes = heroes;
        AddChild(_falls);
    }

    public override void _Process(double delta)
    {
        long started = Game.Dev.FrameLog.Begin();
        _seconds += delta;
        if (_building.Plants.Count != _count) Follow();
        Hits(delta);
        Stir(delta);
        Falls(delta);
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewport().GetVisibleRect();
        var near = view.Grow(Cell * FloraLooks.LightReach);
        _lights.Clear();
        _sources.Clear();
        _near.Clear();
        _glowing.Meeting(near.Position.X, near.Position.Y, near.End.X, near.End.Y, _near);
        foreach (var lights in _near)
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
            _shown.Remove(cell);
            _glowing.Remove(cell);
            _leafy.Remove(cell);
            _shedding.Forget(cell);
            bool topples = !first && FloraLooks.Of[kind].Stir == FloraLooks.Stir.Topples;
            if (!first) Felled(cell, kind, picture.Texture.GetSize().Y, spray: !topples);
            if (topples) Fall(cell, picture, glow);
            else
            {
                picture.QueueFree();
                glow?.QueueFree();
            }
        }
    }

    private void Show(Plant plant)
    {
        var look = FloraLooks.Of[plant.Kind];
        uint hash = Hash(plant.Cell);
        int variant = (int)(hash % (uint)look.Variants);
        string name = $"planet/{plant.Kind}_{variant}";
        string drawn = FloraLooks.Drawn(plant.Kind, variant, hash);
        var texture = PixelArt.Picture(drawn);
        var size = texture.GetSize();
        // Planted: its lowest drawn row Sink pixels under the top of the ground it grows from, the rows there not drawn.
        int x = plant.Cell % _grid.Width, y = plant.Cell / _grid.Width;
        var (top, bottom) = UsedRows(drawn);
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
        if (look.Stir == FloraLooks.Stir.Bends) Lit.SetBending(picture);
        else Lit.Set(picture);
        AddChild(picture);
        if (look.Stir is FloraLooks.Stir.Sheds or FloraLooks.Stir.Topples)
        {
            // Its leaves: the upper part of what is drawn of it.
            var crown = new Rect2(corner.X, corner.Y + top, size.X, (bottom - top) * 0.45f);
            _leafy.Add(plant.Cell, corner.X, corner.Y + from, corner.X + size.X, corner.Y + from + rows, (plant.Cell, crown));
        }
        Sprite2D? glow = null;
        var lights = Array.Empty<(Vector2, Color)>();
        if (Glows(name) is { } shine)
        {
            glow = new Sprite2D { Texture = shine.Texture, Centered = false, Position = corner, ZIndex = Layers.Flora, Material = GlowMaterial };
            AddChild(glow);
            lights = shine.Points.Select(p => (corner + p.At, p.Colour)).ToArray();
            _glowing.Add(plant.Cell, corner.X, corner.Y + from, corner.X + size.X, corner.Y + from + size.Y, lights);
        }
        _shown[plant.Cell] = (plant.Kind, picture, glow, lights);
    }

    /// <summary>
    /// The first and last rows of <paramref name="texture"/> with anything drawn in them, found once a picture: reading a texture
    /// back is slow, and a planet's first look shows hundreds of plants of a few pictures.
    /// </summary>
    private static (int Top, int Bottom) UsedRows(string picture)
    {
        if (Used.TryGetValue(picture, out var rows)) return rows;
        var used = HiRes.Logical(picture).GetUsedRect();
        return Used[picture] = (used.Position.Y, used.End.Y - 1);
    }

    private static readonly Dictionary<string, (int Top, int Bottom)> Used = new();

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

    /// <summary>
    /// The heroes stir the grass at their feet, everything that bends bent by it (<see cref="Bend"/>); a runner whose body meets a
    /// bush or an acacia shakes a few leaves off it (plant-stirs). Reads the plants its body meets, never every plant.
    /// </summary>
    private void Stir(double delta)
    {
        var feel = FloraLooks.Feeling;
        const float half = Content.Hero.Width / 2000f * Cell, tall = Content.Hero.Height / 1000f * Cell;
        foreach (var (feet, vx) in _heroes())
        {
            _stirs.Body(feet.X, feet.Y, vx, feel);
            if (Math.Abs(vx) < FloraLooks.ScatterSpeed) continue;
            _met.Clear();
            _leafy.Meeting(feet.X - half, feet.Y - tall, feet.X + half, feet.Y, _met);
            foreach (var (cell, crown) in _met)
                if (_shedding.Sheds(cell, vx, _seconds)) Shed(cell, crown, vx);
        }
        _stirs.Advance(delta, feel);
        Bend.Push(_stirs);
    }

    /// <summary>A plant brushed by a runner: the hit's shake, and leaves drifting off its crown the way the runner went.</summary>
    private void Shed(int cell, Rect2 crown, float vx)
    {
        if (!_shown.TryGetValue(cell, out var shown)) return;
        Shake(cell, shown.Picture);
        var glims = Stages.Of(Layers.Effects).Glims;
        double ground = (cell / _grid.Width + 1) * Cell, heading = vx > 0 ? 0 : Math.PI;
        for (int i = 0; i < FloraLooks.ScatterLeaves; i++)
            glims.Spray(FloraLooks.ShedLeaf, glims.Between(crown.Position.X, crown.End.X), glims.Between(crown.Position.Y, crown.End.Y), 1,
                heading, (-0.6, 0.6), (10, 35), ground);
    }

    /// <summary>A felled tree falling: its picture turned about its foot, the fall, and the seconds it has lain.</summary>
    private sealed class Falling(Sprite2D picture, Topple fall, Vector2 foot, float tall, double ground)
    {
        public readonly Sprite2D Picture = picture;
        public readonly Topple Fall = fall;
        public readonly Vector2 Foot = foot;
        public readonly float Tall = tall;
        public readonly double Ground = ground;
        public double Lain;

        /// <summary>The middle of its crown now, in pixels of the world.</summary>
        public Vector2 Crown => Foot + new Vector2(MathF.Sin(Fall.Rotation), -MathF.Cos(Fall.Rotation)) * Tall * 0.7f;
    }

    /// <summary>
    /// A felled acacia kept to fall over (plant-stirs): pivoted at its foot, tipping away from the hero nearest it, leaves flying
    /// off its crown as it goes.
    /// </summary>
    private void Fall(int cell, Sprite2D picture, Sprite2D? glow)
    {
        var rest = _shaking.Remove(cell, out var shaking) ? shaking.Home : picture.Position;
        var size = picture.RegionRect.Size;
        // Its foot: the middle of its lowest drawn row, at the top of the soil.
        var foot = rest + new Vector2(size.X / 2, size.Y);
        picture.Offset = -new Vector2(size.X / 2, size.Y);
        picture.Position = foot;
        glow?.Reparent(picture);
        var heroes = _heroes();
        var xs = new float[heroes.Count];
        for (int i = 0; i < xs.Length; i++) xs[i] = heroes[i].Feet.X;
        var falling = new Falling(picture, new Topple(Topple.Away(foot.X, xs, FloraLooks.FellerReach), FloraLooks.FallPull), foot, size.Y, foot.Y);
        _falling.Add(falling);
        Leaves(falling.Crown, FloraLooks.FallLeaves, falling.Ground);
    }

    /// <summary>The trees falling go on falling; leaves burst where one lands; one lain its while fades and is let go.</summary>
    private void Falls(double delta)
    {
        for (int i = _falling.Count - 1; i >= 0; i--)
        {
            var f = _falling[i];
            if (!f.Fall.Lying)
            {
                if (f.Fall.Advance(delta)) Leaves(f.Crown, FloraLooks.LandLeaves, f.Ground);
                f.Picture.Rotation = f.Fall.Rotation;
                continue;
            }
            f.Lain += delta;
            double fade = (f.Lain - FloraLooks.LieSeconds) / FloraLooks.FadeSeconds;
            if (fade <= 0) continue;
            if (fade >= 1)
            {
                f.Picture.QueueFree();
                _falling.RemoveAt(i);
                continue;
            }
            f.Picture.Modulate = new Color(1, 1, 1, 1 - (float)fade);
        }
    }

    /// <summary><paramref name="count"/> leaves flung off a crown at <paramref name="at"/>, falling to <paramref name="ground"/>.</summary>
    private static void Leaves(Vector2 at, int count, double ground) =>
        Stages.Of(Layers.Effects).Glims.Spray(FloraLooks.Leaf, at.X, at.Y, (int)Math.Round(count * Fx.Count), -Math.PI / 2, (-1.6, 1.6), (25, 80), ground);

    /// <summary>A felled plant's leaves, or a rock's chips, falling where it stood; heard there. A tree that falls over (<paramref name="spray"/> false) sheds its own as it goes.</summary>
    private void Felled(int cell, string kind, float tall, bool spray)
    {
        if (!spray)
        {
            if (GroundSounds.FallOf(kind) is { } heard) _falls.Play(heard, new Vector2((float)((cell % _grid.Width + 0.5) * Cell), (cell / _grid.Width + 1) * Cell));
            return;
        }
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
        if (!HiRes.Has($"{name}_glow")) return GlowsOf[name] = null;
        return GlowsOf[name] = (PixelArt.Picture($"{name}_glow"), PointsOf(HiRes.Logical($"{name}_glow")));
    }

    private static readonly Dictionary<string, (Texture2D, (Vector2, Color)[])?> GlowsOf = new();

    /// <summary>
    /// Where a glow picture lights from: its bright pixels gathered by six-pixel squares, the brightest squares first, at most
    /// <see cref="FloraLooks.MostLights"/>, each at its pixels' middle in their colour.
    /// </summary>
    private static (Vector2 At, Color Colour)[] PointsOf(Image image)
    {
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
