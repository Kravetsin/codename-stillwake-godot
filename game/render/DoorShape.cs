using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>
/// One door, every shape (0067): the pictures a door cell is drawn with by the sides it joins its doorway on, as
/// the sim decides them (<see cref="Doorways.LinksOf"/>): a door's pieces by its joins up and down, a hatch's by
/// its joins either side.
/// </summary>
public static class DoorShape
{
    /// <summary>The picture of a door cell joined on <paramref name="links"/> (<see cref="Doorways.LinksOf"/>), shut or slid open.</summary>
    public static string Picture(bool closed, int links) => (closed ? Closed : Open)[links & 15];

    /// <summary>Whether a doorway is heard, and toggled by a script, at this cell of it: joined neither above nor to its left.</summary>
    public static bool Leads(int links) => (links & (Doorways.Up | Doorways.Left)) == 0;

    /// <summary>Every hatch picture there is, each once: what the ship's atlas packs beside the doors'.</summary>
    public static IEnumerable<string> HatchPictures =>
        Enumerable.Range(0, 16).Where(Doorways.IsHatch).SelectMany(l => new[] { Picture(true, l), Picture(false, l) }).Distinct();

    // Made once, not a string a cell: a big ship draws half a million cells as it loads.
    private static readonly string[] Closed = Names("closed"), Open = Names("open");

    private static string[] Names(string look) => Enumerable.Range(0, 16)
        .Select(l => Doorways.IsHatch(l) ? $"hatch_{look}_{l & (Doorways.Left | Doorways.Right)}" : $"door_{look}_{l & (Doorways.Up | Doorways.Down)}")
        .ToArray();
}
