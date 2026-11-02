using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The Istukan in ink (2026-11-01-guards-in-ink): where each of its parts goes in an <see cref="IstukanArt.Pose"/>, the guard page's pose ported. Each node's key
/// poses mixed in the game's order, legs solved between hip and foot, a slow sway awake and a shiver stuck; its seams, core and lamp shining as the pose says. Plain C#, tested.
/// </summary>
public sealed class IstukanRig
{
    private static readonly CreatureArt.IstukanDesign D = CreatureArt.InkIstukan;

    private readonly (double A, double X, double Y)[] _joints = new (double, double, double)[D.Nodes.Length], _placed = new (double, double, double)[D.Nodes.Length];
    private readonly bool[] _done = new bool[D.Nodes.Length];
    private readonly int _lamp = Array.FindIndex(D.Nodes, n => n.Kind == CreatureArt.IstukanNode.Kinds.Lamp), _core = Array.FindIndex(D.Nodes, n => n.Kind == CreatureArt.IstukanNode.Kinds.Core);

    /// <summary>Its pictures this frame, in draw order: a node's plate, then the light of its seam; its lamp two.</summary>
    public InkSlot[] Slots { get; } = new InkSlot[D.Nodes.Length + 1 + Array.FindAll(D.Nodes, n => n.Seam != null).Length];

    /// <summary>Places its parts in <paramref name="p"/>, <paramref name="clock"/> seconds into its sway; answers where its lamp and core are, in its own pixels.</summary>
    public IstukanArt.Anchors Place(IstukanArt.Pose p, double clock)
    {
        Mix(p, clock);
        Array.Clear(_done);
        for (int i = 0; i < D.Nodes.Length; i++) Placed(i);
        double seam = Math.Clamp(p.Seam, 0, 1);
        int s = 0;
        for (int i = 0; i < D.Nodes.Length; i++)
        {
            var node = D.Nodes[i];
            var (a, x, y) = _placed[i];
            switch (node.Kind)
            {
                case CreatureArt.IstukanNode.Kinds.Lamp:
                    var lit = p.Lamp ?? TickArt.Black;
                    double amp = p.Lamp == null ? 0 : Math.Clamp(p.LampAmp, 0, 1);
                    Slots[s++] = new InkSlot("lamp", true, a, x, y, amp, TickArt.Rgb.Mix(lit, TickArt.Black, 0.15));
                    Slots[s++] = new InkSlot("lamp_heart", true, a, x, y, amp, TickArt.Rgb.Mix(lit, TickArt.White, 0.55));
                    break;
                case CreatureArt.IstukanNode.Kinds.Core:
                    Slots[s++] = new InkSlot("core", true, a, x, y, seam, TickArt.White);
                    break;
                default:
                    Slots[s++] = new InkSlot(node.Part, false, a, x, y);
                    if (node.Seam is { } part) Slots[s++] = new InkSlot(part, true, a, x, y, seam, TickArt.White);
                    break;
            }
        }
        return new IstukanArt.Anchors((_placed[_lamp].X, _placed[_lamp].Y), (_placed[_core].X, _placed[_core].Y));
    }

    /// <summary>Each node's place and turn, its keys mixed each over the last by how far it has gone; shivering stuck, swaying when still.</summary>
    private void Mix(IstukanArt.Pose p, double clock)
    {
        double still = (1 - p.Fold) * (1 - p.Fly) * (1 - p.Stuck) * (1 - p.Dead), ms = clock * 1000;
        ReadOnlySpan<double> stages = [p.Fold, p.Crouch, p.Fly, p.Stuck, p.Dead];
        for (int i = 0; i < D.Nodes.Length; i++)
        {
            var node = D.Nodes[i];
            double x = node.Keys[0], y = node.Keys[1], a = node.Keys[2];
            for (int k = 0; k < stages.Length; k++)
            {
                double t = stages[k];
                if (t <= 0) continue;
                int o = (k + 1) * 3;
                (x, y, a) = (x + (node.Keys[o] - x) * t, y + (node.Keys[o + 1] - y) * t, a + (node.Keys[o + 2] - a) * t);
            }
            a += node.Shiver * p.Stuck * (1 - p.Dead) * Math.Sin(ms * 0.085 + i * 1.7) + node.Drift * still * Math.Sin(ms / 760 + i * 1.3);
            _joints[i] = (a, x, y);
        }
    }

    /// <summary>Node <paramref name="i"/> in its own pixels: on its parent, or a leg's bone solved from the body to its foot.</summary>
    private (double A, double X, double Y) Placed(int i)
    {
        if (_done[i]) return _placed[i];
        _done[i] = true;
        var node = D.Nodes[i];
        if (node.Ik is { } ik) return _placed[i] = Upper(ik);
        if (node.Kind == CreatureArt.IstukanNode.Kinds.Lower)
        {
            var up = Placed(node.Upper);
            var leg = D.Nodes[node.Upper].Ik!;
            return _placed[i] = (Lower(leg, up), up.X - Math.Sin(up.A) * leg.L1, up.Y + Math.Cos(up.A) * leg.L1);
        }
        var (a, x, y) = _joints[i];
        if (node.Parent < 0) return _placed[i] = (a, x, y);
        var (pa, px, py) = Placed(node.Parent);
        return _placed[i] = (pa + a, px + x * Math.Cos(pa) - y * Math.Sin(pa), py + x * Math.Sin(pa) + y * Math.Cos(pa));
    }

    /// <summary>The ankle a leg's bones reach for, on its foot's node.</summary>
    private (double X, double Y) Ankle(CreatureArt.IstukanIk ik)
    {
        var (a, x, y) = _joints[ik.Foot];
        return (x + ik.AnkleX * Math.Cos(a) - ik.AnkleY * Math.Sin(a), y + ik.AnkleX * Math.Sin(a) + ik.AnkleY * Math.Cos(a));
    }

    /// <summary>A leg's upper bone: at its hip, hanging along +y, turned so its knee comes forward.</summary>
    private (double A, double X, double Y) Upper(CreatureArt.IstukanIk ik)
    {
        var (a, x, y) = _joints[D.Body];
        double hx = x + ik.HipX * Math.Cos(a) - ik.HipY * Math.Sin(a), hy = y + ik.HipX * Math.Sin(a) + ik.HipY * Math.Cos(a);
        var (ax, ay) = Ankle(ik);
        double dx = ax - hx, dy = ay - hy, reach = Math.Clamp(Math.Sqrt(dx * dx + dy * dy), Math.Abs(ik.L1 - ik.L2) + 0.01, ik.L1 + ik.L2 - 0.01);
        double bend = Math.Acos(Math.Clamp((ik.L1 * ik.L1 + reach * reach - ik.L2 * ik.L2) / (2 * ik.L1 * reach), -1, 1));
        return (Math.Atan2(-dx, dy) - bend * ik.Bend, hx, hy);
    }

    /// <summary>A leg's lower bone's turn, from its knee to its ankle.</summary>
    private double Lower(CreatureArt.IstukanIk ik, (double A, double X, double Y) up)
    {
        var (ax, ay) = Ankle(ik);
        return Math.Atan2(-(ax - (up.X - Math.Sin(up.A) * ik.L1)), ay - (up.Y + Math.Cos(up.A) * ik.L1));
    }
}
