namespace Stillwake.Content;

/// <summary>
/// Magnetic flares breaking machines (2026-10-30-magnetic-flare): once <see cref="ArmedBy"/> is known, a place's flare comes every
/// <see cref="Flare.EveryMin"/> to <see cref="Flare.EveryMax"/> ticks, seen <see cref="Flare.Warning"/> ticks before it strikes, and
/// breaks <see cref="Flare.PercentMin"/> to <see cref="Flare.PercentMax"/> percent of the electric machines on a network, rounded up.
/// </summary>
public static class MagneticFlares
{
    /// <summary>A place's flares, a record, not the event: how often, how long seen coming, what share of the machines each breaks, in percent.</summary>
    public sealed record Flare(int EveryMin, int EveryMax, int Warning, int PercentMin, int PercentMax);

    /// <summary>The blueprint whose knowing arms the flares: once the player can build an oxygen generator, one can be repaired.</summary>
    public const string ArmedBy = "generator";

    /// <summary>The starting space: one every eight to fifteen minutes, ten seconds of aurora, a tenth to three tenths of the machines broken.</summary>
    public static readonly Flare StartingSpace = new(8 * 60 * 20, 15 * 60 * 20, 10 * 20, PercentMin: 10, PercentMax: 30);
}

/// <summary>What repairing a broken machine costs (2026-10-30-magnetic-flare).</summary>
public static class RepairRules
{
    /// <summary>
    /// The share, in thousandths, of each item of a machine's build cost a repair takes, rounded up, so a one-item cost takes
    /// that item; and of its build ticks the repair's work takes, rounded up.
    /// </summary>
    public const int Share = 250;

    /// <summary>The item a repair never takes, whatever the cost holds.</summary>
    public const string Never = "circuit";
}
