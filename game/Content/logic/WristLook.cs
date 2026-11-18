namespace Stillwake.Game;

/// <summary>
/// How the wrist computer's gauges and keys look and move (0081), the user's picks from the wrist-gauges mockup:
/// segmented gauges (A), energy as the bolt and a number, the bag's sort key on the display's right edge (A); the air bar
/// as the air-motion pick E has it. Pixels are screen pixels; a cosmetic of the client, never read by the sim.
/// </summary>
public static class WristLook
{
    /// <summary>
    /// A drop between two reads this big, of full, is a hit: its lost part stays lit white. Just over a running breath (2 of
    /// 45 suit units, 4.4 %), so breathing never counts as one.
    /// </summary>
    public const double BigDrop = 0.05;

    /// <summary>
    /// The air bar's springs (air-motion pick E), a second: the lit edge following the air; a part put on or taken off growing
    /// or shrinking; the bar turning red; the refill's light; the edge's highlight.
    /// </summary>
    public const double AirFollow = 9, AirResize = 7, AirRed = 5, AirGain = 4, AirEdge = 8;

    /// <summary>What a breath changes is spread over this long, so the bar glides instead of stepping once a breath.</summary>
    public const double BreathS = 1.0;

    /// <summary>The refill's light shows while the air rose by a breath this long ago or less.</summary>
    public const double GainS = 1.15;

    /// <summary>How fast the edge moves, in units a second, for its highlight to show whole.</summary>
    public const double EdgeSpeed = 1.2;

    /// <summary>A hit's lost air: lit whole this long, then drained back to the edge over <see cref="HitDrainS"/>; gone by <see cref="HitS"/>.</summary>
    public const double HitHoldS = 0.6, HitDrainS = 0.7, HitS = 1.6;

    /// <summary>A canister breathed: how long its light runs over the bar; how long a breath waits for its air to be read.</summary>
    public const double CanisterS = 2.2, CanisterWaitS = 1;

    /// <summary>The motes rising off the bar, and how high in pixels, on a slow refill and when a canister is breathed.</summary>
    public const int Motes = 4, MoteRise = 7, SurgeMotes = 12, SurgeRise = 13;

    /// <summary>
    /// Breathing room air again (out of thin air or vacuum): one soft wave across the air bar over this long; and never again
    /// within <see cref="AgainGapS"/>, so a sill's to and fro waves once.
    /// </summary>
    public const double WaveS = 0.85, AgainGapS = 4;

    /// <summary>How bright the charge row is with no battery on the belt.</summary>
    public const float NoBattery = 0.35f;

    /// <summary>The bag's sort key on the display's right edge: its size, how far right of the screen and down from its top.</summary>
    public const int SideKeyWidth = 24, SideKeyHeight = 34, SideKeyOut = 6, SideKeyDown = 2;

    /// <summary>A machine header's keys (sort, drones): square.</summary>
    public const int KeycapSide = 24;

    /// <summary>A key's glyph glows this long after it is let go.</summary>
    public const double KeyAfterS = 0.45;

    /// <summary>A sort's light crossing the grid: how long it takes, how long it fades after, and its band behind.</summary>
    public const double SweepS = 0.34, SweepFadeS = 0.1;

    /// <summary>The band of light behind a sort's line, in pixels.</summary>
    public const int SweepBand = 46;

    /// <summary>The display's lamp flares this long after the bag is sorted.</summary>
    public const double LampBlinkS = 0.5;
}
