

using Stillwake.Content;

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
    public static bool IsDoor(int structure) => Structures.Of(structure).Group == StructureGroup.Door;

    /// <summary>Whether <paramref name="structure"/> is a hatch cell, shut or open.</summary>
    public static bool IsHatch(int structure) => Structures.Of(structure).Group == StructureGroup.Hatch;

    /// <summary>Whether <paramref name="structure"/> is a doorway lying along its row, its panel sliding across it, as a hatch does (<see cref="Join.Row"/>).</summary>
    public static bool Lies(int structure) => Structures.Of(structure).Join == Join.Row;

    /// <summary>Whether <paramref name="structure"/> is a launch platform's cell, shut or open: a doorway opened with power alone (Landing).</summary>
    public static bool IsPlatform(int structure) => Structures.Of(structure).Group == StructureGroup.Platform;

    /// <summary>Whether <paramref name="structure"/> is part of a door, a hatch or a launch platform.</summary>
    public static bool IsDoorway(int structure) => Structures.Of(structure).Group != StructureGroup.None;

    /// <summary>Whether <paramref name="structure"/> is a doorway's cell as it stands open: sealing nothing, an opening or a platform's hole.</summary>
    public static bool IsOpen(int structure) => IsDoorway(structure) && !Structures.Is(structure, Trait.Seals);

    /// <summary>Whether <paramref name="structure"/> is a doorway's cell as it stands shut: sealed.</summary>
    public static bool IsClosed(int structure) => IsDoorway(structure) && Structures.Is(structure, Trait.Seals);

    /// <summary>The code <paramref name="structure"/>, a doorway cell, takes <paramref name="open"/> or shut: its own, or its toggle.</summary>
    public static int Toggled(int structure, bool open) =>
        IsOpen(structure) == open ? structure : Structures.Of(structure).Toggle ?? structure;

    /// <summary>
    /// The sides <paramref name="cell"/>, a doorway cell, joins its doorway on, the structure of a grid <paramref name="width"/>
    /// wide and <paramref name="size"/> cells read through <paramref name="structureAt"/>: a door cell (joining in a column) its
    /// neighbours above and below, a door's or a hatch's the cell beside it in its piece, paired from the left end of its row's run of the same kind.
    /// </summary>
    public static int LinksOf(int cell, int width, int size, Func<int, int> structureAt)
    {
        var spec = Structures.Of(structureAt(cell));
        if (spec.Group == StructureGroup.None) return 0;
        bool Same(int c) => c >= 0 && c < size && Structures.Of(structureAt(c)).Group == spec.Group;
        int links = 0;
        if (spec.Join == Join.Column)
        {
            if (Same(cell - width)) links |= Up;
            if (Same(cell + width)) links |= Down;
        }
        int x = cell % width, left = 0;
        // A launch platform's run is one doorway, piece after piece: it opens whole (Landing).
        if (spec.Join == Join.Run)
        {
            if (x > 0 && Same(cell - 1)) links |= Left;
            if (x < width - 1 && Same(cell + 1)) links |= Right;
            return links;
        }
        while (left < x && Same(cell - left - 1)) left++;
        if (x < width - 1 && Same(cell + 1) && left % Thick == 0) links |= Right;
        if (x > 0 && Same(cell - 1) && left % Thick == 1) links |= Left;
        return links;
    }
}
