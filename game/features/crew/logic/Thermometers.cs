using Stillwake.Game.Shared;
using Stillwake.Sim.Crew;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What the vitals' thermometer says (the-thermometer): the degrees where the hero stands, calm inside its limit, frost-blue
/// past it and deeper the further; on hover the reading and the limit.
/// </summary>
public static class Thermometers
{
    /// <summary>What the thermometer reads for hero <paramref name="peer"/> at <paramref name="cell"/>, its body's: the degrees there, its limit, the degrees it feels past it.</summary>
    public static (int Degrees, int Low, int Felt) Read(ICrewQueries crew, int peer, int cell) => (crew.TemperatureAt(cell), crew.LowOf(peer), crew.Felt(peer, cell));

    /// <summary>How deep the reading's frost is, 0 to 1: the degrees <paramref name="felt"/>, eased as the screen's frost is (<see cref="Report.Cold"/>).</summary>
    public static float Frost(int felt) => Report.Eased((float)felt / Report.FeltFull);

    /// <summary>The reading as it is drawn: a true minus below zero, a plus above it, the degree sign: −50°, 0°, +20°.</summary>
    public static string Reading(int degrees) => (degrees > 0 ? "+" : "") + ItemCards.Degrees(degrees) + "°";

    /// <summary>The hover: the reading, and the coldest the hero withstands, red while it feels the cold.</summary>
    public static Tip Tip(int degrees, int low, int felt) =>
        new(Reading(degrees), new TipLine(T("crew.withstands", ("n", ItemCards.Degrees(low))), felt > 0 ? Tone.Bad : Tone.Dim));
}
