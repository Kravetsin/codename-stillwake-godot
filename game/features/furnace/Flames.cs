using System;

namespace Stillwake.Game.Features.Furnace;

/// <summary>
/// How brightly a fire burns while it shows one picture of its flames, as a share of its full light
/// (<c>Light.Fire</c>): drawn afresh with each picture, which its glow and light then breathe toward
/// (<see cref="Breathe"/>) rather than jump to (0075); now and then a coal cracks and it flares past whole.
/// </summary>
public static class Flames
{
    /// <summary>How long a fire shows one picture before the next, in ms (FLICKER_MS).</summary>
    public const double StepMs = 110;
    /// <summary>The dimmest a fire burns between cracks.</summary>
    public const float Low = 0.55f;
    /// <summary>The share of pictures a coal cracks in, and how bright the fire flares then.</summary>
    public const double CrackChance = 0.1;
    public const float Crack = 1.5f;
    /// <summary>The share of the way to a picture's brightness a fire's glow goes each 60 Hz frame.</summary>
    public const double BreathPerFrame = 0.18;

    /// <summary>The brightness of a picture for <paramref name="roll"/>, drawn from 0 to 1.</summary>
    public static float Blaze(double roll) =>
        roll < CrackChance ? Crack : Low + (1 - Low) * (float)((roll - CrackChance) / (1 - CrackChance));

    /// <summary>Whether a picture at <paramref name="blaze"/> is a coal cracking.</summary>
    public static bool Cracks(float blaze) => blaze > 1;

    /// <summary>
    /// A fire's glow <paramref name="elapsedMs"/> on from <paramref name="glow"/>, eased toward the picture's
    /// <paramref name="blaze"/>: <see cref="BreathPerFrame"/> of the way each 60 Hz frame, alike at any frame rate.
    /// </summary>
    public static float Breathe(float glow, float blaze, double elapsedMs) =>
        blaze + (glow - blaze) * (float)Math.Pow(1 - BreathPerFrame, elapsedMs * 60 / 1000);
}
