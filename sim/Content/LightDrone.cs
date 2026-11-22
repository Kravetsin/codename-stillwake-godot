namespace Stillwake.Content;

/// <summary>
/// The light drone (light-drone, drone-companion): a shard of the alien core that wrecked the scoop (<see cref="CoreMachine"/>), given
/// for good to a hero who touches the core with E, opening its bag rows (<see cref="Inventory.Start"/>); every hero still without one
/// is given it once the core breaks loose. A small drone floating by a paired hero's head, its beam where
/// the pointer points once its lamp is learned, seen by friends, drawn by the client alone. It comes dark: its lamp, scanner and alarms are
/// forms each player's drone learns (<see cref="Lessons"/>); the lamp draws no power.
/// </summary>
public static class LightDrone
{
    // Not a const: every constant of this table is a whole number (ContentTests).
    /// <summary>The item that is the light drone.</summary>
    public static readonly string Item = "lightDrone";

    /// <summary>
    /// The machine that is the alien core (drone-companion): lodged by the scoop's right lock, never broken or taken apart, pressed with E
    /// for a drone; gone for whoever has theirs, and for everyone once a cell of the floor under it is broken.
    /// </summary>
    public static readonly string CoreMachine = "alienCore";

    /// <summary>
    /// The scanner form (2026-11-07-the-scanner, drone-lessons): learned by each player from wrecks broken (<see cref="Lessons"/>); in that
    /// mode the drone's light is out and a wreck's hit points show under the pointer.
    /// </summary>
    public static readonly string ScannerMode = "scanner";

    /// <summary>
    /// The lamp form (drone-lessons): learned by each player passing a lamp a player built; till then the paired drone gives no light.
    /// </summary>
    public static readonly string LampMode = "droneLamp";

    /// <summary>The alarms form (drone-lessons): learned passing the wreck's emergency lamps; the drone's thoughts over its head show only once it is known.</summary>
    public static readonly string AlarmsMode = "droneAlarms";
}
