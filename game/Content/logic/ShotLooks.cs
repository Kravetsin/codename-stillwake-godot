namespace Stillwake.Game;

/// <summary>
/// How a shot in flight looks (shots-are-the-sims): a line <paramref name="Length"/> pixels of the world long and
/// <paramref name="Width"/> wide behind its point, coloured from <paramref name="Tail"/> at its back through <paramref name="Body"/>
/// a quarter of the way along to <paramref name="Point"/> at its front, "rrggbb" or "rrggbbaa". Drawn unlit over a dark rim, so it
/// shows on a pale sky as in a cave; or, with a <paramref name="Glow"/>, over a wide soft line of that colour instead, light of its own.
/// </summary>
public sealed record ShotLook(double Length, double Width, string Tail, string Body, string Point, string? Glow = null);

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

    /// <summary>The look of projectile <paramref name="id"/> fired from <paramref name="gun"/>: the railgun's its own, else the nail's.</summary>
    public static ShotLook Of(string id, string? gun) => gun == Content.Shots.Railgun.Item ? Railed : Nail;
}
