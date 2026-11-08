using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>
/// Generated vessels (generated-vessels): a class and a seed described as a <see cref="Vessel"/>, stamped into the things its box
/// holds, then damaged by what wrecked it. Pure: the same class, seed and cause give the same parts on every peer. Draws come from
/// a local <see cref="Rng"/> of the seed mixed with the class's place in <see cref="Vessels.Classes"/>, never the world's.
/// </summary>
public static class Shipyard
{
    /// <summary>The parts of a vessel of <paramref name="cls"/> from <paramref name="seed"/>, wrecked by <paramref name="cause"/>.</summary>
    public static IReadOnlyList<VesselPart> Make(VesselClass cls, uint seed, VesselCause cause)
    {
        var vessel = Describe(cls, seed);
        return Damage(Stamp(vessel), vessel, cause);
    }

    /// <summary>A local draw of stream <paramref name="stream"/>: describing is 0, damage 1.</summary>
    private static Rng Draws(VesselClass cls, uint seed, uint stream) =>
        new(new RngState { S = unchecked(seed ^ ((uint)Vessels.IndexOf(cls.Id) + 1) * 0x9E3779B9u ^ (stream + 1) * 0x85EBCA6Bu) });

    private static int Between(Rng rng, (int Min, int Max) range) => range.Min + (int)rng.Below((uint)(range.Max - range.Min + 1));

