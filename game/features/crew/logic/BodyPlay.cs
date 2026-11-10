using System;
using System.Collections.Generic;
using Feel = Stillwake.Game.HeroMotion;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The body an answer puts it in at a moment (body-answers), each channel of <see cref="BodyAnswer"/> from rest: the torso's
/// <see cref="Lean"/>, the hip's <see cref="Dip"/> and <see cref="Push"/>, the far arm and its elbow, the far hand's <see cref="Grip"/>,
/// the aimed arm's <see cref="Kick"/>, and each foot, pixels ahead of where it stands at rest (<see cref="Front"/>, <see cref="Back"/>) and lifted.
/// </summary>
public readonly record struct BodyState(double Lean, double Dip, double Push, double FarArm, double FarElbow, double Grip, double Kick,
    double Front, double Back, double FrontLift = 0, double BackLift = 0)
{
    /// <summary>At rest: every channel at nought, the feet where they stand.</summary>
    public static readonly BodyState Rest = default;

    /// <summary>This and <paramref name="other"/> together, as two answers playing at once are.</summary>
    public BodyState Plus(BodyState other) => new(Lean + other.Lean, Dip + other.Dip, Push + other.Push, FarArm + other.FarArm,
        FarElbow + other.FarElbow, Math.Min(1, Grip + other.Grip), Kick + other.Kick, Front + other.Front, Back + other.Back,
        FrontLift + other.FrontLift, BackLift + other.BackLift);
}

/// <summary>
/// What the body layer changes of the limbs a pose gives (body-answers), in the facing's frame, as differences from them: the hip
/// moved (<see cref="HipX"/> ahead, <see cref="HipY"/> down), the torso's <see cref="Lean"/>, the far arm's turn (in the body's
/// frame, not the torso's), the aimed arm's <see cref="Kick"/>, each leg's and boot's turn, and how far the far hand holds the haft.
/// </summary>
public readonly record struct BodyLayer(double HipX, double HipY, double Lean, Bend FarArm, double Kick, Bend FrontLeg, Bend BackLeg,
    double FrontFoot, double BackFoot, double Grip)
{
    /// <summary>Nothing changed: the pose as it is.</summary>
    public static readonly BodyLayer None = default;

    /// <summary>In the world's axes for a figure facing <paramref name="facing"/>, or back: what runs along x turned with it.</summary>
    public BodyLayer Mirrored(int facing) => new(HipX * facing, HipY, Lean * facing, Turned(FarArm, facing), Kick * facing,
        Turned(FrontLeg, facing), Turned(BackLeg, facing), FrontFoot * facing, BackFoot * facing, Grip);

    /// <summary>This and <paramref name="by"/> times <paramref name="other"/>.</summary>
    public BodyLayer Plus(BodyLayer other, double by = 1) => new(HipX + other.HipX * by, HipY + other.HipY * by, Lean + other.Lean * by,
        Add(FarArm, other.FarArm, by), Kick + other.Kick * by, Add(FrontLeg, other.FrontLeg, by), Add(BackLeg, other.BackLeg, by),
        FrontFoot + other.FrontFoot * by, BackFoot + other.BackFoot * by, Grip + other.Grip * by);

    private static Bend Turned(Bend b, int facing) => new(b.Upper * facing, b.Lower * facing);
    private static Bend Add(Bend a, Bend b, double by) => new(a.Upper + b.Upper * by, a.Lower + b.Lower * by);
}

/// <summary>
/// The body answering an action (body-answers): an answer's channels at a moment of its clock, scaled; and that laid over the
/// limbs a pose gives. Ported from the axe combos' body (axe-combos), without the chain.
/// </summary>
public static class BodyPlay
{
    /// <summary><paramref name="answer"/> at <paramref name="at"/> on its clock, every channel and step times <paramref name="scale"/>.</summary>
    public static BodyState At(BodyAnswer answer, double at, double scale)
    {
        var (front, frontLift) = FootAt(answer.Feet, Foot.Front, at);
        var (back, backLift) = FootAt(answer.Feet, Foot.Back, at);
        return new(Channel(answer.Lean, at) * scale, Channel(answer.Dip, at) * scale, Channel(answer.Push, at) * scale,
            Channel(answer.FarArm, at) * scale, Channel(answer.FarElbow, at) * scale, Math.Min(1, Channel(answer.Grip, at) * scale),
            Channel(answer.Kick, at) * scale, front * scale, back * scale, frontLift * Math.Min(1, scale), backLift * Math.Min(1, scale));
    }

