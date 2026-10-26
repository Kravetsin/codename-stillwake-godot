using System;
using System.Collections.Generic;
using System.Linq;
using Run = Stillwake.Game.CreatureLooks.Run;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// What each node of a creature's rig does this frame (0102), the loper page's pose ported (its <c>pose()</c>, the user's pick)
/// and carried on into a gallop: plain C#, tested. Every motion is blended by how much it walks, gallops, flies, grazes or
/// notices a hero, never switched, so nothing pops. Angles in radians clockwise, offsets in the world's pixels, in the body's
/// own way (facing right).
/// </summary>
public sealed class CreaturePose
{
    /// <summary>One node's turn and shift this frame.</summary>
    public readonly record struct Joint(double A, double X, double Y);

    /// <summary>The eye drawn: open, half shut grazing, shut blinking, or wide startled.</summary>
    public enum EyeState { Open, Half, Blink, Wide }

    /// <summary>
    /// What the creature is about this frame: its strides (<see cref="CreatureGait.Phase"/>) and how much it walks, gallops, is
    /// off the ground and flees; how much it grazes and notices a hero; the clock; its ears' and eye's state
    /// (<see cref="CreatureMotion"/>); and where it looks, a unit vector in its own way, or none.
    /// </summary>
    public readonly record struct Doing(
        double Phase, double Walking, double Gallop, double Air, double Flee, double Graze, double Notice, double ClockMs,
        double Ears, double Eye, (double X, double Y)? Look);

    private readonly CreatureArt.Design _design;
    private readonly Run _run;
    private readonly bool[] _fore;

    /// <summary>Each node's this frame, in the rig's order.</summary>
    public Joint[] Joints { get; }

    public EyeState Eye { get; private set; }

    /// <summary>Where its pupil looks, a unit vector in its own way.</summary>
    public (double X, double Y) Look { get; private set; } = (0.98, 0.18);

    /// <summary>Its stretch along the way it runs, a share of its length: out as its legs reach, gathered as they meet.</summary>
    public double Stretch { get; private set; }

    public CreaturePose(CreatureArt.Design design, Run run)
    {
        (_design, _run) = (design, run);
        Joints = new Joint[design.Rig.Count];
        // A leg ahead of the legs' middle is a foreleg: in a gallop the forelegs reach together, then the hind ones.
        var legs = design.Rig.Where(n => n.Drive == CreatureArt.Drive.Leg).ToList();
        double middle = legs.Count > 0 ? legs.Average(n => n.X) : 0;
        _fore = design.Rig.Select(n => n.Drive == CreatureArt.Drive.Leg && n.X > middle).ToArray();
    }

