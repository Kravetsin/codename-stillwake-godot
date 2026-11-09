namespace Stillwake.Content;

/// <summary>
/// What a burst does where its shot ends (blasting-charge, unified-damage): the shared wave of <paramref name="Power"/> in tenths, a hit's
/// damage in every direction, spent at <see cref="Damage.Step"/> a cell and what is left of each wall's strength, 7/5 of that aslant,
/// within <paramref name="Reach"/> cells of it (round-blasts); a built thing at its <see cref="Damage.BuiltTakes"/> share. Every wreck
/// whose box comes within <paramref name="Reach"/> cells takes <paramref name="WreckDamage"/>.
/// </summary>
public sealed record Blast(int Power, int Reach, int WreckDamage);

/// <summary>The bursts of what is thrown.</summary>
public static class Blasting
{
    /// <summary>
    /// The blasting charge's: 500 to spend, so solid stone (122 a cell) opens four cells out and cracks the next, soil (42 a cell)
    /// the whole circle of 8, the ruins' alloy (600) only cracks to 496, opened by a second; a scrap wall cracks to 18 of its 20 and
    /// breaks to a second. 120 off each wreck in reach, most at once.
    /// </summary>
    public static readonly Blast Charge = new(Power: 500, Reach: 8, WreckDamage: 120);
}
