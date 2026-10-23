namespace Stillwake.Content;

/// <summary>
/// Power, in units a tick. A cable joins two nodes whose nearest cells are at most <see cref="MaxSpan"/>
/// cells apart, measured straight; longer runs need anchors between. What machines make and draw is in
/// <see cref="Machines"/>.
/// </summary>
public static class Power
{
    public const int MaxSpan = 20;

    /// <summary>What a cable costs its layer, and gives back when cut.</summary>
    public static readonly IReadOnlyList<(string Item, int Count)> CableCost = [("wire", 1)];
}
