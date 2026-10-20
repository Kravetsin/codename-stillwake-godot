namespace Stillwake.Game;

/// <summary>
/// How the wrist computer's gauges and keys look and move (0081), the user's picks from the wrist-gauges mockup:
/// segmented gauges (A), the loss trail that flashes and drains (T3), energy as a number and a bar (N2), the bag's
/// sort key on the display's right edge (A). Pixels are screen pixels; a cosmetic of the client, never read by the sim.
/// </summary>
public static class WristLook
{
    /// <summary>Between two gauge tabs' screens, and from the column's right edge to what stands on its rail: one seam.</summary>
    public const int TabGap = 6;

    /// <summary>A gauge tab's screen height.</summary>
    public const int TabHeight = 30;

    /// <summary>The energy readout's tab, right of the column: wide enough for "5 400".</summary>
    public const int ReadoutWidth = 48;

    /// <summary>The oxygen gauge's cells: three suit units a cell.</summary>
    public const int SuitCells = 15;

    /// <summary>The energy gauge's cells for each battery the belt can wear, a wider gap between batteries.</summary>
    public const int CellsPerBattery = 4;

    /// <summary>A cell's height, its top in its tab, the gap between cells and the wider one between batteries.</summary>
    public const int CellHeight = 10, CellTop = 10, CellGap = 2, GroupGap = 4;

    /// <summary>Where the meter starts in the oxygen tab (after the canister) and in the energy tab (after the bolt), and its margin on the right.</summary>
    public const int SuitMeterLeft = 30, EnergyMeterLeft = 32, MeterRight = 12;

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

    /// <summary>The cells' colours: unlit, and the unlit cell's top edge.</summary>
    public const uint CellOff = 0x16203a, CellOffTop = 0x1f2b46;

    /// <summary>Oxygen's sky, red in danger; energy's green, amber while the belt runs low (the slot bar's own).</summary>
    public const uint Suit = 0x38bdf8, SuitLow = 0xef4444, Energy = 0x7ee36b, EnergyLow = 0xf2b84b;

    /// <summary>How bright the energy tabs are with no battery on the belt.</summary>
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
