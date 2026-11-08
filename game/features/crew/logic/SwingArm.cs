using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The front arm of a figure that holds the axe (0040) or aims the harpoon (0044). During a swing, or
/// aiming at a phase that stays 0, the arm is straight and turns about the shoulder
/// (<see cref="AxeSwing.ArmAt"/>). Between swings it bends as its pose says. It never
/// jumps when a swing begins, ends or follows another toward a new point: the arm drawn the frame before
/// is kept as an offset from the new one, and the offset eases out over <see cref="BlendMs"/>.
/// </summary>
public struct SwingArm
{
    /// <summary>
    /// How long a hand-over eases, in ms: the axe's wind-up (0.35 of its 500 ms), so a swing begun from rest is the swing's
    /// own by its downstroke, and the blade drawn striking is the one that hits.
    /// </summary>
    public const double BlendMs = 175;

    private Bend? _drawn;
    private double? _phase;
    /// <summary>How far the arm drawn at the last hand-over was from the one wanted then.</summary>
    private Bend _offset;
    private double _sinceMs;
    /// <summary>How long the last hand-over eases, in ms; none given yet, <see cref="BlendMs"/>.</summary>
    private double? _blendMs;

    /// <summary>Whether the arm is swung or aimed and its last hand-over has eased out: drawn where it is wanted.</summary>
    public readonly bool Settled => _phase != null && _sinceMs >= (_blendMs ?? BlendMs);

    /// <summary>
    /// The arm to draw <paramref name="elapsedMs"/> after the last: swung to <paramref name="swing"/>'s
    /// angle from straight ahead of the shoulder, as far through as its phase says, or at
    /// <paramref name="rest"/> between swings; the elbow bent <paramref name="elbow"/> as the item held wants. A hand-over eases
    /// over <paramref name="blendMs"/>, <see cref="BlendMs"/> unless given.
    /// </summary>
    public Bend Next(Bend rest, (double Angle, double Phase)? swing, double elapsedMs, double elbow = 0, double? blendMs = null)
    {
        var wanted = swing is { Angle: var angle } ? AxeSwing.ArmAt(angle, elbow) : rest;
        // A swing begun or ended, or the next begun while held: its phase starts again.
        bool handOver = (swing is null) != (_phase is null) || swing?.Phase < _phase;
        _phase = swing?.Phase;
        if (handOver && _drawn is Bend was)
            (_offset, _sinceMs, _blendMs) = (new Bend(Poses.Apart(wanted.Upper, was.Upper), was.Lower - wanted.Lower), 0, blendMs ?? BlendMs);
        else _sinceMs += elapsedMs;
        double t = Math.Min(1, _sinceMs / (_blendMs ?? BlendMs)), left = 1 - t * t * (3 - 2 * t);
        // The elbow never folds back further than the arm wanted, whatever the pose does meanwhile.
        var drawn = new Bend(wanted.Upper + _offset.Upper * left, Math.Max(Math.Min(0, wanted.Lower), wanted.Lower + _offset.Lower * left));
        _drawn = drawn;
        return drawn;
    }

    /// <summary>
    /// The arm drawn in the end, <paramref name="arm"/>, after what moves it beside a swing (an item carried on a run): the next
    /// hand-over eases from there, so a swing begun on the run never snaps the arm out of its trail.
    /// </summary>
    public void Drew(Bend arm) => _drawn = arm;
}
