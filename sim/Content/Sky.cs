namespace Stillwake.Content;

/// <summary>
/// A planet's day and night (2026-11-07-night-chills): its hour counted in ticks, its night the last stretch of each day, and
/// the chill night lays on a hero under the open sky. What dusk and night look like is the client's (<c>Daylight</c>).
/// </summary>
public static class Sky
{
    /// <summary>
    /// A day: <paramref name="Length"/> ticks, from the start of daylight at 0; its last <paramref name="Night"/> ticks are night.
    /// At night a hero whose cell has no back wall pays <paramref name="Chill"/> units of air more once every
    /// <paramref name="Every"/> breaths (<see cref="Crew.BreathEvery"/>), the cape sparing its share as of all cold.
    /// </summary>
    public sealed record Cycle(int Length, int Night, int Chill, int Every);

    /// <summary>The first planet's: a day of 20 minutes, 6 of them night; at night outside a unit more every 5 s, twice thin air's cost.</summary>
    public static readonly Cycle First = new(Length: 24_000, Night: 7_200, Chill: 1, Every: 5);
}
