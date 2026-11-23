using Stillwake.Content;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// A vessel's guards (derelict-guards): one asleep at each of its docks (<see cref="Shipyard.Guards"/>), spawned as its world opens,
/// before any other creature there, so their ids follow the docks. Read from the vessel's description, never the grid: Places writes
/// the vessel after this module has run. What they wake and hunt by is the vessel's too: its rooms, hull box, floors, hatches, way in.
/// </summary>
public sealed partial class CreaturesModule
{
    /// <summary>Kept in a guard's <c>Mem</c>: its dock's place in <see cref="Docks"/>.</summary>
    internal const string DockKey = "dock";

    private IReadOnlyList<Shipyard.Dock> _docks = [];

    /// <summary>Each dock's room's open cells in the world, in <see cref="Docks"/>' order. Derived.</summary>
    private Box[] _cabins = [];

    /// <summary>The box of the vessel's hull and rooms in the world, its armour in it: aboard. <see cref="Box.None"/> off a vessel world. Derived.</summary>
    private Box _hull = Box.None;

    /// <summary>The rows of the vessel's floors in the world, a deck's and a hatch's, ascending and distinct. Derived.</summary>
    private int[] _floors = [];

    /// <summary>Its hatches in the world, each its left column and floor row, by row then column. Derived.</summary>
    private (int X, int Y)[] _hatches = [];

    /// <summary>Its doors between rooms in the world, the way in left out, each its left column and top row, by row then column. Derived.</summary>
    private (int X, int Y)[] _doors = [];

    /// <summary>Its way in through the armour (<see cref="Shipyard.WayIn"/>) in the world: never opened by a guard. Derived.</summary>
    private Box _wayIn = Box.None;

    /// <summary>The guards' docks of this surface's vessel, in <see cref="Shipyard.Guards"/>'s order; none off a vessel world. Derived.</summary>
    public IReadOnlyList<Shipyard.Dock> Docks => _docks;

    /// <summary>The box of this surface's vessel's hull and rooms, in the world: a hero in it is aboard. <see cref="Box.None"/> off a vessel world.</summary>
    public Box Hull => _hull;

    /// <summary>The place in <see cref="Docks"/> of the dock <paramref name="c"/> was spawned at; null for a creature of no vessel.</summary>
    public static int? DockOf(Creature c) => Spec.Get(c.Species).Lifetime == Spec.Lifetime.Aboard ? c.Recall(DockKey) : null;

    /// <summary>The cell of the shut doorway <paramref name="c"/> pries, a guard aboard; null while it pries none.</summary>
    public static int? PryingAt(Creature c) => c.Recall(ProwlDrive.PryKey) is int pried and > 0 ? pried - 1 : null;

    public override void Rebuild()
    {
        // Only an intact vessel has guards (Shipyard.Guards), built as Places writes it; a drawn wreck needs no build to know it.
        var cls = Rules is VesselKind kind ? Vessels.Class(kind.Class)! : null;
        bool drawnWreck = cls != null && Surfaces.Wreck == null && Shipyard.IsWreck(cls, Surfaces.Seed);
        if (cls == null || drawnWreck || (Surfaces.Vessel ?? Shipyard.Build(cls, Surfaces.Seed, Surfaces.Wreck)) is not { Wreck: null } built)
        {
            (_docks, _cabins, _hull, _floors, _hatches, _wayIn, _doors) = ([], [], Box.None, [], [], Box.None, []);
            return;
        }
        var v = built.Vessel;
        int m = Vessels.Margin;
        _docks = Shipyard.Guards(built);
        _cabins = [.. _docks.Select(d => v.Compartments[d.Room]).Select(r => new Box(m + r.Left, m + r.Top, m + r.Left + r.Width - 1, m + r.Floor - 1))];
        // The hull and its rooms, its bow's open rows past the last room too; never a module, rib or housing bulged onto it.
        _hull = Box.None;
        foreach (var h in v.Sections)
            if (h.Kind is Vessel.SectionKind.Body or Vessel.SectionKind.Bow or Vessel.SectionKind.Bridge or Vessel.SectionKind.Tier)
                _hull = _hull.Union(new Box(m + h.Left, m + h.Top, m + h.Left + h.Width - 1, m + h.Bottom));
        _floors = [.. v.Decks.Select(d => m + d.Floor).Concat(v.Hatches.Select(h => m + h.Y)).Distinct().Order()];
        _hatches = [.. v.Hatches.Select(h => (X: m + h.X, Y: m + h.Y)).OrderBy(h => h.Y).ThenBy(h => h.X)];
        var door = Shipyard.WayIn(built);
        _wayIn = new Box(m + door.X, m + door.Y, m + door.X + Doorways.Thick - 1, m + door.Y + Vessels.Open - 1);
        _doors = [.. v.Doors.Where(d => d != door).Select(d => (X: m + d.X, Y: m + d.Y)).OrderBy(d => d.Y).ThenBy(d => d.X)];
    }

