using Stillwake.Sim.Crew;

namespace Stillwake.Game;

/// <summary>
/// How a hurt shows (2026-11-08-hurts-are-shown), the user's picks from the poison feedback page: the screen's edges wash in
/// the kind's colour and fade, and the air gauge's lost part blinks in it before it drains white. One look for every kind.
/// </summary>
public static class HurtLook
{
    /// <summary>The edge's wash: up in <see cref="RiseS"/>, gone by <see cref="FadeS"/>, at most <see cref="Most"/> opaque.</summary>
    public const double RiseS = 0.06, FadeS = 0.7, Most = 0.8;

    /// <summary>How long the gauge's lost part blinks in the kind's colour, and how fast.</summary>
    public const double BlinkS = 0.6, BlinkHz = 7;

    /// <summary>A kind's colour, as hex: muted, the grim ink and watercolour of the rest.</summary>
    public static string Of(Harm harm) => harm switch
    {
        Harm.Blow => "b0302a",
        Harm.Shock => "8fd8ff",
        Harm.Poison => "c6e260",
        Harm.Burn => "ff8a2a",
        _ => "ffffff",
    };

    /// <summary>The edge's opacity <paramref name="seconds"/> after a hurt: a quick rise, an eased fade, nothing outside.</summary>
    public static double Edge(double seconds)
    {
        if (seconds < 0 || seconds >= FadeS) return 0;
        if (seconds < RiseS) return Most * seconds / RiseS;
        double t = (seconds - RiseS) / (FadeS - RiseS);
        return Most * (1 - t * t * (3 - 2 * t));
    }

    /// <summary>How far the gauge's lost part is the kind's colour, 0 to 1, <paramref name="seconds"/> after a hurt: blinking, then white.</summary>
    public static double Blink(double seconds) =>
        seconds < 0 || seconds >= BlinkS ? 0 : 0.5 + 0.5 * System.Math.Cos(seconds * BlinkHz * 2 * System.Math.PI);
}
