using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>
/// A stamped vessel as cells and things while its wreck's events change it (vessel-wrecks, the mockup's model): each cell space,
/// open (a back wall only), hull or glass; every structure but a wall and every machine a thing on it; its compartments as rooms.
/// Coordinates are the frame's, with room round it for a break's halves. Laid back into parts once the chain is done.
/// </summary>
internal sealed partial class Hulk
{
    public const byte Space = 0, Air = 1, Wall = 2, Glass = 3;

    /// <summary>Cells kept round the frame on every side: a break moves a half at most half its gap and a few rows.</summary>
    private const int Pad = 32;

    private readonly VesselClass _cls;
    private readonly Vessel _v;
    private readonly int _w, _h, _gw, _gh;
    private byte[] _cells, _orig;
    private bool[] _skin, _carved, _vented;
    private readonly List<Thing> _things = [];
    private List<Room> _rooms = [];
    private List<Vessel.Section> _sections;
    private int[] _sternDecks, _bowDecks;
    // Columns before it use the stern's decks once a break split them.
    private int _split = int.MaxValue;
    private (int X0, int Y0, int X1, int Y1) _prot;
    private readonly int _arrival, _bodyLeft, _bodyTop, _bodyBottom, _bowLeft;
    private Breach? _last;
    private (WreckKind Kind, int X2)? _damage;
    private (int X2, int Y2, int R)? _blast;
    private readonly List<int> _rocks = [];
    private readonly List<Record> _records = [];

    /// <summary>A structure but a wall, or a machine, standing on the hulk; <c>Door</c>, <c>Shaft</c>, <c>Hatch</c> its vessel's by index.</summary>
    private sealed class Thing
    {
        public required string Layer, Kind;
        public int X, Y, W, H;
        public bool Open, Outside, Gone;
        public List<(string Item, int Count)>? Holds;
        public int Door = -1, Shaft = -1, Hatch = -1;
        public bool Machine => Layer == "machine";
    }

    /// <summary>A compartment as the events find it: by its vessel's index (a split one's two halves share it), burnt once a fire reached it.</summary>
    private sealed class Room
    {
        public required Vessel.Compartment C;
        public int Index, Of = -1;
        public bool Burnt;
        public int Left => C.Left;
        public int Top => C.Top;
        public int Width => C.Width;
        public int Height => C.Height;
        public int Floor => C.Floor;
    }

    /// <summary>A breach: where it opens, its direction out, how far its cold reaches, and whether icing lay on it.</summary>
    private sealed class Breach
    {
        public int X, Y, DirX, DirY, Reach;
        public bool Frozen;
    }

    /// <summary>An event's record while the chain runs: what <see cref="WreckEvent"/> is made of.</summary>
    private sealed class Record
    {
        public WreckKind Kind;
        public int X, Y, Radius, DirX, DirY;
        public readonly List<(int X, int Y)> Carved = [], Stones = [];
        public readonly List<int> Compartments = [];
        public WreckBreak? Break;
        public WreckKind? Cause;
    }

