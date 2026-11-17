using Stillwake.Content;
using Aboard = Stillwake.Content.Vessels.Aboard;

namespace Stillwake.Sim;

public static partial class Shipyard
{
    /// <summary>
    /// A guard's dock aboard a vessel (derelict-enemies): the guard's species, the compartment it is in by its index, and the dock's box,
    /// from its top-left (<paramref name="X"/>, <paramref name="Y"/>) in the vessel's frame, <paramref name="Width"/> by
    /// <paramref name="Height"/>, standing on that room's floor.
    /// </summary>
    public sealed record Dock(string Species, int Room, int X, int Y, int Width, int Height);

    /// <summary>
    /// The guards' docks aboard intact vessel <paramref name="v"/> (<see cref="Vessels.Aboard"/>), drawn from a stream of their own (3) so
    /// the vessel stays as it was: for each kind in turn, its rooms (a deck tall, or more) counted, the arrival room among them; the
    /// candidates, those rooms in index order but the arrival room, where the dock's box fits clear of every fitting, ladder and shelf;
    /// <c>min(ceil(rooms / Per), candidates)</c> of them drawn by a partial Fisher–Yates; then in index order one draw each of a column
    /// where the box fits. Spiders, then robots, each by room index.
    /// </summary>
    public static IReadOnlyList<Dock> Guards(Vessel v)
    {
        var rng = Draws(Vessels.Class(v.Class)!, v.Seed, 3);
        var taken = Taken(v);
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
                if (!c.Holds(ax, ay) && Fits(c, rule, taken).Count > 0) candidates.Add(i);
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
                var columns = Fits(c, rule, taken);
                int x = columns[(int)rng.Below((uint)columns.Count)];
                docks.Add(new(rule.Species, i, x, c.Floor - rule.Height, rule.Width, rule.Height));
            }
        }
        return docks;
    }

    /// <summary>The left columns, in order, where <paramref name="rule"/>'s box stands on <paramref name="c"/>'s floor inside it with no cell <paramref name="taken"/>.</summary>
    private static List<int> Fits(Vessel.Compartment c, Aboard.Rule rule, HashSet<(int, int)> taken)
    {
        var fits = new List<int>();
        int top = c.Floor - rule.Height;
        if (top < c.Top) return fits;
        for (int x = c.Left; x + rule.Width <= c.Left + c.Width; x++)
        {
            bool clear = true;
            for (int y = top; y < c.Floor && clear; y++)
                for (int dx = 0; dx < rule.Width && clear; dx++)
                    clear = !taken.Contains((x + dx, y));
            if (clear) fits.Add(x);
        }
        return fits;
    }

    /// <summary>Every cell inside <paramref name="v"/> a fitting, a ladder or a shelf of an interior covers: what stands before a back wall.</summary>
    private static HashSet<(int, int)> Taken(Vessel v)
    {
        var taken = new HashSet<(int, int)>();
        foreach (var f in v.Fittings)
        {
            var spec = Machines.Get(f.Kind);
            for (int dy = 0; dy < spec.Height; dy++)
                for (int dx = 0; dx < spec.Width; dx++) taken.Add((f.X + dx, f.Y + dy));
        }
        foreach (var s in v.Shafts)
            for (int y = s.Top; y <= s.Bottom; y++) taken.UnionWith([(s.Column, y), (s.Column + 1, y)]);
        foreach (var c in v.Compartments)
        {
            if (c.Interior.Length == 0) continue;
            var rows = Interior(c.Interior).Rows;
            for (int y = 0; y < rows.Count; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    if (Structures.OfChar(rows[y][x]) is int code && Structures.Of(code).Build is not null) taken.Add((c.Left + x, c.Top + y));
        }
        return taken;
    }
}
