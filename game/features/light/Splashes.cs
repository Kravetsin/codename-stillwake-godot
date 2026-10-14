using Godot;
using Stillwake.Game.Render;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// Fine light in the cells mode (0026): a soft spot round each glowing particle, drawn a texel a pixel of
/// the world into a buffer over the screen that <see cref="Lit"/> adds pixel by pixel, so an ember passing
/// a wall leaves a small glowing spot that moves with it where a cell's light would spread it over the
/// cell. Its strength and colour are its kind's in <see cref="L.Emits"/>, as it fades; it reaches
/// <see cref="L.SplashPerSize"/> pixels per pixel of its size, and nothing shadows it.
/// </summary>
public partial class Splashes : SubViewport
{
    private readonly Node2D _spots = new()
    {
        TextureFilter = CanvasItem.TextureFilterEnum.Linear,
        Material = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
        },
    };

    private ViewportTexture? _texture;

    public Splashes()
    {
        TransparentBg = true;
        Disable3D = true;
        RenderTargetUpdateMode = UpdateMode.Disabled;
        AddChild(_spots);
        _spots.Draw += DrawSpots;
    }

    /// <summary>
    /// Draws the spots over <paramref name="view"/>, the world's stretch on screen, in pixels of the world,
    /// and hands them to <see cref="Lit"/>; each frame in the cells mode.
    /// </summary>
    public void Show(Rect2 view)
    {
        // Whole pixels of the world, so a texel lies on a pixel of the art.
        var corner = view.Position.Floor();
        var size = new Vector2I((int)Mathf.Ceil(view.End.X - corner.X) + 1, (int)Mathf.Ceil(view.End.Y - corner.Y) + 1);
        if (Size != size) Size = size;
        CanvasTransform = new Transform2D(0, -corner);
        RenderTargetUpdateMode = UpdateMode.Always;
        _spots.QueueRedraw();
        Lit.ShowSplashes(_texture ??= GetTexture(), new Rect2(corner, size));
    }

    /// <summary>Stops drawing the spots: the godot mode lights the particles itself.</summary>
    public void Stop() => RenderTargetUpdateMode = UpdateMode.Disabled;

    private void DrawSpots()
    {
        var live = Particles.Shared.Live;
        for (int i = 0; i < live.Count; i++)
        {
            var p = live[i];
            if (p.Effect.Glow is null) continue;
            var emits = p.Effect == Effect.Ember ? L.Emits.Ember : L.Emits.Spark;
            float s = emits.Strength * (float)p.Alpha, r = L.SplashPerSize * (float)p.Size;
            _spots.DrawTextureRect(Spot, new Rect2((float)p.X - r, (float)p.Y - r, r * 2, r * 2), false,
                new Color(emits.Colour.R * s, emits.Colour.G * s, emits.Colour.B * s));
        }
    }

    private static ImageTexture? _spot;

    /// <summary>A spot's light, whole in its middle, falling off smoothly to nothing at its edge.</summary>
    private static ImageTexture Spot => _spot ??= MakeSpot();

    private static ImageTexture MakeSpot()
    {
        const int size = 32;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Min(1, new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).Length() / (size / 2f));
                float falls = (1 - d) * (1 - d);
                image.SetPixel(x, y, new Color(1, 1, 1, falls));
            }
        return ImageTexture.CreateFromImage(image);
    }
}
