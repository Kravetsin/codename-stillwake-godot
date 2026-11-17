namespace Stillwake.Content;

/// <summary>What happened to a wrecked vessel (vessel-wrecks), one event of its chain.</summary>
public enum WreckKind { Explosion, Fire, Icing, Asteroid, Break }

/// <summary>A step of a forced chain (vessel-wrecks): its event, and whether its group is old; fresh otherwise.</summary>
public sealed record WreckStep(WreckKind Kind, bool Old = false);

/// <summary>
/// Wrecked vessels (vessel-wrecks): one of the four classes, built as intact, then a chain of events in time order. The tunables
/// a designer changes; the counts, radii, gaps and fire's reach are each class's (<see cref="VesselClass"/>).
/// </summary>
public static class Wrecks
{
    /// <summary>A contact is a wreck this many times in a hundred; 0 till wrecks come among the contacts (W-4).</summary>
    public const int WreckPercent = 0;

    /// <summary>The last group of a chain is fresh this many times in a hundred, else old; every earlier group is old.</summary>
    public const int FreshPercent = 50;

    /// <summary>The share, in whole percent, of each stack in a crate a fire burns in a compartment it reached, rounded down.</summary>
    public const int BurntLoss = 50;

    /// <summary>Bodies in suits an explosion, a fire, an asteroid or a break leaves, least and most.</summary>
    public static readonly (int Min, int Max) BodiesPerEvent = (0, 2);

    /// <summary>Asteroid strikes a liner or a tanker takes at most, and any other class.</summary>
    public const int RocksBig = 2, Rocks = 1;

    /// <summary>A break's bow half moves this many rows up or down; its stern half a row the other way this many times in a hundred.</summary>
    public static readonly (int Min, int Max) BowRows = (2, 3);

    public const int SternBackPercent = 40;

    /// <summary>A break's cut runs at a rib this many columns at least from the stern and the bow.</summary>
    public const int BreakFromEnds = 22;

    /// <summary>How far a breach pulls loose things, in cells: an explosion's, an asteroid's on a small class and a big one, a break's.</summary>
    public const int PullBlast = 14, PullRock = 16, PullRockBig = 22, PullBreak = 18;

    /// <summary>
    /// Out of a hundred, how often a loose thing within <see cref="PullLostPercent"/> of its pull's reach is lost: a small one, any
    /// other.
    /// </summary>
    public const int LostSmall = 80, LostOther = 45, PullLostPercent = 35;

    /// <summary>A crate this many columns or fewer from where a fire started becomes a pile of bits.</summary>
    public const int SeatReach = 8;

    /// <summary>Cells round the arrival room that no event touches, every side: its walls and doors stand.</summary>
    public const int ProtectRing = 2;
}
