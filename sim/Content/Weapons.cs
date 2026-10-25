namespace Stillwake.Content;

/// <summary>
/// An item that deals damage (ADRs 0034, 0054): tools are weapons, and every one works alike on everything.
/// <paramref name="Damage"/> is what a hit takes off a wreck or a creature, <paramref name="BlockDamage"/> off a wall, a back
/// wall or a plant (the fan of rays): a weapon made to fight hits blocks far less than a tool. <paramref name="Pace"/> the ticks
/// between hits while the button is held, the client's pace, which the sim does not check (0034). A hit is a fan of
/// <paramref name="Rays"/> rays spread evenly over <paramref name="Arc"/> steps of a turn (<see cref="Headings"/>) round the
/// aim, each <paramref name="Range"/> thousandths of a cell long from the hand, each striking the first thing it meets
/// (the fan of rays). An electric weapon pays nothing a hit: its item draws its <c>Draw</c> every tick it runs (0055).
/// <paramref name="Effects"/> are what a hit does besides, each a chip on its card.
/// </summary>
public sealed record Weapon(string Item, int Damage, int BlockDamage, int Pace, int Arc, int Rays, int Range, IReadOnlyList<HitEffect> Effects);

/// <summary>
/// Everything that deals damage, by its item. A better weapon is more damage a second, nothing else: what
/// it may break is never limited, only how long it takes (0054).
/// </summary>
public static class Weapons
{
    /// <summary>
    /// The fire axe (0034): 2 a swing, 4 on blocks, a swing every half second, its wave a quarter turn wide and 3.5 cells long,
    /// 9 rays: a band of about 4 cells of a wall's face a swing, soil at a swing, stone at three, a 4×20 tunnel of it in 30 s.
    /// It never wears out.
    /// </summary>
    public static readonly Weapon Axe = new("fireAxe", Damage: 2, BlockDamage: 4, Pace: 10, Arc: 64, Rays: 9, Range: 3500, Effects: []);

    /// <summary>
    /// The drill (0054): 1 a hit, 4 on blocks, a hit every 2 ticks, its beam 8 steps (11°) wide and 5.5 cells long, 3 rays: one
    /// or two cells, soil at a hit, stone at three, a 4×20 tunnel of it in 16 s, on the belt's batteries; with the arc module
    /// fitted (0098), twice either for twice the draw.
    /// </summary>
    public static readonly Weapon Drill = new("drill", Damage: 1, BlockDamage: 4, Pace: 2, Arc: 8, Rays: 3, Range: 5500, Effects: []);

    public static readonly IReadOnlyList<Weapon> All = [Axe, Drill];

    /// <summary>The weapon that is <paramref name="item"/>, or null for an item that deals no damage.</summary>
    public static Weapon? Of(string? item)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Item == item) return All[i];
        return null;
    }
}

/// <summary>
/// Something a weapon's hit does besides its damage, named in the client's words <c>effect.&lt;Id&gt;</c> and
/// told in <c>effect.&lt;Id&gt;.about</c>; its card's chip is <paramref name="Colour"/> on
/// <paramref name="Ground"/>, "rrggbb" (the card mockup's fire was ff7a2e on 3a1a0c, its ice 6fd3ff on 0c2a3a).
/// </summary>
public sealed record HitEffect(string Id, string Colour, string Ground);
