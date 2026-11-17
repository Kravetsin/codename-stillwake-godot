using Stillwake.Content;
using Aboard = Stillwake.Content.Vessels.Aboard;

namespace Stillwake.Sim;

public static partial class Shipyard
{
    /// <summary>
    /// A guard's dock aboard a vessel (derelict-enemies): its <paramref name="Rule"/> (a capsule or a stand, its guard's species and
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
    /// The guards' docks aboard intact vessel <paramref name="v"/> (<see cref="Vessels.Aboard"/>), drawn from a stream of their own (3) so
    /// the vessel stays as it was: for each kind in turn, its rooms (a deck tall, or more) counted, the arrival room among them; the
    /// candidates, those rooms in index order but the arrival room, where the dock's box fits clear of every ladder and, but for one that
    /// replaces furniture, of every fitting and shelf; <c>min(ceil(rooms / Per), candidates)</c> of them drawn by a partial Fisher–Yates;
    /// then in index order one draw each of a column where the box fits, clear of furniture where it can be. Spiders, then robots, each
    /// by room index. What a replacing dock covers is left out as the vessel is written (<see cref="Docked"/>).
    /// </summary>
    public static IReadOnlyList<Dock> Guards(Vessel v)
    {
        var rng = Draws(Vessels.Class(v.Class)!, v.Seed, 3);
        var (ladders, furniture) = Taken(v);
        var (ax, ay) = Door(v);
        var docks = new List<Dock>();
        foreach (var rule in Aboard.Rules)
        {
            int rooms = 0;
            var candidates = new List<int>();
            for (int i = 0; i < v.Compartments.Count; i++)
            {
                var c = v.Compartments[i];
                if (c.Decks > 1 != rule.Big) continue;
                rooms++;
                if (!c.Holds(ax, ay) && Fits(c, rule, ladders, furniture).Count > 0) candidates.Add(i);
            }
            int count = Math.Min((rooms + rule.Per - 1) / rule.Per, candidates.Count);
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
    /// <paramref name="parts"/> of a vessel without what a dock of <paramref name="docks"/> that replaces furniture covers: every machine and
    /// every piece but a wall with a cell in its box, whole, a crate with what it held. The parts as Places lays them.
    /// </summary>
    public static IReadOnlyList<VesselPart> Docked(IReadOnlyList<VesselPart> parts, IReadOnlyList<Dock> docks)
    {
        var boxes = docks.Where(d => d.Rule.Replaces).ToList();
        if (boxes.Count == 0) return parts;
        bool Covered(VesselPart p) => p.Layer != "back" && p.Kind != "wall"
            && Covers(p).Any(c => boxes.Exists(d => c.X >= d.X && c.X < d.X + d.Width && c.Y >= d.Y && c.Y < d.Y + d.Height));
        return [.. parts.Where(p => !Covered(p))];
    }

    /// <summary>
    /// The left columns, in order, where <paramref name="rule"/>'s box stands on <paramref name="c"/>'s floor inside it clear of every
    /// ladder and of all <paramref name="furniture"/>; for a rule that replaces furniture, with none so clear, those clear of ladders.
    /// </summary>
    private static List<int> Fits(Vessel.Compartment c, Aboard.Rule rule, HashSet<(int, int)> ladders, HashSet<(int, int)> furniture)
    {
        var clear = Columns(c, rule, ladders, furniture);
        return clear.Count > 0 || !rule.Replaces ? clear : Columns(c, rule, ladders, null);
    }

    private static List<int> Columns(Vessel.Compartment c, Aboard.Rule rule, HashSet<(int, int)> ladders, HashSet<(int, int)>? furniture)
    {
        var fits = new List<int>();
        int top = c.Floor - rule.Height;
        if (top < c.Top) return fits;
        for (int x = c.Left; x + rule.Width <= c.Left + c.Width; x++)
        {
            bool clear = true;
            for (int y = top; y < c.Floor && clear; y++)
                for (int dx = 0; dx < rule.Width && clear; dx++)
                    clear = !ladders.Contains((x + dx, y)) && furniture?.Contains((x + dx, y)) != true;
            if (clear) fits.Add(x);
        }
        return fits;
    }

    /// <summary>Every cell inside <paramref name="v"/> a ladder covers, and every one a fitting or a shelf of an interior covers: what stands before a back wall.</summary>
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
