namespace Stillwake.Content;

/// <summary>
/// The hand harpoon (ADR 0044): distances in thousandths of a cell, speeds in thousandths of a cell a
/// tick, times in ticks. The sim reads only <see cref="ReelSpeed"/>; the shot, its reach, its cooldown and
/// the grapple that pulls the hero are the client's, among friends.
/// </summary>
public static class Harpoon
{
    // Not a const: every constant of this table is a whole number (ContentTests).
    /// <summary>The item that is the harpoon, crafted at the lifepod.</summary>
    public static readonly string Item = "harpoon";

    /// <summary>How far the rod flies from the muzzle before it winds back: 40 cells, a tow line (0068).</summary>
    public const int Range = 40_000;

    /// <summary>How fast the rod flies: 80 cells a second.</summary>
    public const int RodSpeed = 4_000;

    /// <summary>How fast a rod come free snaps back to the gun, a miss or a line let go: 160 cells a second.</summary>
    public const int ReturnSpeed = 8_000;

    /// <summary>How fast a reeled piece comes in along its longer axis: 16 cells a second (0068).</summary>
    public const int ReelSpeed = 800;

    /// <summary>How far before the gun a reeled piece stops, inside the hands' reach: 3 cells.</summary>
    public const int Stop = 3_000;

    /// <summary>
    /// Reeling, the client sends the reel again to where the point before the gun is now once it has moved
    /// this far from the last one sent, so the piece follows a hero that swims: 2 cells.
    /// </summary>
    public const int FollowBeyond = 2_000;

    /// <summary>And at most once in this many ticks: 0.5 s.</summary>
    public const int FollowTicks = 10;

    /// <summary>How fast the grapple pulls the hero toward a rod stuck in the ship's structure: 20 cells a second.</summary>
    public const int PullSpeed = 1_000;

    /// <summary>How far short of the rod's point the hero's middle is pulled to, as of a surface: a cell.</summary>
    public const int PullStop = 1_000;

    /// <summary>
    /// Ticks from a hero's shot till it fires again: 5 s. The cooldown is the hero's: every harpoon it
    /// holds waits for it, so swapping guns does not dodge it.
    /// </summary>
    public const int CooldownTicks = 100;
}
