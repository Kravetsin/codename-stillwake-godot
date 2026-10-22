using System;

namespace Stillwake.Game.Features.Fauna;

/// <summary>
/// How fast a creature's legs go (0102): plain C#, tested. Its speed is the sim's, the ground it covered on the last tick, eased
/// so a step up or a turn does not jolt its legs; its strides are counted by time at a rate that grows with that speed and is
/// capped (<see cref="StridesAt"/>), so a flight at 15 cells a second is a gallop of long bounds rather than a blur. Times in
/// milliseconds, speeds in pixels a second; the same at any frame rate.
/// </summary>
public sealed class CreatureGait(FaunaLooks.Run run)
{
    /// <summary>Its speed as its legs go by, eased toward the sim's.</summary>
    public double Speed { get; private set; }

    /// <summary>How far it has run, in strides: its legs' place is the fraction.</summary>
    public double Phase { get; private set; }

    /// <summary>How much it flees, 0 to 1, eased in and out with its flight.</summary>
    public double Flee { get; private set; }

    /// <summary>How much it is off the ground, 0 to 1, eased: its legs tucked.</summary>
    public double Air { get; private set; }

    /// <summary>
    /// Strides a second at <paramref name="speed"/>: one each <c>Stride</c> pixels at a crawl, more as it goes faster but never
    /// <c>MostStrides</c>, smoothly. Continuous and rising, so no speed jolts the legs from one rate to another.
    /// </summary>
    public static double StridesAt(double speed, FaunaLooks.Run run) =>
        run.MostStrides * Math.Tanh(Math.Max(0, speed) / (run.MostStrides * run.Stride));

    /// <summary>
    /// A frame of <paramref name="elapsedMs"/>: the sim moves it <paramref name="speed"/> pixels a second, on the ground or not,
    /// fleeing or not. Its strides go on only on the ground.
    /// </summary>
    public void Step(double elapsedMs, double speed, bool grounded, bool fleeing)
    {
        if (elapsedMs <= 0) return;
        Speed += (speed - Speed) * Ease(elapsedMs, FaunaLooks.SpeedMs);
        Flee += ((fleeing ? 1 : 0) - Flee) * Ease(elapsedMs, FaunaLooks.BlendMs);
        Air += ((grounded ? 0 : 1) - Air) * Ease(elapsedMs, FaunaLooks.BlendMs * 0.5);
        if (grounded) Phase += StridesAt(Speed, run) * elapsedMs / 1000;
    }

    /// <summary>How much its legs swing, 0 standing to 1 walking.</summary>
    public double Walking => Math.Clamp((Speed - FaunaLooks.StillBelow) / (FaunaLooks.WalkFrom - FaunaLooks.StillBelow), 0, 1);

    /// <summary>How much it gallops, 0 at a walk to 1 at full flight, eased at both ends.</summary>
    public double Gallop
    {
        get
        {
            double t = Math.Clamp((Speed - FaunaLooks.GallopFrom) / (FaunaLooks.GallopFull - FaunaLooks.GallopFrom), 0, 1);
            return t * t * (3 - 2 * t);
        }
    }

    /// <summary>The share of the way to its target an eased value goes in <paramref name="elapsedMs"/>, <paramref name="ms"/> its pace, whatever the frame rate.</summary>
    public static double Ease(double elapsedMs, double ms) => 1 - Math.Exp(-elapsedMs / ms);
}
