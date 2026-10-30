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
    /// A vessel of <paramref name="cls"/> from <paramref name="seed"/>: the hull first (stern, midsections, bow, a bridge on top),
    /// then the decks inside it, a compartment a midsection a deck furnished by its role, the shafts between decks and the way in.
    /// </summary>
    public static Vessel Describe(VesselClass cls, uint seed)
    {
        var rng = Draws(cls, seed, 0);
        int mids = Between(rng, cls.Mids), decks = Between(rng, cls.Decks);
        int a = cls.ArmourDepth, top = cls.BridgeHeight, body = cls.BodyHeight(decks), bottom = top + body - 1;
        int hull = cls.Bells, bow = hull + mids * cls.MidWidth, width = bow + cls.Bow;

        var sections = new List<Vessel.Section> { new(Vessel.SectionKind.Stern, 0, cls.Bells, top, bottom) };
        for (int i = 0; i < mids; i++) sections.Add(new(Vessel.SectionKind.Mid, hull + i * cls.MidWidth, cls.MidWidth, top, bottom));
        // The tip keeps armour round a cell or two of space; the bottom slopes up more than the top comes down, as a keel does.
        int inset = body - 2 * a - 2;
        sections.Add(new(Vessel.SectionKind.Bow, bow, cls.Bow, top, bottom, TaperTop: inset / 3, TaperBottom: inset - inset / 3));
        int lastMid = hull + (mids - 1) * cls.MidWidth;
        if (cls.Bridge > 0) sections.Add(new(Vessel.SectionKind.Bridge, lastMid - a, cls.Bridge, 0, top - 1));

        var deckList = new List<Vessel.Deck>();
        for (int d = 0; d < decks; d++) deckList.Add(new(top + a + Vessels.Pitch * d + Vessels.Open));

        var compartments = new List<Vessel.Compartment>();
        for (int i = 0; i < mids; i++)
            for (int d = 0; d < decks; d++)
            {
                int left = i == 0 ? hull + a : hull + i * cls.MidWidth, right = hull + (i + 1) * cls.MidWidth;
                var role = i == 0 ? VesselRole.Engine
                    : i == mids / 2 && d == decks / 2 && decks > 1 ? VesselRole.Life
                    : d == 0 ? VesselRole.Quarters : VesselRole.Hold;
                compartments.Add(Furnished(rng, d, left, right - left, deckList[d].Top, role));
            }
        if (cls.Bridge > 0) compartments.Add(Furnished(rng, -1, lastMid, cls.Bridge - 2 * a, a, VesselRole.Bridge));

        var shafts = new List<Vessel.Shaft>();
        // Between each two decks, a midsection's first columns; never the one the bridge's shaft comes down, nor the last pair's.
        int last = -1;
        for (int d = 0; d + 1 < decks; d++)
        {
            var choices = Enumerable.Range(0, mids).Where(i => (cls.Bridge == 0 || i != mids - 1) && (i != last || mids == 1)).ToList();
            last = choices[(int)rng.Below((uint)choices.Count)];
            int column = last == 0 ? hull + a : hull + last * cls.MidWidth;
            shafts.Add(new(column, deckList[d].Top + 1, deckList[d + 1].Floor - 1));
        }
        if (cls.Bridge > 0)
        {
            int from = a + 1, to = deckList[0].Floor - 1;
            shafts.Add(new(lastMid, (to - from + 1) % 2 == 0 ? from : from + 1, to));
        }

        var doors = new List<Vessel.Door> { new(hull + a - 2, deckList[0].Top) };

        var fittings = new List<Vessel.Fitting>();
        foreach (var c in compartments)
        {
            if (c.Interior is not { } id) continue;
            int w = Interior(id).Width;
            foreach (var (kind, cell) in Drawn(id).Machines())
            {
                IReadOnlyList<(string Item, int Count)> loot = Machines.Is(kind, MachineTrait.Container) ? Loot(rng, c.Role) : [];
                fittings.Add(new(kind, c.Left + cell % w, c.Top + cell / w, loot));
            }
        }
        return new Vessel(cls.Id, seed, width, bottom + 1, sections, deckList, shafts, doors, compartments, fittings);
    }

    /// <summary>A compartment furnished with an interior of its role that fits, drawn; none where none does.</summary>
    private static Vessel.Compartment Furnished(Rng rng, int deck, int left, int width, int top, VesselRole role)
    {
        var fits = Vessels.Interiors.Where(i => i.Role == role && i.Width <= width).ToList();
        string? interior = fits.Count == 0 ? null : fits[(int)rng.Below((uint)fits.Count)].Id;
        return new(deck, left, width, top, role, interior);
    }

    /// <summary>A crate's contents in a compartment of <paramref name="role"/>: one to three stacks of its role's loot.</summary>
    private static IReadOnlyList<(string Item, int Count)> Loot(Rng rng, VesselRole role)
    {
        var table = Vessels.Loot[role];
        int stacks = 1 + (int)rng.Below(3);
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
        foreach (var c in v.Compartments)
        {
            if (c.Interior is not { } id) continue;
            var rows = Interior(id).Rows;
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

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (In(x, y)) parts.Add(new(x, y, "back", "backWall", Vessels.Hull));
                if (solid[y * w + x] == 1) parts.Add(new(x, y, "structure", "wall", Vessels.Hull));
            }
        parts.Sort((p, q) => p.Y != q.Y ? p.Y.CompareTo(q.Y) : p.X != q.X ? p.X.CompareTo(q.X) : Rank(p.Layer).CompareTo(Rank(q.Layer)));
        return parts;
    }

    private static int Rank(string layer) => layer switch { "back" => 0, "structure" => 1, _ => 2 };

    /// <summary>
    /// <paramref name="parts"/> of <paramref name="v"/> after <paramref name="cause"/>: as they are for none. A breach is a crater of
    /// the class's radius, from above, below or the bow: every thing with a cell in it gone whole, a crate with what it held.
    /// </summary>
    public static IReadOnlyList<VesselPart> Damage(IReadOnlyList<VesselPart> parts, Vessel v, VesselCause cause)
    {
        if (cause == VesselCause.None) return parts;
        var cls = Vessels.Class(v.Class)!;
        var rng = Draws(cls, v.Seed, 1);
        var body = v.Sections.First(s => s.Kind == Vessel.SectionKind.Mid);
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
        return [.. parts.Where(p => !Covers(p).Any(c => (c.X - cx) * (c.X - cx) + (c.Y - cy) * (c.Y - cy) <= r2))];
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
