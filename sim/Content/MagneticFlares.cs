namespace Stillwake.Content;

/// <summary>
/// Magnetic flares breaking machines (2026-10-30-magnetic-flare): once <see cref="ArmedBy"/> is known, a place's flare comes every
/// <see cref="Flare.EveryMin"/> to <see cref="Flare.EveryMax"/> ticks, seen <see cref="Flare.Warning"/> ticks before it strikes, and
/// breaks <see cref="Flare.PercentMin"/> to <see cref="Flare.PercentMax"/> percent of the electric machines a cable ends on, rounded up.
/// </summary>
public static class MagneticFlares
{
    /// <summary>A place's flares, a record, not the event: how often, how long seen coming, what share of the machines each breaks, in percent.</summary>
    public sealed record Flare(int EveryMin, int EveryMax, int Warning, int PercentMin, int PercentMax);

    /// <summary>The blueprint whose knowing arms the flares: once the player can build an oxygen generator, one can be repaired.</summary>
    public const string ArmedBy = "generator";

    /// <summary>The starting space: one every eight to fifteen minutes, thirty seconds of aurora, a tenth to three tenths of the machines broken.</summary>
    public static readonly Flare StartingSpace = new(8 * 60 * 20, 15 * 60 * 20, 30 * 20, PercentMin: 10, PercentMax: 30);
}

/// <summary>What repairing a broken machine takes: only work (2026-10-30-free-repair).</summary>
public static class RepairRules
{
    /// <summary>The share, in thousandths, of a machine's build ticks the repair's work takes, rounded up.</summary>
    public const int Share = 250;
}
