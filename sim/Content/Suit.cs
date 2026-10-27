namespace Stillwake.Content;

/// <summary>
/// Suits (2026-10-27-suits-are-worn): a helmet and a body, each an item worn in its own equipment slot, mixed as the
/// player likes. The helmet holds the air: its <c>Tank</c> is the hero's whole pool while worn, <see cref="Crew.Lungs"/>
/// without one. The body keeps the cold out: without one, each breath where the air is not aired costs
/// <see cref="Crew.BareCold"/> more. The basic helmet and body are free at the pod; later ones are rows of <see cref="Items"/>.
/// </summary>
public static class Suit
{
    // Not consts: every constant of this table is a whole number (ContentTests).
    /// <summary>The basic body, made for nothing at the lifepod.</summary>
    public static readonly string Basic = "suit";

    /// <summary>The kind of equipment slot a body is worn in, of <see cref="Inventory.Equipment"/>.</summary>
    public static readonly string Slot = "suit";
}

/// <summary>A suit's helmet (<see cref="Suit"/>): what holds the hero's air.</summary>
public static class Helmet
{
    // Not consts: every constant of this table is a whole number (ContentTests).
    /// <summary>The basic helmet, made for nothing at the lifepod.</summary>
    public static readonly string Basic = "helmet";

    /// <summary>The kind of equipment slot a helmet is worn in: the head.</summary>
    public static readonly string Slot = "head";

    /// <summary>The kind of slot for what is fixed onto any helmet: the lamp.</summary>
    public static readonly string Fitting = "lamp";
}
