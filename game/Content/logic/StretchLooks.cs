using System;
using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>
/// How a stretch (docs/decisions/2026-11-15-stretches-of-space.md) is seen coming, drawn only, without a word: its far picture drifting in behind
/// the ship, the radar's band in its colour, frost on the hull before the ice belt. All of it from the sky's <c>SkyNow</c>, so a
/// peer joining mid-warning sees what the others do.
/// </summary>
public static class StretchLooks
{
    /// <summary>The ice belt's block and the contraband drift's crate: the wrecks the stretches bring, drawn their own way.</summary>
    public const string IceBlock = "iceBlock", ContrabandCrate = "contrabandCrate";

    /// <summary>
    /// A stretch's look: its far picture (<paramref name="Far"/>), none for a planet's weather; the radar's band in <paramref name="Band"/>,
    /// none unbanded; whether frost creeps over the hull before it (<paramref name="Frost"/>).
    /// </summary>
    public sealed record Look(string Id, Backdrop? Far, (float R, float G, float B)? Band, bool Frost, Weather? Weather = null);

    /// <summary>
    /// A planet's weather as it is seen and heard (docs/plans/surface-storms.md), the pick page's numbers: <paramref name="Streaks"/>
    /// over the open sky, <paramref name="Count"/> of them over the pick's view (<see cref="Weather.View"/>) at the storm's height;
    /// <paramref name="Wisps"/> drifting with them, if any; the sky and the land behind turned toward <paramref name="Haze"/>, at most
    /// <paramref name="HazeMost"/> of the way; daylight multiplied toward <paramref name="Dim"/>; its <paramref name="Wind"/>'s loop,
    /// none unheard. <paramref name="Visibility"/>: how the world in view washes out round the hero, none where it stays clear.
    /// </summary>
    public sealed record Weather(Streak Streaks, int Count, Wisps? Wisps, (float R, float G, float B) Haze, float HazeMost,
        (float R, float G, float B) Dim, Sound.Cue? Wind, Wash? Visibility = null)
    {
        /// <summary>The pick's view, in pixels of the world, and the box past it its streaks wrapped round: what <see cref="Count"/> filled.</summary>
        public const float View = 192, ViewTall = 112, BoxWide = View + 24, BoxTall = ViewTall + 18;

        /// <summary>How far the plants lean at the storm's height, in a gust's strengths: the pick's full storm over its strongest gust.</summary>
        public const float SwayGain = 2.5f;
    }

    /// <summary>
    /// The streaks a weather blows: falling at <paramref name="Angle"/> radians below the horizontal, leftward, at
    /// <paramref name="Speed"/> pixels a second, <paramref name="Length"/> long, tapering to their tail from <paramref name="Width"/> wide
    /// (each 0.7 to 1.3 of it), in turn of <paramref name="Colours"/>; <paramref name="Low"/>: thicker near the ground, jittering, a
    /// speck of grit beside some.
    /// </summary>
    public sealed record Streak(float Angle, (float Least, float Most) Speed, (float Least, float Most) Length, float Width, bool Low,
        IReadOnlyList<(float R, float G, float B, float A)> Colours);

    /// <summary>
    /// Soft wisps of what a weather carries, <paramref name="Pictures"/> under <c>art/hd</c>: <paramref name="Count"/> over the pick's
    /// view at the storm's height, drawn at <paramref name="Alpha"/> (each 0.6 to 1 of it), drifting left at <paramref name="Speed"/>.
    /// </summary>
    public sealed record Wisps(IReadOnlyList<string> Pictures, int Count, float Alpha, (float Least, float Most) Speed);

    /// <summary>
    /// A weather washing out the world in view («Блёклый мир», the pick of 2026-11-16): blurred to a pixel of the world, turned
    /// <paramref name="Grey"/> of the way to grey and <paramref name="Flat"/> of the way to its haze; clear round the hero's chest out to
    /// <paramref name="Clear"/>'s inner pixels, fading to washed at its outer, the patch <paramref name="Tall"/> times as tall as wide.
    /// </summary>
    public sealed record Wash(float Grey, float Flat, (float Inner, float Outer) Clear, float Tall);

    /// <summary>The picked wash, both storms': the pick page's numbers.</summary>
    public static readonly Wash Washed = new(Grey: 0.8f, Flat: 0.42f, Clear: (16, 34), Tall: 1.35f);

