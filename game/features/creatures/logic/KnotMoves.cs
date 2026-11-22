using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// «Клубок» moving (breakers-look): a core wound in bands that lift off it as it winds up, two arms. At work on a wreck it takes
/// hold, pries three times, yanks its whole ball back and picks at the plate by turns before the next bite. Plain C#, tested.
/// </summary>
public sealed class KnotMoves : IBreakerMoves
{
    /// <summary>A bite at a wreck, in seconds.</summary>
    public const double BiteS = 2.6;

    public static readonly KnotMoves One = new();

    private static readonly int Core = Bone("core"), Long0 = Bone("long0"), Long1 = Bone("long1"), Short = Bone("short"), Cross = Bone("cross");
    private static readonly int[] Arms = [Bone("arm0"), Bone("arm1")], Hands = [Bone("hand0"), Bone("hand1")];

    private static int Bone(string id) => Array.FindIndex(CreatureArt.BreakerKnot.Bones, b => b.Id == id) is var at and >= 0 ? at : throw new InvalidOperationException(id);

    public void Move(in BreakerPose p, Span<BreakerMove> m)
    {
        double t = p.T, w = Math.Sin(t * 3), busy = p.Work;
        double bite = t / BiteS - Math.Floor(t / BiteS);
        double pry = Pulse(bite, 0.2, 0.06, 0.56, 0.08) * Math.Sin((bite - 0.2) / 0.4 * Math.Tau * 3), yank = Pulse(bite, 0.64, 0.05, 0.74, 0.18);

        double loose = p.Tell + p.Hurt * 0.7 + (w + 1) * 0.07 - p.Grip * 0.1;
        loose += (0.08 + yank * 0.3 + Math.Max(0, pry) * 0.1 - loose) * busy;
        double turn = p.Tell * Math.Sin(t * 47) * 0.03 + p.Fly * Math.Sin(t * 5) * 0.15 + p.Hurt * 0.3;
        m[Core] = new BreakerMove(Mix(turn, pry * 0.13 - yank * 0.28, busy), busy * (-yank * 62 + Math.Max(0, pry) * 16), Swapped: p.Dead > 0);
        m[Long0] = m[Long1] = new BreakerMove(-loose * 0.65);
        m[Short] = new BreakerMove(-loose * 0.9);
        m[Cross] = new BreakerMove(loose * 0.5);

        for (int i = 0; i < Arms.Length; i++)
        {
            double side = i == 1 ? 1 : -1, u = Math.Sin(t * 5 + i * 2);
            double held = Pulse(bite, i == 1 ? 0.05 : 0.12, 0.1, i == 1 ? 0.66 : 0.7, 0.08), pick = (1 - held) * Math.Max(0, Math.Sin(t * 8 + i * Math.PI));
            double arm = u * 0.2 + side * p.Tell * 0.7 - side * p.Grip * 0.35, hand = u * 0.3 + side * 0.3 - side * p.Grip * 0.5;
            double armAt = side * (-0.22 * held + 0.3 * pick) - pry * 0.18 * held + side * yank * 0.2, handAt = side * (0.5 * pick - 0.5 * held) + pry * 0.3 * held;
            m[Arms[i]] = new BreakerMove(Mix(arm, armAt, busy), busy * (-45 * pick - 22 * (1 - held)));
            m[Hands[i]] = new BreakerMove(Mix(hand, handAt, busy), Swapped: busy > 0.5 ? held > 0.5 || yank > 0.5 : p.Grip > 0.5);
        }
    }

    private static double Mix(double a, double b, double u) => a + (b - a) * u;

    private static double Pulse(double at, double from, double rise, double till, double fall) =>
        TickMotion.Ease((at - from) / rise) * (1 - TickMotion.Ease((at - till) / fall));
}
