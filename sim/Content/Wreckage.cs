namespace Stillwake.Content;

/// <summary>
/// Wreckage (ADRs 0034, 0054): debris too big to catch by hand, broken by whatever deals damage. Each
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
    ];

    /// <summary>The hit points of <paramref name="item"/>, or null when it is no wreckage but caught by hand.</summary>
    public static int? HpOf(string item)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Item == item) return All[i].Hp;
        return null;
    }
}
