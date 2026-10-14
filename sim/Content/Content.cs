namespace Stillwake.Content;

/// <summary>A wall material: the code saves store it by.</summary>
public sealed record Material(string Id, int Code);

/// <summary>A machine's footprint in cells and where conduits meet it.</summary>
public sealed record MachineSpec(string Kind, int Width, int Height, IReadOnlyList<(int X, int Y)> Ports);

/// <summary>Game data, as packages/content holds it: only what the ported modules read.</summary>
public static class Data
{
    public static readonly IReadOnlyList<Material> Materials = [new("scrap", 1), new("iron", 2)];
    public const string DefaultHull = "scrap";

    public static readonly IReadOnlyList<MachineSpec> Machines =
    [
        new("tank", 2, 2, [(0, 1)]),
        new("generator", 2, 2, [(0, 1)]),
        new("solarPanel", 3, 1, []),
        new("vent", 1, 1, [(0, 0)]),
        new("lamp", 1, 1, []),
        new("analyzer", 2, 2, []),
        new("crate", 2, 1, []),
        new("furnace", 2, 2, []),
    ];

    public static Material? MaterialById(string id) => Materials.FirstOrDefault(m => m.Id == id);
    public static Material? MaterialByCode(int code) => Materials.FirstOrDefault(m => m.Code == code);
    public static MachineSpec? Machine(string kind) => Machines.FirstOrDefault(m => m.Kind == kind);

    /// <summary>What each character of an ASCII map means (content/src/legend.ts).</summary>
    public const string Legend = " .#=HDd-vxTGPFVLKAS";
    public const string LegendUnder = " -o";

    /// <summary>Which machine a map character draws.</summary>
    public static string? MachineOf(char c) => c switch
    {
        'T' => "tank",
        'G' => "generator",
        'P' => "solarPanel",
        'F' => "furnace",
        'V' => "vent",
        'L' => "lamp",
        'K' => "crate",
        'A' => "analyzer",
        _ => null,
    };
}
