using System;
using System.Collections.Generic;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Menu;

/// <summary>
/// The title's hero at a moment, in the rig's pixels from its middle, seen from the front: each arm shoulder, elbow, hand; each
/// leg hip, knee, ankle, toe; the helmet's middle and how far the head is tilted.
/// </summary>
public sealed record TitleBody(Vec[][] Arms, Vec[][] Legs, Vec Helm, double Tilt);

/// <summary>
/// The title's hero (<see cref="TitleScene"/>): the rig's proportions seen from the front, spread and limp, its limbs stirring
/// slowly; its forearms float up, its knees fall apart.
/// </summary>
public static class TitleHero
{
    /// <summary>The shoulders' and hips' half spread and heights, the neck's height, the boot past the ankle, in the rig's pixels.</summary>
    public const double Shoulders = 5.2, ShoulderY = -7.6, Hips = 2.5, HipY = 4.2, NeckY = -10, Toe = 2.2;

    /// <summary>The helmet's radius and how far its middle sits above the neck.</summary>
    public const double HelmR = 6.4, HelmUp = 6.3;

    /// <summary>Where the tether is clipped on, at the belt.</summary>
    public static readonly Vec Attach = new(4.8, 4);

    private const double Seed = 1.3, Tau = 2 * Math.PI;

    /// <summary>The hero <paramref name="seconds"/> into the title.</summary>
    public static TitleBody At(double seconds)
    {
        double W(double period, double reach, double shift) => reach * Math.Sin(Tau * seconds / period + shift + Seed);
        // Angles from hanging straight down, outward positive: the upper limb, then the lower.
        var arms = new[] { (1.82 + W(15, 0.12, 0), 2.32 + W(12, 0.16, 1)), (2.02 + W(18, 0.12, 2), 2.38 + W(14, 0.15, 3)) };
        var legs = new[] { (0.34 + W(17, 0.07, 4), 0.04 + W(13, 0.1, 5)), (0.42 + W(19, 0.07, 6), 0.12 + W(11, 0.1, 7)) };
        double tilt = 0.3 + W(21, 0.12, 8);
        var armPts = new Vec[2][];
        var legPts = new Vec[2][];
        for (int i = 0; i < 2; i++)
        {
            int s = i == 0 ? -1 : 1;
            var o = new Vec(s * Shoulders, ShoulderY);
            var e = Out(o, s, arms[i].Item1, Rig.Arm.Upper);
            armPts[i] = [o, e, Out(e, s, arms[i].Item2, Rig.Arm.Lower)];
            var p = new Vec(s * Hips, HipY);
            var k = Out(p, s, legs[i].Item1, Rig.Leg.Upper);
            var f = Out(k, s, legs[i].Item2, Rig.Leg.Lower);
            legPts[i] = [p, k, f, Out(f, s, legs[i].Item2, Toe)];
        }
        return new TitleBody(armPts, legPts, new Vec(HelmUp * Math.Sin(tilt), NeckY - HelmUp * Math.Cos(tilt)), tilt);
    }

    /// <summary>The hero's outline: its joints with their limbs' half widths, the helmet, the torso; what the scene keeps clear.</summary>
    public static IEnumerable<(Vec At, double R)> Outline(TitleBody body)
    {
        foreach (var arm in body.Arms)
            foreach (var p in arm) yield return (p, 2.4);
        foreach (var leg in body.Legs)
            foreach (var p in leg) yield return (p, 2.8);
        yield return (body.Helm, HelmR);
        yield return (new Vec(0, -4), 7);
    }

    private static Vec Out(Vec from, int side, double angle, double length) =>
        new(from.X + side * Math.Sin(angle) * length, from.Y + Math.Cos(angle) * length);
}
