using System;
using System.Collections.Generic;
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
/// <param name="Target">Where what it feels draws its body, in hundredths of a degree: past a death the body never stops short of it.</param>
/// <param name="Gasp">The ticks left of its body's last breath past a death (<see cref="BodyHeat.GaspLeft"/>); 0 while none runs.</param>
public readonly record struct ThermometerReading(int Degrees, int Low, int High, int Felt, int Heat, int Body = Content.Temperature.Normal,
    int Target = Content.Temperature.Normal, int Gasp = 0);

/// <summary>
/// What the vitals' thermometer says (the-thermometer, body-temperature): the hero's body to a tenth, calm at its normal,
/// frost-blue below it and ember above (2026-11-14-heat-limits), deeper toward the death either way; on hover the body, the degrees
/// where it stands and both limits, the one crossed in red.
/// </summary>
public static class Thermometers
{
    /// <summary>
    /// What the thermometer reads for hero <paramref name="peer"/> at <paramref name="cell"/>: its <paramref name="body"/> in degrees
    /// and the ticks left of its last breath, <paramref name="gasp"/>, as its client keeps them.
    /// </summary>
    public static ThermometerReading Read(ICrewQueries crew, int peer, int cell, float body, int gasp = 0) =>
        new(crew.TemperatureAt(cell), crew.LowOf(peer), crew.HighOf(peer), crew.Felt(peer, cell), crew.Heat(peer, cell), (int)MathF.Round(body * 100),
            (int)MathF.Round(BodyHeat.Target(crew.Felt(peer, cell), crew.Heat(peer, cell)) * 100), gasp);

    /// <summary>How far the body's last breath has gone, 0 at its start to 1 at its end, from its <paramref name="gasp"/> ticks left; −1 while none runs.</summary>
    public static float Breath(int gasp) => gasp > 0 ? Math.Clamp(1 - gasp / (float)Content.Crew.LastBreath, 0, 1) : -1;

    /// <summary>
    /// The heart's beat the mercury pulses red with through the last breath, 0 to about 1 at <paramref name="clockS"/>: a beat and its
    /// echo, slowing as the cold takes the body, racing as the heat does, faster still toward the end.
    /// </summary>
    public static float Beat(double clockS, bool cold, float breath)
    {
        double per = 60.0 / (cold ? 44 - 20 * breath : 140 + 30 * breath), p = (clockS % per + per) % per;
        return (float)(Math.Exp(-Math.Pow((p - 0.04) / 0.07, 2)) + 0.55 * Math.Exp(-Math.Pow((p - 0.3) / 0.08, 2)));
    }

    /// <summary>
    /// How deep the reading's frost is at a <paramref name="body"/> of so many degrees, 0 to 1: as the screen's frost is
    /// (<see cref="Report.ColdOf"/>).
    /// </summary>
    public static float Frost(float body) => Report.ColdOf(body);

    /// <summary>How deep the reading's heat is at a <paramref name="body"/> of so many degrees, 0 to 1: as the screen's haze is (<see cref="Report.HeatOf"/>).</summary>
    public static float Hot(float body) => Report.HeatOf(body);

    /// <summary>The seconds in which the shown body closes about two thirds of its way to the sim's (<see cref="Glide"/>): a breath's step slides.</summary>
    public const double GlideS = 0.25;

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

    /// <summary>The scale's ends, in degrees: the cold's death and the heat's.</summary>
    public const float Coldest = Content.Temperature.ColdDeath / 100f, Hottest = Content.Temperature.HeatDeath / 100f;

    /// <summary>Whether <paramref name="degrees"/> lie at or past either death: a target there, which the body reaches and never stops at.</summary>
    public static bool Past(float degrees) => degrees <= Coldest || degrees >= Hottest;

    /// <summary>
    /// Where <paramref name="degrees"/> stand round the temperature dial's ring, in screen radians (y down, the top at −π/2): the
    /// normal at the top, the heat clockwise and the cold counter-clockwise, each linear to its death at the bottom, where a
    /// target past either is pinned.
    /// </summary>
    public static float RingAngle(float degrees)
    {
        const float top = -MathF.PI / 2;
        return degrees >= Normal
            ? top + MathF.PI * Math.Min(1, (degrees - Normal) / (Hottest - Normal))
            : top - MathF.PI * Math.Min(1, (Normal - degrees) / (Normal - Coldest));
    }

    /// <summary>The ring's notches, a whole degree each from death to death but the normal, which has a pointer; long at each fifth.</summary>
    public static IEnumerable<(float Angle, bool Long)> RingNotches()
    {
        int normal = (int)MathF.Round(Normal);
        for (int d = (int)MathF.Ceiling(Coldest); d <= (int)Hottest; d++)
            if (d != normal) yield return (RingAngle(d), d % 5 == 0);
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
