using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where a piece of light reads its columns from the long strip (<c>LightStrip</c>, hud-vitals): its start as baked, then the strip's
/// own end. A piece longer than the strip repeats the strip's body between them, so it is drawn whole however long it grows: the air's
/// bar runs to the edge's keys, past the strip's length on a wide screen. The air bar's shader reads its columns by the same rule.
/// </summary>
public static class StripCuts
{
    /// <summary>The strip's length and the baked margin round it, in block units; how much of the strip's end a piece keeps.</summary>
    public const float Long = 304, Margin = 2, Cap = 8;

    /// <summary>
    /// The strip's column, counted from its light's start (<see cref="Margin"/> into the picture), that the point
    /// <paramref name="along"/> a piece <paramref name="length"/> long reads; from −<see cref="Margin"/> to its length + <see cref="Margin"/>.
    /// </summary>
    public static float Column(float along, float length)
    {
        if (length <= Long)
        {
            float cut = Math.Max(0, length - Math.Min(Cap, length / 2));
            return along < cut ? along : along + Long - length;
        }
        // The start as baked; the body again and again; the strip's own end.
        if (along < Long - Cap) return along;
        if (along >= length - Cap) return along + Long - length;
        float body = Long - 2 * Cap, into = along - (Long - Cap);
        return Cap + (into - body * MathF.Floor(into / body));
    }
}
