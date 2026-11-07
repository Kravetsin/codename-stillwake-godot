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

    /// <summary>What moves a cave lurker's node (cave-hostiles): its body, a leg, its head, its caps; a snail's tail, front, eye stalk, shell, plug.</summary>
    public enum LurkerDrive { Body, Leg, Head, Caps, Tail, Front, Stalk, Shell, Plug }

    /// <summary>What a lurker's light mask shines as: the garden's teal (violet as it flares), violet, amber at a snail's mouth, the amber of its veins, the cracks of a hurt shell, an eye.</summary>
    public enum LurkerShine { Teal, Violet, Amber, Vein, Crack, Eye }

    /// <summary>
    /// A lurker's pose numbers, the pick page's, pixels and radians: its body's drop hunkered and rise crouched and pitch; its legs folded
    /// hunkered, swung flying and splayed sagging; its head tucked and raised; its caps swayed sagging; a snail's tail and front drawn in,
    /// its eye stalks laid back, its shell's radius and rock, its plug pushed out open.
    /// </summary>
    public sealed record LurkerPose(double Drop = 0, double Rise = 0, double Pitch = 0, double LegFold = 0, double TuckX = 0, double TuckY = 0, double TuckRot = 0,
        double HeadUp = 0, double HeadCrouch = 0, double LegFly = 0, double LegSplay = 0, double SagCaps = 0, double TailIn = 0, double FrontIn = 0, double StalkBack = 0,
        double Radius = 0, double Rock = 0, (double X, double Y) PlugOut = default);

    /// <summary>A node of a lurker, in draw order: its picture (none for a snail's body), its parent's place (−1 none), its place on it, what moves it, its rest turn and stride phase, the light masks over it.</summary>
    public sealed record LurkerNode(string Id, string? Part, int Parent, double X, double Y, LurkerDrive Drive, double Phase = 0, double Base = 0, (string Part, LurkerShine Shine)[]? Lit = null);

    /// <summary>A light a lurker gives: on node <paramref name="Node"/> at its place, <paramref name="R"/> pixels, its colour and strength; a mouth's dims as its plug shuts it.</summary>
    public sealed record LurkerLight(int Node, double X, double Y, double R, LurkerShine Colour, double K, bool Mouth = false);

    /// <summary>A cave lurker's design in ink (<c>Features.Creatures.LurkerRig</c>): its parts, whether it is a snail, the point its body tips about, its pose numbers, nodes and lights.</summary>
    public sealed record LurkerDesign(string Id, bool Snail, (double X, double Y) Centre, IReadOnlyDictionary<string, Part> Parts, LurkerPose Pose, LurkerNode[] Nodes, LurkerLight[] Lights);
}
