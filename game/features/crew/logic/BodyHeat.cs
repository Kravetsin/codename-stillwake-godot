using System;
using T = Stillwake.Content.Temperature;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The hero's body temperature (body-temperature), kept by its own client: each frame it closes <c>Temperature.Pull</c>
/// thousandths a second of its gap to where the degrees felt past its limits draw it, <c>Temperature.Sway</c> hundredths a degree
/// off <c>Normal</c>; away from normal no faster than <c>Temperature.Drift</c> thousandths a second for each degree felt, so the
/// deeper the cold the faster. Past either death the hero lives a last breath; at its end the client sends <c>Succumb</c>, and
/// the body stays as it died while the hero lies out, frosted over or scorched. Never saved or sent: a hero waking, a rejoin or creative mode starts it well.
/// </summary>
public sealed class BodyHeat
{
    /// <summary>A well body, in degrees.</summary>
    public const float Normal = T.Normal / 100f;

    /// <summary>The body, in degrees.</summary>
    public float Degrees { get; private set; } = Normal;

    /// <summary>Where <paramref name="felt"/> degrees past the cold's limit or <paramref name="heat"/> past the heat's draw the body, in degrees.</summary>
    public static float Target(int felt, int heat) => Normal + T.Sway * (heat - felt) / 100f;

    /// <summary>The most degrees a second a body moves away from normal feeling <paramref name="felt"/> or <paramref name="heat"/>.</summary>
    public static float Rate(int felt, int heat) => (felt + heat) * T.Drift / 1000f;

    /// <summary>The seconds a body lives on past either death, as a hero out of air does (2026-11-05-the-last-breath): ten.</summary>
    public const double LastBreathS = (double)Content.Crew.LastBreath / Sim.World.TicksPerSecond;

    private double _dying;

    /// <summary>
    /// The body <paramref name="seconds"/> on toward its <see cref="Target"/>. Past either death its last breath runs; back inside
    /// both in time it ends. True on the step that breath runs out: the hero dies, and the body stays so (<see cref="Gone"/>).
    /// </summary>
    public bool Step(double seconds, int felt, int heat)
    {
        float gap = Target(felt, heat) - Degrees;
        float step = gap * (float)(1 - Math.Pow(1 - T.Pull / 1000.0, seconds));
        if (Math.Abs(Degrees + step - Normal) > Math.Abs(Degrees - Normal))
        {
            float most = (float)(Rate(felt, heat) * seconds);
            step = Math.Clamp(step, -most, most);
        }
        Degrees += step;
        _dying = Dead(Degrees) ? _dying + seconds : 0;
        return Gone = _dying >= LastBreathS;
    }

    /// <summary>Well again: woken, rejoined or creative.</summary>
    public void Reset() => (Degrees, _dying, Gone) = (Normal, 0, false);

    /// <summary>Whether its last breath ran out: its client steps it no more till <see cref="Reset"/>, the hero lying as it died.</summary>
    public bool Gone { get; private set; }

    /// <summary>
    /// The ticks left of its last breath, as the sim counts a hero's out of air (<c>Hero.Gasp</c>), for the screen to close as
    /// that one does; 0 with none, 1 once gone, till the sim has the hero out.
    /// </summary>
    public int GaspLeft => Gone ? 1 : _dying > 0 ? Math.Max(1, (int)Math.Ceiling((LastBreathS - _dying) * Sim.World.TicksPerSecond)) : 0;

    /// <summary>Whether a body of <paramref name="degrees"/> has reached the cold's death or the heat's.</summary>
    public static bool Dead(float degrees) => degrees <= T.ColdDeath / 100f || degrees >= T.HeatDeath / 100f;

    /// <summary>
    /// The thousandths of its speed a hero moves on foot and works its hand tools at with its body at <paramref name="degrees"/>:
    /// whole at normal, falling straight to <c>Temperature.Least</c> at either death, each hundredth of a degree its share (rounded
    /// down, so any chill shows). Afloat the body never slows it: nothing there weighs.
    /// </summary>
    public static int SpeedOf(float degrees)
    {
        int off = (int)MathF.Round(Math.Abs(degrees - Normal) * 100);
        int span = degrees < Normal ? T.Normal - T.ColdDeath : T.HeatDeath - T.Normal;
        return Math.Clamp(1000 - (off * (1000 - T.Least) + span - 1) / span, T.Least, 1000);
    }
}
