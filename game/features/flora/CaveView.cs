using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;
using Structure = Stillwake.Content.Structure;
using Structures = Stillwake.Content.Structures;
using Trait = Stillwake.Content.Trait;

namespace Stillwake.Game.Features.Flora;

/// <summary>
/// What grows in a planet's caves, seen (0080): each growth the sim keeps standing (<see cref="BuildingModule.Plants"/> of a
/// cave kind) drawn as a picture of its kind lit as the world is, what of it glows added over it unlit, its light handed to the
/// light feature (<see cref="Lights"/>, the nearest the view's middle, at most <see cref="CaveLooks.MostLights"/>). Drawn by
/// chunks of 64 × 64 cells near the view, a canvas item each — tens of thousands of growths, no node apiece. Glints sparkle
/// where a helmet's beam falls on mica, a vein or a crystal; spores, drips and embers drift from what is in view.
/// </summary>
public partial class CaveView : Node2D
{
    private const int Cell = ShipCamera.Cell, Chunk = 64;

    /// <summary>A picture's rows, all of them: one not planted.</summary>
    private const int Whole = -1;

    private readonly IGridQueries _grid;
    private readonly BuildingModule _building;
    private readonly Spec.PlanetType _type;
    private readonly Func<IReadOnlyList<(Vector2 At, Vector2 Toward, Vector2 Head)>> _lamps;
    private readonly int _across;
    /// <summary>The standing cave growths by chunk, and the cells of all of them.</summary>
    private readonly Dictionary<int, List<Plant>> _byChunk = new();
    private readonly HashSet<int> _standing = [];
    /// <summary>The cells of the moss standing: a run of it joins.</summary>
    private readonly HashSet<int> _moss = [];
    private readonly Dictionary<int, Patch> _shown = new();
    private readonly List<LightSource> _lights = [];
    private readonly List<(LightSource Light, float Far)> _near = [];
    private readonly Sparkles _sparkles = new() { ZIndex = Layers.Flora, Material = GlowMaterial };
    private readonly Queue<double> _motes = new();
    private double _seconds, _moteDue;
    private int _count = -1, _version = -1;

    /// <summary>The light of the glowing growths near the view this frame, the nearest its middle first.</summary>
    public IReadOnlyList<LightSource> Lights => _lights;

    /// <summary>How many glints sparkle in a beam this frame: for <c>--measure</c>.</summary>
    public int Glinting => _sparkles.Glints.Count;

    /// <param name="lamps">Where each helmet lamp on this surface shines from and the way it points, as the light takes them.</param>
    /// <summary>A growth felled heard where it stood, by its kind; a drip heard where it splashes, now and then (0093).</summary>
    private readonly Voices _falls = new(Game.Sound.World, voices: 3);
    private readonly Heard2D _drip = Cues.Player2D(Game.Sound.CaveDrip, Game.Sound.World, voices: 2);
    private double _dripQuietS;

    public CaveView(IGridQueries grid, BuildingModule building, Spec.PlanetType type, Func<IReadOnlyList<(Vector2 At, Vector2 Toward, Vector2 Head)>> lamps)
    {
        (_grid, _building, _type, _lamps) = (grid, building, type, lamps);
        _across = (grid.Width + Chunk - 1) / Chunk;
        AddChild(_sparkles);
        AddChild(_falls);
        AddChild(_drip);
    }

    /// <summary>The mean time this view took a frame since it was last asked, in milliseconds: for <c>--measure</c>.</summary>
    public double TakeCostMs()
    {
        double ms = _frames > 0 ? _spentMs / _frames : 0;
        (_spentMs, _frames) = (0, 0);
        return ms;
    }

    private double _spentMs;
    private int _frames;

    public override void _Process(double delta)
    {
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        _seconds += delta;
        if (_building.Plants.Count != _count) Follow();
        if (_grid.Version != _version) Regrown();
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewportRect();
        Show(view);
        Light(view);
        _sparkles.Glints.Clear();
        Glints(view);
        _sparkles.Time = _seconds;
        _sparkles.QueueRedraw();
        _dripQuietS -= delta;
        Land();
        Motes(view, delta);
        _spentMs += System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        _frames++;
    }