    private static (float, float, float, float) Rgba(int r, int g, int b, float a) => (r / 255f, g / 255f, b / 255f, a);
    private static (float, float, float) Rgb(int r, int g, int b) => (r / 255f, g / 255f, b / 255f);

    /// <summary>
    /// A stretch's far picture: a row of places, each <paramref name="Width"/> pixels of its plane wide with <paramref name="Gap"/> of
    /// that between, holding one of <paramref name="Pictures"/> (under <c>art/hd</c>) at random, or else a scatter of
    /// <paramref name="Scattered"/>'s pieces; its middle <paramref name="Above"/> of the screen above the screen's when it first shows.
    /// It passes right to left at <paramref name="Speed"/> pixels a second, steady: its leading edge comes in at the right edge as the
    /// warning starts, and once the stretch has gone the row ends where the screen's right edge is then, so its trailing edge passes out too.
    /// </summary>
    public sealed record Backdrop(IReadOnlyList<string> Pictures, float Width, float Gap, float Speed, float Above, Field? Scattered = null);

    /// <summary>
    /// A field of pieces cut from one picture, <paramref name="Atlas"/> (under <c>art/hd</c>), each of <paramref name="Pieces"/> its
    /// rectangle there: <paramref name="Each"/> of them a place, anywhere across it and thickest along the row's middle, within
    /// <paramref name="Tall"/> pixels of the plane; a piece drawn <paramref name="Small"/> to <paramref name="Large"/> pixels of the plane
    /// a pixel of its own, under one so the camera at its nearest never shows its pixels; drawn in <paramref name="Veil"/>, dimmer than
    /// any wreck drifting by, so a field far off never reads as debris near.
    /// </summary>
    public sealed record Field(string Atlas, IReadOnlyList<(int X, int Y, int W, int H)> Pieces, int Each, float Tall, float Small, float Large,
        (float R, float G, float B) Veil);

    /// <summary>
    /// The ice belt: a field of ice cut from the picked pictures, small and middling pieces over the whole sky, frosting the hull.
    /// The contraband drift: one derelict of three, slow, reaching the ship as the drift starts and gone about as it ends.
    /// </summary>
    public static readonly IReadOnlyList<Look> All =
    [
        new(Content.Stretches.IceBelt.Id, new([], Width: 500, Gap: 0, Speed: 8, Above: 0,
            new("stretches/ice_pieces", IcePieces, Each: 18, Tall: 1400, Small: 0.07f, Large: 0.15f, Veil: (0.24f, 0.29f, 0.38f))), (0.64f, 0.8f, 0.92f), Frost: true),
        new(Content.Stretches.ContrabandDrift.Id,
            new(["stretches/hulk_1", "stretches/hulk_2", "stretches/hulk_3"], Width: 1100, Gap: 3, Speed: 4, Above: 0.16f),
            (0.98f, 0.58f, 0.26f), Frost: false),
        // The first planet's storms (docs/plans/surface-storms.md), no far picture and no band: the dust storm «Пыльная буря» (A),
        // grit blown almost along the ground in an ochre haze; the rain storm «Косой дождь» (B), slanting rain in a cold grey-blue one.
        new(Content.Stretches.DustStorm.Id, Far: null, Band: null, Frost: false, new Weather(
            new Streak(0.2f, (120, 190), (1.4f, 4.2f), 0.45f, Low: true, [Rgba(132, 98, 56, 0.82f), Rgba(74, 56, 36, 0.78f), Rgba(176, 144, 98, 0.72f)]),
            Count: 240, new Wisps(["planet/dust_wisp_0", "planet/dust_wisp_1", "planet/dust_wisp_2"], Count: 8, Alpha: 0.42f, Speed: (50, 85)),
            Haze: Rgb(176, 138, 88), HazeMost: 0.62f, Dim: (0.62f, 0.53f, 0.42f), Wind: Sound.StormWind, Visibility: Washed)),
        new(Content.Stretches.RainStorm.Id, Far: null, Band: null, Frost: false, new Weather(
            new Streak(1.12f, (170, 230), (6, 11), 0.34f, Low: false, [Rgba(206, 218, 228, 0.52f), Rgba(150, 166, 182, 0.5f), Rgba(92, 106, 122, 0.42f)]),
            Count: 210, Wisps: null, Haze: Rgb(118, 132, 146), HazeMost: 0.5f, Dim: (0.7f, 0.74f, 0.8f), Wind: Sound.StormRain, Visibility: Washed)),
    ];

