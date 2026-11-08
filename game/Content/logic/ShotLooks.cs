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
}

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

    /// <summary>The look of projectile <paramref name="id"/> fired from <paramref name="gun"/>: the railgun's and a thrown charge's their own, else the nail's.</summary>
    public static ShotLook Of(string id, string? gun) =>
        gun == Content.Shots.Railgun.Item ? Railed : gun == Content.Shots.Thrown.Item ? Charge : Nail;
}
