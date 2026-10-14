using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// Space behind the ship (the stars of features/debris in TypeScript): a tile of stars over the whole
/// screen, a screen pixel a star whatever the zoom, moving a little with the camera so they read as far
/// away and streaming slowly by as the ship moves. Unlit, on a canvas layer of its own behind the world.
/// </summary>
public partial class Stars : CanvasLayer
{
    /// <summary>The star tile, in screen pixels, and how many stars it holds.</summary>
    private const int Tile = 512, Count = 420;
    /// <summary>How fast the stars stream by, in screen pixels a second.</summary>
    private const float Speed = 8;
    /// <summary>How much of the camera's movement the stars follow.</summary>
    private const float Parallax = 0.08f;

    private readonly Sprite2D _sky;
    private float _stream;

    public Stars()
    {
        Layer = -1;
        var rng = new RandomNumberGenerator { Seed = 7 };
        var image = Image.CreateEmpty(Tile, Tile, false, Image.Format.Rgba8);
        for (int i = 0; i < Count; i++)
        {
            int size = rng.Randf() < 0.15f ? 2 : 1;
            var colour = new Color(223 / 255f, 232 / 255f, 245 / 255f, 0.2f + rng.Randf() * 0.55f);
            int x = rng.RandiRange(0, Tile - size), y = rng.RandiRange(0, Tile - size);
            image.FillRect(new Rect2I(x, y, size, size), colour);
        }
        _sky = new Sprite2D
        {
            Texture = ImageTexture.CreateFromImage(image),
            Centered = false,
            RegionEnabled = true,
            TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
        };
        AddChild(_sky);
    }

    public override void _Process(double delta)
    {
        _stream = (_stream + (float)delta * Speed) % Tile;
        var viewport = GetViewport();
        var screen = viewport.GetVisibleRect().Size;
        var camera = viewport.GetCamera2D();
        var seen = camera == null ? Vector2.Zero : camera.GetScreenCenterPosition() * camera.Zoom.X;
        _sky.RegionRect = new Rect2(seen * Parallax + new Vector2(_stream, 0), screen);
    }
}