    /// <param name="entry">The intact vessel's way in, by index in its doors: the arrival room is the compartment just inside it.</param>
    public Hulk(VesselClass cls, Vessel v, IReadOnlyList<VesselPart> parts, int entry)
    {
        _cls = cls;
        _v = v;
        (_w, _h) = (v.Width, v.Height);
        (_gw, _gh) = (_w + 2 * Pad, _h + 2 * Pad);
        int n = _gw * _gh;
        _cells = new byte[n];
        _skin = new bool[n];
        _carved = new bool[n];
        _vented = new bool[n];
        var outside = new HashSet<(string, int, int)>();
        foreach (var f in v.Outside) outside.Add((f.Kind, f.X, f.Y));
        var doorAt = new Dictionary<(int, int), int>();
        for (int d = 0; d < v.Doors.Count; d++)
            for (int y = v.Doors[d].Y; y < v.Doors[d].Y + Vessels.Open; y += 2) doorAt[(v.Doors[d].X, y)] = d;
        var shaftAt = new Dictionary<(int, int), int>();
        for (int s = 0; s < v.Shafts.Count; s++)
            for (int y = v.Shafts[s].Top; y <= v.Shafts[s].Bottom; y += 2) shaftAt[(v.Shafts[s].Column, y)] = s;
        var hatchAt = new Dictionary<(int, int), int>();
        for (int h = 0; h < v.Hatches.Count; h++) hatchAt[(v.Hatches[h].X, v.Hatches[h].Y)] = h;
        foreach (var p in parts)
        {
            int i = I(p.X, p.Y);
            if (p.Layer == "back") _cells[i] = _cells[i] == Space ? Air : _cells[i];
            else if (p.Kind == "wall") _cells[i] = p.Material == Vessels.Glass ? Glass : Wall;
            else
            {
                bool machine = p.Layer == "machine";
                var (w, h) = machine ? (Machines.Get(p.Kind).Width, Machines.Get(p.Kind).Height) : Builds.PieceOf(p.Kind) ?? (1, 1);
                _things.Add(new Thing
                {
                    Layer = p.Layer,
                    Kind = p.Kind,
                    X = p.X,
                    Y = p.Y,
                    W = w,
                    H = h,
                    Open = p.Open,
                    Holds = p.Holds?.ToList(),
                    Outside = machine && outside.Contains((p.Kind, p.X, p.Y)),
                    Door = !machine && p.Kind == "door" && doorAt.TryGetValue((p.X, p.Y), out int d) ? d : -1,
                    Shaft = !machine && p.Kind == "ladder" && shaftAt.TryGetValue((p.X, p.Y), out int s) ? s : -1,
                    Hatch = !machine && p.Kind == Builds.Hatch && hatchAt.TryGetValue((p.X, p.Y), out int k) ? k : -1,
                });
            }
        }
        _orig = (byte[])_cells.Clone();
        // A hull cell within two cells of space, as stamped.
        for (int y = 0; y < _h; y++)
            for (int x = 0; x < _w; x++)
            {
                if (At(x, y) < Wall) continue;
                for (int dy = -2; dy <= 2 && !_skin[I(x, y)]; dy++)
                    for (int dx = -2; dx <= 2; dx++)
                        if (Math.Abs(dx) + Math.Abs(dy) <= 2 && At(x + dx, y + dy) == Space)
                        {
                            _skin[I(x, y)] = true;
                            break;
                        }
            }
        for (int c = 0; c < v.Compartments.Count; c++) _rooms.Add(new Room { C = v.Compartments[c], Index = c });
        _sections = [.. v.Sections];
        _sternDecks = _bowDecks = [.. v.Decks.Select(d => d.Floor)];
        var body = v.Sections.First(s => s.Kind == Vessel.SectionKind.Body);
        (_bodyLeft, _bodyTop, _bodyBottom) = (body.Left, body.Top, body.Bottom);
        _bowLeft = v.Sections.First(s => s.Kind == Vessel.SectionKind.Bow).Left;
        var door = v.Doors[entry];
        int ax = door.X + 2, ay = door.Y + Vessels.Open - 1;
        _arrival = _rooms.FindIndex(r => r.C.Holds(ax, ay));
        var room = _rooms[_arrival];
        int ring = Wrecks.ProtectRing;
        _prot = (room.Left - ring, room.Top - ring, room.Left + room.Width - 1 + ring, room.Floor + ring);
    }

    private int I(int x, int y) => (y + Pad) * _gw + x + Pad;

    private bool In(int x, int y) => x >= -Pad && y >= -Pad && x < _w + Pad && y < _h + Pad;

    private int XOf(int i) => i % _gw - Pad;

    private int YOf(int i) => i / _gw - Pad;

    /// <summary>The cell at (<paramref name="x"/>, <paramref name="y"/>): space past the room round the frame.</summary>
    private byte At(int x, int y) => In(x, y) ? _cells[I(x, y)] : Space;

    /// <summary>Whether (<paramref name="x"/>, <paramref name="y"/>) is in the arrival room or the ring round it: no event touches it.</summary>
    private bool Prot(int x, int y) => x >= _prot.X0 && x <= _prot.X1 && y >= _prot.Y0 && y <= _prot.Y1;

