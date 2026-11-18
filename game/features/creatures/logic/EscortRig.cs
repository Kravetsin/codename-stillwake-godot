using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The escort A1 «Конвоир» in ink (derelict-enemies): where each of its parts goes in a <see cref="Pose"/>, the guard page's <c>drawA1</c>
/// ported. Legs solved hip to ankle as it strides, its rail gun turned to its aim and kicked back by a shot, its claw arm reaching out by
/// two-bone IK and its jaws opening, its camera head bowed and everything stowed in its stand, its red lamp shining. Its origin the middle
/// of its feet, facing right, y down, in world pixels. Plain C#, tested.
/// </summary>
public sealed class EscortRig
{
    private static readonly CreatureArt.PartsDesign D = CreatureArt.InkEscort;

    /// <summary>World pixels to a cell: the page drew in cells.</summary>
    private const double Px = 8;

    /// <summary>The hip over the feet; the legs' bones; the ankle's height over the sole; a stride's reach and lift; the step's turns a cell.</summary>
    private const double HipX = -0.1, HipY = -2.4, Thigh = 1.15, Shin = 1.15, Ankle = 0.36, Stride = 0.7, Lift = 0.42;

    /// <summary>A stride's turn of phase for each cell walked: the page's 2π over 2.8 cells.</summary>
    public const double PhasePerCell = 2 * Math.PI / 2.8;

    /// <summary>The gun's pivot, its turn at rest and stowed, its length, its aim's limits up and down, how far a shot kicks it back.</summary>
    private const double GunX = -0.5, GunY = -4.92, GunRest = 0.03, GunStow = 0.55, GunLength = 2.35, AimUp = -1.25, AimDown = 0.5, Kick = 0.35;

    /// <summary>The head's pivot and its bow stowed; its lamp on it.</summary>
    private const double HeadX = 0.1, HeadY = -4.5, HeadBow = 0.3, LampX = 0.7, LampY = -0.21;

    /// <summary>The claw's shoulder and bones; the jaws' hinges on the pincer; how far they open.</summary>
    private const double ShoulderX = 0.45, ShoulderY = -4.08, Upper = 1.0, Fore = 1.4, JawX = 0.24, JawY = 0.08, JawOpen = 0.55;

    /// <summary>Where the wrist reaches at rest, stowed and reaching out: the page's.</summary>
    private static readonly (double X, double Y) WristRest = (1.2, -3.0), WristFold = (0.85, -3.4), WristOut = (2.8, -3.7);

    /// <summary>
    /// A pose: its <paramref name="Phase"/> in its stride and how much it walks (<paramref name="Move"/>, 0..1); how stowed in its stand
    /// (<paramref name="Fold"/>, 0..1); where it aims, in its own pixels, and how much (<paramref name="AimMix"/>); a shot's kick
    /// (<paramref name="Recoil"/>) and its coils' charge (<paramref name="Coil"/>), 0..1; its claw reached out (<paramref name="Claw"/>) and
    /// its jaws open (<paramref name="Open"/>), 0..1; its lamp (<paramref name="Lamp"/>), 0..1.
    /// </summary>
    public readonly record struct Pose(double Phase, double Move, double Fold, (double X, double Y)? Aim, double AimMix, double Recoil, double Coil, double Claw,
        double Open, double Lamp);

    /// <summary>Where its gun's muzzle and its lamp are in a pose, in its own pixels.</summary>
    public readonly record struct Anchors((double X, double Y) Muzzle, (double X, double Y) Lamp, double GunTurn);

    /// <summary>Its pictures this frame, in draw order.</summary>
    public InkSlot[] Slots { get; } = new InkSlot[18];

    private static readonly TickArt.Rgb Red = new(255, 43, 28), Coils = new(168, 236, 255);

    /// <summary>Places its parts in <paramref name="p"/>; answers where its muzzle and lamp are.</summary>
    public Anchors Place(Pose p)
    {
        double bob = -Math.Abs(Math.Sin(p.Phase)) * 0.09 * p.Move, jx = -p.Recoil * 0.14;
        var hip = (X: HipX + jx, Y: HipY + bob);
        int s = 0;
        Leg(ref s, hip, p, far: true);
        // The gun, behind the body, turned to its aim.
        var pivot = (X: GunX + jx, Y: GunY + bob);
        double turn = GunRest + p.Fold * GunStow;
        if (p.Aim is { } aim && p.AimMix > 0) turn += (Math.Clamp(Math.Atan2(aim.Y / Px - pivot.Y, aim.X / Px - pivot.X), AimUp, AimDown) - turn) * p.AimMix;
        double back = -p.Recoil * Kick, cos = Math.Cos(turn), sin = Math.Sin(turn);
        var gun = (X: pivot.X + cos * back, Y: pivot.Y + sin * back);
        Slots[s++] = Plate("gun", turn, gun);
        Slots[s++] = new InkSlot("gun_coil", true, turn, gun.X * Px, gun.Y * Px, Math.Clamp(p.Coil, 0, 1), Coils);
        Slots[s++] = Plate("torso", 0, (jx, bob));
        // The head, bowed as it is stowed; its lamp on it.
        double bow = p.Fold * HeadBow;
        var head = (X: HeadX + jx, Y: HeadY + bob);
        var lamp = (X: head.X + LampX * Math.Cos(bow) - LampY * Math.Sin(bow), Y: head.Y + LampX * Math.Sin(bow) + LampY * Math.Cos(bow));
        Slots[s++] = Plate("head", bow, head);
        double lit = Math.Clamp(p.Lamp, 0, 1);
        Slots[s++] = new InkSlot("lamp", true, 0, lamp.X * Px, lamp.Y * Px, lit, Red);
        Slots[s++] = new InkSlot("lamp_heart", true, 0, lamp.X * Px, lamp.Y * Px, lit, TickArt.White);
        Leg(ref s, hip, p, far: false);
        Arm(ref s, p, jx, bob);
        Slots[s++] = Plate("shoulder", 0, (jx, bob));
        var muzzle = (gun.X + cos * GunLength, gun.Y + sin * GunLength);
        return new Anchors((muzzle.Item1 * Px, muzzle.Item2 * Px), (lamp.X * Px, lamp.Y * Px), turn);
    }

