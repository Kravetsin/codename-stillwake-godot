using System;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>A sign a hero makes with its near hand in place of words (emotes).</summary>
public enum Gesture { Point, Wave, Come, Yes, No }

/// <summary>
/// How each <see cref="Gesture"/> moves the near arm and shapes the hand (emotes): played over the arm the pose gives, blended in
/// and out, the hand's frame from its strip (hand), whatever it held stowed meanwhile. Client only: a gesture changes nothing the
/// world holds.
/// </summary>
public static class Gestures
{
    /// <summary>How long each gesture plays, in milliseconds.</summary>
    public const double PointMs = 1200, WaveMs = 1600, ComeMs = 1400, NodMs = 1000;

    /// <summary>How long a gesture takes to come in and to go out again, in milliseconds.</summary>
    public const double BlendMs = 180;

    /// <summary>The hand's frames in its strip (hand): 0 the grip it hangs and holds in, 1 open, 2 pointing, 3 the thumb out ahead.</summary>
    public const int GripFrame = 0, OpenFrame = 1, PointFrame = 2, ThumbFrame = 3;

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

    /// <summary>Beckoning: the upper arm out ahead at the chest, the forearm folding up from nearly straight to this far, this many times.</summary>
    public const double ComeUpper = 1.25, ComeOpen = 0.25, ComeFold = 1.35;
    public const int Comes = 2;

    /// <summary>
    /// A thumb up or down: the upper arm a little ahead, the forearm out ahead of the chest, the shoulder pumping it once up or down this far. The thumb
    /// stands out ahead of the hanging hand, so out ahead it points up; mirrored, down.
    /// </summary>
    public const double ThumbUpper = 0.45, YesLower = 1.25, NoLower = 0.95, Pump = 0.3;

    public static double MsOf(Gesture gesture) => gesture switch
    {
        Gesture.Point => PointMs,
        Gesture.Wave => WaveMs,
        Gesture.Come => ComeMs,
        _ => NodMs,
    };

    public static int FrameOf(Gesture gesture) => gesture switch
    {
        Gesture.Point => PointFrame,
        Gesture.Wave or Gesture.Come => OpenFrame,
        _ => ThumbFrame,
    };

    /// <summary>Whether the hand's frame is drawn mirrored about the wrist: the thumb down.</summary>
    public static bool Mirrored(Gesture gesture) => gesture == Gesture.No;

    /// <summary>How much of the arm the gesture <paramref name="ms"/> in takes from the pose: none at its start and end, eased.</summary>
    public static double WeightAt(Gesture gesture, double ms)
    {
        double t = Math.Clamp(Math.Min(ms, MsOf(gesture) - ms) / BlendMs, 0, 1);
        return t * t * (3 - 2 * t);
    }

    /// <summary>
    /// The near arm of a gesture <paramref name="ms"/> in, pointing at <paramref name="toward"/> from the shoulder in the arm's frame
    /// (ahead and down positive); the others need none.
    /// </summary>
    public static Bend ArmAt(Gesture gesture, double ms, (double X, double Y) toward)
    {
        double share = Math.Clamp(ms / MsOf(gesture), 0, 1);
        switch (gesture)
        {
            case Gesture.Point:
                double angle = Math.Clamp(Math.Atan2(toward.Y, toward.X), -PointMost, PointMost);
                double reach = PointReach * (Rig.Arm.Upper + Rig.Arm.Lower);
                return Rig.Reach(new Vec(Math.Cos(angle) * reach, Math.Sin(angle) * reach), Rig.Arm, 1);
            case Gesture.Wave:
                // The forearm upright over the raised elbow, rocking side to side from the middle.
                return new Bend(WaveUpper, Math.PI - WaveUpper + WaveSway * Math.Sin(2 * Math.PI * Waves * share));
            case Gesture.Come:
                return new Bend(ComeUpper, ComeOpen + ComeFold * (0.5 - 0.5 * Math.Cos(2 * Math.PI * Comes * share)));
            case Gesture.Yes:
                // The shoulder pumps, the forearm keeps its turn: the thumb stays upright as the hand rises.
                double up = Pump * Math.Sin(Math.PI * share);
                return new Bend(ThumbUpper + up, YesLower - up);
            default:
                double down = Pump * Math.Sin(Math.PI * share);
                return new Bend(ThumbUpper - down, NoLower + down);
        }
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
        Handwork.Come => Gesture.Come,
        Handwork.Yes => Gesture.Yes,
        Handwork.No => Gesture.No,
        _ => null,
    };

    public static Handwork WorkOf(Gesture gesture) => gesture switch
    {
        Gesture.Point => Handwork.Point,
        Gesture.Wave => Handwork.Wave,
        Gesture.Come => Handwork.Come,
        Gesture.Yes => Handwork.Yes,
        _ => Handwork.No,
    };
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

/// <summary>
/// The gesture key (emotes): a tap points at the pointer; held, a wheel opens round where the pointer was, a hand per side, and letting
/// go picks the one the pointer has moved toward, or none from the middle. A quick flick toward one picks it without waiting.
/// Distances are the screen's pixels, the caller's to scale.
/// </summary>
public sealed class GestureWheel
{
    /// <summary>A press let go sooner than this is a tap, unless it moved out of the middle; held longer, the wheel shows.</summary>
    public const double TapMs = 200;

    /// <summary>The middle's radius, where letting go picks nothing, and how far out each hand sits, in pixels at the normal slot size.</summary>
    public const double Middle = 18, Radius = 54;

    /// <summary>Each side's gesture, from the top clockwise: yes above, come on the right, no below, wave on the left.</summary>
    public static readonly Gesture[] Sides = [Gesture.Yes, Gesture.Come, Gesture.No, Gesture.Wave];

    private bool _held;
    private double _heldMs, _middle = Middle;

    /// <summary>Where the key was pressed, and where the pointer is now, on the screen.</summary>
    public (double X, double Y) From { get; private set; }
    public (double X, double Y) Pointer { get; private set; }

    /// <summary>Whether the wheel shows: the key held past a tap.</summary>
    public bool Open => _held && _heldMs >= TapMs;

    /// <summary>The gesture the pointer has moved toward, picked if the key is let go now; none in the middle.</summary>
    public Gesture? Toward => SideOf(Pointer.X - From.X, Pointer.Y - From.Y, _middle);

    /// <summary>
    /// Follows the key, <paramref name="held"/> or not, and the pointer at <paramref name="pointer"/> on the screen, the middle
    /// <paramref name="middle"/> pixels across: the gesture asked for as the key is let go, if any.
    /// </summary>
    public Gesture? Step(bool held, double elapsedMs, (double X, double Y) pointer, double middle)
    {
        (Pointer, _middle) = (pointer, middle);
        if (held)
        {
            if (!_held) (_held, _heldMs, From) = (true, 0, pointer);
            else _heldMs += elapsedMs;
            return null;
        }
        if (!_held) return null;
        _held = false;
        return Toward ?? (_heldMs < TapMs ? Gesture.Point : null);
    }

    /// <summary>The side an offset <paramref name="dx"/>, <paramref name="dy"/> from the middle points to, screen down positive; none within it.</summary>
    public static Gesture? SideOf(double dx, double dy, double middle)
    {
        if (dx * dx + dy * dy < middle * middle) return null;
        return Math.Abs(dx) > Math.Abs(dy) ? (dx > 0 ? Sides[1] : Sides[3]) : (dy < 0 ? Sides[0] : Sides[2]);
    }
}