    /// <summary>Brings the growths by chunk up to those standing: a felled one's chunk drawn again, a fall of what it was shown.</summary>
    private void Follow()
    {
        bool first = _count < 0;
        _count = _building.Plants.Count;
        var now = new HashSet<int>();
        foreach (var plant in _building.Plants)
        {
            if (!CaveLooks.Of.ContainsKey(plant.Kind)) continue;
            now.Add(plant.Cell);
            if (first && plant.Kind == "moss") _moss.Add(plant.Cell);
            if (first) Of(ChunkOf(plant.Cell)).Add(plant);
        }
        if (first)
        {
            _standing.UnionWith(now);
            return;
        }
        foreach (int cell in _standing.Where(c => !now.Contains(c)).ToList())
        {
            _standing.Remove(cell);
            _moss.Remove(cell);
            int chunk = ChunkOf(cell);
            var list = Of(chunk);
            int at = list.FindIndex(p => p.Cell == cell);
            if (at < 0) continue;
            Felled(list[at]);
            list.RemoveAt(at);
            Redraw(chunk);
        }
    }

    private List<Plant> Of(int chunk)
    {
        if (!_byChunk.TryGetValue(chunk, out var list)) _byChunk[chunk] = list = [];
        return list;
    }

    private int ChunkOf(int cell) => cell / _grid.Width / Chunk * _across + cell % _grid.Width / Chunk;

    /// <summary>The grid moved: the chunks round the cells it changed are drawn again, for moss shows on the faces open now.</summary>
    private void Regrown()
    {
        var log = _version < 0 ? null : _grid.ChangedSince(_version);
        _version = _grid.Version;
        if (log == null)
        {
            foreach (int chunk in _shown.Keys.ToList()) Redraw(chunk);
            return;
        }
        int w = _grid.Width;
        foreach (var change in log)
            // Two cells round it: a growth in the rock is two by two, kept in the chunk of its top-left (0104).
            foreach (int cell in (ReadOnlySpan<int>)[change.Cell, change.Cell - 1, change.Cell + 1, change.Cell - w, change.Cell + w,
                change.Cell - 2, change.Cell + 2, change.Cell - 2 * w, change.Cell + 2 * w])
                if (cell >= 0 && cell < w * _grid.Height) Redraw(ChunkOf(cell));
    }

    private void Redraw(int chunk)
    {
        if (!_shown.Remove(chunk, out var patch)) return;
        patch.QueueFree();
        _range = default;
    }

    /// <summary>
    /// The chunks within a chunk of the view drawn, those two past that let go: looked at only when the chunks in view change
    /// or one was let go to be drawn again, so a still view costs nothing and allocates nothing.
    /// </summary>
    private void Show(Rect2 view)
    {
        int c0 = Math.Max(0, (int)(view.Position.X / Cell / Chunk) - 1), c1 = Math.Min(_across - 1, (int)(view.End.X / Cell / Chunk) + 1);
        int r0 = Math.Max(0, (int)(view.Position.Y / Cell / Chunk) - 1), r1 = Math.Min((_grid.Height - 1) / Chunk, (int)(view.End.Y / Cell / Chunk) + 1);
        if ((c0, c1, r0, r1) == _range) return;
        _range = (c0, c1, r0, r1);
        for (int r = r0; r <= r1; r++)
            for (int c = c0; c <= c1; c++)
            {
                int chunk = r * _across + c;
                if (!_shown.ContainsKey(chunk) && _byChunk.TryGetValue(chunk, out var list) && list.Count > 0) AddChild(_shown[chunk] = Build(list));
            }
        // The glints over the pictures of the same layer: drawn last of it.
        MoveChild(_sparkles, -1);
        _far.Clear();
        foreach (int chunk in _shown.Keys)
            if (chunk % _across < c0 - 2 || chunk % _across > c1 + 2 || chunk / _across < r0 - 2 || chunk / _across > r1 + 2) _far.Add(chunk);
        foreach (int chunk in _far)
        {
            _shown[chunk].QueueFree();
            _shown.Remove(chunk);
        }
    }

    /// <summary>The chunks last looked at, their first and last columns and rows; and scratch for those let go.</summary>
    private (int, int, int, int) _range;
    private readonly List<int> _far = [];

