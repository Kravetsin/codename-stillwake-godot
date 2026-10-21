namespace Stillwake.Content;

/// <summary>
/// The planet's core: a band of lava over its bedrock (<see cref="Ground.LavaRows"/>), a natural material nothing digs or
/// builds. A hero touching it — its reported cell in it, or the lava up to <see cref="Reach"/> rows under that cell, so
/// standing on it — has its suit pay <see cref="Burn"/> units at every breath, the suit alone, as the cold past the field.
/// </summary>
public static class Lava
{
    // Not a const: every constant of this table is a whole number (ContentTests).
    /// <summary>The material's id (<see cref="Materials"/>).</summary>
    public static readonly string Material = "lava";

    /// <summary>
    /// What touching it costs a suit a breath: a third of a full suit (<see cref="Crew.Suit"/>), so with the breath itself a
    /// full suit is gone on the third breath, some three seconds.
    /// </summary>
    public const int Burn = 15;

    /// <summary>
    /// How many rows under a hero's reported cell, its body's middle, lava burns it: a hero's feet are two rows under its
    /// middle standing (<see cref="Hero.Height"/> 2625 thousandths, the middle 1312 over the feet).
    /// </summary>
    public const int Reach = 2;
}
