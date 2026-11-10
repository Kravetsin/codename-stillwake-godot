namespace Stillwake.Content;

/// <summary>
/// The hidden "wanted" meter on every hero (tracker-catchers, catcher-loot): its former owners' tracker on its shin. It fills by the
/// worth (<see cref="Market.WorthOf"/>) of what the hero breaks or kills; full, a catcher is sent, and comes after a warning.
/// </summary>
public static class Wanted
{
    /// <summary>
    /// The least and most worth a full meter needs, both included, drawn at random after each reset: placeholders for 10 to 20
    /// minutes of greedy salvage, till a play pass measures them.
    /// </summary>
    public const int NeedLeast = 4000, NeedMost = 8000;

    /// <summary>The least and most breaths, both included, between a full meter and its catcher: the bracelet's alarm.</summary>
    public const int ComingLeast = 30, ComingMost = 60;
}