    /// <summary>A chunk's growths as pictures, glows, lights, glints and what drifts from them.</summary>
    private Patch Build(List<Plant> plants)
    {
        var patch = new Patch();
        int w = _grid.Width;
        foreach (var plant in plants)
        {
            var look = CaveLooks.Of[plant.Kind];
            var kind = Spec.Flora.Get(plant.Kind);
            int x = plant.Cell % w, y = plant.Cell / w, band = Terrain.BandOf(_type, y);
            uint hash = Hash(plant.Cell);
            var pictures = new List<(string Name, Vector2 At, int From, int Rows, bool Front)>();
            if (plant.Kind == "moss")
            {
                // On each face of its stone open now: its top a floor's tufts, its bottom a ceiling's strands, its sides a film.
                string tone = band == 0 ? "mossGreen" : "moss";
                var corner = new Vector2(x * Cell - 4, y * Cell - 4);
                int v = (int)(hash % (uint)look.Variants);
                int own = plant.Cell, across = kind.Across, tall = kind.Tall;
                var open = FacesOpen(own, across, tall);
                // A face's band runs on along a run of moss showing the same face, the next block along, so a run joins rather
                // than repeating its piece.
                void Face(Planting.Faces face, string name, int before, int after)
                {
                    if ((open & face) == 0) return;
                    bool Runs(int n) => _moss.Contains(n) && (FacesOpen(n, across, tall) & face) != 0;
                    var band = Atlas.Band[(tone, face)];
                    int thick = (int)(face is Planting.Faces.Up or Planting.Faces.Down ? band.Size.Y : band.Size.X);
                    if (Planting.Strip(face, x, y, Cell, across, tall, thick, Runs(own + before), Runs(own + after)) is (int sx, int sy, int sw, int sh))
                        (face == Planting.Faces.Up ? patch.Front : patch.Pictures).Items.Add((new Rect2(sx, sy, sw, sh), band));
                    pictures.Add((name, corner, 0, Whole, face == Planting.Faces.Up));
                }
                Face(Planting.Faces.Up, $"caves/{tone}_top_{v}", -across, across);
                Face(Planting.Faces.Down, $"caves/{tone}_bottom_{(v + 1) % look.Variants}", -across, across);
                Face(Planting.Faces.Left, $"caves/{tone}_left_{(v + 2) % look.Variants}", -tall * w, tall * w);
                Face(Planting.Faces.Right, $"caves/{tone}_right_{(v + 3) % look.Variants}", -tall * w, tall * w);
            }
            else if (look.Variants > 0)
            {
                int v = (int)(hash % (uint)look.Variants);
                string name = look.ByBand ? $"caves/{plant.Kind}_{band}_{v}" : $"caves/{plant.Kind}_{v}";
                var size = Atlas.At[name].Size;
                if (kind.On == Spec.Flora.Anchor.Rock) pictures.Add((name, new Vector2(x * Cell - 4, y * Cell - 4), 0, Whole, false));
                else
                {
                    // Planted: its base sunk under the ground's top edge, or its top up into the ceiling, those rows not drawn.
                    var (top, bottom) = Atlas.Used[name];
                    var (px, py, from, rows) = Planting.Picture(kind.On, x, y, Cell, kind.Across, (int)size.X, (int)size.Y, top, bottom, look.Sink);
                    pictures.Add((name, new Vector2(px, py), from, rows, false));
                    if (plant.Kind == "stalactite" && hash / 7 % CaveLooks.DripShare == 0) Drip(patch, new Vector2(px + size.X / 2, py + bottom + 1));
                }
            }
            Vector2? glowAt = null;
            foreach (var (name, at, from, rows, front) in pictures)
            {
                var region = Atlas.At[name];
                var into = front ? patch.Front : patch.Pictures;
                if (rows == Whole) into.Items.Add((new Rect2(at, region.Size), region));
                else if (rows > 0) into.Items.Add((new Rect2(at.X, at.Y + from, region.Size.X, rows), new Rect2(region.Position.X, region.Position.Y + from, region.Size.X, rows)));
                if (GlowOf(name) is not { } glow) continue;
                (front ? patch.FrontShine : patch.Shine).Items.Add((new Rect2(at, region.Size), region));
                glowAt ??= at + glow.Middle;
                if (look.Glints > 0 && kind.On != Spec.Flora.Anchor.Rock)
                    foreach (var point in glow.Brightest.Take(look.Glints)) patch.Glints.Add((at + point, Colour(look.GlintColour), plant.Cell));
            }
            var colour = plant.Kind is "moss" or "roots" && band == 0 ? CaveLooks.ColdMoss : look.Light;
            // From the open, off the face of what holds it, a few pixels wide: a light in the rock lit only the rock, and a point on a face cast a hard shadow along it.
            if (colour is { } c && glowAt is { } middle && Planting.Light(kind.On, x, y, Cell, kind.Across, kind.Tall, middle.X, middle.Y, FacesOpen(plant.Cell, kind.Across, kind.Tall)) is { } shines)
                patch.Lights.Add(new LightSource(shines.X, shines.Y, c, look.Strength, look.Reach, look.Keep, 1, CaveLooks.LightSize));
            // Mica and veins glint on the faces of their stone that are open, a pixel in.
            if (kind.On == Spec.Flora.Anchor.Rock && look.Glints > 0)
                for (int i = 0; i < look.Glints; i++)
                    if (FacePoint(plant.Cell, kind.Across, kind.Tall, hash >> (i * 5)) is { } point)
                        patch.Glints.Add((point, Colour(look.GlintColour), (int)(point.Y / Cell) * w + (int)(point.X / Cell)));
            if (glowAt is { } glowing)
            {
                if (plant.Kind.StartsWith("glowcap", StringComparison.Ordinal) || plant.Kind == "hangcap") patch.Spores.Add((glowing, Colour(look.Light!.Value)));
                if (plant.Kind == "heatCrack" && hash % 3 == 0) patch.Embers.Add(glowing);
            }
        }
        return patch;
    }

