namespace Stillwake.Game;

/// <summary>
/// How a shot in flight looks (shots-are-the-sims): a line <paramref name="Length"/> pixels of the world long and
/// <paramref name="Width"/> wide behind its point, coloured from <paramref name="Tail"/> at its back through <paramref name="Body"/>
/// a quarter of the way along to <paramref name="Point"/> at its front, "rrggbb". Drawn unlit over a dark rim, so it shows on a
/// pale sky as in a cave.
/// </summary>
public sealed record ShotLook(double Length, double Width, string Tail, string Body, string Point);

/// <summary>Every projectile's look, by its id (<c>Content.Shots</c>).</summary>
public static class ShotLooks
{
    /// <summary>
    /// The nail, the user's pick «Стальной гвоздь»: the nail itself, a cell long, its bright head behind, its grey shank, its
    /// light point in front.
    /// </summary>
    public static readonly ShotLook Nail = new(8, 2, Tail: "f4f7fa", Body: "8a94a2", Point: "c9ced6");

    /// <summary>The look of projectile <paramref name="id"/>: the nail's, the only one flown yet.</summary>
    public static ShotLook Of(string id) => Nail;
}