    /// <summary>The place in <see cref="Docks"/> of <paramref name="c"/>'s dock; null for one past them (a save of another build): no dock.</summary>
    private int? DockIndex(Creature c) => c.Recall(DockKey) is int i && i >= 0 && i < _docks.Count ? i : null;

    /// <summary>Whether <paramref name="c"/> has a dock of this vessel: one without wakes at once and keeps to no deck.</summary>
    internal bool Docked(Creature c) => DockIndex(c) != null;

    /// <summary>The floor row of the deck <paramref name="c"/>'s dock stands on: its room's floor, in the world; null with no dock.</summary>
    internal int? DeckFloor(Creature c) => DockIndex(c) is int i ? Vessels.Margin + _docks[i].Y + _docks[i].Height : null;

    /// <summary>
    /// The middle, in thousandths, of the door on the deck whose floor is row <paramref name="floor"/> nearest <paramref name="x"/>
    /// (thousandths), the lower column at a tie; the way in never; null for none on that deck.
    /// </summary>
    internal int? DoorOn(int floor, int x)
    {
        int? best = null;
        foreach (var d in _doors)
        {
            int middle = (d.X + 1) * Unit;
            if (d.Y == floor - Vessels.Open && (best is not int b || Math.Abs(middle - x) < Math.Abs(b - x))) best = middle;
        }
        return best;
    }

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

    /// <summary>Whether an awake hero's reported cell is in the room of <paramref name="c"/>'s dock: not seen through a doorway, nor in one.</summary>
    internal bool HeroInCabin(Creature c)
    {
        if (DockIndex(c) is not int dock) return false;
        var room = _cabins[dock];
        for (int i = 0; i < _heroes.Count; i++)
            if (_awake[i] && room.Contains(_heroes[i].X, _heroes[i].Y)) return true;
        return false;
    }

    /// <summary>
    /// The reported cell of the awake hero aboard (in the hull box) nearest <paramref name="c"/> by squared distance in cells, the first in
    /// the crew's order at a tie, as <c>Shots</c>' homing finds one; null for none aboard.
    /// </summary>
    internal (int X, int Y)? Quarry(Creature c) => QuarryOf(c)?.At;

    /// <summary>The <see cref="Quarry"/> of <paramref name="c"/> with its hero's peer: what an escort's shot locks on.</summary>
    internal (int Peer, (int X, int Y) At)? QuarryOf(Creature c)
    {
        int x = Down(c.X, Unit), y = Down(c.Y - 1, Unit);
        (int Peer, (int X, int Y) At)? best = null;
        long far = long.MaxValue;
        for (int i = 0; i < _heroes.Count; i++)
        {
            var at = _heroes[i];
            if (!_awake[i] || !_hull.Contains(at.X, at.Y)) continue;
            long d = (long)(at.X - x) * (at.X - x) + (long)(at.Y - y) * (at.Y - y);
            if (d >= far) continue;
            (far, best) = (d, (_peers[i], at));
        }
        return best;
    }

    /// <summary>Whether a body of <paramref name="s"/> at (<paramref name="x"/>, <paramref name="y"/>) lies wholly in the hull box.</summary>
    internal bool InHull(Spec.Shape s, int x, int y)
    {
        int half = s.Width * Unit / 2;
        return _hull.Contains(Down(x - half, Unit), Down(y - s.Height * Unit, Unit)) && _hull.Contains(Down(x + half - 1, Unit), Down(y - 1, Unit));
    }

    /// <summary>The deck row <paramref name="row"/> is on: how many of the vessel's floors lie above it, so a raised bridge is above the first deck.</summary>
    internal int Level(int row)
    {
        int level = 0;
        while (level < _floors.Length && _floors[level] < row) level++;
        return level;
    }

    /// <summary>The floor row next above row <paramref name="row"/> when <paramref name="up"/>, else the first at or under it; null for none.</summary>
    internal int? FloorBy(int row, bool up)
    {
        int? found = null;
        foreach (int f in _floors)
            if (up ? f < row : f >= row && found == null) found = f;
        return found;
    }

