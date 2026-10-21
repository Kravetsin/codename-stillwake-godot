namespace Stillwake.Game;

/// <summary>
/// How the camera follows this machine's hero (<c>features/crew/CameraFollow.cs</c>), as the movement review's
/// item 12 picked it and its playtest changed it. Across, it sits ahead of the hero the way it moves, by
/// <see cref="LeadShare"/> of the screen's width at <see cref="LeadSpeed"/> or faster: the lead turns over
/// <see cref="LeadTurnMs"/> and eases over <see cref="SmoothMs"/>, while the hero itself is followed exactly, so
/// nothing trails. Up and down under gravity it always follows the body's middle, eased over <see cref="SmoothMs"/>,
/// jumps too, and never more than <see cref="Catch"/> cells behind. Floating in space it leads up and down as across.
/// </summary>
public static class Camera
{
    /// <summary>How far ahead the camera looks at full lead, as a share of the screen's width: 6 cells of the farthest zoom's 96.</summary>
    public const double LeadShare = 1.0 / 16;

    /// <summary>The speed of the full lead, in thousandths of a cell a controller step: a run; a walk leads about half as far.</summary>
    public const int LeadSpeed = Stillwake.Content.Hero.Run;

    /// <summary>How long the lead takes to turn, as the time constant it eases by, in milliseconds.</summary>
    public const double LeadTurnMs = 350;

    /// <summary>How long the camera takes to settle, as the time constant it eases by, in milliseconds: the lead's size, and the height.</summary>
    public const double SmoothMs = 180;

    /// <summary>The farthest the body's middle gets above or below the screen's middle under gravity, in cells, however fast it moves.</summary>
    public const double Catch = 1.5;
}
