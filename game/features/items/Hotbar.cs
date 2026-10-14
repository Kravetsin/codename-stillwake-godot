namespace Stillwake.Game.Features.Items;

/// <summary>The inventory's first row, the hotbar (apps/game/src/features/items/hotbar.ts).</summary>
public static class Hotbar
{
    /// <summary>Slots in a row: the inventory's first row is the hotbar, chosen with keys 1 to 9 or the wheel.</summary>
    public const int Row = 9;

    /// <summary>The hotbar slot after one turn of the wheel: down the next, up the one before, round the row.</summary>
    public static int WheelSlot(int slot, int deltaY)
    {
        if (deltaY == 0) return slot;
        return (slot + (deltaY > 0 ? 1 : Row - 1)) % Row;
    }
}
