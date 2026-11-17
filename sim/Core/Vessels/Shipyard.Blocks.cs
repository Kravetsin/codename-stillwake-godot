using Stillwake.Content;
using F = Stillwake.Content.Vessels.Fits;

namespace Stillwake.Sim;

public static partial class Shipyard
{
    /// <summary>A room of a block before it is placed: its first deck, how many it spans, its role, its drawing (empty: furnished by code).</summary>
    private sealed record Stacked(int Deck, int Decks, VesselRole Role, string Interior);

    /// <summary>A block (big-vessels): its left column, its width, its rooms from the top of its band down.</summary>
    private sealed class Block(int width, List<Stacked> stack)
    {
        public int Left;
        public int Width = width;
        public readonly List<Stacked> Stack = stack;
    }

    /// <summary>
    /// A raised room of a big class (big-vessels): its section's kind, its columns inside its armour, its tier over the hull (1 on the
    /// hull, 2 on a tier-1 room), its role and drawing.
    /// </summary>
    private sealed record Raised(Vessel.SectionKind Kind, int Left, int Width, int Tier, VesselRole Role, string Interior);

    /// <summary>A room on <paramref name="deck"/> of a drawing of its role.</summary>
    private static Stacked RoomOf(Rng rng, int deck, VesselRole role, bool main = false) => new(deck, 1, role, Furnish(rng, role, main).Interior);

    /// <summary>A block as wide as drawn, or as its widest drawing and <paramref name="past"/> columns past it (a ladder's two).</summary>
    private static Block Made(int drawn, List<Stacked> stack, int past = 2) =>
        new(Math.Max(drawn, stack.Where(s => s.Interior.Length > 0).Select(s => Interior(s.Interior).Width + past).DefaultIfEmpty(0).Max()), stack);

    /// <summary>The service block <paramref name="width"/> wide over <paramref name="decks"/> decks: quarters on the first, then engine rooms, holds or quarters.</summary>
    private static Block Service(Rng rng, int width, int decks) =>
        Made(width, [RoomOf(rng, 0, VesselRole.Quarters), .. Enumerable.Range(1, decks - 1).Select(d =>
            (int)rng.Below(3) switch { 0 => RoomOf(rng, d, VesselRole.Engine), 1 => RoomOf(rng, d, VesselRole.Hold), _ => RoomOf(rng, d, VesselRole.Quarters) })]);

