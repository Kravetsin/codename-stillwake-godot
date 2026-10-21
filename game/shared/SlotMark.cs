namespace Stillwake.Game.Shared;

/// <summary>
/// The mark in an inventory slot's top-right corner: what the open fed station or fire does with the item, or that the
/// open machine takes it as a module, else the violet "?" of an item not analysed yet. A machine's mark goes before the "?".
/// </summary>
public enum SlotMark
{
    None,
    /// <summary>Not analysed yet: the violet "?".</summary>
    Unknown,
    /// <summary>The open fed station smelts it: a small orange flame.</summary>
    Smelts,
    /// <summary>The open machine's fire burns it as fuel: the same orange flame.</summary>
    Burns,
    /// <summary>The open grow capsule grows it: a small green sprout.</summary>
    Grows,
    /// <summary>A module the open machine takes (0086): its technology's icon.</summary>
    Fits,
}
