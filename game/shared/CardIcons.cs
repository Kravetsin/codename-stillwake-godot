using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Shared;

/// <summary>A tool card's signs as textures (<see cref="CardSigns"/>), drawn from their SVG once at the size shown, kept after.</summary>
public static class CardIcons
{
    private static readonly Dictionary<(CardSign, int, bool), Texture2D> Made = new();

    /// <summary><paramref name="sign"/> at <paramref name="size"/> px, struck through where <paramref name="off"/>.</summary>
    public static Texture2D Of(CardSign sign, int size, bool off = false)
    {
        if (Made.TryGetValue((sign, size, off), out var made)) return made;
        var image = new Image();
        image.LoadSvgFromString(CardSigns.Svg(sign, size, off));
        return Made[(sign, size, off)] = ImageTexture.CreateFromImage(image);
    }
}
