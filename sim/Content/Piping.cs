namespace Stillwake.Content;

/// <summary>Pipes as spans (0109): straight runs between machines' ports and joints, laid with the wrench.</summary>
public static class Piping
{
    /// <summary>The longest span, in cells: its length rounded up (<c>PipesModule.LengthOf</c>).</summary>
    public const int MaxSpan = 12;

    /// <summary>A span costs one <see cref="Item"/> for every started this many cells of its length.</summary>
    public const int CellsPerItem = 4;

    /// <summary>What a span is made of.</summary>
    public const string Item = "tube";

    /// <summary>What turning a joint into a valve costs, given back when it is turned back or its last span is cut.</summary>
    public static readonly IReadOnlyList<(string Item, int Count)> ValveCost = [("tube", 1), ("gear", 1)];
}
