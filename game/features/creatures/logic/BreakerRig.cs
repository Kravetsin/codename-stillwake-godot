using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>How a kind of breaker moves its bones in a pose (<see cref="BreakerRig"/>): each a turn and a shift from rest.</summary>
public interface IBreakerMoves
{
    void Move(in BreakerPose pose, Span<BreakerMove> moves);
}

/// <summary>A bone from rest: turned, shifted in its sheet's pixels, its other picture shown.</summary>
public readonly record struct BreakerMove(double A, double X = 0, double Y = 0, bool Swapped = false);

/// <summary>
/// What a breaker is drawn from: its clock, how much it is at work on a wreck rather than flying (0..1), how fast it flies, winding up,
/// gripping and reeling from a blow (0..1 each), how far it has come apart (0..<see cref="BreakerMotion.Apart"/>), what shines of it and in which colour.
/// </summary>
public readonly record struct BreakerPose(double T, double Work = 0, double Fly = 0, double Tell = 0, double Grip = 0, double Hurt = 0,
    double Dead = 0, double Lamp = 1, TickArt.Rgb Tint = default);

/// <summary>
/// A breaker of painted pieces (breakers-look): each hung on its parent's joint and moved by its kind (<see cref="IBreakerMoves"/>);
/// killed, the same pieces fly apart. The pick page's engine ported, its origin the body's middle. Plain C#, tested.
/// </summary>
public sealed class BreakerRig
{
    private readonly CreatureArt.BreakerDesign _design;
    private readonly IBreakerMoves _kind;
    private readonly BreakerMove[] _moves;
    private readonly (double X, double Y, double A)[] _at;
    private readonly bool[] _placed;

    public BreakerRig(CreatureArt.BreakerDesign design, IBreakerMoves kind)
    {
        (_design, _kind) = (design, kind);
        var bones = design.Bones;
        (_moves, _at, _placed) = (new BreakerMove[bones.Length], new (double, double, double)[bones.Length], new bool[bones.Length]);
        int slots = 0;
        foreach (var bone in bones) slots += 1 + (bone.Swap != null ? 1 : 0) + (bone.Shine != null ? 1 : 0);
        Slots = new InkSlot[slots];
    }

    /// <summary>Its pictures this frame in draw order, a swapped one clear while its other shows.</summary>
    public InkSlot[] Slots { get; }

    public void Place(in BreakerPose pose)
    {
        Array.Clear(_moves);
        Array.Clear(_placed);
        _kind.Move(pose, _moves);
        var bones = _design.Bones;
        double dead = pose.Dead, fade = dead > 0 ? Math.Clamp((BreakerMotion.Apart - dead) / 0.6, 0, 1) : 1;
        int n = 0;
        for (int i = 0; i < bones.Length; i++)
        {
            var bone = bones[i];
            var (x, y, a) = Joint(i);
            double atX = x * _design.Scale, atY = y * _design.Scale;
            if (dead > 0)
            {
                double own = i * 2.399, away = (Math.Sqrt(x * x + y * y) > 8 ? Math.Atan2(y, x) : own) + 0.3 * Math.Sin(own);
                atX += Math.Cos(away) * dead * (9 + i % 5 * 3);
                atY += Math.Sin(away) * dead * (9 + i % 4 * 3);
                a += dead * Math.Sin(i * 1.9) * 2.2;
            }
            bool swapped = bone.Swap != null && _moves[i].Swapped;
            Slots[n++] = Slot(bone.Picture, swapped ? 0 : fade);
            if (bone.Swap is { } other) Slots[n++] = Slot(other, swapped ? fade : 0);
            if (bone.Shine is { } shine) Slots[n++] = new InkSlot(shine, true, a, atX, atY, swapped ? 0 : fade * pose.Lamp, pose.Tint, _design.Sizes[shine] * bone.K);

            InkSlot Slot(string part, double alpha) => new(part, false, a, atX, atY, alpha, Scale: _design.Sizes[part] * bone.K);
        }
    }

    private (double X, double Y, double A) Joint(int i)
    {
        if (_placed[i]) return _at[i];
        var bone = _design.Bones[i];
        var move = _moves[i];
        double x = bone.X + move.X, y = bone.Y + move.Y, a = bone.A + move.A;
        if (bone.Parent >= 0)
        {
            var (upX, upY, upA) = Joint(bone.Parent);
            double k = _design.Bones[bone.Parent].K, cos = Math.Cos(upA), sin = Math.Sin(upA);
            (x, y, a) = (upX + (x * cos - y * sin) * k, upY + (x * sin + y * cos) * k, upA + a);
        }
        _placed[i] = true;
        return _at[i] = (x, y, a);
    }
}
