namespace Stillwake.Game;

/// <summary>
/// The hero's feet on uneven ground (<c>Footing</c>), in pixels of the world at zoom 1: on a slope they step as the ground
/// pick page's walker steps, each on the ground under it, the hips dropping for the lower one to reach.
/// </summary>
public static class FootLook
{
    /// <summary>
    /// The page's stride on a slope: each foot swings half this either side of the middle, and the stride runs two radians
    /// for each of its lengths walked.
    /// </summary>
    public const double Stride = 10;

    /// <summary>How high a foot is lifted as it swings forward on a slope, walking (the page's) and running.</summary>
    public const double Lift = 3.2, RunLift = 4.5;

    /// <summary>
    /// The tightest a knee folds on a slope, as a share of the leg's reach to its ankle: past it the hips drop no further, and
    /// the lower foot reaches with its toe or heel; a swinging foot comes down instead. Keeps a knee off the slope ahead.
    /// </summary>
    public const double Fold = 0.7;

    /// <summary>How much steeper than the slope, at most, a boot is sheared for its toe or heel to reach the ground: 1 is 45° more.</summary>
    public const double Tip = 1;

    /// <summary>How far above or below the hero's feet a foot looks for ground; past it the foot keeps the hero's level.</summary>
    public const double Reach = 12;

    /// <summary>The most the hips drop for a foot on lower ground; past it the leg reaches straight and the foot hangs.</summary>
    public const double MostDrop = 10;

    /// <summary>The share of the way the hips' drop and the page's walk move toward their aim each 60th of a second (the page's).</summary>
    public const double Ease = 0.35;
}
