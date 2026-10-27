namespace Stillwake.Content;

/// <summary>
/// What flies as a shot (shots-are-the-sims): what a hit adds to its gun's, <paramref name="Damage"/> off a creature or a wreck and
/// <paramref name="BlockDamage"/> off a wall or a built piece; <paramref name="Weight"/>, how many times <see cref="Creatures.Gravity"/>
/// pulls it on a planet, 0 for none; <paramref name="HitsBodies"/>, whether creatures and wrecks stop it, or solid cells alone. How
/// fast and far it flies is its gun's.
/// </summary>
public sealed record Projectile(string Id, int Damage, int BlockDamage, int Weight, bool HitsBodies);

/// <summary>
/// A hero's gun: the item <paramref name="Item"/>, firing one of <paramref name="Ammo"/> a shot, the first of them the hero carries; a
/// hit takes <paramref name="Damage"/> and <paramref name="BlockDamage"/> with its projectile's added (as Terraria's), and knocks by
/// <paramref name="Knock"/> as a melee weapon's pace does (<see cref="Creatures.KnockPace"/>); the shot leaves at
/// <paramref name="Speed"/> thousandths of a cell a tick, gone after <paramref name="Reach"/> cells' worth of flight; a shot every
/// <paramref name="Pace"/> ticks, the client's pace, which the sim does not check (0034); a shot that pierces (<paramref name="Pierces"/>)
/// flies on with both damages times <paramref name="PierceKeep"/> thousandths, rounded down, at each (2026-10-27-shots-pierce); its
/// projectile pulled by <paramref name="Fall"/> thousandths of its weight; <paramref name="Energy"/> power-ticks of the belt's charge a
/// shot, none for 0; held <paramref name="Charge"/> ticks before it fires, the client's word as the pace is, 0 firing on the click
/// (2026-10-27-the-railgun); out in space each shot kicks the hero back along its barrel by <paramref name="Recoil"/> thousandths of a
/// cell a step, fading by <see cref="Hero.KickKeep"/>, the client's alone as its movement is.
/// </summary>
public sealed record Gun(
    string Item, IReadOnlyList<string> Ammo, int Damage, int BlockDamage, int Knock, int Speed, int Reach, int Pace, int PierceKeep,
    Pierce Pierces = Pierce.Broken, int Fall = 1000, int Energy = 0, int Charge = 0, int Recoil = 0);

/// <summary>What a gun's shot flies on through: only what its hit breaks or kills (the nailer's), or everything it hits.</summary>
public enum Pierce { Broken, All }

/// <summary>Everything fired, and the guns that fire it.</summary>
public static class Shots
{
    /// <summary>The nail: 2 on its gun's damage, nothing on its block damage; weight 7, 294 a tick on the first planet.</summary>
    public static readonly Projectile Nail = new("nail", Damage: 2, BlockDamage: 0, Weight: 7, HitsBodies: true);

    /// <summary>
    /// The nailer: 4 and its nail's 2, 6 a hit; 1 off a block; a full knock; 60 cells a second, a nail at 45° on the first planet
    /// landing about 30 cells out; gone at 288 cells, a screen's half and a whole screen past it at the farthest zoom (192 cells
    /// across); a shot every 3 s; half kept through what a nail breaks: 6, 3, 1; in space a kick of 60, about a cell.
    /// </summary>
    public static readonly Gun Nailer = new("nailer", ["nail"], Damage: 4, BlockDamage: 1, Knock: 10, Speed: 3000, Reach: 288, Pace: 60, PierceKeep: 500,
        Recoil: 60);

    /// <summary>
    /// The railgun (2026-10-27-the-railgun): 22 and its nail's 2, 24 a hit, then 15, 9, 5, 3, 1 through everything it passes; 4 off a
    /// block, then 2, 1; a full knock; 300 cells a second, gone at 384 cells' worth, 25 ticks; a tenth of the nail's pull, 29 a tick
    /// on the first planet, under a cell's drop across 100; 120 of the belt's charge a shot, fifteen a battery; held 0.8 s to
    /// fire, then 2 s till the next; in space a kick of 220, some four cells back.
    /// </summary>
    public static readonly Gun Railgun = new("railgun", ["nail"], Damage: 22, BlockDamage: 4, Knock: 10, Speed: 15000, Reach: 384, Pace: 40,
        PierceKeep: 650, Pierces: Pierce.All, Fall: 100, Energy: 120, Charge: 16, Recoil: 220);

    /// <summary>
    /// The guard's bolt, struck by a <see cref="Creatures.Shoot"/> as an arc, never flown: four units of a suit off each hero it
    /// crosses, as that hero's client sees it; nothing stops it but a solid cell, and it breaks none.
    /// </summary>
    public static readonly Projectile Bolt = new("bolt", Damage: 4, BlockDamage: 0, Weight: 0, HitsBodies: false);

    public static readonly IReadOnlyList<Projectile> Projectiles = [Nail, Bolt];

    public static readonly IReadOnlyList<Gun> Guns = [Nailer, Railgun];

    /// <summary>The projectile <paramref name="id"/>; throws for none.</summary>
    public static Projectile Get(string id)
    {
        for (int i = 0; i < Projectiles.Count; i++)
            if (Projectiles[i].Id == id) return Projectiles[i];
        throw new InvalidOperationException($"shots: no projectile {id}");
    }

    /// <summary>The gun that is <paramref name="item"/>, or null for an item that fires nothing.</summary>
    public static Gun? GunOf(string? item)
    {
        for (int i = 0; i < Guns.Count; i++)
            if (Guns[i].Item == item) return Guns[i];
        return null;
    }

    /// <summary>
    /// What a hit of <paramref name="projectile"/> fired from <paramref name="gun"/> (none: a creature's, its own alone) takes off a
    /// creature or a wreck, after it <paramref name="pierced"/> that many things (<see cref="Pierce"/>).
    /// </summary>
    public static int DamageOf(string? gun, string projectile, int pierced = 0) =>
        Kept(GunOf(gun), (GunOf(gun)?.Damage ?? 0) + Get(projectile).Damage, pierced);

    /// <summary>What a hit of <paramref name="projectile"/> fired from <paramref name="gun"/> takes off a wall or a built piece, after it <paramref name="pierced"/> that many things.</summary>
    public static int BlockDamageOf(string? gun, string projectile, int pierced = 0) =>
        Kept(GunOf(gun), (GunOf(gun)?.BlockDamage ?? 0) + Get(projectile).BlockDamage, pierced);

    /// <summary><paramref name="damage"/> kept through <paramref name="pierced"/> things: times the gun's keep each, rounded down; no gun, no fold.</summary>
    private static int Kept(Gun? gun, int damage, int pierced)
    {
        if (gun == null) return damage;
        for (int i = 0; i < pierced && damage > 0; i++) damage = damage * gun.PierceKeep / 1000;
        return damage;
    }
}
