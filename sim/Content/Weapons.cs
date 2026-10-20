namespace Stillwake.Content;

/// <summary>
/// An item that deals damage (ADRs 0034, 0054): tools are weapons, and every one works alike on everything.
/// <paramref name="Damage"/> is what a hit takes off a wreck's hit points; <paramref name="Pace"/> the ticks
/// between hits while the button is held, the client's pace, which the sim does not check (0034). An electric
/// weapon pays nothing a hit: its item draws its <c>Draw</c> every tick it runs (0055).
/// <paramref name="Effects"/> are what a hit does besides, each a chip on its card.
/// </summary>
public sealed record Weapon(string Item, int Damage, int Pace, IReadOnlyList<HitEffect> Effects);

/// <summary>
/// Everything that deals damage, by its item. A better weapon is more damage a second, nothing else: what
/// it may break is never limited, only how long it takes (0054).
/// </summary>
public static class Weapons
{
    /// <summary>The fire axe (0034, 0039, 0040): 2 a swing, a swing every half second, 4 a second. It never wears out.</summary>
    public static readonly Weapon Axe = new("fireAxe", Damage: 2, Pace: 10, Effects: []);

    /// <summary>The drill (0054): 1 a hit, a hit every 2 ticks, 10 a second, on the belt's batteries.</summary>
    public static readonly Weapon Drill = new("drill", Damage: 1, Pace: 2, Effects: []);

    /// <summary>
    /// The arc drill (0075): the drill with arcs crawling on its coil, half as much damage a second again, 15: 3 a hit, a
    /// hit every 4 ticks, drawing as the drill does. Never made: tech to be found in planet ruins (<see cref="Found"/>).
    /// </summary>
    public static readonly Weapon ArcDrill = new("arcDrill", Damage: 3, Pace: 4, Effects: []);

    public static readonly IReadOnlyList<Weapon> All = [Axe, Drill, ArcDrill];

    /// <summary>
    /// The weapons no station makes: found, not crafted; till the planet ruins they come from are in the game, only a dev
    /// world (the testbed's crate, <c>--arc-drill</c>) holds one.
    /// </summary>
    public static readonly IReadOnlyList<Weapon> Found = [ArcDrill];

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
