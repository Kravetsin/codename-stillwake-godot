using Spec = Stillwake.Content;

namespace Stillwake.Game.Shared;

/// <summary>How large every item slot is drawn: a setting of this machine's player (Esc → Settings).</summary>
public enum SlotScale
{
    /// <summary>40 px, the 16 px art at ×2.</summary>
    Normal,
    /// <summary>56 px, the art at ×3.</summary>
    Large,
}

/// <summary>How a slot holding an item is tinted, by what the item is.</summary>
public enum SlotTint
{
    /// <summary>A resource, a part, a canister: plain grey.</summary>
    Plain,
    /// <summary>A tool (<see cref="ItemCards.IsTool"/>): amber, as a tool's recipe and card are.</summary>
    Tool,
    /// <summary>Worn (<c>ItemSpec.Equip</c>: the jetpack, the battery): the equipment's green.</summary>
    Worn,
}

/// <summary>
/// Every item slot's size, and the HUD laid out from it (0058): one value, read from <see cref="Ui.SlotScale"/>,
/// so the hotbar, the inventory, equipment, crates, machines, the Fabricator and the stack in hand agree.
/// </summary>
public static class Slots
{
    /// <summary>The icon's inset from a slot's edge, the same at every size.</summary>
    public const int Inset = 4;

    /// <summary>The gap between slots in a grid.</summary>
    public const int Gap = 4;

    /// <summary>The screen's margin to the hotbar, the gauges, the inventory and the equipment.</summary>
    public const int Margin = 12;

    /// <summary>A slot's side in pixels.</summary>
    public static int SizeOf(SlotScale scale) => scale == SlotScale.Large ? 56 : 40;

    /// <summary>An icon's side in pixels: a whole multiple of the 16 px art, so no pixel is drawn wider than its neighbour.</summary>
    public static int IconOf(SlotScale scale) => SizeOf(scale) - 2 * Inset;

    /// <summary>The top of the suit's and the energy gauges: under the hotbar's panel (a slot and 4 px of padding round it) with 8 px between.</summary>
    public static float GaugeTop(int slot) => Margin + slot + 2 * 4 + 8;

    /// <summary>The top of the open inventory's grid and of the equipment: under the 30 px gauges with 18 px between.</summary>
    public static float InventoryTop(int slot) => GaugeTop(slot) + 30 + 18;

    /// <summary>How a slot holding <paramref name="item"/> is tinted.</summary>
    public static SlotTint TintOf(string item) =>
        ItemCards.IsTool(item) ? SlotTint.Tool : Spec.Items.Get(item).Equip != null ? SlotTint.Worn : SlotTint.Plain;

    /// <summary>The setting's word in the settings file.</summary>
    public static string NameOf(SlotScale scale) => scale == SlotScale.Large ? "large" : "normal";

    /// <summary>The setting a word in the settings file names; none, or a word this build does not know, is <see cref="SlotScale.Normal"/>.</summary>
    public static SlotScale ScaleNamed(string? name) => name == "large" ? SlotScale.Large : SlotScale.Normal;
}