    /// <summary>Whether a box meets the arrival room's ring.</summary>
    private bool ProtBox(int x0, int y0, int x1, int y1) => x0 <= _prot.X1 && x1 >= _prot.X0 && y0 <= _prot.Y1 && y1 >= _prot.Y0;

    private bool ProtThing(Thing t) => ProtBox(t.X, t.Y, t.X + t.W - 1, t.Y + t.H - 1);

    private bool RibAt(int x, int y)
    {
        foreach (var s in _sections)
            if (s.Kind == Vessel.SectionKind.Rib && x >= s.Left && x < s.Left + s.Width && y >= s.Top && y <= s.Bottom) return true;
        return false;
    }

    /// <summary>The floor rows of the decks in column <paramref name="x"/>: a break's halves have their own.</summary>
    private int[] DecksAt(int x) => x < _split ? _sternDecks : _bowDecks;

    /// <summary>The first row of hull from the top in column <paramref name="x"/>; past the room round the frame for none.</summary>
    private int TopRow(int x)
    {
        int y = -Pad;
        while (y < _h + Pad && At(x, y) < Wall) y++;
        return y;
    }

    /// <summary>Each thing standing by its cells, but lamps when <paramref name="lamps"/> is false; -1 for none.</summary>
    private int[] Occupancy(bool lamps = true)
    {
        var occ = new int[_gw * _gh];
        Array.Fill(occ, -1);
        for (int t = 0; t < _things.Count; t++)
        {
            var th = _things[t];
            if (th.Gone || !lamps && th.Kind == Vessels.Lamp) continue;
            for (int y = th.Y; y < th.Y + th.H; y++)
                for (int x = th.X; x < th.X + th.W; x++)
                    if (In(x, y)) occ[I(x, y)] = t;
        }
        return occ;
    }

    /// <summary>Lamps hanging in cell (<paramref name="x"/>, <paramref name="y"/>) lost.</summary>
    private void LoseLampsAt(int x, int y)
    {
        foreach (var t in _things)
            if (!t.Gone && t.Kind == Vessels.Lamp && x >= t.X && x < t.X + t.W && y >= t.Y && y < t.Y + t.H) t.Gone = true;
    }

    /// <summary>
    /// Cells removed and what stood on them (the mockup's carve), but the arrival room's ring; a rib's skin kept when
    /// <paramref name="keepRibs"/>, the frame across the hole. Every structure on a carved cell goes; returns the machines inside
    /// the hull it hit, for the event to take.
    /// </summary>
    private List<Thing> Carve(List<(int X, int Y)> cells, Record? rec, bool keepRibs = false)
    {
        var cut = new bool[_gw * _gh];
        foreach (var (x, y) in cells)
        {
            if (!In(x, y) || Prot(x, y)) continue;
            int i = I(x, y);
            byte c = _cells[i];
            if (c == Space || cut[i] || keepRibs && c == Wall && _skin[i] && RibAt(x, y)) continue;
            _cells[i] = Space;
            cut[i] = _carved[i] = true;
            rec?.Carved.Add((x, y));
        }
        var hit = new List<Thing>();
        foreach (var t in _things)
        {
            if (t.Gone || !Touches(t, cut)) continue;
            if (!t.Machine) t.Gone = true;
            else if (!t.Outside) hit.Add(t);
        }
        return hit;
    }

    private bool Touches(Thing t, bool[] cells)
    {
        for (int y = t.Y; y < t.Y + t.H; y++)
            for (int x = t.X; x < t.X + t.W; x++)
                if (In(x, y) && cells[I(x, y)]) return true;
        return false;
    }

