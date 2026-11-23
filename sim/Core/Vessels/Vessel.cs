using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>
/// A generated vessel described, before it is cells (generated-vessels): what <see cref="Shipyard.Describe"/> makes of a class and
/// a seed. Columns and rows are its own, from its box's top-left, which holds what stands outside its hull too; the stern is on the
/// left. Enemy vessels will read this, never cells.
/// </summary>
public sealed record Vessel(
    string Class,
    uint Seed,
    int Width,
    int Height,
    IReadOnlyList<Vessel.Section> Sections,
    IReadOnlyList<Vessel.Deck> Decks,
    IReadOnlyList<Vessel.Shaft> Shafts,
    IReadOnlyList<Vessel.Door> Doors,
    IReadOnlyList<Vessel.Hatch> Hatches,
    IReadOnlyList<Vessel.Compartment> Compartments,
    IReadOnlyList<Vessel.Fitting> Fittings,
    IReadOnlyList<Vessel.Fitting> Outside)
{
    /// <summary>
    /// The body, the bow and a raised bridge are the hull and its rooms; a module, a rib and the housing behind the bridge are solid
    /// hull added round them (derelict-looks), never cutting into it. A tier is a raised room but the bridge (big-vessels): a liner's
    /// promenade and the dome on it.
    /// </summary>
    public enum SectionKind { Body, Bow, Bridge, Module, Rib, Housing, Tier }

    /// <summary>
    /// A part of the hull along the width: its columns and rows. A bow keeps its rows for <c>Flat</c> columns, then its top falls a
    /// row and its bottom rises one every <c>Run</c> columns, <c>Fall</c> and <c>Rise</c> rows in all, toward the tip.
    /// </summary>
    public sealed record Section(SectionKind Kind, int Left, int Width, int Top, int Bottom, int Flat = 0, int Run = 0, int Fall = 0, int Rise = 0)
    {
        /// <summary>The hull's top and bottom rows in column <paramref name="x"/> of this section.</summary>
        public (int Top, int Bottom) RowsAt(int x)
        {
            int steps = Run == 0 || x - Left < Flat ? 0 : (x - Left - Flat) / Run + 1;
            return (Top + Math.Min(steps, Fall), Bottom - Math.Min(steps, Rise));
        }
    }

    /// <summary>A deck: its floor's row, the six open rows over it from <see cref="Top"/>.</summary>
    public sealed record Deck(int Floor)
    {
        public int Top => Floor - Vessels.Open;
    }

    /// <summary>A ladder two columns wide from <c>Column</c>, its rows <c>Top</c> to <c>Bottom</c>, an even count: whole pieces.</summary>
    public sealed record Shaft(int Column, int Top, int Bottom);

    /// <summary>
    /// A door two columns from <c>X</c>, a deck's six rows from <c>Y</c>: the way in through the armour, always shut, or one between
    /// two rooms, <c>Open</c> as it was left.
    /// </summary>
    public sealed record Door(int X, int Y, bool Open = false);

    /// <summary>A hatch two columns from <c>X</c> in the floor row <c>Y</c>: the way between two decks, or up to a raised bridge.</summary>
    public sealed record Hatch(int X, int Y);

    /// <summary>
    /// A room: its columns from <c>Left</c> past its door and its open rows from <c>Top</c>; <c>Deck</c> -1 for a raised room, -2 for
    /// one raised on it. <c>Interior</c> the drawing it holds from <c>Left</c>, the rest of its columns bare floor; empty for a room
    /// furnished by code (big-vessels). <c>Decks</c> it spans, from <c>Deck</c> down, with no floor between (a tall room).
    /// </summary>
    public sealed record Compartment(int Deck, int Left, int Width, int Top, VesselRole Role, string Interior, int Decks = 1)
    {
        /// <summary>Its open rows: a deck's, and a floor's and a deck's more for each deck past the first.</summary>
        public int Height => Vessels.Pitch * Decks - 1;

        /// <summary>The row of its floor, under its open rows.</summary>
        public int Floor => Top + Height;

        /// <summary>Whether cell (<paramref name="x"/>, <paramref name="y"/>) is one of its open cells.</summary>
        public bool Holds(int x, int y) => x >= Left && x < Left + Width && y >= Top && y < Floor;

        /// <summary>
        /// The compartment this is the bow half of, by index, where a break cut one in two (vessel-wrecks): listed after every
        /// whole one, its stern half keeping the index; -1 for any other.
        /// </summary>
        public int Of { get; init; } = -1;
    }

    /// <summary>A machine from its top-left, and what it holds: a crate's loot.</summary>
    public sealed record Fitting(string Kind, int X, int Y, IReadOnlyList<(string Item, int Count)> Loot);
}

/// <summary>
/// What a vessel's box holds, a thing a cell from its top-left as a grid op places it: a layer, a kind and a wall's material,
/// what a crate holds, and whether a door's piece stands open.
/// </summary>
public sealed record VesselPart(
    int X, int Y, string Layer, string Kind, string? Material = null, IReadOnlyList<(string Item, int Count)>? Holds = null, bool Open = false);
