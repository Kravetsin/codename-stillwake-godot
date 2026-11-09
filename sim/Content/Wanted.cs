namespace Stillwake.Content;

/// <summary>
/// The hidden "wanted" meter on every hero (tracker-catchers): its former owners' tracker on its shin. While the hero acts on a planet
/// it fills each breath, slower under rock; full, a catcher comes for it. In units and ticks.
/// </summary>
public static class Wanted
{
    /// <summary>The ticks a hero counts as active after it last moved a cell or ran: three of its client's reports.</summary>
    public const int ActiveFor = 60;

    /// <summary>What a breath of an active hero adds under the open sky: no back wall behind it.</summary>
    public const int Open = 2;

    /// <summary>What a breath adds with a back wall behind the hero: under rock, a weak signal.</summary>
    public const int Under = 1;

    /// <summary>
    /// The least and most a full meter needs, both included, drawn at random after each reset: 10 to 15 minutes of activity
    /// under the open sky.
    /// </summary>
    public const int NeedLeast = 1200, NeedMost = 1800;

    /// <summary>The meter blinks the bracelet from this many thirds of full: <c>Wanted × 3 ≥ Need × Blink</c>.</summary>
    public const int Blink = 2;
}