    /// <summary>Whether <paramref name="cell"/> is open: nothing built or grown there.</summary>
    private bool Open(int cell) => cell >= 0 && cell < _grid.Width * _grid.Height && _grid.StructureAt(cell) == Structure.Empty;

    /// <summary>
    /// The faces open of the block <paramref name="across"/> by <paramref name="tall"/> cells down and right of
    /// <paramref name="cell"/>, a growth's in the rock: each where a cell along it is open, as the ground grows them (0104).
    /// </summary>
    private Planting.Faces FacesOpen(int cell, int across, int tall)
    {
        int w = _grid.Width, x = cell % w;
        bool Row(int from)
        {
            for (int dx = 0; dx < across && x + dx < w; dx++)
                if (Open(from + dx)) return true;
            return false;
        }
        bool Column(int from, int column)
        {
            for (int dy = 0; dy < tall && column >= 0 && column < w; dy++)
                if (Open(from + dy * w)) return true;
            return false;
        }
        return (Row(cell - w) ? Planting.Faces.Up : 0) | (Row(cell + tall * w) ? Planting.Faces.Down : 0)
            | (Column(cell - 1, x - 1) ? Planting.Faces.Left : 0) | (Column(cell + across, x + across) ? Planting.Faces.Right : 0);
    }

    /// <summary>
    /// A point a pixel inside an open face of the stone block <paramref name="across"/> by <paramref name="tall"/> cells at
    /// <paramref name="cell"/>, by <paramref name="hash"/>; null for none open.
    /// </summary>
    private Vector2? FacePoint(int cell, int across, int tall, uint hash)
    {
        int w = _grid.Width, x = cell % w, y = cell / w;
        var open = FacesOpen(cell, across, tall);
        Span<int> faces = stackalloc int[4];
        int n = 0;
        if ((open & Planting.Faces.Up) != 0) faces[n++] = 0;
        if ((open & Planting.Faces.Down) != 0) faces[n++] = 1;
        if ((open & Planting.Faces.Left) != 0) faces[n++] = 2;
        if ((open & Planting.Faces.Right) != 0) faces[n++] = 3;
        if (n == 0) return null;
        // Along a face of its 16 pixels, a few from either end.
        int along = 2 + (int)(hash / 4 % 12), inward = 1 + (int)(hash / 64 % 2);
        return faces[(int)(hash % (uint)n)] switch
        {
            0 => new Vector2(x * Cell + along, y * Cell + inward),
            1 => new Vector2(x * Cell + along, (y + tall) * Cell - 1 - inward),
            2 => new Vector2(x * Cell + inward, y * Cell + along),
            _ => new Vector2((x + across) * Cell - 1 - inward, y * Cell + along),
        } + new Vector2(0.5f, 0.5f);
    }

    /// <summary>The lights of the chunks near the view, the nearest its middle first, at most <see cref="CaveLooks.MostLights"/>.</summary>
    private void Light(Rect2 view)
    {
        _lights.Clear();
        _near.Clear();
        var middle = view.GetCenter();
        foreach (var patch in _shown.Values)
            foreach (var light in patch.Lights)
            {
                var at = new Vector2((float)light.X, (float)light.Y);
                if (view.Grow(light.Reach * Cell).HasPoint(at)) _near.Add((light, at.DistanceSquaredTo(middle)));
            }
        if (_near.Count > CaveLooks.MostLights) _near.Sort((a, b) => a.Far.CompareTo(b.Far));
        for (int i = 0; i < Math.Min(_near.Count, CaveLooks.MostLights); i++) _lights.Add(_near[i].Light);
    }

