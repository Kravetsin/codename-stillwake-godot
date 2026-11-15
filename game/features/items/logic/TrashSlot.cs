namespace Stillwake.Game.Features.Items;

/// <summary>Where the trash lies in the bag (trash-slot): its last column, under the bag's last open slot.</summary>
public static class TrashSlot
{
    /// <summary>
    /// The empty cells before the trash in a grid of <paramref name="columns"/> holding <paramref name="open"/> open bag slots:
    /// it ends their last row, or a row of its own when that row is full.
    /// </summary>
    public static int Gaps(int open, int columns = Hotbar.Row) => (columns - 1 - open % columns + columns) % columns;
}
