namespace Stillwake.Game;

/// <summary>
/// How a shot in flight looks (shots-are-the-sims): a streak <paramref name="Length"/> pixels of the world long behind its point,
/// fading from <paramref name="Head"/> at the point to nothing, "rrggbb".
/// </summary>
public sealed record ShotLook(double Length, string Head);

/// <summary>Every projectile's look, by its id (<c>Content.Shots</c>).</summary>
public static class ShotLooks
{
    /// <summary>The nail: a short bright steel streak, three quarters of a cell, a placeholder till a designer's.</summary>
    public static readonly ShotLook Nail = new(6, "dfe4ec");

    /// <summary>The look of projectile <paramref name="id"/>: the nail's for any with none of its own yet.</summary>
    public static ShotLook Of(string id) => Nail;
}