    /// <summary>The air that left (the mockup's ventAll): every open cell reached from one open to space, through open doors, never shut ones or a hatch.</summary>
    private void Vent()
    {
        int n = _gw * _gh;
        var blocked = new bool[n];
        foreach (var t in _things)
            if (!t.Gone && (t.Kind == "door" && !t.Open || t.Kind == Builds.Hatch))
                for (int y = t.Y; y < t.Y + t.H; y++)
                    for (int x = t.X; x < t.X + t.W; x++)
                        if (In(x, y)) blocked[I(x, y)] = true;
        _vented = new bool[n];
        var stack = new List<int>();
        for (int i = 0; i < n; i++)
        {
            if (_cells[i] != Air || blocked[i]) continue;
            int x = XOf(i), y = YOf(i);
            if (At(x + 1, y) == Space || At(x - 1, y) == Space || At(x, y + 1) == Space || At(x, y - 1) == Space)
            {
                _vented[i] = true;
                stack.Add(i);
            }
        }
        while (stack.Count > 0)
        {
            int i = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            int x = XOf(i), y = YOf(i);
            foreach (var (dx, dy) in N4)
            {
                if (!In(x + dx, y + dy)) continue;
                int j = I(x + dx, y + dy);
                if (_vented[j] || _cells[j] != Air || blocked[j]) continue;
                _vented[j] = true;
                stack.Add(j);
            }
        }
    }

    private static readonly (int Dx, int Dy)[] N4 = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    /// <summary>Every cell of space beside an open cell the air left, in reading order: the breaches as they open.</summary>
    private List<(int X, int Y)> Holes()
    {
        var holes = new List<(int, int)>();
        for (int i = 0; i < _cells.Length; i++)
        {
            if (_cells[i] != Space) continue;
            int x = XOf(i), y = YOf(i);
            foreach (var (dx, dy) in N4)
                if (In(x + dx, y + dy) && _vented[I(x + dx, y + dy)])
                {
                    holes.Add((x, y));
                    break;
                }
        }
        return holes;
    }

    /// <summary>Whether <paramref name="t"/> fits at (<paramref name="x"/>, <paramref name="y"/>): on open cells none but it holds, clear of the arrival room's ring, a hull cell under one of its columns.</summary>
    private bool CanPlace(int[] occ, int self, Thing t, int x, int y)
    {
        if (ProtBox(x, y, x + t.W - 1, y + t.H - 1)) return false;
        for (int yy = y; yy < y + t.H; yy++)
            for (int xx = x; xx < x + t.W; xx++)
            {
                if (At(xx, yy) != Air) return false;
                int o = occ[I(xx, yy)];
                if (o >= 0 && o != self) return false;
            }
        for (int xx = x; xx < x + t.W; xx++)
            if (At(xx, y + t.H) >= Wall) return true;
        return false;
    }

    /// <summary>Slides <paramref name="t"/> along its floor by <paramref name="dx"/> a column at a time while it fits, at most <paramref name="max"/>; returns how far.</summary>
    private int MoveBy(int[] occ, int self, Thing t, int dx, int max)
    {
        Mark(occ, t, -1, self);
        int moved = 0;
        while (moved < max && CanPlace(occ, self, t, t.X + dx, t.Y))
        {
            t.X += dx;
            moved++;
        }
        Mark(occ, t, self, -1);
        return moved;
    }

    private void Mark(int[] occ, Thing t, int to, int from)
    {
        for (int y = t.Y; y < t.Y + t.H; y++)
            for (int x = t.X; x < t.X + t.W; x++)
                if (In(x, y) && (from < 0 || occ[I(x, y)] == from)) occ[I(x, y)] = to;
    }

