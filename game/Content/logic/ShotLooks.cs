namespace Stillwake.Game;

/// <summary>
/// How a shot in flight looks (shots-are-the-sims): a line <paramref name="Length"/> pixels of the world long and
/// <paramref name="Width"/> wide behind its point, coloured from <paramref name="Tail"/> at its back through <paramref name="Body"/>
/// a quarter of the way along to <paramref name="Point"/> at its front, "rrggbb" or "rrggbbaa". Drawn unlit over a dark rim, so it
/// shows on a pale sky as in a cave; or, with a <paramref name="Glow"/>, over a wide soft line of that colour instead, light of its own.
/// One thrown whole has its <see cref="Picture"/> drawn instead, its line then a trail of sparks: specks <paramref name="Width"/> across
/// along its length, jittered, from <paramref name="Point"/> nearest it to <paramref name="Tail"/>.
/// </summary>
public sealed record ShotLook(double Length, double Width, string Tail, string Body, string Point, string? Glow = null)
{
    /// <summary>What is drawn of one thrown whole; none for a streak.</summary>
    public ShotPicture? Picture { get; init; }

    /// <summary>A bomb drawn in its place, pointing along its flight, its line then a short trail of sparks; none for a streak.</summary>
    public ShotBomb? Bomb { get; init; }

    /// <summary>A rocket drawn in its place, nose along its flight, its flame and smoke behind; none for a streak.</summary>
    public ShotRocket? Rocket { get; init; }
}

/// <summary>
/// A small bomb in flight, drawn as shapes lit by the world: a body <paramref name="Length"/> by <paramref name="Width"/> pixels of the
/// world, <paramref name="Fill"/> inside a <paramref name="Rim"/>, two <paramref name="Fins"/> splayed behind; behind them
/// <paramref name="Sparks"/> specks of its look's <see cref="ShotLook.Point"/> colour, every <paramref name="Gap"/> pixels from
/// <paramref name="First"/> back, fading.
/// </summary>
public sealed record ShotBomb(double Length, double Width, string Fill, string Rim, string Fins, int Sparks, double First, double Gap);

/// <summary>
/// A rocket in flight drawn as itself: its <paramref name="Still"/> picture (in <c>art/hd</c>), lit by the world, nose along its flight, its
/// <paramref name="Middle"/> (in that picture's pixels) on the shot's point, flipped flying left so its top stays up. Out of its nozzle,
/// <paramref name="Nozzle"/> pixels of the world behind its middle, a torn flame, unlit, <paramref name="Flame"/> long and
/// <paramref name="FlameWidth"/> wide at most, flickering anew every <paramref name="FlickerMs"/>; a light of <paramref name="Light"/> there to
/// <paramref name="Strength"/>, out to <paramref name="Reach"/> cells; a puff of <paramref name="Smoke"/> left behind every
/// <paramref name="SmokeMs"/>, blown back <paramref name="Push"/> and out <paramref name="Spread"/> pixels a second at most.
/// </summary>
public sealed record ShotRocket(
    string Still, (double X, double Y) Middle, double Nozzle, double Flame, double FlameWidth, double FlickerMs,
    (int R, int G, int B) Light, double Strength, int Reach, Render.Luminous Smoke, double SmokeMs, double Push, double Spread);

/// <summary>
/// A shot thrown whole, drawn as itself: its <paramref name="Still"/> picture (in <c>art/hd</c>), lit by the world, turning round its
/// <paramref name="Middle"/> (in that picture's pixels) <paramref name="Turns"/> times a second the way it was thrown; over it, unlit,
/// <paramref name="Frames"/> frames of its <paramref name="Spark"/> side by side, each <paramref name="Margin"/> pixels larger than the
/// still one on every side, cycled <paramref name="Fps"/> times a second; and a light of <paramref name="Light"/> about it, flickering
/// with the frames (<paramref name="Flicker"/>) to <paramref name="Strength"/>, keeping <paramref name="Keep"/> a cell out to
/// <paramref name="Reach"/> cells.
/// </summary>
public sealed record ShotPicture(
    string Still, (double X, double Y) Middle, double Turns, string Spark, int Frames, int Margin, double Fps,
    (int R, int G, int B) Light, double Strength, int Reach, float Keep, double[] Flicker);

