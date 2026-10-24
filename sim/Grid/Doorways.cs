using Structure = Stillwake.Content.Structure;


namespace Stillwake.Sim.Grid;

/// <summary>
/// The shape of doorways (0067, 0104, 0111): a door is built of segments <see cref="Thick"/> cells wide and tall, and its
/// segments stacked one above another are one doorway; a hatch is one piece <see cref="Thick"/> cells wide and a cell tall,
/// a doorway of its own. A door cell joins the door cells above and below it, and the cell beside it within its segment; a
/// hatch cell joins only the cell beside it within its piece. A door never joins a hatch. The grid toggles a doorway by these
/// joins, and the client draws, hears, outlines and lights it by them.
/// </summary>
public static class Doorways
{
    /// <summary>Which sides of a cell, a bit each, as the ship's pictures are numbered.</summary>
    public const int Up = 1, Right = 2, Down = 4, Left = 8;

    /// <summary>A door segment's width in cells, and a hatch piece's (0104).</summary>
    public const int Thick = 2;

    /// <summary>Whether <paramref name="structure"/> is a door cell, shut or open.</summary>
    public static bool IsDoor(int structure) => structure is Structure.DoorClosed or Structure.DoorOpen;

    /// <summary>Whether <paramref name="structure"/> is a hatch cell, shut or open.</summary>
    public static bool IsHatch(int structure) => structure is Structure.HatchClosed or Structure.HatchOpen;

    /// <summary>Whether <paramref name="structure"/> is part of a door or a hatch.</summary>
    public static bool IsDoorway(int structure) => IsDoor(structure) || IsHatch(structure);

    /// <summary>Whether <paramref name="structure"/> is a doorway's cell as it stands open.</summary>
    public static bool IsOpen(int structure) => structure is Structure.DoorOpen or Structure.HatchOpen;

    /// <summary>Whether <paramref name="structure"/> is a doorway's cell as it stands shut.</summary>
    public static bool IsClosed(int structure) => structure is Structure.DoorClosed or Structure.HatchClosed;

    /// <summary>
    /// The sides <paramref name="cell"/>, a doorway cell, joins its doorway on, the structure of a grid <paramref name="width"/>
    /// wide and <paramref name="size"/> cells read through <paramref name="structureAt"/>: a door cell its neighbours above and
    /// below, a door's or a hatch's the cell beside it in its piece, paired from the left end of its row's run of the same kind.
    /// </summary>
    public static int LinksOf(int cell, int width, int size, Func<int, int> structureAt)
    {
        int structure = structureAt(cell);
        if (!IsDoorway(structure)) return 0;
        bool door = IsDoor(structure);
        bool Same(int c) => c >= 0 && c < size && (door ? IsDoor(structureAt(c)) : IsHatch(structureAt(c)));
        int links = 0;
        if (door)
        {
            if (Same(cell - width)) links |= Up;
            if (Same(cell + width)) links |= Down;
        }
        int x = cell % width, left = 0;
        while (left < x && Same(cell - left - 1)) left++;
        if (x < width - 1 && Same(cell + 1) && left % Thick == 0) links |= Right;
        if (x > 0 && Same(cell - 1) && left % Thick == 1) links |= Left;
        return links;
    }
}
