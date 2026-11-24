using Stillwake.Content;
using Aboard = Stillwake.Content.Vessels.Aboard;

namespace Stillwake.Sim;

public static partial class Shipyard
{
    /// <summary>
    /// A guard's dock aboard a vessel (derelict-guards): its <paramref name="Rule"/> (a capsule or a stand, its guard's species and
    /// box), the compartment it is in by its index, and its box's top-left (<paramref name="X"/>, <paramref name="Y"/>) in the
    /// vessel's frame, standing on that room's floor.
    /// </summary>
    public sealed record Dock(Aboard.Rule Rule, int Room, int X, int Y)
    {
        public string Species => Rule.Species;

        public int Width => Rule.Width;

        public int Height => Rule.Height;
    }

    /// <summary>
    /// The guards' docks aboard vessel <paramref name="b"/> as built (<see cref="Vessels.Aboard"/>), drawn from a stream of their own (16)
    /// so the vessel stays as it was: for each kind in turn, its rooms (a deck tall, or more) counted, the arrival room among them, a
    /// wreck's bow half of a cut room not; the candidates, every room in index order but the arrival room, where the dock's box fits clear
    /// of the way (a ladder, a hatch in its floor) and, on a wreck, of what it lost (<see cref="Lost"/>);
    /// <c>min(ceil(rooms / Per), candidates)</c> of them (a wreck's <c>WreckPer</c>) drawn by a partial Fisher–Yates; then in index order one draw each of a column
    /// where the box fits, clear of every fitting and shelf still standing where it can be, else over them. Spiders, then robots, each by
    /// room index. What a capsule covers is left out as the vessel is written (<see cref="Docked"/>); a stand stands over what it covers.
    /// </summary>
    public static IReadOnlyList<Dock> Guards(Built b)
    {
        var v = b.Vessel;
        var rng = Draws(Vessels.Class(v.Class)!, v.Seed, 16);
        var (ladders, furniture) = Taken(v);
        if (b.Wreck != null) (ladders, furniture) = (Lost(b, ladders), Standing(b, furniture));
        var (ax, ay) = Door(b);
        var docks = new List<Dock>();
        foreach (var rule in Aboard.Rules)
        {
            int rooms = 0;
            var candidates = new List<int>();
            for (int i = 0; i < v.Compartments.Count; i++)
            {
                var c = v.Compartments[i];
                if (c.Decks > 1 != rule.Big) continue;
                if (c.Of < 0) rooms++;
                if (!c.Holds(ax, ay) && Fits(c, rule, ladders, furniture).Count > 0) candidates.Add(i);
            }
            int per = b.Wreck == null ? rule.Per : rule.WreckPer, count = Math.Min((rooms + per - 1) / per, candidates.Count);
            for (int k = 0; k < count; k++)
            {
                int j = k + (int)rng.Below((uint)(candidates.Count - k));
                (candidates[k], candidates[j]) = (candidates[j], candidates[k]);
            }
            var picked = candidates.Take(count).Order().ToList();
            foreach (int i in picked)
            {
                var c = v.Compartments[i];
                var columns = Fits(c, rule, ladders, furniture);
                int x = columns[(int)rng.Below((uint)columns.Count)];
                docks.Add(new(rule, i, x, c.Floor - rule.Height));
            }
        }
        return docks;
    }

    /// <summary>
    /// <paramref name="ladders"/>, every piece of a ladder left standing (a shaft cut short too), and every cell of a room of wreck
    /// <paramref name="b"/> its chain took (derelict-guards): one an event carved (<see cref="WreckEvent.Carved"/>), or with no back wall
    /// left, open to space (a break's gap); and every cell of the floor row under a room carved or with no structure left (a sunk deck).
    /// </summary>
    private static HashSet<(int, int)> Lost(Built b, HashSet<(int, int)> ladders)
    {
        var v = b.Vessel;
        var backed = new HashSet<(int, int)>();
        var built = new HashSet<(int, int)>();
        var lost = new HashSet<(int, int)>(ladders);
        foreach (var p in b.Parts)
        {
            if (p.Layer == "back") backed.Add((p.X, p.Y));
            else if (p.Kind == "ladder") lost.UnionWith(Covers(p));
            if (p.Layer == "structure") built.Add((p.X, p.Y));
        }
        var carved = new HashSet<int>();
        foreach (var e in b.Wreck!.Events) carved.UnionWith(e.Carved);
        foreach (var c in v.Compartments)
            for (int x = c.Left; x < c.Left + c.Width; x++)
            {
                for (int y = c.Top; y < c.Floor; y++)
                    if (!backed.Contains((x, y)) || carved.Contains(y * v.Width + x)) lost.Add((x, y));
                if (!built.Contains((x, c.Floor)) || carved.Contains(c.Floor * v.Width + x)) lost.Add((x, c.Floor));
            }
        return lost;
    }

