using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A planet's sky behind its ground (0079), in the "Stillwake" style (<c>art/planet</c>): the day's gradient, a pale gas
/// giant, high thin cloud and the sun with its glint. Drawn in screen space under the world, a pixel of art a pixel of the
/// world at the camera's zoom; the gradient's horizon lies where the land's ground line does, moved by its share of the
/// camera's height. The land in front of it is composed from the planet's own pictures (<c>Features.Backdrop</c>).
/// </summary>
public partial class PlanetSky : Node2D
{
    /// <summary>The row of the gradient where the land meets the ground: the land's ground line lies there.</summary>
    private const float GroundLine = 272;

    /// <summary>How much of the camera's movement the sky, the far things in it and the clouds follow.</summary>
    private const float SkyFollows = 0.04f, CloudFollows = 0.06f;

    /// <summary>How fast the clouds drift by, in pixels of art a second.</summary>
    private const float CloudDrift = 1.5f;

    /// <summary>
    /// Where the giant hangs and the sun is at noon, their middles as shares of the screen across and down: on the screen, not by the
    /// ground, so they stay in the sky however near the camera is. The clouds' top, likewise.
    /// </summary>
    private static readonly Vector2 Noon = new(0.5f, 0.08f), Giant = new(0.17f, 0.22f);
    private const float CloudsAt = 0.02f;

    /// <summary>
    /// Shares of the screen across where the sun rises and sets, and down where it does: about the far mountains' tops, so it clears
    /// them early in the day and sinks into them only in the dusk (the user's, 2026-11-17). It crosses the sky left to right.
    /// </summary>
    private const float Rises = 0.06f, Sets = 0.94f, Rim = 0.22f;

    /// <summary>How far across the sky the sun is, 0 rising to 1 setting (<c>Daylight.Sun</c>); null while it is down.</summary>
    public float? Day { get; set; }

    private readonly ColorRect _above = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Sprite2D _sky, _giant, _clouds, _sun;

    /// <param name="set">The folder of its pictures under <c>art</c>: its planet type's backdrop (<see cref="PlanetLooks"/>, 0095).</param>
    public PlanetSky(string set)
    {
        var sky = PixelArt.Far($"{set}/sky");
        _above.Color = TopOf(sky);
        AddChild(_above);
        AddChild(_sky = Layer(sky, repeat: false));
        AddChild(_giant = Layer(PixelArt.Picture($"{set}/giant"), repeat: false));
        AddChild(_clouds = Layer(PixelArt.Far($"{set}/clouds"), repeat: true));
        _sun = Layer(PixelArt.Picture($"{set}/sun"), repeat: false);
        _sun.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        AddChild(_sun);
        AddChild(_haze);
    }

    private static Sprite2D Layer(Texture2D texture, bool repeat) => new()
    {
        Texture = texture,
        Centered = false,
        RegionEnabled = repeat,
        TextureRepeat = repeat ? CanvasItem.TextureRepeatEnum.Enabled : CanvasItem.TextureRepeatEnum.Disabled,
    };

    /// <summary>
    /// Lays the layers for a camera looking at <paramref name="centre"/> (pixels of the world) at <paramref name="zoom"/> on
    /// a <paramref name="screen"/>, the land's ground line at row <paramref name="ground"/> (pixels), <paramref name="seconds"/> into the world.
    /// </summary>
    public void Follow(Vector2 centre, float zoom, Vector2 screen, float ground, double seconds)
    {
        float sky = screen.Y / 2 + (ground - centre.Y) * zoom * SkyFollows;
        _above.Size = screen;
        var gradient = _sky.Texture.GetSize();
        _sky.Position = new Vector2(0, sky - GroundLine * zoom);
        _sky.Scale = new Vector2(screen.X / gradient.X, zoom);
        _giant.Position = screen * Giant - _giant.Texture.GetSize() * zoom / 2;
        _giant.Scale = Vector2.One * zoom;
        _sun.Visible = Day != null;
        if (Day is float day)
        {
            // An arc from the rim up to noon's height, steep at its ends: the sun is soon high and low only near its rise and set.
            float high = Mathf.Sqrt(Mathf.Max(0, Mathf.Sin(Mathf.Pi * day)));
            var at = new Vector2(Mathf.Lerp(Rises, Sets, day) * screen.X, Mathf.Lerp(Rim, Noon.Y, high) * screen.Y);
            _sun.Position = at - _sun.Texture.GetSize() * zoom / 2;
            _sun.Scale = Vector2.One * zoom;
        }
        Spread(_clouds, centre.X * CloudFollows + (float)(seconds * CloudDrift + _blown), zoom, screen, screen.Y * CloudsAt);
        if (_haze.Share > 0 && (_haze.Horizon != sky || _haze.Screen != screen))
        {
            (_haze.Horizon, _haze.Screen) = (sky, screen);
            _haze.QueueRedraw();
        }
    }

    /// <summary>
    /// A planet's weather over its sky (docs/plans/surface-storms.md): the sky dulled toward <paramref name="colour"/>, from
    /// <paramref name="top"/> of <paramref name="share"/> at the screen's top to all of it at the horizon and below; the high cloud
    /// drifting <paramref name="blow"/> pixels of art a second faster. None at a share of 0.
    /// </summary>
    public void Weather((float R, float G, float B) colour, float share, float top, float blow)
    {
        _blow = blow;
        if (_haze.Colour == colour && _haze.Share == share && _haze.Top == top) return;
        (_haze.Colour, _haze.Share, _haze.Top) = (colour, share, top);
        _haze.QueueRedraw();
    }

    public override void _Process(double delta) => _blown += delta * _blow;

    private double _blown;
    private float _blow;
    private readonly Haze _haze = new();

    /// <summary>The weather's haze over the sky, in screen space: a gradient down to the horizon, whole below it.</summary>
    private sealed partial class Haze : Node2D
    {
        public (float R, float G, float B) Colour;
        public float Share, Top, Horizon;
        public Vector2 Screen;

        public override void _Draw()
        {
            if (Share <= 0) return;
            var low = new Color(Colour.R, Colour.G, Colour.B, Share);
            var high = low with { A = Share * Top };
            float horizon = Mathf.Clamp(Horizon, 0, Screen.Y);
            if (horizon > 0)
                DrawPolygon([new(0, 0), new(Screen.X, 0), new(Screen.X, horizon), new(0, horizon)], [high, high, low, low]);
            if (horizon < Screen.Y) DrawRect(new Rect2(0, horizon, Screen.X, Screen.Y - horizon), low);
        }
    }

    /// <summary>A repeating layer over the whole screen's width, its top at <paramref name="y"/>, looking at <paramref name="x"/> pixels of art across.</summary>
    private static void Spread(Sprite2D layer, float x, float zoom, Vector2 screen, float y)
    {
        float across = screen.X / zoom;
        // From its second row: a smoothly sampled repeat would blend its bottom row into its top, a line across the sky.
        layer.RegionRect = new Rect2(Mathf.PosMod(x - across / 2, layer.Texture.GetWidth()), 1, across, layer.Texture.GetHeight() - 1);
        layer.Position = new Vector2(0, y + zoom);
        layer.Scale = Vector2.One * zoom;
    }

    /// <summary>The first pixel of the picture's top row as drawn.</summary>
    private static Color TopOf(Texture2D texture)
    {
        var image = (texture is CanvasTexture { DiffuseTexture: { } painted } ? painted : texture).GetImage();
        if (image.IsCompressed()) image.Decompress();
        return image.GetPixel(0, 0);
    }
}
