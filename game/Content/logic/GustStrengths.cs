using Spec = Stillwake.Content;

namespace Stillwake.Game;

/// <summary>
/// How hard each action pushes the air round it (air-gusts): its strength, 1 a railgun's shot, and how far it is felt, in cells.
/// What it stirs is the surroundings' (<c>Gusts</c>).
/// </summary>
public static class GustStrengths
{
    /// <summary>A swung tool's stroke, from its blade as it lands; none for a running tool, whose work is seen all along.</summary>
    public static (double Strength, double Radius)? StrokeOf(string? tool) => Spec.Weapons.Of(tool) switch
    {
        var w when w == Spec.Weapons.Sledgehammer => (0.75, 6),
        var w when w == Spec.Weapons.Axe => (0.5, 5),
        var w when w == Spec.Weapons.Pickaxe => (0.45, 4.5),
        var w when w == Spec.Weapons.Spear => (0.3, 4),
        _ => null,
    };

    /// <summary>A railgun's shot round the shooter's feet, and along its barrel from the muzzle: a cannon's.</summary>
    public static readonly (double Strength, double Radius) RailFeet = (1, 7), RailMuzzle = (0.8, 5);
}
