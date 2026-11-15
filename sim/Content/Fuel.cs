namespace Stillwake.Content;

/// <summary>
/// Biofuel as a liquid (fuel-liquid): a can is a litre, poured into the stores of the machines that hold it, piped between
/// them and burnt from them. In millilitres, as oxygen and water are.
/// </summary>
public static class Fuel
{
    /// <summary>The item: a jerrycan of biofuel, a litre.</summary>
    public const string Item = "biofuel";

    public const int Litre = 1000;

    /// <summary>What a can holds: a litre (the user's word).</summary>
    public const int Can = Litre;

    /// <summary>What a descent burns from the drop pod's store; a return burns none.</summary>
    public const int Descent = 5 * Litre;

    /// <summary>
    /// What a store gives a tick to the stores piped from its output to their inputs (tanks-in-a-line), a tank's
    /// <see cref="Air.TankFlow"/>: a litre in 2.5 seconds, a pod's 20 in 50.
    /// </summary>
    public const int Flow = Air.TankFlow;
}
