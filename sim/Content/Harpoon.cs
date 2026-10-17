namespace Stillwake.Content;

/// <summary>
/// The hand harpoon (ADR 0044): distances in thousandths of a cell, speeds in thousandths of a cell a
/// tick, times in ticks. The sim reads only <see cref="ReelSpeed"/>; the shot, its reach and its
/// cooldown are the client's, among friends.
/// </summary>
public static class Harpoon
{
    // Not a const: every constant of this table is a whole number (ContentTests).
    /// <summary>The item that is the harpoon, crafted at the lifepod.</summary>
    public static readonly string Item = "harpoon";

    /// <summary>How far the rod flies from the muzzle before it winds back: 12 cells.</summary>
    public const int Range = 12_000;

    /// <summary>How fast the rod flies: 40 cells a second.</summary>
    public const int RodSpeed = 2_000;

    /// <summary>How fast a rod come free snaps back to the gun, a miss or a line let go: 80 cells a second.</summary>
    public const int ReturnSpeed = 4_000;

    /// <summary>How fast a reeled piece comes in along its longer axis: 5 cells a second.</summary>
    public const int ReelSpeed = 250;

    /// <summary>How far before the gun a reeled piece stops, inside the hands' reach: 1.5 cells.</summary>
    public const int Stop = 1_500;

    /// <summary>
    /// Ticks from a hero's shot till it fires again: 5 s. The cooldown is the hero's: every harpoon it
    /// holds waits for it, so swapping guns does not dodge it.
    /// </summary>
    public const int CooldownTicks = 100;
}
