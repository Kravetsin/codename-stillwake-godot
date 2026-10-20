using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Flora;

/// <summary>
/// What grows in a planet's caves, seen (0080): each growth the sim keeps standing (<see cref="BuildingModule.Plants"/> of a
/// cave kind) drawn as a picture of its kind lit as the world is, what of it glows added over it unlit, its light handed to the
/// light feature (<see cref="Lights"/>, the nearest the view's middle, at most <see cref="CaveLooks.MostLights"/>). Drawn by
/// chunks of 32 × 32 cells near the view, a canvas item each — tens of thousands of growths, no node apiece. Glints sparkle
/// where a helmet's beam falls on mica, a vein or a crystal; spores, drips and embers drift from what is in view.
/// </summary>
public partial class CaveView : Node2D
{
    private const int Cell = ShipCamera.Cell, Chunk = 32;

    private readonly IGridQueries _grid;
    private readonly BuildingModule _building;
    private readonly Spec.Ground _ground;
    private readonly Func<IReadOnlyList<(Vector2 At, Vector2 Toward)>> _lamps;
    private readonly int _across;
    /// <summary>The standing cave growths by chunk, and the cells of all of them.</summary>
    private readonly Dictionary<int, List<Plant>> _byChunk = new();
    private readonly HashSet<int> _standing = [];
    private readonly Dictionary<int, Patch> _shown = new();
    private readonly List<LightSource> _lights = [];
    private readonly List<(LightSource Light, float Far)> _near = [];
    private readonly Sparkles _sparkles = new() { ZIndex = Layers.Glows, Material = GlowMaterial };
    private readonly Queue<double> _motes = new();
    private double _seconds, _moteDue;
    private int _count = -1, _version = -1;

    /// <summary>The light of the glowing growths near the view this frame, the nearest its middle first.</summary>
    public IReadOnlyList<LightSource> Lights => _lights;

    /// <summary>How many glints sparkle in a beam this frame: for <c>--measure</c>.</summary>
    public int Glinting => _sparkles.Glints.Count;

