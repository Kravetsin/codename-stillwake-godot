namespace Stillwake.Game;

/// <summary>
/// What a hero's person looks like under the suit (2026-10-28-one-head): the skin and hair ramps its pictures are
/// recoloured into and how many of each picture there are. Client data: the sim checks no index. The pictures are
/// painted by <c>tools/hero-art</c>, whose <c>parts.js</c> holds these ramps too (<c>HeroLooksTests</c> keeps them alike).
/// Colours are 0xRRGGBB, each ramp light, own, shade and outline.
/// </summary>
public static class HeroLooks
{
    /// <summary>Skin tones, light to deep.</summary>
    public static readonly int[][] Skins =
    [
        [0xf8e0cf, 0xefc8ae, 0xd3a189, 0x8c5c4b],
        [0xf3d0a9, 0xe5b68b, 0xc68e69, 0x7e5039],
        [0xecc08e, 0xdba570, 0xb98052, 0x71472a],
        [0xd9aa7d, 0xc38d5f, 0x9e6c45, 0x5f3b24],
        [0xc28d5f, 0xa87242, 0x83542e, 0x4b2d17],
        [0x9e6943, 0x835231, 0x623a22, 0x381e0f],
        [0x74492f, 0x5b3822, 0x432716, 0x24130a],
    ];

    /// <summary>The skin the pictures are drawn in, which every other is swapped in for.</summary>
    public const int SkinDrawn = 2;

    /// <summary>Hair colours: black, brown, copper, blond, grey, dyed rose. Brows and beards take them too.</summary>
    public static readonly int[][] Hairs =
    [
        [0x4c4855, 0x302d36, 0x1f1d24, 0x100f13],
        [0x7d5636, 0x5c3c24, 0x412817, 0x25170d],
        [0xd57c42, 0xab562c, 0x7e3b1d, 0x48210f],
        [0xf1d78e, 0xd6b262, 0xaa863e, 0x6b5121],
        [0xe3e3e7, 0xb6b8c0, 0x888a96, 0x4b4d57],
        [0xea8db6, 0xc46390, 0x93466a, 0x56253a],
    ];

    /// <summary>The hair colour the pictures are drawn in.</summary>
    public const int HairDrawn = 1;

    /// <summary>The pictures of each kind: <c>hero_hair_*</c>, <c>hero_face_*</c> and <c>hero_beard_*</c>; a look's beard 0 is none.</summary>
    public const int HairStyles = 6, Faces = 5, Beards = 2;

    /// <summary>
    /// A look's index <paramref name="i"/> among <paramref name="count"/>: wrapped, never negative, so an index the sim kept as
    /// sent from a stale or altered client still draws (2026-10-28-looks-are-printed).
    /// </summary>
    public static int Wrap(int i, int count) => (i % count + count) % count;
}
