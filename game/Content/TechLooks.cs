using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>
/// Each module technology's icon (0086), by its id (<c>Content.Modules</c>' <c>Tech</c>): 7×7 pixels, a character each,
/// outlined dark as the slots' flame and sprout are. It marks what takes a module, and what fits, in place of words: the
/// socket's ghost, the slot mark of a module that fits the machine open, the module's card. Placeholders until the user
/// picks them on a page.
/// </summary>
public static class TechLooks
{
    /// <summary>An icon's rows, top first, and the colour of each character, 0xRRGGBB; any other is clear.</summary>
    public sealed record Look(IReadOnlyList<string> Rows, IReadOnlyDictionary<char, uint> Palette);

    /// <summary>The icons' side, pixels: the slots' marks' size, drawn at whole multiples.</summary>
    public const int Side = 7;

    /// <summary>The arcs': a small zigzag of lightning, violet down its left edge and white-hot to its right.</summary>
    public static readonly Look Arc = new(
        ["...KvwK", "..KvwK.", ".KvwK..", "KvwwwK.", ".KKvwK.", ".KvwK..", "KvwK..."],
        new Dictionary<char, uint> { ['K'] = 0x1a1e25, ['v'] = 0xa78bfa, ['w'] = 0xf4eeff });

    public static readonly IReadOnlyDictionary<string, Look> All = new Dictionary<string, Look> { ["arc"] = Arc };

    /// <summary>Technology <paramref name="tech"/>'s icon; throws for one with none.</summary>
    public static Look Of(string tech) => All[tech];
}
