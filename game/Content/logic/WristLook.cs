namespace Stillwake.Game;

/// <summary>
/// How the wrist computer's gauges and keys look and move (0081), the user's picks from the wrist-gauges mockup:
/// segmented gauges (A), the loss trail that flashes and drains (T3), energy as the bolt and a number, the bag's
/// sort key on the display's right edge (A). Pixels are screen pixels; a cosmetic of the client, never read by the sim.
/// </summary>
public static class WristLook
{
    /// <summary>The equal stretches of the air row the chips of lost air and a canister's wash count in: three suit units each at a basic helmet.</summary>
    public const int SuitCells = 15;

    /// <summary>
    /// A drop between two reads this big, of full, is a hit: its lost part flashes too. Just over a running breath (2 of
    /// 45 suit units, 4.4 %), so breathing never flashes; a big refill eases in.
    /// </summary>
    public const double BigDrop = 0.05;

    /// <summary>How long a lost part stays whole before it drains, and how long a hit's flash fades.</summary>
    public const double TrailHoldS = 0.6, FlashS = 0.3;

    /// <summary>How fast the lost part drains: this much of full a second, and this much more for each of full still lost.</summary>
    public const double TrailDrain = 0.2, TrailDrainPerGap = 4;

    /// <summary>A small rise slides at an even pace over this long, so a slow refill moves steadily.</summary>
    public const double SlideS = 1.0;

    /// <summary>A big refill eases out at this rate a second, never slower than <see cref="RiseFloor"/> of full a second.</summary>
    public const double RiseEase = 6, RiseFloor = 0.12;

    /// <summary>The refill's light (a sweep along the cells, the head bright) stays this long after the value stops rising.</summary>
    public const double GainS = 0.45;

    /// <summary>The fill's bright leading edge fades this long after the fill stops moving, rising or falling.</summary>
    public const double EdgeFadeS = 0.3;

    /// <summary>
    /// A canister breathed (0047): how long the motes rise thick and high off the cells from the click; the tab's flood, rim
    /// and shake are <c>GaugeWash</c>'s (air-feedback).
    /// </summary>
    public const double SurgeS = 1.3;

    /// <summary>The motes rising off the cells, and how high in pixels, on a slow refill and in a canister's surge.</summary>
    public const int Motes = 4, MoteRise = 7, SurgeMotes = 12, SurgeRise = 13;

    /// <summary>
    /// Breathing room air again (out of thin air or vacuum): one soft wave across the oxygen cells over this long, this many
    /// cells either side of its middle, this bright; and never again within <see cref="AgainGapS"/>, so a sill's to and fro waves once.
    /// </summary>
    public const double WaveS = 0.85, WaveBand = 2.5, WaveBright = 0.4, AgainGapS = 4;

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
