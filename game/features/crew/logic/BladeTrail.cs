using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The pale trail a combo's blade leaves on the fast part of each cut (axe-combos): its edge every <see cref="Combos.TrailStepMs"/>
/// back over <see cref="Combos.TrailMs"/> within the play, newest first (<see cref="Edges"/>), and how opaque the band between each two
/// samples is (<see cref="Opacity"/>): <see cref="Combos.TrailMost"/> at the blade fading to none at the tail, only within the cut,
/// fainter where the edge moves slower than <see cref="Combos.TrailFast"/> pixels a ms.
/// </summary>
public sealed record BladeTrail(IReadOnlyList<(Vec From, Vec To)> Edges, IReadOnlyList<double> Opacity)
{
    /// <summary>Whether any of it shows.</summary>
    public bool Live
    {
        get
        {
            foreach (double o in Opacity)
                if (o > 0) return true;
            return false;
        }
    }

    /// <summary>
    /// The trail <paramref name="nowMs"/> into a play: <paramref name="edgeAt"/> gives the edge's ends at a moment of it, and
    /// <paramref name="cutting"/> whether the blade cuts then.
    /// </summary>
    public static BladeTrail Of(Func<double, (Vec From, Vec To)> edgeAt, Func<double, bool> cutting, double nowMs)
    {
        var edges = new List<(Vec, Vec)>();
        for (double age = 0; age <= Combos.TrailMs && age <= nowMs; age += Combos.TrailStepMs) edges.Add(edgeAt(nowMs - age));
        var opacity = new double[Math.Max(0, edges.Count - 1)];
        for (int i = 0; i < opacity.Length; i++)
        {
            double age = (i + 0.5) * Combos.TrailStepMs;
            var (a, b) = (Middle(edges[i]), Middle(edges[i + 1]));
            double speed = Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y)) / Combos.TrailStepMs;
            opacity[i] = cutting(nowMs - age) ? Combos.TrailMost * (1 - age / Combos.TrailMs) * Math.Min(1, speed / Combos.TrailFast) : 0;
        }
        return new(edges, opacity);
    }

    private static Vec Middle((Vec From, Vec To) edge) => new((edge.From.X + edge.To.X) / 2, (edge.From.Y + edge.To.Y) / 2);
}
