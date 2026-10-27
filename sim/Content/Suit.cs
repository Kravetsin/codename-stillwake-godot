namespace Stillwake.Content;

/// <summary>
/// Suits (2026-10-27-suits-are-worn): items worn in their own equipment slot, each one's <c>Tank</c> the hero's whole
/// pool of air while worn. Bare, a hero holds <see cref="Crew.Lungs"/>, and where the air is not aired each breath
/// costs it <see cref="Crew.BareCold"/> more. The basic suit is free at the pod; later suits are rows of
/// <see cref="Items"/>.
/// </summary>
public static class Suit
{
    // Not consts: every constant of this table is a whole number (ContentTests).
    /// <summary>The basic suit, made for nothing at the lifepod.</summary>
    public static readonly string Basic = "suit";

    /// <summary>The kind of equipment slot a suit is worn in, of <see cref="Inventory.Equipment"/>.</summary>
    public static readonly string Slot = "suit";
}
