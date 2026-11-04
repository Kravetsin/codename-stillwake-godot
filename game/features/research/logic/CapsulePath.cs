namespace Stillwake.Game.Features.Research;

/// <summary>
/// Where a trade station's capsule is drawn (2026-10-30-trade-capsule): off its station slowly, then faster, up and astern and away
/// from the camera, shrinking till it is gone; coming back the same way reversed. Between the two it is out of sight.
/// </summary>
public static class CapsulePath
{
    /// <summary>Ticks it is seen at each end of its flight.</summary>
    public const int Seen = 160;
    /// <summary>How far it goes while seen, in cells: up and astern.</summary>
    public const double Rise = 48, Astern = 28;
    /// <summary>How big it is drawn at its station, its picture's own size, and how small it is when it goes.</summary>
    public const double Near = 1, Far = 0.13;
    /// <summary>Ticks its flame burns the longer as it lifts off and as it touches down (trade-capsule-ink).</summary>
    public const int Kick = 10;

    /// <summary>Whether its flame kicks with <paramref name="left"/> ticks of its <paramref name="flight"/> left: just off its station, or just back.</summary>
    public static bool Kicks(double left, int flight) => flight - left < Kick || left < Kick;
    /// <summary>The share of the way past which it fades out.</summary>
    public const double FadeFrom = 0.8;

    /// <summary>What it looks like: its offset from the station's middle in cells, its size and how opaque.</summary>
    public readonly record struct Pose(double X, double Y, double Scale, double Alpha);

    /// <summary>Its pose with <paramref name="left"/> ticks of its <paramref name="flight"/> left; null while out of sight.</summary>
    public static Pose? At(double left, int flight)
    {
        double gone = flight - left;
        double way = gone < Seen ? gone / Seen : left < Seen ? left / Seen : -1;
        if (way < 0) return null;
        double eased = way * way;
        double alpha = way > FadeFrom ? (1 - way) / (1 - FadeFrom) : 1;
        return new Pose(-Astern * eased, -Rise * eased, Near + (Far - Near) * way, alpha);
    }
}
