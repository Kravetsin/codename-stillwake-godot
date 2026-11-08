namespace Stillwake.Content;

/// <summary>
/// What a burst does where its shot ends (deep-digging): the meteor's wave (2026-10-29-meteor-wave) of <paramref name="Power"/>, spent
/// at <see cref="Meteors.Step"/> a cell and <see cref="Meteors.PerStrength"/> a point of a wall's armour, inside the box of
/// <paramref name="Reach"/> cells either way; natural ground alone breaks. Every wreck whose box comes within <paramref name="Reach"/>
/// cells takes <paramref name="WreckDamage"/>.
/// </summary>
public sealed record Blast(int Power, int Reach, int WreckDamage);

/// <summary>The bursts of what is thrown.</summary>
public static class Blasting
{
    /// <summary>
    /// The blasting charge's: 100 to spend, so solid stone (10 a cell, its armour 4) opens some 20 cells across, soil (4 a cell) the
    /// whole box, 21 cells, and the ruins' alloy (16 a cell) soaks it within a few; 120 off each wreck in reach, most at once.
    /// </summary>
    public static readonly Blast Charge = new(Power: 100, Reach: 10, WreckDamage: 120);
}
