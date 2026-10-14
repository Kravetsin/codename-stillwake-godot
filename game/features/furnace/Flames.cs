namespace Stillwake.Game.Features.Furnace;

/// <summary>
/// How brightly a fire burns while it shows one picture of its flames, as a share of its full light
/// (<c>Light.Fire</c>): drawn afresh with each picture and held till the next, so its light jumps with
/// the flames rather than fading; now and then a coal cracks and it flares past whole.
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
    /// <summary>Embers a crack throws up at once.</summary>
    public const int CrackEmbers = 3;

    /// <summary>The brightness of a picture for <paramref name="roll"/>, drawn from 0 to 1.</summary>
    public static float Blaze(double roll) =>
        roll < CrackChance ? Crack : Low + (1 - Low) * (float)((roll - CrackChance) / (1 - CrackChance));

    /// <summary>Whether a picture at <paramref name="blaze"/> is a coal cracking.</summary>
    public static bool Cracks(float blaze) => blaze > 1;
}
