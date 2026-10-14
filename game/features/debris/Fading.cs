using System;

namespace Stillwake.Game.Features.Debris;

/// <summary>How a piece of debris fades (features/debris).</summary>
public static class Fading
{
    /// <summary>How far a piece fades in after it spawns and out before it is gone, in thousandths of a cell.</summary>
    private const double Fade = 8000;

    /// <summary>How visible a piece at <paramref name="x"/> is: out of the dark over the start of its path, back into it at the end.</summary>
    public static double FadeOf(double x, int start, int end)
    {
        double into = Math.Min(1, (start - x) / Fade);
        double away = Math.Min(1, (x - end) / Fade);
        return Math.Max(0, Math.Min(into, away));
    }
}
