namespace Stillwake.Sim.Grid;

/// <summary>
/// The shape of doorways (0067): a doorway is one straight run of door cells. A door cell with a door cell above
/// or below stands in a door, joined up and down only; every other door cell lies in a hatch, joined to the hatch
/// cells either side of it, so a hatch run stops beside a door. The grid toggles a doorway by these joins, and the
/// client draws, hears, outlines and lights it by them.
/// </summary>
public static class Doorways
{
    /// <summary>Which sides of a cell, a bit each, as the ship's pictures are numbered.</summary>
    public const int Up = 1, Right = 2, Down = 4, Left = 8;

    public static bool IsDoor(int structure) => structure is Structure.DoorClosed or Structure.DoorOpen;

    /// <summary>
    /// The sides <paramref name="cell"/>, a door cell, joins its doorway on, the structure of a grid
    /// <paramref name="width"/> wide and <paramref name="size"/> cells read through <paramref name="structureAt"/>.
    /// </summary>
    public static int LinksOf(int cell, int width, int size, Func<int, int> structureAt)
    {
        bool Door(int c) => c >= 0 && c < size && IsDoor(structureAt(c));
        bool Stands(int c) => Door(c - width) || Door(c + width);
        int links = (Door(cell - width) ? Up : 0) | (Door(cell + width) ? Down : 0);
        if (links != 0) return links;
        int x = cell % width;
        if (x < width - 1 && Door(cell + 1) && !Stands(cell + 1)) links |= Right;
        if (x > 0 && Door(cell - 1) && !Stands(cell - 1)) links |= Left;
        return links;
    }

    /// <summary>Whether a door cell joined on <paramref name="links"/> lies in a hatch: joined neither up nor down.</summary>
    public static bool IsHatch(int links) => (links & (Up | Down)) == 0;
}
