using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// The scorch round the core's hole in the wreck's roof (<see cref="CoreScorch"/>): its soot multiplied over the plates and their heat tint, drawn again only
/// when a plate goes or comes back; its ember line and sparks, unlit, breathing each frame while the hole is in view.
/// </summary>
public partial class CoreScorchView : Node2D
{
    private const float Fine = CoreScorch.Fine;
    // A face's strip in the ember atlases: its band's depth and the hot line, a texel clear round it.
    private const int Slot = CoreScorch.CellFine + 2;

    // The soot multiplies what is under it; faded with the front layer, it fades to white, which leaves it as it was.
    private const string SootCode = """
        shader_type canvas_item;
        render_mode blend_mul, unshaded;
        void fragment() { COLOR = vec4(mix(vec3(1.0), COLOR.rgb, COLOR.a), 1.0); }
        """;

    private readonly CoreScorch _scorch;
    private readonly Sketch _soot = new(Layers.Edges, lit: true), _tint = new(Layers.Edges, lit: true), _embers = new(Layers.Fixtures);
    private readonly Rect2 _hole;
    private ImageTexture? _sootTexture, _tintTexture, _lineAtlas, _bandAtlas;
    private Rect2I[] _strips = [];
    private double _seconds;
    private float _dot;

    public CoreScorchView(IGridQueries grid)
    {
        _scorch = new CoreScorch(grid);
        var box = CoreScorch.Box;
        // The hole and as far as a spark flies off it.
        _hole = new Rect2(World(box.X * CoreScorch.CellFine, box.Y * CoreScorch.CellFine),
            new Vector2(box.Width, box.Height) * CoreScorch.CellFine / Fine).Grow(3 * ShipCamera.Cell);
        _soot.Material = new ShaderMaterial { Shader = new Shader { Code = SootCode } };
        foreach (var node in new[] { _soot, _tint, _embers })
        {
            node.ZAsRelative = false;
            node.TextureFilter = TextureFilterEnum.Linear;
            node.Visible = false;
            AddChild(node);
        }
        _soot.Paint = g => PaintSoot(g, _sootTexture);
        _tint.Paint = g => PaintSoot(g, _tintTexture);
        _embers.Paint = PaintEmbers;
    }

    public override void _Process(double delta)
    {
        _seconds += delta;
        if (_scorch.Look())
        {
            if (_sootTexture == null && (_scorch.LiveSoot.Count > 0 || _scorch.LiveFaces.Count > 0)) Make();
            _soot.Visible = _tint.Visible = _scorch.LiveSoot.Count > 0;
            _soot.QueueRedraw();
            _tint.QueueRedraw();
        }
        var canvas = GetViewport().GetCanvasTransform();
        var view = canvas.AffineInverse() * GetViewportRect();
        _embers.Visible = _scorch.LiveFaces.Count > 0 && view.Intersects(_hole);
        if (!_embers.Visible) return;
        // A spark is a fine pixel, yet never under a pixel of the screen.
        _dot = Mathf.Max(1 / Fine, 1 / canvas.X.Length());
        _embers.QueueRedraw();
    }

    private Vector2 World(double fx, double fy) => new((float)(_scorch.Origin.X + fx / Fine), (float)(_scorch.Origin.Y + fy / Fine));

    private Rect2 WorldRect(Rect2I fine) => new(World(fine.Position.X, fine.Position.Y), new Vector2(fine.Size.X, fine.Size.Y) / Fine);