    /// <summary>A plate at (<paramref name="at"/>) in cells, turned <paramref name="a"/>.</summary>
    private static InkSlot Plate(string part, double a, (double X, double Y) at) => new(part, false, a, at.X * Px, at.Y * Px);

    /// <summary>A bone drawn hanging from <paramref name="from"/> turned to reach toward <paramref name="to"/>.</summary>
    private static InkSlot Bone(string part, (double X, double Y) from, (double X, double Y) to) =>
        Plate(part, Math.Atan2(to.Y - from.Y, to.X - from.X) - Math.PI / 2, from);

    /// <summary>A leg: its ankle where its stride puts it, its knee solved forward between, its foot flat under the ankle; the far one half a stride on.</summary>
    private void Leg(ref int s, (double X, double Y) hip, Pose p, bool far)
    {
        double ph = p.Phase + (far ? Math.PI : 0), stride = Math.Sin(ph) * Stride * p.Move, lift = Math.Max(0, Math.Cos(ph)) * Lift * p.Move;
        var ankle = (X: 0.05 + stride + (far ? -0.12 : 0.12), Y: -Ankle - lift);
        var knee = Knee(hip, ankle, Thigh, Shin, 1);
        string tail = far ? "Far" : "";
        Slots[s++] = Bone("thigh" + tail, hip, knee);
        Slots[s++] = Bone("shin" + tail, knee, ankle);
        Slots[s++] = Plate("foot" + tail, 0, ankle);
    }

    /// <summary>The claw arm: its wrist where its reach and stowing put it, its elbow solved under, its pincer along its forearm, its jaws opened about their hinges.</summary>
    private void Arm(ref int s, Pose p, double jx, double bob)
    {
        static (double X, double Y) Mix((double X, double Y) a, (double X, double Y) b, double f) => (a.X + (b.X - a.X) * f, a.Y + (b.Y - a.Y) * f);
        var shoulder = (X: ShoulderX + jx, Y: ShoulderY + bob);
        var reach = Mix(Mix(WristRest, WristFold, Math.Clamp(p.Fold, 0, 1)), WristOut, Math.Clamp(p.Claw, 0, 1));
        var elbow = Knee(shoulder, (reach.X + jx, reach.Y + bob), Upper, Fore, -1);
        double a = Math.Atan2(reach.Y + bob - elbow.Y, reach.X + jx - elbow.X);
        var wrist = (X: elbow.X + Math.Cos(a) * Fore, Y: elbow.Y + Math.Sin(a) * Fore);
        Slots[s++] = Bone("clawUpper", shoulder, elbow);
        Slots[s++] = Bone("clawFore", elbow, wrist);
        Slots[s++] = Plate("pincer", a, wrist);
        double open = Math.Clamp(p.Open, 0, 1) * JawOpen;
        (double X, double Y) On(double x, double y) => (wrist.X + x * Math.Cos(a) - y * Math.Sin(a), wrist.Y + x * Math.Sin(a) + y * Math.Cos(a));
        Slots[s++] = Plate("jawUpper", a - open, On(JawX, -JawY));
        Slots[s++] = Plate("jawLower", a + open, On(JawX, JawY));
    }

    /// <summary>The joint between bones <paramref name="l1"/> and <paramref name="l2"/> from <paramref name="from"/> toward <paramref name="to"/>, bent its <paramref name="bend"/> way (the page's <c>ik</c>).</summary>
    private static (double X, double Y) Knee((double X, double Y) from, (double X, double Y) to, double l1, double l2, double bend)
    {
        double dx = to.X - from.X, dy = to.Y - from.Y, d = Math.Clamp(Math.Sqrt(dx * dx + dy * dy), Math.Abs(l1 - l2) + 0.001, l1 + l2 - 0.001);
        double b = Math.Acos(Math.Clamp((l1 * l1 + d * d - l2 * l2) / (2 * l1 * d), -1, 1)), an = Math.Atan2(dy, dx) - bend * b;
        return (from.X + Math.Cos(an) * l1, from.Y + Math.Sin(an) * l1);
    }

    /// <summary>Every part it places: its design's.</summary>
    public static bool Places(string part) => D.Parts.ContainsKey(part);
}