    /// <summary>The glints in view that a helmet's beam falls on, nothing between: each as bright as the beam is there.</summary>
    private void Glints(Rect2 view)
    {
        var lamps = _lamps();
        if (lamps.Count == 0) return;
        float reach = Game.Light.HeadLampReach * Cell, cosEdge = Mathf.Cos(Game.Light.HeadLampHalfAngle);
        foreach (var patch in _shown.Values)
            foreach (var (at, colour, own) in patch.Glints)
            {
                if (!view.HasPoint(at)) continue;
                float best = 0;
                for (int i = 0; i < lamps.Count; i++)
                {
                    var (from, toward, _) = lamps[i];
                    var d = at - from;
                    float far = d.Length();
                    if (far > reach || far < 1) continue;
                    float cos = d.Dot(toward) / far;
                    if (cos < cosEdge || !Seen(from, at, own)) continue;
                    best = Math.Max(best, (1 - far / reach) * Math.Min(1, (cos - cosEdge) / 0.12f));
                }
                if (best > 0.05f) _sparkles.Glints.Add((at, colour, best, (float)(Hash(own) % 1000) / 1000f * Mathf.Tau));
            }
    }

    /// <summary>Whether nothing solid stands between <paramref name="from"/> and <paramref name="to"/>, but <paramref name="own"/>, the glint's own cell.</summary>
    private bool Seen(Vector2 from, Vector2 to, int own)
    {
        int w = _grid.Width, steps = (int)(from.DistanceTo(to) / 4) + 1;
        for (int i = 1; i < steps; i++)
        {
            var p = from.Lerp(to, (float)i / steps);
            int cell = (int)(p.Y / Cell) * w + (int)(p.X / Cell);
            if (cell != own && Structures.Is(_grid.StructureAt(cell), Trait.Blocks)) return false;
        }
        return true;
    }

    /// <summary>
    /// Spores, drips and embers from what is in view: made at <see cref="CaveLooks.MotesASecond"/>, never more than
    /// <see cref="CaveLooks.MostMotes"/> alive, each from one of the sources in view drawn at random.
    /// </summary>
    private void Motes(Rect2 view, double delta)
    {
        while (_motes.Count > 0 && _motes.Peek() <= _seconds) _motes.Dequeue();
        _moteDue += delta * CaveLooks.MotesASecond;
        if (_moteDue < 1) return;
        _sources.Clear();
        foreach (var patch in _shown.Values)
        {
            foreach (var (at, colour) in patch.Spores) if (view.HasPoint(at)) _sources.Add((0, at, colour, 0));
            foreach (var (at, floor) in patch.Drips) if (view.HasPoint(at)) _sources.Add((1, at, default, floor));
            foreach (var at in patch.Embers) if (view.HasPoint(at)) _sources.Add((2, at, default, 0));
        }
        var glims = Glims.Shared;
        for (; _moteDue >= 1; _moteDue--)
        {
            if (_sources.Count == 0 || _motes.Count >= CaveLooks.MostMotes) continue;
            var (what, at, colour, landsOn) = _sources[(int)glims.Between(0, _sources.Count - 0.001)];
            Glim? made = what switch
            {
                0 => glims.Add(SporeOf(colour),
                    at.X + glims.Between(-12, 12), at.Y + glims.Between(-8, 4), glims.Between(-3, 3), glims.Between(-5, -1), bright: glims.Between(0.4, 0.9)),
                1 => glims.Add(CaveLooks.Drip, at.X, at.Y + 1, 0, 8, floor: landsOn),
                _ => glims.Add(CaveLooks.Ember_, at.X + glims.Between(-6, 6), at.Y, glims.Between(-4, 4), glims.Between(-14, -5), bright: glims.Between(0.4, 0.8)),
            };
            if (made != null) _motes.Enqueue(_seconds + made.LifeMs / 1000);
            if (made != null && what == 1) _falling.Add((made, landsOn));
        }
    }

    private readonly List<(int What, Vector2 At, Color Colour, float Floor)> _sources = [];

    /// <summary>The drips falling, each with the row of pixels it lands on.</summary>
    private readonly List<(Glim Drip, float Floor)> _falling = [];

    /// <summary>A drip from the tip of a stalactite at <paramref name="tip"/>, landing on what is under it: none with nowhere to fall.</summary>
    private void Drip(Patch patch, Vector2 tip)
    {
        if (Planting.DripFloor(_grid.StructureAt, _grid.Width, _grid.Height, Cell, tip.X, tip.Y, CaveLooks.DripRows) is double floor)
            patch.Drips.Add((tip, (float)floor));
    }

