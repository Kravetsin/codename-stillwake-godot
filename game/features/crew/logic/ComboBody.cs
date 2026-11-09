using System;
using System.Collections.Generic;
using Feel = Stillwake.Game.HeroMotion;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The body of a combo's step at a moment (axe-combos), each channel of <see cref="BodyMotion"/>: the torso's <see cref="Lean"/>,
/// the hip's <see cref="Dip"/> and <see cref="Push"/>, the far arm, the far hand's <see cref="Grip"/>, and each foot, pixels ahead
/// of where it stands at rest (<see cref="Front"/>, <see cref="Back"/>) and lifted.
/// </summary>
public readonly record struct BodyState(double Lean, double Dip, double Push, double FarArm, double FarElbow, double Grip,
    double Front, double Back, double FrontLift = 0, double BackLift = 0)
{
    /// <summary>Standing still: every channel at rest, the feet where they stand.</summary>
    public static readonly BodyState Rest = new(0, 0, 0, 0, Combos.FarElbowRest, 0, 0, 0);

    /// <summary>Whether every channel is within <paramref name="by"/> of <paramref name="other"/>'s.</summary>
    public bool Near(BodyState other, double by) =>
        Math.Abs(Lean - other.Lean) < by && Math.Abs(Dip - other.Dip) < by && Math.Abs(Push - other.Push) < by
        && Math.Abs(FarArm - other.FarArm) < by && Math.Abs(FarElbow - other.FarElbow) < by && Math.Abs(Grip - other.Grip) < by
        && Math.Abs(Front - other.Front) < by && Math.Abs(Back - other.Back) < by && Math.Abs(FrontLift - other.FrontLift) < by
        && Math.Abs(BackLift - other.BackLift) < by;
}

/// <summary>
/// What the body layer changes of the limbs a pose gives (axe-combos), in the facing's frame, as differences from them: the hip
/// moved (<see cref="HipX"/> ahead, <see cref="HipY"/> down), the torso's <see cref="Lean"/>, the far arm's turn
/// (<see cref="FarArm"/>, in the body's frame, not the torso's), each leg's and boot's turn, how far the far hand holds the haft
/// (<see cref="Grip"/>) and how far the head follows the blade (<see cref="Follow"/>, 0 to 1).
/// </summary>
public readonly record struct BodyLayer(double HipX, double HipY, double Lean, Bend FarArm, Bend FrontLeg, Bend BackLeg,
    double FrontFoot, double BackFoot, double Grip, double Follow)
{
    /// <summary>Nothing changed: the pose as it is.</summary>
    public static readonly BodyLayer None = default;

    /// <summary>In the world's axes for a figure facing <paramref name="facing"/>, or back: what runs along x turned with it.</summary>
    public BodyLayer Mirrored(int facing) => new(HipX * facing, HipY, Lean * facing, Turned(FarArm, facing), Turned(FrontLeg, facing),
        Turned(BackLeg, facing), FrontFoot * facing, BackFoot * facing, Grip, Follow);

    /// <summary>This and <paramref name="by"/> times <paramref name="other"/>.</summary>
    public BodyLayer Plus(BodyLayer other, double by = 1) => new(HipX + other.HipX * by, HipY + other.HipY * by, Lean + other.Lean * by,
        Add(FarArm, other.FarArm, by), Add(FrontLeg, other.FrontLeg, by), Add(BackLeg, other.BackLeg, by), FrontFoot + other.FrontFoot * by,
        BackFoot + other.BackFoot * by, Grip + other.Grip * by, Follow + other.Follow * by);

    private static Bend Turned(Bend b, int facing) => new(b.Upper * facing, b.Lower * facing);
    private static Bend Add(Bend a, Bend b, double by) => new(a.Upper + b.Upper * by, a.Lower + b.Lower * by);
}

/// <summary>
/// The body in a combo's strike (axe-combos): each channel of its step's <see cref="BodyMotion"/> at a moment, on the real clock,
/// never stopped at a hit; and that laid over the limbs a pose gives.
/// </summary>
public static class ComboBody
{
    /// <summary>The body <paramref name="ms"/> into <paramref name="play"/>, from where the play before left it; none for a play that moves none.</summary>
    public static BodyState? At(MotionPlay play, double ms)
    {
        if (play.Body is not { } body) return null;
        var from = play.BodyFrom ?? BodyState.Rest;
        var rest = BodyState.Rest;
        var (front, frontLift) = FootAt(from.Front, body.Feet, Foot.Front, ms);
        var (back, backLift) = FootAt(from.Back, body.Feet, Foot.Back, ms);
        return new(Channel(body.Lean, ms, from.Lean, rest.Lean), Channel(body.Dip, ms, from.Dip, rest.Dip), Channel(body.Push, ms, from.Push, rest.Push),
            Channel(body.FarArm, ms, from.FarArm, rest.FarArm), Channel(body.FarElbow, ms, from.FarElbow, rest.FarElbow),
            Channel(body.Grip, ms, from.Grip, rest.Grip), front, back, frontLift, backLift);
    }

    /// <summary>A channel at <paramref name="ms"/>, its first key at <paramref name="first"/>; one with no keys stays at <paramref name="rest"/>.</summary>
    private static double Channel(IReadOnlyList<BodyKey> keys, double ms, double first, double rest)
    {
        if (keys.Count == 0) return rest;
        if (ms <= keys[0].Ms) return first;
        for (int i = 1; i < keys.Count; i++)
        {
            var (was, to) = (keys[i - 1], keys[i]);
            if (ms >= to.Ms) continue;
            double from = i == 1 ? first : was.Value;
            return from + (to.Value - from) * MotionPlay.Eased(to.Ease, (ms - was.Ms) / (to.Ms - was.Ms));
        }
        return keys.Count == 1 ? first : keys[^1].Value;
    }

