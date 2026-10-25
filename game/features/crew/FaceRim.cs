using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A tool's aim (the fan of rays) as a field of light (<see cref="AimGlow"/>): one small picture of it a frame, laid over the
/// world, light added and unlit, so it reads in the dark, breathing with the aim's outline (<see cref="Breath"/>). Drawn
/// pixels, no light of its own.
/// </summary>
public partial class FaceRim : Sprite2D
{
    /// <summary>How bright the glow is at its brightest, over its colour.</summary>
    private const float Strength = 1.6f;

    private readonly ImageTexture _texture = new();
    private Image? _image;

    public FaceRim()
    {
        ZIndex = Layers.Dug;
        Centered = false;
        TextureFilter = TextureFilterEnum.Nearest;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add, LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        Visible = false;
    }

    /// <summary>Shows <paramref name="field"/> in <paramref name="colour"/> this frame, or nothing with none.</summary>
    public void Show(GlowField? field, Color colour)
    {
        Visible = field != null;
        if (field == null) return;
        bool resized = _image == null || _image.GetWidth() != field.Width || _image.GetHeight() != field.Height;
        if (resized) _image = Image.CreateEmpty(field.Width, field.Height, false, Image.Format.Rgba8);
        float breath = (float)Breath.Alpha(Time.GetTicksMsec());
        for (int y = 0; y < field.Height; y++)
            for (int x = 0; x < field.Width; x++)
                _image!.SetPixel(x, y, colour with { A = Mathf.Min(1, field.Light[y * field.Width + x] * breath * Strength) });
        if (resized) _texture.SetImage(_image);
        else _texture.Update(_image);
        Texture = _texture;
        Position = new Vector2(field.Left, field.Top);
    }
}