    /// <summary>
    /// Loose things pulled toward the breaches (the plan's pulled things): each in a room a hole opens into, every cell of it vented,
    /// in reading order, finds its nearest hole; within <c>PullLostPercent</c> of <paramref name="reach"/> it may be lost, else it
    /// slides along its floor toward the hole, to the farthest cell it fits.
    /// </summary>
    private void Pull(Rng r, int reach)
    {
        var holes = Holes();
        if (holes.Count == 0) return;
        var occ = Occupancy();
        var breached = new bool[_rooms.Count];
        for (int k = 0; k < _rooms.Count; k++)
        {
            var rm = _rooms[k];
            foreach (var (hx, hy) in holes)
                if (hx >= rm.Left - 1 && hx <= rm.Left + rm.Width && hy >= rm.Top - 1 && hy <= rm.Top + rm.Height)
                {
                    breached[k] = true;
                    break;
                }
        }
        var loose = new List<int>();
        for (int t = 0; t < _things.Count; t++)
        {
            var th = _things[t];
            if (th.Gone || !th.Machine || th.Outside || !Vessels.Loose.Contains(Vessels.FirstLook(th.Kind)) || ProtThing(th)) continue;
            if (!AllVented(th)) continue;
            int bx = th.X + th.W / 2, by = th.Y + th.H - 1;
            bool inBreached = false;
            for (int k = 0; k < _rooms.Count && !inBreached; k++) inBreached = breached[k] && _rooms[k].C.Holds(bx, by);
            if (inBreached) loose.Add(t);
        }
        loose.Sort((a, b) => _things[a].Y != _things[b].Y ? _things[a].Y.CompareTo(_things[b].Y) : _things[a].X.CompareTo(_things[b].X));
        long far = 100L * reach * reach, lost = (long)reach * reach * Wrecks.PullLostPercent * Wrecks.PullLostPercent;
        foreach (int t in loose)
        {
            var th = _things[t];
            int cx2 = 2 * th.X + th.W, cy2 = 2 * th.Y + th.H;
            long best = long.MaxValue;
            int hx = 0;
            foreach (var (x, y) in holes)
            {
                long dx = 2 * x + 1 - cx2, dy = 2 * y + 1 - cy2, q = 25 * dx * dx + 81 * dy * dy;
                if (q < best) (best, hx) = (q, x);
            }
            if (best > far) continue;
            if (best * 100 < lost && r.Below(100) < (th.W * th.H <= Vessels.SmallCells ? Wrecks.LostSmall : Wrecks.LostOther))
            {
                Mark(occ, th, -1, t);
                th.Gone = true;
                continue;
            }
            int step = Math.Sign(2 * hx + 1 - cx2);
            if (step == 0) continue;
            int max = Math.Min(Math.Max(1, (Math.Abs(2 * hx + 1 - cx2) - th.W) / 2), 2 + (int)r.Below((uint)Math.Max(1, reach / 2)));
            MoveBy(occ, t, th, step, max);
        }
    }

    private bool AllVented(Thing t)
    {
        for (int y = t.Y; y < t.Y + t.H; y++)
            for (int x = t.X; x < t.X + t.W; x++)
                if (!In(x, y) || !_vented[I(x, y)]) return false;
        return true;
    }

    /// <summary>
    /// The armour over (and with <paramref name="outward"/> under) columns <paramref name="x0"/> to <paramref name="x1"/> moved a row
    /// (the mockup's shiftArmour): out into space, or in from the top over the rows under it; never by the arrival room, onto a thing or
    /// over glass. <paramref name="near"/> picks the columns.
    /// </summary>
    private void ShiftArmour(int x0, int x1, int cy, bool outward, Func<int, bool>? near = null)
    {
        var occ = Occupancy(lamps: false);
        for (int x = x0; x <= x1; x++)
        {
            if (near != null && !near(x)) continue;
            foreach (bool up in (ReadOnlySpan<bool>)[true, false])
            {
                if (!outward && !up) continue;
                int step = up ? 1 : -1, y = up ? -Pad : _h + Pad - 1;
                while (In(x, y) && At(x, y) == Space) y += step;
                if (!In(x, y) || At(x, y) != Wall || (up ? y > cy : y < cy)) continue;
                int th = 0;
                while (th < 4 && At(x, y + step * th) == Wall) th++;
                int inner = y + step * th;
                if (At(x, inner) != Air || th < 2) continue;
                if (outward)
                {
                    int o = y - step, last = y + step * (th - 1);
                    if (!In(x, o) || At(x, o) != Space || occ[I(x, o)] >= 0 || Prot(x, o) || Prot(x, last) || occ[I(x, last)] >= 0) continue;
                    _cells[I(x, o)] = Wall;
                    _skin[I(x, o)] = true;
                    _cells[I(x, last)] = Air;
                    _skin[I(x, last)] = false;
                }
                else
                {
                    if (occ[I(x, inner)] >= 0 || Prot(x, y) || Prot(x, inner)) continue;
                    LoseLampsAt(x, inner);
                    for (int k = th; k > 0; k--)
                    {
                        _cells[I(x, y + k)] = Wall;
                        _skin[I(x, y + k)] = true;
                    }
                    _cells[I(x, y)] = Space;
                    _skin[I(x, y)] = false;
                }
            }
        }
    }

