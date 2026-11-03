using System;
using Godot;

namespace Stillwake.Game.Shared;

/// <summary>Draws a number as a <see cref="CountFlare"/> heats it (credit-counter): each digit toward the flare's colour, a rise haloed.</summary>
public static class FlareText
{
    /// <summary>What a rise lights toward, and a fall darkens toward.</summary>
    public static readonly Color Rise = new(1f, 246 / 255f, 222 / 255f), Fall = new(176 / 255f, 62 / 255f, 40 / 255f);

    /// <summary>
    /// Draws <paramref name="text"/> ending at <paramref name="right"/>, a digit at a time from the right, each heated by <paramref name="heatOf"/> of its
    /// column (0 the units) toward <paramref name="hot"/>, haloed while <paramref name="halo"/>. Returns how wide it came out.
    /// </summary>
    public static float DrawRight(CanvasItem on, Font font, int size, string text, float right, float baseline, Color colour, Color hot, Func<int, double> heatOf, bool halo)
    {
        float x = right;
        int column = 0;
        for (int i = text.Length - 1; i >= 0; i--)
        {
            string glyph = text[i].ToString();
            x -= font.GetStringSize(glyph, HorizontalAlignment.Left, -1, size).X;
            float heat = char.IsDigit(text[i]) ? (float)heatOf(column++) : 0;
            Draw(on, font, size, glyph, new Vector2(x, baseline), colour, hot, heat, halo);
        }
        return right - x;
    }

    /// <summary>Draws <paramref name="text"/> at <paramref name="at"/>, all of it heated by <paramref name="heat"/>.</summary>
    public static void Draw(CanvasItem on, Font font, int size, string text, Vector2 at, Color colour, Color hot, float heat, bool halo)
    {
        if (halo && heat > 0.02f)
            on.DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, size, Mathf.RoundToInt(2 + 2 * heat), colour.Lerp(hot, 0.5f) with { A = 0.5f * heat * colour.A });
        on.DrawString(font, at, text, HorizontalAlignment.Left, -1, size, colour.Lerp(hot, heat * (halo ? 0.75f : 0.65f)) with { A = colour.A });
    }
}
