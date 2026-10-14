using System;
using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A hero's figure (apps/game/src/features/crew/figure.ts): helmet and visor, body, air pack, two arms
/// and two legs that bend at shoulders and elbows, hips and knees, each part a picture from
/// <c>art/hero_*.png</c> the rig moves and turns smoothly. <see cref="Show"/> places it at the hero's
/// feet and poses it.
/// </summary>
public partial class Figure : Node2D
{
    /// <summary>The far arm and leg, darkened by this much, so the body reads as round.</summary>
    private const float FarShade = 0.7f;
    /// <summary>Milliseconds for a limb to settle into a new angle: a pose change eases, a stride barely lags.</summary>
    private const double SettleMs = 30;
    /// <summary>Slower for the head, so it follows the pointer rather than snapping to it.</summary>
    private const double HeadSettleMs = 90;
    /// <summary>How far the air pack sticks out behind the middle: what a body lying on its back rests on.</summary>
    private const double Back = Rig.W * 0.62;

    /// <summary>Each part's joint, the pixel it turns at, as art.ts draws it.</summary>
    private static readonly Dictionary<string, Vector2> Joints = new()
    {
        ["helmet"] = new(6.5f, 12),
        ["torso"] = new(6, 0),
        ["pack"] = new(0, 0),
        ["upperArm"] = new(2.5f, 2.5f),
        ["forearm"] = new(2.5f, 2.9f),
        ["thigh"] = new(3, 2.5f),
        ["shin"] = new(3, 2.18f),
        ["sheet"] = new(0, 6),
    };

    private sealed record Limb(Node2D Upper, Node2D Lower)
    {
        /// <summary>Rotations run clockwise and the figure faces +x, so forward is negative.</summary>
        public void Bend(Bend b)
        {
            Upper.Rotation = (float)-b.Upper;
            Lower.Rotation = (float)-b.Lower;
        }
    }

    private readonly Limb _frontArm, _backArm, _frontLeg, _backLeg;
    private readonly Node2D _body, _helmet;
    private readonly Sprite2D _plan;

    private Limbs? _shown;
    private (int X, int Y)? _last;
    private double _travelled;
    // Where the stride began: a hero setting off starts its step from standing, not mid-stride.
    private double _strideFrom;
    private Pose? _wasPose;
    private double _timeMs, _head;
    /// <summary>Milliseconds of a landing still to spring back from.</summary>
    private double _landingMs;

    public Figure()
    {
        var far = new Color(FarShade, FarShade, FarShade);
        _backLeg = MakeLimb(Rig.Leg, "thigh", "shin", far, out var backLeg);
        _backArm = MakeLimb(Rig.Arm, "upperArm", "forearm", far, out var backArm);
        _frontLeg = MakeLimb(Rig.Leg, "thigh", "shin", Colors.White, out var frontLeg);
        _frontArm = MakeLimb(Rig.Arm, "upperArm", "forearm", Colors.White, out var frontArm);
        backLeg.Position = new Vector2((float)-Rig.HipApart, (float)-Rig.Hip);
        frontLeg.Position = new Vector2((float)Rig.HipApart, (float)-Rig.Hip);
        backArm.Position = frontArm.Position = new Vector2(0, (float)-Rig.Shoulder);
        var torso = new Node2D();
        var pack = Part("pack");
        pack.Position = new Vector2((float)-Back, (float)(-Rig.Shoulder - Rig.H * 0.02));
        var chest = Part("torso");
        chest.Position = new Vector2(0, (float)-Rig.Neck);
        torso.AddChild(pack);
        torso.AddChild(chest);
        // The helmet turns at the neck.
        _helmet = new Node2D { Position = new Vector2(0, (float)-Rig.Neck) };
        _helmet.AddChild(Part("helmet"));
        _plan = Part("sheet");
        _plan.Visible = false;

        // The body turns and leans around its middle: its parts hang from a node half its height down.
        _body = new Node2D();
        var parts = new Node2D { Position = new Vector2(0, (float)(Rig.H / 2)) };
        _body.AddChild(parts);
        // Both legs come out from under the body, which hides their joints; the near arm hangs in front,
        // its hand over the sheet it holds.
        foreach (var n in new Node2D[] { backArm, backLeg, frontLeg, torso, _helmet, _plan, frontArm }) parts.AddChild(n);
        AddChild(_body);
    }

    /// <summary>A part of the pixel-art hero, its joint at the origin.</summary>
    private static Sprite2D Part(string name, Color? shade = null) => new()
    {
        Texture = GD.Load<Texture2D>($"res://art/hero_{name}.png"),
        Centered = false,
        Offset = -Joints[name],
        SelfModulate = shade ?? Colors.White,
    };

