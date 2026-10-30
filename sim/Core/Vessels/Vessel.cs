using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>
/// A generated vessel described, before it is cells (generated-vessels): what <see cref="Shipyard.Describe"/> makes of a class and
/// a seed. Columns and rows are its own, from its box's top-left; the stern is on the left. Enemy vessels will read this, never
/// cells.
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
    IReadOnlyList<Vessel.Compartment> Compartments,
    IReadOnlyList<Vessel.Fitting> Fittings)
{
    public enum SectionKind { Stern, Mid, Bow, Bridge }

    /// <summary>
    /// A part of the hull along the width: its columns and rows; a bow's top and bottom move in by <c>TaperTop</c> and
    /// <c>TaperBottom</c> rows over its width, toward the tip. The stern holds the engine bells, outside the hull.
    /// </summary>
    public sealed record Section(SectionKind Kind, int Left, int Width, int Top, int Bottom, int TaperTop = 0, int TaperBottom = 0)
    {
        /// <summary>The hull's top and bottom rows in column <paramref name="x"/> of this section.</summary>
        public (int Top, int Bottom) RowsAt(int x)
        {
            int j = x - Left + 1;
            return (Top + TaperTop * j / Width, Bottom - TaperBottom * j / Width);
        }
    }

    /// <summary>A deck: its floor's row, the six open rows over it from <see cref="Top"/>.</summary>
    public sealed record Deck(int Floor)
    {
        public int Top => Floor - Vessels.Open;
    }

    /// <summary>A ladder two columns wide from <c>Column</c>, its rows <c>Top</c> to <c>Bottom</c>, an even count: whole pieces.</summary>
    public sealed record Shaft(int Column, int Top, int Bottom);

    /// <summary>A door through the armour, two columns from <c>X</c>, a deck's six rows from <c>Y</c>: the way in.</summary>
    public sealed record Door(int X, int Y);

    /// <summary>
    /// A part of a deck the generator furnishes: its columns from <c>Left</c> and its open rows from <c>Top</c>; <c>Deck</c> -1 for
    /// the bridge's. Its first two columns are kept for a shaft. <c>Interior</c> the drawing it holds, none where none fits.
    /// </summary>
    public sealed record Compartment(int Deck, int Left, int Width, int Top, VesselRole Role, string? Interior);

    /// <summary>A machine from its top-left, and what it holds: a crate's loot.</summary>
    public sealed record Fitting(string Kind, int X, int Y, IReadOnlyList<(string Item, int Count)> Loot);
}

/// <summary>
/// What a vessel's box holds, a thing a cell from its top-left as a grid op places it: a layer, a kind and a wall's material,
/// and what a crate holds.
/// </summary>
public sealed record VesselPart(int X, int Y, string Layer, string Kind, string? Material = null, IReadOnlyList<(string Item, int Count)>? Holds = null);