    /// <summary>
    /// A vessel of <paramref name="cls"/> from <paramref name="seed"/> (a vessel of rooms): its decks drawn, then its rooms, each
    /// furnished by its role. An aft room a deck: the way in on the first, engine rooms below, the bottom one the main engine's by
    /// its bell. Then a hold and a cockpit, each on the shortest deck its rule allows, and the extras on the shortest of all,
    /// sorted aft to fore; an extra that would run its deck past <c>Length</c> is not built. The body is as long as the longest
    /// deck, a shorter deck's last room taking the slack; a door between each two rooms, the well's hatches just past the aft
    /// rooms, a raised bridge over the first deck's forward room, its hatch over that room's first columns.
    /// </summary>
    public static Vessel Describe(VesselClass cls, uint seed)
    {
        var rng = Draws(cls, seed, 0);
        int decks = Between(rng, cls.Decks);
        int a = cls.ArmourDepth, top = cls.BridgeHeight, body = cls.BodyHeight(decks), bottom = top + body - 1;
        int hull = cls.Bells, start = hull + a;
        var deckList = new List<Vessel.Deck>();
        for (int d = 0; d < decks; d++) deckList.Add(new(top + a + Vessels.Pitch * d + Vessels.Open));

        var aft = new List<Room>();
        for (int d = 0; d < decks; d++) aft.Add(Furnish(rng, d == 0 ? VesselRole.Quarters : VesselRole.Engine, main: d > 0 && d == decks - 1));
        int aftWidth = aft.Max(r => r.Width);

        var manifest = new List<VesselRole> { VesselRole.Hold };
        if (cls.BridgeKind == VesselBridge.Cockpit) manifest.Add(VesselRole.Bridge);
        int required = manifest.Count, extras = Between(rng, cls.Extras);
        for (int i = 0; i < extras; i++) manifest.Add(cls.ExtraRoles[(int)rng.Below((uint)cls.ExtraRoles.Count)]);
        var rooms = Enumerable.Range(0, decks).Select(_ => new List<Room>()).ToArray();
        var length = Enumerable.Repeat(aftWidth, decks).ToArray();
        for (int i = 0; i < manifest.Count; i++)
        {
            var room = Furnish(rng, manifest[i], main: false);
            // A required room keeps to its rule's decks; an extra takes the shortest of all, so no deck runs on bare.
            bool extra = i >= required;
            var rule = Vessels.RuleOf(room.Role);
            int deck = -1;
            for (int d = decks - 1; d >= 0; d--)
                if ((extra || Allows(rule.Deck, d)) && (deck < 0 || length[d] < length[deck])) deck = d;
            if (extra && length[deck] + 2 + room.Width > cls.Length) continue;
            rooms[deck].Add(room);
            length[deck] += 2 + room.Width;
        }
        for (int d = 0; d < decks; d++) rooms[d] = rooms[d].OrderBy(r => Vessels.RuleOf(r.Role).Order).ToList();

        int run = length.Max();
        // The first deck's forward room's interior, where a raised bridge's ladder comes down: the bridge stands from there.
        int forward = rooms[0].Count == 0 ? start : start + length[0] - rooms[0][^1].Width;
        Room? raised = cls.BridgeKind == VesselBridge.Raised ? Furnish(rng, VesselRole.Bridge, main: false) : null;
        if (raised != null) run = Math.Max(run, forward + raised.Width - start);

        var sections = new List<Vessel.Section>
        {
            new(Vessel.SectionKind.Stern, 0, cls.Bells, top, bottom),
            new(Vessel.SectionKind.Body, hull, a + run, top, bottom),
        };
        // The tip keeps armour round a cell or two of space; the bottom slopes up more than the top comes down, as a keel does.
        int inset = body - 2 * a - 2, bow = start + run;
        sections.Add(new(Vessel.SectionKind.Bow, bow, cls.Bow, top, bottom, TaperTop: inset / 3, TaperBottom: inset - inset / 3));
        if (raised != null) sections.Add(new(Vessel.SectionKind.Bridge, forward - a, raised.Width + 2 * a, 0, top - 1));

        var doors = new List<Vessel.Door> { new(hull + a - 2, deckList[0].Top) };
        var compartments = new List<Vessel.Compartment>();
        for (int d = 0; d < decks; d++)
        {
            int x = start, slack = run - length[d], deckTop = deckList[d].Top;
            compartments.Add(new(d, x, aftWidth + (rooms[d].Count == 0 ? slack : 0), deckTop, aft[d].Role, aft[d].Interior));
            x += aftWidth;
            for (int k = 0; k < rooms[d].Count; k++)
            {
                var room = rooms[d][k];
                doors.Add(new(x, deckTop));
                int w = room.Width + (k == rooms[d].Count - 1 ? slack : 0);
                compartments.Add(new(d, x + 2, w, deckTop, room.Role, room.Interior));
                x += 2 + w;
            }
        }
        if (raised != null) compartments.Add(new(-1, forward, raised.Width, a, VesselRole.Bridge, raised.Interior));

        // The well: past the aft rooms' doors, a hatch in each floor between decks and a ladder down the deck under it.
        var hatches = new List<Vessel.Hatch>();
        var shafts = new List<Vessel.Shaft>();
        int well = start + aftWidth + 2;
        for (int d = 0; d + 1 < decks; d++)
        {
            hatches.Add(new(well, deckList[d].Floor));
            shafts.Add(new(well, deckList[d + 1].Top, deckList[d + 1].Floor - 1));
        }
        if (raised != null)
        {
            hatches.Add(new(forward, top));
            shafts.Add(new(forward, top + 1, top + Vessels.Open));
        }

        var fittings = new List<Vessel.Fitting>();
        foreach (var c in compartments)
        {
            int w = Interior(c.Interior).Width;
            foreach (var (drawn, cell) in Drawn(c.Interior).Machines())
            {
                // A salvage of several looks wears one drawn here, so vessels differ.
                string kind = Vessels.SalvageOf(drawn) is { Looks: > 1 } s ? Vessels.LookKind(s, (int)rng.Below((uint)s.Looks)) : drawn;
                IReadOnlyList<(string Item, int Count)> loot = Machines.Is(kind, MachineTrait.Container) ? Loot(rng, c.Role) : [];
                fittings.Add(new(kind, c.Left + cell % w, c.Top + cell / w, loot));
            }
        }
        return new Vessel(cls.Id, seed, bow + cls.Bow, bottom + 1, sections, deckList, shafts, doors, hatches, compartments, fittings);
    }

    /// <summary>A room drawn before it is placed: its role, its interior and that interior's columns.</summary>
    private sealed record Room(VesselRole Role, string Interior, int Width);

    /// <summary>A room of <paramref name="role"/> with an interior of it drawn: one holding the main engine, or one without.</summary>
    private static Room Furnish(Rng rng, VesselRole role, bool main)
    {
        var fits = Vessels.Interiors.Where(i => i.Role == role && i.Main == main).ToList();
        var interior = fits[(int)rng.Below((uint)fits.Count)];
        return new(role, interior.Id, interior.Width);
    }

