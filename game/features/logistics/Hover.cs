using System;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.DroneLook;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Features.Logistics;

/// <summary>
/// How a drone is drawn moving (0062), apart from Godot: one rig a drone, in the world's pixels and seconds. A spring
/// follows a target — the sim's place between ticks, lifted, and its speed — so it eases off and in, leans past a stop a
/// little and rounds a corner; it leans into its speed and its acceleration, faces the way it flies, bobs, spins its
/// rotors and swings its cargo as a pendulum. The audition page's model (drone-art, <c>page.js</c>), stepped at 240 Hz.
/// </summary>
public sealed class Hover
{
    /// <summary>The longest step the spring takes: shorter than a frame, so a stiff spring stays steady at any frame rate.</summary>
    public const double MaxStep = 1.0 / 240;

    // The model's own shape, not a pick: how fast the lean and the rotors follow, the pendulum's own swing, and the
    // speed past which it turns to face its way.
    private const double TiltEaseS = 0.08, SpinEaseS = 0.12, RotorFps = 20, FaceAt = 6;
    private const double CargoHz = 1.4, CargoDamping = 0.3, CargoMost = 0.6;

    private readonly int _id;

    public double X { get; private set; }
    public double Y { get; private set; }
    public double Vx { get; private set; }
    public double Vy { get; private set; }
    /// <summary>Its lean, in radians, clockwise.</summary>
    public double Tilt { get; private set; }
    /// <summary>Which way it faces: 1 right, -1 left.</summary>
    public int Face { get; private set; } = 1;
    /// <summary>Its cargo's swing from straight down, in radians.</summary>
    public double Swing { get; private set; }
    /// <summary>How fast its rotors turn, 0 to 1.</summary>
    public double Spin { get; set; }

    private double _swingV, _phase, _t;

    public Hover(int id)
    {
        _id = id;
        // Each drone out of step with the next: its rotors and its bob.
        _phase = id * 1.7;
        _t = id * 0.37;
    }

    /// <summary>Set down at (<paramref name="x"/>, <paramref name="y"/>), still and level.</summary>
    public void Place(double x, double y)
    {
        (X, Y, Vx, Vy, Tilt, Swing, _swingV) = (x, y, 0, 0, 0, 0, 0);
    }

    /// <summary>
    /// Moved on <paramref name="dt"/> seconds toward (<paramref name="tx"/>, <paramref name="ty"/>) moving at
    /// (<paramref name="tvx"/>, <paramref name="tvy"/>), its rotors toward <paramref name="spinTo"/>; <paramref name="cruise"/>
    /// is a drone's speed in flight, pixels a second. In steps of at most <see cref="MaxStep"/>.
    /// </summary>
    public void Step(double dt, double tx, double ty, double tvx, double tvy, double spinTo, double cruise, L.Motion m)
    {
        int n = Math.Max(1, (int)Math.Ceiling(dt / MaxStep));
        for (int i = 0; i < n; i++) StepOnce(dt / n, tx, ty, tvx, tvy, spinTo, cruise, m);
    }

    private void StepOnce(double dt, double tx, double ty, double tvx, double tvy, double spinTo, double cruise, L.Motion m)
    {
        double w = 2 * Math.PI * m.SpringHz, z = m.Damping;
        double ax = w * w * (tx - X) + 2 * z * w * (tvx - Vx);
        double ay = w * w * (ty - Y) + 2 * z * w * (tvy - Vy);
        Vx += ax * dt;
        Vy += ay * dt;
        X += Vx * dt;
        Y += Vy * dt;
        // A little into its speed while it cruises, more into its acceleration as it sets off or brakes.
        double deg = Math.Clamp(Vx / cruise * m.TiltCruise + ax * 0.012 * (m.TiltMax / 14), -m.TiltMax, m.TiltMax);
        Tilt += (deg * Math.PI / 180 - Tilt) * (1 - Math.Exp(-dt / TiltEaseS));
        if (Vx > FaceAt) Face = 1;
        else if (Vx < -FaceAt) Face = -1;
        // The cargo, a pendulum the acceleration pushes back.
        double wc = 2 * Math.PI * CargoHz, kc = m.Swing * Math.PI / 180 * wc * wc / 500;
        _swingV += (-wc * wc * Swing - 2 * CargoDamping * wc * _swingV - kc * ax) * dt;
        Swing = Math.Clamp(Swing + _swingV * dt, -CargoMost, CargoMost);
        Spin += (spinTo - Spin) * (1 - Math.Exp(-dt / SpinEaseS));
        _phase += dt * RotorFps * Spin;
        _t += dt;
    }

