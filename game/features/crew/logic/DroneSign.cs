using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A gesture shown by the light drone (light-drone): the wheel's hand as a hologram over the drone, so friends far off read it. It
/// rises out of the lens as the gesture blends in, holds, and sinks back as it blends out (<see cref="Gestures.WeightAt"/>);
/// a point's hand turns toward where it points. Client only, from the gesture already playing on the figure. Lengths in world px.
/// </summary>
public static class DroneSign
{
    /// <summary>The hand picture's height, and how far over the drone's middle its middle stands once risen.</summary>
    public const double Height = 10, Above = 11;

    /// <summary>Its size as it leaves the lens, a share of its full size.</summary>
    public const double Least = 0.35;

    /// <summary>Its alpha at full, and how deep its flicker dips below that.</summary>
    public const double Alpha = 0.85, FlickerDepth = 0.18;

    /// <summary>Its pale cyan, as hex; the hand's picture is reduced to its brightness and tinted so.</summary>
    public const string Tint = "a8eeff";

    /// <summary>The gesture <paramref name="motion"/> plays, as the figure is given it; none for a motion no gesture has.</summary>
    public static Gesture? KindOf(GestureMotion motion)
    {
        foreach (var gesture in Enum.GetValues<Gesture>())
            if (ReferenceEquals(GestureMotions.Of(gesture), motion)) return gesture;
        return null;
    }

    /// <summary>
    /// The sign <paramref name="ms"/> into <paramref name="motion"/>, at <paramref name="timeMs"/> on the client's clock: how far it
    /// has risen out of the lens (0 in it, 1 risen), its size as a share of full, and its alpha, none before and after the gesture.
    /// </summary>
    public static (double Risen, double Scale, double Alpha) At(GestureMotion motion, double ms, double timeMs)
    {
        double w = Gestures.WeightAt(motion, ms);
        return (w, Least + (1 - Least) * w, Alpha * w * Flicker(timeMs));
    }

    /// <summary>A tube's unsteady light: whole at most, never below <see cref="FlickerDepth"/> under it.</summary>
    public static double Flicker(double timeMs) => 1 - FlickerDepth * (0.5 + 0.5 * Math.Sin(timeMs * 0.031) * Math.Sin(timeMs * 0.0047));

    /// <summary>Where its middle is, from the drone's middle: on the way from <paramref name="lens"/> to <see cref="Above"/> it.</summary>
    public static (double X, double Y) Place(double risen, (double X, double Y) lens) =>
        (lens.X + (0 - lens.X) * risen, lens.Y + (-Above - lens.Y) * risen);

    /// <summary>
    /// How the sign's whole picture is turned, clockwise, and mirrored first, over the wheel's own turn of the hand: a point's
    /// hanging finger turned to <paramref name="toward"/>, mirrored pointing left so it is right's mirror image; the others as the wheel shows them.
    /// </summary>
    public static (double Turn, bool Mirrored) Turned(Gesture gesture, (double X, double Y) toward) =>
        gesture == Gesture.Point && (toward.X != 0 || toward.Y != 0) ? (Math.Atan2(toward.Y, toward.X) - Math.PI / 2, toward.X < 0) : (0, false);
}
