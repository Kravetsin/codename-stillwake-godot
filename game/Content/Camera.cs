namespace Stillwake.Game;

/// <summary>
/// How the camera follows this machine's hero (<c>features/crew/CameraFollow.cs</c>), as the movement review's
/// item 12 picked it. It leads the way the hero moves by <see cref="LeadShare"/> of the screen's width at
/// <see cref="LeadSpeed"/> or faster, the lead turning over <see cref="LeadTurnMs"/>, and eases onto where it
/// aims over <see cref="SmoothMs"/>. Under gravity it keeps the height the hero last stood at through a jump: it
/// rises only once the body's middle is more than <see cref="HoldUp"/> cells above it and drops once it is more
/// than <see cref="HoldDown"/> below, never letting a fall get more than <see cref="CatchDown"/> below the
/// screen's middle. Floating in space it holds nothing and leads up and down too.
/// </summary>
public static class Camera
{
    /// <summary>
    /// How far ahead the camera aims at full lead, as a share of the screen's width: 6 cells of the farthest zoom's 96. The
    /// easing trails a moving aim by its speed for <see cref="SmoothMs"/>, so a run looks about 3.6 cells ahead there, a walk 2.
    /// </summary>
    public const double LeadShare = 1.0 / 16;

    /// <summary>The speed of the full lead, in thousandths of a cell a controller step: a run; a walk leads about half as far.</summary>
    public const int LeadSpeed = Stillwake.Content.Hero.Run;

    /// <summary>How long the lead takes to turn, as the time constant it eases by, in milliseconds.</summary>
    public const double LeadTurnMs = 350;

    /// <summary>How long the camera takes to reach where it aims, as the time constant it eases by, in milliseconds.</summary>
    public const double SmoothMs = 180;

    /// <summary>
    /// How far the body's middle may rise above the height held before the camera follows it up, in cells: over a
    /// whole jump, which rises about 4 aboard and about 6 under a planet's two thirds of the ship's gravity.
    /// </summary>
    public const double HoldUp = 6.5;

    /// <summary>How far the body's middle may drop below the height held before the camera follows it down, in cells.</summary>
    public const double HoldDown = 2;

    /// <summary>The farthest a fall gets below the screen's middle, in cells, however fast it falls.</summary>
    public const double CatchDown = 2.4;
}
