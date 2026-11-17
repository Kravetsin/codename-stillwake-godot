using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>The five events on the hulk (vessel-wrecks, the mockup's fates.js), what each may follow, and scavenging after.</summary>
internal sealed partial class Hulk
{
    private bool Big => _cls.Layout != VesselLayout.Rooms;

    /// <summary>
    /// The events that may come next after <paramref name="chain"/> (the plan's table), in <see cref="WreckKind"/>'s order: none past
    /// the class's most.
    /// </summary>
    public List<WreckKind> Allowed(IReadOnlyList<WreckKind> chain)
    {
        var ok = new List<WreckKind>();
        if (chain.Count >= _cls.Events.Max) return ok;
        int Count(WreckKind k) => chain.Count(c => c == k);
        if (Count(WreckKind.Explosion) == 0 && BlastSeats().Count > 0) ok.Add(WreckKind.Explosion);
        if (SealedRooms().Count > 0) ok.Add(WreckKind.Fire);
        if (Count(WreckKind.Icing) == 0 && (chain.Count == 0 || _last is { Frozen: false })) ok.Add(WreckKind.Icing);
        if (Count(WreckKind.Asteroid) < (Big ? Wrecks.RocksBig : Wrecks.Rocks) && Spots().Count > 0) ok.Add(WreckKind.Asteroid);
        if (_cls.Gap.Max > 0 && Count(WreckKind.Break) == 0 && BreakRibs().Count > 0) ok.Add(WreckKind.Break);
        return ok;
    }

    /// <summary>Applies <paramref name="kind"/> after <paramref name="chain"/>, drawing on its own stream <paramref name="r"/>.</summary>
    public void Apply(WreckKind kind, IReadOnlyList<WreckKind> chain, Rng r)
    {
        var rec = new Record { Kind = kind };
        _records.Add(rec);
        switch (kind)
        {
            case WreckKind.Explosion: Explode(r, rec); break;
            case WreckKind.Fire: Burn(r, rec, chain.Count == 0, chain.Count > 0 && chain[^1] == WreckKind.Explosion); break;
            case WreckKind.Icing: Ice(rec, chain.Count == 0); break;
            case WreckKind.Asteroid: Strike(r, rec); break;
            default: Break(r, rec); break;
        }
    }

    // ---------- explosion ----------

    /// <summary>
    /// Where an explosion may go up, centre doubled: the stern's first engine room's main engine (its room's middle without one); a
    /// tanker's two tank bays nearest the middle, at a tall tank (the bay's middle without one). None in the arrival room's ring.
    /// </summary>
    private List<(int Cx2, int Cy2, Thing? Seat)> BlastSeats()
    {
        var seats = new List<(int, int, Thing?)>();
        if (_cls.Layout == VesselLayout.Tanker)
        {
            bool Tall(Thing t) => !t.Gone && t.Machine && Vessels.FirstLook(t.Kind) is Vessels.TankTall or Vessels.FuelColumn3 or Vessels.FuelColumn4;
            var bays = _rooms.FindAll(b => b.C.Role == VesselRole.TankBay);
            var tall = bays.FindAll(b => _things.Exists(t => Tall(t) && t.X >= b.Left && t.X < b.Left + b.Width));
            if (tall.Count > 0) bays = tall;
            int mid2 = _bodyLeft + _bowLeft;
            bays.Sort((a, b) => Math.Abs(2 * a.Left - mid2) != Math.Abs(2 * b.Left - mid2)
                ? Math.Abs(2 * a.Left - mid2).CompareTo(Math.Abs(2 * b.Left - mid2)) : a.Index.CompareTo(b.Index));
            foreach (var bay in bays.Take(2))
            {
                var tank = _things.Find(t => Tall(t) && t.X >= bay.Left && t.X < bay.Left + bay.Width);
                seats.Add(tank != null ? (2 * tank.X + tank.W, 2 * tank.Y + tank.H, tank) : (2 * bay.Left + bay.Width, 2 * bay.Top + bay.Height, null));
            }
        }
        else
        {
            var eng = _rooms.Where(rm => rm.C.Role == VesselRole.Engine).OrderBy(rm => rm.Left).ThenBy(rm => rm.Index).FirstOrDefault();
            if (eng != null)
            {
                var engine = _things.Find(t => !t.Gone && t.Machine && Vessels.FirstLook(t.Kind) is "salvageMainEngine" or "salvageEngine" && eng.C.Holds(t.X + 1, t.Y + 1));
                seats.Add((engine != null ? 2 * engine.X + engine.W : 2 * eng.Left + eng.Width, 2 * eng.Top + eng.Height, null));
            }
        }
        return seats.FindAll(s => !Prot(s.Item1 / 2, s.Item2 / 2));
    }

    /// <summary>
    /// An explosion (the mockup's blast): a lobed void of the blast radius round its seat, doubled toward the nearer skin, the armour
    /// over it moved out a row, decks within 1.2 radii sunk a row, doors within 2.2 radii opened; a freighter's stern cut back at its
    /// first rib, ragged by up to two columns a band, everything aft of the cut lost. Then the loose things pulled.
    /// </summary>
    private void Explode(Rng r, Record rec)
    {
        var seats = BlastSeats();
        var (cx2, cy2, tank) = seats[(int)r.Below((uint)Math.Min(2, seats.Count))];
        int radius = Shipyard.Between(r, _cls.Blast);
        if (tank != null) tank.Gone = true;
        Breach? breach = null;
        (rec.DirX, rec.DirY) = (0, 0);
        var rib = _sections.Where(s => s.Kind == Vessel.SectionKind.Rib).OrderBy(s => s.Left).FirstOrDefault();
        if (_cls.Id == "freighter" && rib != null && Math.Abs(2 * rib.Left - cx2) < 80)
        {
            // The stern blown off at its seam, ragged band by band; what stood aft of the cut thrown clear.
            var bands = Bands();
            var cut = new int[_h];
            for (int b = 0; b + 1 < bands.Count; b++)
            {
                int j = (int)r.Below(5) - 1;
                for (int y = Math.Max(0, bands[b]); y < Math.Min(_h, bands[b + 1]); y++) cut[y] = rib.Left + j;
            }
            foreach (var t in _things)
                if (!t.Gone && t.Machine && !ProtThing(t) && 2 * t.X + t.W < 2 * cut[Math.Clamp(t.Y + t.H / 2, 0, _h - 1)]) t.Gone = true;
            var list = new List<(int, int)>();
            for (int y = 0; y < _h; y++)
            {
                int to = cut[y] + (r.Below(100) < 30 ? 1 : 0);
                for (int x = -Pad; x < to; x++) list.Add((x, y));
            }
            foreach (var t in Carve(list, rec)) t.Gone = true;
            cx2 = 2 * (rib.Left + 1);
            breach = new Breach { X = rib.Left, Y = cy2 / 2, DirX = -1, DirY = 0, Reach = 26 };
        }
        bool toTop = cy2 - 2 * _bodyTop < 2 * _bodyBottom - cy2;
        var lobes = new int[8];
        for (int k = 0; k < 8; k++) lobes[k] = 75 + (int)r.Below(51);
        var cells = new List<(int, int)>();
        // The box: 1.6 radii across, two up and down, rounded out.
        int x0 = (cx2 * 5 - 16 * radius) / 10 - 1, x1 = (cx2 * 5 + 16 * radius) / 10 + 1, y0 = cy2 / 2 - 2 * radius - 1, y1 = cy2 / 2 + 2 * radius + 1;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                long dx = 2 * x + 1 - cx2, dy = 2 * y + 1 - cy2;
                long rad = 2L * radius * lobes[Octant(dx, dy)] * (88 + r.Below(25));
                if ((toTop ? dy < 0 : dy > 0) && Math.Abs(dx) * 100 < 110L * radius) rad = rad * 19 / 10;
                // dy stretched by 1.15: 1.3225 in hundredths.
                if ((100 * dx * dx + 132 * dy * dy) * 1_000_000 < rad * rad) cells.Add((x, y));
            }
        foreach (var t in Carve(cells, rec)) t.Gone = true;
        int cy = cy2 / 2;
        ShiftArmour((cx2 * 5 - 18 * radius) / 10, (cx2 * 5 + 18 * radius + 9) / 10, cy, outward: true);
        BuckleDecks(cx2, 12 * radius, 1);
        foreach (var t in _things)
        {
            if (t.Gone || t.Door < 0 || t.Open) continue;
            long dx = 2 * t.X + 2 - cx2, dy = 2 * DoorTop(t) + Vessels.Open - cy2;
            if (100 * (dx * dx + dy * dy) < 44L * 44 * radius * radius && !DoorProt(t)) OpenDoor(t);
        }
        Vent();
        Pull(r, Wrecks.PullBlast);
        _last = breach ?? new Breach { X = cx2 / 2, Y = toTop ? _bodyTop : _bodyBottom, DirX = 0, DirY = toTop ? -1 : 1, Reach = radius * 3 + 8 };
        _blast = (cx2, cy2, radius);
        _damage = (WreckKind.Explosion, cx2);
        (rec.X, rec.Y, rec.Radius) = (cx2 / 2, cy2 / 2, radius);
        (rec.DirX, rec.DirY) = breach != null ? (-1, 0) : (0, toTop ? -1 : 1);
    }

    /// <summary>The row bands a cut runs in: over the body, each deck down to its floor, and under the last.</summary>
    private List<int> Bands() => [-Pad, _bodyTop, .. DecksAt(int.MinValue).Where(d => d < _h), _h + Pad];

    /// <summary>The top row of the door piece <paramref name="t"/> is part of.</summary>
    private int DoorTop(Thing t)
    {
        int top = t.Y;
        while (_things.Exists(o => !o.Gone && o.Door == t.Door && o.Y == top - 2)) top -= 2;
        return top;
    }

    private bool DoorProt(Thing piece) => _things.Exists(o => o.Door == piece.Door && !o.Gone && ProtThing(o));

    /// <summary>Every piece of the door <paramref name="piece"/> is part of opened.</summary>
    private void OpenDoor(Thing piece)
    {
        foreach (var o in _things)
            if (o.Door == piece.Door && !o.Gone) o.Open = true;
    }

    /// <summary>Which eighth of the circle offset (<paramref name="x"/>, <paramref name="y"/>) points into, from straight back round through up.</summary>
    private static int Octant(long x, long y)
    {
        long ax = Math.Abs(x), ay = Math.Abs(y);
        if (y < 0) return x < 0 ? (ay <= ax ? 0 : 1) : (ay > ax ? 2 : 3);
        return x >= 0 ? (ay <= ax ? 4 : 5) : (ay > ax ? 6 : 7);
    }

    // ---------- fire ----------

    /// <summary>
    /// A fire (the mockup's fire and burn): from its start compartments through cells holding air and open doors, never a shut door,
    /// a hatch or the arrival room's ring, a step a cost of 2 and up 1 (fire climbs), out to its reach. First in the chain it starts in
    /// the galley (a liner) or the widest life support (others), else quarters; right after an explosion in the one or two sealed
    /// compartments nearest the blast within three of its radii; else in the one nearest the latest event. Each compartment is burnt by
    /// its nearest reached cell; crates by the seat become piles of bits, every other crate in a burnt compartment loses
    /// <c>BurntLoss</c> of each stack.
    /// </summary>
    private void Burn(Rng r, Record rec, bool first, bool afterBlast)
    {
        var sealedRooms = SealedRooms();
        var seeds = new List<Room>();
        long Dist(int ax2, int ay2, int bx2, int by2) => (long)(ax2 - bx2) * (ax2 - bx2) + (long)(ay2 - by2) * (ay2 - by2);
        if (afterBlast && _blast is { } b)
        {
            var near = sealedRooms.FindAll(rm =>
                Dist(2 * rm.Left + rm.Width, 2 * rm.Top + rm.Height, b.X2, b.Y2) < (6L * b.R + rm.Width) * (6L * b.R + rm.Width));
            near.Sort((p, q) => Dist(2 * p.Left + p.Width, 2 * p.Top, b.X2, b.Y2).CompareTo(Dist(2 * q.Left + q.Width, 2 * q.Top, b.X2, b.Y2)) is int c && c != 0 ? c : p.Index.CompareTo(q.Index));
            seeds.AddRange(near.Take(2));
            if (seeds.Count == 0) seeds.Add(sealedRooms.OrderBy(rm => Dist(2 * rm.Left, 2 * rm.Top, b.X2, b.Y2)).ThenBy(rm => rm.Index).First());
        }
        else if (first)
        {
            var role = _cls.Layout == VesselLayout.Liner ? VesselRole.Galley : VesselRole.Life;
            var pref = sealedRooms.Where(rm => rm.C.Role == role).OrderByDescending(rm => rm.Width).ThenBy(rm => rm.Index).FirstOrDefault()
                ?? sealedRooms.Find(rm => rm.C.Role == VesselRole.Quarters);
            seeds.Add(pref ?? sealedRooms[(int)r.Below((uint)sealedRooms.Count)]);
        }
        else
        {
            var at = _records[^2];
            seeds.Add(sealedRooms.OrderBy(rm => Dist(2 * rm.Left + rm.Width, 2 * rm.Top + rm.Height, 2 * at.X, 2 * at.Y)).ThenBy(rm => rm.Index).First());
        }
        var origin = seeds[0];
        // A start with no open door gets its first, in reading order, opened: the fire needs air.
        var doors = _things.FindAll(t => !t.Gone && t.Door >= 0 && (t.X == origin.Left - 2 || t.X == origin.Left + origin.Width) && t.Y >= origin.Top - 1 && t.Y < origin.Floor);
        if (doors.Count > 0 && !doors.Exists(t => t.Open))
        {
            doors.Sort((p, q) => p.Y != q.Y ? p.Y.CompareTo(q.Y) : p.X.CompareTo(q.X));
            if (doors.Find(t => !DoorProt(t)) is { } opened) OpenDoor(opened);
        }
        var stove = _things.Find(t => !t.Gone && t.Machine && Vessels.FirstLook(t.Kind) is Vessels.Stove or Vessels.LifeTank or "salvageTanks" && origin.C.Holds(t.X, t.Y));
        int seat2 = stove != null ? 2 * stove.X + stove.W : 2 * origin.Left + origin.Width;
        int reach = 2 * _cls.FireReach;
        var dist = Flood(seeds, reach);
        var burnt = new List<(Room Room, int Heat)>();
        foreach (var rm in _rooms)
        {
            if (rm.Burnt || rm.Index == _arrival) continue;
            int best = int.MaxValue;
            for (int y = rm.Top; y < rm.Floor; y++)
                for (int x = rm.Left; x < rm.Left + rm.Width; x++)
                    if (In(x, y) && dist[I(x, y)] >= 0) best = Math.Min(best, dist[I(x, y)]);
            int heat = seeds.Contains(rm) ? 100 : best == int.MaxValue ? 0 : Math.Max(0, 100 * (reach - best) / reach);
            if (heat > 6) burnt.Add((rm, heat));
        }
        foreach (var (rm, _) in burnt)
        {
            rm.Burnt = true;
            if (!rec.Compartments.Contains(rm.Index)) rec.Compartments.Add(rm.Index);
        }
        rec.Compartments.Sort();
        foreach (int c in rec.Compartments) rec.Heats.Add(burnt.Where(b => b.Room.Index == c).Max(b => b.Heat));
        foreach (var t in _things)
        {
            if (t.Gone || !t.Machine || t.Outside) continue;
            int bx = t.X + t.W / 2, by = t.Y + t.H - 1;
            var room = burnt.Find(b => b.Room.C.Holds(bx, by)).Room;
            if (room == null) continue;
            if (room == origin && Vessels.FirstLook(t.Kind) is "crate" or "salvageCrate" && Math.Abs(2 * t.X + t.W - seat2) <= 2 * Wrecks.SeatReach && !ProtThing(t))
            {
                var bits = Machines.Get(Wrecks.Bits);
                (t.Kind, t.Y, t.W, t.H, t.Holds) = (Wrecks.Bits, t.Y + t.H - bits.Height, bits.Width, bits.Height, null);
            }
            else if (t.Holds != null)
                for (int k = t.Holds.Count - 1; k >= 0; k--)
                {
                    var (item, count) = t.Holds[k];
                    int left = count - count * Wrecks.BurntLoss / 100;
                    if (left > 0) t.Holds[k] = (item, left);
                    else t.Holds.RemoveAt(k);
                }
        }
        (rec.X, rec.Y, rec.Radius, rec.DirX, rec.DirY) = (seat2 / 2, origin.Floor - 1, _cls.FireReach, 0, -1);
    }

    /// <summary>The fire's reach from <paramref name="seeds"/> in half steps, -1 where it never came: a step 2, a step up 1.</summary>
    private int[] Flood(List<Room> seeds, int reach)
    {
        int n = _gw * _gh;
        var blocked = new bool[n];
        foreach (var t in _things)
            if (!t.Gone && (t.Kind == "door" && !t.Open || t.Kind == Builds.Hatch))
                for (int y = t.Y; y < t.Y + t.H; y++)
                    for (int x = t.X; x < t.X + t.W; x++)
                        if (In(x, y)) blocked[I(x, y)] = true;
        bool Open(int x, int y) => In(x, y) && _cells[I(x, y)] == Air && !blocked[I(x, y)] && !_vented[I(x, y)] && !Prot(x, y);
        var dist = new int[n];
        Array.Fill(dist, -1);
        var buckets = new List<int>[reach + 3];
        for (int k = 0; k < buckets.Length; k++) buckets[k] = [];
        foreach (var rm in seeds)
            for (int y = rm.Top; y < rm.Floor; y++)
                for (int x = rm.Left; x < rm.Left + rm.Width; x++)
                    if (Open(x, y) && dist[I(x, y)] < 0)
                    {
                        dist[I(x, y)] = 0;
                        buckets[0].Add(I(x, y));
                    }
        for (int d = 0; d <= reach; d++)
            for (int h = 0; h < buckets[d].Count; h++)
            {
                int i = buckets[d][h];
                if (dist[i] != d) continue;
                int x = XOf(i), y = YOf(i);
                foreach (var (dx, dy) in N4)
                {
                    if (!Open(x + dx, y + dy)) continue;
                    int j = I(x + dx, y + dy), nd = d + (dy < 0 ? 1 : 2);
                    if (dist[j] >= 0 && dist[j] <= nd) continue;
                    dist[j] = nd;
                    buckets[nd].Add(j);
                }
            }
        return dist;
    }

    // ---------- icing ----------

    /// <summary>
    /// Icing (the mockup's ice): first in the chain the power lost and every compartment frozen; after a breach, its latest one
    /// frozen and the compartments within its cold the air left. Looks only: the hulk's cells and things stay.
    /// </summary>
    private void Ice(Record rec, bool first)
    {
        if (first)
        {
            foreach (var rm in _rooms)
                if (rm.Index != _arrival && !rec.Compartments.Contains(rm.Index)) rec.Compartments.Add(rm.Index);
            rec.Compartments.Sort();
            (rec.X, rec.Y) = (_w / 2, _h / 2);
            return;
        }
        var b = _last!;
        Vent();
        rec.Compartments.AddRange(Frozen(b).Distinct().Order());
        b.Frozen = true;
        (rec.X, rec.Y, rec.Radius, rec.DirX, rec.DirY) = (b.X, b.Y, b.Reach, b.DirX, b.DirY);
    }

    // ---------- asteroid ----------

    /// <summary>
    /// Where an asteroid may strike: a column over a module's top (the hull's top row no lower than four under the body's along its
    /// crater), three crater radii from an earlier strike, its crater's box clear of the arrival room's ring: its widest lobe across
    /// (1.35 radii), its scan deep.
    /// </summary>
    private List<int> Spots()
    {
        int R = _cls.Rock, span = _bowLeft - _bodyLeft - 2 * R - 10, from = _bodyLeft + R + 5;
        var spots = new List<int>();
        for (int x = from + span * 15 / 100; x <= from + span * 85 / 100; x++)
        {
            bool ok = true;
            for (int dx = -R; dx <= R && ok; dx += 2) ok = TopRow(x + dx) <= _bodyTop + 4;
            foreach (int p in _rocks) ok &= Math.Abs(p - x) >= 3 * R;
            int top = TopRow(x);
            if (ok && !ProtBox(x - (135 * R + 99) / 100 - 1, top - 2, x + (135 * R + 99) / 100 + 1, top + (22 * R + 9) / 10 + 1)) spots.Add(x);
        }
        return spots;
    }

    /// <summary>
    /// An asteroid (the mockup's rock): a lobed crater from the top, a channel narrowing on along its direction, ribs at the skin kept
    /// across it as the frame; the armour round it pushed in a row, decks under it sunk up to two rows; one to three shower holes
    /// downrange; stones on the crater's floor and in the first hole; the loose things pulled; its own breach frozen.
    /// </summary>
    private void Strike(Rng r, Record rec)
    {
        int R = _cls.Rock;
        var spots = Spots();
        int cx = spots[(int)r.Below((uint)spots.Count)];
        int side = r.Below(2) == 0 ? 1 : -1;
        var dir = Shipyard.Directions[1 + (int)r.Below(2)];
        long ux = side * dir.Ux, uy = dir.Uy;
        int top = TopRow(cx);
        int ccx2 = 2 * cx + (int)(6 * ux * R / 10_000), ccy2 = 2 * top + R;
        var lobes = new int[8];
        for (int k = 0; k < 8; k++) lobes[k] = 78 + (int)r.Below(43);
        var cells = new List<(int, int)>();
        int x0 = cx - (18 * R + 9) / 10, x1 = cx + (18 * R + 9) / 10, y1 = top + (22 * R + 9) / 10;
        for (int y = top - 2; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                long dx = 2 * x + 1 - ccx2, dy = 2 * y + 1 - ccy2;
                long rad = 2L * R * lobes[Octant(dx, dy)] * (88 + r.Below(25));
                // dy shrunk by 1.25: 0.64 in hundredths.
                bool hit = (100 * dx * dx + 64 * dy * dy) * 1_000_000 < rad * rad;
                long t = dx * ux + dy * uy, p = Math.Abs(-dx * uy + dy * ux);
                if (t > 0 && t < 2600L * R && 36 * p < 11 * (3600L * R - t)) hit = true;
                if (hit) cells.Add((x, y));
            }
        var keep = cells.FindAll(c => !(At(c.Item1, c.Item2) == Wall && _skin[I(c.Item1, c.Item2)] && RibAt(c.Item1, c.Item2)));
        foreach (var t in Carve(keep, rec)) t.Gone = true;
        ShiftArmour(cx - R - 6, cx + R + 6, ccy2 / 2, outward: false, x => 10 * Math.Abs(x - cx) > 8 * R);
        BuckleDecks(ccx2, 9 * R, 2);
        Lodge(rec, (ccx2 + (int)(12 * ux * R / 10_000)) / 2, ccy2 / 2);
        Lodge(rec, (ccx2 - 8 * R / 10) / 2, ccy2 / 2);
        for (int k = 0, n = 1 + (int)r.Below(3); k < n; k++)
        {
            int sx = cx + side * (R + 5 + (int)r.Below((uint)(26 * R / 10 + 1)));
            int rr = 1 + (int)r.Below(2);
            if (sx < _bodyLeft + 3 || sx > _bowLeft - 3) continue;
            int st = TopRow(sx);
            if (st > _bodyTop + 4) continue;
            var hole = new List<(int, int)>();
            for (int y = st - 1; y <= st + 16 * rr / 10; y++)
                for (int x = sx - rr - 1; x <= sx + rr + 1; x++)
                {
                    long dx = 2 * x + 1 - 2 * sx, dy = 2 * y + 1 - 2 * st, rad = 2L * rr * (85 + r.Below(31));
                    // dy shrunk by 1.3: 169 against 100.
                    if ((169 * dx * dx + 100 * dy * dy) * 10_000 < 169 * rad * rad) hole.Add((x, y));
                }
            foreach (var t in Carve(hole, rec)) t.Gone = true;
            if (k == 0) Lodge(rec, sx, st);
        }
        Vent();
        Pull(r, Big ? Wrecks.PullRockBig : Wrecks.PullRock);
        _last = new Breach { X = ccx2 / 2, Y = top, DirX = 0, DirY = -1, Reach = 4 * R + 10 };
        // A strike always freezes what leaked out of it (the user's pick: a strike alone reads dull).
        rec.Compartments.AddRange(Frozen(_last).Distinct().Order());
        _last.Frozen = true;
        _rocks.Add(cx);
        _damage = (WreckKind.Asteroid, ccx2);
        (rec.X, rec.Y, rec.Radius, rec.DirX, rec.DirY) = (ccx2 / 2, top, R, side * dir.X, dir.Y);
    }

    /// <summary>A stone dropped down column <paramref name="x"/> (its middle) from row <paramref name="y"/> to the first hull under it, where it fits.</summary>
    private void Lodge(Record rec, int x, int y)
    {
        var spec = Machines.Get(Vessels.Stone);
        int left = x - spec.Width / 2;
        var occ = Occupancy();
        bool Free(int xx, int yy) => In(xx, yy) && At(xx, yy) is Space or Air && occ[I(xx, yy)] < 0 && !Prot(xx, yy);
        for (int yy = y; yy < _h + Pad - spec.Height; yy++)
        {
            bool fits = true;
            for (int dy = 0; dy < spec.Height && fits; dy++)
                for (int dx = 0; dx < spec.Width && fits; dx++) fits = Free(left + dx, yy + dy);
            if (!fits) return;
            for (int dx = 0; dx < spec.Width; dx++)
                if (At(left + dx, yy + spec.Height) >= Wall)
                {
                    _things.Add(new Thing { Layer = "machine", Kind = Vessels.Stone, X = left, Y = yy, W = spec.Width, H = spec.Height });
                    rec.Stones.Add((left, yy));
                    return;
                }
        }
    }

    // ---------- break ----------

    /// <summary>
    /// The ribs a break may cut at: <c>BreakFromEnds</c> columns from the stern and the bow, the cut's widest ragged reach, its torn faces
    /// and what lands across the gap clear of the arrival room's ring.
    /// </summary>
    private List<Vessel.Section> BreakRibs()
    {
        int clear = 12;
        return _sections.FindAll(s => s.Kind == Vessel.SectionKind.Rib && s.Left > _bodyLeft + Wrecks.BreakFromEnds && s.Left < _bowLeft - Wrecks.BreakFromEnds
            && (s.Left + 1 + clear < _prot.X0 || s.Left + 1 - clear > _prot.X1));
    }

    /// <summary>
    /// A break (the mockup's break): cut at the rib nearest the latest explosion or asteroid, or (overload) one of the two nearest the
    /// middle; each band's cut ragged by -3..3 columns and a deck's floor by 2..4 more; the stern half moved half the gap sternward and
    /// its rows, the bow the rest bowward and its rows; a thing across the cut lost, a compartment cut in two halves; the faces torn
    /// 0-2 cells deeper a row; deck plates at the faces bent down a row; the loose things pulled.
    /// </summary>
    private void Break(Rng r, Record rec)
    {
        var ribs = BreakRibs();
        int target2 = _damage is { } dmg ? dmg.X2 : _bodyLeft + _bowLeft;
        ribs.Sort((a, b) => Math.Abs(2 * a.Left - target2) != Math.Abs(2 * b.Left - target2)
            ? Math.Abs(2 * a.Left - target2).CompareTo(Math.Abs(2 * b.Left - target2)) : a.Left.CompareTo(b.Left));
        var rib = _damage != null ? ribs[0] : ribs[(int)r.Below((uint)Math.Min(2, ribs.Count))];
        int gap = Shipyard.Between(r, _cls.Gap), gs = gap / 2, gb = gap - gs;
        int dyB = (r.Below(2) == 0 ? -1 : 1) * Shipyard.Between(r, Wrecks.BowRows);
        int dyS = r.Below(100) < Wrecks.SternBackPercent ? -Math.Sign(dyB) : 0;
        int c0 = rib.Left + 1;
        var bands = Bands();
        var cut = new int[_gh];
        for (int b = 0; b + 1 < bands.Count; b++)
        {
            int j = (int)r.Below(7) - 3;
            for (int y = bands[b]; y < bands[b + 1]; y++) cut[y + Pad] = c0 + j;
        }
        foreach (int d in _bowDecks)
            if (d < _h) cut[d + Pad] += (r.Below(2) == 0 ? 1 : -1) * (2 + (int)r.Below(3));
        int CutAt(int y) => cut[Math.Clamp(y + Pad, 0, _gh - 1)];
        bool Stern(int x, int y) => x < CutAt(y);
        (int X, int Y) Mv(int x, int y, bool stern) => (x + (stern ? -gs : gb), y + (stern ? dyS : dyB));
        (int X, int Y) Mp(int x, int y) => Mv(x, y, Stern(x, y));
        // Everything laid down before moves with its half (the mockup's remapAll).
        foreach (var e in _records)
        {
            if (e == rec) continue;
            (e.X, e.Y) = Mp(e.X, e.Y);
            for (int k = 0; k < e.Carved.Count; k++) e.Carved[k] = Mp(e.Carved[k].X, e.Carved[k].Y);
            for (int k = 0; k < e.Stones.Count; k++) e.Stones[k] = Mp(e.Stones[k].X, e.Stones[k].Y);
        }
        for (int k = 0; k < _rocks.Count; k++) _rocks[k] = Mp(_rocks[k], _bodyTop).X;
        if (_last != null) (_last.X, _last.Y) = Mp(_last.X, _last.Y);
        if (_blast is { } bl) _blast = (2 * Mp(bl.X2 / 2, bl.Y2 / 2).X + bl.X2 % 2, 2 * Mp(bl.X2 / 2, bl.Y2 / 2).Y + bl.Y2 % 2, bl.R);
        bool protStern = Stern(_prot.X0, _prot.Y0);
        var (px0, py0) = Mv(_prot.X0, _prot.Y0, protStern);
        _prot = (px0, py0, px0 + _prot.X1 - _prot.X0, py0 + _prot.Y1 - _prot.Y0);

        int n = _gw * _gh;
        var cells = new byte[n];
        var orig = new byte[n];
        var skin = new bool[n];
        var carved = new bool[n];
        var taken = new bool[n];
        for (int i = 0; i < n; i++)
        {
            if (_cells[i] == Space && _orig[i] == Space && !_carved[i]) continue;
            var (nx, ny) = Mp(XOf(i), YOf(i));
            if (!In(nx, ny)) continue;
            int j = I(nx, ny);
            if (taken[j]) continue;
            taken[j] = true;
            (cells[j], orig[j], skin[j], carved[j]) = (_cells[i], _orig[i], _skin[i], _carved[i]);
        }
        foreach (var t in _things)
        {
            if (t.Gone) continue;
            bool s0 = Stern(t.X, t.Y), across = false;
            for (int y = t.Y; y < t.Y + t.H && !across; y++)
                for (int x = t.X; x < t.X + t.W && !across; x++) across = Stern(x, y) != s0;
            if (across)
            {
                t.Gone = true;
                continue;
            }
            (t.X, t.Y) = Mv(t.X, t.Y, s0);
        }
        var rooms = new List<Room>();
        foreach (var rm in _rooms)
        {
            int c = CutAt(rm.Top + rm.Height / 2);
            if (rm.Left < c)
            {
                var (nx, ny) = Mv(rm.Left, rm.Top, true);
                rooms.Add(new Room { C = rm.C with { Left = nx, Top = ny, Width = Math.Min(rm.Width, c - rm.Left) }, Index = rm.Index, Of = rm.Of, Burnt = rm.Burnt });
            }
            if (rm.Left + rm.Width > c)
            {
                int left = Math.Max(rm.Left, c);
                var (nx, ny) = Mv(left, rm.Top, false);
                rooms.Add(new Room { C = rm.C with { Left = nx, Top = ny, Width = rm.Left + rm.Width - left }, Index = rm.Index, Of = rm.Left < c ? rm.Index : rm.Of, Burnt = rm.Burnt });
            }
        }
        _rooms = rooms;
        var sections = new List<Vessel.Section>();
        foreach (var s in _sections)
        {
            Vessel.Section Moved(Vessel.Section part, bool stern) => part with { Left = part.Left + (stern ? -gs : gb), Top = part.Top + (stern ? dyS : dyB), Bottom = part.Bottom + (stern ? dyS : dyB) };
            if (s.Kind != Vessel.SectionKind.Bow && s.Left < c0 && s.Left + s.Width > c0)
            {
                sections.Add(Moved(s with { Width = c0 - s.Left }, true));
                sections.Add(Moved(s with { Left = c0, Width = s.Left + s.Width - c0 }, false));
            }
            else sections.Add(Moved(s, s.Left + s.Width / 2 < c0));
        }
        _sections = sections;
        var decks0 = _bowDecks;
        _sternDecks = [.. decks0.Select(d => d + dyS)];
        _bowDecks = [.. decks0.Select(d => d + dyB)];
        _split = c0;
        (_cells, _orig, _skin, _carved) = (cells, orig, skin, carved);

        int FaceX(int y, bool stern) => Mv(CutAt(y - (stern ? dyS : dyB)), 0, stern).X;
        var torn = new List<(int, int)>();
        for (int y = -Pad; y < _h + Pad; y++)
            foreach (bool stern in (ReadOnlySpan<bool>)[true, false])
            {
                int fx = FaceX(y, stern), depth = r.Below(2) == 0 ? (int)r.Below(3) : 0;
                for (int k = 0; k < depth; k++) torn.Add((stern ? fx - 1 - k : fx + k, y));
            }
        foreach (var t in Carve(torn, rec)) t.Gone = true;
        int gcx2 = 2 * c0 - gs + gb;
        var occ = Occupancy();
        foreach (bool stern in (ReadOnlySpan<bool>)[true, false])
        {
            int dy = stern ? dyS : dyB;
            for (int k = 0; k + 1 < decks0.Length; k++)
            {
                int yd = decks0[k] + dy, x = stern ? FaceX(yd, true) - 1 : FaceX(yd, false);
                while (In(x, yd) && At(x, yd) == Space && Math.Abs(2 * x - gcx2) < 2 * gap) x += stern ? -1 : 1;
                for (int j = 0; j < 2; j++)
                {
                    int xx = x + (stern ? -j : j);
                    if (At(xx, yd) != Wall || At(xx, yd + 1) != Air || At(xx, yd + 2) != Air || occ[I(xx, yd + 1)] >= 0 || occ[I(xx, yd)] >= 0
                        || StandsOn(occ, xx, yd) || Prot(xx, yd) || Prot(xx, yd + 1)) continue;
                    _cells[I(xx, yd + 1)] = Wall;
                    _cells[I(xx, yd)] = Air;
                }
            }
        }
        Vent();
        Pull(r, Wrecks.PullBreak);
        int gy0 = _bodyTop + Math.Min(dyS, dyB), gy1 = _bodyBottom + Math.Max(dyS, dyB);
        _last = new Breach { X = gcx2 / 2, Y = gy0, DirX = 0, DirY = -1, Reach = 30 };
        rec.Break = new WreckBreak(c0, gap, dyS, dyB);
        rec.Cause = _damage?.Kind;
        (rec.X, rec.Y, rec.Radius, rec.DirX, rec.DirY) = (gcx2 / 2, (gy0 + gy1) / 2, gap / 2, 1, 0);
    }

    // ---------- scavengers ----------

    /// <summary>
    /// Scavengers after an old first group (the mockup's strip): the skin round 2–4 ribs taken, the ribs kept; up to two of the
    /// fate's holes patched, hull set back over them. Engines and every room's things stay.
    /// </summary>
    public List<WreckPatch> Strip(Rng r)
    {
        var holes = new List<int>();
        for (int i = 0; i < _cells.Length; i++)
            if (_carved[i] && _orig[i] == Wall && _skin[i]) holes.Add(i);
        var ribs = _sections.FindAll(s => s.Kind == Vessel.SectionKind.Rib);
        for (int k = 0, n = Math.Min(ribs.Count, 2 + (int)r.Below(3)); k < n; k++)
        {
            int at = (int)r.Below((uint)ribs.Count);
            var rib = ribs[at];
            ribs.RemoveAt(at);
            int x0 = rib.Left - 3 - (int)r.Below(3), x1 = rib.Left + rib.Width + 2 + (int)r.Below(3);
            bool topSide = r.Below(100) < 60;
            var list = new List<(int, int)>();
            for (int x = x0; x < x1; x++)
            {
                int y = topSide ? -Pad : _h + Pad - 1;
                while (In(x, y) && At(x, y) == Space) y += topSide ? 1 : -1;
                for (int j = 0; j < 2; j++)
                {
                    int yy = y + (topSide ? j : -j);
                    if (At(x, yy) == Wall && _skin[I(x, yy)]) list.Add((x, yy));
                }
            }
            Carve(list, null, keepRibs: true);
        }
        holes = holes.FindAll(i => _cells[i] == Space);
        var patches = new List<WreckPatch>();
        var occ = Occupancy();
        for (int k = 0; k < 2 && holes.Count > 0; k++)
        {
            int i = holes[(int)r.Below((uint)holes.Count)], x = XOf(i), y = YOf(i);
            var set = new List<(int X, int Y)>();
            for (int dx = -1; dx <= 2; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int xx = x + dx, yy = y + dy;
                    if (In(xx, yy) && At(xx, yy) == Space && _orig[I(xx, yy)] == Wall && _skin[I(xx, yy)] && occ[I(xx, yy)] < 0 && !Prot(xx, yy)) set.Add((xx, yy));
                }
            if (set.Count < 2) continue;
            foreach (var (px, py) in set) _cells[I(px, py)] = Wall;
            patches.Add(new(set.Min(c => c.X), set.Min(c => c.Y), set.Max(c => c.X) - set.Min(c => c.X) + 1, set.Max(c => c.Y) - set.Min(c => c.Y) + 1));
        }
        return patches;
    }
}
