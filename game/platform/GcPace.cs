namespace Stillwake.Game.Platform;

/// <summary>
/// When the game collects its youngest garbage itself: after <see cref="Every"/> bytes allocated since the last collection of any
/// kind. Left to itself the runtime waits for some 50 MB, and by then a few MB of particles, sounds and messages alive a second or two
/// are caught alive and copied: 7 to 12 ms, a visible hitch every few seconds at 240 Hz. What a collection copies grows with the
/// bytes between collections; collected every 2 MB it takes some 0.4 ms on a planet and 1 ms on the busy testbed, a few times a
/// second. Plain C#, so it is tested.
/// </summary>
public sealed class GcPace
{
    /// <summary>The bytes allocated between two collections, at most: a quarter to half a second of play.</summary>
    public const long Every = 2 * 1024 * 1024;

    private long _since = -1;
    private int _collections = -1;

    /// <summary>
    /// Whether to collect now, given every byte the process has allocated so far and how many youngest-generation collections ran:
    /// a collection the runtime ran itself counts as one, and the bytes are counted from it.
    /// </summary>
    public bool Due(long allocated, int collections)
    {
        if (collections != _collections || _since < 0) (_since, _collections) = (allocated, collections);
        if (allocated - _since < Every) return false;
        (_since, _collections) = (allocated, collections + 1);
        return true;
    }
}
