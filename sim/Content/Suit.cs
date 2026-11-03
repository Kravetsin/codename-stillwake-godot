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

    /// <summary>
    /// The regen suit (2026-11-03-the-regen-suit): its mode on, it gives back after each breath what the breath took from the
    /// helmet's air, paid from the belt; its blueprint sold on the black market.
    /// </summary>
    public static readonly string Regen = "regenSuit";

    /// <summary>The most air the regen suit gives back a breath, in units.</summary>
    public const int RegenMost = 1;

    /// <summary>What each unit the regen suit gives back costs the belt, in power-ticks (<see cref="Charge"/>): a battery's 1800 make 120.</summary>
    public const int RegenCost = 15;
}

/// <summary>A suit's helmet (<see cref="Suit"/>): what holds the hero's air.</summary>
public static class Helmet
{
    // Not consts: every constant of this table is a whole number (ContentTests).
    /// <summary>The basic helmet, made for nothing at the lifepod.</summary>
    public static readonly string Basic = "helmet";

    /// <summary>The heavy helmet (2026-11-03-the-heavy-helmet): more than twice the basic's air, for the jetpack's long flights; its blueprint sold on the black market.</summary>
    public static readonly string Heavy = "heavyHelmet";

    /// <summary>The kind of equipment slot a helmet is worn in: the head.</summary>
    public static readonly string Slot = "head";

    /// <summary>The kind of slot for what is fixed onto whatever helmet is worn: a lamp now, later a sensor or the like.</summary>
    public static readonly string Mount = "mount";
}