    /// <summary>Whether a room of rule <paramref name="deck"/> may stand on deck <paramref name="d"/>.</summary>
    private static bool Allows(VesselDeck deck, int d) => deck switch
    {
        VesselDeck.Top => d == 0,
        VesselDeck.Low => d > 0,
        _ => true,
    };

    /// <summary>A storage crate's contents in a compartment of <paramref name="role"/>: a stack or two of its role's loot.</summary>
    private static IReadOnlyList<(string Item, int Count)> Loot(Rng rng, VesselRole role)
    {
        var table = Vessels.Loot[role];
        int stacks = 1 + (int)rng.Below(2);
        var loot = new List<(string, int)>();
        for (int k = 0; k < stacks; k++)
        {
            var entry = table[(int)rng.Below((uint)table.Count)];
            loot.Add((entry.Item, entry.Min + (int)rng.Below((uint)(entry.Max - entry.Min + 1))));
        }
        return loot;
    }

    private static VesselInterior Interior(string id) => Vessels.Interiors.First(i => i.Id == id);

    /// <summary>An interior's drawing parsed as a map: its machines and its structures by character.</summary>
    private static Layout Drawn(string id) => Layout.Parse(Interior(id).Rows);

    /// <summary>
    /// The things <paramref name="v"/>'s box holds, intact, in reading order of their top-left cells (back walls, then structures,
    /// then machines in a cell): a back wall in every cell of the hull, walls where armour and floors are, a deck's floor of the
    /// hull, ladders down its shafts, its doors shut, its interiors' floors and machines, and the engine bells outside the hull.
    /// </summary>
    public static IReadOnlyList<VesselPart> Stamp(Vessel v)
    {
        var cls = Vessels.Class(v.Class)!;
        int w = v.Width, h = v.Height, a = cls.ArmourDepth;
        var inside = new bool[w * h];
        foreach (var s in v.Sections)
        {
            if (s.Kind == Vessel.SectionKind.Stern) continue;
            for (int x = s.Left; x < s.Left + s.Width; x++)
            {
                var (top, bottom) = s.RowsAt(x);
                for (int y = top; y <= bottom; y++) inside[y * w + x] = true;
            }
        }
        bool In(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && inside[y * w + x];

        // 0 nothing, 1 a wall of the hull, 2 a floor.
        var solid = new byte[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!In(x, y)) continue;
                for (int k = 1; k <= a && solid[y * w + x] == 0; k++)
                    if (!In(x - k, y) || !In(x + k, y) || !In(x, y - k) || !In(x, y + k)) solid[y * w + x] = 1;
            }
        // A deck's floor between it and the next, and the bridge's over the body's top armour.
        for (int d = 0; d + 1 < v.Decks.Count; d++)
            for (int x = 0; x < w; x++)
                if (In(x, v.Decks[d].Floor)) solid[v.Decks[d].Floor * w + x] = 1;
        if (v.Sections.FirstOrDefault(s => s.Kind == Vessel.SectionKind.Bridge) is { } bridge)
            for (int y = bridge.Bottom + 1; y <= bridge.Bottom + a; y++)
                for (int x = bridge.Left; x < bridge.Left + bridge.Width; x++)
                    if (In(x, y)) solid[y * w + x] = 1;

        var parts = new List<VesselPart>();
        foreach (var door in v.Doors)
        {
            // The armour round the door opened, the door in its last two columns.
            for (int y = door.Y; y < door.Y + Vessels.Open; y++)
                for (int x = door.X + 2 - a; x < door.X + 2; x++) solid[y * w + x] = 0;
            for (int y = door.Y; y < door.Y + Vessels.Open; y += 2) parts.Add(new(door.X, y, "structure", "door"));
        }
        foreach (var shaft in v.Shafts)
        {
            for (int y = shaft.Top; y <= shaft.Bottom; y++)
                for (int x = shaft.Column; x < shaft.Column + 2; x++) solid[y * w + x] = 0;
            for (int y = shaft.Top; y <= shaft.Bottom; y += 2) parts.Add(new(shaft.Column, y, "structure", "ladder"));
        }
        foreach (var hatch in v.Hatches)
        {
            solid[hatch.Y * w + hatch.X] = solid[hatch.Y * w + hatch.X + 1] = 0;
            parts.Add(new(hatch.X, hatch.Y, "structure", Builds.Hatch));
        }
        foreach (var c in v.Compartments)
        {
            var rows = Interior(c.Interior).Rows;
            for (int y = 0; y < rows.Count; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    if (Structures.OfChar(rows[y][x]) is int code && Structures.Of(code).Build is { } build && build != "wall")
                        parts.Add(new(c.Left + x, c.Top + y, "structure", build));
        }
        foreach (var f in v.Fittings) parts.Add(new(f.X, f.Y, "machine", f.Kind, Holds: f.Loot.Count == 0 ? null : f.Loot));

        // The engine bells, a deck each but the first, which has the door: two rows of wall, wider away from the hull.
        var stern = v.Sections[0];
        for (int d = 1; d < v.Decks.Count; d++)
        {
            int deckTop = v.Decks[d].Top;
            for (int x = stern.Left; x < stern.Left + stern.Width; x++)
            {
                int wide = x - stern.Left < stern.Width / 2 ? 0 : 1, narrow = x - stern.Left == stern.Width / 2 - 1 ? 1 : wide;
                foreach (int y in (int[])[deckTop + wide, deckTop + narrow, deckTop + Vessels.Open - 1 - wide, deckTop + Vessels.Open - 1 - narrow])
                    solid[y * w + x] = 1;
            }
        }

        var glass = Glass(v, cls, solid);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool window = glass.Contains((x, y));
                if (In(x, y) && !window) parts.Add(new(x, y, "back", "backWall", Vessels.Hull));
                if (solid[y * w + x] == 1) parts.Add(new(x, y, "structure", "wall", window ? Vessels.Glass : Vessels.Hull));
            }
        parts.Sort(InReadingOrder);
        return parts;
    }

