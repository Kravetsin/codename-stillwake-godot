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
/// The fire axe (ADR 0034): what a hit with it takes off a wreck, and how often a held button swings it.
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
}
