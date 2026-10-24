namespace Stillwake.Content;

/// <summary>Structure codes as stored in saves: never reuse or renumber one. Rules ask for a code's traits (<see cref="Structures"/>).</summary>
public static class Structure
{
    public const int Empty = 0, Wall = 1, Floor = 2, Ladder = 3, DoorClosed = 4, DoorOpen = 5, HatchClosed = 6, HatchOpen = 7;
}

/// <summary>What a structure does, a bit each (0112): the questions systems ask of a cell instead of naming its kind.</summary>
[Flags]
public enum Trait
{
    None = 0,
    /// <summary>Stops a body from every side: a hero, a creature, a stack.</summary>
    Blocks = 1,
    /// <summary>Holds what stands on it from above; a body below passes up through it unless it also <see cref="BlocksFromBelow"/>.</summary>
    Supports = 2,
    /// <summary>A hero climbs it; its top holds a hero who is not climbing.</summary>
    Climbable = 4,
    /// <summary>Airtight: closes a room.</summary>
    Seals = 8,
    /// <summary>Stops light and starlight.</summary>
    BlocksLight = 16,
    /// <summary>Carries a material (<see cref="Materials"/>): drawn, dug, rooted in, leaking and heard by it.</summary>
    Terrain = 32,
    /// <summary>An open doorway: air passes, and a room's edge runs through it.</summary>
    Opening = 64,
    /// <summary>A hero's body stops in it as in a wall, though it is stood on as a floor is: a shut hatch.</summary>
    BlocksFromBelow = 128,
}

/// <summary>Which doorways a structure is part of (0111): its cells open and shut, and join, with their own group's only.</summary>
public enum StructureGroup { None, Door, Hatch }

/// <summary>How a doorway's pieces join one another beyond the piece: a door's stacked segments are one doorway.</summary>
public enum Join { None, Column }

/// <summary>How the client draws a structure.</summary>
public enum StructureDraw
{
    None,
    /// <summary>Its material's picture, edged where it meets no other of its kind, with the ground's details and tops.</summary>
    Autotile,
    /// <summary><see cref="StructureSpec.Tile"/> cut over the world a cell at a time.</summary>
    Cut,
    /// <summary><see cref="StructureSpec.Tile"/> two cells wide, its columns paired from its row's left end, as its pieces are (0104).</summary>
    Paired,
    /// <summary>A doorway's panel by its shape (<c>DoorShape</c>).</summary>
    Door,
}

/// <summary>What a drag of a structure's card lays: a line of cells or pieces, or the rectangle round both ends.</summary>
public enum DragMode { Line, Area }

/// <summary>
/// One structure code's rules and looks (0112): its <see cref="Traits"/>, what a hull of it leaks (<see cref="Leak"/>; a terrain's
/// own by its material), the code it turns into when toggled, the doorway it is part of and how it joins, the build that places it
/// and how it is drawn and dragged.
/// </summary>
/// <param name="Char">Its character in an ASCII map (<see cref="Legend"/>), null for none.</param>
/// <param name="Means">What the map character means, for the legend.</param>
/// <param name="Toggle">The code it becomes when its doorway opens or shuts; its own for none.</param>
/// <param name="Build">What places it, by id: <c>wall</c> for walls (costing their material), else one of <see cref="Builds"/>.</param>
public sealed record StructureSpec(
    int Code,
    char? Char,
    string Means,
    Trait Traits,
    string? Build = null,
    int Leak = 0,
    int? Toggle = null,
    StructureGroup Group = StructureGroup.None,
    Join Join = Join.None,
    StructureDraw Draw = StructureDraw.None,
    string? Tile = null,
    DragMode Drag = DragMode.Line)
{
    public bool Has(Trait trait) => (Traits & trait) != 0;
}

/// <summary>Every structure code's spec (0112), a flat table indexed by code.</summary>
public static class Structures
{
    public static IReadOnlyList<StructureSpec> All => Table;

    private static readonly StructureSpec[] Table =
    [
        new(Structure.Empty, null, "nothing", Trait.None),
        new(Structure.Wall, '#', "hull wall, of the default hull material",
            Trait.Blocks | Trait.Seals | Trait.BlocksLight | Trait.Terrain, Build: "wall", Draw: StructureDraw.Autotile, Drag: DragMode.Area),
        new(Structure.Floor, '=', "floor: a platform heroes stand on and climb through from below",
            Trait.Supports, Build: "floor", Draw: StructureDraw.Cut, Tile: "floor"),
        new(Structure.Ladder, 'H', "ladder", Trait.Climbable, Build: "ladder", Draw: StructureDraw.Paired, Tile: "ladder"),
        new(Structure.DoorClosed, 'D', "door, closed", Trait.Blocks | Trait.Seals | Trait.BlocksLight, Build: "door",
            Leak: Air.DoorLeak, Toggle: Structure.DoorOpen, Group: StructureGroup.Door, Join: Join.Column, Draw: StructureDraw.Door),
        new(Structure.DoorOpen, 'd', "door, open", Trait.Opening, Build: "door",
            Leak: Air.DoorLeak, Toggle: Structure.DoorClosed, Group: StructureGroup.Door, Join: Join.Column, Draw: StructureDraw.Door),
        // A hatch differs from a door only in being climbed when open (0111): shut, a floor that stops a body from below.
        new(Structure.HatchClosed, 'h', "hatch, closed: a doorway lying in a deck",
            Trait.Supports | Trait.BlocksFromBelow | Trait.Seals | Trait.BlocksLight, Build: Builds.Hatch,
            Leak: Air.DoorLeak, Toggle: Structure.HatchOpen, Group: StructureGroup.Hatch, Draw: StructureDraw.Door),
        new(Structure.HatchOpen, 'o', "hatch, open", Trait.Opening | Trait.Climbable, Build: Builds.Hatch,
            Leak: Air.DoorLeak, Toggle: Structure.HatchClosed, Group: StructureGroup.Hatch, Draw: StructureDraw.Door),
    ];

    private static readonly Trait[] TraitsOf = Table.Select(s => s.Traits).ToArray();

    /// <summary>The spec of structure <paramref name="code"/>; throws for a code none has.</summary>
    public static StructureSpec Of(int code) => Table[code];

    /// <summary>Whether structure <paramref name="code"/> has <paramref name="trait"/>: one array read, for loops over whole layers.</summary>
    public static bool Is(int code, Trait trait) => (TraitsOf[code] & trait) != 0;

    /// <summary>Stood on or stopped at: <see cref="Trait.Blocks"/> or <see cref="Trait.Supports"/>.</summary>
    public static bool Solid(int code) => Is(code, Trait.Blocks | Trait.Supports);

    /// <summary>The code a build of <paramref name="build"/> places: the first with it, a doorway shut; null for one that places no structure.</summary>
    public static int? Placed(string build)
    {
        foreach (var s in Table)
            if (s.Build == build) return s.Code;
        return null;
    }

    /// <summary>The code map character <paramref name="c"/> draws, null for one that draws no structure.</summary>
    public static int? OfChar(char c)
    {
        foreach (var s in Table)
            if (s.Char == c) return s.Code;
        return null;
    }
}
