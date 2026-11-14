using System;
using Stillwake.Game.Shared;
using Stillwake.Sim.Crew;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Crew;

/// <summary>What the thermometer reads: the degrees where the hero stands, its limits both ways, and what it feels past either.</summary>
/// <param name="Degrees">The degrees at the hero's body.</param>
/// <param name="Low">The coldest it withstands.</param>
/// <param name="High">The hottest it withstands (2026-11-14-heat-limits).</param>
/// <param name="Felt">The degrees it feels past <paramref name="Low"/>.</param>
/// <param name="Heat">The degrees it feels past <paramref name="High"/>.</param>
public readonly record struct ThermometerReading(int Degrees, int Low, int High, int Felt, int Heat);

/// <summary>
/// What the vitals' thermometer says (the-thermometer): the degrees where the hero stands, calm inside its limits, frost-blue
/// past the cold's and ember past the heat's (2026-11-14-heat-limits), deeper the further; on hover the reading and both limits,
/// the one crossed in red.
/// </summary>
public static class Thermometers
{
    /// <summary>What the thermometer reads for hero <paramref name="peer"/> at <paramref name="cell"/>, its body's.</summary>
    public static ThermometerReading Read(ICrewQueries crew, int peer, int cell) =>
        new(crew.TemperatureAt(cell), crew.LowOf(peer), crew.HighOf(peer), crew.Felt(peer, cell), crew.Heat(peer, cell));

    /// <summary>
    /// How deep the reading's frost or heat is, 0 to 1: the degrees <paramref name="felt"/> past a limit, eased as the screen's frost
    /// and haze are (<see cref="Report.Cold"/>, <see cref="Report.Heat"/>).
    /// </summary>
    public static float Depth(float felt) => Report.Eased(felt / Report.FeltFull);

    /// <summary>The degrees past the high over which the frost star gives way to the heat's flame (2026-11-14-heat-limits).</summary>
    public const float SwapDegrees = 1.5f;

    /// <summary>How far the star has given way to the flame, 0 to 1, at <paramref name="heat"/> degrees past the high.</summary>
    public static float Swap(float heat) => Math.Clamp(heat / SwapDegrees, 0, 1);

    /// <summary>The seconds in which the shown reading closes about two thirds of its way to the sim's (<see cref="Glide"/>).</summary>
    public const double GlideS = 0.3;

    /// <summary>
    /// The reading shown <paramref name="delta"/> seconds on from <paramref name="shown"/>: it glides to the sim's whole
    /// <paramref name="degrees"/>, eased out, so stepping from the ship's +20° into space passes the degrees between (some
    /// 1.4 s for 60°), and settles on them once within half a degree.
    /// </summary>
    public static float Glide(float shown, int degrees, double delta)
    {
        float left = degrees - shown;
        return Math.Abs(left) < 0.5f ? degrees : shown + left * (float)(1 - Math.Exp(-delta / GlideS));
    }

    /// <summary>A glided reading as it is drawn: the nearest whole degree.</summary>
    public static int Shown(float shown) => (int)MathF.Round(shown, MidpointRounding.AwayFromZero);

    /// <summary>The reading as it is drawn: a true minus below zero, a plus above it, the degree sign: −50°, 0°, +20°.</summary>
    public static string Reading(int degrees) => ItemCards.Signed(degrees) + "°";

    /// <summary>The hover: the reading, and the coldest and the hottest the hero withstands, each red while it is crossed.</summary>
    public static Tip Tip(ThermometerReading read) =>
        new(Reading(read.Degrees),
            new TipLine(T("crew.withstands", ("n", ItemCards.Degrees(read.Low))), read.Felt > 0 ? Tone.Bad : Tone.Dim),
            new TipLine(T("crew.withstandsHeat", ("n", ItemCards.Signed(read.High))), read.Heat > 0 ? Tone.Bad : Tone.Dim));
}