    /// <summary>Its rotors' picture, 0 to <see cref="L.RotorFrames"/> - 1; the first while they stand.</summary>
    public int Frame => Spin < 0.05 ? 0 : (int)Math.Floor(_phase) % L.RotorFrames;

    /// <summary>How far it bobs now, pixels down: half as much at full speed, none with its rotors still.</summary>
    public double Bob(double cruise, L.Motion m)
    {
        double moving = Math.Min(1, Math.Sqrt(Vx * Vx + Vy * Vy) / cruise);
        return m.BobAmp * Math.Sin(2 * Math.PI * _t / m.BobPeriod + _id * 2.1) * (1 - 0.5 * moving) * Spin;
    }

    /// <summary>Its speed as a share of a cruising drone's, 0 to 1.</summary>
    public double Speed(double cruise) => Math.Min(1, Math.Sqrt(Vx * Vx + Vy * Vy) / cruise);

    /// <summary>A point of its body, (<paramref name="dx"/>, <paramref name="dy"/>) from its middle as it faces right, turned and mirrored as it is, at <paramref name="bob"/>.</summary>
    public (double X, double Y) Body(double dx, double dy, double bob)
    {
        double c = Math.Cos(Tilt), s = Math.Sin(Tilt), fx = dx * Face;
        return (X + fx * c - dy * s, Y + bob + fx * s + dy * c);
    }

    /// <summary>Where its cargo's middle hangs: from the clamp under its body, along its swing.</summary>
    public (double X, double Y) Cargo(double bob)
    {
        var (cx, cy) = Body(0, L.Clamp, bob);
        return (cx + Math.Sin(Swing) * L.Tether, cy + Math.Cos(Swing) * L.Tether);
    }

    /// <summary>
    /// How far over its cell's centre a drone flies, by the structure of its cell (<paramref name="here"/>) and of the one
    /// above: <c>LiftUnder</c> under a solid cell, so it never pokes into a ceiling, else <c>Lift</c>. Inside a door, the
    /// door's own cells above are no ceiling: it flies through at its height, and dips only under the wall over the door.
    /// </summary>
    public static double Lift(int here, int above, L.Motion m) =>
        Solid(above) && !(IsDoor(here) && IsDoor(above)) ? m.LiftUnder : m.Lift;

    private static bool IsDoor(int structure) => structure is Structure.DoorClosed or Structure.DoorOpen;

    /// <summary>Whether a cell of <paramref name="structure"/> is solid over a drone: a wall, a floor, a closed door or a closed hatch; none above the grid's top.</summary>
    public static bool Solid(int structure) => structure is Structure.Wall or Structure.Floor or Structure.DoorClosed or Structure.HatchClosed;

    /// <summary>
    /// A landing <paramref name="t"/> seconds in: how far over its pad the drone is wanted, pixels, how fast its rotors
    /// should turn, and whether it has touched down. Over the pad first, then down onto it, eased, as the rotors slow.
    /// </summary>
    public static (double Above, double SpinTo, bool Down) Landing(double t)
    {
        if (t < L.LandOverS) return (L.LandAbove, 1, false);
        if (t >= L.LandOverS + L.LandDownS) return (0, 0, true);
        double k = (t - L.LandOverS) / L.LandDownS, e = 1 - (1 - k) * (1 - k);
        return (L.LandAbove * (1 - e), 1 - k, false);
    }
}
