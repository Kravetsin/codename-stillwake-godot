using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>
/// One door, every shape (0067): the pictures a door cell is drawn with by the sides it joins its doorway on, as
/// the sim decides them (<see cref="Doorways.LinksOf"/>): a door's pieces by its joins up and down, a hatch's by
/// its joins either side. Between shut and open, <see cref="Steps"/> − 1 pictures of its panel sliding: a door's
/// up into its frame, a hatch's along into its jamb.
/// </summary>
public static class DoorShape
{
    /// <summary>How many steps a slide from shut (0) to open takes: a cell's 16 pixels, two at a time.</summary>
    public const int Steps = 8;

    /// <summary>The picture of a door cell joined on <paramref name="links"/> (<see cref="Doorways.LinksOf"/>), shut or slid open.</summary>
    public static string Picture(bool closed, int links) => (closed ? Closed : Open)[links & 15];

    /// <summary>The picture of a door cell joined on <paramref name="links"/>, <paramref name="step"/> of <see cref="Steps"/> of the way open.</summary>
    public static string Picture(int step, int links) => Sliding[step][links & 15];

    /// <summary>Whether a doorway is heard, and toggled by a script, at this cell of it: joined neither above nor to its left.</summary>
    public static bool Leads(int links) => (links & (Doorways.Up | Doorways.Left)) == 0;

    /// <summary>Every hatch picture there is, each once: what the ship's atlas packs beside the doors'.</summary>
    public static IEnumerable<string> HatchPictures =>
        Enumerable.Range(0, 16).Where(Doorways.IsHatch).SelectMany(l => new[] { Picture(true, l), Picture(false, l) }).Distinct();

    /// <summary>
    /// Every picture of a panel part way open, each once, with the shut and open pictures it is made from, how far it has
    /// slid in pixels, and whether it is a hatch's: what the ship's atlas makes with <see cref="Slid"/>.
    /// </summary>
    public static IEnumerable<(string Name, string Closed, string Open, int Shift, bool Hatch)> SlidPictures =>
        Enumerable.Range(0, 16).Select(l => l & (Doorways.IsHatch(l) ? Doorways.Left | Doorways.Right : Doorways.Up | Doorways.Down)).Distinct()
            .SelectMany(l => Enumerable.Range(1, Steps - 1).Select(step =>
                (Sliding[step][l], Closed[l], Open[l], step * Cell / Steps, Doorways.IsHatch(l))));

    /// <summary>
    /// A panel slid <paramref name="shift"/> pixels open, from pictures <paramref name="closed"/> and <paramref name="open"/>
    /// (RGBA, <paramref name="width"/> pixels a row): the open picture, but where the two differ, the shut one's pixel
    /// <paramref name="shift"/> further down (a door's panel rising) or, for a <paramref name="hatch"/>, further right (sliding
    /// left), while that pixel is still panel; past it, open.
    /// </summary>
    public static byte[] Slid(byte[] closed, byte[] open, int width, int shift, bool hatch)
    {
        int height = closed.Length / 4 / width;
        var slid = (byte[])open.Clone();
        bool Panel(int x, int y)
        {
            int i = (y * width + x) * 4;
            return closed[i] != open[i] || closed[i + 1] != open[i + 1] || closed[i + 2] != open[i + 2] || closed[i + 3] != open[i + 3];
        }
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (!Panel(x, y)) continue;
                int fromX = hatch ? x + shift : x, fromY = hatch ? y : y + shift;
                if (fromX >= width || fromY >= height || !Panel(fromX, fromY)) continue;
                System.Array.Copy(closed, (fromY * width + fromX) * 4, slid, (y * width + x) * 4, 4);
            }
        return slid;
    }

    private const int Cell = 16;

    // Made once, not a string a cell: a big ship draws half a million cells as it loads.
    private static readonly string[] Closed = Names("closed"), Open = Names("open");
    private static readonly string[][] Sliding = Enumerable.Range(0, Steps + 1)
        .Select(step => step == 0 ? Closed : step == Steps ? Open : Names($"slid{step}")).ToArray();

    private static string[] Names(string look) => Enumerable.Range(0, 16)
        .Select(l => Doorways.IsHatch(l) ? $"hatch_{look}_{l & (Doorways.Left | Doorways.Right)}" : $"door_{look}_{l & (Doorways.Up | Doorways.Down)}")
        .ToArray();
}