    /// <summary>A foot <paramref name="ms"/> in, from <paramref name="x"/>: eased along each of its steps and lifted on a half sine.</summary>
    private static (double X, double Lift) FootAt(double x, IReadOnlyList<FootStep> steps, Foot foot, double ms)
    {
        foreach (var step in steps)
        {
            if (step.Foot != foot) continue;
            if (ms >= step.ToMs)
            {
                x = step.To;
                continue;
            }
            if (ms <= step.FromMs) break;
            double p = (ms - step.FromMs) / (step.ToMs - step.FromMs);
            return (x + (step.To - x) * MotionPlay.Eased(Ease.Sine, p), step.Lift * Math.Sin(Math.PI * p));
        }
        return (x, 0);
    }

    /// <summary>
    /// The body's way back to rest from <paramref name="at"/>, a step let go (axe-combos): every channel eased to rest over
    /// <see cref="Combos.BackMs"/>; the far foot home first if it moved, then the near one.
    /// </summary>
    public static BodyMotion BackOf(BodyState at)
    {
        var feet = new List<FootStep>();
        if (at.Back != 0) feet.Add(new(Foot.Back, 0, 0, Combos.BackFarMs, Combos.BackLift));
        if (at.Front != 0)
            feet.Add(feet.Count > 0 ? new(Foot.Front, 0, Combos.BackNearFromMs, Combos.BackNearToMs, Combos.BackLift) : new(Foot.Front, 0, 0, Combos.BackNearAloneMs, Combos.BackLift));
        var rest = BodyState.Rest;
        return new(Combos.BackOf(rest.Lean), Combos.BackOf(rest.Dip), Combos.BackOf(rest.Push), Combos.BackOf(rest.FarArm),
            Combos.BackOf(rest.FarElbow), Combos.BackOf(rest.Grip), feet);
    }

    /// <summary>
    /// <paramref name="state"/> laid over <paramref name="shown"/>, the limbs <paramref name="pose"/> gives, as differences from rest;
    /// none off the floor (in the air, climbing, afloat), where only the arm strikes. Standing, the hip sits at the feet's middle and
    /// <see cref="BodyState.Push"/> ahead, lowered by the dip and where a leg cannot reach its foot, each leg reaching its ankle with
    /// the knee forward, its boot flat; striding (<paramref name="moving"/> 1) the legs keep their gait and only the torso and far arm
    /// move. Nothing goes more than <paramref name="roomPx"/> ahead.
    /// </summary>
    public static BodyLayer? Over(Limbs shown, BodyState state, Pose pose, double moving, double roomPx)
    {
        if (pose is not (Pose.Stand or Pose.Walk or Pose.Run)) return null;
        var layer = new BodyLayer(0, 0, state.Lean, new Bend(state.FarArm - BodyState.Rest.FarArm, state.FarElbow - BodyState.Rest.FarElbow),
            default, default, 0, 0, state.Grip, 1);
        double legs = Math.Clamp(1 - moving, 0, 1);
        if (legs <= 0) return layer;
        var (now, rest) = (Stance(state, roomPx, shown.Tilt), Stance(BodyState.Rest, double.PositiveInfinity, shown.Tilt));
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
    /// with the hip; nowhere and unturned with none, so every held item, beam, light and cape is where it was without a combo.
    /// </summary>
    public static (Vec At, double Turn) WaistOf(BodyLayer layer)
    {
        double lean = layer.Lean;
        return (new Vec(layer.HipX - Rig.Hip * Math.Sin(lean), -Rig.Hip + layer.HipY + Rig.Hip * Math.Cos(lean)), lean);
    }

    /// <summary>The standing body of <paramref name="state"/>: where the hip's middle is from the feet, and each leg and boot.</summary>
    internal readonly record struct Standing(double HipX, double HipY, Bend FrontLeg, Bend BackLeg, double FrontFoot, double BackFoot);

    internal static Standing Stance(BodyState state, double roomPx, double tilt)
    {
        double front = Math.Min(state.Front, roomPx), back = Math.Min(state.Back, roomPx);
        double hipX = Math.Min((front + back) / 2 + state.Push, roomPx), hipY = -Rig.Hip + state.Dip;
        var frontAnkle = new Vec(Rig.HipApart + front, -state.FrontLift - Rig.Sole);
        var backAnkle = new Vec(-Rig.HipApart + back, -state.BackLift - Rig.Sole);
        // Low enough that both legs reach, never locked straight.
        double reach = Rig.ToAnkle.Upper + Rig.ToAnkle.Lower - Combos.LegSlack;
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
/// The body layer drawn (axe-combos): what it is wanted to be, eased over <see cref="Combos.LayerMs"/> as the arm's hand-overs are
/// (<see cref="SwingArm.Left"/>) whenever it starts or stops applying or the hero turns round, in the world's axes, so the stance
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
        if (snap || !_begun) (_offset, _sinceMs) = (BodyLayer.None, Combos.LayerMs);
        else if (on != _on || facing != _facing && (on || _sinceMs < Combos.LayerMs)) (_offset, _sinceMs) = (_drawn.Plus(want, -1), 0);
        else _sinceMs += elapsedMs;
        (_on, _facing, _begun) = (on, facing, true);
        _drawn = want.Plus(_offset, SwingArm.Left(_sinceMs, Combos.LayerMs));
        return _drawn.Mirrored(facing);
    }
}
