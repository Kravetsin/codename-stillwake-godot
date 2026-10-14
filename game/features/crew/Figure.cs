using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A hero's figure (apps/game/src/features/crew/figure.ts): helmet and visor, body, air pack, two arms
/// and two legs that bend at shoulders and elbows, hips and knees, each part a picture from
/// <c>art/hero_*.png</c> recoloured into its suit (<see cref="Wear"/>), lit where it stands as everything
/// in the ship is, the rig moving and turning the parts smoothly. <see cref="Show"/> places it at the
/// hero's feet and poses it, dust kicked up from its boots stepping and landing.
/// </summary>
public partial class Figure : Node2D
{
    /// <summary>The far arm and leg, in the suit's colour darkened by this much, so the body reads as round.</summary>
    private const double FarShade = 0.7;
    /// <summary>Milliseconds for a limb to settle into a new angle: a pose change eases, a stride barely lags.</summary>
    private const double SettleMs = 30;
    /// <summary>Slower for the head, so it follows the pointer rather than snapping to it.</summary>
    private const double HeadSettleMs = 90;
    /// <summary>Afloat, limbs lag as if pushing through water.</summary>
    private const double FloatSettleMs = 110, FloatTurnMs = 220;
    /// <summary>How far the air pack sticks out behind the middle: what a body lying on its back rests on.</summary>
    private const double Back = Rig.W * 0.62;
    /// <summary>Specks of dust a boot kicks up stepping, and a landing at most to either side, by its fall (DUST).</summary>
    private const int DustRun = 6, DustLandLeast = 4, DustLandMost = 12;
    private const double DustLandPerSpeed = 1.0 / 40;

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
    /// <summary>Every part with its picture's name, and whether it is a far limb's, to recolour.</summary>
    private readonly List<(Sprite2D Sprite, string Part, bool Far)> _parts = [];
    private int? _colour;

    private Limbs? _shown;
    private (int X, int Y)? _last;
    private double _travelled;
    // Where the stride began: a hero setting off starts its step from standing, not mid-stride.
    private double _strideFrom;
    private Pose? _wasPose;
    private double _timeMs, _head;
    /// <summary>Milliseconds of a landing still to spring back from.</summary>
    private double _landingMs;
    /// <summary>How long since passing out, and what picks the heap it ends in, new each time.</summary>
    private double _outMs;
    private int _slump;
    /// <summary>The step the walk is on, counted from where it began; a new one puts a foot down.</summary>
    private int? _step;
    /// <summary>How fast the hero last fell, thousandths of a cell a step: how hard it lands.</summary>
    private int _fall;

    public Figure(int colour)
    {
        _backLeg = MakeLimb(Rig.Leg, "thigh", "shin", far: true, out var backLeg);
        _backArm = MakeLimb(Rig.Arm, "upperArm", "forearm", far: true, out var backArm);
        _frontLeg = MakeLimb(Rig.Leg, "thigh", "shin", far: false, out var frontLeg);
        _frontArm = MakeLimb(Rig.Arm, "upperArm", "forearm", far: false, out var frontArm);
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
        Wear(colour);
    }

    /// <summary>The figure in a suit of <paramref name="colour"/>, 0xRRGGBB: its pictures recoloured, when it changes.</summary>
    public void Wear(int colour)
    {
        if (_colour == colour) return;
        _colour = colour;
        int far = Suits.Shade(colour, FarShade);
        foreach (var (sprite, part, isFar) in _parts) sprite.Texture = SuitArt.Of(part, isFar ? far : colour);
    }

    /// <summary>A part of the pixel-art hero, its joint at the origin, lit where it stands.</summary>
    private Sprite2D Part(string name, bool far = false)
    {
        var sprite = new Sprite2D { Centered = false, Offset = -Joints[name] };
        Lit.Set(sprite);
        _parts.Add((sprite, name, far));
        return sprite;
    }

    /// <summary>
    /// A limb hanging from its joint in two pictures, the upper turned at the joint and the lower at the
    /// elbow or knee; the upper over the lower, so the joint between them is the upper's rounded end.
    /// </summary>
    private Limb MakeLimb(Segments size, string upper, string lower, bool far, out Node2D view)
    {
        view = new Node2D();
        var bend = new Node2D { Position = new Vector2(0, (float)size.Upper) };
        bend.AddChild(Part(lower, far));
        view.AddChild(bend);
        view.AddChild(Part(upper, far));
        return new Limb(view, bend);
    }

    /// <summary><paramref name="from"/> moved toward <paramref name="to"/> over <paramref name="elapsedMs"/>, settling in about <paramref name="within"/> ms.</summary>
    private static double Settle(double from, double to, double elapsedMs, double within = SettleMs) =>
        from + (to - from) * Math.Min(1, elapsedMs / within);

