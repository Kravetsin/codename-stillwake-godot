using System.Collections.Generic;

namespace Stillwake.Game;

public static partial class CreatureArt
{
    /// <summary>A two-bone limb in ink: its bones' parts, its hip on the body, where its foot rests (and how high), which way its knee bends, its step's phase of a stride, its turns folded in flight, its bones' lengths.</summary>
    public sealed record InkLeg(string Upper, string Lower, double HipX, double HipY, double FootX, double FootY, double Bend, double Phase, double FoldUpper, double FoldLower, double L1, double L2);

    /// <summary>The tick's design in ink: its parts and what its pose needs of them (<c>Features.Creatures.TickRig</c>). Places in world pixels, turns in radians.</summary>
    public sealed record TickDesign(string Id, IReadOnlyDictionary<string, Part> Parts, (double X, double Y) Centre, double Crouch, double Stride, double Lift, double LookAmp, double HeadUp,
        double HeadSaw, double JawOpen, double SawTilt, InkLeg[] FarLegs, InkLeg[] NearLegs, InkLeg Arm, (double X, double Y) NozzleAt, (double X, double Y) NozzleEnd, (double X, double Y) HeadAt,
        (double X, double Y) JawAt, (double X, double Y) Eye, (double X, double Y) EyeTravel, (double X, double Y) Cut, (double X, double Y)[] Arcs);

    /// <summary>A bone of a limb solved between a hip and a foot: the hip on the body's node, the foot's node, its bones, its ankle on the foot, its knee's way.</summary>
    public sealed record IstukanIk(double HipX, double HipY, int Foot, double L1, double L2, double AnkleX, double AnkleY, double Bend);

    /// <summary>
    /// One node of the Istukan in ink, in draw order: its picture, its parent (−1 none), its kind, and its place and turn in each key pose
    /// (x, y, a a pose: standing, folded, crouched, flown, stuck, dead), mixed in that order; its slow sway and its shiver while stuck.
    /// </summary>
    public sealed record IstukanNode(string Part, int Parent, IstukanNode.Kinds Kind, double[] Keys, IstukanIk? Ik = null, int Upper = -1, string? Seam = null, double Drift = 0, double Shiver = 0)
    {
        /// <summary>A plate placed by its keys, a limb's bone solved, or where its lamp or core shines.</summary>
        public enum Kinds { Plain, Upper, Lower, Lamp, Core }
    }

    /// <summary>The Istukan's design in ink (<c>Features.Creatures.IstukanRig</c>): its parts, which node its body is, its nodes.</summary>
    public sealed record IstukanDesign(string Id, IReadOnlyDictionary<string, Part> Parts, int Body, IstukanNode[] Nodes);
}
