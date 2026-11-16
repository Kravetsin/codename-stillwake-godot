using System;
using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>Which side of the near hand a gesture shows: its back, as it hangs (hero_hand_*), or its palm (the far hand's pictures).</summary>
public enum HandSide { Back, Palm }

/// <summary>
/// One key of a gesture (emotes): at <paramref name="Share"/> of it, the upper arm <paramref name="Upper"/> radians from hanging, ahead
/// positive (for an aimed gesture, from the line to its target), the elbow bent <paramref name="Elbow"/>, the hand turned
/// <paramref name="Wrist"/> against the forearm, ahead positive, and the head <paramref name="Head"/> radians down; eased in from the
/// key before by <paramref name="Ease"/>.
/// </summary>
public readonly record struct GestureKey(double Share, double Upper, double Elbow, double Wrist = 0, double Head = 0, Ease Ease = Ease.Sine);

/// <summary>
/// How a gesture moves (emotes): <paramref name="Ms"/> long, the hand's frame <paramref name="Frame"/> of its strip shown from
/// <paramref name="Side"/>, mirrored about the wrist if <paramref name="Mirrored"/>, the arm through <paramref name="Keys"/>, laid along
/// the line to its target if <paramref name="Aimed"/>. It is blended in over the pose and out again by the figure.
/// </summary>
public sealed record GestureMotion(double Ms, int Frame, IReadOnlyList<GestureKey> Keys, HandSide Side = HandSide.Back, bool Mirrored = false,
    bool Aimed = false)
{
    /// <summary>The arm, hand and head at <paramref name="share"/> of it.</summary>
    public GestureKey At(double share)
    {
        if (share <= Keys[0].Share) return Keys[0];
        for (int i = 1; i < Keys.Count; i++)
        {
            var (was, to) = (Keys[i - 1], Keys[i]);
            if (share >= to.Share) continue;
            double t = (share - was.Share) / (to.Share - was.Share);
            t = to.Ease switch
            {
                Ease.Sine => (1 - Math.Cos(t * Math.PI)) / 2,
                Ease.In => t * t,
                Ease.Out => 1 - (1 - t) * (1 - t),
                _ => t,
            };
            double Mix(double a, double b) => a + (b - a) * t;
            return new(share, Mix(was.Upper, to.Upper), Mix(was.Elbow, to.Elbow), Mix(was.Wrist, to.Wrist), Mix(was.Head, to.Head));
        }
        return Keys[^1];
    }
}

/// <summary>A sign a hero makes with its near hand in place of words (emotes).</summary>
public enum Gesture { Point, Wave, Come, Yes, No }

/// <summary>Each gesture's motion (emotes): the table the figure plays.</summary>
public static class GestureMotions
{
    /// <summary>The hand's frames in its strip (hand): 0 the grip it hangs and holds in, 1 open, 2 pointing, 3 the thumb out ahead.</summary>
    public const int GripFrame = 0, OpenFrame = 1, PointFrame = 2, ThumbFrame = 3;

    private static readonly Dictionary<Gesture, GestureMotion> Table = new()
    {
        [Gesture.Point] = new(1200, PointFrame, [new(0, -0.245, 0.495)], Aimed: true),
        [Gesture.Wave] = new(1600, OpenFrame, Sampled(s => new(s, 2.5, Math.PI - 2.5 + 0.45 * Math.Sin(2 * Math.PI * 3 * s)))),
        [Gesture.Come] = new(1400, OpenFrame, Sampled(s => new(s, 1.25, 0.25 + 1.35 * (0.5 - 0.5 * Math.Cos(2 * Math.PI * 2 * s))))),
        [Gesture.Yes] = new(1000, ThumbFrame, Sampled(s => new(s, 0.45 + 0.3 * Math.Sin(Math.PI * s), 1.25 - 0.3 * Math.Sin(Math.PI * s)))),
        [Gesture.No] = new(1000, ThumbFrame, Sampled(s => new(s, 0.45 - 0.3 * Math.Sin(Math.PI * s), 0.95 + 0.3 * Math.Sin(Math.PI * s))), Mirrored: true),
    };

    public static GestureMotion Of(Gesture gesture) => Table[gesture];

    /// <summary>Keys sampled from <paramref name="at"/> at <paramref name="steps"/> even shares, joined straight: a motion drawn as a curve.</summary>
    public static GestureKey[] Sampled(Func<double, GestureKey> at, int steps = 32)
    {
        var keys = new GestureKey[steps + 1];
        for (int i = 0; i <= steps; i++) keys[i] = at((double)i / steps) with { Share = (double)i / steps, Ease = Ease.Linear };
        return keys;
    }
}
