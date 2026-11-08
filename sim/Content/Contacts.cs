namespace Stillwake.Content;

/// <summary>
/// The radar's contacts (2026-11-08-radar-contacts): a place now and then on the ship's orbit, the first <see cref="First"/>
/// ticks into a world, each next <see cref="EveryMin"/> to <see cref="EveryMax"/> ticks after the last, each seen for
/// <see cref="Lasts"/> ticks. Where on the orbit is an offset from the ship, <see cref="Nearest"/> to <see cref="Reach"/> radar
/// units either side, <see cref="Reach"/> the radar's edge.
/// </summary>
public static class Contacts
{
    /// <summary>A derelict ship drifting in space.</summary>
    public const string Wreck = "wreck";

    /// <summary>The kinds a contact is drawn from, evenly.</summary>
    public static readonly IReadOnlyList<string> Kinds = [Wreck];

    /// <summary>The first contact of a world, 15 min in: a fixed tick, so a short run never draws from the world's Rng.</summary>
    public const int First = 18_000;
    /// <summary>The least ticks from a contact to the next: 15 min at 20 ticks a second.</summary>
    public const int EveryMin = 18_000;
    /// <summary>The most: 30 min.</summary>
    public const int EveryMax = 36_000;
    /// <summary>Ticks a contact is seen: 15 min. No more than <see cref="EveryMin"/>, so one is up at a time.</summary>
    public const int Lasts = 18_000;

    /// <summary>The radar's edge, either side of the ship, in radar units.</summary>
    public const int Reach = 100;
    /// <summary>A contact drawn is never nearer the ship than this.</summary>
    public const int Nearest = 25;
}