    /// <summary>Where each piece of ice lies in <c>stretches/ice_pieces</c>, the largest first: cut from the picked sheets by alpha.</summary>
    // A property, not a field: All above is made first and reads it.
    private static (int X, int Y, int W, int H)[] IcePieces =>
    [
        (4, 4, 458, 275), (466, 4, 278, 249), (748, 4, 328, 238), (1080, 4, 356, 201), (1440, 4, 277, 158), (1721, 4, 160, 132),
        (4, 283, 167, 122), (175, 283, 110, 105), (289, 283, 92, 98), (385, 283, 118, 91), (507, 283, 143, 90), (654, 283, 114, 80),
        (772, 283, 59, 74), (835, 283, 75, 69), (914, 283, 61, 68), (979, 283, 60, 63), (1043, 283, 53, 60), (1100, 283, 63, 59),
        (1167, 283, 38, 56), (1209, 283, 57, 54), (1270, 283, 65, 53), (1339, 283, 67, 50), (1410, 283, 49, 49), (1463, 283, 45, 46),
        (1512, 283, 47, 44), (1563, 283, 45, 44), (1612, 283, 63, 43), (1679, 283, 35, 41), (1718, 283, 61, 40), (1783, 283, 32, 36),
        (1819, 283, 47, 36), (1870, 283, 36, 36), (1910, 283, 36, 35), (1950, 283, 34, 34), (1988, 283, 31, 33), (4, 409, 28, 33),
        (36, 409, 35, 32), (75, 409, 35, 32), (114, 409, 32, 31), (150, 409, 27, 31), (181, 409, 33, 31), (218, 409, 29, 30),
        (251, 409, 32, 30), (287, 409, 27, 29), (318, 409, 28, 28), (350, 409, 30, 28), (384, 409, 27, 27), (415, 409, 30, 27),
        (449, 409, 26, 26), (479, 409, 27, 26), (510, 409, 32, 26), (546, 409, 32, 26), (582, 409, 23, 25), (609, 409, 27, 25),
        (640, 409, 26, 25), (670, 409, 24, 24), (698, 409, 24, 24), (726, 409, 24, 24), (754, 409, 24, 24), (782, 409, 23, 23),
        (809, 409, 24, 23), (837, 409, 22, 22), (863, 409, 28, 22), (895, 409, 20, 21), (919, 409, 20, 21), (943, 409, 21, 21),
        (968, 409, 22, 20), (994, 409, 19, 20), (1017, 409, 21, 20), (1042, 409, 23, 20), (1069, 409, 21, 19), (1094, 409, 21, 19),
        (1119, 409, 20, 17),
    ];

    /// <summary>How much of the camera's movement a far picture follows: farther than the far wrecks (<c>Backdrop.WreckParallax</c>).</summary>
    public const float Parallax = 0.15f;

    /// <summary>A piece of a field where it lies: <paramref name="Piece"/> of its pieces, its middle at <paramref name="X"/>, <paramref name="Y"/> in its place, drawn <paramref name="Scale"/> and turned <paramref name="Turn"/> radians, mirrored or not.</summary>
    public readonly record struct Spot(int Piece, float X, float Y, float Scale, float Turn, bool Mirror);

    /// <summary>
    /// The pieces of place <paramref name="place"/> of field <paramref name="field"/> seeded <paramref name="seed"/>, <paramref name="width"/>
    /// wide, into <paramref name="spots"/>: the larger pieces chosen more often than their number, so the field is not all crumbs.
    /// </summary>
    public static void PiecesAt(Field field, long place, uint seed, float width, List<Spot> spots)
    {
        ulong hash = (((ulong)place * 0x9E3779B97F4A7C15UL) ^ seed) + 0x632BE59BD9B4E019UL;
        float Next()
        {
            hash ^= hash << 13;
            hash ^= hash >> 7;
            hash ^= hash << 17;
            return (hash >> 40) / (float)(1 << 24);
        }
        for (int i = 0; i < field.Each; i++)
        {
            float x = Next() * width, y = (Next() + Next() - 1) * field.Tall / 2;
            int piece = (int)(MathF.Pow(Next(), 1.6f) * field.Pieces.Count);
            float scale = field.Small + (field.Large - field.Small) * Next(), turn = (Next() - 0.5f) * 0.6f;
            spots.Add(new(piece, x, y, scale, turn, Next() < 0.5f));
        }
    }