    /// <summary>A limb settled the short way round, so arms whirling in space never unwind.</summary>
    private static Bend SettleBend(Bend from, Bend to, double elapsedMs, double within) =>
        new(SettleAngle(from.Upper, to.Upper, elapsedMs, within), SettleAngle(from.Lower, to.Lower, elapsedMs, within));

    /// <summary>An angle settled toward <paramref name="to"/> the short way round, so a body stops tumbling without unwinding.</summary>
    private static double SettleAngle(double from, double to, double elapsedMs, double within = SettleMs)
    {
        const double turn = 2 * Math.PI;
        double apart = ((to - from) % turn + turn + Math.PI) % turn - Math.PI;
        return Settle(to - apart, to, elapsedMs, within);
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
        if (pose == Pose.Lie && _wasPose != Pose.Lie) (_outMs, _slump) = (0, Random.Shared.Next());
        _outMs += elapsedMs;
        // Where the feet are, in pixels of the world.
        double feetX = (double)hero.X / Hero.Unit * Rig.Cell, feetY = (double)hero.Y / Hero.Unit * Rig.Cell;
        if (pose == Pose.Air) _fall = Math.Max(0, hero.Vy);
        if (_wasPose == Pose.Air && pose is Pose.Stand or Pose.Walk or Pose.Run)
        {
            _landingMs = Poses.Land.Ms;
            int puff = Math.Min(DustLandMost, Math.Max(DustLandLeast, (int)Math.Round(_fall * DustLandPerSpeed, MidpointRounding.AwayFromZero)));
            Particles.Shared.Emit(Effect.Dust, feetX, feetY, puff, -0.35);
            Particles.Shared.Emit(Effect.Dust, feetX, feetY, puff, Math.PI + 0.35);
        }
        // A foot comes down at the end of each half stride, a quarter stride in.
        // Only running kicks up dust; a walk is quiet.
        double? stride = pose == Pose.Run ? Poses.Stride.Run : null;
        int? stepping = stride is double st ? (int)Math.Floor((_travelled - _strideFrom - st / 4) / (st / 2)) : null;
        if (stepping != null && _step != null && stepping != _step)
            Particles.Shared.Emit(Effect.Dust, feetX + hero.Facing * Rig.W * 0.3, feetY, DustRun);
        _step = stepping;
        _landingMs = Math.Max(0, _landingMs - elapsedMs);
        _wasPose = pose;
        bool holding = holdsPlan && Poses.HoldsIn(pose);
        var to = Poses.LimbsOf(pose, hero, new Moment(_travelled - _strideFrom, _timeMs, _landingMs / Poses.Land.Ms, holding ? _head : null, _outMs, _slump));
        var from = _shown ?? to;
        double within = pose == Pose.Float ? FloatSettleMs : SettleMs;
        var shown = new Limbs(
            SettleBend(from.FrontArm, to.FrontArm, elapsedMs, within),
            SettleBend(from.BackArm, to.BackArm, elapsedMs, within),
            SettleBend(from.FrontLeg, to.FrontLeg, elapsedMs, within),
            SettleBend(from.BackLeg, to.BackLeg, elapsedMs, within),
            // Afloat the body turns slowly, stretching out head first or righting itself to land.
            SettleAngle(from.Tilt, to.Tilt, elapsedMs, pose == Pose.Float ? FloatTurnMs : SettleMs),
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
        _body.Position = new Vector2(0, (float)((shown.Lying ? -(Rig.H / 2) * c - Back * s : -Rig.H / 2) - shown.Lift));
        Position = new Vector2((float)feetX, (float)feetY);
        Scale = new Vector2(hero.Facing, 1);
    }
}

/// <summary>
/// The hero's pictures in any suit (<c>textureOf</c> of art.ts): <c>art/hero_*.png</c> are drawn in
/// <see cref="Suits.Drawn"/>, and each of its shades becomes the same shade of another; gear, visor, pack and
/// the plan's sheet keep their colours. Made once a part and colour.
/// </summary>
public static class SuitArt
{
    private static readonly Dictionary<(string Part, int Colour), Texture2D> Made = new();

    public static Texture2D Of(string part, int colour)
    {
        if (Made.TryGetValue((part, colour), out var kept)) return kept;
        var image = GD.Load<Texture2D>($"res://art/hero_{part}.png").GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        int[] from = Suits.ShadesOf(Suits.Drawn), to = Suits.ShadesOf(colour);
        if (colour != Suits.Drawn)
            for (int y = 0; y < image.GetHeight(); y++)
                for (int x = 0; x < image.GetWidth(); x++)
                {
                    var c = image.GetPixel(x, y);
                    int at = Array.IndexOf(from, (c.R8 << 16) | (c.G8 << 8) | c.B8);
                    if (c.A8 > 0 && at >= 0) image.SetPixel(x, y, new Color((uint)(to[at] << 8 | c.A8)));
                }
        return Made[(part, colour)] = ImageTexture.CreateFromImage(image);
    }
}
