using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Dev;

/// <summary>Where the dev panel's teleport puts the hero in a vessel beside the ship (generated-vessels).</summary>
public static class VesselDoor
{
    /// <summary>
    /// The cell a hero's feet stand in just inside the vessel's door, the first door cell of its box in reading order: on the deck
    /// past the door, two cells wide and a deck's six rows tall, and past the two columns a shaft may come down. One whose door a
    /// breach took, floating over its middle. None with no vessel out.
    /// </summary>
    public static int? Inside(IGridQueries grid)
    {
        if (grid.Reserved is not { } box) return null;
        int width = grid.Width;
        for (int y = box.Top; y <= box.Bottom; y++)
            for (int x = box.Left; x <= box.Right; x++)
                if (Doorways.IsDoor(grid.StructureAt(y * width + x)))
                    return (y + Content.Vessels.Open - 1) * width + x + 4;
        return (box.Top - 2) * width + box.Left + box.Width / 2;
    }
}
