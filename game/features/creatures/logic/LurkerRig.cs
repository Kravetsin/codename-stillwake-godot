using System;
using System.Collections.Generic;
using static Stillwake.Game.CreatureArt;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// A cave lurker in ink (2026-11-07-cave-hostiles): where each of its parts goes in a <see cref="LurkerArt.Pose"/>, the pick page's pose
/// ported. Its plates in draw order, then what shines over them (a teal mask two pictures, teal fading to violet as it flares); its
/// lights where its nodes put them. Plain C#, tested.
/// </summary>
public sealed class LurkerRig
{
    private readonly LurkerDesign _d;
    private readonly (double A, double X, double Y, double Alpha)[] _placed;
    private readonly bool[] _done;
    private readonly int _shell;
    private readonly List<LurkerArt.Light> _lights = [];

    public LurkerRig(LurkerDesign design)
    {
        _d = design;
        _placed = new (double, double, double, double)[design.Nodes.Length];
        _done = new bool[design.Nodes.Length];
        _shell = Array.FindIndex(design.Nodes, n => n.Drive == LurkerDrive.Shell);
        int slots = 0;
        foreach (var node in design.Nodes)
        {
            if (node.Part != null) slots++;
            foreach (var (_, shine) in node.Lit ?? []) slots += shine == LurkerShine.Teal ? 2 : 1;
        }
        Slots = new InkSlot[slots];
    }

    /// <summary>Its pictures this frame: its plates in draw order, then their light masks.</summary>
    public InkSlot[] Slots { get; }

    /// <summary>The lights it gives this frame, in its own pixels.</summary>
    public IReadOnlyList<LurkerArt.Light> Lights => _lights;

    /// <summary>Places its parts and lights in <paramref name="p"/>.</summary>
    public void Place(LurkerArt.Pose p)
    {
        Array.Clear(_done);
        for (int i = 0; i < _d.Nodes.Length; i++) Placed(i, p);
        int s = 0;
        for (int i = 0; i < _d.Nodes.Length; i++)
            if (_d.Nodes[i].Part is { } part)
            {
                var (a, x, y, alpha) = _placed[i];
                Slots[s++] = new InkSlot(part, false, a, x, y, alpha);
            }
        for (int i = 0; i < _d.Nodes.Length; i++)
        {
            var (a, x, y, alpha) = _placed[i];
            foreach (var (part, shine) in _d.Nodes[i].Lit ?? [])
            {
                double k = Math.Clamp(Shine(shine, p), 0, 1) * alpha;
                if (shine == LurkerShine.Teal)
                {
                    Slots[s++] = new InkSlot(part, true, a, x, y, k, LurkerArt.Teal);
                    Slots[s++] = new InkSlot(part, true, a, x, y, Math.Clamp(p.Glow * p.Flare * 1.15, 0, 1) * alpha, LurkerArt.Violet);
                }
                else Slots[s++] = new InkSlot(part, true, a, x, y, k, LurkerArt.Of(shine));
            }
        }
        Glows(p);
    }

    /// <summary>How strongly a mask shines: the page's <c>shineOf</c>; a teal one's violet half is its own slot.</summary>
    private static double Shine(LurkerShine shine, LurkerArt.Pose p) => shine switch
    {
        LurkerShine.Teal => p.Glow * (1 - p.Flare),
        LurkerShine.Violet => p.Glow * (0.7 + 0.5 * p.Flare),
        LurkerShine.Amber => p.Glow * (1 - 0.88 * p.Lid),
        LurkerShine.Vein => p.Glow * (0.75 + 0.25 * (1 - p.Lid)),
        LurkerShine.Crack => p.Crack,
        _ => p.EyeLit,
    };

    /// <summary>The page's <c>lightsOf</c>: a teal light turns violet and grows as it flares, a mouth's dims as its plug shuts, a cracked shell glows.</summary>
    private void Glows(LurkerArt.Pose p)
    {
        _lights.Clear();
        foreach (var l in _d.Lights)
        {
            var (a, x, y, _) = _placed[l.Node];
            double k = l.K * p.Glow;
            var colour = LurkerArt.Of(l.Colour);
            if (l.Colour == LurkerShine.Teal) (colour, k) = (TickArt.Rgb.Mix(LurkerArt.Teal, LurkerArt.Violet, p.Flare), k * (1 + 0.4 * p.Flare));
            if (l.Colour == LurkerShine.Violet) k *= 0.7 + 0.6 * p.Flare;
            if (l.Mouth) k *= 1 - 0.85 * p.Lid;
            _lights.Add(new(x + l.X * Math.Cos(a) - l.Y * Math.Sin(a), y + l.X * Math.Sin(a) + l.Y * Math.Cos(a), l.R * (1 + 0.25 * p.Flare), colour, k));
        }
        if (p.Crack > 0 && _shell >= 0) _lights.Add(new(_placed[_shell].X, _placed[_shell].Y, 22, LurkerArt.Crack, 0.7 * p.Crack));
    }

