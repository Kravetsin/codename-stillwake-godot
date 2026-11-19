namespace Stillwake.Content;

/// <summary>
/// The light drone (light-drone, drone-companion): crafted at the pod from the dormant core every hero comes aboard with
/// (<see cref="Core"/>) armoured in raw scrap, a right click in hand pairs a hero with it for good, the
/// item gone, and opens its bag rows (<see cref="Inventory.Start"/>). A small drone floating by a paired hero's head, its beam where
/// the pointer points once its lamp is bought, seen by friends, drawn by the client alone. It comes dark: its lamp and scanner are modes
/// found later, each a node; the lamp, once bought, draws no power.
/// </summary>
public static class LightDrone
{
    // Not a const: every constant of this table is a whole number (ContentTests).
    /// <summary>The item that is the light drone.</summary>
    public static readonly string Item = "lightDrone";

    /// <summary>
    /// The dormant core (drone-companion): in every hero's kit (<see cref="Inventory.Kit"/>), an unknown object that will not wake
    /// bare; crafted into the drone with <see cref="Armour"/> chunks of raw scrap. Gear, so a death keeps it; one a hero, so one drone.
    /// </summary>
    public static readonly string Core = "dormantCore";

    /// <summary>How many raw scrap chunks the core wakes in.</summary>
    public const int Armour = 5;

    /// <summary>
    /// The node that opens the scanner mode (2026-11-07-the-scanner, light-drone): bought once on the black market, known crew-wide;
    /// in that mode the drone's light is out and a wreck's hit points show under the pointer.
    /// </summary>
    public static readonly string ScannerMode = "scanner";

    /// <summary>
    /// The node that opens the lamp mode (light-drone): bought once on the black market from the start, known crew-wide; till then the
    /// paired drone gives no light.
    /// </summary>
    public static readonly string LampMode = "droneLamp";
}
