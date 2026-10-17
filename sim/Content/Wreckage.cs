namespace Stillwake.Content;

/// <summary>
/// Wreckage (ADRs 0034, 0054, 0056): debris too big to catch by hand, broken by whatever deals damage. Each
/// kind has its hit points, and they alone say how hard it is: every hit takes the striking weapon's
/// <see cref="Weapon.Damage"/> off them, any weapon on any kind, and at none left it breaks into its
/// loot (<see cref="Loot"/>), floating where it was.
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
        // Rock from the band round the ship (0054): 12 s of the drill, 30 s of the axe.
        ("asteroid", 120),
        // Its kinds (0056): the bigger, the harder and the more rock; the rich one, ore-dense, more rock for its size.
        ("asteroidSmall", 50),
        ("asteroidLarge", 200),
        ("asteroidRich", 160),
        // Derelicts far out (0056), as hard as rock: 11 to 18 s of the drill, 28 to 45 s of the axe.
        ("engineBlock", 180),
        ("cargoHold", 140),
        ("podCluster", 110),
    ];

    /// <summary>The hit points of <paramref name="item"/>, or null when it is no wreckage but caught by hand.</summary>
    public static int? HpOf(string item)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Item == item) return All[i].Hp;
        return null;
    }
}