    /// <summary>Each drip that reached what it falls on splashes there and is gone: it never falls through the ground.</summary>
    private void Land()
    {
        for (int i = _falling.Count - 1; i >= 0; i--)
        {
            var (drip, floor) = _falling[i];
            if (drip.AgeMs < drip.LifeMs && drip.Y < floor - 0.01) continue;
            if (drip.AgeMs < drip.LifeMs)
            {
                drip.AgeMs = drip.LifeMs;
                Glims.Shared.Spray(CaveLooks.Splash, drip.X, floor - 0.5, CaveLooks.SplashDrops, -Math.PI / 2, (-1.1, 1.1), (14, 34), floor);
                // Heard now and then, however many fall: a cave drips, it does not rain.
                if (_dripQuietS <= 0)
                {
                    var glims = Glims.Shared;
                    _dripQuietS = glims.Between(Game.Sound.CaveDripLeastS, Game.Sound.CaveDripMostS);
                    _drip.PlayAt(new Vector2((float)drip.X, floor), 1, (float)glims.Between(Game.Sound.CaveDripLowDb, 0));
                }
            }
            _falling[i] = _falling[^1];
            _falling.RemoveAt(_falling.Count - 1);
        }
    }

    /// <summary>A spore in <paramref name="colour"/>, made once a colour rather than once a spore.</summary>
    private static Luminous SporeOf(Color colour)
    {
        if (!Spores.TryGetValue(colour, out var look))
            Spores[colour] = look = CaveLooks.Spore with { Ramp = [(236, 255, 246), ((int)(colour.R * 255), (int)(colour.G * 255), (int)(colour.B * 255))] };
        return look;
    }

    private static readonly Dictionary<Color, Luminous> Spores = new();

    /// <summary>A felled growth's fall: a glowcap's puff of spores, a crystal's glinting shards, a spike's pebbles.</summary>
    private void Felled(Plant plant)
    {
        var look = CaveLooks.Of[plant.Kind];
        var kind = Spec.Flora.Get(plant.Kind);
        int w = _grid.Width, row = plant.Cell / w;
        // Its block's middle and bottom: one on a floor stands up from its cell, any other hangs or lies down from it (0104).
        double x = (plant.Cell % w + kind.Across / 2.0) * Cell, bottom = (kind.On == Spec.Flora.Anchor.Floor ? row + 1 : row + kind.Tall) * Cell;
        double y = bottom - kind.Tall * Cell / 2.0;
        var glims = Glims.Shared;
        if (look.Light is { } c && plant.Kind != "moss" && plant.Kind != "heatCrack")
        {
            var ramp = new[] { (255, 255, 255), ((int)(c.R * 255), (int)(c.G * 255), (int)(c.B * 255)) };
            for (int i = 0; i < 10; i++)
                glims.Add(CaveLooks.Spore with { Ramp = ramp, LifeMs = (900, 1800) }, x + glims.Between(-6, 6), y + glims.Between(-6, 6), glims.Between(-18, 18), glims.Between(-24, 4));
        }
        else if (plant.Kind is "stalactite" or "stalagmite")
            glims.Spray(FloraLooks.Pebble, x, y, 6, -Math.PI / 2, (-1.4, 1.4), (20, 60), bottom);
        if (GroundSounds.FallOf(plant.Kind) is { } fall) _falls.Play(fall, new Vector2((float)x, (float)y));
    }

    private static Color Colour((int R, int G, int B) c) => new(c.R / 255f, c.G / 255f, c.B / 255f);
    private static Color Colour((float R, float G, float B) c) => new(c.R, c.G, c.B);

    /// <summary>What of picture <paramref name="name"/> glows, its middle by brightness, and its brightest pixels, the brightest first; null for none.</summary>
    private static (Texture2D Texture, Vector2 Middle, Vector2[] Brightest)? GlowOf(string name)
    {
        if (Glows.TryGetValue(name, out var known)) return known;
        if (!ResourceLoader.Exists($"res://art/{name}_glow.png")) return Glows[name] = null;
        var texture = PixelArt.Picture($"{name}_glow");
        var image = texture.GetImage();
        if (image.IsCompressed()) image.Decompress();
        var sum = Vector2.Zero;
        float weight = 0;
        var points = new List<(Vector2 At, float A)>();
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
            {
                float a = image.GetPixel(x, y).A;
                if (a < 0.05f) continue;
                sum += new Vector2(x + 0.5f, y + 0.5f) * a;
                weight += a;
                if (a > 0.85f) points.Add((new Vector2(x + 0.5f, y + 0.5f), a));
            }
        // The brightest, spread out: no two within three pixels.
        var brightest = new List<Vector2>();
        foreach (var (at, _) in points.OrderByDescending(p => p.A).ThenBy(p => p.At.Y).ThenBy(p => p.At.X))
            if (brightest.All(b => b.DistanceTo(at) > 3)) brightest.Add(at);
        return Glows[name] = (texture, weight > 0 ? sum / weight : texture.GetSize() / 2, brightest.ToArray());
    }

    private static readonly Dictionary<string, (Texture2D, Vector2, Vector2[])?> Glows = new();

