namespace Stillwake.Content;

/// <summary>
/// Temperature (2026-11-14-temperature-in-degrees): every cell's in whole degrees, a surface's by its kind's depth bands
/// (<see cref="Surfaces.Kind.Temperatures"/>); a hero withstands down to its body's <c>Low</c>, or <see cref="BareLow"/>
/// with none, and up to its <c>High</c>, or <see cref="BareHigh"/> (2026-11-14-heat-limits), the cape's <see cref="Cape.Warmth"/> lower
/// both. Each degree felt past either moves the hero's body (body-temperature): its own temperature, in hundredths of a degree,
/// kept by the hero's client and drawn toward a target off <see cref="Normal"/>; it passes out at <see cref="ColdDeath"/> or <see cref="HeatDeath"/>.
/// </summary>
public static class Temperature
{
    /// <summary>The coldest a hero with no body worn withstands.</summary>
    public const int BareLow = 0;

    /// <summary>The hottest a hero with no body worn withstands (2026-11-14-heat-limits): a body's comfort.</summary>
    public const int BareHigh = 30;

    /// <summary>An aired cell's, on any surface and at any depth: a sealed room is a shelter.</summary>
    public const int Room = 20;

    /// <summary>Space's at the field's line: an airless cell afloat there, colder past it toward <see cref="Deep"/>.</summary>
    public const int Space = -200;

    /// <summary>
    /// Space's at the world's edge, and any cell off the grid: past the field's line it falls straight to this (body-temperature).
    /// The body cools by the degrees felt (<see cref="Drift"/>), so no suit's wearer, however fast it flies, reaches the edge alive.
    /// </summary>
    public const int Deep = -2000;

    /// <summary>Space's by the ship: an airless cell afloat in the build zone, cooling to <see cref="Space"/> across the field.</summary>
    public const int Hull = -100;

    /// <summary>A well body's temperature, in hundredths of a degree: where it settles with nothing felt.</summary>
    public const int Normal = 3700;

    /// <summary>Hundredths of a degree a body settles off <see cref="Normal"/> for each degree felt past a limit.</summary>
    public const int Sway = 20;

    /// <summary>Thousandths of its gap to its target a body closes a second, compounded: a wider gap moves it faster, up to <see cref="Drift"/> away from <see cref="Normal"/>.</summary>
    public const int Pull = 250;

    /// <summary>
    /// The most thousandths of a degree a body moves away from <see cref="Normal"/> a second for each degree felt past a limit: 90
    /// past a basic suit at the field's line, 0.27° a second, 33 s to <see cref="ColdDeath"/>; deeper out faster. Back toward normal
    /// it is unbounded.
    /// </summary>
    public const int Drift = 3;

    /// <summary>The body's temperature, in hundredths of a degree, at or under which a hero passes out of the cold.</summary>
    public const int ColdDeath = 2800;

    /// <summary>The body's temperature, in hundredths of a degree, at or over which a hero passes out of the heat.</summary>
    public const int HeatDeath = 4200;

    /// <summary>
    /// Thousandths of its speed a hero moves on foot and works at with its body at <see cref="ColdDeath"/> or <see cref="HeatDeath"/>:
    /// the least, reached straight from whole at <see cref="Normal"/>, every tenth of a degree its share.
    /// </summary>
    public const int Least = 200;
}
