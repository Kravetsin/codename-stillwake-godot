using System;
using System.Collections.Generic;
using static Stillwake.Game.GestureMotions;

namespace Stillwake.Game.Dev;

/// <summary>
/// The gestures' candidate motions for the user's pick (emotes), each beside the one played now: drawn by <see cref="GestureSheet"/>.
/// The picked ones go into <see cref="GestureMotions"/>; this list goes once they have.
/// </summary>
public static class GestureCandidates
{
    public sealed record Candidate(string Id, Gesture Gesture, string Name, GestureMotion Motion);

    private static double Wave(double times, double s) => Math.Sin(2 * Math.PI * times * s);
    /// <summary>From 0 out to 1 and back <paramref name="times"/> times, eased.</summary>
    private static double Beat(double times, double s) => 0.5 - 0.5 * Math.Cos(2 * Math.PI * times * s);
    /// <summary>Once out and back over the whole gesture, eased.</summary>
    private static double Bump(double s) => Math.Pow(Math.Sin(Math.PI * s), 2);

    /// <summary>A straight arm along the line to the target: the elbow all but locked.</summary>
    private const double Straight = 0.06, AlongLine = -0.03;

    public static readonly IReadOnlyList<Candidate> All =
    [
        new("point-now", Gesture.Point, "Сейчас", Of(Gesture.Point)),
        new("point-straight", Gesture.Point, "Прямая рука", new(1200, PointFrame, [new(0, AlongLine, Straight)], Aimed: true)),
        new("point-jab", Gesture.Point, "С тычком: «вон там»", new(1200, PointFrame,
            [new(0, AlongLine, Straight), new(0.16, -0.3, 0.75), new(0.3, AlongLine, Straight, Ease: Ease.Out), new(1, AlongLine, Straight)], Aimed: true)),
        new("point-twice", Gesture.Point, "Два тычка: «там, там»", new(1300, PointFrame,
            [new(0, AlongLine, Straight), new(0.12, -0.3, 0.75), new(0.24, AlongLine, Straight, Ease: Ease.Out), new(0.38, -0.3, 0.75),
             new(0.5, AlongLine, Straight, Ease: Ease.Out), new(1, AlongLine, Straight)], Aimed: true)),

        new("wave-now", Gesture.Wave, "Сейчас", Of(Gesture.Wave)),
        new("wave-elbow", Gesture.Wave, "Локоть вперёд, предплечье машет перед шлемом", new(1600, OpenFrame,
            Sampled(s => new(s, 1.55, 1.45 + 0.35 * Wave(3, s), Wrist: 0.25 * Wave(3, s))))),
        new("wave-wrist", Gesture.Wave, "Локоть вперёд, машет кистью", new(1600, OpenFrame,
            Sampled(s => new(s, 1.6, 1.4 + 0.08 * Wave(3, s), Wrist: 0.6 * Wave(3, s))))),
        new("wave-overhead", Gesture.Wave, "Прямая рука над головой", new(1600, OpenFrame, Sampled(s => new(s, 2.9 + 0.35 * Wave(2, s), 0.12)))),
        new("wave-ahead", Gesture.Wave, "Рука впереди, качает предплечьем", new(1600, OpenFrame, Sampled(s => new(s, 2.25, 0.55 + 0.4 * Wave(3, s))))),

        new("come-now", Gesture.Come, "Сейчас", Of(Gesture.Come)),
        new("come-palm", Gesture.Come, "Ладонью вверх, кисть к себе", new(1400, OpenFrame,
            Sampled(s => new(s, 0.9, 0.75, Wrist: 1.1 * Beat(2, s))), Side: HandSide.Palm)),
        new("come-scoop", Gesture.Come, "Загребает к себе", new(1400, OpenFrame, Sampled(s => new(s, 1.6 - 0.6 * Beat(2, s), 0.2 + 1.7 * Beat(2, s))))),
        new("come-overhead", Gesture.Come, "Мах над головой к себе", new(1500, OpenFrame, Sampled(s => new(s, 2.0 + 0.9 * Beat(2, s), 0.3 + 0.3 * Beat(2, s))))),

        new("yes-now", Gesture.Yes, "Сейчас", Of(Gesture.Yes)),
        new("yes-nod", Gesture.Yes, "Палец вверх у груди и два кивка", new(1200, ThumbFrame,
            Sampled(s => new(s, 0.5 + 0.1 * Math.Sin(Math.PI * s), 1.25 - 0.1 * Math.Sin(Math.PI * s), Head: 0.45 * Beat(2, s))))),
        new("yes-high", Gesture.Yes, "Палец вверх перед собой и кивок", new(1100, ThumbFrame,
            Sampled(s => new(s, 1.25 + 0.2 * Bump(s), 0.5 - 0.2 * Bump(s), Head: 0.3 * Bump(s))))),

        new("no-now", Gesture.No, "Сейчас", Of(Gesture.No)),
        new("no-finger", Gesture.No, "Грозит пальцем", new(1200, PointFrame,
            Sampled(s => new(s, 1.1, 1.75 + 0.06 * Wave(4, s), Wrist: 0.5 * Wave(4, s))))),
        new("no-thumb-palm", Gesture.No, "Палец вниз, кисть ладонью", new(1100, ThumbFrame,
            Sampled(s => new(s, 0.45 - 0.25 * Bump(s), 1.0 + 0.25 * Bump(s))), Side: HandSide.Palm, Mirrored: true)),
        new("no-wipe", Gesture.No, "Отмахивается ладонью", new(1200, OpenFrame, Sampled(s => new(s, 0.55, 1.0 + 0.45 * Wave(2, s), Wrist: -0.3)))),
    ];
}