    /// <summary>
    /// The bridge's windows, walls of the hull turned glass, with nothing behind them: a raised bridge's forward armour across the
    /// middle of its open rows; a cockpit's the bow's top armour over its first <c>Vessels.CockpitGlass</c> columns, down its taper.
    /// </summary>
    private static HashSet<(int X, int Y)> Glass(Vessel v, VesselClass cls, byte[] solid)
    {
        var glass = new HashSet<(int, int)>();
        int a = cls.ArmourDepth;
        bool Wall(int x, int y) => solid[y * v.Width + x] == 1;
        if (v.Sections.FirstOrDefault(s => s.Kind == Vessel.SectionKind.Bridge) is { } bridge)
        {
            for (int x = bridge.Left + bridge.Width - a; x < bridge.Left + bridge.Width; x++)
                for (int y = a + 1; y < a + Vessels.Open - 1; y++)
                    if (Wall(x, y)) glass.Add((x, y));
        }
        else
        {
            var bow = v.Sections.First(s => s.Kind == Vessel.SectionKind.Bow);
            for (int x = bow.Left; x < bow.Left + Vessels.CockpitGlass; x++)
                for (int y = bow.RowsAt(x).Top; y < bow.RowsAt(x).Top + a; y++)
                    if (Wall(x, y)) glass.Add((x, y));
        }
        return glass;
    }

    private static int Rank(string layer) => layer switch { "back" => 0, "structure" => 1, _ => 2 };

    /// <summary>Parts by their top-left cells in reading order, back walls, then structures, then machines in a cell: how they are laid.</summary>
    private static int InReadingOrder(VesselPart p, VesselPart q) =>
        p.Y != q.Y ? p.Y.CompareTo(q.Y) : p.X != q.X ? p.X.CompareTo(q.X) : Rank(p.Layer).CompareTo(Rank(q.Layer));

    /// <summary>
    /// <paramref name="parts"/> of <paramref name="v"/> after <paramref name="cause"/>: as they are for none. A breach is a crater of
    /// the class's radius, from above, below or the bow: every thing with a cell in it gone whole, a crate with what it held; what
    /// tore the hull left on a deck by it (<see cref="LeaveDebris"/>).
    /// </summary>
    public static IReadOnlyList<VesselPart> Damage(IReadOnlyList<VesselPart> parts, Vessel v, VesselCause cause)
    {
        if (cause == VesselCause.None) return parts;
        var cls = Vessels.Class(v.Class)!;
        var rng = Draws(cls, v.Seed, 1);
        var body = v.Sections.First(s => s.Kind == Vessel.SectionKind.Body);
        int hull = body.Left, bow = v.Sections.First(s => s.Kind == Vessel.SectionKind.Bow).Left;
        int side = (int)rng.Below(3);
        int x0 = hull + (int)rng.Below((uint)(bow - hull));
        var (cx, cy) = side switch
        {
            0 => (x0, body.Top),
            1 => (x0, body.Bottom),
            _ => (v.Width - 1, (body.Top + body.Bottom) / 2),
        };
        int r2 = cls.Crater * cls.Crater;
        var left = parts.Where(p => !Covers(p).Any(c => (c.X - cx) * (c.X - cx) + (c.Y - cy) * (c.Y - cy) <= r2)).ToList();
        LeaveDebris(left, v, rng, cx, cy, cls.Crater);
        left.Sort(InReadingOrder);
        return left;
    }

