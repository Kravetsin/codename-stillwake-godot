namespace Stillwake.Content;

/// <summary>
/// The hand tools that work a layer of their own directly, with no plan (0108): held, each shows its overlay, its left
/// button laying and its right taking away. Every hero comes aboard with them (<see cref="Inventory.Kit"/>).
/// </summary>
public static class HandTools
{
    /// <summary>The stripper: lays cables from the inventory and cuts them, refunded.</summary>
    public const string Stripper = "stripper";

    /// <summary>The wrench (0109): lays pipe spans from the inventory and takes them away, refunded; turns joints into valves.</summary>
    public const string Wrench = "wrench";
}
