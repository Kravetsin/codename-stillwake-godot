namespace Stillwake.Game;

/// <summary>
/// How a shot in flight looks (shots-are-the-sims): a streak <paramref name="Length"/> pixels of the world long and
/// <paramref name="Width"/> wide behind its point, fading from <paramref name="Head"/> to nothing, its point a square of
/// <paramref name="Width"/> in <paramref name="Head"/>, "rrggbb"; drawn unlit, so it shows in the dark and at the farthest zoom.
/// </summary>
public sealed record ShotLook(double Length, double Width, string Head);

/// <summary>Every projectile's look, by its id (<c>Content.Shots</c>).</summary>
public static class ShotLooks
{
    /// <summary>
    /// The nail: a bright steel streak two cells long, about what it flies in a frame at 60 fps (60 cells a second), so it reads as
    /// one moving line rather than a dot that jumps; a placeholder till a designer's.
    /// </summary>
    public static readonly ShotLook Nail = new(16, 2, "f4f7fb");

    /// <summary>The look of projectile <paramref name="id"/>: the nail's for any with none of its own yet.</summary>
    public static ShotLook Of(string id) => Nail;
}
