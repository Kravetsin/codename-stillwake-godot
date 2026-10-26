namespace Stillwake.Content;

/// <summary>
/// What flies as a shot (shots-are-the-sims): <paramref name="Speed"/> thousandths of a cell a tick at launch;
/// <paramref name="Weight"/> how many times <see cref="Creatures.Gravity"/> pulls it on a planet, 0 for none; gone after
/// <paramref name="Reach"/> cells' worth of flight. A hit takes <paramref name="Damage"/> off a creature or a wreck,
/// <paramref name="BlockDamage"/> off a wall or a built piece, and knocks by <paramref name="Pace"/> as a melee weapon's
/// hit does (<see cref="Creatures.KnockPace"/>). <paramref name="HitsBodies"/>: whether creatures and wrecks stop it, or
/// solid cells alone.
/// </summary>
public sealed record Projectile(string Id, int Speed, int Weight, int Reach, int Damage, int BlockDamage, int Pace, bool HitsBodies);

/// <summary>
/// A hero's gun: the item <paramref name="Item"/>, firing one <paramref name="Ammo"/> a shot as <paramref name="Projectile"/>,
/// a shot every <paramref name="Pace"/> ticks: the client's pace, which the sim does not check (0034).
/// </summary>
public sealed record Gun(string Item, string Ammo, Projectile Projectile, int Pace);

/// <summary>Everything fired, and the guns that fire it.</summary>
public static class Shots
{
    /// <summary>
    /// The nail: 60 cells a second; weight 7, 294 a tick on the first planet, about 30 cells at 45°; gone at 288 cells, a
    /// screen's half and a whole screen past it at the farthest zoom (192 cells across). 6 off a creature or a wreck, 1 off a
    /// block, a full knock.
    /// </summary>
    public static readonly Projectile Nail = new("nail", Speed: 3000, Weight: 7, Reach: 288, Damage: 6, BlockDamage: 1, Pace: 10, HitsBodies: true);

    /// <summary>The nailer: one nail a shot, a shot every 3 s.</summary>
    public static readonly Gun Nailer = new("nailer", "nail", Nail, Pace: 60);

    public static readonly IReadOnlyList<Projectile> Projectiles = [Nail];

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
}
