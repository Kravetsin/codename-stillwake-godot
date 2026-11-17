using Stillwake.Content;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// A vessel's guards (derelict-enemies): one asleep at each of its docks (<see cref="Shipyard.Guards"/>), spawned as its world opens,
/// before any other creature there, so their ids follow the docks. Read from the vessel's description, never the grid: Places writes
/// the vessel after this module has run.
/// </summary>
public sealed partial class CreaturesModule
{
    /// <summary>Kept in a guard's <c>Mem</c>: its dock's place in <see cref="Docks"/>.</summary>
    internal const string DockKey = "dock";

    private IReadOnlyList<Shipyard.Dock> _docks = [];

    /// <summary>The guards' docks of this surface's vessel, in <see cref="Shipyard.Guards"/>'s order; none off a vessel world. Derived.</summary>
    public IReadOnlyList<Shipyard.Dock> Docks => _docks;

    /// <summary>The place in <see cref="Docks"/> of the dock <paramref name="c"/> was spawned at; null for a creature of no vessel.</summary>
    public static int? DockOf(Creature c) => Spec.Get(c.Species).Lifetime == Spec.Lifetime.Aboard ? c.Recall(DockKey) : null;

    public override void Rebuild() =>
        _docks = Rules is VesselKind kind ? Shipyard.Guards(Shipyard.Describe(Vessels.Class(kind.Class)!, Surfaces.Seed)) : [];

    /// <summary>A guard asleep on the floor in the middle of each dock's box, in their order: the frame's top-left at (<c>Margin</c>, <c>Margin</c>).</summary>
    protected override void Opened()
    {
        int m = Vessels.Margin;
        for (int i = 0; i < _docks.Count; i++)
        {
            var dock = _docks[i];
            var spec = Spec.Get(dock.Species);
            var (x, y) = Standing(spec, m + dock.X + (dock.Width - spec.Body.Width) / 2, m + dock.Y + dock.Height - 1);
            Add(spec, x, y).Keep(DockKey, i);
        }
    }
}
