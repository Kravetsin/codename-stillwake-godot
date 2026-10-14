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
    private const double FloatSettleMs = 110;
    /// <summary>
    /// Afloat: how slowly the body turns to the way it swims, the arms reaching that way at once, and how
    /// slowly treading water, the breaststroke and the crawl blend into each other. Left for right it
    /// turns at once: a flat figure squeezed through a turn looks worse than a jump.
    /// </summary>
    private const double FloatTurnMs = 650, SwimBlendMs = 320;
    /// <summary>The most the arms reach ahead of a body still turning, in radians, so they never pass through it.</summary>
    private const double ArmsLeadMost = 1.2;
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
        ["sheet"] = new(0, 8),
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
    private double _outMs, _outTilt;
    private int _slump;
    private readonly Sprite2D _screen;
    private readonly List<Line2D> _beams = new();
    private readonly List<Vector2> _beamTo = new();
    private double _beamMs;
    /// <summary>
    /// Placing plans, the hand thrusts the planner at them and it beams each one in, a line of its light
    /// fading over <see cref="BeamMs"/>; at most <see cref="BeamsMost"/> lines, a long drag beaming its
    /// first cells.
    /// </summary>
    private const double BeamMs = 320;
    private const int BeamsMost = 8;
    private static readonly Color BeamColour = new(0.49f, 0.83f, 0.99f);
    private static readonly CanvasItemMaterial BeamMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };
    private double _swim, _fast, _heading;
    /// <summary>Passed out in space, limbs go slack slowly, as if the strength drained out of them.</summary>
    private const double SlackMs = 450;
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
        // The planner's screen glows: drawn unlit over it, so it shows in the dark.
        _screen = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://art/hero_sheet_screen.png"),
            Centered = false,
            Offset = -Joints["sheet"],
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded },
        };
        _plan.AddChild(_screen);
        for (int i = 0; i < BeamsMost; i++)
        {
            var beam = new Line2D { TopLevel = true, Width = 1, Visible = false, ZIndex = 1, Material = BeamMaterial };
            _beams.Add(beam);
            AddChild(beam);
        }

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

    /// <summary>Has the hero aim its planner at <paramref name="cells"/>, in pixels of the world, and beam them in.</summary>
    public void Beam(IReadOnlyList<Vector2> cells)
    {
        _beamTo.Clear();
        for (int i = 0; i < cells.Count && i < BeamsMost; i++) _beamTo.Add(cells[i]);
        _beamMs = BeamMs;
    }

    /// <summary>A line of the planner's light from its screen to each cell just planned, fading.</summary>
    private void ShowBeams(bool holding)
    {
        float left = (float)(_beamMs / BeamMs);
        // The middle of its screen.
        var from = _plan.ToGlobal(new Vector2(7, -4));
        for (int i = 0; i < _beams.Count; i++)
        {
            var beam = _beams[i];
            beam.Visible = holding && left > 0 && i < _beamTo.Count;
            if (!beam.Visible) continue;
            beam.Points = [from, _beamTo[i]];
            beam.DefaultColor = BeamColour with { A = 0.85f * left * left };
        }
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
        if (pose is Pose.Lie or Pose.Tumble && pose != _wasPose) (_outMs, _slump, _outTilt) = (0, Random.Shared.Next(), _shown?.Tilt ?? 0);
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
        if (Poses.Swimming(hero)) _heading = Poses.HeadingOf(hero);
        bool afloat = pose == Pose.Float;
        _swim = afloat ? Settle(_swim, Poses.Swimming(hero) ? 1 : 0, elapsedMs, SwimBlendMs) : 0;
        _fast = afloat ? Settle(_fast, Poses.SwimmingFast(hero) ? 1 : 0, elapsedMs, SwimBlendMs) : 0;
        var to = Poses.LimbsOf(pose, hero, new Moment(_travelled - _strideFrom, _timeMs, _landingMs / Poses.Land.Ms, holding ? _head : null,
            _outMs, _slump, _outTilt, _swim, _fast, _heading));
        var from = _shown ?? to;
        double within = pose switch { Pose.Float => FloatSettleMs, Pose.Tumble => SlackMs, _ => SettleMs };
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
        // Afloat, the arms already reach the way the body is still turning to.
        double lead = afloat ? Math.Clamp(Poses.Apart(shown.Tilt, to.Tilt), -ArmsLeadMost, ArmsLeadMost) : 0;
        // Placing plans, the planner thrusts at them: out fast, back slower.
        double t = 1 - _beamMs / BeamMs;
        double thrust = holding && _beamMs > 0 ? (t < 0.3 ? t / 0.3 : (1 - t) / 0.7) : 0;
        var frontArm = shown.FrontArm;
        double aim = 0;
        if (thrust > 0 && _beamTo.Count > 0)
        {
            // Toward the first cell, from the shoulder, as the figure faces.
            var at = _beamTo[0];
            double dx = (at.X - feetX) * hero.Facing, dy = at.Y - (feetY - Rig.Shoulder);
            aim = Math.Atan2(dy, dx);
            double reach = 0.95 * (Rig.Arm.Upper + Rig.Arm.Lower);
            var held = Rig.EndOf(shown.FrontArm, Rig.Arm);
            var point = new Vec(Math.Cos(aim) * reach, Math.Sin(aim) * reach);
            frontArm = Rig.Reach(new Vec(held.X + (point.X - held.X) * thrust, held.Y + (point.Y - held.Y) * thrust), Rig.Arm, 1);
        }
        _frontArm.Bend(frontArm with { Upper = frontArm.Upper + lead });
        _backArm.Bend(shown.BackArm with { Upper = shown.BackArm.Upper + lead });
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
            var hand = Rig.EndOf(frontArm, Rig.Arm);
            _plan.Position = new Vector2((float)hand.X, (float)(-Rig.Shoulder + hand.Y));
            double turn = Poses.SheetTurn(_head);
            _plan.Rotation = (float)(turn + Poses.Apart(turn, aim) * thrust);
            _screen.Modulate = new Color(1, 1, 1) * (float)(1 + 0.8 * thrust);
        }
        _beamMs = Math.Max(0, _beamMs - elapsedMs);
        // Lying down, the middle sinks as the body turns, so it comes to rest on the floor.
        double c = Math.Abs(Math.Cos(shown.Tilt)), s = Math.Abs(Math.Sin(shown.Tilt));
        _body.Position = new Vector2(0, (float)((shown.Lying ? -(Rig.H / 2) * c - Back * s : -Rig.H / 2) - shown.Lift));
        Position = new Vector2((float)feetX, (float)feetY);
        Scale = new Vector2(hero.Facing, 1);
        ShowBeams(holding);
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