    /// <summary>
    /// The inner decks' floors round doubled column <paramref name="cx2"/> sunk (the mockup's buckleDecks): within
    /// <paramref name="reachTenths"/> tenths of a cell and a cell and a half <paramref name="most"/> rows, a cell more further out
    /// one, where nothing stands on the floor or under it, no hatch is in it and the arrival room's ring is not.
    /// </summary>
    private void BuckleDecks(int cx2, int reachTenths, int most)
    {
        var occ = Occupancy(lamps: false);
        var structure = new bool[_gw * _gh];
        foreach (var t in _things)
            if (!t.Gone && !t.Machine)
                for (int y = t.Y; y < t.Y + t.H; y++)
                    for (int x = t.X; x < t.X + t.W; x++)
                        if (In(x, y)) structure[I(x, y)] = true;
        int from = (cx2 * 5 - reachTenths - 60) / 10 - 1, to = (cx2 * 5 + reachTenths + 60) / 10 + 1;
        for (int x = from; x <= to; x++)
        {
            var decks = DecksAt(x);
            for (int k = 0; k + 1 < decks.Length; k++)
            {
                int yd = decks[k];
                if (At(x, yd) != Wall || structure[I(x, yd)] || Prot(x, yd) || StandsOn(occ, x, yd)) continue;
                // Tenths of a cell from the centre: within the reach and a half a cell, all the way; within six more, one row.
                int d = Math.Abs(2 * x + 1 - cx2) * 5;
                int sh = d < reachTenths + 15 ? most : d < reachTenths + 60 ? 1 : 0;
                while (sh > 0)
                {
                    bool ok = true;
                    for (int j = 1; j <= sh; j++)
                        if (At(x, yd + j) != Air || occ[I(x, yd + j)] >= 0 || structure[I(x, yd + j)] || Prot(x, yd + j)) ok = false;
                    if (ok) break;
                    sh--;
                }
                if (sh == 0) continue;
                for (int j = 1; j <= sh; j++) LoseLampsAt(x, yd + j);
                _cells[I(x, yd)] = Air;
                _cells[I(x, yd + sh)] = Wall;
            }
        }
    }

    /// <summary>Whether a thing stands on hull cell (<paramref name="x"/>, <paramref name="y"/>): its bottom row just over it.</summary>
    private bool StandsOn(int[] occ, int x, int y) => In(x, y - 1) && occ[I(x, y - 1)] >= 0;

    /// <summary>Whether <paramref name="r"/> may burn: unburnt, four wide, not the arrival room, its floor's middle open and holding air.</summary>
    private bool Sealed(Room r)
    {
        if (r.Burnt || r.Width < 4 || r.Index == _arrival) return false;
        int x = r.Left + r.Width / 2, y = r.Top + r.Height - 1;
        return At(x, y) == Air && !_vented[I(x, y)];
    }

    private List<Room> SealedRooms() => _rooms.FindAll(Sealed);

    /// <summary>The rooms a breach opens into this event: a cell <paramref name="rec"/> carved inside one or within a cell of its bounds.</summary>
    private List<Room> Opened(Record rec)
    {
        var list = new List<Room>();
        foreach (var rm in _rooms)
            foreach (var (x, y) in rec.Carved)
                if (x >= rm.Left - 1 && x <= rm.Left + rm.Width && y >= rm.Top - 1 && y <= rm.Floor)
                {
                    list.Add(rm);
                    break;
                }
        return list;
    }

    /// <summary>The rooms within <paramref name="b"/>'s cold whose floor's middle the air left: what icing on it froze.</summary>
    private List<int> Frozen(Breach b)
    {
        var list = new List<int>();
        foreach (var rm in _rooms)
        {
            long dx = 2 * rm.Left + rm.Width - 2 * b.X, dy = 2 * rm.Top + rm.Height - 2 * b.Y;
            int x = rm.Left + rm.Width / 2, y = rm.Top + rm.Height - 1;
            if (dx * dx + dy * dy < 4L * b.Reach * b.Reach && In(x, y) && _vented[I(x, y)] && rm.Index != _arrival) list.Add(rm.Index);
        }
        return list;
    }
}
