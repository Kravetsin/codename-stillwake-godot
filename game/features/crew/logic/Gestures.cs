using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// How a gesture plays on the near arm (gestures-for-words): its motion (<see cref="GestureMotions"/>) over the arm the pose gives, blended in and
/// out, whatever the hand held stowed meanwhile; who may start one, and how one goes over the wire. Client only: a gesture changes
/// nothing the world holds.
/// </summary>
public static class Gestures
{
    /// <summary>How long a gesture takes to come in and to go out again, in milliseconds.</summary>
    public const double BlendMs = 180;

    /// <summary>
    /// Pointing behind its back, the arm stops this far past straight up or down, in radians from ahead: the hero faces the pointer,
    /// so only walking past the place it points at turns it behind.
    /// </summary>
    public const double PointMost = Math.PI / 2 + 0.35;

    /// <summary>The gesture key's wheel (<see cref="Wheel{T}"/>), from the top clockwise: yes above, no below right, wave below left; a tap points.</summary>
    public static readonly Gesture[] WheelSides = [Gesture.Yes, Gesture.No, Gesture.Wave];

    public static double MsOf(Gesture gesture) => GestureMotions.Of(gesture).Ms;

    /// <summary>How much of the arm <paramref name="motion"/> <paramref name="ms"/> in takes from the pose: none at its start and end, eased.</summary>
    public static double WeightAt(GestureMotion motion, double ms)
    {
        double t = Math.Clamp(Math.Min(ms, motion.Ms - ms) / BlendMs, 0, 1);
        return t * t * (3 - 2 * t);
    }

    /// <summary>
    /// The near arm, the hand's turn against the forearm and the head's nod of <paramref name="motion"/> <paramref name="ms"/> in, an
    /// aimed one laid along the line to <paramref name="toward"/> from the shoulder in the arm's frame (ahead and down positive).
    /// </summary>
    public static (Bend Arm, double Wrist, double Head) ArmAt(GestureMotion motion, double ms, (double X, double Y) toward)
    {
        var key = motion.At(Math.Clamp(ms / motion.Ms, 0, 1));
        // The line to the target as the upper arm's turn from hanging: straight ahead a quarter turn.
        double line = motion.Aimed ? Math.PI / 2 - Math.Clamp(Math.Atan2(toward.Y, toward.X), -PointMost, PointMost) : 0;
        return (new Bend(line + key.Upper, key.Elbow), key.Wrist, key.Head);
    }

    /// <summary>
    /// Whether a hero in <paramref name="pose"/> may start a gesture: on its feet, jumping or afloat, its hands at no other work and the
    /// plan away; never on a ladder, sitting, strapped in or passed out. What it holds is stowed while it plays.
    /// </summary>
    public static bool CanStart(Pose pose, bool handsBusy, bool holdsPlan) =>
        pose is Pose.Stand or Pose.Walk or Pose.Run or Pose.Air or Pose.Float && !handsBusy && !holdsPlan;

    /// <summary>The gesture <paramref name="work"/> is, sent as hands' work (<see cref="Busy"/>); none for other work.</summary>
    public static Gesture? Of(Handwork? work) => work switch
    {
        Handwork.Point => Gesture.Point,
        Handwork.Wave => Gesture.Wave,
        Handwork.Yes => Gesture.Yes,
        Handwork.No => Gesture.No,
        _ => null,
    };

    public static Handwork WorkOf(Gesture gesture) => gesture switch
    {
        Gesture.Point => Handwork.Point,
        Gesture.Wave => Handwork.Wave,
        Gesture.Yes => Handwork.Yes,
        _ => Handwork.No,
    };
}

/// <summary>
/// A gesture as it plays on one hero (gestures-for-words): this machine's, started by its keys, or another's, heard from what its peer sends.
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
