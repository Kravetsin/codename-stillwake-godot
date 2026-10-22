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
        ('#', "hull wall, of the default hull material"),
        ('/', "hull wall cut to ◢, solid bottom right: a floor slope rising to the right (0069)"),
        ('\\', "hull wall cut to ◣, solid bottom left: a floor slope rising to the left"),
        ('r', "hull wall cut to ◤, solid top left: a ceiling slope"),
        ('7', "hull wall cut to ◥, solid top right: a ceiling slope"),
        ('=', "floor: a platform heroes stand on and climb through from below"),
        ('H', "ladder"),
        ('D', "door, closed"),
        ('d', "door, open"),
        ('-', "pipe"),
        ('v', "valve, open"),
        ('x', "valve, closed"),
        ('T', "oxygen tank: draw its whole footprint"),
        ('G', "oxygen generator: draw its whole footprint"),
        ('P', "solar panel: draw its whole footprint"),
        ('F', "furnace: draw its whole footprint"),
        ('B', "burner generator: draw its whole footprint"),
        ('V', "vent"),
        ('L', "lamp"),
        ('J', "arc lamp: one of the arc makers' hung aboard (0091)"),
        ('K', "crate: draw its whole footprint"),
        ('A', "analyzer: draw its whole footprint"),
        ('C', "charging station: draw its whole footprint"),
        ('M', "former: draw its whole footprint"),
        ('E', "electric smelter: draw its whole footprint"),
        ('Y', "circuit assembler: draw its whole footprint"),
        ('g', "grow capsule: draw its whole footprint"),
        ('R', "drone bay: draw its whole footprint"),
        ('Q', "capacitor bank: draw its whole footprint (0101)"),
        ('O', "lifepod: draw its whole footprint; heroes are made at its hatch"),
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

    /// <summary>Which machine a map character draws.</summary>
    public static string? MachineOf(char c) => c switch
    {
        'T' => "tank",
        'G' => "generator",
        'P' => "solarPanel",
        'F' => "furnace",
        'B' => "burnerGenerator",
        'V' => "vent",
        'L' => "lamp",
        'J' => "arcLamp",
        'K' => "crate",
        'A' => "analyzer",
        'C' => "charger",
        'M' => "former",
        'E' => "smelter",
        'Y' => "assembler",
        'g' => "growCapsule",
        'R' => "droneBay",
        'Q' => "capacitorBank",
        'O' => "lifepod",
        _ => null,
    };
}
