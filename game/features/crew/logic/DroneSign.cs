using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A gesture shown by the light drone (light-drone, drone-threads): its sparks draw the gesture's fan over it on threads, so friends
/// far off read it; a point's fan turns to where it points, the others stand up. Client only, from the gesture already playing on the
/// figure.
/// </summary>
public static class DroneSign
{
    /// <summary>The gesture <paramref name="motion"/> plays, as the figure is given it; none for a motion no gesture has.</summary>
    public static Gesture? KindOf(GestureMotion motion)
    {
        foreach (var gesture in Enum.GetValues<Gesture>())
            if (ReferenceEquals(GestureMotions.Of(gesture), motion)) return gesture;
        return null;
    }

    /// <summary>
    /// Where <paramref name="gesture"/>'s fan heads, radians, a point's to <paramref name="toward"/> where it points (from the drone);
    /// none for the others, which stand up swaying, nor for a point at the drone itself.
    /// </summary>
    public static float? Turned(Gesture gesture, (double X, double Y) toward) =>
        gesture == Gesture.Point && (toward.X != 0 || toward.Y != 0) ? (float)Math.Atan2(toward.Y, toward.X) : null;
}