    /// <summary>Poses every node for <paramref name="s"/>.</summary>
    public void Set(in Doing s)
    {
        var p = _design.Pose;
        double tau = Math.Tau, ph = s.Phase, clk = s.ClockMs, walk = s.Walking * (1 - s.Air), g = s.Gallop * (1 - s.Air);
        double ears = s.Ears, startled = Math.Clamp((s.Eye - 1) / 0.45, 0, 1);
        // Grazing: the head down, nibbling, up for a look every few seconds (the page's).
        double graze = s.Graze * Math.Clamp(1 - Math.Max(0, Math.Sin(clk / 1400) - 0.75) * 4, 0, 1);
        double nib = graze * Math.Max(0, Math.Sin(clk / 70)) * 0.07;
        double bob = 0, roll = 0, rock = 0, bound = 0;
        Stretch = 0;
        switch (_design.Gait)
        {
            case CreatureArt.Gait.Walk:
            case CreatureArt.Gait.Tripod:
                bob = -Math.Abs(Math.Cos(ph * tau)) * 0.7 * walk * (1 - g);
                // A gallop: gathered, legs under it, at a quarter stride; pushed off into the air, stretched out, at three quarters,
                // nose up rising and down landing. Four legs bound once a stride, six at each tripod's push.
                double reach = ph - 0.25;
                bound = -_run.Bound * g * Math.Abs(Math.Sin(reach * Math.PI * (_design.Gait == CreatureArt.Gait.Tripod ? 2 : 1)));
                rock = -_run.Rock * g * Math.Sin(reach * tau);
                Stretch = -_run.Stretch * g * Math.Sin(ph * tau);
                break;
            case CreatureArt.Gait.Waddle:
                bob = -Math.Abs(Math.Sin(ph * tau)) * 1.6 * walk * (1 - g);
                roll = Math.Sin(ph * tau) * 0.09 * walk * (1 - g);
                // Fleeing it bounds along in hops, one a stride: squashed on the ground, tall aloft, nose up rising and down falling.
                bound = -_run.Bound * g * Math.Abs(Math.Sin(ph * Math.PI));
                rock = -_run.Rock * g * Math.Sin(ph * tau);
                Stretch = _run.Stretch * g * Math.Cos(ph * tau);
                break;
            case CreatureArt.Gait.Hop:
                bob = -Math.Abs(Math.Sin(ph * Math.PI)) * p.Hop * (1 + 0.2 * g) * walk;
                break;
        }
        double bodyA = (_design.Headless ? p.Graze * graze + nib * 0.5 : 0) + roll + rock;
        var rig = _design.Rig;
        for (int i = 0; i < rig.Count; i++)
        {
            var n = rig[i];
            double a = 0, x = 0, y = 0;
            switch (n.Drive)
            {
                case CreatureArt.Drive.Body:
                    (a, y) = (bodyA, bob + bound);
                    break;
                case CreatureArt.Drive.Head:
                    a = p.Graze * graze + nib + s.Notice * (-0.16 + (s.Look is { } l ? l.Y * 0.25 : 0)) + s.Flee * 0.22 - startled * 0.08 - rock * 0.5;
                    break;
                case CreatureArt.Drive.Ear:
                    double flick = Math.Max(0, Math.Sin(clk / 330 + n.X) - 0.96) * 6;
                    a = n.Base + p.EarUp + (p.EarBack - p.EarUp) * ears - s.Notice * p.EarPerk - flick * 0.3
                        + s.Flee * Math.Sin(clk / 90) * 0.05 + graze * 0.15;
                    break;
                case CreatureArt.Drive.Tail:
                    a = Math.Sin(clk / 650) * p.TailAmp + s.Flee * p.TailFlee + startled * p.TailFlee * 0.6;
                    break;
                case CreatureArt.Drive.Pod:
                    a = Math.Sin(clk / 380) * 0.12 + nib * 2;
                    break;
                case CreatureArt.Drive.Antler:
                    a = Math.Sin(clk / 900) * 0.03;
                    break;
                case CreatureArt.Drive.Leg:
                    (a, x, y) = Leg(n, _fore[i], ph, walk, g);
                    // Tucked in the air: hind legs back, forelegs forward.
                    double tuckA = n.Hind ? -0.6 : n.X > 0 ? 0.55 : -0.45;
                    (a, y) = (a + (tuckA - a) * s.Air, y + (-1 - y) * s.Air);
                    a -= bodyA;
                    break;
            }
            Joints[i] = new Joint(a, x, y);
        }
        Eye = s.Eye < 0.3 ? EyeState.Blink : s.Eye > 1.2 ? EyeState.Wide : graze > 0.5 ? EyeState.Half : EyeState.Open;
        Look = s.Look ?? (graze > 0.5 ? (0.55, 0.83) : (0.98, 0.18));
    }

    /// <summary>A leg's turn and shift on the ground: the page's walk, its swing and lift growing into a gallop's.</summary>
    private (double A, double X, double Y) Leg(CreatureArt.Node n, bool fore, double ph, double walk, double g)
    {
        var p = _design.Pose;
        double tau = Math.Tau;
        switch (_design.Gait)
        {
            case CreatureArt.Gait.Hop:
                {
                    double k = Math.Sin(ph * Math.PI) * walk;
                    return n.Hind ? (-0.75 * k, 0, 0) : (0.5 * k, 0, -k * 1.5);
                }
            case CreatureArt.Gait.Waddle:
                {
                    // Paws shuffle at a waddle; bounding, they tuck up under the body while it is aloft.
                    double w = (ph + n.Phase) * tau, hop = Math.Abs(Math.Sin(ph * Math.PI)) * g;
                    return (0, Math.Sin(w) * 1.2 * walk * (1 - g), -Math.Max(0, Math.Cos(w)) * p.Lift * walk * (1 - g) - hop * 1.5);
                }
            default:
                {
                    // Walking in diagonal pairs (a tripod in turn for six); galloping, the forelegs together, the hind half a stride on,
                    // the far one of each pair a little behind its near one.
                    double far = n.Part.EndsWith("Far", StringComparison.Ordinal) ? 0.06 : 0;
                    double gallop = _design.Gait == CreatureArt.Gait.Walk ? (fore ? 0 : 0.5) + far : n.Phase;
                    double w = (ph + n.Phase + (gallop - n.Phase) * g) * tau;
                    double amp = p.LegAmp + _run.Reach * g;
                    return (Math.Sin(w) * amp * walk, 0, -Math.Max(0, -Math.Cos(w)) * p.Lift * (1 + 0.6 * g) * walk);
                }
        }
    }

    /// <summary>The rig's nodes by id, each's index: for its parent.</summary>
    public static IReadOnlyDictionary<string, int> Indexed(CreatureArt.Design design)
    {
        var all = new Dictionary<string, int>();
        for (int i = 0; i < design.Rig.Count; i++) all[design.Rig[i].Id] = i;
        return all;
    }
}