    /// <summary>The cells of <paramref name="furniture"/> something of wreck <paramref name="b"/> still covers: what its chain left standing.</summary>
    private static HashSet<(int, int)> Standing(Built b, HashSet<(int, int)> furniture)
    {
        var covered = new HashSet<(int, int)>();
        foreach (var p in b.Parts)
            if (p.Layer != "back") covered.UnionWith(Covers(p));
        covered.IntersectWith(furniture);
        return covered;
    }

    /// <summary>
    /// <paramref name="parts"/> of a vessel without what a dock of <paramref name="docks"/> that replaces furniture covers: every machine and
    /// every piece but a wall with a cell in its box, whole, a crate with what it held. The parts as Places lays them.
    /// </summary>
    public static IReadOnlyList<VesselPart> Docked(IReadOnlyList<VesselPart> parts, IReadOnlyList<Dock> docks)
    {
        var boxes = docks.Where(d => d.Rule.Crowded == Aboard.Crowded.Replace).ToList();
        if (boxes.Count == 0) return parts;
        bool Covered(VesselPart p) => p.Layer != "back" && p.Kind != "wall"
            && Covers(p).Any(c => boxes.Exists(d => c.X >= d.X && c.X < d.X + d.Width && c.Y >= d.Y && c.Y < d.Y + d.Height));
        return [.. parts.Where(p => !Covered(p))];
    }

    /// <summary>
    /// The left columns, in order, where <paramref name="rule"/>'s box stands on <paramref name="c"/>'s floor inside it clear of every
    /// cell of the way (its floor's row under it too) and of all <paramref name="furniture"/>; with none so clear, those clear of the way.
    /// </summary>
    private static List<int> Fits(Vessel.Compartment c, Aboard.Rule rule, HashSet<(int, int)> ladders, HashSet<(int, int)> furniture)
    {
        var clear = Columns(c, rule, ladders, furniture);
        return clear.Count > 0 ? clear : Columns(c, rule, ladders, null);
    }

    private static List<int> Columns(Vessel.Compartment c, Aboard.Rule rule, HashSet<(int, int)> ladders, HashSet<(int, int)>? furniture)
    {
        var fits = new List<int>();
        int top = c.Floor - rule.Height;
        if (top < c.Top) return fits;
        for (int x = c.Left; x + rule.Width <= c.Left + c.Width; x++)
        {
            bool clear = true;
            // Its box clear, and the floor row under it: a wreck's lost floor is in the way.
            for (int y = top; y <= c.Floor && clear; y++)
                for (int dx = 0; dx < rule.Width && clear; dx++)
                    clear = !ladders.Contains((x + dx, y)) && furniture?.Contains((x + dx, y)) != true;
            if (clear) fits.Add(x);
        }
        return fits;
    }

    /// <summary>
    /// The way inside <paramref name="v"/>, kept open: every cell a ladder covers and the cells over a hatch; and every cell a fitting or a
    /// shelf of an interior covers, what stands before a back wall. No fitting is solid (only structures are), so a guard may stand in one.
    /// </summary>
    private static (HashSet<(int, int)> Ladders, HashSet<(int, int)> Furniture) Taken(Vessel v)
    {
        var ladders = new HashSet<(int, int)>();
        var furniture = new HashSet<(int, int)>();
        foreach (var f in v.Fittings)
        {
            var spec = Machines.Get(f.Kind);
            for (int dy = 0; dy < spec.Height; dy++)
                for (int dx = 0; dx < spec.Width; dx++) furniture.Add((f.X + dx, f.Y + dy));
        }
        foreach (var s in v.Shafts)
            for (int y = s.Top; y <= s.Bottom; y++) ladders.UnionWith([(s.Column, y), (s.Column + 1, y)]);
        foreach (var h in v.Hatches) ladders.UnionWith([(h.X, h.Y - 1), (h.X + 1, h.Y - 1)]);
        foreach (var c in v.Compartments)
        {
            if (c.Interior.Length == 0) continue;
            var rows = Interior(c.Interior).Rows;
            for (int y = 0; y < rows.Count; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    if (Structures.OfChar(rows[y][x]) is int code && Structures.Of(code).Build is not null) furniture.Add((c.Left + x, c.Top + y));
        }
        return (ladders, furniture);
    }
}
