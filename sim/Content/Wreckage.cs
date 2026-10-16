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
        ("cargoContainer", 11),
        ("machineBlock", 19),
        ("cableRack", 9),
        ("tankCluster", 7),
        ("shieldSlab", 31),
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
/// The fire axe (ADRs 0034, 0039): what a hit with it takes off a wreck and how often a held button
/// swings it; how far its blade reaches is the client's picture of it (0040). It never wears out.
/// </summary>
public static class Axe
{
    /// <summary>The item that is the axe, crafted at the lifepod.</summary>
    public const string Item = "fireAxe";

    /// <summary>The hit points one hit takes off a wreck.</summary>
    public const int Damage = 2;

    /// <summary>Ticks between swings while the button is held: the client's pace, which the sim does not check (0034).</summary>
    public const int SwingTicks = 10;

    /// <summary>What a hit does besides its damage: none yet. The axe's card shows each as a chip in its colours.</summary>
    public static readonly IReadOnlyList<HitEffect> Effects = [];
}

/// <summary>
/// Something a tool's hit does besides its damage, named in the client's words <c>effect.&lt;Id&gt;</c> and
/// told in <c>effect.&lt;Id&gt;.about</c>; its card's chip is <paramref name="Colour"/> on
/// <paramref name="Ground"/>, "rrggbb" (the card mockup's fire was ff7a2e on 3a1a0c, its ice 6fd3ff on 0c2a3a).
/// </summary>
public sealed record HitEffect(string Id, string Colour, string Ground);
