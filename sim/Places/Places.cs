using System.Text.Json.Serialization;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Places;

/// <summary>A vessel the radar found that the ship has not jumped to (generated-vessels): its class and the seed it grows from.</summary>
public sealed record Contact(int Id, string Class, uint Seed);

/// <summary>Finds a vessel of <c>Class</c>: a contact, its seed drawn from the world's Rng. Sent by the dev flag until the radar does.</summary>
public sealed record Find(string Class) : Command;

/// <summary>
/// Jumps beside contact <c>Contact</c> (generated-vessels): the vessel there before is cleared, and this one is written into its
/// box right of the ship, wrecked by a breach; <c>Intact</c>, sent only by the dev flag, as built.
/// </summary>
public sealed record Jump(int Contact, bool Intact = false) : Command;

/// <summary>The vessel beside the ship taken away whole, nothing given, and its box freed for building: the dev panel's.</summary>
public sealed record Dismiss : Command;

/// <summary>The places slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class PlacesState
{
    /// <summary>The contacts the radar holds, in the order found: at most <c>Vessels.MaxContacts</c>.</summary>
    [JsonInclude] internal List<Contact> Contacts { get; set; } = [];
    [JsonInclude] internal int NextContactId { get; set; } = 1;
}

/// <summary>
/// Where the ship is and what lies beside it (generated-vessels): the contacts the radar found, and the jump that writes a vessel
/// into the grid beside the ship, the last one cleared. Aboard only; the cells are the grid's, the vessel's box its reserved one.
/// </summary>
public sealed class PlacesModule : Module<PlacesState>
{
    private readonly IGridQueries _grid;
    private readonly IGridOps _gridOps;
    private readonly IItemsOps _itemsOps;
    private readonly IBuildingOps _building;

    public override string Id => "places";

    public PlacesModule(IGridQueries grid, IGridOps gridOps, IItemsOps itemsOps, IBuildingOps building)
    {
        _grid = grid;
        _gridOps = gridOps;
        _itemsOps = itemsOps;
        _building = building;
        Handles<Find>(FindProblem, ApplyFind);
        Handles<Jump>(JumpProblem, ApplyJump);
        Handles<Dismiss>(_ => Rules != Content.Surfaces.Ship ? "places.notAboard" : _grid.Reserved == null ? "places.noVessel" : null,
            _ => Clear());
    }

    /// <summary>The vessel out taken apart to nothing (<c>Building.Wipe</c>) and its box freed.</summary>
    private void Clear()
    {
        _building.Wipe(_grid.Reserved!.Value);
        _gridOps.Reserve(null);
    }

    protected override PlacesState Init(Layout layout) => new();

    /// <summary>The contacts the radar holds, in the order found: live and read-only.</summary>
    public IReadOnlyList<Contact> Contacts => State.Contacts;

    private string? FindProblem(Find c) =>
        Rules != Content.Surfaces.Ship ? "places.notAboard"
        : Vessels.Class(c.Class) == null ? "places.noClass"
        : State.Contacts.Count >= Vessels.MaxContacts ? "places.full" : null;

    private void ApplyFind(Find c) => State.Contacts.Add(new(State.NextContactId++, c.Class, Random.Below(1UL << 32)));

    private string? JumpProblem(Jump c)
    {
        if (Rules != Content.Surfaces.Ship) return "places.notAboard";
        if (State.Contacts.Find(k => k.Id == c.Contact) is not { } contact) return "places.noContact";
        return BoxFor(Describe(contact)) == null ? "places.noRoom" : null;
    }

    /// <summary>
    /// The old vessel taken apart to nothing, the new one described, its box reserved beside the ship, and its parts laid in reading
    /// order, so its machines' ids follow it; its crates filled.
    /// </summary>
    private void ApplyJump(Jump c)
    {
        var contact = State.Contacts.Find(k => k.Id == c.Contact)!;
        var vessel = Describe(contact);
        var box = BoxFor(vessel)!.Value;
        if (_grid.Reserved is { } old) _building.Wipe(old);
        _gridOps.Reserve(box);
        int width = _grid.Width;
        foreach (var part in Shipyard.Damage(Shipyard.Stamp(vessel), vessel, c.Intact ? VesselCause.None : VesselCause.Breach))
        {
            int cell = (box.Top + part.Y) * width + box.Left + part.X;
            _gridOps.Lay(cell, new Placement(part.Layer, part.Kind, part.Material));
            if (part.Holds is { } holds && _grid.MachineAt(cell) is { } crate)
                foreach (var (item, count) in holds) _itemsOps.PutIn(crate.Id, new Stack(item, count));
        }
        State.Contacts.RemoveAll(k => k.Id == contact.Id);
    }

    private static Vessel Describe(Contact contact) => Shipyard.Describe(Vessels.Class(contact.Class)!, contact.Seed);

    /// <summary>
    /// The box <paramref name="vessel"/> goes into: <c>Vessels.Gap</c> columns right of the ship, its rows centred on the ship's
    /// (rounded down), moved inside the grid by <c>Vessels.EdgeClear</c>; null when it does not fit.
    /// </summary>
    private Box? BoxFor(Vessel vessel)
    {
        var ship = ShipBox();
        if (ship.IsEmpty) return null;
        int edge = Vessels.EdgeClear, left = ship.Right + Vessels.Gap + 1, right = left + vessel.Width - 1;
        int lowest = _grid.Height - edge - vessel.Height;
        if (right > _grid.Width - 1 - edge || lowest < edge) return null;
        int top = Math.Clamp(ship.Top + ship.Height / 2 - vessel.Height / 2, edge, lowest);
        return new Box(left, top, right, top + vessel.Height - 1);
    }

    /// <summary>The tight box of everything built but the vessel's: <c>Grid.Built</c> with no vessel out, else found by a walk of it.</summary>
    private Box ShipBox()
    {
        var built = _grid.Built;
        if (_grid.Reserved is not { } vessel) return built;
        var structure = _grid.StructureLayer;
        var back = _grid.BackLayer;
        int width = _grid.Width, left = int.MaxValue, right = -1, top = int.MaxValue, bottom = -1;
        for (int y = built.Top; y <= built.Bottom; y++)
            for (int x = built.Left; x <= built.Right; x++)
            {
                int cell = y * width + x;
                if (vessel.Contains(x, y) || structure[cell] == Structure.Empty && back[cell] == Back.None && _grid.MachineAt(cell) == null) continue;
                (left, right, top, bottom) = (Math.Min(left, x), Math.Max(right, x), Math.Min(top, y), Math.Max(bottom, y));
            }
        return right < 0 ? Box.None : new Box(left, top, right, bottom);
    }

    /// <summary>Now and then, as a jump writes thousands of cells: a vessel dismissed, out or not; a contact found, of a class or none; a jump to one, held or not, intact or not.</summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(16) != 0) return null;
        if (rng.Below(4) == 0) return new Dismiss { From = from };
        if (rng.Below(2) == 0)
        {
            string cls = rng.Below(5) == 0 ? "nothing" : Vessels.Classes[(int)rng.Below((uint)Vessels.Classes.Count)].Id;
            return new Find(cls) { From = from };
        }
        int contact = State.Contacts.Count > 0 && rng.Below(4) != 0
            ? State.Contacts[(int)rng.Below((uint)State.Contacts.Count)].Id
            : (int)rng.Below(4);
        return new Jump(contact, rng.Below(2) == 0) { From = from };
    }
}