    /// <summary>The colour a far picture is drawn in: its paint is pale already, so only a little darker and cooler, its detail kept.</summary>
    public static readonly (float R, float G, float B) Veil = (0.56f, 0.6f, 0.68f);

    /// <summary>Ticks a stretch's look takes to ease back once it has gone: the frost melts, the far picture and the band fade (a minute).</summary>
    public const int Thaw = 60 * 20;

    /// <summary>How long the hull stays wet after the frost, in <see cref="Thaw"/>s from its end: the pick's 3.2 s to its 2.2.</summary>
    public const double Dry = 1.45;

    /// <summary>Which of <paramref name="count"/> pictures place <paramref name="place"/> of a far row seeded <paramref name="seed"/> takes: mixed, not in turn.</summary>
    public static int PictureAt(long place, uint seed, int count)
    {
        ulong hash = ((ulong)place + seed) * 0x9E3779B97F4A7C15UL;
        return (int)((hash >> 33) % (ulong)count);
    }

    /// <summary>The look of stretch <paramref name="id"/>; none for none or one unknown.</summary>
    public static Look? Of(string? id)
    {
        if (id == null) return null;
        foreach (var look in All)
            if (look.Id == id) return look;
        return null;
    }
}

/// <summary>
/// One view's reading of the stretch shown: which, how far its warning has run (<see cref="Grow"/>), and once it has gone how far
/// its look has eased back (<see cref="After"/>). Read from what the sky says now, every frame or on a panel's timer; only the
/// easing back is counted here, from <paramref name="clock"/> in ticks, so a peer that joins after a stretch ended sees none.
/// </summary>
public sealed class StretchShown
{
    private double? _ended;

    /// <summary>The stretch shown, coming, running or easing back; none.</summary>
    public StretchLooks.Look? Look { get; private set; }

    /// <summary>The share of its warning run, 0 at its start to 1 as it comes; 1 while it runs and after.</summary>
    public float Grow { get; private set; }

    /// <summary>0 till it has gone; then the ticks since, in <see cref="StretchLooks.Thaw"/>s, to <see cref="StretchLooks.Dry"/>.</summary>
    public double After { get; private set; }

    /// <summary>How far it has eased back, 0 to 1 over <see cref="StretchLooks.Thaw"/>.</summary>
    public float Melt => (float)Math.Min(1, After);

    /// <summary>The share of its band shown: its warning's run, eased back after.</summary>
    public float Shown => Grow * (1 - Melt);

    /// <summary>Ticks since its warning began, while it is coming or running: when it began, the same for every peer and after a load.</summary>
    public int Since { get; private set; }

    /// <summary>The whole warning of the stretch shown, in ticks: what <see cref="Since"/> counts past to its start.</summary>
    public int Warning { get; private set; }

    /// <summary>
    /// The sky now on a surface of <paramref name="rules"/>: as <see cref="See(string?, string?, int, int, double, int)"/>, the warning
    /// that of its own stretch.
    /// </summary>
    public void See(Content.Surfaces.Kind rules, string? running, string? coming, int comingIn, double clock, int ran = 0) =>
        See(running, coming, comingIn, (running ?? coming) is { } id ? rules.StretchOf(id)?.Warning ?? 0 : 0, clock, ran);

    /// <summary>
    /// The sky now: <paramref name="running"/>, the stretch running <paramref name="ran"/> ticks, else <paramref name="coming"/> with
    /// <paramref name="comingIn"/> ticks of its warning left (<c>SkyNow</c>), the whole of which is <paramref name="warning"/>;
    /// <paramref name="clock"/> the world's ticks, between them too.
    /// </summary>
    public void See(string? running, string? coming, int comingIn, int warning, double clock, int ran = 0)
    {
        if ((running ?? coming) is { } id)
        {
            Look = StretchLooks.Of(id);
            Grow = running != null ? 1 : Math.Clamp(1 - comingIn / (float)Math.Max(1, warning), 0, 1);
            Since = running != null ? warning + ran : Math.Max(0, warning - comingIn);
            Warning = warning;
            (After, _ended) = (0, null);
            return;
        }
        if (Look == null) return;
        _ended ??= clock;
        After = Math.Max(0, (clock - _ended.Value) / StretchLooks.Thaw);
        if (After < StretchLooks.Dry) return;
        (Look, Grow, After, _ended) = (null, 0, 0, null);
    }
}