    /// <param name="lamps">Where each helmet lamp on this surface shines from and the way it points, as the light takes them.</param>
    public CaveView(IGridQueries grid, BuildingModule building, Spec.Ground ground, Func<IReadOnlyList<(Vector2 At, Vector2 Toward)>> lamps)
    {
        (_grid, _building, _ground, _lamps) = (grid, building, ground, lamps);
        _across = (grid.Width + Chunk - 1) / Chunk;
        AddChild(_sparkles);
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
            foreach (int cell in (ReadOnlySpan<int>)[change.Cell, change.Cell - 1, change.Cell + 1, change.Cell - w, change.Cell + w])
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
            int x = plant.Cell % w, y = plant.Cell / w, band = Terrain.BandOf(_ground, y);
            uint hash = Hash(plant.Cell);
            var pictures = new List<(string Name, Vector2 At)>();
            if (plant.Kind == "moss")
            {
                // On each face of its stone open now: its top a floor's tufts, its bottom a ceiling's strands, its sides a film.
                string tone = band == 0 ? "mossGreen" : "moss";
                var corner = new Vector2(x * Cell - 4, y * Cell - 4);
                int v = (int)(hash % (uint)look.Variants);
                if (Open(plant.Cell - w)) pictures.Add(($"caves/{tone}_top_{v}", corner));
                if (Open(plant.Cell + w)) pictures.Add(($"caves/{tone}_bottom_{(v + 1) % look.Variants}", corner));
                if (x > 0 && Open(plant.Cell - 1)) pictures.Add(($"caves/{tone}_left_{(v + 2) % look.Variants}", corner));
                if (x < w - 1 && Open(plant.Cell + 1)) pictures.Add(($"caves/{tone}_right_{(v + 3) % look.Variants}", corner));
            }
            else if (look.Variants > 0)
            {
                int v = (int)(hash % (uint)look.Variants);
                string name = look.ByBand ? $"caves/{plant.Kind}_{band}_{v}" : $"caves/{plant.Kind}_{v}";
                var size = Atlas.At[name].Size;
                var corner = kind.On switch
                {
                    Spec.Flora.Anchor.Floor => new Vector2(x * Cell + Cell / 2 - (int)size.X / 2, (y + 1) * Cell - (int)size.Y + 1 + (Shape.IsFloor(_grid.ShapeAt(plant.Cell + w)) ? Cell / 2 - 1 : 0)),
                    Spec.Flora.Anchor.Ceiling => new Vector2(x * Cell + Cell / 2 - (int)size.X / 2, y * Cell - 1),
                    _ => new Vector2(x * Cell - 4, y * Cell - 4),
                };
                pictures.Add((name, corner));
                if (plant.Kind == "stalactite" && hash / 7 % CaveLooks.DripShare == 0) patch.Drips.Add(corner + new Vector2(size.X / 2, size.Y));
            }
            Vector2? glowAt = null;
            foreach (var (name, at) in pictures)
            {
                var region = Atlas.At[name];
                patch.Pictures.Items.Add((new Rect2(at, region.Size), region));
                if (GlowOf(name) is not { } glow) continue;
                patch.Shine.Items.Add((new Rect2(at, region.Size), region));
                glowAt ??= at + glow.Middle;
                if (look.Glints > 0 && kind.On != Spec.Flora.Anchor.Rock)
                    foreach (var point in glow.Brightest.Take(look.Glints)) patch.Glints.Add((at + point, Colour(look.GlintColour), plant.Cell));
            }
            var colour = plant.Kind is "moss" or "roots" && band == 0 ? CaveLooks.ColdMoss : look.Light;
            if (colour is { } c && glowAt is { } middle && hash / 13 % 100 < (uint)look.LightShare)
                patch.Lights.Add(new LightSource(middle.X, middle.Y, c, look.Strength, look.Reach, look.Keep, 1));
            // Mica and veins glint on the faces of their stone that are open, a pixel in.
            if (kind.On == Spec.Flora.Anchor.Rock && look.Glints > 0)
                for (int i = 0; i < look.Glints; i++)
                    if (FacePoint(plant.Cell, hash >> (i * 5)) is { } point) patch.Glints.Add((point, Colour(look.GlintColour), plant.Cell));
            if (glowAt is { } from)
            {
                if (plant.Kind.StartsWith("glowcap", StringComparison.Ordinal) || plant.Kind == "hangcap") patch.Spores.Add((from, Colour(look.Light!.Value)));
                if (plant.Kind == "heatCrack" && hash % 3 == 0) patch.Embers.Add(from);
            }
        }
        return patch;
    }

    /// <summary>Whether <paramref name="cell"/> is open: nothing built or grown there.</summary>
    private bool Open(int cell) => cell >= 0 && cell < _grid.Width * _grid.Height && _grid.StructureAt(cell) == Structure.Empty;

    /// <summary>A point a pixel inside an open face of the stone at <paramref name="cell"/>, by <paramref name="hash"/>; null for none open.</summary>
    private Vector2? FacePoint(int cell, uint hash)
    {
        int w = _grid.Width, x = cell % w, y = cell / w;
        Span<int> faces = stackalloc int[4];
        int n = 0;
        if (Open(cell - w)) faces[n++] = 0;
        if (Open(cell + w)) faces[n++] = 1;
        if (x > 0 && Open(cell - 1)) faces[n++] = 2;
        if (x < w - 1 && Open(cell + 1)) faces[n++] = 3;
        if (n == 0) return null;
        int along = 2 + (int)(hash / 4 % 12), inward = 1 + (int)(hash / 64 % 2);
        return faces[(int)(hash % (uint)n)] switch
        {
            0 => new Vector2(x * Cell + along, y * Cell + inward),
            1 => new Vector2(x * Cell + along, (y + 1) * Cell - 1 - inward),
            2 => new Vector2(x * Cell + inward, y * Cell + along),
            _ => new Vector2((x + 1) * Cell - 1 - inward, y * Cell + along),
        } + new Vector2(0.5f, 0.5f);
    }

    /// <summary>The lights of the chunks near the view, the nearest its middle first, at most <see cref="CaveLooks.MostLights"/>.</summary>
    private void Light(Rect2 view)
    {
        _lights.Clear();
        _near.Clear();
        var near = view.Grow(Cell * 4);
        var middle = view.GetCenter();
        foreach (var patch in _shown.Values)
            foreach (var light in patch.Lights)
            {
                var at = new Vector2((float)light.X, (float)light.Y);
                if (near.HasPoint(at)) _near.Add((light, at.DistanceSquaredTo(middle)));
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
                    var (from, toward) = lamps[i];
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
            if (cell != own && _grid.StructureAt(cell) is Structure.Wall or Structure.DoorClosed) return false;
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
            foreach (var (at, colour) in patch.Spores) if (view.HasPoint(at)) _sources.Add((0, at, colour));
            foreach (var at in patch.Drips) if (view.HasPoint(at)) _sources.Add((1, at, default));
            foreach (var at in patch.Embers) if (view.HasPoint(at)) _sources.Add((2, at, default));
        }
        var glims = Glims.Shared;
        for (; _moteDue >= 1; _moteDue--)
        {
            if (_sources.Count == 0 || _motes.Count >= CaveLooks.MostMotes) continue;
            var (what, at, colour) = _sources[(int)glims.Between(0, _sources.Count - 0.001)];
            Glim? made = what switch
            {
                0 => glims.Add(SporeOf(colour),
                    at.X + glims.Between(-12, 12), at.Y + glims.Between(-8, 4), glims.Between(-3, 3), glims.Between(-5, -1), bright: glims.Between(0.4, 0.9)),
                1 => glims.Add(CaveLooks.Drip, at.X, at.Y + 1, 0, 8),
                _ => glims.Add(CaveLooks.Ember_, at.X + glims.Between(-6, 6), at.Y, glims.Between(-4, 4), glims.Between(-14, -5), bright: glims.Between(0.4, 0.8)),
            };
            if (made != null) _motes.Enqueue(_seconds + made.LifeMs / 1000);
        }
    }

    private readonly List<(int What, Vector2 At, Color Colour)> _sources = [];

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
        int w = _grid.Width;
        double x = (plant.Cell % w + 0.5) * Cell, y = (plant.Cell / w + 0.5) * Cell;
        var glims = Glims.Shared;
        if (look.Light is { } c && plant.Kind != "moss" && plant.Kind != "heatCrack")
        {
            var ramp = new[] { (255, 255, 255), ((int)(c.R * 255), (int)(c.G * 255), (int)(c.B * 255)) };
            for (int i = 0; i < 10; i++)
                glims.Add(CaveLooks.Spore with { Ramp = ramp, LifeMs = (900, 1800) }, x + glims.Between(-6, 6), y + glims.Between(-6, 6), glims.Between(-18, 18), glims.Between(-24, 4));
        }
        else if (plant.Kind is "stalactite" or "stalagmite")
            glims.Spray(FloraLooks.Pebble, x, y, 6, -Math.PI / 2, (-1.4, 1.4), (20, 60), (plant.Cell / w + 1) * Cell);
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
        public readonly Pictures Shine = new() { ZIndex = Layers.Glows, Material = GlowMaterial, Texture = Atlas.Glows };
        public readonly List<LightSource> Lights = [];
        public readonly List<(Vector2 At, Color Colour, int Own)> Glints = [];
        public readonly List<(Vector2 At, Color Colour)> Spores = [];
        public readonly List<Vector2> Drips = [], Embers = [];

        public Patch()
        {
            Lit.Set(Pictures);
            AddChild(Pictures);
            AddChild(Shine);
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
                if (GlowOf(name) is { } glow) glows.BlitRect(Rgba(glow.Texture.GetImage()), whole, (Vector2I)at.Position);
            }
            (Pictures, Glows) = (ImageTexture.CreateFromImage(pictures), ImageTexture.CreateFromImage(glows));
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
                        for (int band = 0; band <= Spec.Flora.Bands.Count; band++) yield return $"caves/{kind}_{band}_{v}";
                    }
                    else yield return $"caves/{kind}_{v}";
                }
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
