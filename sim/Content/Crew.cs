namespace Stillwake.Content;

/// <summary>
/// A hero's breathing, in oxygen units and ticks (20 a second). Every <see cref="BreathEvery"/> ticks a
/// hero takes a breath of one unit, from an aired room's air or else from the suit, which holds
/// <see cref="Suit"/> units; in an aired room the suit also refills by up to <see cref="Refill"/> units a
/// breath. An empty suit passes the hero out for <see cref="PassOut"/> ticks. Heroes wear one of
/// <see cref="Colours"/> suit colours, each taken by one hero at most while there are colours to go round.
/// Running, a hero is out of breath and breathes <see cref="Exerted"/> units a breath:
/// stamina is the air it costs. Within <see cref="PodReach"/> cells of the lifepod a hero's suit takes
/// up to <see cref="PodRefill"/> units a breath of the pod's own air first, from no store.
/// </summary>
public static class Crew
{
    public const int Suit = 45;
    public const int BreathEvery = 20;
    public const int Refill = 3;
    public const int PassOut = 100;
    public const int Colours = 4;
    public const int Exerted = 2;
    /// <summary>The pod's air a breath: twice a calm breath, so a suit fills by the pod even in vacuum, slowly.</summary>
    public const int PodRefill = 2;
    /// <summary>How far from the pod's cells its air reaches a hero's, in cells either way: standing by it.</summary>
    public const int PodReach = 2;

    /// <summary>The pod's hatch, where heroes are made, from its top-left cell: the middle of its bottom row.</summary>
    public static readonly (int X, int Y) PodHatch = (2, 5);

    /// <summary>
    /// What a canister breathed into the suit gives it, in units, up to full: absolute, so a bigger suit
    /// later still gets a canister's worth (0047).
    /// </summary>
    public const int CanisterRefill = 20;

    /// <summary>
    /// The fewest ticks between two of a client's reports of its hero's cell while the sim's is not the
    /// body's: once a second, so the drain past the field follows a hero flying out (0050).
    /// </summary>
    public const int ReportEvery = 20;

    // Not a const: every constant of this table is a whole number (ContentTests).
    /// <summary>The item breathed into the suit.</summary>
    public static readonly string Canister = "canister";
}
