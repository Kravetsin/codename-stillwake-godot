namespace Stillwake.Content;

/// <summary>
/// A hero's breathing, in oxygen units and ticks (20 a second). Every <see cref="BreathEvery"/> ticks a
/// hero takes a breath of one unit, from an aired room's air or else from its own air, which holds its
/// lungs and the worn helmet's <c>Tank</c> past them (air-segments); in an aired room that air also
/// fills from empty in <see cref="FillBreaths"/> breaths, whatever it holds. No air left starts the hero's <see cref="LastBreath"/>; run down,
/// it passes the hero out for <see cref="PassOut"/> ticks (2026-11-05-the-last-breath). Heroes wear one of
/// <see cref="Colours"/> suit colours, each taken by one hero at most while there are colours to go round.
/// Running, a hero is out of breath and breathes <see cref="Exerted"/> units a breath:
/// stamina is the air it costs. Within <see cref="PodReach"/> cells of the lifepod a hero's air takes
/// up to <see cref="PodRefill"/> units a breath of the pod's own air first, from no store.
/// </summary>
public static class Crew
{
    /// <summary>The air a hero's lungs hold, the first part of its air, helmet or not (air-segments).</summary>
    public const int Lungs = 20;
    public const int BreathEvery = 20;
    /// <summary>The breaths an aired room fills a hero's air in from empty, whatever it holds: a breath gives its share, rounded up (air-segments).</summary>
    public const int FillBreaths = 15;
    public const int PassOut = 100;
    /// <summary>The ticks a hero lives on once its air runs dry, ten seconds: any air back in time spares it.</summary>
    public const int LastBreath = 200;
    /// <summary>The ticks of a last breath a unit of lava, shock or blow cuts: the breath a unit of air is worth.</summary>
    public const int LastBreathPerUnit = BreathEvery;
    /// <summary>
    /// The ticks a hero is spared from its <c>PairDrone</c> on, as its drone's meeting plays (drone-meeting): no air spent, no last
    /// breath, cold, heat, hunger, blow or lava; the scene's 24.45 s from E, rounded up.
    /// </summary>
    public const int MeetingTicks = 490;
    /// <summary>
    /// The ticks a hero is spared from its <c>Envision</c> on, as the vision of a form its drone learned plays (form-vision): one window for every
    /// form, the longest scene's (the scanner's 13.8 s) and 1 s for the round trip to the host, spared as by <see cref="MeetingTicks"/>.
    /// </summary>
    public const int VisionTicks = 296;
    public const int Colours = 6;
    public const int Exerted = 2;
    /// <summary>The pod's air a breath: twice a calm breath, so a hero's air fills by the pod even in vacuum, slowly; a bare one's holds.</summary>
    public const int PodRefill = 2;
    /// <summary>How far from the pod's cells its air reaches a hero's, in cells either way: standing by it.</summary>
    public const int PodReach = 2;

    /// <summary>The pod's hatch, where heroes are made, from its top-left cell: the middle of its bottom row.</summary>
    public static readonly (int X, int Y) PodHatch = (2, 5);

    /// <summary>
    /// What a canister breathed in gives a hero's air, in units, up to full: absolute, so a bigger suit
    /// still gets a canister's worth (0047).
    /// </summary>
    public const int CanisterRefill = 20;

    /// <summary>Ticks after a canister breathed before a hero may breathe another, 20 s (food-and-the-body).</summary>
    public const int CanisterCooldown = 400;

    /// <summary>
    /// The fewest ticks between two of a client's reports of its hero's cell while the sim's is not the
    /// body's: once a second, so the drain past the field follows a hero flying out (0050).
    /// </summary>
    public const int ReportEvery = 20;

    /// <summary>
    /// The fewest ticks between two reports while a creature threatens the hero (<c>Creatures.ThreatReach</c>): every tick it
    /// moves, so what charges it aims a round trip behind, not a second (2026-10-26-creatures-are-composed).
    /// </summary>
    public const int ReportThreatened = 1;

    // Not a const: every constant of this table is a whole number (ContentTests).
    /// <summary>The item breathed in to refill a hero's air.</summary>
    public static readonly string Canister = "canister";
}
