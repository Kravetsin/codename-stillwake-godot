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
    /// The tightest a swinging leg's knee folds on a slope, as a share of its reach to its ankle: past it the foot comes down
    /// instead, into the ground no deeper than <see cref="Lip"/> hides. Keeps a knee off the slope ahead.
    /// </summary>
    public const double Fold = 0.7;

    /// <summary>
    /// How deep the lip drawn before the heroes runs under a slope's surface, in pixels (<c>GridView</c>): the slope's own wall
    /// cut to that band, so a level boot's end in the ground is hidden. At 45° a boot nine pixels long spans nine of height.
    /// </summary>
    public const int Lip = 10;

    /// <summary>How far above or below the hero's feet a foot looks for ground; past it the foot keeps the hero's level.</summary>
    public const double Reach = 12;

    /// <summary>The most the hips drop for a foot on lower ground; past it the leg reaches straight and the foot hangs.</summary>
    public const double MostDrop = 14;

    /// <summary>The share of the way the hips' drop and the page's walk move toward their aim each 60th of a second (the page's).</summary>
    public const double Ease = 0.35;
}
