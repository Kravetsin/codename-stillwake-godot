using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// A drone's form as a picture (drone-lessons): its thread icon's lines drawn in ember on a clear square, made once a size, so the board, its finds
/// and the scan over the head show a form where they show an item's picture.
/// </summary>
public static class FormPictures
{
    private static readonly Dictionary<(string, int), Texture2D> Made = [];

    /// <summary>The picture of <paramref name="form"/>, <paramref name="side"/> px; null for what is no form.</summary>
    public static Texture2D? Of(string form, int side)
    {
        if (FormIcons.Of(form) is not { } icon) return null;
        side = Math.Max(8, side);
        if (Made.TryGetValue((form, side), out var made)) return made;
        var image = Image.CreateEmpty(side, side, false, Image.Format.Rgba8);
        var ember = new Color(DroneInk.Ember.R / 255f, DroneInk.Ember.G / 255f, DroneInk.Ember.B / 255f);
        float half = side / 2f, r = half * 0.86f, width = Math.Max(1.2f, side / 22f);
        foreach (var step in icon.Parts.Steps)
        {
            float ax = half + step.Ax * r, ay = half + step.Ay * r, bx = half + step.Bx * r, by = half + step.By * r;
            int n = Math.Max(1, (int)MathF.Ceiling(MathF.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay)) * 2));
            for (int i = 0; i <= n; i++) Dot(image, ax + (bx - ax) * i / n, ay + (by - ay) * i / n, width, ember);
        }
        return Made[(form, side)] = ImageTexture.CreateFromImage(image);
    }

    /// <summary>A soft round dot <paramref name="width"/> px across at (<paramref name="x"/>, <paramref name="y"/>), kept at the most alpha it is given.</summary>
    private static void Dot(Image image, float x, float y, float width, Color colour)
    {
        float rad = width / 2;
        int x0 = Math.Max(0, (int)(x - rad - 1)), x1 = Math.Min(image.GetWidth() - 1, (int)(x + rad + 1));
        int y0 = Math.Max(0, (int)(y - rad - 1)), y1 = Math.Min(image.GetHeight() - 1, (int)(y + rad + 1));
        for (int py = y0; py <= y1; py++)
            for (int px = x0; px <= x1; px++)
            {
                float d = MathF.Sqrt((px + 0.5f - x) * (px + 0.5f - x) + (py + 0.5f - y) * (py + 0.5f - y));
                float a = Math.Clamp(rad + 0.5f - d, 0, 1);
                if (a > image.GetPixel(px, py).A) image.SetPixel(px, py, colour with { A = a });
            }
    }
}
