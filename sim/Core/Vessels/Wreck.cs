using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>
/// What wrecked a vessel (vessel-wrecks): its events in time order, whether scavengers came after (the first group old), and the
/// holes they patched. What the client draws its looks from, never re-running the damage. Coordinates are in the frame after the
/// whole chain: a break moved every earlier event's with its half.
/// </summary>
public sealed record VesselWreck(IReadOnlyList<WreckEvent> Events, bool Scavenged, IReadOnlyList<WreckPatch> Patches);

/// <summary>
/// One event of a wreck's chain: its kind, its group's age and the group (events that follow as a consequence share it); its
/// centre and radius in frame cells and its direction (<see cref="Shipyard.Directions"/>, unscaled); the frame cells it carved, by
/// index in reading order (those a break moved off the frame left out); the compartments it burnt (a fire) or froze (icing, an
/// asteroid), by index; a break's cut; the stones an asteroid left; and what caused a break (an explosion, an asteroid; null for
/// overload).
/// </summary>
public sealed record WreckEvent(
    WreckKind Kind, bool Old, int Group,
    int X, int Y, int Radius, int DirX, int DirY,
    IReadOnlyList<int> Carved,
    IReadOnlyList<int> Compartments,
    WreckBreak? Break, IReadOnlyList<(int X, int Y)> Stones, WreckKind? Cause);

/// <summary>A break: the column it cut at, the columns between the halves, and the rows the stern and the bow moved (down is plus).</summary>
public sealed record WreckBreak(int Column, int Gap, int SternRows, int BowRows);

/// <summary>A patch scavengers laid over a hole, hull cells set back: its box in frame cells.</summary>
public sealed record WreckPatch(int X, int Y, int Width, int Height);

/// <summary>
/// A vessel as built (vessel-wrecks): described, stamped, then its wreck's chain applied. <c>Vessel</c> is the vessel as wrecked:
/// its sections, decks, compartments, doors, hatches and ladders moved with their half and those an event took left out; its
/// fittings what stands. <c>Wreck</c> null for one intact; <c>Parts</c> what its box holds, in reading order.
/// </summary>
public sealed record Built(Vessel Vessel, VesselWreck? Wreck, IReadOnlyList<VesselPart> Parts)
{
    /// <summary>The way in a hero arrives by, in <see cref="Vessel.Doors"/>: the intact vessel's first in reading order, wherever it moved.</summary>
    public int Entry { get; init; }
}
