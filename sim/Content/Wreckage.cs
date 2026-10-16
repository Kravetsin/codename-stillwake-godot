namespace Stillwake.Content;

/// <summary>
/// Wreckage (ADR 0034): debris too big to catch by hand, broken with an axe. Each kind has its hit
/// points; every hit takes the axe's <see cref="Axe.Damage"/> off them, and at none left it breaks into
/// its loot (<see cref="Loot"/>), floating where it was.
/// </summary>
public static class Wreckage
{
    /// <summary>Each kind of wreckage, by its item, and its hit points.</summary>
    public static readonly IReadOnlyList<(string Item, int Hp)> All =
    [
        ("hullSection", 15),
    ];

    /// <summary>The hit points of <paramref name="item"/>, or null when it is no wreckage but caught by hand.</summary>
    public static int? HpOf(string item)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Item == item) return All[i].Hp;
        return null;
    }
}

/// <summary>
/// The fire axe (ADRs 0034, 0039): what a hit with it takes off a wreck, how often a held button swings
/// it and how far its blade reaches.
/// It never wears out.
/// </summary>
public static class Axe
{
    /// <summary>The item that is the axe, crafted at the lifepod.</summary>
    public const string Item = "fireAxe";

    /// <summary>The hit points one hit takes off a wreck.</summary>
    public const int Damage = 2;

    /// <summary>Ticks between swings while the button is held: the client's pace, which the sim does not check (0034).</summary>
    public const int SwingTicks = 10;

    /// <summary>
    /// How far the blade reaches from the hero's shoulder, in thousandths of a cell: the arm and the axe's
    /// length. A wreck within it as the blade comes down is hit (0039); the client's word, as the pace is.
    /// </summary>
    public const int Reach = 2000;
}