    /// <summary>A glow's: added over what lies under it, unlit, for it gives light.</summary>
    private static readonly CanvasItemMaterial GlowMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    private static uint Hash(int cell)
    {
        uint h = unchecked((uint)cell * 0x85EBCA77u + 0x27D4EB2Fu);
        h ^= h >> 13;
        h = unchecked(h * 0xC2B2AE3Du);
        return h ^ (h >> 16);
    }

    /// <summary>A chunk drawn: its pictures lit, its glows added, and what it lights, glints and lets drift.</summary>
    private sealed partial class Patch : Node2D
    {
        public readonly Pictures Pictures = new() { ZIndex = Layers.Flora, Texture = Atlas.Pictures };
        public readonly Pictures Shine = new() { ZIndex = Layers.Flora, Material = GlowMaterial, Texture = Atlas.Glows };
        // Moss on a floor over the living edge's chips laid before the heroes' feet, which hid it: added after the ground's.
        public readonly Pictures Front = new() { ZIndex = Layers.GroundFront, Texture = Atlas.Pictures };
        public readonly Pictures FrontShine = new() { ZIndex = Layers.GroundFront, Material = GlowMaterial, Texture = Atlas.Glows };
        public readonly List<LightSource> Lights = [];
        public readonly List<(Vector2 At, Color Colour, int Own)> Glints = [];
        public readonly List<(Vector2 At, Color Colour)> Spores = [];
        public readonly List<(Vector2 At, float Floor)> Drips = [];
        public readonly List<Vector2> Embers = [];

        public Patch()
        {
            Lit.Set(Pictures);
            Lit.Set(Front);
            AddChild(Pictures);
            AddChild(Shine);
            AddChild(Front);
            AddChild(FrontShine);
        }
    }

    /// <summary>Pictures of the atlas drawn as one canvas item, one batch, each where it stands.</summary>
    private sealed partial class Pictures : Node2D
    {
        public required Texture2D Texture { get; init; }
        public readonly List<(Rect2 At, Rect2 Of)> Items = [];

        public override void _Draw()
        {
            foreach (var (at, of) in Items) DrawTextureRectRegion(Texture, at, of);
        }
    }

    /// <summary>
    /// Every cave picture packed into one texture, a row of the tallest first, a pixel apart, and each one's glow into a second
    /// at the same place: a chunk is drawn in a batch for its pictures and one for its glows, not one a picture.
    /// </summary>
    private static class Atlas
    {
        private const int Across = 512;
        public static readonly Texture2D Pictures, Glows;
        public static readonly Dictionary<string, Rect2> At = new();

        /// <summary>Each picture's first and last rows with anything drawn in them: its top and its base.</summary>
        public static readonly Dictionary<string, (int Top, int Bottom)> Used = new();

        /// <summary>A pixel of each moss tone's band along each face, stretched along a run of it: its colour, its thickness.</summary>
        public static readonly Dictionary<(string Tone, Planting.Faces Face), Rect2> Band = new();

        static Atlas()
        {
            var names = Names().Select(n => (Name: n, Image: ImageOf(n))).OrderByDescending(p => p.Image.GetHeight()).ThenBy(p => p.Name, StringComparer.Ordinal).ToList();
            int x = 0, y = 0, row = 0;
            foreach (var (name, image) in names)
            {
                if (x + image.GetWidth() > Across) (x, y, row) = (0, y + row + 1, 0);
                At[name] = new Rect2(x, y, image.GetWidth(), image.GetHeight());
                (x, row) = (x + image.GetWidth() + 1, Math.Max(row, image.GetHeight()));
            }
            var pictures = Image.CreateEmpty(Across, y + row + 1, false, Image.Format.Rgba8);
            var glows = Image.CreateEmpty(Across, y + row + 1, false, Image.Format.Rgba8);
            foreach (var (name, image) in names)
            {
                var at = At[name];
                var whole = new Rect2I(0, 0, (int)at.Size.X, (int)at.Size.Y);
                pictures.BlitRect(image, whole, (Vector2I)at.Position);
                var used = image.GetUsedRect();
                Used[name] = (used.Position.Y, used.End.Y - 1);
                if (GlowOf(name) is { } glow) glows.BlitRect(Faded(Rgba(glow.Texture.GetImage())), whole, (Vector2I)at.Position);
            }
            foreach (string tone in (string[])["moss", "mossGreen"])
                foreach (var (face, name, line, thick) in MossBands)
                    Band[(tone, face)] = BandOf(names.Where(n => n.Name.StartsWith($"caves/{tone}_{name}_", StringComparison.Ordinal)).ToList(), face, line, thick);
            (Pictures, Glows) = (ImageTexture.CreateFromImage(pictures), ImageTexture.CreateFromImage(glows));
        }

