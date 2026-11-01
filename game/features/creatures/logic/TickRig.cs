using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// A picture of a body in ink placed this frame: its part, turned about its pivot and set in the body's own pixels (origin the middle of its
/// feet); lit as the world, or shining over it in <paramref name="Tint"/> at <paramref name="Alpha"/>.
/// </summary>
public readonly record struct InkSlot(string Part, bool Shines, double A, double X, double Y, double Alpha = 1, TickArt.Rgb Tint = default);

/// <summary>
/// The tick in ink (2026-11-01-guards-in-ink): where each of its parts goes in a <see cref="TickArt.Pose"/>, the sentry page's pose ported. Four
/// stilts and a cutter arm bent by two-bone IK, feet planted as it crawls; the body lowered by its crouch; the head on its look. Plain C#, tested.
/// </summary>
public sealed class TickRig
{
    /// <summary>The share of a stride a foot stays planted: it slides back as the body goes on, then swings ahead.</summary>
    private const double Stance = 0.72;

    private static readonly CreatureArt.TickDesign D = CreatureArt.InkTick;

    /// <summary>Its pictures this frame, in draw order.</summary>
    public InkSlot[] Slots { get; } = new InkSlot[(D.FarLegs.Length + D.NearLegs.Length + 1) * 2 + 7];

    /// <summary>Places its parts in <paramref name="p"/>; answers where its anchors are, in its own pixels.</summary>
    public TickArt.Anchors Place(TickArt.Pose p)
    {
        double turn = p.Saw * D.SawTilt, cos = Math.Cos(turn), sin = Math.Sin(turn);
        // Motion's gait counts its crawl's pixels: a planted foot goes back as fast as the body goes on.
        double strides = p.Gait / TickMotion.GaitPerPixel * Stance / D.Stride;
        double down = p.Crouch * D.Crouch - Math.Max(0, Math.Sin(strides * Math.Tau * 2)) * 0.3 * p.Walk;
        (double X, double Y) Root(double x, double y) =>
            (D.Centre.X + (x - D.Centre.X) * cos - (y - D.Centre.Y) * sin, D.Centre.Y + (x - D.Centre.X) * sin + (y - D.Centre.Y) * cos + down);

        var knees = new List<(double, double)>(5);
        var feet = new List<(double, double)>(D.NearLegs.Length);
        int n = 0;
        foreach (var leg in D.FarLegs) Leg(leg, Stepped(leg, strides, p), p, turn, Root, knees, ref n);
        var body = Root(0, 0);
        var nozzle = On(body, turn, D.NozzleAt);
        Slots[n++] = new InkSlot("nozzle", false, turn, nozzle.X, nozzle.Y);
        var claw = Leg(D.Arm, ArmAt(p, Root), p, turn, Root, knees, ref n);
        Slots[n++] = new InkSlot("body", false, turn, body.X, body.Y);
        double look = Math.Clamp(p.Look, -1, 1);
        double headTurn = turn + look * D.LookAmp + p.Alert * D.HeadUp + p.Saw * D.HeadSaw;
        var head = On(body, turn, D.HeadAt);
        var jaw = On(head, headTurn, D.JawAt);
        Slots[n++] = new InkSlot("jaw", false, headTurn + (p.Jaw + p.Alert * 0.2) * D.JawOpen, jaw.X, jaw.Y);
        Slots[n++] = new InkSlot("head", false, headTurn, head.X, head.Y);
        foreach (var leg in D.NearLegs) feet.Add(Leg(leg, Stepped(leg, strides, p), p, turn, Root, knees, ref n));

        var eye = On(head, headTurn, D.Eye);
        double lit = Math.Clamp(p.EyeAmp, 0, 1);
        Slots[n++] = new InkSlot("lamp", true, headTurn, head.X, head.Y, lit, TickArt.Rgb.Mix(p.Eye, TickArt.Black, 0.22));
        var pupil = On(eye, headTurn, (look * D.EyeTravel.X, look * D.EyeTravel.Y));
        Slots[n++] = new InkSlot("pupil", true, headTurn, pupil.X, pupil.Y, lit, TickArt.Rgb.Mix(p.Eye, TickArt.White, 0.6));
        Slots[n] = new InkSlot("cracks", true, turn, body.X, body.Y, p.Cracks, TickArt.Rgb.Mix(new TickArt.Rgb(255, 120, 40), new TickArt.Rgb(255, 240, 200), p.Cracks));

        var arcs = new List<(double, double)>(knees);
        foreach (var at in D.Arcs) arcs.Add(Root(at.X, at.Y));
        return new TickArt.Anchors(eye, On(nozzle, turn, D.NozzleEnd), feet, claw, arcs, Root(D.Centre.X, D.Centre.Y));
    }

