using System;
using Stillwake.Game.Features.Sound;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Logistics;

/// <summary>
/// A drone's rotors heard (0062), apart from Godot: its motor (<see cref="MotorSound"/> with <see cref="S.DroneMotor"/>)
/// spun up as it takes off and down once it touches down, and from it, how fast it flies and its id, its loop's pitch
/// scale and volume: higher and louder at speed, wavering on three slow sines, each drone tuned a little apart.
/// </summary>
public struct Whir
{
    private MotorSound _motor;
    private double _t;

    /// <summary>Whether it is heard: from the frame it takes off till its motor has run down after landing.</summary>
    public readonly bool Playing => _motor.Playing;

    /// <summary>Moved on <paramref name="elapsedS"/> seconds, its motor running while the drone is in the air.</summary>
    public void Step(bool flying, double elapsedS)
    {
        _motor.Step(flying, false, elapsedS, S.DroneMotor);
        _t += elapsedS;
    }

    /// <summary>Its loop's pitch scale for drone <paramref name="id"/> flying at <paramref name="speed"/> of a cruising drone's, 0 to 1.</summary>
    public readonly float Pitch(int id, double speed) =>
        (float)(Swarm(id) * _motor.Speed(S.DroneMotor) * (1 + S.DroneSpeedPitch * speed) * Math.Pow(2, S.DroneWobbleCents * Wobble(_t, id) / 1200));

    /// <summary>Its loop's volume on top of its cue's, in decibels: rising with its motor, louder at speed, swaying with its wobble.</summary>
    public readonly float GainDb(int id, double speed) =>
        (float)(_motor.GainDb(S.DroneMotor) + S.DroneSpeedDb * speed + S.DroneWobbleDb * Wobble(_t, id));

    /// <summary>How far drone <paramref name="id"/>'s wobble is, -1 to 1, <paramref name="t"/> seconds on: three slow sines, each drone its own phases.</summary>
    public static double Wobble(double t, int id)
    {
        double sum = 0;
        for (int k = 0; k < S.DroneWobble.Count; k++)
        {
            var (hz, share) = S.DroneWobble[k];
            sum += share * Math.Sin(2 * Math.PI * (hz * t + Frac(id * 0.7548776662 + k * 0.5698402910)));
        }
        return sum;
    }

    /// <summary>Drone <paramref name="id"/>'s own tuning, within <see cref="S.DroneSwarm"/> of 1: ids side by side land far apart.</summary>
    public static double Swarm(int id) => 1 + S.DroneSwarm * (Frac(id * 0.6180339887) * 2 - 1);

    private static double Frac(double x) => x - Math.Floor(x);
}
