namespace Stillwake.Content;

/// <summary>
/// What a burst does where it goes off (blasting-charge, unified-damage): the shared wave of <paramref name="Power"/> in tenths, a hit's
/// damage in every direction, spent at <see cref="Damage.Step"/> a cell and what is left of each wall's strength, 7/5 of that aslant,
/// within <paramref name="Reach"/> cells of it (round-blasts); a player's own ship at its <see cref="Damage.OwnShipTakes"/> share. Every wreck
/// whose box comes within <paramref name="Wave"/> cells takes <paramref name="WreckDamage"/>; every awake hero and live creature within
/// it and in its sight <paramref name="Body"/>, the one who set it off too: the only damage a burst does to a body. Debris bodies within
/// <paramref name="Push"/> cells are pushed away (debris-bursts, <see cref="Debris.BurstPush"/>): in space the wave carries far past
/// what it digs.
/// </summary>
public sealed record Blast(int Power, int Reach, int Wave, int Push, int WreckDamage, int Body);

/// <summary>The bursts of what is thrown, fired or broken.</summary>
public static class Blasting
{
    /// <summary>
    /// The blasting charge's: 500 to spend, so solid stone (122 a cell) opens four cells out and cracks the next, soil (42 a cell)
    /// the whole circle of 8, the ruins' alloy (600) only cracks to 496, opened by a second; a scrap wall's cell struck breaks
    /// and its neighbours crack; aboard, a tenth: a scrap wall cracks by 49, nine to break it. 120 off each wreck within 12, most at once;
    /// 10 off each body within 12, its thrower's too; debris pushed within 30.
    /// </summary>
    public static readonly Blast Charge = new(Power: 500, Reach: 8, Wave: 12, Push: 30, WreckDamage: 120, Body: 10);

    /// <summary>
    /// The catcher's shell (catcher-combat): 150 to spend, so a stone cell opens and its neighbours crack, soil some three cells round; 30 off
    /// each wreck within 3, 3 off each body; debris pushed within 6.
    /// </summary>
    public static readonly Blast Shell = new(Power: 150, Reach: 3, Wave: 3, Push: 6, WreckDamage: 30, Body: 3);

    /// <summary>
    /// The catcher's missile (catcher-missile): 450 to spend, so a scrap hull's cell struck breaks (the room vents) and iron cracks; 60 off
    /// each wreck within 4, 6 off each body; debris pushed within 10.
    /// </summary>
    public static readonly Blast Missile = new(Power: 450, Reach: 4, Wave: 4, Push: 10, WreckDamage: 60, Body: 6);

    /// <summary>
    /// A fuel drum's (<c>Wreckage.Bursts</c>), under the charge's as drums are free: 300 to spend, short of a scrap hull cell's 400, so the
    /// cell it meets cracks and holds, digging 6 round; 60 off each wreck within 12, past every stream wreck's hit points (the shield slab's
    /// 31 the most), so a drum among a cluster salvages it at once; 8 off each body within 12, twice a breaker's 4, so a drum is a danger
    /// and a bait; debris pushed within 30.
    /// </summary>
    public static readonly Blast Drum = new(Power: 300, Reach: 6, Wave: 12, Push: 30, WreckDamage: 60, Body: 8);

    /// <summary>
    /// A burst with no <see cref="Blast"/> of its own, a meteor's or a lightning strike's, pushes debris within this many times its
    /// <c>Reach</c>: 3, so a meteor's 4 stirs the pieces 12 cells round where it struck the hull, well short of a charge's 30.
    /// </summary>
    public const int PushPerReach = 3;
}
