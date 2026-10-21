namespace Stillwake.Game.Shared;

/// <summary>
/// The mark in an inventory slot's top-right corner: what the open fed station or fire does with the item, or that the
/// open machine takes it as a module, else the violet "?" of an item not analysed yet. A machine's mark goes before the "?".
/// The same marks, drawn by <see cref="MarkLooks"/>, are the faint ghosts of a station's own empty slots (0095).
/// </summary>
public enum SlotMark
{
    None,
    /// <summary>Not analysed yet: the violet "?"; an analyzer's slot's ghost.</summary>
    Unknown,
    /// <summary>The open fed station smelts it: a crucible.</summary>
    Smelts,
    /// <summary>The open machine's fire burns it as fuel: a flame.</summary>
    Burns,
    /// <summary>The open grow capsule grows it: a sprout.</summary>
    Grows,
    /// <summary>A module the open machine takes (0086): its technology's icon.</summary>
    Fits,
    /// <summary>A charging station's slot's ghost, a battery: no inventory slot wears it.</summary>
    Charges,
    /// <summary>A drone bay's slots' ghost, a drone: no inventory slot wears it.</summary>
    Docks,
}