    /// <summary>A channel at <paramref name="at"/>: at its first key before it, eased from key to key, at its last after.</summary>
    private static double Channel(IReadOnlyList<BodyKey> keys, double at)
    {
        if (keys.Count == 0) return 0;
        if (at <= keys[0].At) return keys[0].Value;
        for (int i = 1; i < keys.Count; i++)
        {
            var (was, to) = (keys[i - 1], keys[i]);
            if (at >= to.At) continue;
            return was.Value + (to.Value - was.Value) * MotionPlay.Eased(to.Ease, (at - was.At) / (to.At - was.At));
        }
        return keys[^1].Value;
    }

    /// <summary>A foot at <paramref name="at"/>: eased along each of its steps from where the one before left it and lifted on a half sine.</summary>
    private static (double X, double Lift) FootAt(IReadOnlyList<FootStep> steps, Foot foot, double at)
    {
        double x = 0;
        foreach (var step in steps)
        {
            if (step.Foot != foot) continue;
            if (at >= step.To)
            {
                x = step.Ahead;
                continue;
            }
            if (at <= step.From) break;
            double p = (at - step.From) / (step.To - step.From);
            return (x + (step.Ahead - x) * MotionPlay.Eased(Ease.Sine, p), step.Lift * Math.Sin(Math.PI * p));
        }
        return (x, 0);
    }

    /// <summary>
    /// <paramref name="state"/> laid over <paramref name="shown"/>, the limbs <paramref name="pose"/> gives, as differences from rest;
    /// none off the floor (in the air, climbing, afloat). Standing, the hip sits at the feet's middle and <see cref="BodyState.Push"/>
    /// ahead, lowered by the dip and where a leg cannot reach its foot, each leg reaching its ankle with the knee forward, its boot flat;
    /// striding (<paramref name="moving"/> 1) the legs keep their gait and only the torso and the arms move. Nothing goes more than
    /// <paramref name="roomPx"/> ahead, nor further back than <paramref name="roomBackPx"/>.
    /// </summary>
    public static BodyLayer? Over(Limbs shown, BodyState state, Pose pose, double moving, double roomPx, double roomBackPx)
    {
        if (pose is not (Pose.Stand or Pose.Walk or Pose.Run)) return null;
        var layer = new BodyLayer(0, 0, state.Lean, new Bend(state.FarArm, state.FarElbow), state.Kick, default, default, 0, 0, state.Grip);
        double legs = Math.Clamp(1 - moving, 0, 1);
        if (legs <= 0) return layer;
        var (now, rest) = (Stance(state, roomPx, roomBackPx, shown.Tilt), Stance(BodyState.Rest, double.PositiveInfinity, double.PositiveInfinity, shown.Tilt));
        static Bend Apart(Bend a, Bend b, double by) => new((a.Upper - b.Upper) * by, (a.Lower - b.Lower) * by);
        var hip = new Vec((now.HipX - rest.HipX) * legs, (now.HipY - rest.HipY) * legs);
        // Laid over the pose's own legs, a toe or heel may reach under the floor: kept out of it as the gait's are.
        (Bend, double) Kept(Bend leg, double foot, Bend by, double turn, double hipX)
        {
            var (kept, boot) = Poses.OnFloor(new Bend(leg.Upper + by.Upper, leg.Lower + by.Lower), foot + turn, hipX, shown.Tilt, shown.Lift, hip);
            return (new Bend(kept.Upper - leg.Upper, kept.Lower - leg.Lower), boot - foot);
        }
        var (frontLeg, frontFoot) = Kept(shown.FrontLeg, shown.FrontFoot, Apart(now.FrontLeg, rest.FrontLeg, legs), (now.FrontFoot - rest.FrontFoot) * legs, Rig.HipApart);
        var (backLeg, backFoot) = Kept(shown.BackLeg, shown.BackFoot, Apart(now.BackLeg, rest.BackLeg, legs), (now.BackFoot - rest.BackFoot) * legs, -Rig.HipApart);
        return layer with { HipX = hip.X, HipY = hip.Y, FrontLeg = frontLeg, BackLeg = backLeg, FrontFoot = frontFoot, BackFoot = backFoot };
    }

