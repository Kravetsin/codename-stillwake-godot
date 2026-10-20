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

/// <summary>
/// Opens a new surface of kind <c>Kind</c> (<c>Content.Surfaces</c>), its seed drawn from the world's random
/// numbers: a dev command until the drop pod. Nobody moves there by it.
/// </summary>
public sealed record OpenSurface(string Kind) : Command;

/// <summary>
/// Moves the sender's hero and what it carries to surface <c>To</c>, at <c>Cell</c> or that surface's spawn:
/// refused <c>world.noSurface</c> for a surface there is not, <c>world.there</c> for the one it is on.
/// </summary>
public sealed record Transfer(int To, int? Cell = null) : Command;
