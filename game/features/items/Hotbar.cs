namespace Stillwake.Game.Features.Items;

/// <summary>The inventory's first row, the hotbar (apps/game/src/features/items/hotbar.ts).</summary>
public static class Hotbar
{
    /// <summary>Slots in a row: the inventory's first row is the hotbar, chosen with keys 1 to 9 or the wheel.</summary>
    public const int Row = Content.Inventory.Hotbar;

    /// <summary>The digit in hotbar slot <paramref name="slot"/>'s corner: the key that chooses it, 1 for the first.</summary>
    public static string KeyOf(int slot) => (slot + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The hotbar slot a digit key chooses, 1 for the first, the inverse of <see cref="KeyOf"/>; none for any other digit.</summary>
    public static int? SlotOfDigit(int digit) => digit is >= 1 and <= Row ? digit - 1 : null;

    /// <summary>The hotbar slot after one turn of the wheel: down the next, up the one before, round the row.</summary>
    public static int WheelSlot(int slot, int deltaY)
    {
        if (deltaY == 0) return slot;
        return (slot + (deltaY > 0 ? 1 : Row - 1)) % Row;
    }
}