    /// <summary>The soot and its tint over the box, and each face's strips: worked out once, the first time the scorch is seen.</summary>
    private void Make()
    {
        var box = CoreScorch.Box;
        int wide = box.Width * CoreScorch.CellFine, tall = box.Height * CoreScorch.CellFine;
        var (soot, tint) = (new byte[wide * tall * 4], new byte[wide * tall * 4]);
        foreach (var (cx, cy) in CoreScorch.SootCells)
            for (int y = 0; y < CoreScorch.CellFine; y++)
                for (int x = 0; x < CoreScorch.CellFine; x++)
                {
                    int fx = cx * CoreScorch.CellFine + x, fy = cy * CoreScorch.CellFine + y;
                    var (keep, t) = CoreScorch.SootAt(fx, fy);
                    int i = ((fy - box.Y * CoreScorch.CellFine) * wide + fx - box.X * CoreScorch.CellFine) * 4;
                    (soot[i], soot[i + 1], soot[i + 2], soot[i + 3]) = (Byte(255 * keep), Byte(244 * keep), Byte(236 * keep), 255);
                    if (keep >= 1) (soot[i], soot[i + 1], soot[i + 2]) = (255, 255, 255);
                    (tint[i], tint[i + 1], tint[i + 2], tint[i + 3]) = ((byte)t.R, (byte)t.G, (byte)t.B, (byte)t.A);
                }
        _sootTexture = Texture(wide, tall, soot);
        _tintTexture = Texture(wide, tall, tint);

        var faces = CoreScorch.Faces;
        var (line, band) = (new byte[Slot * Slot * faces.Count * 4], new byte[Slot * Slot * faces.Count * 4]);
        _strips = new Rect2I[faces.Count];
        foreach (var f in faces)
        {
            var strip = StripOf(f, 0, CoreScorch.CellFine);
            _strips[f.Index] = strip;
            var shape = CoreScorch.Band(f);
            for (int s = 0; s < CoreScorch.CellFine; s++)
                for (int k = 0; k <= CoreScorch.Depth; k++)
                {
                    bool on = k == 0 ? shape[s].Core : k <= shape[s].Depth;
                    if (!on) continue;
                    var (px, py) = CoreScorch.BandPixel(f, s, k);
                    int i = ((f.Index * Slot + 1 + py - strip.Position.Y) * Slot + 1 + px - strip.Position.X) * 4;
                    var (into, c) = k == 0 ? (line, CoreScorch.Ember) : (band, CoreScorch.EmberDull);
                    (into[i], into[i + 1], into[i + 2], into[i + 3]) = ((byte)c.R, (byte)c.G, (byte)c.B, 255);
                }
        }
        _lineAtlas = Texture(Slot, Slot * faces.Count, line);
        _bandAtlas = Texture(Slot, Slot * faces.Count, band);
    }

    /// <summary>The fine pixels of a face's strip from <paramref name="from"/> along it for <paramref name="length"/>, the hot line and the band behind it.</summary>
    private static Rect2I StripOf(ScorchFace f, int from, int length)
    {
        var (a, b) = (CoreScorch.BandPixel(f, from, 0), CoreScorch.BandPixel(f, from + length - 1, CoreScorch.Depth));
        var (x, y) = (Mathf.Min(a.X, b.X), Mathf.Min(a.Y, b.Y));
        return new Rect2I(x, y, Mathf.Max(a.X, b.X) - x + 1, Mathf.Max(a.Y, b.Y) - y + 1);
    }

    private static byte Byte(double v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v), 0, 255);

    private static ImageTexture Texture(int wide, int tall, byte[] texels) =>
        ImageTexture.CreateFromImage(Image.CreateFromData(wide, tall, false, Image.Format.Rgba8, texels));

    private void PaintSoot(Sketch g, ImageTexture? texture)
    {
        if (texture == null) return;
        var box = CoreScorch.Box;
        foreach (var (x, y) in _scorch.LiveSoot)
        {
            var cell = new Rect2I(x * CoreScorch.CellFine, y * CoreScorch.CellFine, CoreScorch.CellFine, CoreScorch.CellFine);
            var region = new Rect2((x - box.X) * CoreScorch.CellFine, (y - box.Y) * CoreScorch.CellFine, CoreScorch.CellFine, CoreScorch.CellFine);
            g.DrawTextureRectRegion(texture, WorldRect(cell), region);
        }
    }

    private void PaintEmbers(Sketch g)
    {
        if (_lineAtlas == null || _bandAtlas == null) return;
        foreach (var f in _scorch.LiveFaces)
        {
            var strip = _strips[f.Index];
            for (int chunk = 0; chunk * CoreScorch.Chunk < CoreScorch.CellFine; chunk++)
            {
                var part = StripOf(f, chunk * CoreScorch.Chunk, CoreScorch.Chunk);
                var region = new Rect2(1 + part.Position.X - strip.Position.X, f.Index * Slot + 1 + part.Position.Y - strip.Position.Y, part.Size.X, part.Size.Y);
                var (line, band) = CoreScorch.Glow(CoreScorch.Breath(f, chunk, _seconds));
                var at = WorldRect(part);
                g.DrawTextureRectRegion(_bandAtlas, at, region, new Color(1, 1, 1, (float)band));
                g.DrawTextureRectRegion(_lineAtlas, at, region, new Color(1, 1, 1, (float)line));
            }
        }
        foreach (var spark in _scorch.LiveSparks)
        {
            if (CoreScorch.SparkAt(spark, _seconds) is not { } now) continue;
            var dull = CoreScorch.EmberDull;
            g.DrawRect(Dot(now.Trail), Color.Color8((byte)dull.R, (byte)dull.G, (byte)dull.B, 128));
            var hot = CoreScorch.SparkColour(now.Age);
            g.DrawRect(Dot(now.Head), Color.Color8((byte)hot.R, (byte)hot.G, (byte)hot.B));
        }
    }

    private Rect2 Dot((int X, int Y) fine) => new(World(fine.X, fine.Y), new Vector2(_dot, _dot));
}