    /// <summary>
    /// The hatch in floor row <paramref name="floor"/> whose middle is nearest <paramref name="x"/> (thousandths), the lower column at a tie:
    /// its middle in thousandths; null for none in that floor.
    /// </summary>
    internal int? HatchIn(int floor, int x)
    {
        int? best = null;
        foreach (var h in _hatches)
        {
            int middle = (h.X + 1) * Unit;
            if (h.Y == floor && (best is not int b || Math.Abs(middle - x) < Math.Abs(b - x))) best = middle;
        }
        return best;
    }

    /// <summary>Whether a hatch lies in floor row <paramref name="row"/> right over a body whose middle is <paramref name="x"/> (thousandths).</summary>
    internal bool HatchAt(int x, int row)
    {
        foreach (var h in _hatches)
            if (h.Y == row && (h.X + 1) * Unit == x) return true;
        return false;
    }

    /// <summary>The floor row of the hatch right over a body whose middle is <paramref name="x"/> (thousandths), nearest above its feet <paramref name="y"/>; null for none.</summary>
    internal int? HatchOver(int x, int y)
    {
        int? row = null;
        foreach (var h in _hatches)
            if ((h.X + 1) * Unit == x && h.Y * Unit < y && (row == null || h.Y > row)) row = h.Y;
        return row;
    }

    /// <summary>Whether a body of <paramref name="s"/> with its feet flush on row <paramref name="y"/>'s top (thousandths) has an open hatch of that row under it.</summary>
    internal bool OnOpenHatch(Spec.Shape s, int x, int y)
    {
        if (y % Unit != 0) return false;
        int half = s.Width * Unit / 2, row = y / Unit;
        foreach (var h in _hatches)
            if (h.Y == row && h.X * Unit < x + half && (h.X + Doorways.Thick) * Unit > x - half && Doorways.IsOpen(_grid.StructureAt(row * _grid.Width + h.X)))
                return true;
        return false;
    }

    /// <summary>
    /// Whether cell (<paramref name="column"/>, <paramref name="row"/>) is in an open hatch narrower than a body of <paramref name="s"/>:
    /// floor to it, its feet on the floor either side (the escort's).
    /// </summary>
    internal bool Spanned(Spec.Shape s, int column, int row)
    {
        if (s.Width <= Doorways.Thick) return false;
        foreach (var h in _hatches)
            if (h.Y == row && column >= h.X && column < h.X + Doorways.Thick && Doorways.IsOpen(_grid.StructureAt(row * _grid.Width + h.X)))
                return true;
        return false;
    }

    /// <summary>
    /// The first shut doorway cell, row by row from the top-left, in the box of cells (<paramref name="x0"/>, <paramref name="y0"/>) to
    /// (<paramref name="x1"/>, <paramref name="y1"/>), the way in left out: what a guard pries open; null for none.
    /// </summary>
    internal int? ShutDoorway(int x0, int y0, int x1, int y1)
    {
        for (int y = Math.Max(0, y0); y <= Math.Min(_grid.Height - 1, y1); y++)
            for (int x = Math.Max(0, x0); x <= Math.Min(_grid.Width - 1, x1); x++)
                if (!_wayIn.Contains(x, y) && Doorways.IsClosed(_grid.StructureAt(y * _grid.Width + x))) return y * _grid.Width + x;
        return null;
    }

    /// <summary>Whether a body of <paramref name="s"/> at (<paramref name="x"/>, <paramref name="y"/>) is in nothing solid but a shut doorway: one shut on it (0006).</summary>
    internal bool InShutDoorway(Spec.Shape s, int x, int y)
    {
        int half = s.Width * Unit / 2;
        int left = Down(x - half, Unit), right = Down(x + half - 1, Unit), top = Down(y - s.Height * Unit, Unit), bottom = Down(y - 1, Unit);
        bool any = false;
        for (int row = top; row <= bottom; row++)
            for (int column = left; column <= right; column++)
            {
                if (!Solid(column, row)) continue;
                if (column < 0 || row < 0 || column >= _grid.Width || row >= _grid.Height
                    || !Doorways.IsClosed(_grid.StructureAt(row * _grid.Width + column))) return false;
                any = true;
            }
        return any;
    }

    /// <summary>Opens the doorway <paramref name="cell"/> is in: a guard's pry done.</summary>
    internal void Open(int cell) => _gridOps.SetDoorway(cell, true);
}
