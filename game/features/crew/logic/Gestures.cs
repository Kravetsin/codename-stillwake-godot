using System;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>A sign a hero makes with its near hand in place of words (emotes): pointing at a place, or waving.</summary>
public enum Gesture { Point, Wave }

/// <summary>
/// How each <see cref="Gesture"/> moves the near arm and shapes the hand (emotes): played over the arm the pose gives, blended in
/// and out, the hand's frame from its strip (hand). Client only: a gesture changes nothing the world holds.
/// </summary>
public static class Gestures
{
    /// <summary>How long each gesture plays, in milliseconds.</summary>
    public const double PointMs = 1200, WaveMs = 1600;

    /// <summary>How long a gesture takes to come in and to go out again, in milliseconds.</summary>
    public const double BlendMs = 180;

    /// <summary>The hand's frames in its strip (hand): 0 the grip it hangs and holds in, 1 open, 2 pointing.</summary>
    public const int GripFrame = 0, OpenFrame = 1, PointFrame = 2;

    /// <summary>How far of its whole length the arm reaches out pointing: straight, but not locked.</summary>
    public const double PointReach = 0.97;

    /// <summary>
    /// Pointing behind its back, the arm stops this far past straight up or down, in radians from ahead: the hero faces the pointer,
    /// so only walking past the place it points at turns it behind.
    /// </summary>
    public const double PointMost = Math.PI / 2 + 0.35;

    /// <summary>Waving: the upper arm raised up and ahead, radians from hanging; the forearm upright, rocked this far to each side, this many times.</summary>
    public const double WaveUpper = 2.5, WaveSway = 0.45;
    public const int Waves = 3;

    public static double MsOf(Gesture gesture) => gesture == Gesture.Point ? PointMs : WaveMs;

    public static int FrameOf(Gesture gesture) => gesture == Gesture.Point ? PointFrame : OpenFrame;

    /// <summary>How much of the arm the gesture <paramref name="ms"/> in takes from the pose: none at its start and end, eased.</summary>
    public static double WeightAt(Gesture gesture, double ms)
    {
        double t = Math.Clamp(Math.Min(ms, MsOf(gesture) - ms) / BlendMs, 0, 1);
        return t * t * (3 - 2 * t);
    }

    /// <summary>
    /// The near arm of a gesture <paramref name="ms"/> in, pointing at <paramref name="toward"/> from the shoulder in the arm's frame
    /// (ahead and down positive); the wave needs none.
    /// </summary>
    public static Bend ArmAt(Gesture gesture, double ms, (double X, double Y) toward)
    {
        if (gesture == Gesture.Point)
        {
            double angle = Math.Clamp(Math.Atan2(toward.Y, toward.X), -PointMost, PointMost);
            double reach = PointReach * (Rig.Arm.Upper + Rig.Arm.Lower);
            return Rig.Reach(new Vec(Math.Cos(angle) * reach, Math.Sin(angle) * reach), Rig.Arm, 1);
        }
        // The forearm upright over the raised elbow, rocking side to side from the middle.
        double sway = WaveSway * Math.Sin(2 * Math.PI * Waves * Math.Clamp(ms / WaveMs, 0, 1));
        return new Bend(WaveUpper, Math.PI - WaveUpper + sway);
    }

    /// <summary>
    /// Whether a hero in <paramref name="pose"/> may start a gesture: on its feet, jumping or afloat, its hands at no other work and the
    /// plan away; never on a ladder, sitting, strapped in or passed out.
    /// </summary>
    public static bool CanStart(Pose pose, bool handsBusy, bool holdsPlan) =>
        pose is Pose.Stand or Pose.Walk or Pose.Run or Pose.Air or Pose.Float && !handsBusy && !holdsPlan;

    /// <summary>The gesture <paramref name="work"/> is, sent as hands' work (<see cref="Busy"/>); none for other work.</summary>
    public static Gesture? Of(Handwork? work) => work switch
    {
        Handwork.Point => Gesture.Point,
        Handwork.Wave => Gesture.Wave,
        _ => null,
    };

    public static Handwork WorkOf(Gesture gesture) => gesture == Gesture.Point ? Handwork.Point : Handwork.Wave;
}

/// <summary>
/// A gesture as it plays on one hero (emotes): this machine's, started by its keys, or another's, heard from what its peer sends.
/// <see cref="Count"/> numbers them, so the same gesture made twice is news, as a chop's swings are (<see cref="Busy"/>).
/// </summary>
public sealed class GesturePlay
{
    /// <summary>The gesture playing; none between.</summary>
    public Gesture? Kind { get; private set; }

    /// <summary>Where a point points, in thousandths of a cell, as the hero's own position.</summary>
    public Point At { get; private set; }

    /// <summary>How many gestures this hero has begun.</summary>
    public int Count { get; private set; }

    /// <summary>How far into it, in milliseconds.</summary>
    public double Ms { get; private set; }

    public void Start(Gesture kind, Point at)
    {
        (Kind, At, Ms) = (kind, at, 0);
        Count++;
    }

    public void Stop() => Kind = null;

    /// <summary>Plays on by <paramref name="elapsedMs"/>; it ends as its time runs out.</summary>
    public void Step(double elapsedMs)
    {
        if (Kind is not { } kind) return;
        Ms += elapsedMs;
        if (Ms >= Gestures.MsOf(kind)) Kind = null;
    }

    /// <summary>What is sent of it while it plays: the hands' work (<see cref="Busy"/>).</summary>
    public Busy? Sent => Kind is { } kind ? new Busy(Gestures.WorkOf(kind), At, Count) : null;

    /// <summary>
    /// Follows another's hero by what its peer sent: a gesture numbered anew starts here from its beginning, on this machine's clock;
    /// other work of its hands cuts the one playing off.
    /// </summary>
    public void Hear(Busy? busy)
    {
        if (busy is not { } sent) return;
        if (Gestures.Of(sent.Work) is { } kind)
        {
            if (sent.Swings != Count) (Kind, At, Count, Ms) = (kind, sent.At, sent.Swings, 0);
        }
        else Kind = null;
    }
}