    /// <summary>
    /// Where the upper body goes under <paramref name="layer"/>, in the body's frame: turned by its lean about the hip and moved
    /// with the hip; nowhere and unturned with none, so every held item, beam, light and cape is where it was without an answer.
    /// </summary>
    public static (Vec At, double Turn) WaistOf(BodyLayer layer) =>
        (new Vec(layer.HipX - Rig.Hip * Math.Sin(layer.Lean), -Rig.Hip + layer.HipY + Rig.Hip * Math.Cos(layer.Lean)), layer.Lean);

    /// <summary>The standing body of <paramref name="state"/>: where the hip's middle is from the feet, and each leg and boot.</summary>
    internal readonly record struct Standing(double HipX, double HipY, Bend FrontLeg, Bend BackLeg, double FrontFoot, double BackFoot);

    internal static Standing Stance(BodyState state, double roomPx, double roomBackPx, double tilt)
    {
        double front = Math.Clamp(state.Front, -roomBackPx, roomPx), back = Math.Clamp(state.Back, -roomBackPx, roomPx);
        double hipX = Math.Clamp((front + back) / 2 + state.Push, -roomBackPx, roomPx), hipY = -Rig.Hip + state.Dip;
        var frontAnkle = new Vec(Rig.HipApart + front, -state.FrontLift - Rig.Sole);
        var backAnkle = new Vec(-Rig.HipApart + back, -state.BackLift - Rig.Sole);
        // Low enough that both legs reach, never locked straight.
        double reach = Rig.ToAnkle.Upper + Rig.ToAnkle.Lower - BodyAnswers.LegSlack;
        foreach (var (ankle, side) in new[] { (frontAnkle, 1), (backAnkle, -1) })
        {
            double dx = ankle.X - (hipX + side * Rig.HipApart);
            hipY = Math.Max(hipY, ankle.Y - Math.Sqrt(Math.Max(0, reach * reach - dx * dx)));
        }
        var frontLeg = Rig.Reach(new Vec(frontAnkle.X - hipX - Rig.HipApart, frontAnkle.Y - hipY), Rig.ToAnkle, -1);
        var backLeg = Rig.Reach(new Vec(backAnkle.X - hipX + Rig.HipApart, backAnkle.Y - hipY), Rig.ToAnkle, -1);
        // Boots flat by the floor's rule, turning back toward the shin as a foot rises.
        static double Kept(double lift) => Math.Clamp(1 - lift / Feel.Feet.Lifting, 0, 1);
        return new(hipX, hipY, frontLeg, backLeg, Poses.Flat(frontLeg, tilt) * Kept(state.FrontLift), Poses.Flat(backLeg, tilt) * Kept(state.BackLift));
    }
}

/// <summary>
/// The body layer drawn (body-answers): what it is wanted to be, eased over <see cref="BodyAnswers.LayerMs"/> as the arm's hand-overs
/// are (<see cref="SwingArm.Left"/>) whenever it starts or stops applying or the hero turns round, in the world's axes, so the stance
/// swaps sides without a jump; snapped where the body drops (lying down, dying).
/// </summary>
public struct BodyBlend
{
    /// <summary>The layer drawn last, and how far it was from the one wanted at the last hand-over, in the world's axes.</summary>
    private BodyLayer _drawn, _offset;
    private double _sinceMs;
    private bool _on, _begun;
    private int _facing;

    /// <summary>The layer to draw <paramref name="elapsedMs"/> after the last, in the facing's frame: <paramref name="wanted"/>, none for no layer.</summary>
    public BodyLayer Next(BodyLayer? wanted, int facing, bool snap, double elapsedMs)
    {
        var want = (wanted ?? BodyLayer.None).Mirrored(facing);
        bool on = wanted != null;
        if (snap || !_begun) (_offset, _sinceMs) = (BodyLayer.None, BodyAnswers.LayerMs);
        else if (on != _on || facing != _facing && (on || _sinceMs < BodyAnswers.LayerMs)) (_offset, _sinceMs) = (_drawn.Plus(want, -1), 0);
        else _sinceMs += elapsedMs;
        (_on, _facing, _begun) = (on, facing, true);
        _drawn = want.Plus(_offset, SwingArm.Left(_sinceMs, BodyAnswers.LayerMs));
        return _drawn.Mirrored(facing);
    }
}