    /// <summary>
    /// A big class's vessel laid from its blocks (big-vessels): <paramref name="bands"/>' blocks placed, the first band's first block the
    /// aft one (the way in over the engine rooms), its last the forward one, and <paramref name="raised"/> over the hull. Stream 2: its
    /// modules, none bulging up under a raised room or the <paramref name="housing"/> nor more than <paramref name="bulgeDown"/> rows
    /// under the keel; ribs; the bow. Then stream 0 on from <paramref name="rng"/>: the inner doors' states, a door where both rooms'
    /// floors meet; hatches, laid before furniture: a well down <paramref name="service"/>'s left and one by the last block's right, the
    /// raised rooms', then a repair pass till every room is reached; with <paramref name="tankLadders"/> a ladder up the side of each tall
    /// tank room no ladder runs down; then each room's furniture in reading order. A nozzle by each engine room of the aft block; the parts
    /// outside but a freighter's keel. Last, on stream 0 still, each tank's liquid (<see cref="Fill"/>).
    /// </summary>
    private static Vessel Lay(VesselClass cls, uint seed, Rng rng, int decks, IReadOnlyList<List<Block>> bands, Block service,
        IReadOnlyList<Raised> raised, int housing, int bulgeDown, bool tankLadders)
    {
        int a = cls.ArmourDepth, body = cls.BodyHeight(decks), hull = Vessels.NozzleSize.W;
        var aft = bands[0][0];
        int start = aft.Left, run = bands[0][^1].Left + bands[0][^1].Width - start, bow = start + run;

        var shape = Draws(cls, seed, 2);
        var calm = raised.Select(r => (Left: r.Left - a, Right: r.Left + r.Width + 2 * a)).ToList();
        if (housing >= 0) calm.Add((housing, housing + Vessels.HousingWidth));
        bool Calm(int left, int width) => calm.Exists(c => left < c.Right && left + width > c.Left);
        var modules = new List<(int Left, int Width, int Up, int Down)>();
        for (int left = hull; left < bow;)
        {
            bool stern = modules.Count == 0;
            int next = stern ? start + aft.Width + Vessels.RibWidth / 2 : left + Between(shape, Vessels.ModuleEvery);
            int right = next > bow - Vessels.ModuleStop ? bow : next;
            var bulge = stern ? Vessels.SternBulge : Vessels.Bulge;
            int up = Between(shape, bulge), down = Math.Min(bulgeDown, Between(shape, bulge));
            modules.Add((left, right - left, Calm(left, right - left) ? 0 : up, down));
            left = right;
        }
        var ribs = new List<(int Left, int Up, int Down)>();
        for (int i = 1; i < modules.Count; i++)
            ribs.Add((modules[i].Left - Vessels.RibWidth / 2, Math.Max(modules[i - 1].Up, modules[i].Up) + 1,
                Math.Max(modules[i - 1].Down, modules[i].Down) + 1));
        int bowRun = Between(shape, cls.BowRun);
        var (fall, rise) = cls.BowSteps(body);
        int bowWidth = cls.BowFlat + bowRun * Math.Max(fall, rise);

        int raisedHeight = cls.BridgeHeight;
        int over = Math.Max(modules.Max(m => m.Up), ribs.Count == 0 ? 0 : ribs.Max(r => r.Up));
        int under = Math.Max(modules.Max(m => m.Down), ribs.Count == 0 ? 0 : ribs.Max(r => r.Down));
        int top = Math.Max(raisedHeight * raised.Max(r => r.Tier), over), bottom = top + body - 1;
        var deckList = new List<Vessel.Deck>();
        for (int d = 0; d < decks; d++) deckList.Add(new(top + a + Vessels.Pitch * d + Vessels.Open));

        var sections = new List<Vessel.Section>
        {
            new(Vessel.SectionKind.Body, hull, a + run, top, bottom),
            new(Vessel.SectionKind.Bow, bow, bowWidth, top, bottom, Flat: cls.BowFlat, Run: bowRun, Fall: fall, Rise: rise),
        };
        foreach (var r in raised)
            sections.Add(new(r.Kind, r.Left - a, r.Width + 2 * a, top - r.Tier * raisedHeight, top - (r.Tier - 1) * raisedHeight - 1));
        foreach (var m in modules) sections.Add(new(Vessel.SectionKind.Module, m.Left, m.Width, top - m.Up, bottom + m.Down));
        foreach (var r in ribs) sections.Add(new(Vessel.SectionKind.Rib, r.Left, Vessels.RibWidth, top - r.Up, bottom + r.Down));
        if (housing >= 0) sections.Add(new(Vessel.SectionKind.Housing, housing, Vessels.HousingWidth, top - raisedHeight + Vessels.HousingDrop, top - 1));

        // The rooms, in reading order.
        var rooms = new List<Vessel.Compartment>();
        foreach (var b in bands.SelectMany(b => b))
            foreach (var s in b.Stack) rooms.Add(new(s.Deck, b.Left, b.Width, deckList[s.Deck].Top, s.Role, s.Interior, s.Decks));
        foreach (var r in raised) rooms.Add(new(-r.Tier, r.Left, r.Width, top - r.Tier * raisedHeight + a, r.Role, r.Interior));
        rooms = [.. rooms.OrderBy(c => c.Top).ThenBy(c => c.Left)];
        var links = rooms.Select(_ => new List<int>()).ToArray();
        void Link(Vessel.Compartment p, Vessel.Compartment q)
        {
            int i = rooms.IndexOf(p), j = rooms.IndexOf(q);
            links[i].Add(j);
            links[j].Add(i);
        }
        Vessel.Compartment? RoomAt(int col, int deck) => rooms.Find(c => c.Deck >= 0 && deck >= c.Deck && deck < c.Deck + c.Decks && col >= c.Left && col < c.Left + c.Width);

        // Doors: the way in, then in each band's gaps, a deck at a time, where that deck is both rooms' lowest.
        var doors = new List<Vessel.Door> { new(hull + a - 2, deckList[0].Top) };
        foreach (var band in bands)
        {
            int first = band.Min(b => b.Stack.Min(s => s.Deck)), end = band.Max(b => b.Stack.Max(s => s.Deck + s.Decks));
            for (int i = 0; i + 1 < band.Count; i++)
            {
                int gap = band[i].Left + band[i].Width;
                for (int d = first; d < end; d++)
                    if (RoomAt(gap - 1, d) is { } left && RoomAt(gap + 2, d) is { } right && d == left.Deck + left.Decks - 1 && d == right.Deck + right.Decks - 1)
                    {
                        doors.Add(new(gap, deckList[d].Top, Open: rng.Below(Vessels.DoorOpenOneIn) == 0));
                        Link(left, right);
                    }
            }
        }

        // Hatches, a ladder down from each: whole pieces from the lower room's top, a row left over a step.
        var hatches = new List<Vessel.Hatch>();
        var shafts = new List<Vessel.Shaft>();
        void Hatch(int col, Vessel.Compartment upperRoom, Vessel.Compartment lowerRoom, int row, int shaftTop, int shaftRows)
        {
            hatches.Add(new(col, row));
            shafts.Add(new(col, shaftTop, shaftTop + shaftRows - shaftRows % 2 - 1));
            Link(upperRoom, lowerRoom);
        }
        void Down(int col, Vessel.Compartment upperRoom, Vessel.Compartment lowerRoom) => Hatch(col, upperRoom, lowerRoom, upperRoom.Floor, lowerRoom.Top, lowerRoom.Height);
        // Two wells, at the service block's left and two columns short of the first band's last block's right (past its drawings).
        var last = bands[0][^1];
        foreach (int well in (int[])[service.Left, last.Left + last.Width - 2])
            for (int d = 0; d + 1 < decks; d++)
                if (RoomAt(well, d) is { } p && RoomAt(well + 1, d) == p && RoomAt(well, d + 1) is { } q && RoomAt(well + 1, d + 1) == q && p != q && p.Floor + 1 == q.Top)
                    Down(well, p, q);
        // A raised room's hatch over its first columns, its ladder through the armour under it: a second tier's runs on through the first.
        var lifted = rooms.Where(c => c.Deck < 0).OrderByDescending(c => c.Deck).ThenBy(c => c.Left).ToList();
        foreach (var c in lifted)
        {
            var beneath = c.Deck == -1 ? RoomAt(c.Left, 0)! : lifted.First(r => r.Deck == c.Deck + 1 && r.Left == c.Left);
            int row = top + (c.Deck + 1) * raisedHeight;
            Hatch(c.Left, c, beneath, row, row + 1, Vessels.Open);
        }

        // The repair pass: while a room is not reached from the way in, the first such gets a hatch to a reached room right above it,
        // else right below, at the right end of the columns they share, two columns in.
        int airlock = rooms.FindIndex(c => c.Deck == 0 && c.Left == start);
        bool[] Reached()
        {
            var seen = new bool[rooms.Count];
            var todo = new Stack<int>();
            todo.Push(airlock);
            seen[airlock] = true;
            while (todo.Count > 0)
                foreach (int n in links[todo.Pop()])
                    if (!seen[n])
                    {
                        seen[n] = true;
                        todo.Push(n);
                    }
            return seen;
        }
        bool Fits(Vessel.Compartment p, Vessel.Compartment q, int col) =>
            !hatches.Exists(h => h.Y == p.Floor && Math.Abs(h.X - col) < 2)
            && !shafts.Exists(s => Math.Abs(s.Column - col) < 2 && s.Top < q.Floor && s.Bottom >= q.Top)
            && Clear(p, col, below: false) && Clear(q, col, below: true);
        bool Repair(Vessel.Compartment p, Vessel.Compartment q)
        {
            int from = Math.Max(p.Left, q.Left);
            for (int col = Math.Min(p.Left + p.Width, q.Left + q.Width) - 2; col >= from; col--)
                if (Fits(p, q, col))
                {
                    Down(col, p, q);
                    return true;
                }
            return false;
        }
        for (var seen = Reached(); Array.IndexOf(seen, false) is var lost and >= 0; seen = Reached())
        {
            var u = rooms[lost];
            bool done = rooms.Where((c, i) => seen[i] && c.Floor + 1 == u.Top).Any(c => Repair(c, u))
                || rooms.Where((c, i) => seen[i] && u.Floor + 1 == c.Top).Any(c => Repair(u, c));
            if (!done) throw new InvalidOperationException($"{cls.Id} {seed}: the {u.Role} on deck {u.Deck} at {u.Left} cannot be reached");
        }

        // A tall tank room's ladder up its tank's side (the mockup's), where no hatch's ladder already comes down it.
        if (tankLadders)
            foreach (var c in rooms.Where(c => c.Decks > 1 && c.Role is VesselRole.FuelBay or VesselRole.TankBay))
                if (!shafts.Exists(s => s.Column >= c.Left && s.Column < c.Left + c.Width && s.Top < c.Floor && s.Bottom >= c.Top))
                    shafts.Add(new(c.Left + c.Width - F.TankLadderFromRight, c.Top, c.Top + c.Height - c.Height % 2 - 1));

        // Furniture, room by room in reading order: a drawing's machines, or the room furnished by its role.
        var fittings = new List<Vessel.Fitting>();
        foreach (var c in rooms)
        {
            if (c.Interior.Length == 0)
            {
                Fit(rng, c, Kept(c, hatches, shafts), fittings);
                continue;
            }
            int w = Interior(c.Interior).Width;
            foreach (var (drawn, cell) in Drawn(c.Interior).Machines())
            {
                int looks = Vessels.LooksOf(drawn);
                string kind = looks > 1 ? Vessels.LookKind(drawn, (int)rng.Below((uint)looks)) : drawn;
                IReadOnlyList<(string Item, int Count)> loot = Machines.Is(kind, MachineTrait.Container) ? Loot(rng, c.Role) : [];
                fittings.Add(new(kind, c.Left + cell % w, c.Top + cell / w, loot));
            }
        }

        // A nozzle out of the stern by each engine room; the parts outside but the freighter's keel.
        var outside = new List<Vessel.Fitting>();
        foreach (var s in aft.Stack.Where(s => s.Role == VesselRole.Engine))
            outside.Add(new(Vessels.Nozzle, hull - Vessels.NozzleSize.W, deckList[s.Deck].Top, []));
        Outfit(shape, sections, outside, start + aft.Width, raised.First(r => r.Kind == Vessel.SectionKind.Bridge).Left, a, cls.Layout, rooms);
        Fill(rng, rooms, fittings, outside);
        int width = Math.Max(bow + bowWidth, outside.Max(f => f.X + Machines.Get(f.Kind).Width));
        int height = Math.Max(bottom + 1 + under, outside.Max(f => f.Y + Machines.Get(f.Kind).Height));
        var vessel = new Vessel(cls.Id, seed, width, height, sections, deckList, shafts, doors, hatches, rooms, fittings, outside);
        int lift = -Math.Min(0, outside.Min(f => f.Y));
        return lift == 0 ? vessel : Lowered(vessel, lift);
    }

