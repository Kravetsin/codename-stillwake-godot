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
/// <paramref name="Pace"/> ticks, the client's pace, which the sim does not check (0034).
/// </summary>
public sealed record Gun(string Item, IReadOnlyList<string> Ammo, int Damage, int BlockDamage, int Knock, int Speed, int Reach, int Pace);

/// <summary>Everything fired, and the guns that fire it.</summary>
public static class Shots
{
    /// <summary>The nail: 2 on its gun's damage, nothing on its block damage; weight 7, 294 a tick on the first planet.</summary>
    public static readonly Projectile Nail = new("nail", Damage: 2, BlockDamage: 0, Weight: 7, HitsBodies: true);

    /// <summary>
    /// The nailer: 4 and its nail's 2, 6 a hit; 1 off a block; a full knock; 60 cells a second, a nail at 45° on the first planet
    /// landing about 30 cells out; gone at 288 cells, a screen's half and a whole screen past it at the farthest zoom (192 cells
    /// across); a shot every 3 s.
    /// </summary>
    public static readonly Gun Nailer = new("nailer", ["nail"], Damage: 4, BlockDamage: 1, Knock: 10, Speed: 3000, Reach: 288, Pace: 60);

    /// <summary>
    /// The guard's bolt, struck by a <see cref="Creatures.Shoot"/> as an arc, never flown: four units of a suit off each hero it
    /// crosses, as that hero's client sees it; nothing stops it but a solid cell, and it breaks none.
    /// </summary>
    public static readonly Projectile Bolt = new("bolt", Damage: 4, BlockDamage: 0, Weight: 0, HitsBodies: false);

    public static readonly IReadOnlyList<Projectile> Projectiles = [Nail, Bolt];

    public static readonly IReadOnlyList<Gun> Guns = [Nailer];

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

    /// <summary>What a hit of <paramref name="projectile"/> fired from <paramref name="gun"/> (none: a creature's, its own alone) takes off a creature or a wreck.</summary>
    public static int DamageOf(string? gun, string projectile) => (GunOf(gun)?.Damage ?? 0) + Get(projectile).Damage;

    /// <summary>What a hit of <paramref name="projectile"/> fired from <paramref name="gun"/> takes off a wall or a built piece.</summary>
    public static int BlockDamageOf(string? gun, string projectile) => (GunOf(gun)?.BlockDamage ?? 0) + Get(projectile).BlockDamage;
}
