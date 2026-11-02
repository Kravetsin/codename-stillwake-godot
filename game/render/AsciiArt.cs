using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// Small pictures drawn as ASCII in code (render/pixel-art.ts's <c>pixelTexture</c>, render/power-art.ts):
/// a character a pixel, coloured by a palette, made once under a key and kept. Sharp at any zoom, as the
/// project samples textures nearest.
/// </summary>
public static class AsciiArt
{
    private static readonly Dictionary<string, ImageTexture> Made = new();

    /// <summary>A picture of <paramref name="rows"/>, each character's colour from <paramref name="palette"/> (0xRRGGBB); any other is clear.</summary>
    public static ImageTexture Texture(string key, IReadOnlyList<string> rows, IReadOnlyDictionary<char, uint> palette)
    {
        if (Made.TryGetValue(key, out var kept)) return kept;
        var image = Image.CreateEmpty(rows[0].Length, rows.Count, false, Image.Format.Rgba8);
        for (int y = 0; y < rows.Count; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (palette.TryGetValue(rows[y][x], out uint c)) image.SetPixel(x, y, Colour(c));
        return Made[key] = ImageTexture.CreateFromImage(image);
    }

    /// <summary>A colour from 0xRRGGBB, as the TypeScript game writes them.</summary>
    public static Color Colour(uint rgb, float alpha = 1) =>
        new(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, alpha);

    /// <summary>A plug hanging loose: over a machine that makes or draws power with no cable to it.</summary>
    public static ImageTexture Unplugged() => Texture("unplugged",
        ["..K.K..", "..K.K..", ".KKKKK.", ".KwwwK.", ".KwwwK.", "..KwK..", "...K..."],
        new Dictionary<char, uint> { ['K'] = 0x1a1e25, ['w'] = 0x9aa3ad });

    /// <summary>A bolt over a machine short of power: red with none, amber with some.</summary>
    public static ImageTexture Bolt(bool none) => Texture($"bolt:{none}",
        ["....KKK", "...KyyK", "..KyyK.", ".KyyKKK", "KyyyyyK", "KKKyyK.", "..KyyK.", ".KyyK..", ".KKK..."],
        new Dictionary<char, uint> { ['K'] = 0x1a1e25, ['y'] = none ? 0xe05555u : 0xf0b44cu });


    /// <summary>A wrench on an orange plate over a machine a flare broke: it wants a hand, not power.</summary>
    public static ImageTexture Broken() => Texture("broken",
        [
        "..KKKKKKKKK..",
        ".KrrrrrrrrrK.",
        "KrrrrrrwrrwrK",
        "KrrrrrrwwrwrK",
        "KrrrrrrwwwwrK",
        "KrrrrrwwwwrrK",
        "KrrrrwwwrrrrK",
        "KrrrwwwrrrrrK",
        "KrrwwwrrrrrrK",
        "KrwwwrrrrrrrK",
        "KrwwrrrrrrrrK",
        ".KrrrrrrrrrK.",
        "..KKKKKKKKK..",
    ],
        new Dictionary<char, uint> { ['K'] = 0x1a1e25, ['r'] = 0xe0662a, ['w'] = 0xf4f6f8 });

    /// <summary>A cable's plug, in a socket it is plugged into.</summary>
    public static ImageTexture Plug() => Texture("power-plug",
        [".KKK.", "KhhhK", "KhlhK", "KhhhK", ".KKK."],
        new Dictionary<char, uint> { ['K'] = 0x1a1e25, ['h'] = 0x7c8794, ['l'] = 0xc5ccd8 });

    /// <summary>What an upgrade of <paramref name="kind"/> is drawn as on the tree: a bag, a belt, a capsule.</summary>
    public static ImageTexture Upgrade(string kind) => kind switch
    {
        Content.Upgrades.Bag => Texture("upgrade-bag",
            ["....KKKK....", "...Kw..wK...", "..KKKKKKKK..", ".KbbbbbbbbK.", ".KbbbbbbbbK.", ".KbbKKKKbbK.", ".KbbKyyKbbK.", ".KbbKKKKbbK.", ".KbbbbbbbbK.", ".KbbbbbbbbK.", "..KKKKKKKK.."],
            new Dictionary<char, uint> { ['K'] = 0x1a1e25, ['w'] = 0xc5ccd8, ['b'] = 0x7c6a4f, ['y'] = 0xf0b44c }),
        Content.Upgrades.Belt => Texture("upgrade-belt",
            ["............", "............", "KKKKKKKKKKKK", "bbbKKKKKKbbb", "bbbKyyyyKbbb", "bbbKyKKyKbbb", "bbbKyyyyKbbb", "bbbKKKKKKbbb", "KKKKKKKKKKKK", "............"],
            new Dictionary<char, uint> { ['K'] = 0x1a1e25, ['b'] = 0x7c6a4f, ['y'] = 0xf0b44c }),
        _ => Capsule(),
    };

    /// <summary>A trade station's capsule, nose up.</summary>
    public static ImageTexture Capsule() => Texture("capsule",
        ["....KK....", "...KwwK...", "..KwwwwK..", "..KwbbwK..", "..KwbbwK..", "..KwwwwK..", "..KwwwwK..", "..KyyyyK..", ".KKwwwwKK.", ".KwKKKKwK.", ".KK....KK."],
        new Dictionary<char, uint> { ['K'] = 0x1a1e25, ['w'] = 0xc5ccd8, ['b'] = 0x4da3d6, ['y'] = 0xf0b44c });

    /// <summary>The coin credits are written with.</summary>
    public static ImageTexture Credit() => Texture("credit",
    [
        "....KKKKKKKK....",
        "..KKyyyyyyyyKK..",
        ".KyyhhhhhhhhyyK.",
        ".KyhhyyyyyyyhoK.",
        "KyhhyyyKKKyyyhoK",
        "KyhyyyKKKKKyyyoK",
        "KyhyyKKyyyKKyyoK",
        "KyhyyKKyyyyyyyoK",
        "KyhyyKKyyyyyyyoK",
        "KyhyyKKyyyKKyyoK",
        "KyhyyyKKKKKyyyoK",
        "KyyoyyyKKKyyyooK",
        ".KyoooyyyyyyooK.",
        ".KyyoooooooooyK.",
        "..KKyyyyyyyyKK..",
        "....KKKKKKKK....",
    ],
        new Dictionary<char, uint> { ['K'] = 0x3a2a10, ['y'] = 0xf0c050, ['h'] = 0xffe9a0, ['o'] = 0xb8862c });

    /// <summary>A book with a star on its cover: the Handbook's (0066), a placeholder till its picture is picked.</summary>
    public static ImageTexture Book() => Texture("book",
    [
        "................",
        "...KKKKKKKKKKK..",
        "...KsscccccccK..",
        "...Kssccccccce..",
        "...Ksscccaccce..",
        "...Kssccaaacce..",
        "...Ksscaaaaace..",
        "...Kssccaaacce..",
        "...Ksscccaccce..",
        "...Kssccccccce..",
        "...Kssclllllce..",
        "...Kssccccccce..",
        "...Kssccccccce..",
        "...KKppppppppe..",
        "...K.ppppppppp..",
        "................",
    ],
        new Dictionary<char, uint> { ['K'] = 0x0b0f17, ['c'] = 0x3f6f8a, ['s'] = 0x2c4e63, ['p'] = 0xe2e8f0, ['e'] = 0xcbd5e1, ['a'] = 0xf2b84b, ['l'] = 0x94b8cc });

    /// <summary>A cracked square: a wreck's hit points, beside their number.</summary>
    public static ImageTexture Crack() => Texture("crack",
        ["eeeeeeeee", "e...w...e", "e..w....e", "e...ww..e", "e.....w.e", "e....w..e", "e...w...e", "e....w..e", "eeeeeeeee"],
        new Dictionary<char, uint> { ['e'] = 0xcbd5e1, ['.'] = 0x1e293b, ['w'] = 0xf1f5f9 });

    /// <summary>A peg on a wall holding a cable's end: where one cable ends, or where two meet.</summary>
    public static ImageTexture Peg() => Texture("power-peg",
        ["..KK..", ".KhhK.", "KhllhK", "KhllhK", ".KhhK.", "..KK.."],
        new Dictionary<char, uint> { ['K'] = 0x1a1e25, ['h'] = 0x5a6273, ['l'] = 0x9aa3b5 });
}
