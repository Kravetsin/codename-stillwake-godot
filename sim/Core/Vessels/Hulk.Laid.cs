using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>The hulk laid back into a vessel's parts and its description as wrecked (vessel-wrecks).</summary>
internal sealed partial class Hulk
{
    /// <summary>
    /// The vessel as built after the chain: its parts in reading order, its description moved and cut as its cells were, and the wreck's
    /// record, each event's age and group from <paramref name="ages"/>, in chain order.
    /// </summary>
    /// <param name="r">The draws of what is thrown: an old group keeps <c>Wrecks.ThrownOldPercent</c> of them.</param>
    public Built Laid(int entry, IReadOnlyList<(bool Old, int Group)> ages, bool scavenged, IReadOnlyList<WreckPatch> patches, Rng r)
    {
        Settle(r);
        LayThrown(r, ages);
        var parts = new List<VesselPart>();
        for (int i = 0; i < _cells.Length; i++)
        {
            int x = XOf(i), y = YOf(i);
            byte c = _cells[i];
            if (c is Air or Wall) parts.Add(new(x, y, "back", "backWall", Vessels.Hull));
            if (c is Wall or Glass) parts.Add(new(x, y, "structure", "wall", c == Glass ? Vessels.Glass : Vessels.Hull));
        }
        foreach (var t in _things)
            if (!t.Gone)
                parts.Add(t.Machine ? new(t.X, t.Y, "machine", t.Kind, Holds: t.Holds is { Count: > 0 } h ? h : null)
                    : new(t.X, t.Y, "structure", t.Kind, Open: t.Open));
        parts.Sort(Shipyard.InReadingOrder);

        var doors = new List<Vessel.Door>();
        int moved = -1;
        for (int d = 0; d < _v.Doors.Count; d++)
        {
            var pieces = _things.FindAll(t => !t.Gone && t.Door == d);
            if (pieces.Count != Vessels.Open / 2) continue;
            var top = pieces.MinBy(t => t.Y)!;
            if (d == entry) moved = doors.Count;
            doors.Add(new(top.X, top.Y, top.Open));
        }
        var shafts = new List<Vessel.Shaft>();
        for (int s = 0; s < _v.Shafts.Count; s++)
        {
            var was = _v.Shafts[s];
            var pieces = _things.FindAll(t => !t.Gone && t.Shaft == s);
            if (pieces.Count != (was.Bottom - was.Top) / 2 + 1) continue;
            var top = pieces.MinBy(t => t.Y)!;
            shafts.Add(new(top.X, top.Y, top.Y + was.Bottom - was.Top));
        }
        var hatches = _things.FindAll(t => !t.Gone && t.Hatch >= 0).OrderBy(t => t.Hatch).Select(t => new Vessel.Hatch(t.X, t.Y)).ToList();
        var compartments = new List<Vessel.Compartment>();
        for (int c = 0; c < _v.Compartments.Count; c++) compartments.Add(_rooms.Find(rm => rm.Index == c && rm.Of < 0)!.C);
        foreach (var rm in _rooms)
            if (rm.Of >= 0) compartments.Add(rm.C with { Of = rm.Of });
        static IReadOnlyList<(string, int)> Loot(Thing t) => t.Holds is { } h ? h : [];
        var vessel = _v with
        {
            Sections = _sections,
            Decks = [.. _bowDecks.Select(d => new Vessel.Deck(d))],
            Shafts = shafts,
            Doors = doors,
            Hatches = hatches,
            Compartments = compartments,
            Fittings = [.. _things.Where(t => !t.Gone && t.Machine && !t.Outside).Select(t => new Vessel.Fitting(t.Kind, t.X, t.Y, Loot(t)))],
            Outside = [.. _things.Where(t => !t.Gone && t.Machine && t.Outside).Select(t => new Vessel.Fitting(t.Kind, t.X, t.Y, Loot(t)))],
        };
        var events = new List<WreckEvent>();
        for (int k = 0; k < _records.Count; k++)
        {
            var e = _records[k];
            var carved = e.Carved.Where(c => c.X >= 0 && c.Y >= 0 && c.X < _w && c.Y < _h).Select(c => c.Y * _w + c.X).Distinct().Order().ToList();
            events.Add(new(e.Kind, ages[k].Old, ages[k].Group, e.X, e.Y, e.Radius, e.DirX, e.DirY, carved, [.. e.Compartments], e.Break,
                [.. e.Stones.Where(s => _things.Exists(t => !t.Gone && t.Kind == Vessels.Stone && t.X == s.X && t.Y == s.Y))], e.Cause)
            {
                Heats = [.. e.Heats],
            });
        }
        // The rooms the air left, as the chain and the scavengers leave them: by their floor's middle, as a fire finds them sealed.
        Vent();
        var vented = new List<int>();
        foreach (var rm in _rooms)
        {
            int x = rm.Left + rm.Width / 2, y = rm.Top + rm.Height - 1;
            if (In(x, y) && _vented[I(x, y)] && !vented.Contains(rm.Index)) vented.Add(rm.Index);
        }
        vented.Sort();
        return new Built(vessel, new VesselWreck(events, scavenged, patches) { Vented = vented }, parts) { Entry = moved };
    }

