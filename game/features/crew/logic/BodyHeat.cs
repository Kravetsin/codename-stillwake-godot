using System;
using T = Stillwake.Content.Temperature;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The hero's body temperature (body-temperature), kept by its own client: each frame it closes <c>Temperature.Pull</c>
/// thousandths a second of its gap to where the degrees felt past its limits draw it, <c>Temperature.Sway</c> hundredths a degree
/// off <c>Normal</c>; away from normal no faster than <c>Temperature.Drift</c> a second. At either death the client sends
/// <c>Succumb</c>. Never saved or sent: a hero waking, a rejoin or creative mode starts it well.
/// </summary>
public sealed class BodyHeat
{
    /// <summary>A well body, in degrees.</summary>
    public const float Normal = T.Normal / 100f;

    /// <summary>The body, in degrees.</summary>
    public float Degrees { get; private set; } = Normal;

    /// <summary>Where <paramref name="felt"/> degrees past the cold's limit or <paramref name="heat"/> past the heat's draw the body, in degrees.</summary>
    public static float Target(int felt, int heat) => Normal + T.Sway * (heat - felt) / 100f;

    /// <summary>The body <paramref name="seconds"/> on toward its <see cref="Target"/>; true once it has reached either death.</summary>
    public bool Step(double seconds, int felt, int heat)
    {
        float gap = Target(felt, heat) - Degrees;
        float step = gap * (float)(1 - Math.Pow(1 - T.Pull / 1000.0, seconds));
        if (Math.Abs(Degrees + step - Normal) > Math.Abs(Degrees - Normal))
        {
            float most = (float)(T.Drift / 100.0 * seconds);
            step = Math.Clamp(step, -most, most);
        }
        Degrees += step;
        return Dead(Degrees);
    }

    /// <summary>Well again: woken, rejoined or creative.</summary>
    public void Reset() => Degrees = Normal;

    /// <summary>Whether a body of <paramref name="degrees"/> has reached the cold's death or the heat's.</summary>
    public static bool Dead(float degrees) => degrees <= T.ColdDeath / 100f || degrees >= T.HeatDeath / 100f;

    /// <summary>
    /// The thousandths of its speed a hero moves and works its hand tools at with its body at <paramref name="degrees"/>: whole within
    /// <c>Temperature.Easy</c> of normal, falling straight to <c>Temperature.Least</c> at either death.
    /// </summary>
    public static int SpeedOf(float degrees)
    {
        int off = (int)MathF.Round(Math.Abs(degrees - Normal) * 100) - T.Easy;
        int span = degrees < Normal ? T.Normal - T.ColdDeath - T.Easy : T.HeatDeath - T.Normal - T.Easy;
        return Math.Clamp(1000 - off * (1000 - T.Least) / span, T.Least, 1000);
    }
}
