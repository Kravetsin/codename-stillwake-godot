namespace Stillwake.Content;

/// <summary>
/// Temperature (2026-11-14-temperature-in-degrees): every cell's in whole degrees, a surface's by its kind's depth bands
/// (<see cref="Surfaces.Kind.Temperatures"/>); a hero withstands down to its body's <c>Low</c>, or <see cref="BareLow"/>
/// with none, the cape's <see cref="Cape.Warmth"/> lower; each degree past it costs its air <see cref="Bite"/> a breath.
/// </summary>
public static class Temperature
{
    /// <summary>The coldest a hero with no body worn withstands.</summary>
    public const int BareLow = 0;

    /// <summary>An aired cell's, on any surface and at any depth: a sealed room is a shelter.</summary>
    public const int Room = 20;

    /// <summary>Space's far from the ship: an airless cell afloat at and past the field's line, and any cell off the grid on any surface.</summary>
    public const int Space = -40;

    /// <summary>Space's by the ship: an airless cell afloat in the build zone, warming from <see cref="Space"/> across the field.</summary>
    public const int Hull = -20;

    /// <summary>Thousandths of a unit of air each degree past a hero's limit costs it a breath, flat: every degree the same.</summary>
    public const int Bite = 20;
}
