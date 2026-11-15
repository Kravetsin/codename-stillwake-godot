using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Research;

/// <summary>How large the board and its strip make a thing's picture: a tool's icon oversampled, so the board's zoom and a large screen keep it sharp.</summary>
public static class BoardPictures
{
    /// <summary>How much larger than it is laid out a tool's icon is baked: the board's most zoom, 1.5, on a screen twice the reference.</summary>
    public const int ToolOver = 3;

    /// <summary>
    /// The side to make <paramref name="id"/>'s picture at where it is laid out <paramref name="size"/> px wide, <paramref name="placement"/> what its
    /// recipe places: a held tool's icon <see cref="ToolOver"/> times larger; a blueprint, an upgrade or any other item at its size.
    /// </summary>
    public static int Side(string id, Placement? placement, int size) =>
        placement == null && Content.Upgrades.Of(id) == null && HeldLook.Of(id) != null ? size * ToolOver : size;
}