    /// <summary>
    /// Whether the two columns from <paramref name="col"/> are clear in <paramref name="c"/>'s drawing for a hatch: over its whole height for
    /// the ladder in the room <paramref name="below"/> it, on its last row for the room over it; a room furnished by code keeps them itself.
    /// </summary>
    private static bool Clear(Vessel.Compartment c, int col, bool below)
    {
        if (c.Interior.Length == 0) return true;
        var rows = Interior(c.Interior).Rows;
        for (int k = col - c.Left; k < col - c.Left + 2; k++)
        {
            if (k < 0 || k >= rows[0].Length) continue;
            foreach (var row in below ? rows : [rows[^1]])
                if (row[k] != '.') return false;
        }
        return true;
    }

    /// <summary>The columns of <paramref name="c"/> no furniture stands in: its walkway, a hatch's in its floor, a ladder's down it.</summary>
    private static HashSet<int> Kept(Vessel.Compartment c, List<Vessel.Hatch> hatches, List<Vessel.Shaft> shafts)
    {
        var kept = new HashSet<int>();
        for (int k = 0; k < Vessels.Fits.Walkway; k++) kept.Add(c.Left + k);
        foreach (var h in hatches)
            if (h.Y == c.Floor) kept.UnionWith([h.X, h.X + 1]);
        foreach (var s in shafts)
            if (s.Top < c.Floor && s.Bottom >= c.Top) kept.UnionWith([s.Column, s.Column + 1]);
        return kept;
    }
}
