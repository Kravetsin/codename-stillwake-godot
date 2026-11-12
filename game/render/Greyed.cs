using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A picture drained of its colour to a cool slate (canister-mark): what cannot be had now, a canister cooling. Made once a
/// picture, the first time it is asked for.
/// </summary>
public static class Greyed
{
    private static readonly Dictionary<Texture2D, Texture2D> Made = [];

    /// <summary><paramref name="picture"/> greyed: its brightness kept, darker and a little blue.</summary>
    public static Texture2D Of(Texture2D picture)
    {
        if (Made.TryGetValue(picture, out var made)) return made;
        var image = picture.GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
            {
                var c = image.GetPixel(x, y);
                float v = 40 / 255f + (0.3f * c.R + 0.59f * c.G + 0.11f * c.B) * 0.55f;
                image.SetPixel(x, y, new Color(v * 0.92f, v * 0.97f, Mathf.Min(1, v * 1.08f), c.A));
            }
        return Made[picture] = ImageTexture.CreateFromImage(image);
    }
}