    /// <summary>
    /// A limb hanging from its joint in two pictures, the upper turned at the joint and the lower at the
    /// elbow or knee; the upper over the lower, so the joint between them is the upper's rounded end.
    /// </summary>
    private static Limb MakeLimb(Segments size, string upper, string lower, Color shade, out Node2D view)
    {
        view = new Node2D();
        var bend = new Node2D { Position = new Vector2(0, (float)size.Upper) };
        bend.AddChild(Part(lower, shade));
        view.AddChild(bend);
        view.AddChild(Part(upper, shade));
        return new Limb(view, bend);
    }

    /// <summary><paramref name="from"/> moved toward <paramref name="to"/> over <paramref name="elapsedMs"/>, settling in about <paramref name="within"/> ms.</summary>
    private static double Settle(double from, double to, double elapsedMs, double within = SettleMs) =>
        from + (to - from) * Math.Min(1, elapsedMs / within);

    private static Bend SettleBend(Bend from, Bend to, double elapsedMs) =>
        new(Settle(from.Upper, to.Upper, elapsedMs), Settle(from.Lower, to.Lower, elapsedMs));

    /// <summary>An angle settled toward <paramref name="to"/> the short way round, so a body stops tumbling without unwinding.</summary>
    private static double SettleAngle(double from, double to, double elapsedMs)
    {
        const double turn = 2 * Math.PI;
        double apart = ((to - from) % turn + turn + Math.PI) % turn - Math.PI;
        return Settle(to - apart, to, elapsedMs);
    }

    /// <summary>
    /// Places the figure at <paramref name="hero"/>'s feet and eases it into <paramref name="pose"/>, its
    /// head turned by <paramref name="look"/> while awake and the plan in its hands while
    /// <paramref name="holdsPlan"/>.
    /// </summary>
    public void Show(Hero hero, Pose pose, double look, bool holdsPlan, double elapsedMs)
    {
        _timeMs += elapsedMs;
        // Walking backwards, away from where it faces, the stride runs backwards too.
        if (_last is var (lx, ly)) _travelled += ((double)(hero.X - lx) * hero.Facing + Math.Abs(hero.Y - ly)) / Hero.Unit;
        _last = (hero.X, hero.Y);
        if (pose != _wasPose) _strideFrom = _travelled;
        if (_wasPose == Pose.Air && pose is Pose.Stand or Pose.Walk) _landingMs = Poses.Land.Ms;
        _landingMs = Math.Max(0, _landingMs - elapsedMs);
        _wasPose = pose;
        bool holding = holdsPlan && Poses.HoldsIn(pose);
        var to = Poses.LimbsOf(pose, hero, new Moment(_travelled - _strideFrom, _timeMs, _landingMs / Poses.Land.Ms, holding ? _head : null));
        var from = _shown ?? to;
        var shown = new Limbs(
            SettleBend(from.FrontArm, to.FrontArm, elapsedMs),
            SettleBend(from.BackArm, to.BackArm, elapsedMs),
            SettleBend(from.FrontLeg, to.FrontLeg, elapsedMs),
            SettleBend(from.BackLeg, to.BackLeg, elapsedMs),
            SettleAngle(from.Tilt, to.Tilt, elapsedMs),
            Settle(from.Lift, to.Lift, elapsedMs),
            to.Lying);
        _shown = shown;
        _frontArm.Bend(shown.FrontArm);
        _backArm.Bend(shown.BackArm);
        _frontLeg.Bend(shown.FrontLeg);
        _backLeg.Bend(shown.BackLeg);
        _body.Rotation = (float)shown.Tilt;
        bool awake = pose is not (Pose.Lie or Pose.Tumble);
        _head = Settle(_head, awake ? look : 0, elapsedMs, HeadSettleMs);
        _helmet.Rotation = (float)_head;
        // The sheet is in the front hand, tilted to face the eyes: up toward them looking up, flatter
        // looking down at it.
        _plan.Visible = holding;
        if (holding)
        {
            var hand = Rig.EndOf(shown.FrontArm, Rig.Arm);
            _plan.Position = new Vector2((float)hand.X, (float)(-Rig.Shoulder + hand.Y));
            _plan.Rotation = (float)Poses.SheetTurn(_head);
        }
        // Lying down, the middle sinks as the body turns, so it comes to rest on the floor.
        double c = Math.Abs(Math.Cos(shown.Tilt)), s = Math.Abs(Math.Sin(shown.Tilt));
        _body.Position = new Vector2(0, (float)(shown.Lying ? -(Rig.H / 2) * c - Back * s : -Rig.H / 2 - shown.Lift));
        Position = new Vector2((float)((double)hero.X / Hero.Unit * Rig.Cell), (float)((double)hero.Y / Hero.Unit * Rig.Cell));
        Scale = new Vector2(hero.Facing, 1);
    }
}
