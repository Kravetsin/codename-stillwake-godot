namespace Stillwake.Content;

/// <summary>
/// What a burst does where its shot ends (blasting-charge): the meteor's wave (2026-10-29-meteor-wave) of <paramref name="Power"/>, spent
/// at <see cref="Meteors.Step"/> a cell and <see cref="Meteors.PerStrength"/> a point of a wall's armour, 7/5 of that aslant, within
/// <paramref name="Reach"/> cells of it (round-blasts); natural ground alone breaks. Every wreck whose box comes within <paramref name="Reach"/>
/// cells takes <paramref name="WreckDamage"/>.
/// </summary>
public sealed record Blast(int Power, int Reach, int WreckDamage);

/// <summary>The bursts of what is thrown.</summary>
public static class Blasting
{
    /// <summary>
    /// The blasting charge's: 63 to spend, so solid stone (10 a cell, its armour 4) opens a round crater 6 cells out, soil (4 a cell)
    /// the whole circle of 8, and the ruins' alloy (16 a cell) soaks it within 3; 120 off each wreck in reach, most at once.
    /// </summary>
    public static readonly Blast Charge = new(Power: 63, Reach: 8, WreckDamage: 120);
}
