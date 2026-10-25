namespace Stillwake.Content;

/// <summary>
/// Directions as whole numbers (the tool's fan of rays): a heading is a step of <see cref="Turn"/> to the full turn, 0 along
/// +x and growing clockwise as the screen shows it, y down. Its sine and cosine come from <see cref="Sines"/>, in thousandths.
/// </summary>
public static class Headings
{
    /// <summary>The steps of a full turn: 256, a step a little under 1.5°.</summary>
    public const int Turn = 256;

    /// <summary>The sine of each step of the first quarter turn, 0 to 64, in thousandths, rounded to nearest; the rest mirror it.</summary>
    public static readonly IReadOnlyList<int> Sines =
    [
        0, 25, 49, 74, 98, 122, 147, 171, 195, 219, 243, 267, 290, 314, 337, 360, 383, 405, 428, 450, 471, 493, 514, 535, 556, 576,
        596, 615, 634, 653, 672, 690, 707, 724, 741, 757, 773, 788, 803, 818, 831, 845, 858, 870, 882, 893, 904, 914, 924, 933, 942,
        950, 957, 964, 970, 976, 981, 985, 989, 992, 995, 997, 999, 1000, 1000,
    ];
}
