using System;

namespace Stillwake.Game.Features.Debris;

/// <summary>How a piece of debris fades (features/debris), and whether one gone was broken rather than faded out.</summary>
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

    /// <summary>
    /// Whether a piece gone, last seen at <paramref name="x"/> <paramref name="ticks"/> ticks before, was broken
    /// (0034): it had hits in it and would still have been on its path; one that drifted past its end only faded.
    /// </summary>
    public static bool Broken(int damage, int x, int speed, int end, int ticks) =>
        damage > 0 && x - (long)speed * ticks >= end;
}
