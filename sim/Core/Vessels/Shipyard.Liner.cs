using Stillwake.Content;
using L = Stillwake.Content.Vessels.Liner;

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
    /// A liner (big-vessels) from <paramref name="seed"/>. Stream 0: its decks and length drawn, then its blocks: the upper band's
    /// aft block (the way in over engine rooms), the service block, blocks of cabins and now and then a public one (infirmary, lounge or
    /// storage), the pool's and the restaurant's two decks tall with a lounge and a galley over them and an infirmary's public block at
    /// their places along it, the forward block under the bridge; the bottom band's fuel tank by the engines, holds, the hangar at the
    /// bow; the raised bridge, the promenade and its dome. Then the inner doors' states, a door where both rooms' floors meet; hatches,
    /// laid before furniture: the well down the service block's left, the raised rooms', then a repair pass till every room is reached;
    /// then each room's furniture in reading order. Stream 2, as for a vessel of rooms: its modules, its bow, its parts outside.
    /// </summary>
    private static Vessel Liner(VesselClass cls, uint seed)
    {
        var rng = Draws(cls, seed, 0);
        int decks = Between(rng, cls.Decks), low = decks - L.LowDecks, run = Between(rng, L.Run);
        int a = cls.ArmourDepth, body = cls.BodyHeight(decks);
        int hull = Vessels.NozzleSize.W, start = hull + a;

        // A room of a drawing of its role; a block as wide as drawn, or as its widest drawing and a ladder's two columns past it.
        Stacked Room(int deck, VesselRole role, bool main = false) => new(deck, 1, role, Furnish(rng, role, main).Interior);
        Stacked Cabin(int deck) => new(deck, 1, VesselRole.Quarters, "cabin");
        Block Made(int drawn, List<Stacked> stack, int past = 2) =>
            new(Math.Max(drawn, stack.Where(s => s.Interior.Length > 0).Select(s => Interior(s.Interior).Width + past).DefaultIfEmpty(0).Max()), stack);

        var upper = new List<Block>();
        var aft = Made(0, [Room(0, VesselRole.Quarters), .. Enumerable.Range(1, low - 1).Select(d => Room(d, VesselRole.Engine, main: true))], past: 0);
        int serviceWidth = Between(rng, L.Service);
        var service = Made(serviceWidth, [Room(0, VesselRole.Quarters), .. Enumerable.Range(1, low - 1).Select(d =>
            (int)rng.Below(3) switch { 0 => Room(d, VesselRole.Engine), 1 => Room(d, VesselRole.Hold), _ => Room(d, VesselRole.Quarters) })]);
        upper.Add(aft);
        upper.Add(service);
        aft.Left = start;
        service.Left = start + aft.Width + 2;
        int x = service.Left + service.Width + 2;

        int forwardWidth = Between(rng, L.Forward), end = start + run - forwardWidth;
        int poolAt = start + run * Between(rng, L.PoolAt) / 100, infirmaryAt = start + run * Between(rng, L.InfirmaryAt) / 100,
            restaurantAt = start + run * Between(rng, L.RestaurantAt) / 100;
        bool pool = false, infirmary = false, restaurant = false;
        Block Tall(VesselRole role, VesselRole over)
        {
            int w = Between(rng, L.Tall);
            return Made(w, [new(0, 1, over, ""), new(1, 2, role, ""), .. Enumerable.Range(3, Math.Max(0, low - 3)).Select(Cabin)]);
        }
        Block Public(bool sick)
        {
            int w = Between(rng, L.Public);
            return Made(w, [Cabin(0), .. Enumerable.Range(1, low - 1).Select(d => sick && d == 1 ? new Stacked(d, 1, VesselRole.Infirmary, "")
                : (int)rng.Below(3) switch { 0 => new Stacked(d, 1, VesselRole.Infirmary, ""), 1 => new Stacked(d, 1, VesselRole.Lounge, ""), _ => Room(d, VesselRole.Hold) })]);
        }
        for (int guard = 0; guard < 90; guard++)
        {
            Block b;
            if (!pool && x >= poolAt) (b, pool) = (Tall(VesselRole.Pool, VesselRole.Lounge), true);
            else if (!infirmary && x >= infirmaryAt) (b, infirmary) = (Public(sick: true), true);
            else if (!restaurant && x >= restaurantAt) (b, restaurant) = (Tall(VesselRole.Restaurant, VesselRole.Galley), true);
            else if (rng.Below(100) < L.PublicPercent) b = Public(sick: false);
            else b = Made(Between(rng, L.Cabins), [.. Enumerable.Range(0, low).Select(Cabin)]);
            if (x + b.Width + 2 > end) break;
            b.Left = x;
            upper.Add(b);
            x += b.Width + 2;
        }
        var fwd = Made(forwardWidth, [Room(0, VesselRole.Quarters), .. Enumerable.Range(1, low - 1).Select(Cabin)]);
        fwd.Left = x;
        fwd.Width = Math.Max(fwd.Width, start + run - x);
        upper.Add(fwd);
        run = fwd.Left + fwd.Width - start;
        foreach (var role in (VesselRole[])[VesselRole.Pool, VesselRole.Restaurant, VesselRole.Infirmary])
            if (!upper.Any(b => b.Stack.Any(s => s.Role == role))) throw new InvalidOperationException($"liner {seed}: no {role}");

        // The bottom band: the fuel tank under the engines, holds, the hangar at the bow at least so wide.
        var lower = new List<Block> { new(aft.Width, [new(low, L.LowDecks, VesselRole.FuelBay, "")]) { Left = start } };
        int bx = start + aft.Width;
        for (int guard = 0; guard < 90; guard++)
        {
            int w = Between(rng, L.Hold);
            if (start + run - (bx + 2 + w + 2) < L.HangarMin) break;
            lower.Add(new(w, [new(low, L.LowDecks, VesselRole.Hold, "")]) { Left = bx + 2 });
            bx += 2 + w;
        }
        lower.Add(new(start + run - bx - 2, [new(low, L.LowDecks, VesselRole.Hangar, "")]) { Left = bx + 2 });

        // Raised: the bridge over the forward block, its housing behind it, the promenade over the first block well past the service
        // block, short of the housing, and the dome on its stern end.
        var bridgeRoom = Furnish(rng, VesselRole.Bridge, main: false);
        int forward = fwd.Left, bridgeLeft = forward - a, bridgeWidth = bridgeRoom.Width + 2 * a;
        int serviceEnd = service.Left + service.Width;
        int housing = bridgeLeft - Vessels.HousingWidth >= serviceEnd ? bridgeLeft - Vessels.HousingWidth : -1;
        var first = upper.First(b => b.Left > serviceEnd + L.PromenadePast);
        int promenadeWidth = Math.Min(Between(rng, L.Promenade), (housing >= 0 ? housing : bridgeLeft) - L.PromenadeGap - a - first.Left);
        bool promenade = promenadeWidth >= L.PromenadeMin;
        int domeWidth = promenade ? Math.Min(L.DomeMax, promenadeWidth - L.DomeShort) : 0;

        // Stream 2: modules, none bulging up under a raised room or the housing, nor more than a row under the keel.
        var shape = Draws(cls, seed, 2);
        var calm = new List<(int Left, int Right)> { (bridgeLeft, bridgeLeft + bridgeWidth + a) };
        if (housing >= 0) calm.Add((housing, housing + Vessels.HousingWidth));
        if (promenade) calm.Add((first.Left - a, first.Left + promenadeWidth + 2 * a));
        bool Calm(int left, int width) => calm.Exists(c => left < c.Right && left + width > c.Left);
        int bow = start + run;
        var modules = new List<(int Left, int Width, int Up, int Down)>();
        for (int left = hull; left < bow;)
        {
            bool stern = modules.Count == 0;
            int next = stern ? start + aft.Width + Vessels.RibWidth / 2 : left + Between(shape, Vessels.ModuleEvery);
            int right = next > bow - Vessels.ModuleStop ? bow : next;
            var bulge = stern ? Vessels.SternBulge : Vessels.Bulge;
            int up = Between(shape, bulge), down = Math.Min(L.BulgeDown, Between(shape, bulge));
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
        int top = Math.Max(raisedHeight * (promenade ? 2 : 1), over), bottom = top + body - 1;
        var deckList = new List<Vessel.Deck>();
        for (int d = 0; d < decks; d++) deckList.Add(new(top + a + Vessels.Pitch * d + Vessels.Open));

        var sections = new List<Vessel.Section>
        {
            new(Vessel.SectionKind.Body, hull, a + run, top, bottom),
            new(Vessel.SectionKind.Bow, bow, bowWidth, top, bottom, Flat: cls.BowFlat, Run: bowRun, Fall: fall, Rise: rise),
            new(Vessel.SectionKind.Bridge, bridgeLeft, bridgeWidth, top - raisedHeight, top - 1),
        };
        if (promenade)
        {
            sections.Add(new(Vessel.SectionKind.Tier, first.Left - a, promenadeWidth + 2 * a, top - raisedHeight, top - 1));
            sections.Add(new(Vessel.SectionKind.Tier, first.Left - a, domeWidth + 2 * a, top - 2 * raisedHeight, top - raisedHeight - 1));
        }
        foreach (var m in modules) sections.Add(new(Vessel.SectionKind.Module, m.Left, m.Width, top - m.Up, bottom + m.Down));
        foreach (var r in ribs) sections.Add(new(Vessel.SectionKind.Rib, r.Left, Vessels.RibWidth, top - r.Up, bottom + r.Down));
        if (housing >= 0) sections.Add(new(Vessel.SectionKind.Housing, housing, Vessels.HousingWidth, top - raisedHeight + Vessels.HousingDrop, top - 1));

        // The rooms, in reading order.
        var rooms = new List<Vessel.Compartment>();
        foreach (var b in upper.Concat(lower))
            foreach (var s in b.Stack) rooms.Add(new(s.Deck, b.Left, b.Width, deckList[s.Deck].Top, s.Role, s.Interior, s.Decks));
        int raisedTop = top - raisedHeight + a;
        rooms.Add(new(-1, forward, bridgeRoom.Width, raisedTop, VesselRole.Bridge, bridgeRoom.Interior));
        if (promenade)
        {
            rooms.Add(new(-1, first.Left, promenadeWidth, raisedTop, VesselRole.Lounge, ""));
            rooms.Add(new(-2, first.Left, domeWidth, raisedTop - raisedHeight, VesselRole.Lounge, ""));
        }
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
        foreach (var band in (List<Block>[])[upper, lower])
            for (int i = 0; i + 1 < band.Count; i++)
            {
                int gap = band[i].Left + band[i].Width;
                for (int d = band == upper ? 0 : low; d < (band == upper ? low : decks); d++)
                    if (RoomAt(gap - 1, d) is { } left && RoomAt(gap + 2, d) is { } right && d == left.Deck + left.Decks - 1 && d == right.Deck + right.Decks - 1)
                    {
                        doors.Add(new(gap, deckList[d].Top, Open: rng.Below(Vessels.DoorOpenOneIn) == 0));
                        Link(left, right);
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
        int well = service.Left;
        for (int d = 0; d + 1 < decks; d++)
            if (RoomAt(well, d) is { } p && RoomAt(well + 1, d) == p && RoomAt(well, d + 1) is { } q && RoomAt(well + 1, d + 1) == q && p != q && p.Floor + 1 == q.Top)
                Down(well, p, q);
        // A raised room's hatch over its first columns, its ladder through the hull's top armour: the dome's runs on through the promenade.
        var raised = rooms.Where(c => c.Deck < 0).OrderByDescending(c => c.Deck).ThenBy(c => c.Left).ToList();
        foreach (var c in raised)
        {
            var beneath = c.Deck == -1 ? RoomAt(c.Left, 0)! : raised.First(r => r.Deck == -1 && r.Left == c.Left);
            int row = c.Deck == -1 ? top : top - raisedHeight;
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
            if (!done) throw new InvalidOperationException($"liner {seed}: the {u.Role} on deck {u.Deck} at {u.Left} cannot be reached");
        }

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
        for (int d = 1; d < low; d++) outside.Add(new(Vessels.Nozzle, hull - Vessels.NozzleSize.W, deckList[d].Top, []));
        Outfit(shape, sections, outside, start + aft.Width, forward, a, keel: false);
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
