namespace Stillwake.Content;

/// <summary>
/// One rule for what a hit breaks (unified-damage, damage-scale): a tool's blow, a shot, a charge's and a meteor's burst each break a
/// cell when their damage covers what is left of its strength, in tenths, passing on what is over. These are a burst's numbers.
/// </summary>
public static class Damage
{
    /// <summary>What a burst spends crossing a cell to a side or up or down, 7/5 of it aslant (<c>Wave</c>, round-blasts): its falloff.</summary>
    public const int Step = 2;

    /// <summary>
    /// The share of a burst's damage a player's own ship takes from that player's burst, in thousandths (damage-scale): a tenth, so a
    /// charge thrown aboard cracks a scrap wall by 49 and nine break it. Meteors, and anything off the ship, hit in full.
    /// </summary>
    public const int OwnShipTakes = 100;

    /// <summary>A built machine's hit points with none of its own, in tenths: a scrap wall's strength.</summary>
    public const int MachineHp = 400;
}
