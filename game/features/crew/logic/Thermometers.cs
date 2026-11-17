using System;
using System.Globalization;
using Stillwake.Game.Shared;
using Stillwake.Sim.Crew;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Crew;

/// <summary>What the thermometer reads: the hero's body, the degrees where it stands, its limits both ways, and what it feels past either.</summary>
/// <param name="Degrees">The degrees at the hero's body.</param>
/// <param name="Low">The coldest it withstands.</param>
/// <param name="High">The hottest it withstands (2026-11-14-heat-limits).</param>
/// <param name="Felt">The degrees it feels past <paramref name="Low"/>.</param>
/// <param name="Heat">The degrees it feels past <paramref name="High"/>.</param>
/// <param name="Body">Its body's temperature, in hundredths of a degree (body-temperature).</param>
public readonly record struct ThermometerReading(int Degrees, int Low, int High, int Felt, int Heat, int Body = Content.Temperature.Normal);

/// <summary>
/// What the vitals' thermometer says (the-thermometer, body-temperature): the hero's body to a tenth, calm at its normal,
/// frost-blue below it and ember above (2026-11-14-heat-limits), deeper toward the death either way; on hover the body, the degrees
/// where it stands and both limits, the one crossed in red.
/// </summary>
public static class Thermometers
{
    /// <summary>What the thermometer reads for hero <paramref name="peer"/> at <paramref name="cell"/>, its body's.</summary>
    public static ThermometerReading Read(ICrewQueries crew, int peer, int cell) =>
        new(crew.TemperatureAt(cell), crew.LowOf(peer), crew.HighOf(peer), crew.Felt(peer, cell), crew.Heat(peer, cell), crew.BodyOf(peer));

    /// <summary>
    /// How deep the reading's frost is at a <paramref name="body"/> of so many degrees, 0 to 1: as the screen's frost is
    /// (<see cref="Report.ColdOf"/>).
    /// </summary>
    public static float Frost(float body) => Report.ColdOf(body);

    /// <summary>How deep the reading's heat is at a <paramref name="body"/> of so many degrees, 0 to 1: as the screen's haze is (<see cref="Report.HeatOf"/>).</summary>
    public static float Hot(float body) => Report.HeatOf(body);

    /// <summary>The degrees over its normal across which the body's frost star gives way to the heat's flame (2026-11-14-heat-limits).</summary>
    public const float SwapDegrees = 1.5f;

    /// <summary>How far the star has given way to the flame, 0 to 1, at a <paramref name="body"/> of so many degrees.</summary>
    public static float Swap(float body) => Math.Clamp((body - Normal) / SwapDegrees, 0, 1);

    /// <summary>The seconds in which the shown body closes about two thirds of its way to the sim's (<see cref="Glide"/>): a breath's step slides.</summary>
    public const double GlideS = 1.0;

    /// <summary>How near the sim's body, in degrees, the shown one settles on it.</summary>
    public const float Snap = 0.05f;

    /// <summary>A well body, in degrees.</summary>
    public const float Normal = Content.Temperature.Normal / 100f;

    /// <summary>
    /// The body shown <paramref name="delta"/> seconds on from <paramref name="shown"/>: it glides to the sim's
    /// <paramref name="body"/>, in degrees, eased out, so its once-a-breath steps slide, and settles on it within <see cref="Snap"/>.
    /// </summary>
    public static float Glide(float shown, float body, double delta)
    {
        float left = body - shown;
        return Math.Abs(left) < Snap ? body : shown + left * (float)(1 - Math.Exp(-delta / GlideS));
    }

    /// <summary>A glided body as it is drawn: the nearest tenth of a degree, in tenths.</summary>
    public static int Shown(float shown) => (int)MathF.Round(shown * 10, MidpointRounding.AwayFromZero);

    /// <summary>The body as it is drawn from its <paramref name="tenths"/> of a degree: 37.0°.</summary>
    public static string Body(int tenths) => (tenths / 10f).ToString("0.0", CultureInfo.InvariantCulture) + "°";

    /// <summary>A place's degrees as they are written: a true minus below zero, a plus above it, the degree sign: −50°, 0°, +20°.</summary>
    public static string Reading(int degrees) => ItemCards.Signed(degrees) + "°";

    /// <summary>The hover: the body, the degrees where it stands, and the coldest and the hottest it withstands, each red while it is crossed.</summary>
    public static Tip Tip(ThermometerReading read) =>
        new(Body(Shown(read.Body / 100f)),
            new TipLine(T("crew.here", ("n", Reading(read.Degrees))), Tone.Dim),
            new TipLine(T("crew.withstands", ("n", ItemCards.Degrees(read.Low))), read.Felt > 0 ? Tone.Bad : Tone.Dim),
            new TipLine(T("crew.withstandsHeat", ("n", ItemCards.Signed(read.High))), read.Heat > 0 ? Tone.Bad : Tone.Dim));
}
