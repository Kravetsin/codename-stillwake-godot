namespace Stillwake.Content;

/// <summary>
/// Drone logistics (0062): a drone bay launches the drones docked in its slots to carry items from providers and
/// storage to requesters, and what fed stations make to storage, flying through air and doors within its zone.
/// </summary>
public static class Logistics
{
    /// <summary>A bay's zone: the cells within this many of its footprint, a square; a machine is in when any cell of it is.</summary>
    public const int Reach = 16;

    /// <summary>Thousandths of a cell a flying drone moves a tick, cell centre to cell centre: four cells a second.</summary>
    public const int Speed = 200;

    /// <summary>A bay looks for a job every this many ticks, bays spread by their ids; it launches one drone at most a look.</summary>
    public const int LookEvery = 10;

    /// <summary>The most requests a requester crate holds: a row of its grid.</summary>
    public const int RequestsMost = 9;

    // Not consts: every constant of this table is a whole number (ContentTests).
    /// <summary>The machine that launches drones, and the item a drone is while it is not flying.</summary>
    public static readonly string Bay = "droneBay", Drone = "drone";
}
