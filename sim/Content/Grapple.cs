namespace Stillwake.Content;

/// <summary>
/// The grappling gun (grapple-climbs): the harpoon's mechanism made for climbing, its rod a hook that grips only the
/// structure and pulls the hero at once. Its range, the rod's speeds and where the pull stops are the harpoon's
/// (<see cref="Harpoon"/>); speeds in thousandths of a cell a tick, times in ticks. All of it is the client's.
/// </summary>
public static class Grapple
{
    // Not a const: every constant of this table is a whole number (ContentTests).
    /// <summary>The item that is the grappling gun, made at the workbench.</summary>
    public static readonly string Item = "grapple";

    /// <summary>How fast it pulls the hero toward its hook: 30 cells a second, half again the harpoon's.</summary>
    public const int PullSpeed = 1_500;

    /// <summary>Ticks from a shot till it fires again: 1 s, so a miss costs a moment, never the climb.</summary>
    public const int CooldownTicks = 20;
}
