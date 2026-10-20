using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// Space behind the ship (the stars of features/debris in TypeScript): a tile of stars over the whole
/// screen, a screen pixel a star whatever the zoom, moving a little with the camera so they read as far
/// away and streaming slowly by as the ship moves. Unlit, on a canvas layer of its own behind the world.
/// They glide a fraction of a pixel a frame: the tile is filtered, as Pixi filtered the TypeScript tile,
/// and read where the camera is this frame, once it has followed the hero. Over them, nearer, the far
/// wrecks and the dust (<see cref="FarWrecks"/>, <see cref="Dust"/>), still under the world.
/// </summary>
public partial class Stars : CanvasLayer
{
    /// <summary>The star tile, in screen pixels, and how many stars it holds.</summary>
    private const int Tile = 512, Count = 420;
    /// <summary>How fast the stars stream by, in screen pixels a second.</summary>
    private const float Speed = 8;
    /// <summary>How much of the camera's movement the stars follow.</summary>
    private const float Parallax = 0.08f;
    private static readonly Color Star = new(223 / 255f, 232 / 255f, 245 / 255f);

    private readonly Sprite2D _sky;
    /// <summary>A planet's sky behind its ground, drawn over the screen in place of space (0076, 0078).</summary>
    private readonly PlanetSky _day = new() { Visible = false };
    private readonly FarWrecks _wrecks = new();
    private readonly Dust _dust = new();
    private float _stream;

    public Stars()
    {
        Layer = ScreenLayers.Stars;
        // After every node at the default priority, the camera among them: its centre is this frame's.
        ProcessPriority = 1;
        var rng = new RandomNumberGenerator { Seed = 7 };
        var image = Image.CreateEmpty(Tile, Tile, false, Image.Format.Rgba8);
        // Clear in the stars' own colour: filtered toward black, a star between two pixels would dim twice over.
        image.Fill(new Color(Star, 0));
        for (int i = 0; i < Count; i++)
        {
            int size = rng.Randf() < 0.15f ? 2 : 1;
            var colour = new Color(Star, 0.2f + rng.Randf() * 0.55f);
            int x = rng.RandiRange(0, Tile - size), y = rng.RandiRange(0, Tile - size);
            image.FillRect(new Rect2I(x, y, size, size), colour);
        }
        _sky = new Sprite2D
        {
            Texture = ImageTexture.CreateFromImage(image),
            Centered = false,
            RegionEnabled = true,
            TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
            // The project draws nearest: a star would hold still and jump a whole pixel every few frames.
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
        };
        AddChild(_day);
        AddChild(_sky);
        AddChild(_wrecks);
        AddChild(_dust);
    }

    /// <summary>
    /// A planet's sky behind everything (<see cref="PlanetSky"/>), its land's ground line at this row of the world, in pixels:
    /// space and what drifts in it hidden; null for space, as aboard (0076).
    /// </summary>
    public float? Ground
    {
        get => _ground;
        set
        {
            _ground = value;
            _day.Visible = value != null;
            _sky.Visible = _wrecks.Visible = _dust.Visible = value == null;
        }
    }

    private float? _ground;

    /// <summary>The world's time, in seconds of ticks, that the far wrecks and the dust have drifted by: every peer's alike.</summary>
    public double Seconds { get; set; }

    public override void _Process(double delta)
    {
        _stream = (_stream + (float)delta * Speed) % Tile;
        var viewport = GetViewport();
        var screen = viewport.GetVisibleRect().Size;
        var camera = viewport.GetCamera2D();
        var centre = camera?.GetScreenCenterPosition() ?? Vector2.Zero;
        float zoom = camera?.Zoom.X ?? 1;
        _sky.RegionRect = new Rect2(centre * zoom * Parallax + new Vector2(_stream, 0), screen);
        if (_ground is float ground) _day.Follow(centre, zoom, screen, ground, Seconds);
        _wrecks.Follow(centre, zoom, screen, Seconds);
        _dust.Follow(centre, zoom, screen, Seconds);
    }
}
