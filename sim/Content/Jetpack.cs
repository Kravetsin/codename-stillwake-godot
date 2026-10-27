namespace Stillwake.Content;

/// <summary>
/// The oxygen jetpack (ADR 0047), worn on the back: in space, Shift held vents the suit's oxygen to thrust
/// the hero. The sim reads <see cref="TicksPerUnit"/>; the flight is the client's, its speeds in thousandths
/// of a cell per controller step, 60 a second, as <see cref="Hero"/>'s are.
/// </summary>
public static class Jetpack
{
    // Not a const: every constant of this table is a whole number (ContentTests).
    /// <summary>The item that is the jetpack, crafted at the lifepod.</summary>
    public static readonly string Item = "jetpack";

    /// <summary>Ticks of thrust a unit of the hero's air lasts: the basic suit's tank thrusts some 22 seconds, bare lungs ten.</summary>
    public const int TicksPerUnit = 10;

    /// <summary>What thrust adds to the hero's speed a step: full speed in about half a second.</summary>
    public const int Accel = 16;

    /// <summary>The fastest the jetpack flies: 30 cells a second.</summary>
    public const int Top = 500;

    /// <summary>What a coast loses a step without thrust: some fourteen cells from full speed to a stop.</summary>
    public const int Drag = 8;
}
