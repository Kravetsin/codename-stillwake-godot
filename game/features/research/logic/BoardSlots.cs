using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>Where a thing stands on the board, the first note at the origin.</summary>
/// <param name="Row">Whether it stands in the row of what is known with no part noted, which may shift as the board grows.</param>
public sealed record BoardSlot(string Id, double X, double Y, bool Row);

/// <summary>
/// Where everything on the board stands (docs/plans/board.md), a pure function of what was found in what order, so a late guest gets the
/// same board. Nothing moves on its own: a thing holds its slot before it is known, and inks in where it stands.
/// </summary>
public static class BoardSlots
{
    /// <summary>How close two slots may stand.</summary>
    public const double NoteSize = 84;

    /// <summary>The way between neighbours on an arc, the first arc's distance from what it goes round, how far either side of straight out an arc reaches (radians), and the row's distance above the first note.</summary>
    private const double Spacing = NoteSize * 1.1, Radius = 140, ArcHalf = 2.1, RowOut = 2 * Radius;

    /// <param name="found">What was obtained, in the order first obtained.</param>
    /// <param name="things">Every blueprint and craft, in the order they take their slots when one part notes several.</param>
    /// <param name="granted">What is known: only the row reads it.</param>
    public static IReadOnlyList<BoardSlot> Of(IReadOnlyList<string> found, IReadOnlyList<BoardThing> things, IReadOnlyCollection<string> granted)
    {
        var slots = new List<BoardSlot>();
        if (found.Count == 0) return slots;
        var at = new Dictionary<string, (double X, double Y)>(StringComparer.Ordinal);
        void Take(string id, string? round)
        {
            var spot = round == null ? (0, 0) : Next(at[round], at.Values);
            at[id] = spot;
            slots.Add(new BoardSlot(id, spot.X, spot.Y, false));
        }
        for (int i = 0; i < found.Count; i++)
        {
            string item = found[i];
            // Sooner than its parts: nothing noted to go round but the note before it.
            if (!at.ContainsKey(item)) Take(item, i == 0 ? null : found[i - 1]);
            foreach (var thing in things)
                if (!thing.Market && !at.ContainsKey(thing.Id) && thing.Parts.Any(p => p.Item == item)) Take(thing.Id, item);
        }
        var known = granted.ToHashSet(StringComparer.Ordinal);
        int along = 0;
        // What takes nothing has no part to be noted by and is no find: the pod's helmet shows once made.
        foreach (var thing in things.Where(t => t.Parts.Count > 0 && !at.ContainsKey(t.Id) && known.Contains(t.Id)).OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            while (!Free((along * Spacing, -RowOut), at.Values)) along++;
            var spot = (along * Spacing, -RowOut);
            at[thing.Id] = spot;
            slots.Add(new BoardSlot(thing.Id, spot.Item1, spot.Item2, true));
        }
        return slots;
    }

    /// <summary>The next free slot on an arc round <paramref name="round"/>, straight out from the first note first, then either side, then the next arc out.</summary>
    private static (double X, double Y) Next((double X, double Y) round, IReadOnlyCollection<(double X, double Y)> taken)
    {
        double outward = round is { X: 0, Y: 0 } ? -Math.PI / 2 : Math.Atan2(round.Y, round.X);
        for (double radius = Radius; ; radius += NoteSize)
        {
            double step = Spacing / radius;
            for (int n = 0; n * step <= ArcHalf; n++)
                foreach (int side in n == 0 ? [1] : (int[])[1, -1])
                {
                    double angle = outward + side * n * step;
                    (double X, double Y) spot = (round.X + radius * Math.Cos(angle), round.Y + radius * Math.Sin(angle));
                    if (Free(spot, taken)) return spot;
                }
        }
    }

    private static bool Free((double X, double Y) spot, IReadOnlyCollection<(double X, double Y)> taken) =>
        taken.All(t => (t.X - spot.X) * (t.X - spot.X) + (t.Y - spot.Y) * (t.Y - spot.Y) >= NoteSize * NoteSize);
}