    /// <summary>Where a stilt's foot is in its step, its rest and the walk's share of the step: a planted foot slides back, then swings ahead, off the ground only then.</summary>
    private static (double X, double Y) Stepped(CreatureArt.InkLeg leg, double strides, TickArt.Pose p)
    {
        double t = strides + leg.Phase;
        t -= Math.Floor(t);
        double s = D.Stride;
        var (x, y) = t < Stance ? (s / 2 - s * (t / Stance), 0.0)
            : (-s / 2 + s * ((t - Stance) / (1 - Stance)), -D.Lift * Math.Sin((t - Stance) / (1 - Stance) * Math.PI));
        return (leg.FootX + x * p.Walk, leg.FootY + y * p.Walk);
    }

    /// <summary>Where the cutter's blade goes: held before it, raised when alerted; sawing, at the line, up and down with its stroke.</summary>
    private static (double X, double Y) ArmAt(TickArt.Pose p, Func<double, double, (double X, double Y)> root)
    {
        var rest = root(D.Arm.FootX, D.Arm.FootY - p.Alert * 2.2);
        rest.Y = Math.Min(rest.Y, -0.6);
        if (p.Saw <= 0) return rest;
        // Motion's saw goes 0.7..1 with its stroke.
        double stroke = Math.Clamp((p.Saw - 0.7) / 0.3, 0, 1) - 0.5;
        var cut = root(D.Cut.X + stroke * 0.9, D.Cut.Y - stroke * 2.2);
        return (rest.X + (cut.X - rest.X) * p.Saw, rest.Y + (cut.Y - rest.Y) * p.Saw);
    }

    /// <summary>Two bones from <paramref name="leg"/>'s hip to <paramref name="to"/>, the lower drawn first; folded in flight. Answers its end.</summary>
    private (double X, double Y) Leg(CreatureArt.InkLeg leg, (double X, double Y) to, TickArt.Pose p, double turn, Func<double, double, (double X, double Y)> root,
        List<(double, double)> knees, ref int n)
    {
        var hip = root(leg.HipX, leg.HipY);
        double vx = to.X - hip.X, vy = to.Y - hip.Y, reach = Math.Clamp(Math.Sqrt(vx * vx + vy * vy), Math.Abs(leg.L1 - leg.L2) + 0.01, leg.L1 + leg.L2 - 0.01);
        double upper = Math.Atan2(vy, vx) + leg.Bend * Math.Acos(Math.Clamp((leg.L1 * leg.L1 + reach * reach - leg.L2 * leg.L2) / (2 * leg.L1 * reach), -1, 1));
        double kneeX = hip.X + leg.L1 * Math.Cos(upper), kneeY = hip.Y + leg.L1 * Math.Sin(upper);
        double lower = Math.Atan2(to.Y - kneeY, to.X - kneeX);
        if (p.Fly > 0)
        {
            upper = LerpAngle(upper, turn + leg.FoldUpper, p.Fly);
            lower = LerpAngle(lower, turn + leg.FoldLower, p.Fly);
            (kneeX, kneeY) = (hip.X + leg.L1 * Math.Cos(upper), hip.Y + leg.L1 * Math.Sin(upper));
        }
        Slots[n++] = new InkSlot(leg.Lower, false, lower, kneeX, kneeY);
        Slots[n++] = new InkSlot(leg.Upper, false, upper, hip.X, hip.Y);
        knees.Add((kneeX, kneeY));
        return (kneeX + leg.L2 * Math.Cos(lower), kneeY + leg.L2 * Math.Sin(lower));
    }

    private static (double X, double Y) On((double X, double Y) at, double turn, (double X, double Y) by) =>
        (at.X + by.X * Math.Cos(turn) - by.Y * Math.Sin(turn), at.Y + by.X * Math.Sin(turn) + by.Y * Math.Cos(turn));

    private static double LerpAngle(double a, double b, double t)
    {
        double q = (b - a) % Math.Tau;
        if (q > Math.PI) q -= Math.Tau;
        if (q < -Math.PI) q += Math.Tau;
        return a + q * t;
    }
}
