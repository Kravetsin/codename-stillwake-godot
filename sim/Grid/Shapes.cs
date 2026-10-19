namespace Stillwake.Sim.Grid;

/// <summary>
/// A wall's shape (0069), as stored in saves: never reuse or renumber a code. <see cref="None"/> is a whole wall;
/// the others keep the solid half-cell triangle in one corner, cut at 45°: ◢ <see cref="BottomRight"/>,
/// ◣ <see cref="BottomLeft"/>, ◤ <see cref="TopLeft"/>, ◥ <see cref="TopRight"/>. The geometry is in thousandths
/// of a cell, x right and y down from the cell's top-left, integers only, shared by the sim and the hero.
/// </summary>
public static class Shape
{
    public const int None = 0, BottomRight = 1, BottomLeft = 2, TopLeft = 3, TopRight = 4;

    /// <summary>Thousandths of a cell, the unit of the geometry.</summary>
    public const int Unit = 1000;

    /// <summary>Every shape a wall may have but whole, by code.</summary>
    public static readonly int[] Cut = [BottomRight, BottomLeft, TopLeft, TopRight];

    /// <summary>Whether the shape is a floor slope (◢ ◣): solid along its bottom, its diagonal a ground to walk on.</summary>
    public static bool IsFloor(int shape) => shape is BottomRight or BottomLeft;

    /// <summary>
    /// Whether the point (<paramref name="fx"/>, <paramref name="fy"/>), thousandths into the cell, is solid in a
    /// wall of <paramref name="shape"/>: a point stands for the thousandth square below and right of it, solid when
    /// its middle is on the solid side of the diagonal, so ◢ and ◣ mirror each other exactly.
    /// </summary>
    public static bool Solid(int shape, int fx, int fy) => shape switch
    {
        BottomRight => fx + fy >= Unit - 1,
        BottomLeft => fy >= fx,
        TopLeft => fx + fy <= Unit - 1,
        TopRight => fy <= fx,
        _ => true,
    };

    /// <summary>
    /// The first solid thousandth down the column <paramref name="fx"/> of a wall of <paramref name="shape"/>: where
    /// something resting on it there lies, below the cell's top. 0 for a whole wall and the ceiling shapes.
    /// </summary>
    public static int TopAt(int shape, int fx) => shape switch
    {
        BottomRight => Unit - 1 - fx,
        BottomLeft => fx,
        _ => 0,
    };

    /// <summary>The sides a wall of <paramref name="shape"/> fills whole, as <see cref="Doorways"/> numbers sides: those that seal and stop like a wall's.</summary>
    public static int ClosedSides(int shape) => shape switch
    {
        BottomRight => Doorways.Down | Doorways.Right,
        BottomLeft => Doorways.Down | Doorways.Left,
        TopLeft => Doorways.Up | Doorways.Left,
        TopRight => Doorways.Up | Doorways.Right,
        _ => Doorways.Up | Doorways.Right | Doorways.Down | Doorways.Left,
    };
}
