namespace Stillwake.Content;

/// <summary>
/// One rule for what a hit breaks (unified-damage): a tool's blow, a shot, a charge's and a meteor's burst each break a cell when
/// their damage covers what is left of its strength, in tenths, passing on what is over. These are a burst's numbers.
/// </summary>
public static class Damage
{
    /// <summary>What a burst spends crossing a cell to a side or up or down, 7/5 of it aslant (<c>Wave</c>, round-blasts): its falloff.</summary>
    public const int Step = 2;

    /// <summary>
    /// The share of a burst's damage a built cell takes, in thousandths (walls not natural, doors, ladders, hatches, floors, built
    /// machines): about a twenty-seventh, so one charge cracks a scrap wall and a second breaks it; an iron hull takes four or five.
    /// </summary>
    public const int BuiltTakes = 37;

    /// <summary>A built machine's hit points with none of its own, in tenths: a scrap wall's strength.</summary>
    public const int MachineHp = 20;
}
