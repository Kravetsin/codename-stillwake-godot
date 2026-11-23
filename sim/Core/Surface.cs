namespace Stillwake.Sim;

/// <summary>
/// One grid of a world, run by a set of modules of its own (0076): the ship, the planets heroes beam down to, and the
/// vessels they visit. <c>Kind</c> names its rules in content; <c>Seed</c> is what its ground grows from, 0 for the
/// ship, whose grid is its layout. Ids are never reused: a closed surface keeps its id, its slot empty (vessel-worlds).
/// </summary>
public sealed record Surface(int Id, string Kind, uint Seed)
{
    /// <summary>The kind of surface 0, the ship: there from the start, never closed.</summary>
    public const string Ship = "ship";

    /// <summary>Closed once nobody stood on it (a transient kind's, vessel-worlds): no modules, no phase run, no transfer to it.</summary>
    public bool Closed { get; init; }

    /// <summary>
    /// A planet's ores of <c>Content.Landing.Ores</c> (planet-variety), item ids in that order, its ground grown with these only;
    /// null for all: the ship, a vessel, a dev or test planet, an old save's.
    /// </summary>
    public IReadOnlyList<string>? Ores { get; init; }

    /// <summary>
    /// A vessel's forced chain of events (vessel-wrecks), a dev's: an empty one an intact vessel; null for the chain its seed draws,
    /// as for every other surface.
    /// </summary>
    public IReadOnlyList<Content.WreckStep>? Wreck { get; init; }
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
