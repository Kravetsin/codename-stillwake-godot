namespace Stillwake.Sim.Grid;

/// <summary>
/// The shape of doorways (0067, 0104): a doorway is one straight run of door cells, <see cref="Thick"/> cells thick. A door
/// cell in a run of more than <see cref="Thick"/> door cells up and down stands in a door; every other door cell lies in a
/// hatch. A cell joins the door cells above and below it, and those either side that stand as it does, so a door's two
/// columns are one doorway, a hatch's two rows another, and a hatch run stops beside a door. The grid toggles a doorway by
/// these joins, and the client draws, hears, outlines and lights it by them.
/// </summary>
public static class Doorways
{
    /// <summary>Which sides of a cell, a bit each, as the ship's pictures are numbered.</summary>
    public const int Up = 1, Right = 2, Down = 4, Left = 8;

    /// <summary>Set in <see cref="LinksOf"/> beside the sides for a cell standing in a door, clear for one lying in a hatch.</summary>
    public const int Standing = 16;

    /// <summary>A doorway's thickness in cells (0104): a 16 px door's width, a hatch's height.</summary>
    public const int Thick = 2;

    public static bool IsDoor(int structure) => structure is Structure.DoorClosed or Structure.DoorOpen;

    /// <summary>
    /// The sides <paramref name="cell"/>, a door cell, joins its doorway on, with <see cref="Standing"/> for one in a door, the
    /// structure of a grid <paramref name="width"/> wide and <paramref name="size"/> cells read through <paramref name="structureAt"/>.
    /// </summary>
    public static int LinksOf(int cell, int width, int size, Func<int, int> structureAt)
    {
        bool Door(int c) => c >= 0 && c < size && IsDoor(structureAt(c));
        // In a run of more than Thick up and down: counting no further than Thick each way is enough to tell.
        bool Stands(int c)
        {
            int run = 0;
            for (int k = 1; k <= Thick && Door(c - k * width); k++) run++;
            for (int k = 1; k <= Thick && Door(c + k * width); k++) run++;
            return run >= Thick;
        }
        bool stands = Stands(cell);
        int links = (Door(cell - width) ? Up : 0) | (Door(cell + width) ? Down : 0) | (stands ? Standing : 0);
        int x = cell % width;
        if (x < width - 1 && Door(cell + 1) && Stands(cell + 1) == stands) links |= Right;
        if (x > 0 && Door(cell - 1) && Stands(cell - 1) == stands) links |= Left;
        return links;
    }

    /// <summary>Whether a door cell joined on <paramref name="links"/> (<see cref="LinksOf"/>) lies in a hatch: it does not stand in a door.</summary>
    public static bool IsHatch(int links) => (links & Standing) == 0;
}
