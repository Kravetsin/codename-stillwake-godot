using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A number under a vitals dial (hud-vitals, the pick «dials»): the game's mono, centred under the dial in the gap between the rows,
/// inked round under its colour; groups drawn apart as by a thin space. Drawn at the block's scale in whole screen pixels, so the
/// digits stay sharp at every interface size.
/// </summary>
public static class DialNumber
{
    private static readonly Color Ink = new("0c0e14");

    /// <summary>
    /// Draws <paramref name="parts"/> centred on <paramref name="at"/> (a dial's column, the line <see cref="Vitals.NumberY"/>), in
    /// block units, on a layer drawn <paramref name="scale"/> times its size; then puts back no transform.
    /// </summary>
    public static void Draw(CanvasItem on, IReadOnlyList<string> parts, Vector2 at, float scale, Color colour)
    {
        if (colour.A <= 0.004f || parts.Count == 0) return;
        var font = Style.Mono;
        int size = Math.Max(1, (int)MathF.Round(Vitals.NumberSize * scale));
        float gap = size * 0.25f, wide = gap * (parts.Count - 1);
        foreach (string part in parts) wide += font.GetStringSize(part, HorizontalAlignment.Left, -1, size).X;
        on.DrawSetTransform(at, 0, Vector2.One / scale);
        float x = MathF.Round(-wide / 2), baseline = MathF.Round(size * Style.MonoMiddle);
        int outline = Math.Max(1, (int)MathF.Round(size * 0.3f));
        foreach (string part in parts)
        {
            on.DrawStringOutline(font, new Vector2(x, baseline), part, HorizontalAlignment.Left, -1, size, outline, Ink with { A = 0.8f * colour.A });
            on.DrawString(font, new Vector2(x, baseline), part, HorizontalAlignment.Left, -1, size, colour with { A = 0.96f * colour.A });
            x += MathF.Round(font.GetStringSize(part, HorizontalAlignment.Left, -1, size).X + gap);
        }
        on.DrawSetTransform(Vector2.Zero);
    }
}