/// <summary>Every shot's look (<c>Content.Shots</c>): by the gun that fired it till a projectile has a look of its own.</summary>
public static class ShotLooks
{
    /// <summary>
    /// The nail, the user's pick «Стальной гвоздь»: the nail itself, a cell long, its bright head behind, its grey shank, its
    /// light point in front.
    /// </summary>
    public static readonly ShotLook Nail = new(8, 2, Tail: "f4f7fa", Body: "8a94a2", Point: "c9ced6");

    /// <summary>
    /// A nail from the railgun (2026-10-27-the-railgun), the user's pick «Голубой след»: a long cyan tail, fading out behind, its
    /// head white, glowing.
    /// </summary>
    public static readonly ShotLook Railed = new(40, 2, Tail: "5fd8ff00", Body: "5fd8ffe6", Point: "e8fbff", Glow: "5fd8ff");

    /// <summary>
    /// The blasting charge thrown (blasting-charge), the user's pick «Труба с серой»: the pipe as it is held, turning twice a second,
    /// its fuse sparking through four frames, twelve a second, six sparks behind it, a small warm light about it.
    /// </summary>
    public static readonly ShotLook Charge = new(21, 0.9, Tail: "d8702c", Body: "d8702c", Point: "f4c070")
    {
        Picture = new ShotPicture("hero_blastCharge", (3.8, 2), 2, "shot_blastCharge_spark", 4, 2, 12,
            (240, 160, 64), 0.35, 2, 0.5f, [1, 0.35, 0.8, 0.15]),
    };

    /// <summary>
    /// A catcher's shell (catcher-combat), the user's pick «Столбы света»: a small feathered bomb, point first along its flight, two fins,
    /// four warm sparks trailing behind.
    /// </summary>
    public static readonly ShotLook Shell = new(8, 0.9, Tail: "ffdc8c", Body: "ffdc8c", Point: "ffdc8c")
    {
        Bomb = new ShotBomb(2.8, 1.1, Fill: "3a3d42", Rim: "16181b", Fins: "202226", Sparks: 4, First: 3.1, Gap: 1.6),
    };

    /// <summary>The missile's smoke: grey puffs swelling and thinning out over a second and a half, so its whole arc shows.</summary>
    private static readonly Render.Luminous Smoke = new(Render.Glimmer.Wisp, [(92, 90, 88)], (1400, 1800))
    { Matter = true, Fade = 2, Drag = 1.4, Turbulence = 10, Spin = 1, Size = 2, Grow = 3.5, Alpha = 0.34 };

    /// <summary>
    /// A catcher's missile (catcher-missile), the user's pick «Самоделка»: a scrap pipe with a welded cone, its torn orange flame and its
    /// grey smoke behind, a warm light at its nozzle. No streak of its own.
    /// </summary>
    public static readonly ShotLook Missile = new(0, 0, Tail: "00000000", Body: "00000000", Point: "00000000")
    {
        Rocket = new ShotRocket("shot_missile", (10, 5), Nozzle: 9.4, Flame: 10, FlameWidth: 1.5, FlickerMs: 40,
            Light: (255, 120, 40), Strength: 0.45, Reach: 3, Smoke, SmokeMs: 22, Push: 14, Spread: 18),
    };

    /// <summary>
    /// The look of projectile <paramref name="id"/> fired from <paramref name="gun"/>: the railgun's its own, a charge its own whoever
    /// threw it (a catcher's has no gun, catcher-combat), a catcher's shell and missile their own, else the nail's.
    /// </summary>
    public static ShotLook Of(string id, string? gun) =>
        gun == Content.Shots.Railgun.Item ? Railed : id == Content.Shots.BlastCharge.Id ? Charge : id == Content.Shots.Shell.Id ? Shell
        : id == Content.Shots.Missile.Id ? Missile : Nail;
}