    /// <summary>How far past a crater's edge what tore the hull may lie, in cells.</summary>
    private const int DebrisReach = 12;

    /// <summary>
    /// What tore the hull, left by a breach (plan V-1b): one or two <c>Torn</c> salvage objects (a hull section, a shield slab), each
    /// on a deck's floor the nearest it fits to the crater: inside the hull (a back wall in every cell), clear of every structure
    /// and machine, a wall under each column, within <see cref="DebrisReach"/> of its edge. None where none fits.
    /// </summary>
    private static void LeaveDebris(List<VesselPart> parts, Vessel v, Rng rng, int cx, int cy, int crater)
    {
        var taken = new HashSet<(int, int)>();
        var walls = new HashSet<(int, int)>();
        var backs = new HashSet<(int, int)>();
        foreach (var p in parts)
        {
            if (p.Layer == "back") backs.Add((p.X, p.Y));
            else foreach (var c in Covers(p)) taken.Add(c);
            if (p.Kind == "wall") walls.Add((p.X, p.Y));
        }
        var debris = Vessels.Salvage.Where(s => s.Torn).ToList();
        int reach = (crater + DebrisReach) * (crater + DebrisReach);
        for (int n = 1 + (int)rng.Below(2); n > 0; n--)
        {
            var spec = Machines.Get(debris[(int)rng.Below((uint)debris.Count)].Kind);
            (int X, int Y, int D)? best = null;
            foreach (var deck in v.Decks)
                for (int x = 0, y = deck.Floor - spec.Height; x + spec.Width <= v.Width; x++)
                {
                    int dx = x + spec.Width / 2 - cx, dy = y + spec.Height / 2 - cy, d = dx * dx + dy * dy;
                    if (d > reach || best is { } b && b.D <= d) continue;
                    if (Lies(x, y, spec.Width, spec.Height, deck.Floor, taken, walls, backs)) best = (x, y, d);
                }
            if (best is not { } at) continue;
            parts.Add(new(at.X, at.Y, "machine", spec.Kind));
            for (int dy = 0; dy < spec.Height; dy++)
                for (int dx = 0; dx < spec.Width; dx++) taken.Add((at.X + dx, at.Y + dy));
        }
    }

    /// <summary>Whether a thing <paramref name="w"/> by <paramref name="h"/> from (<paramref name="x"/>, <paramref name="y"/>) lies free inside the hull on the floor at <paramref name="floor"/>.</summary>
    private static bool Lies(int x, int y, int w, int h, int floor, HashSet<(int, int)> taken, HashSet<(int, int)> walls, HashSet<(int, int)> backs)
    {
        for (int dx = 0; dx < w; dx++)
        {
            if (!walls.Contains((x + dx, floor))) return false;
            for (int dy = 0; dy < h; dy++)
                if (taken.Contains((x + dx, y + dy)) || !backs.Contains((x + dx, y + dy))) return false;
        }
        return true;
    }

    /// <summary>The cells <paramref name="p"/> covers: a machine's footprint, a piece's (a ladder's, a door's), else its own.</summary>
    public static IEnumerable<(int X, int Y)> Covers(VesselPart p)
    {
        var (across, tall) = p.Layer == "machine" ? (Machines.Get(p.Kind).Width, Machines.Get(p.Kind).Height)
            : p.Layer == "structure" && Builds.PieceOf(p.Kind) is { } piece ? piece : (1, 1);
        for (int dy = 0; dy < tall; dy++)
            for (int dx = 0; dx < across; dx++)
                yield return (p.X + dx, p.Y + dy);
    }
}
