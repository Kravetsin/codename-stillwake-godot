namespace Stillwake.Content;

/// <summary>
/// An item that deals damage (ADRs 0034, 0054): tools are weapons, and every one works alike on everything.
/// <paramref name="Damage"/> is what a hit takes off a wreck or a creature, <paramref name="BlockDamage"/> (in tenths) off a wall, a back
/// wall or a plant (the fan of rays): a weapon made to fight hits blocks far less than a tool. <paramref name="Pace"/> the ticks
/// between hits while the button is held, the client's pace, which the sim does not check (0034). A hit is a fan of
/// <paramref name="Rays"/> rays spread evenly over <paramref name="Arc"/> steps of a turn (<see cref="Headings"/>) round the
/// aim, each <paramref name="Range"/> thousandths of a cell long from the hand, each striking the first thing it meets
/// (the fan of rays). An electric weapon pays nothing a hit: its item draws its <c>Draw</c> every tick it runs (0055).
/// <paramref name="Effects"/> are what a hit does besides, each a chip on its card. A wall or piece a hit breaks passes
/// <paramref name="Carries"/> percent of what went over its strength, rounded down, to the cell behind it (unified-damage).
/// </summary>
public sealed record Weapon(string Item, int Damage, int BlockDamage, int Pace, int Arc, int Rays, int Range, IReadOnlyList<HitEffect> Effects, int Carries = 0);

/// <summary>
/// Everything that deals damage, by its item. A better weapon is more damage a second, nothing else: what
/// it may break is never limited, only how long it takes (0054).
/// </summary>
public static class Weapons
{
    /// <summary>
    /// The fire axe (0034): 2 a swing, 4 on blocks, a swing every half second, its wave a quarter turn wide and 4.5 cells long,
    /// 9 rays: a band of about 4 cells of a wall's face a swing, soil at a swing, stone at three, a 4×20 tunnel of it in 30 s.
    /// It never wears out.
    /// </summary>
    public static readonly Weapon Axe = new("fireAxe", Damage: 2, BlockDamage: 40, Pace: 10, Arc: 64, Rays: 9, Range: 4500, Effects: []);

    /// <summary>
    /// The drill (0054): 1 a hit, 4 on blocks, a hit every 2 ticks, its fan an eighth of a turn wide and 5.5 cells from the shoulder, a cell and a half past its bit (tool-size),
    /// 7 rays: about the hero's height of a face, soil at a hit, stone at three, on the
    /// belt's batteries; with the arc module fitted (0098), twice either for twice the draw.
    /// </summary>
    public static readonly Weapon Drill = new("drill", Damage: 1, BlockDamage: 40, Pace: 2, Arc: 32, Rays: 7, Range: 5500, Effects: []);

    /// <summary>
    /// The angle grinder (salvage tools): 3 a hit every 4 ticks, 15 a second on wrecks, half again the drill's, but 1 on blocks, 5 a
    /// second; its fan 87° wide and 5 cells from the shoulder, a cell and a half past its wheel (tool-size), 11 rays: a wreck tool, not a
    /// digger; on the belt's batteries; with the arc fitted, twice either.
    /// </summary>
    public static readonly Weapon Grinder = new("grinder", Damage: 3, BlockDamage: 10, Pace: 4, Arc: 62, Rays: 11, Range: 5000, Effects: []);

    /// <summary>The spear (salvage tools): 3 a thrust every 12 ticks, 5 a second, a little over the axe's, 6 cells out, past every swung tool's; the arc doubles it for no power.</summary>
    public static readonly Weapon Spear = new("spear", Damage: 3, BlockDamage: 20, Pace: 12, Arc: 8, Rays: 3, Range: 6000, Effects: []);

    /// <summary>What marks a weapon whose cuts give more loot (salvage tools): its card's row, and Debris's test of it.</summary>
    public static readonly HitEffect Salvage = new("salvage");

    /// <summary>
    /// The plasma cutter (salvage tools): 1 a hit every 4 ticks, 5 a second, nothing on blocks; a wreck it took its hit points
    /// off drops half again its loot by that share (Debris); its jet reaches 8 cells, the farthest of the hand tools; on the belt's batteries.
    /// </summary>
    public static readonly Weapon PlasmaCutter = new("plasmaCutter", Damage: 1, BlockDamage: 0, Pace: 4, Arc: 8, Rays: 3, Range: 8000, Effects: [Salvage]);

    /// <summary>
    /// The sledgehammer (salvage tools): 12 a blow every 1.8 s (swing-motions: never an arm idle over a second), 6.7 a second, more
    /// than the axe's 4, and each blow bursts every wreck of 12 hit points or fewer at once; 6 on blocks, under the weakest ground and plant, so a wall still cracks first; its fan 135° wide and
    /// 4.5 cells from the shoulder as the axe's (tool-size), 13 rays, wider than the axe's: a heavy sweep.
    /// </summary>
    public static readonly Weapon Sledgehammer = new("sledgehammer", Damage: 12, BlockDamage: 60, Pace: 36, Arc: 96, Rays: 13, Range: 4500, Effects: []);

    /// <summary>
    /// The pickaxe (the-pickaxe): the drill's fallback underground, needing no charge. 75 on blocks a blow every 6 ticks, 250 a second,
    /// near two thirds of the drill's 400 and three times the axe's 80: fresh stone and ore at the second blow, 0.6 s, where the
    /// drill takes 0.3 s and the axe 1.5 s; never at the first, so a wall still cracks first, nor a bush (80). 70% of what a broken
    /// cell leaves goes to the one behind (unified-damage): digging stone straight on, every fourth cell falls at one blow; at
    /// half, the carry would settle under what a second-blow cell needs.
    /// </summary>
    public static readonly Weapon Pickaxe = new("pickaxe", Damage: 1, BlockDamage: 75, Pace: 6, Arc: 48, Rays: 9, Range: 4500, Effects: [], Carries: 70);

    public static readonly IReadOnlyList<Weapon> All = [Axe, Drill, Grinder, Spear, PlasmaCutter, Sledgehammer, Pickaxe];

    /// <summary>What a cut wreck's loot comes to when its bonus is won, in thousandths of each line, rounded down: half again.</summary>
    public const int CutBonus = 1500;

    /// <summary>Whether <paramref name="weapon"/>'s cuts give a wreck more loot: it carries <see cref="Salvage"/>.</summary>
    public static bool Salvages(Weapon weapon) => weapon.Effects.Contains(Salvage);

    /// <summary>The weapon that is <paramref name="item"/>, or null for an item that deals no damage.</summary>
    public static Weapon? Of(string? item)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Item == item) return All[i];
        return null;
    }
}

/// <summary>
/// Something a weapon's hit does besides its damage: a row of its card in the client, the cutter's loot (2026-11-07-tool-card-rows).
/// </summary>
public sealed record HitEffect(string Id);
