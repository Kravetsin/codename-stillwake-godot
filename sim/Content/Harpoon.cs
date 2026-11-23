namespace Stillwake.Content;

/// <summary>
/// The hand harpoon (ADR 0044, debris-physics): distances in thousandths of a cell, speeds in thousandths of a cell a
/// tick, times in ticks. The sim reads only <see cref="Range"/>, a hooked line's longest; the shot, its cooldown, the grapple that
/// pulls the hero and the rope that pulls a piece are the client's, the rope the host player's, among friends.
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

    /// <summary>How fast the drum winds the line in while the left button is held: 16 cells a second (0068), the prototype's.</summary>
    public const int ReelSpeed = 800;

    /// <summary>How short the drum winds the line, the hook inside the hands' reach: 3 cells; there winding does nothing.</summary>
    public const int Stop = 3_000;

    /// <summary>
    /// How much line the drum pays out past its wound length under load, a piece swinging wide or the hero jetting off: 10
    /// cells, the prototype's (debris-physics).
    /// </summary>
    public const int PayOut = 10_000;

    /// <summary>
    /// The pull the drum holds before it slips and pays out, in thousandths of a cell a second² times the piece's mass (a cell² of
    /// box at <c>Debris.Density</c> 1000 weighs one): 375 cells/s², the prototype's 3000 px/s². A heavy piece slips it sooner.
    /// </summary>
    public const int Brake = 375_000;

    /// <summary>The brake hardens to 1 + this times as the line paid out nears <see cref="PayOut"/>, by the square of the share: the prototype's 8.</summary>
    public const int BrakeRamp = 8;

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