    /// <summary>Node <paramref name="i"/> in its own pixels and how much of it shows: the page's <c>pose</c>, each node on its parent.</summary>
    private (double A, double X, double Y, double Alpha) Placed(int i, LurkerArt.Pose p)
    {
        if (_done[i]) return _placed[i];
        _done[i] = true;
        var n = _d.Nodes[i];
        var k = _d.Pose;
        if (n.Parent < 0)
        {
            var (cx, cy) = _d.Centre;
            double dy = _d.Snail ? k.Drop * p.Retract : k.Drop * p.Hunk - k.Rise * p.Crouch + k.Drop * 1.1 * p.Sag + 1.3 * p.Squash;
            double ra = _d.Snail ? 0 : -k.Pitch * p.Crouch - 0.12 * p.Fly + p.Lean;
            // Turned about its centre: T(0, dy) · T(C) · R(a) · T(−C).
            return _placed[i] = (ra, cx - (cx * Math.Cos(ra) - cy * Math.Sin(ra)), dy + cy - (cx * Math.Sin(ra) + cy * Math.Cos(ra)), 1);
        }
        var (pa, px, py, alpha) = Placed(n.Parent, p);
        double dx = 0, ddy = 0, rot = n.Base;
        int side = n.X > 0 ? -1 : 1;
        switch (n.Drive)
        {
            case LurkerDrive.Leg:
                ddy = -(k.Drop * p.Hunk + k.Drop * 1.1 * p.Sag + 1.3 * p.Squash) + k.Rise * p.Crouch - 2.2 * p.Fly;
                rot += p.Fly * k.LegFly + p.Sag * k.LegSplay * side + p.Crouch * 0.1 * side + p.Hunk * k.LegFold * side
                    + Math.Sin(Math.Tau * (p.Gait + n.Phase)) * 0.35 * p.Walk;
                break;
            case LurkerDrive.Head:
                (dx, ddy) = (-k.TuckX * p.Hunk + 0.8 * p.Crouch, k.TuckY * p.Hunk + 1.6 * p.Sag);
                rot = k.TuckRot * p.Hunk - k.HeadUp * p.Alert * (1 - p.Hunk) + k.HeadCrouch * p.Crouch + 0.35 * p.Sag + 0.3 * p.Flinch;
                break;
            case LurkerDrive.Caps:
                rot = p.Shudder * 0.16 * Math.Sin(p.Clock / 26 + n.X) + p.Sag * k.SagCaps;
                ddy = 0.6 * p.Sag;
                break;
            case LurkerDrive.Tail:
                dx = k.TailIn * p.Retract;
                alpha *= 1 - Smooth(0.55, 1, p.Retract);
                break;
            case LurkerDrive.Front:
                dx = -k.FrontIn * p.Retract;
                rot = Math.Sin(p.Clock / 900) * 0.03 * (1 - p.Retract);
                alpha *= 1 - Smooth(0.55, 1, p.Retract);
                break;
            case LurkerDrive.Stalk:
                rot += k.StalkBack * p.Stalk + Math.Sin(p.Clock / (560 + n.X * 9)) * 0.1 * (1 - p.Stalk);
                alpha *= 1 - Smooth(0.45, 0.85, p.Stalk);
                break;
            case LurkerDrive.Shell:
                rot = p.Spin + p.Rock;
                break;
            case LurkerDrive.Plug:
                (dx, ddy) = (k.PlugOut.X * (1 - p.Lid), k.PlugOut.Y * (1 - p.Lid));
                alpha *= Smooth(0, 0.6, p.Lid);
                break;
        }
        double lx = n.X + dx, ly = n.Y + ddy;
        return _placed[i] = (pa + rot, px + lx * Math.Cos(pa) - ly * Math.Sin(pa), py + lx * Math.Sin(pa) + ly * Math.Cos(pa), alpha);
    }

    /// <summary>The page's <c>sm</c>: smoothstep from <paramref name="a"/> to <paramref name="b"/>, clamped.</summary>
    private static double Smooth(double a, double b, double t) => TickMotion.Ease((t - a) / (b - a));
}
