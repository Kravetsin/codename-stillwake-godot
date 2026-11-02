using System;
using Godot;

namespace Stillwake.Game.Features.Research;

/// <summary>Draws <see cref="BoardInk"/>'s strokes: thin at their ends, thicker where the stylus pressed, gone where it lifted.</summary>
public static class InkDraw
{
    /// <summary>The thinnest a stroke is drawn on the screen: under this it would fall apart into dashes, and on a scaled canvas it is scaled too.</summary>
    public const float Least = 0.9f;

    /// <summary>A stroke, <paramref name="drawn"/> of the way along; returns where its tip is.</summary>
    public static Vector2 Stroke(CanvasItem on, InkPoint[] stroke, Color colour, float width, float least = Least, float drawn = 1)
    {
        int last = stroke.Length - 1;
        float upto = drawn * last;
        var tip = new Vector2(stroke[0].X, stroke[0].Y);
        for (int i = 0; i < last && i < upto; i++)
        {
            InkPoint a = stroke[i], b = stroke[i + 1];
            float part = MathF.Min(1, upto - i);
            var to = new Vector2(Mathf.Lerp(a.X, b.X, part), Mathf.Lerp(a.Y, b.Y, part));
            float ends = MathF.Min(1, MathF.Min((i + 0.4f) / 2.4f, (last - i + 0.4f) / 2.4f));
            if (!a.Lift) on.DrawLine(new Vector2(a.X, a.Y), to, colour, MathF.Max(least, width * a.Thick * ends), true);
            tip = to;
        }
        return tip;
    }

    public static void Strokes(CanvasItem on, InkPoint[][] strokes, Color colour, float width, float least = Least)
    {
        foreach (var stroke in strokes) Stroke(on, stroke, colour, width, least);
    }

    /// <summary>The stylus at a stroke's tip, and the two specks it flicks up; <paramref name="scale"/> undoes what the canvas is scaled by.</summary>
    public static void Nib(CanvasItem on, Vector2 at, Color colour, float scale)
    {
        on.DrawRect(new Rect2(at + new Vector2(-1, -1) * scale, new Vector2(2.4f, 2.4f) * scale), colour);
        on.DrawRect(new Rect2(at + new Vector2(2, -4) * scale, new Vector2(1.4f, 1.4f) * scale), colour);
        on.DrawRect(new Rect2(at + new Vector2(4, -7) * scale, new Vector2(1.2f, 1.2f) * scale), colour);
    }
}
