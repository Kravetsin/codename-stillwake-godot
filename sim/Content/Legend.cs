namespace Stillwake.Content;

/// <summary>
/// What each character means in a map drawn as ASCII: the starting wreck and scenario tests share this
/// legend. A map with a character missing here does not parse.
/// </summary>
public static class Legend
{
    public static readonly IReadOnlyList<(char Char, string Means)> Map =
    [
        (' ', "space outside the ship"),
        ('.', "open cell inside the ship"),
        .. Structures.All.Where(s => s.Char != null).Select(s => (s.Char!.Value, s.Means)),
        ('-', "pipe"),
        ('v', "valve, open"),
        ('x', "valve, closed"),
        .. Machines.All.Where(m => m.Char != null).Select(m => (m.Char!.Value, m.Means!)),
        ('S', "where the hero wakes, on a map without a lifepod"),
    ];

    /// <summary>
    /// A map may come with a second one, drawn the same size, of what lies under and inside the first:
    /// what one character per cell cannot show on top.
    /// </summary>
    public static readonly IReadOnlyList<(char Char, string Means)> Under =
    [
        (' ', "nothing"),
        ('-', "pipe, under a wall or a machine as anywhere"),
        ('o', "a canister's worth of oxygen in the tank drawn over it"),
    ];

    /// <summary>
    /// A map may come with a third, of what its walls and back walls are made of (0077): <c>.</c> or space for the default
    /// hull, as every map before the planets; one of these for a natural material.
    /// </summary>
    public static readonly IReadOnlyList<(char Char, string Material)> Materials =
    [
        ('s', "soil"),
        ('t', "stone"),
        ('b', "bedrock"),
        ('l', "lava"),
        ('a', "alloy"),
        ('p', "plaster"),
    ];

    /// <summary>The material a material map's <paramref name="c"/> names; null for the default hull, or a character none names.</summary>
    public static string? MaterialOf(char c)
    {
        foreach (var (ch, material) in Materials)
            if (ch == c) return material;
        return null;
    }

    /// <summary>Every character a map may hold.</summary>
    public static readonly string MapChars = string.Concat(Map.Select(e => e.Char));

    /// <summary>Every character a map under a map may hold.</summary>
    public static readonly string UnderChars = string.Concat(Under.Select(e => e.Char));

    private static readonly Dictionary<char, string> MachineKinds =
        Machines.All.Where(m => m.Char != null).ToDictionary(m => m.Char!.Value, m => m.Kind);

    /// <summary>Which machine a map character draws; null for a character no machine has.</summary>
    public static string? MachineOf(char c) => MachineKinds.GetValueOrDefault(c);
}
