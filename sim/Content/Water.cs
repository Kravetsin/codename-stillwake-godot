namespace Stillwake.Content;

/// <summary>
/// Water (water): ice melted into a bottle of it, poured into a cistern, split by an electrolyser into liquid oxygen, a unit
/// of water a unit of liquid. In the units of <see cref="Air"/>.
/// </summary>
public static class Water
{
    /// <summary>The item: a glass bottle of it, one an ice melted.</summary>
    public const string Item = "water";

    /// <summary>What a bottle pours: a canister's 3 litres, so a bottle and a canister read alike (liquid-volumes).</summary>
    public const int Bottle = Air.Canister;

    /// <summary>How many bottles a cistern holds: 150 litres, a tank's.</summary>
    public const int CisternBottles = 50;

    public const int CisternCapacity = CisternBottles * Bottle;

    /// <summary>The electrolyser's rate at full power, units every <see cref="Air.LeakEvery"/> ticks: three a tick, 3.6 litres a minute (industrial-oxygen).</summary>
    public const int ElectrolyserRate = 36000;

    /// <summary>
    /// What a cistern passes a tick to the cisterns piped from its output to their inputs (tanks-in-a-line), a tank's
    /// <see cref="Air.TankFlow"/>: a bottle moves on in some 11 seconds.
    /// </summary>
    public const int CisternFlow = Air.TankFlow;
}
