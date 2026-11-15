using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// How the frost and the haze on the screen follow what is felt (2026-11-14-frost-film): read four times a second, so what is read
/// jumps; what is shown chases it, closing 1 − 1/e of the gap every <see cref="EaseS"/>, the same at any frame rate, and lands on it
/// once within a hair, so a layer hides at 0.
/// </summary>
public static class ScreenEase
{
    /// <summary>The time in which the shown strength closes 1 − 1/e (about two thirds) of its gap to what was read.</summary>
    public const double EaseS = 0.6;
    /// <summary>The gap under which the shown strength lands on what was read: by then <see cref="Fade"/> has it all but gone.</summary>
    public const double Snap = 0.01;
    /// <summary>The strength under which a layer fades out whole rather than only shrinking toward the screen's edge.</summary>
    public const double FadeBelow = 0.25;

    /// <summary>The strength shown <paramref name="delta"/> seconds after <paramref name="shown"/>, chasing <paramref name="target"/>.</summary>
    public static double Toward(double shown, double target, double delta)
    {
        double next = shown + (target - shown) * (1 - Math.Exp(-delta / EaseS));
        return Math.Abs(target - next) < Snap ? target : next;
    }

    /// <summary>
    /// How much of a layer shows at <paramref name="shown"/>, 0 to 1: whole from <see cref="FadeBelow"/>, eased to nothing under it,
    /// so a thawing frost or a cooling haze leaves no thin band at the edges with a hard line while the strength runs out.
    /// </summary>
    public static float Fade(double shown)
    {
        double t = Math.Clamp(shown / FadeBelow, 0, 1);
        return (float)(t * t * (3 - 2 * t));
    }
}