    /// <summary>
    /// Nothing left hanging (vessel-wrecks): a machine held as stamped along a side (standing on a floor, hanging from a ceiling or a
    /// hull, set against one) and holding by fewer cells of it now is thrown off, again till all hold; a lamp is only lost.
    /// </summary>
    private void Settle(Rng r)
    {
        var occ = Occupancy();
        for (bool moved = true; moved;)
        {
            moved = false;
            for (int t = 0; t < _things.Count; t++)
            {
                var th = _things[t];
                if (th.Gone || !th.Machine || th.Rest < 0 || Touching(th, t, th.Rest, occ) >= th.Touched) continue;
                Mark(occ, th, -1, t);
                moved = true;
                long mx = 10 * th.X + 5 * th.W, my = 10 * th.Y + 5 * th.H;
                int near = -1;
                long best = long.MaxValue;
                for (int k = 0; k < _records.Count; k++)
                {
                    long d = (10 * _records[k].X - mx) * (10 * _records[k].X - mx) + (10 * _records[k].Y - my) * (10 * _records[k].Y - my);
                    if (d < best) (best, near) = (d, k);
                }
                var (ux, uy) = near < 0 ? (0, -1000) : Unit(mx - 10 * _records[near].X, my - 10 * _records[near].Y);
                Throw(r, th, mx, my, ux, uy, 3 + r.Below(7));
                th.Event = Math.Max(0, near);
            }
        }
    }

    /// <summary>
    /// What the events threw out, laid in space (vessel-wrecks): each, an old group's only <c>Wrecks.ThrownOldPercent</c> of them, upright
    /// at the free spot nearest its aim within <c>Wrecks.ThrownSearch</c> cells, every cell of it space no part holds, clear of the
    /// arrival room's ring; lost where none is free. A machine of the vessel world then, broken by tools as any.
    /// </summary>
    private void LayThrown(Rng r, IReadOnlyList<(bool Old, int Group)> ages)
    {
        var occ = Occupancy();
        for (int t = 0; t < _things.Count; t++)
        {
            var th = _things[t];
            if (!th.Thrown || !th.Gone) continue;
            if (ages[th.Event].Old && r.Below(100) >= Wrecks.ThrownOldPercent) continue;
            int x0 = WreckMath.FloorDiv(th.Aim.X - 5 * th.W + 5, 10), y0 = WreckMath.FloorDiv(th.Aim.Y - 5 * th.H + 5, 10);
            (int X, int Y)? spot = null;
            for (int d = 0; d <= Wrecks.ThrownSearch && spot == null; d++)
            {
                (long D, int Y, int X)? best = null;
                for (int dy = -d; dy <= d; dy++)
                    for (int dx = -d; dx <= d; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != d || !Free(occ, x0 + dx, y0 + dy, th.W, th.H)) continue;
                        var at = ((long)dx * dx + (long)dy * dy, y0 + dy, x0 + dx);
                        if (best == null || at.CompareTo(best.Value) < 0) best = at;
                    }
                if (best is { } b) spot = (b.X, b.Y);
            }
            if (spot is not { } s) continue;
            (th.X, th.Y, th.Gone, th.Outside, th.Rest) = (s.X, s.Y, false, true, -1);
            Mark(occ, th, t, -1);
        }
    }

    /// <summary>
    /// Whether a thing <paramref name="w"/> by <paramref name="h"/> fits at (<paramref name="x"/>, <paramref name="y"/>) in space alone, a
    /// cell clear of the hull and every part round it: thrown off, it drifts, never seeming to hang where it stood.
    /// </summary>
    private bool Free(int[] occ, int x, int y, int w, int h)
    {
        if (ProtBox(x, y, x + w - 1, y + h - 1)) return false;
        for (int yy = y - 1; yy <= y + h; yy++)
            for (int xx = x - 1; xx <= x + w; xx++)
            {
                bool inside = xx >= x && xx < x + w && yy >= y && yy < y + h;
                if (!In(xx, yy))
                {
                    if (inside) return false;
                    continue;
                }
                if ((inside ? At(xx, yy) != Space : At(xx, yy) >= Wall) || occ[I(xx, yy)] >= 0) return false;
            }
        return true;
    }
}
