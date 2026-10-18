using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Render;

/// <summary>
/// One door, every shape (0067): a door cell joined to a door cell above or below is part of a door, standing in
/// a wall; every other door cell, in a run across or alone, is part of a hatch, lying in a deck. The sim knows no
/// shape; whatever draws or hears a doorway asks here, by <c>linked</c>, the sides of the cell a door cell is on.
/// </summary>
public static class DoorShape
{
    /// <summary>Which sides of a cell, a bit each, as the pictures are numbered.</summary>
    public const int Up = 1, Right = 2, Down = 4, Left = 8;

    /// <summary>Whether a door cell joined to door cells on the sides in <paramref name="linked"/> is part of a hatch: none above or below.</summary>
    public static bool IsHatch(int linked) => (linked & (Up | Down)) == 0;

    /// <summary>
    /// The picture a door cell joined on the sides in <paramref name="linked"/> is drawn with, shut or slid open:
    /// a door's piece by its joins above and below, a hatch's by its joins to either side.
    /// </summary>
    public static string Picture(bool closed, int linked) => (closed ? Closed : Open)[linked & 15];

    /// <summary>Whether a doorway is heard, and toggled by a script, at this cell of it: no door above it or to its left.</summary>
    public static bool Leads(int linked) => (linked & (Up | Left)) == 0;

    /// <summary>Every hatch picture there is, each once: what the ship's atlas packs beside the doors'.</summary>
    public static IEnumerable<string> HatchPictures =>
        Enumerable.Range(0, 16).Where(IsHatch).SelectMany(l => new[] { Picture(true, l), Picture(false, l) }).Distinct();

    // Made once, not a string a cell: a big ship draws half a million cells as it loads.
    private static readonly string[] Closed = Names("closed"), Open = Names("open");

    private static string[] Names(string look) => Enumerable.Range(0, 16)
        .Select(l => IsHatch(l) ? $"hatch_{look}_{l & (Left | Right)}" : $"door_{look}_{l & (Up | Down)}").ToArray();
}
