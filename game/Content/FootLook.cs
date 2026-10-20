namespace Stillwake.Game;

/// <summary>
/// The hero's feet on uneven ground (<c>Footing</c>), in pixels of the world at zoom 1: each foot is planted on the ground
/// under it and the hips drop for the lower one to reach, as the ground pick page walked its hero over slopes.
/// </summary>
public static class FootLook
{
    /// <summary>How far above or below the hero's feet a foot looks for ground; past it the foot keeps the pose's height.</summary>
    public const double Reach = 12;

    /// <summary>The most the hips drop for a foot on lower ground; past it the leg reaches straight and the foot hangs.</summary>
    public const double MostDrop = 8;

    /// <summary>The share of the way the hips' drop and the stride's squeeze move toward their aim each 60th of a second.</summary>
    public const double Ease = 0.35;

    /// <summary>
    /// How much shorter the steps get on the steepest ground (45°): the feet that much nearer the middle, the stride that much
    /// quicker, so a planted foot keeps pace with the ground and the uphill knee never folds shut.
    /// </summary>
    public const double Squeeze = 0.6;
}
