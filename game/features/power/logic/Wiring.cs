using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim.Power;

namespace Stillwake.Game.Features.Power;

/// <summary>How a node lights while a cable is dragged, by the sim's answer for it.</summary>
public enum Light { Fits, Far, Refused }

/// <summary>A cable as drawn: its nodes and its points from node <c>A</c> to node <c>B</c>, in cells.</summary>
public sealed record Line(int A, int B, IReadOnlyList<(double X, double Y)> Points);

/// <summary>What a press with the cable in hand takes hold of.</summary>
public abstract record Grip(int Node);

/// <summary>A cable's end at a machine: that cable alone moves from the machine.</summary>
public sealed record EndGrip(int Node, Ends Cable) : Grip(Node);

/// <summary>A support, an anchor cables end on: every cable there moves with it.</summary>
public sealed record SupportGrip(int Node) : Grip(Node);

/// <summary>A node a new cable is laid from.</summary>
public sealed record LayGrip(int Node) : Grip(Node);

/// <summary>What the hands take hold of on cables (features/power/wiring.ts).</summary>
public static class Wiring
{
    /// <summary>How near a press must be to a cable's end at a machine, in cells, to take that end.</summary>
    private const double EndReach = 1;

    /// <summary>A node a cable fits, one too far for a cable to reach, or one refused for another reason.</summary>
    public static Light LightOf(string? problem) => problem switch
    {
        null => Light.Fits,
        "power.tooLong" or "power.taut" => Light.Far,
        _ => Light.Refused,
    };

    /// <summary>
    /// What a press at <paramref name="at"/> takes hold of: a cable's end at a machine near it, else the
    /// node under it, a support if cables end there and it is no machine, else a node to lay from;
    /// nothing elsewhere.
    /// </summary>
    public static Grip? GripAt((double X, double Y) at, IReadOnlyList<Line> lines, int? node, Func<int, bool> isMachine)
    {
        (Grip Grip, double D)? best = null;
        foreach (var line in lines)
        {
            if (line.Points.Count == 0) continue;
            foreach (var (end, point) in new[] { (line.A, line.Points[0]), (line.B, line.Points[^1]) })
            {
                if (!isMachine(end)) continue;
                double d = Math.Sqrt((point.X - at.X) * (point.X - at.X) + (point.Y - at.Y) * (point.Y - at.Y));
                if (d > EndReach || (best is { } b && b.D <= d)) continue;
                best = (new EndGrip(end, new Ends(line.A, line.B)), d);
            }
        }
        if (best is { } found) return found.Grip;
        if (node is not int n) return null;
        bool cabled = lines.Any(l => l.A == n || l.B == n);
        return cabled && !isMachine(n) ? new SupportGrip(n) : new LayGrip(n);
    }
}
