namespace Stillwake.Game;

/// <summary>
/// How the wrist computer's gauges and keys look and move (0081), the user's picks from the wrist-gauges mockup:
/// segmented gauges (A), the loss trail that flashes and drains (T3), energy as the bolt and a number, the bag's
/// sort key on the display's right edge (A). Pixels are screen pixels; a cosmetic of the client, never read by the sim.
/// </summary>
public static class WristLook
{
    /// <summary>Between two gauge tabs' screens, and from the column's right edge to the bag's sort key: one seam.</summary>
    public const int TabGap = 6;

    /// <summary>A gauge tab's screen height.</summary>
    public const int TabHeight = 30;

    /// <summary>The energy tab at the column's right end: the bolt and the belt's charge, just wide enough for "5 400".</summary>
    public const int EnergyWidth = 70;

    /// <summary>The energy number's right margin in its tab, and its baseline.</summary>
    public const int EnergyRight = 8, EnergyBaseline = 20;

    /// <summary>The oxygen gauge's cells: three suit units a cell.</summary>
    public const int SuitCells = 15;

    /// <summary>A cell's height, its top in its tab and the gap between cells.</summary>
    public const int CellHeight = 10, CellTop = 10, CellGap = 2;

    /// <summary>Where the meter starts in the oxygen tab (after the canister), and its margin on the right.</summary>
    public const int SuitMeterLeft = 30, MeterRight = 12;

    /// <summary>
    /// A drop between two reads this big, of full, is a hit: the lost part flashes, then drains. Just over a running
    /// breath (2 of 45 suit units, 4.4 %), so breathing never flashes.
    /// </summary>
    public const double BigDrop = 0.05;

    /// <summary>How long the lost part stays whole after a hit before it drains, and how long its flash fades.</summary>
    public const double TrailHoldS = 0.15, FlashS = 0.3;

    /// <summary>How fast the lost part drains: this much of full a second, and this much more for each of full still lost.</summary>
    public const double TrailDrain = 0.5, TrailDrainPerGap = 5;

    /// <summary>A small change slides at an even pace over this long: a breath's second, so a steady drain moves steadily.</summary>
    public const double SlideS = 1.0;

    /// <summary>A big refill eases out at this rate a second, never slower than <see cref="RiseFloor"/> of full a second.</summary>
    public const double RiseEase = 6, RiseFloor = 0.12;

    /// <summary>The refill's light (a sweep along the cells, the head bright) stays this long after the value stops rising.</summary>
    public const double GainS = 0.45;

    /// <summary>The fill's bright leading edge fades this long after the fill stops moving, rising or falling.</summary>
    public const double EdgeFadeS = 0.3;

    /// <summary>
    /// A canister breathed (0047): the oxygen gauge's surge, stronger than a slow refill's light, for this long from the
    /// click: a sky wash over the tab, a white sweep across every cell this many cells a second, and more motes rising higher.
    /// </summary>
    public const double SurgeS = 1.3, SurgeSweep = 26, SurgeWash = 0.14;

    /// <summary>The motes rising off the cells, and how high in pixels, on a slow refill and in a canister's surge.</summary>
    public const int Motes = 4, MoteRise = 7, SurgeMotes = 12, SurgeRise = 13;

    /// <summary>
    /// Breathing room air again (out of thin air or vacuum): one soft wave across the oxygen cells over this long, this many
    /// cells either side of its middle, this bright; and never again within <see cref="AgainGapS"/>, so a sill's to and fro waves once.
    /// </summary>
    public const double WaveS = 0.85, WaveBand = 2.5, WaveBright = 0.4, AgainGapS = 4;

    /// <summary>
    /// The danger letting go (air back after a low suit): the red edges give way in <see cref="LetGoInS"/> to a soft edge of
    /// pale light, at most this opaque at the corners, that fades over <see cref="LetGoOutS"/>. Never the whole screen.
    /// </summary>
    public const double LetGoInS = 0.15, LetGoOutS = 0.7;

    /// <summary>The letting-go edge's colour and its opacity at the corners at its brightest.</summary>
    public const uint LetGo = 0xd6f6ff;
    public const float LetGoAlpha = 0.3f;

    /// <summary>The cells' colours: unlit, and the unlit cell's top edge.</summary>
    public const uint CellOff = 0x16203a, CellOffTop = 0x1f2b46;

    /// <summary>Oxygen's sky, red in danger; energy's green, amber while the belt runs low (the slot bar's own).</summary>
    public const uint Suit = 0x38bdf8, SuitLow = 0xef4444, Energy = 0x7ee36b, EnergyLow = 0xf2b84b;

    /// <summary>How bright the energy tab is with no battery on the belt.</summary>
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
