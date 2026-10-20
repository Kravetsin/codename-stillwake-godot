namespace Stillwake.Sim;

/// <summary>
/// One grid of a world, run by a set of modules of its own (0076): the ship, and later the planets heroes beam
/// down to. <c>Kind</c> names its rules in content; <c>Seed</c> is what its ground grows from, 0 for the ship,
/// whose grid is its layout. Ids are never reused.
/// </summary>
public sealed record Surface(int Id, string Kind, uint Seed)
{
    /// <summary>The kind of surface 0, the ship: there from the start, never closed.</summary>
    public const string Ship = "ship";
}
