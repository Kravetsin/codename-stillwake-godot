using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>The hulk laid back into a vessel's parts and its description as wrecked (vessel-wrecks).</summary>
internal sealed partial class Hulk
{
    /// <summary>
    /// The vessel as built after the chain: its parts in reading order, its description moved and cut as its cells were, and the wreck's
    /// record, each event's age and group from <paramref name="ages"/>, in chain order.
    /// </summary>
    public Built Laid(int entry, IReadOnlyList<(bool Old, int Group)> ages, bool scavenged, IReadOnlyList<WreckPatch> patches)
    {
        Settle();
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
    /// hull, set against one) and holding by fewer cells of it now is lost, again till all hold: nothing breakable floats.
    /// </summary>
    private void Settle()
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
                th.Gone = true;
            }
        }
    }
}