        /// <summary>
        /// Where each face's band of moss lies in its pictures, which are 24 × 24 round their cell: its row, along a top or a
        /// bottom, or its column, down a side; and how many pixels thick it is.
        /// </summary>
        private static readonly (Planting.Faces Face, string Name, int Line, int Thick)[] MossBands =
            [(Planting.Faces.Up, "top", 4, 1), (Planting.Faces.Down, "bottom", 19, 2), (Planting.Faces.Left, "left", 4, 1), (Planting.Faces.Right, "right", 19, 1)];

        /// <summary>The first pixel of a moss face's band in its pictures, <paramref name="thick"/> across it, in the atlas.</summary>
        private static Rect2 BandOf(List<(string Name, Image Image)> pictures, Planting.Faces face, int line, int thick)
        {
            bool across = face is Planting.Faces.Up or Planting.Faces.Down;
            foreach (var (name, image) in pictures)
                for (int i = 0; i < 24; i++)
                {
                    var (px, py) = across ? (i, line) : (line, i);
                    if (image.GetPixel(px, py).A < 0.5f) continue;
                    var at = At[name].Position + new Vector2(px, py);
                    return across ? new Rect2(at, new Vector2(1, thick)) : new Rect2(at, new Vector2(thick, 1));
                }
            throw new InvalidOperationException($"No band of moss along {face} in its pictures.");
        }

        /// <summary>The name of every cave picture: each kind's variants, by band for those that are, moss by tone and face.</summary>
        private static IEnumerable<string> Names()
        {
            foreach (var (kind, look) in CaveLooks.Of)
                for (int v = 0; v < look.Variants; v++)
                {
                    if (kind == "moss")
                    {
                        foreach (string tone in (string[])["moss", "mossGreen"])
                            foreach (string face in (string[])["top", "bottom", "left", "right"]) yield return $"caves/{tone}_{face}_{v}";
                    }
                    else if (look.ByBand)
                    {
                        for (int band = 0; band < CaveLooks.Bands; band++) yield return $"caves/{kind}_{band}_{v}";
                    }
                    else yield return $"caves/{kind}_{v}";
                }
        }

        /// <summary>How many pixels in from its picture's edge a glow fades out over: cut off at the edge, its bloom drew the picture's box.</summary>
        private const int FadeIn = 3;

        /// <summary><paramref name="glow"/> fading to nothing at its edges, over <see cref="FadeIn"/> pixels.</summary>
        private static Image Faded(Image glow)
        {
            int w = glow.GetWidth(), h = glow.GetHeight();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int edge = Math.Min(Math.Min(x, w - 1 - x), Math.Min(y, h - 1 - y));
                    if (edge >= FadeIn) continue;
                    var c = glow.GetPixel(x, y);
                    float keep = (edge + 0.5f) / FadeIn;
                    glow.SetPixel(x, y, c with { A = c.A * keep * keep });
                }
            return glow;
        }

        private static Image ImageOf(string name) => Rgba(PixelArt.Picture(name).GetImage());

        private static Image Rgba(Image image)
        {
            if (image.IsCompressed()) image.Decompress();
            if (image.GetFormat() != Image.Format.Rgba8) image.Convert(Image.Format.Rgba8);
            return image;
        }
    }

    /// <summary>The glints the beam falls on this frame: a white point and a cross of its colour, twinkling.</summary>
    private sealed partial class Sparkles : Node2D
    {
        public readonly List<(Vector2 At, Color Colour, float Bright, float Phase)> Glints = [];
        public double Time;

        public override void _Draw()
        {
            foreach (var (at, colour, bright, phase) in Glints)
            {
                float a = bright * (0.55f + 0.45f * Mathf.Sin((float)(Time * CaveLooks.GlintTwinkle * Mathf.Tau) + phase));
                var corner = new Vector2(Mathf.Floor(at.X), Mathf.Floor(at.Y));
                DrawRect(new Rect2(corner, Vector2.One), new Color(1, 1, 1, Math.Min(1, a * 1.1f)));
                for (int k = 1; k <= CaveLooks.GlintArm; k++)
                {
                    var arm = colour with { A = a * (k == 1 ? 0.8f : k == 2 ? 0.4f : 0.16f) };
                    DrawRect(new Rect2(corner + new Vector2(k, 0), Vector2.One), arm);
                    DrawRect(new Rect2(corner + new Vector2(-k, 0), Vector2.One), arm);
                    DrawRect(new Rect2(corner + new Vector2(0, k), Vector2.One), arm);
                    DrawRect(new Rect2(corner + new Vector2(0, -k), Vector2.One), arm);
                }
            }
        }
    }
}
