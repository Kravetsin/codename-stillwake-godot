namespace Stillwake.Game;

/// <summary>
/// The daylight of the moment on a planet (2026-11-07-night-chills), drawn only: the sim keeps the hour (<c>ISkyQueries.Now</c>),
/// this turns it into colours that multiply what the sky lights. By day 1; over the <see cref="Dusk"/> ticks before the night
/// line it fades through the look's dusk to its night, so the light warns before the chill bites; over the <see cref="Dawn"/>
/// ticks before night ends it fades back through dusk to day, the sky brightening while the chill still bites.
/// </summary>
public static class Daylight
{
    /// <summary>The ticks dusk and dawn each take: a minute.</summary>
    public const int Dusk = 1200, Dawn = 1200;

    private static readonly (float R, float G, float B) Whole = (1f, 1f, 1f);

    /// <summary>What the light through the open sky is multiplied by at <paramref name="time"/> of <paramref name="cycle"/>: the starlight's <c>sky</c> uniform.</summary>
    public static (float R, float G, float B) Sky(PlanetLooks.Look look, int time, Stillwake.Content.Sky.Cycle cycle) =>
        At(Whole, look.DuskSky, look.NightSky, time, cycle);

    /// <summary>The one colour every backdrop layer behind the ground, and the fog at its sides, is multiplied by at <paramref name="time"/>.</summary>
    public static (float R, float G, float B) Backdrop(PlanetLooks.Look look, int time, Stillwake.Content.Sky.Cycle cycle) =>
        At(Whole, look.DuskBackdrop, look.NightBackdrop, time, cycle);

    private static (float R, float G, float B) At(
        (float R, float G, float B) day, (float R, float G, float B) dusk, (float R, float G, float B) night, int time, Stillwake.Content.Sky.Cycle cycle)
    {
        int length = cycle.Length, line = length - cycle.Night;
        time = (time % length + length) % length;
        if (time < line - Dusk) return day;
        // Dusk: day to dusk over its first half, dusk to night over its second; dawn the same way back.
        if (time < line) return Through(day, dusk, night, (time - (line - Dusk)) / (float)Dusk);
        if (time < length - Dawn) return night;
        return Through(night, dusk, day, (time - (length - Dawn)) / (float)Dawn);
    }

    private static (float R, float G, float B) Through((float R, float G, float B) from, (float R, float G, float B) mid, (float R, float G, float B) to, float t) =>
        t < 0.5f ? Mix(from, mid, t * 2) : Mix(mid, to, t * 2 - 1);

    private static (float R, float G, float B) Mix((float R, float G, float B) a, (float R, float G, float B) b, float t) =>
        (a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
}
