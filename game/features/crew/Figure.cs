using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Players;
using L = Stillwake.Game.Light;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A hero's figure (apps/game/src/features/crew/figure.ts): a head with its face and hair, body, two arms
/// and two legs that bend at shoulders and elbows, hips and knees, each part a picture from
/// <c>art/hero_*.png</c> recoloured into its suit and skin (<see cref="Wear"/>): in a suit's helmet, the face behind its
/// glass, and its body with the air pack, or bare, the hair and a jumpsuit. Lit where it stands as everything
/// in the ship is, the rig moving and turning the parts smoothly. <see cref="Show"/> places it at the
/// hero's feet and poses it, its boots heard stepping, jumping and landing and kicking up dust: this
/// player's own hero heard the same anywhere, any other from where it is in the world; neither while floating.
/// </summary>
public partial class Figure : Node2D
{
    /// <summary>Milliseconds for a limb to settle into a new angle: a pose change eases, a stride barely lags.</summary>
    private const double SettleMs = 30;
    /// <summary>Slower for the head, so it follows the pointer rather than snapping to it.</summary>
    private const double HeadSettleMs = 90;
    /// <summary>Afloat, limbs lag as if pushing through water.</summary>
    private const double FloatSettleMs = 110;
    /// <summary>
    /// Afloat (0036): how slowly the upright body leans into a swim and rights itself, and how slowly
    /// drifting still, swimming and swimming fast blend into each other. Left for right it turns at once:
    /// a flat figure squeezed through a turn looks worse than a jump.
    /// </summary>
    private const double LeanMs = 450, SwimBlendMs = 320;
    /// <summary>How far the air pack sticks out behind the middle: what a body lying on its back rests on.</summary>
    private const double Back = Rig.W * 0.62;
    /// <summary>Motes of dust a boot kicks up running, and the grains a landing throws at least and at most to either side, by its fall (0075).</summary>
    private const int DustRun = 5, DustLandLeast = 3, DustLandMost = 9;
    private const double DustLandPerSpeed = 1.0 / 100;

    /// <summary>Each part's joint, the pixel it turns at, as art.ts draws it.</summary>
    private static readonly Dictionary<string, Vector2> Joints = new()
    {
        ["helmet"] = new(6.5f, 12),
        ["hair"] = new(8.5f, 14),
        ["torso"] = new(6, 0),
        ["pack"] = new(0, 0),
        ["upperArm"] = new(2.5f, 2.5f),
        ["forearm"] = new(2.5f, 2.9f),
        ["thigh"] = new(3, 2.5f),
        ["shin"] = new(3, 2.18f),
        ["sheet"] = new(0, 5),
    };

    private sealed record Limb(Node2D Upper, Node2D Lower, Node2D? Ankle = null)
    {
        /// <summary>Rotations run clockwise and the figure faces +x, so forward, and a boot's toe up (<paramref name="foot"/>), is negative.</summary>
        public void Bend(Bend b, double foot = 0)
        {
            Upper.Rotation = (float)-b.Upper;
            Lower.Rotation = (float)-b.Lower;
            if (Ankle != null) Ankle.Rotation = (float)-foot;
        }
    }

    /// <summary>
    /// How far the boot's picture reaches up under the shin's, in pixels (2026-11-04-hero-gait): the shin's picture is cut at the
    /// ankle and the boot turns on its own, so no gap opens beside the cut as it turns.
    /// </summary>
    private const float BootOverlap = 1f;

    private readonly Limb _frontArm, _backArm, _frontLeg, _backLeg;
    private readonly Node2D _frontHip, _backHip;
    private readonly Node2D _body, _helmet;
    private readonly Sprite2D _headLamp, _lens;
    /// <summary>The scanner in the lamp's mount (2026-11-07-the-scanner), and its glass's dot lit, drawn again unlit over it.</summary>
    private readonly Sprite2D _scanner, _scannerDot;
    /// <summary>The scanner's dots on the helmet worn, lit one at a time down its glass.</summary>
    private readonly Texture2D?[] _scannerDots = new Texture2D?[3];
    /// <summary>How long each dot stays lit, and the dark beat after the last, in ms.</summary>
    private const double ScannerDotMs = 220;
    private bool _scannerWorn;
    /// <summary>The parts shown by what is worn: the air pack with the body; the helmet's shell and glass, else the hair and beard.</summary>
    private readonly Sprite2D _pack, _shell, _visor, _hair, _beard, _headPart, _facePart;
    /// <summary>The head and face again, added unlit under the helmet's glass while its lamp is on: the face lit by its spill.</summary>
    private readonly Sprite2D _headLit, _faceLit;
    private readonly Node2D _faceLight;
    private const float FaceLight = 0.6f;
    private static readonly Material Added = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add, LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    private Look _look = Look.Zero;
    private bool _lampWorn;
    /// <summary>The suit's body worn, its item; null bare.</summary>
    private string? _bodyWorn = Stillwake.Content.Suit.Basic;
    /// <summary>A switched body's lights drawn unlit over its parts (2026-11-03-the-regen-suit): each band layer, its part, its part's name, whether a far limb's, the light it gives (2026-11-03-hero-lights).</summary>
    private readonly List<(Sprite2D Light, Sprite2D Part, string Name, bool Far, Glow Glow)> _bands = [];
    /// <summary>The collar's arcs and the socket's pip of an empty belt, over the chest.</summary>
    private readonly Sprite2D _arcs = new() { Centered = false, Material = Unshaded }, _pip = new() { Centered = false, Material = Unshaded };
    /// <summary>The light the tracker, the arcs and the pip give (2026-11-03-hero-lights), and every light of the hero's own.</summary>
    /// <summary>A helmet's goggles lit, its <c>_lit</c> picture (tracker-catchers), and the light they give.</summary>
    private readonly Sprite2D _eyes = new() { Centered = false, Material = Unshaded, Visible = false };
    private readonly Glow _eyesGlow = new() { Reach = L.TrackerReach, Keep = L.TrackerKeep };
    private readonly Glow _trackerGlow = new() { Reach = L.TrackerReach, Keep = L.TrackerKeep }, _arcsGlow = new() { Reach = L.SuitReach, Keep = L.SuitKeep },
        _pipGlow = new() { Reach = L.SuitReach, Keep = L.SuitKeep, Colour = new Color(L.SuitPip.R, L.SuitPip.G, L.SuitPip.B) * L.SuitPipLight };
    private readonly List<Glow> _glows = [];
    private bool _givesLight = true;
    private static readonly Color SuitLight = new(L.SuitLight.R, L.SuitLight.G, L.SuitLight.B);
    private Sprite2D _chest = null!;
    private static readonly Material Unshaded = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    private static readonly Dictionary<string, Texture2D?> Lights = new();
    /// <summary>The helmet worn, its item; null bare.</summary>
    private string? _helmetWorn;

    /// <summary>The visor's middle on the helmet, from the neck, facing +x: where air breathed in is drawn to.</summary>
    private static readonly Vector2 VisorOn = new(4, -5);

    /// <summary>The helmet lamp's lens, its two warm pixels drawn again unlit: each figure's own, freed with it.</summary>
    private readonly Texture2D _lensPicture = MakeLens();

    private static Texture2D MakeLens()
    {
        var image = Image.CreateEmpty(1, 2, false, Image.Format.Rgba8);
        image.SetPixel(0, 0, new Color("fff6ce"));
        image.SetPixel(0, 1, new Color("f0be5a"));
        return ImageTexture.CreateFromImage(image);
    }
    /// <summary>The jetpack worn on the back over the air pack (0047), a placeholder picture till its art is picked.</summary>
    private readonly Sprite2D _jetpack;
    /// <summary>The cape (a prototype): its inside behind the hero, its outside over it, arms and all.</summary>
    private readonly Cape _cape = new();
    private readonly CapeView _capeInside, _capeOutside;
    /// <summary>Where the jetpack's nozzle is in its picture: what the plume leaves from.</summary>
    private static readonly Vector2 NozzleAt = new(3, 14);
    private readonly Sprite2D _plan;
    /// <summary>Every part with its picture's name, and whether it is a far limb's, to recolour.</summary>
    private readonly List<(Sprite2D Sprite, string Part, bool Far)> _parts = [];
    /// <summary>Each part's offset for a pixel picture; a hi-res one has <see cref="HiRes.FigureMargin"/> more round it.</summary>
    private readonly Dictionary<Sprite2D, Vector2> _joints = new();
    /// <summary>The boots: the shin's picture below the ankle, turning there (2026-11-04-hero-gait).</summary>
    private readonly HashSet<Sprite2D> _soles = [];
    private int? _colour;

    /// <summary>The player's mark, a tracker strapped to the near shin (2026-11-02-hero-clothes): the strap lit as the hero is, its light unlit, so it reads in a dark room, tinted by the player's colour.</summary>
    private readonly Sprite2D _trackerLight = new() { Centered = false, Offset = TrackerAt, Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded } };
    private readonly Sprite2D _tracker = new() { Centered = false, Offset = TrackerAt };
    private static readonly Vector2 TrackerAt = -Joints["shin"] - new Vector2(HiRes.FigureMargin, HiRes.FigureMargin);
    private static Texture2D? _trackerStrapPicture, _trackerLightPicture;

    private Limbs? _shown;
    private (int X, int Y)? _last;
    private double _travelled;
    // Where the stride began: a hero setting off starts its step from standing, not mid-stride.
    private double _strideFrom;
    /// <summary>
    /// The gait on the floor, eased (<see cref="HeroMotion.Gait"/>): the strides the legs have gone through, from backpedalling
    /// (-1) through walking (0) into running (1), and from standing (0) into striding (1).
    /// </summary>
    private double _cycle, _gait, _moving;
    private Pose? _wasPose;
    private double _timeMs, _head;
    /// <summary>The lean, the landing and the air's timers, read from the motion (0090).</summary>
    private readonly Motion _motion = new();

    /// <summary>The steps the controller makes whole a step, drawn eased up and down them (0106).</summary>
    private readonly StepEase _steps = new(Rig.Cell, HeroMotion.StepMs);
    /// <summary>How long since passing out, and what picks the heap it ends in, new each time.</summary>
    private double _outMs, _outTilt;
    private int _slump;
    private readonly Sprite2D _screen;
    /// <summary>
    /// The planner's screen lights what is near it, a little, in its blue: added over the room's light
    /// as an ember's is, brighter as it beams.
    /// </summary>
    private readonly Glow _glow = new() { Position = new Vector2(6, -4) };
    private const float GlowStrength = 0.35f;
    private static readonly Color GlowColour = new(0.45f, 0.8f, 1f);
    private readonly List<Line2D> _beams = new();
    private readonly List<Vector2> _beamTo = new();
    private double _beamMs;
    /// <summary>
    /// Placing plans, the hand thrusts the planner at them and it beams each one in, a line of its light
    /// fading over <see cref="BeamMs"/>; at most <see cref="BeamsMost"/> lines, a long drag beaming its
    /// first cells.
    /// </summary>
    public const double BeamMs = 320;
    private const int BeamsMost = 8;
    /// <summary>
    /// Working a plan, the screwdriver in the front hand (0030), the arm reaching out a share
    /// <see cref="WorkReach"/> of its length, the tool held at one angle as a hand holds it. Each
    /// <see cref="TwistMs"/> the wrist turns for <see cref="TurnShare"/> of it — the handle's ribs rolling
    /// on three steps, the knuckles coming over so the hand, seen from the side, rises
    /// <see cref="TwistLift"/> pixels and presses <see cref="TwistPush"/> on — then lets go and turns back
    /// to grip again, the hand sinking.
    /// </summary>
    private const double TwistMs = 900, TurnShare = 0.6, TwistLift = 1.5, TwistPush = 0.6, WorkReach = 0.85;
    /// <summary>The builder at work (<see cref="HeldLook.Twist"/>, <see cref="HeldLook.Driven"/>): a frame of its strip, by its grip.</summary>
    private readonly Sprite2D _tool;
    /// <summary>What the front fist holds, the fire axe, the harpoon, the drill or the screwdriver, in the picture of its look (<see cref="HeldLook"/>).</summary>
    private readonly Sprite2D _held;
    /// <summary>A gun's charged picture, its lit pixels alone, over the held one as bright as its charge (<see cref="Charge"/>), lit as it is.</summary>
    private readonly Sprite2D _charged = new() { Centered = false, Visible = false, UseParentMaterial = true };

    /// <summary>The charge's own light on the held stage, held while it charges, the brighter the fuller (2026-10-27-the-railgun).</summary>
    private static readonly (int R, int G, int B) ChargeLight = (95, 216, 255);
    /// <summary>
    /// A stroke playing (<see cref="AxeSwing"/>, swing-motions): where it is aimed from the feet, in pixels of the world's axes,
    /// how far through it is, and the motion it plays.
    /// </summary>
    private (Vector2 Aim, double Phase, MotionPlay Play)? _chop;
    /// <summary>How far the feet are stepped up or down as the figure was last drawn, in pixels (<see cref="StepEase"/>).</summary>
    private double _stepLift;
    /// <summary>The front arm straight through a swing and eased into and out of it (0040).</summary>
    private SwingArm _swingArm;
    /// <summary>How far into carrying its item trailing on a run the figure is, 0 to 1; and the spring its arm swings on (<see cref="HeroMotion.Carry"/>).</summary>
    private double _carry, _sinceCarryMs = double.PositiveInfinity;
    /// <summary>How long an aimed item takes to turn to its rest once the arm hangs.</summary>
    private const double RestMs = 200;

    /// <summary>How far an aimed item hanging has turned to its rest (<see cref="HeldLook.Rest"/>), 0 to 1: up at once as the arm comes up.</summary>
    private double _rest;
    private readonly Spring _armSpring = new(HeroMotion.Carry.SpringHz, HeroMotion.Carry.SpringDamping);
    /// <summary>The swipe of light the axe leaves on its downstroke (0075).</summary>
    private readonly ReachAir _air = new();

    /// <summary>The phase the swing was at last frame: a cut of the air begins as the blade crosses the fan's near edge.</summary>
    private double _cutPhase;
    /// <summary>Whether the swing playing bit something, and is drawn held at the strike (<see cref="AxeSwing.Shown"/>); the phase last drawn.</summary>
    private bool _bit;
    private double _lastPhase;
    /// <summary>The node the body's parts hang from: the hip's place at rest, the legs' frame and the feet's.</summary>
    private readonly Node2D _frame;
    /// <summary>
    /// The upper body (axe-combos): turned by a combo's lean about the hip and moved with it, the identity otherwise; where a
    /// shoulder is, the arms' own frame. What lies behind the legs, the far arm and the cape's inside, hangs from a twin of it.
    /// </summary>
    private readonly Node2D _waist, _waistBehind;
    /// <summary>The body a combo's step puts into the strike, eased in and out (<see cref="ComboBody"/>).</summary>
    private BodyBlend _bodyBlend;
    private readonly BladeTrailView _trail = new();
    /// <summary>The near arm's node among the parts: the held stage's view goes just under it.</summary>
    private readonly Node2D _frontArmNode;
    private Vector2? _workAt;
    private double _workMs;
    private static readonly Color BeamColour = new(0.49f, 0.83f, 0.99f);
    private static readonly CanvasItemMaterial BeamMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };
    private double _swim, _fast;
    /// <summary>Passed out in space, limbs go slack slowly, as if the strength drained out of them.</summary>
    private const double SlackMs = 450;
    /// <summary>The step the walk is on, counted from where it began; a new one puts a foot down.</summary>
    private int? _step;
    /// <summary>The boots heard, each sound by what they stand on: this player's own the same anywhere, another's where it is.</summary>
    private readonly Voices _boots;

    /// <summary>What the boots stand on, or last stood on while in the air (0093): what its steps, jumps and landings sound like.</summary>
    public S.Footing Footing { get; set; } = S.Footing.Deck;

    /// <summary>Whether its hero's feet are in water now (liquids): splashes going in and out, and strokes, not steps, as it moves.</summary>
    public bool InWater { get; set; }

    private readonly Splashes _splashes = new();

    /// <param name="own">Whether it is this player's hero, heard the same anywhere rather than from where it is.</param>
    public Figure(int colour, bool own)
    {
        _own = own;
        AddChild(_boots = new Voices(S.World, voices: 1, placed: !own));
        _backLeg = MakeLimb(Rig.Leg, "thigh", "shin", far: true, out var backLeg);
        _backHip = backLeg;
        _backArm = MakeLimb(Rig.Arm, "upperArm", "forearm", far: true, out var backArm);
        _frontLeg = MakeLimb(Rig.Leg, "thigh", "shin", far: false, out var frontLeg);
        _frontHip = frontLeg;
        _frontArm = MakeLimb(Rig.Arm, "upperArm", "forearm", far: false, out var frontArm);
        _frontArmNode = frontArm;
        backLeg.Position = new Vector2((float)-Rig.HipApart, (float)-Rig.Hip);
        frontLeg.Position = new Vector2((float)Rig.HipApart, (float)-Rig.Hip);
        backArm.Position = frontArm.Position = new Vector2(0, (float)-Rig.Shoulder);
        var torso = new Node2D();
        var pack = _pack = Part("pack");
        pack.Position = new Vector2((float)-Back, (float)(-Rig.Shoulder - Rig.H * 0.02));
        var chest = _chest = Part("torso");
        chest.Position = new Vector2(0, (float)-Rig.Neck);
        chest.AddChild(_arcs);
        chest.AddChild(_pip);
        Gives(_arcs, _arcsGlow);
        Gives(_pip, _pipGlow);
        _jetpack = new Sprite2D { Texture = PixelArt.Picture("hero_jetpack"), Centered = false, Visible = false };
        _jetpack.Position = pack.Position + new Vector2(-2, -1);
        Lit.Set(_jetpack);
        torso.AddChild(pack);
        torso.AddChild(_jetpack);
        torso.AddChild(chest);
        // The head turns at the neck: its face shows through the helmet's glass; bare, the hair over it.
        _helmet = new Node2D { Position = new Vector2(0, (float)-Rig.Neck) };
        (_headPart, _facePart) = (Part("head", joint: "helmet"), Part("face", joint: "helmet"));
        _faceLight = new Node2D { Modulate = new Color(new Color("fff6ce") * FaceLight, 1) };
        _faceLight.AddChild(_headLit = new Sprite2D { Centered = false, Offset = _headPart.Offset, Material = Added });
        _faceLight.AddChild(_faceLit = new Sprite2D { Centered = false, Offset = _facePart.Offset, Material = Added });
        (_beard, _hair) = (Part("beard", joint: "hair"), Part("hair", joint: "hair"));
        (_shell, _visor) = (Part("shell", joint: "helmet"), Part("visor", joint: "helmet"));
        foreach (var n in new Node2D[] { _headPart, _facePart, _faceLight, _beard, _hair, _shell, _visor }) _helmet.AddChild(n);
        _helmet.AddChild(_eyes);
        Gives(_eyes, _eyesGlow);
        // The helmet lamp on its brow (0077), not the player's colour; its lens drawn again unlit: it is the light.
        _headLamp = new Sprite2D { Texture = HeadLampArt.Picture, Centered = false, Offset = HeadLampArt.On - Joints["helmet"], Visible = false };
        Lit.Set(_headLamp);
        _lens = new Sprite2D { Texture = _lensPicture, Position = _headLamp.Offset + HeadLampArt.Lens, Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded } };
        _headLamp.AddChild(_lens);
        _helmet.AddChild(_headLamp);
        _scanner = new Sprite2D { Centered = false, Visible = false };
        Lit.Set(_scanner);
        _scanner.AddChild(_scannerDot = new Sprite2D { Centered = false, Material = Unshaded });
        _helmet.AddChild(_scanner);
        _plan = Part("sheet");
        _plan.Visible = false;
        // The planner's screen glows: drawn unlit over it, so it shows in the dark.
        _screen = new Sprite2D
        {
            Texture = PixelArt.Picture("hero_sheet_screen"),
            Centered = false,
            Offset = -Joints["sheet"],
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded },
        };
        _plan.AddChild(_screen);
        _plan.AddChild(_glow);
        _tool = new Sprite2D { Hframes = 3, Centered = false, Visible = false };
        Lit.Set(_tool);
        _held = new Sprite2D { Centered = false, Visible = false };
        Lit.Set(_held);
        _held.AddChild(_lamp);
        _held.AddChild(_lampGlow);
        _held.AddChild(_charged);
        _lampGlow.Colour = LampColour * LampLight;
        for (int i = 0; i < BeamsMost; i++)
        {
            var beam = new Line2D { TopLevel = true, Width = 1, Visible = false, ZIndex = 1, Material = BeamMaterial };
            _beams.Add(beam);
            AddChild(beam);
        }

        // The body turns and leans around its middle: its parts hang from a node half its height down.
        _body = new Node2D();
        var parts = _frame = new Node2D { Position = new Vector2(0, (float)(Rig.H / 2)) };
        _body.AddChild(parts);
        (_waist, _waistBehind) = (new Node2D(), new Node2D());
        // Both legs come out from under the body, which hides their joints; the near arm hangs in front,
        // its hand over the sheet it holds and closed round the axe's haft or the harpoon's stock.
        foreach (var n in new Node2D[] { _trail, _waistBehind, backLeg, frontLeg, _waist }) parts.AddChild(n);
        _waistBehind.AddChild(backArm);
        foreach (var n in new Node2D[] { torso, _helmet, _plan, _tool, _held, frontArm, _air }) _waist.AddChild(n);
        (_capeInside, _capeOutside) = (new CapeView(_cape, inside: true) { Visible = false }, new CapeView(_cape, inside: false) { Visible = false });
        Lit.Set(_capeInside);
        Lit.Set(_capeOutside);
        _waistBehind.AddChild(_capeInside);
        _waistBehind.MoveChild(_capeInside, 0);
        // The inside lies in the cape's shadow: darker, so it reads as depth behind the hero.
        _capeInside.SelfModulate = new Color(0.72f, 0.72f, 0.78f);
        _waist.AddChild(_capeOutside);
        _air.Position = new Vector2(0, (float)-Rig.Shoulder);
        AddChild(_body);
        Wear(colour);
    }

    /// <summary>The boots' <paramref name="cue"/> at the feet, on the World bus, silent while floating.</summary>
    private void Boots(S.Cue cue, float tune = 1, float offsetDb = 0) => _boots.Play(cue, GlobalPosition, tune, offsetDb);

    /// <summary>Has the hero aim its planner at <paramref name="cells"/>, in pixels of the world, and beam them in.</summary>
    public void Beam(IReadOnlyList<Vector2> cells)
    {
        _beamTo.Clear();
        for (int i = 0; i < cells.Count && i < BeamsMost; i++) _beamTo.Add(cells[i]);
        _beamMs = BeamMs;
    }

    /// <summary>A point of the world from the front shoulder, in the arms' own frame: the body as it is placed, turned, lifted and faced.</summary>
    private (double X, double Y) FromShoulder(Vector2 at)
    {
        var local = _waist.ToLocal(at);
        return (local.X, local.Y + Rig.Shoulder);
    }

    /// <summary>Has the hero work the plan at <paramref name="at"/>, in pixels of the world, with its screwdriver; null when it stops.</summary>
    public void Work(Vector2? at) => _workAt = at;

    /// <summary>
    /// A point <paramref name="aim"/> pixels from the feet, in the world's axes, from the front shoulder in the arms' own frame,
    /// the body as last drawn but faced <paramref name="facing"/>; straight ahead for none (swing-motions).
    /// </summary>
    private (double X, double Y) FromShoulder(Vector2 aim, int facing)
    {
        if (aim == Vector2.Zero) return (1, 0);
        // Never shown, the body has no place yet: the offset as it is, faced.
        if (_shown == null) return (aim.X * facing, aim.Y);
        var local = _waist.Transform.AffineInverse() * (_frame.Transform.AffineInverse() * (_body.Transform.AffineInverse() * new Vector2(aim.X * facing, aim.Y - (float)_stepLift)));
        return (local.X, local.Y + Rig.Shoulder);
    }

    /// <summary>
    /// The aim angle of a stroke aimed <paramref name="aim"/> pixels from the feet, the figure faced <paramref name="facing"/> to
    /// strike it: radians from straight ahead of the shoulder, down positive, as last drawn. Its sector picks the motion (<see cref="Sectors"/>).
    /// </summary>
    public double AimAngleOf(Vector2 aim, int facing)
    {
        var (dx, dy) = FromShoulder(aim, facing);
        return Math.Atan2(dy, dx);
    }

    /// <summary>
    /// Has the hero play the stroke of the tool it wields aimed <paramref name="swing"/>'s offset from its feet, in pixels of the
    /// world's axes, as far through as its phase says, the motion it plays; null between strokes.
    /// </summary>
    public void Chop((Vector2 Aim, double Phase, MotionPlay Play)? swing) => _chop = swing;

    /// <summary>How far, in pixels, the floor before the feet is free for a combo's step to carry the body (<see cref="Hero.RoomAhead"/>).</summary>
    public double RoomAhead { get; set; } = double.PositiveInfinity;

    /// <summary>The swing playing bit something: its arm is drawn stopped on contact for a moment (0075), the sim untouched.</summary>
    public void Bit() => _bit = true;

    private double _flinchMs = double.PositiveInfinity;
    private int _flinchFrom = 1;

    /// <summary>Rammed (0103) by something on the side <paramref name="from"/> says, +1 right in the world: the figure flinches from it.</summary>
    public void Flinch(int from) => (_flinchMs, _flinchFrom) = (0, from >= 0 ? 1 : -1);

    /// <summary>The item the front hand holds, by id, the fire axe or the harpoon: its hotbar slot chosen, the hand free; none for an empty hand.</summary>
    public string? Wields { get; set; }

    /// <summary>The modules fitted into what it wields, as the sim says of the stack its hero works with (0098): laid over its picture; none for most.</summary>
    public IReadOnlyList<Content.ModuleSpec> Fitted { get; set; } = [];

    private HeldParts? _heldParts;

    /// <summary><paramref name="look"/> with <see cref="Fitted"/> laid over it, made again only when either changes.</summary>
    private HeldParts PartsOf(HeldLook look)
    {
        if (_heldParts is not { } kept || kept.Look != look || !ReferenceEquals(_partsFitted, Fitted)) (_heldParts, _partsFitted) = (HeldParts.Of(look, Fitted), Fitted);
        return _heldParts!;
    }

    private IReadOnlyList<Content.ModuleSpec>? _partsFitted;

    /// <summary>Where the harpoon or the drill held aims, in pixels of the world: the arm straight, the barrel's or the bit's line through it; none to let it hang.</summary>
    public Vector2? Aim { get; set; }

    /// <summary>Whether the item held was drawn aimed at <see cref="Aim"/> last frame, its arm up and settled: its muzzle and edge are the aim's (aim-on-use).</summary>
    public bool Aimed { get; private set; }

    private bool _wasAiming;

    /// <summary>The suit's mode, its belt's charge and the breath's phase (<see cref="SuitGlow.Of"/>): what a switched body's lights show; all off by default.</summary>
    public (bool On, bool Charged, double Phase) Suit { get; set; }

    /// <summary>What its tracker shows (tracker-catchers): steady, blinking now and then, or hunted (<see cref="TrackerBlink"/>).</summary>
    public Sim.Crew.Signal Signal { get; set; }

    /// <summary>Whether it wears the tracker on its shin: every hero does; a catcher drawn as one does not (tracker-catchers).</summary>
    public bool Tracked { get; set; } = true;

    /// <summary>
    /// A catcher's gear lit (tracker-catchers): its receiver at <c>Receiver</c> in the figure's colour, as a tracker is (<see cref="TrackerBlink.Lit"/>),
    /// and its goggles at <c>Eyes</c> in <c>EyesColour</c> (0xRRGGBB); none for a hero, whose body lights are its suit's.
    /// </summary>
    public (double Receiver, double Eyes, int EyesColour)? GearLights { get; set; }

    /// <summary>Whether its tracker and lit bands light the world round them; off for a figure in a window of its own, out of the world.</summary>
    public bool GivesLight
    {
        get => _givesLight;
        set
        {
            _givesLight = value;
            foreach (var glow in _glows) glow.Visible = value;
        }
    }

    /// <summary>Whether the hero wears a jetpack on its back (0047).</summary>
    public bool Jetpack
    {
        get => _jetpack.Visible;
        set => _jetpack.Visible = value;
    }

    /// <summary>Whether the hero wears a cape (2026-10-27-the-cape-is-worn): a sheet of cloth from the shoulders to the floor.</summary>
    public bool WearsCape
    {
        get => _capeOutside.Visible;
        set
        {
            if (value == _capeOutside.Visible) return;
            _capeInside.Visible = _capeOutside.Visible = value;
            _cape.Reset();
        }
    }

    /// <summary>Whether the hero wears the helmet lamp (0077): it shows on the helmet's brow and shines, only on a helmet.</summary>
    public bool HeadLamp
    {
        get => _headLamp.Visible;
        set
        {
            _lampWorn = value;
            _headLamp.Visible = _faceLight.Visible = _lampWorn && _helmetWorn != null;
        }
    }

    /// <summary>Whether the hero wears the scanner (2026-11-07-the-scanner): along the helmet's crown, in the lamp's mount; only on a helmet.</summary>
    public bool Scanner
    {
        get => _scanner.Visible;
        set
        {
            if (value == _scannerWorn) return;
            _scannerWorn = value;
            ShowScanner();
        }
    }

    /// <summary>The scanner's pictures for the helmet worn, at its mount: each helmet's hugs its dome.</summary>
    private void ShowScanner()
    {
        _scanner.Visible = _scannerWorn && _helmetWorn != null;
        if (!_scanner.Visible) return;
        bool heavy = _helmetWorn == Stillwake.Content.Helmet.Heavy;
        string name = heavy ? "hero_heavyHelmet_scanner" : "hero_scanner";
        var (x, y) = heavy ? ScannerInk.OnHeavy : ScannerInk.On;
        _scanner.Texture = PixelArt.Picture(name);
        _scanner.Offset = _scannerDot.Offset = new Vector2((float)x - ScannerInk.Margin, (float)y - ScannerInk.Margin) - Joints["helmet"];
        for (int i = 0; i < _scannerDots.Length; i++) _scannerDots[i] = PixelArt.Picture($"{name}_lit_{i}");
    }

    /// <summary>The helmet lamp's lens as drawn, in pixels of the world: where its light shines from, turning with the head.</summary>
    public Vector2 HeadLampAt => _lens.GlobalPosition;

    /// <summary>The visor's middle as drawn, in pixels of the world, turning with the head: where a breath of room air puffs off (0081).</summary>
    public Vector2 Visor => _helmet.ToGlobal(VisorOn);

    /// <summary>The feet as drawn, in pixels of the world, and the way up the body, a unit vector: what a canister's vortex rises round (0081).</summary>
    public (Vector2 Feet, Vector2 Up) Stance => (_frame.ToGlobal(Vector2.Zero), -_frame.GlobalTransform.Y.Normalized());

    /// <summary>Where the helmet turns, at the neck, in pixels of the world: inside the hero, so never in a wall.</summary>
    public Vector2 HeadAt => _helmet.GlobalPosition;

    /// <summary>The way the helmet lamp points, a unit vector in the world: where the head looks, turned the way the hero faces.</summary>
    public Vector2 HeadLampToward => _helmet.GlobalTransform.X.Normalized();

    /// <summary>The worn jetpack's nozzle as drawn, in pixels of the world; none without one.</summary>
    public Vector2? Nozzle => _jetpack.Visible ? _jetpack.ToGlobal(NozzleAt) : null;

    /// <summary>Whether the harpoon held shows its rod in the barrel: ready to fire.</summary>
    public bool Loaded { get; set; } = true;

    /// <summary>How full the charge of the gun held is, 0..1 (2026-10-27-the-railgun): its charged picture laid over it that bright.</summary>
    public double Charge { get; set; }

    /// <summary>
    /// Whether the electric tool held runs (0054, 0055): the drill's bit spins up and its lamp lights, stopped, the lamp goes out and the
    /// bit winds down; the power driver's chuck turns at its work, flat, the wrist twists it (power-driver).
    /// </summary>
    public bool Spinning { get; set; }

    /// <summary>The drill's light and arcs on the held stage, made with it the first time a drill runs in the hand.</summary>
    private DrillLight? _drillLight;
    private Func<(double X, double Y), Vector2>? _heldAt;

    private readonly bool _own;

    /// <summary>What the figure itself throws, its boots' dust, its landing, its gloves' sparks: on its hero's layer.</summary>
    private Stage Worn => Stages.Of(_own ? Layers.Hero : Layers.OtherHeroes);

    private Stage? _heldStage;
    private EffectsView? _heldView;

    /// <summary>
    /// What the held tool throws (effects-on-their-layer), the drill's coil and the harpoon's muzzle: over the torso and the tool,
    /// under the front arm, its view a part of the figure between them. Made on first use, let go of as the figure leaves.
    /// </summary>
    public Stage Held
    {
        get
        {
            if (_heldStage != null) return _heldStage;
            _heldStage = Stages.Held();
            _heldView = new EffectsView(_heldStage) { InWorld = true };
            _waist.AddChild(_heldView);
            _waist.MoveChild(_heldView, _frontArmNode.GetIndex());
            return _heldStage;
        }
    }

    public override void _ExitTree()
    {
        if (_heldStage == null) return;
        Stages.Drop(_heldStage);
        _heldView!.QueueFree();
        (_heldStage, _heldView, _drillLight) = (null, null, null);
    }

    /// <summary>How fast the bit turns, from still (0) to full (1), and how far through its frames the picture is, in frames.</summary>
    private double _spin, _spinFrame;
    /// <summary>The held item's lamp, lit while it runs: its pixel drawn unlit, so it shows in the dark, and the little light it gives.</summary>
    private readonly ColorRect _lamp = new() { Size = Vector2.One, Color = LampColour, Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore, Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded } };
    private readonly Glow _lampGlow = new() { Visible = false };
    private static readonly Color LampColour = new(HeldLook.LampColour);
    private const float LampLight = 0.25f;

    /// <summary>The held harpoon's muzzle, or the drill's point, as drawn this frame, in pixels of the world; none while neither is shown.</summary>
    public Vector2? Muzzle { get; private set; }

    /// <summary>Where the held aimed item's edge begins as drawn this frame: the harpoon's breech, the cutter's nozzle, its jet's start.</summary>
    public Vector2? EdgeStart { get; private set; }

    /// <summary>The way the held harpoon's barrel points as drawn, a unit vector in the world.</summary>
    public Vector2 Barrel { get; private set; } = Vector2.Right;

    /// <summary>A line of the planner's light from its screen to each cell just planned, fading.</summary>
    private void ShowBeams(bool holding)
    {
        float left = (float)(_beamMs / BeamMs);
        // The middle of its screen.
        var from = _plan.ToGlobal(new Vector2(6, -4));
        for (int i = 0; i < _beams.Count; i++)
        {
            var beam = _beams[i];
            beam.Visible = holding && left > 0 && i < _beamTo.Count;
            if (!beam.Visible) continue;
            beam.Points = [from, _beamTo[i]];
            beam.DefaultColor = BeamColour with { A = 0.85f * left * left };
        }
    }

    /// <summary>
    /// A running boot's dust at (<paramref name="x"/>, <paramref name="y"/>), the floor (0075): fine motes kicked back
    /// against the way it <paramref name="faces"/>, settling, and a thin veil drifting back; matter, lit by the room.
    /// </summary>
    private void Kick(double x, double y, int faces)
    {
        var glims = Worn.Glims;
        for (int i = 0; i < Math.Round(DustRun * Fx.Count); i++)
            glims.Add(Luminous.BootDust, x + glims.Between(-1.5, 1.5), y - 0.4, faces * glims.Between(-24, 6), -glims.Between(4, 15), y,
                glims.Between(0.5, 0.85), glims.Between(0.45, 0.8));
        glims.Add(Luminous.DustVeil, x - faces, y - 1.5, -faces * glims.Between(6, 14), -glims.Between(1, 4));
    }

    /// <summary>
    /// A landing's dust at the feet, (<paramref name="x"/>, <paramref name="y"/>) the floor (0075): to either side
    /// <paramref name="grains"/> grains skittering out along the floor, slowing into motes, as many motes, two veils rolling
    /// out and curling; and a faint flat ring racing out along the floor, the only light in it, as wide as the
    /// <paramref name="fall"/> was fast (0090). A heavy landing's ring goes wider, brighter and longer, and two streaks of light
    /// race along the floor either side ahead of it.
    /// </summary>
    private void Land(double x, double y, int grains, int fall)
    {
        var glims = Worn.Glims;
        foreach (int side in (ReadOnlySpan<int>)[-1, 1])
        {
            // It spreads along the floor only as far as the floor runs open: in a shaft, hardly at all (0077).
            double room = side < 0 ? FloorRoom.Left : FloorRoom.Right, spread = Math.Min(1, room / FloorSpread);
            if (room < 2) continue;
            for (int i = 0; i < Math.Round(grains * Fx.Count); i++)
                glims.Add(Luminous.DustGrain, x + side * glims.Between(1, Math.Min(4, room)), y - 0.5, side * glims.Between(30, 80) * spread, -glims.Between(2, 16), y);
            for (int i = 0; i < Math.Round(grains * Fx.Count); i++)
                glims.Add(Luminous.LandDust, x + side * glims.Between(0, Math.Min(6, room)), y - 0.6, side * glims.Between(5, 26) * spread, -glims.Between(3, 14), y,
                    glims.Between(0.5, 0.8), glims.Between(0.45, 0.8));
            if (room < FloorSpread / 2) continue;
            for (int i = 0; i < 2; i++)
                glims.Add(Luminous.LandVeil, x + side * (2 + i * 3), y - 1.4 - i * 0.6, side * glims.Between(12, 26), -glims.Between(1, 5));
            for (int i = 0; i < HeroMotion.Landing.HeavyStreaks && Motion.Heavy(fall); i++)
                glims.Add(Luminous.LandStreak, x + side * 3, y - 0.6, side * glims.Between(90, 140), -glims.Between(0, 8));
        }
        var (to, ms, alpha) = Motion.RingOf(fall);
        double ring = Math.Min(Motion.RingFrom + (to - Motion.RingFrom) * Fx.Length, Math.Min(FloorRoom.Left, FloorRoom.Right));
        if (ring >= 6) Worn.Flares.Shock(x, y - 0.4, Motion.RingFrom, ring, ms, (236, 240, 248), alpha * Fx.Glow, 0.28, 0.16);
    }

    /// <summary>How far, in pixels, a landing's dust needs the floor open to spread its whole way.</summary>
    private const double FloorSpread = 40;

    /// <summary>
    /// How far the floor runs open from the feet to either side, in pixels of the world, till a wall: how far a landing's
    /// dust spreads along it. Set by the crew each frame; unbounded till then.
    /// </summary>
    public (double Left, double Right) FloorRoom { get; set; } = (double.MaxValue, double.MaxValue);

    /// <summary>
    /// On a ladder, how far its top is over the feet, in pixels (<see cref="Hero.LedgeAbove"/>), or in the air how far a ledge
    /// the jump steps up onto is (<see cref="Hero.MantleLedge"/>): what the hands pull the body up onto (0092). Set by the crew
    /// each frame; none away from one.
    /// </summary>
    public double? Ledge { get; set; }

    /// <summary>
    /// Where the feet are drawn instead of where the hero stands, in pixels of the world: on the screen's pixels with the
    /// camera (<see cref="CameraFollow.OnPixels"/>). Set by the crew each frame for its own hero; others' stand where they are.
    /// </summary>
    public Vector2? OnPixels { get; set; }

    /// <summary>
    /// Sparks off the gloves sliding down a ladder (0092): <paramref name="count"/> tiny hot streaks flicked up and out from
    /// either hand, at <paramref name="front"/> and <paramref name="back"/> in pixels of the world.
    /// </summary>
    private void Sparks(Vector2 front, Vector2 back, int count)
    {
        var glims = Worn.Glims;
        for (int i = 0; i < Math.Round(count * Fx.Count); i++)
        {
            var at = i % 2 == 0 ? front : back;
            glims.Add(Luminous.RailSpark, at.X + glims.Between(-0.5, 0.5), at.Y, glims.Between(-30, 30), -glims.Between(40, 110));
        }
    }

    /// <summary>
    /// Every world pixel its suit's and person's pictures cover as drawn now, the colour under it, the last drawn over the rest:
    /// what an emptied body crumbles into (2026-10-29-body-bag, <see cref="PixelBursts"/>).
    /// </summary>
    public void Pixels(Dictionary<(int X, int Y), (byte R, byte G, byte B)> into)
    {
        foreach (var (sprite, _, _) in _parts)
        {
            if (!sprite.IsVisibleInTree() || sprite.Texture is not { } texture || SuitArt.PixelsOf(texture) is not var (w, h, data)) continue;
            if (sprite.RegionEnabled)
            {
                // A shin or a boot: only its rows of the picture, the first of them at its offset.
                int from = Math.Clamp((int)Math.Round(sprite.RegionRect.Position.Y), 0, h), to = Math.Clamp((int)Math.Round(sprite.RegionRect.End.Y), from, h);
                (data, h) = (data[(from * w * 4)..(to * w * 4)], to - from);
            }
            PixelBursts.Paint(into, sprite.GlobalTransform * new Transform2D(0, sprite.Offset), w, h, data);
        }
    }

    /// <summary>
    /// The figure in <paramref name="colour"/>, 0xRRGGBB, the player's, wearing a suit's <paramref name="helmet"/> (its item, null for
    /// none) and <paramref name="body"/> (its item, null bare, 2026-10-27-suits-are-worn), its person in <paramref name="look"/> (look 0
    /// without one, 2026-10-28-looks-are-printed): its pictures recoloured, when any of it changes.
    /// </summary>
    public void Wear(int colour, string? helmet = "helmet", string? body = "suit", Look? look = null)
    {
        look ??= Look.Zero;
        if (_colour == colour && _helmetWorn == helmet && _bodyWorn == body && _look == look) return;
        (_colour, _helmetWorn, _bodyWorn, _look) = (colour, helmet, body, look);
        int skin = HeroLooks.Wrap(look.Skin, HeroLooks.Skins.Length), hair = HeroLooks.Wrap(look.HairColour, HeroLooks.Hairs.Length);
        foreach (var (sprite, part, isFar) in _parts)
        {
            sprite.Texture = SuitArt.Of(PictureOf(part), colour, skin, hair, isFar);
            sprite.Offset = _joints[sprite] - (sprite.Texture is CanvasTexture ? new Vector2(HiRes.FigureMargin, HiRes.FigureMargin) : Vector2.Zero);
            if (part == "shin") CutAtTheAnkle(sprite, _soles.Contains(sprite));
        }
        (_headLit.Offset, _faceLit.Offset) = (_headPart.Offset, _facePart.Offset);
        _pack.Visible = body != null;
        foreach (var (light, part, name, _, glow) in _bands)
            Show(light, glow, body is { } b ? $"hero_{b}_{name}_lit" : null, part.Offset);
        Show(_arcs, _arcsGlow, body is { } a ? $"hero_{a}_arcs" : null, _chest.Offset);
        Show(_pip, _pipGlow, body is { } p ? $"hero_{p}_pip" : null, _chest.Offset);
        _shell.Visible = _visor.Visible = helmet != null;
        Show(_eyes, _eyesGlow, helmet != null ? $"hero_{helmet}_lit" : null, _shell.Offset);
        _headLamp.Offset = HeadLampArt.OnOf(helmet) - Joints["helmet"];
        _lens.Position = _headLamp.Offset + HeadLampArt.Lens;
        _hair.Visible = helmet == null;
        _beard.Visible = helmet == null && HeroLooks.Wrap(look.Beard, HeroLooks.Beards + 1) > 0;
        (_headLit.Texture, _faceLit.Texture) = (_headPart.Texture, _facePart.Texture);
        HeadLamp = _lampWorn;
        ShowScanner();
    }

    /// <summary>
    /// A shin's picture cut at the ankle (2026-11-04-hero-gait): the shin above it, or the <paramref name="boot"/> below it, from
    /// <see cref="BootOverlap"/> over it, placed from the ankle it turns at.
    /// </summary>
    private static void CutAtTheAnkle(Sprite2D sprite, bool boot)
    {
        var size = sprite.Texture.GetSize();
        float ankle = -sprite.Offset.Y + (float)Rig.ToAnkle.Lower;
        (sprite.RegionEnabled, sprite.RegionFilterClipEnabled) = (true, true);
        if (!boot)
        {
            sprite.RegionRect = new Rect2(0, 0, size.X, ankle);
            return;
        }
        float top = ankle - BootOverlap;
        sprite.RegionRect = new Rect2(0, top, size.X, size.Y - top);
        sprite.Offset = new Vector2(sprite.Offset.X, -BootOverlap);
    }

    /// <summary>The picture a part is drawn with: bare, the look's clothes in place of the suit; the look's face, hair and beard.</summary>
    private string PictureOf(string part) => part switch
    {
        "torso" or "upperArm" or "forearm" or "thigh" or "shin" when _bodyWorn == null => $"outfit_{HeroLooks.Wrap(_look.Outfit, HeroLooks.Outfits)}_{part}",
        // A later body's own pictures, as a later helmet's (2026-11-03-the-regen-suit).
        "torso" or "pack" or "upperArm" or "forearm" or "thigh" or "shin" when _bodyWorn is { } body && body != Stillwake.Content.Suit.Basic => $"{body}_{part}",
        "face" => $"face_{HeroLooks.Wrap(_look.Face, HeroLooks.Faces)}",
        "hair" => $"hair_{HeroLooks.Wrap(_look.Hair, HeroLooks.HairStyles)}",
        "beard" => $"beard_{Math.Max(HeroLooks.Wrap(_look.Beard, HeroLooks.Beards + 1) - 1, 0)}",
        // A later helmet's own pictures (2026-11-03-the-heavy-helmet); the basic one's keep their first names.
        "shell" or "visor" when _helmetWorn is { } worn && worn != Stillwake.Content.Helmet.Basic => $"{worn}_{part}",
        "shell" => "helmet_shell",
        _ => part,
    };

    /// <summary>
    /// A part of the pixel-art hero, its joint (its own or <paramref name="joint"/>'s) at the origin, lit where it stands; with the
    /// suit's band of light on it unless not <paramref name="banded"/>.
    /// </summary>
    private Sprite2D Part(string name, bool far = false, string? joint = null, bool banded = true)
    {
        var sprite = new Sprite2D { Centered = false, Offset = -Joints[joint ?? name] };
        Lit.Set(sprite);
        _parts.Add((sprite, name, far));
        _joints[sprite] = sprite.Offset;
        if (banded && SuitGlow.UpOf(name) != null)
        {
            var light = new Sprite2D { Centered = false, Material = Unshaded, Visible = false };
            sprite.AddChild(light);
            _bands.Add((light, sprite, name, far, Gives(light, new Glow { Reach = L.SuitReach, Keep = L.SuitKeep })));
        }
        return sprite;
    }

    /// <summary>A light of the hero's own (2026-11-03-hero-lights): a glow on <paramref name="light"/>, lighting round it while it shows.</summary>
    private Glow Gives(Sprite2D light, Glow glow)
    {
        glow.Visible = _givesLight;
        light.AddChild(glow);
        _glows.Add(glow);
        return glow;
    }

    /// <summary>A body's light layer drawn with picture <paramref name="name"/>, none without, at <paramref name="at"/>; its glow at the picture's middle.</summary>
    private static void Show(Sprite2D light, Glow glow, string? name, Vector2 at)
    {
        (light.Texture, light.Offset) = (name != null ? LightOf(name) : null, at);
        glow.Position = at + (light.Texture != null ? HiRes.MiddleOf(name!) : Vector2.Zero);
    }

    /// <summary>A body's light picture, loaded once; none for a body without it.</summary>
    private static Texture2D? LightOf(string name)
    {
        if (!Lights.TryGetValue(name, out var light))
            Lights[name] = light = HiRes.Has(name) ? HiRes.Texture(HiRes.Load(name)!) : null;
        return light;
    }

    /// <summary>The tracker's light in the player's colour as its <see cref="Signal"/> blinks it, its glow by the same level.</summary>
    private void ShowTracker()
    {
        _tracker.Visible = _trackerLight.Visible = Tracked;
        if (_colour is not int worn || !Tracked)
        {
            _trackerGlow.Colour = default;
            return;
        }
        var colour = new Color((uint)(worn << 8 | 0xff));
        double level = TrackerBlink.Of(Signal, _timeMs);
        var (r, g, b, a) = TrackerBlink.Lit((colour.R, colour.G, colour.B), level);
        _trackerLight.Modulate = new Color((float)r, (float)g, (float)b, (float)a);
        _trackerGlow.Colour = colour * (L.Tracker * (float)level);
    }

    /// <summary>A switched body's lights as its suit is now (<see cref="Suit"/>): the wave, the arcs and the empty belt's pip.</summary>
    private void ShowSuit()
    {
        _eyes.Visible = false;
        _eyesGlow.Colour = default;
        if (GearLights is { } gear)
        {
            ShowGear(gear);
            return;
        }
        var (on, charged, phase) = Suit;
        bool glows = on && charged;
        foreach (var (light, _, name, far, glow) in _bands)
        {
            light.Visible = glows && light.Texture != null;
            if (!light.Visible) continue;
            float strength = (float)(SuitGlow.Bands(phase, SuitGlow.UpOf(name)!.Value) * (far ? SuitArt.FarShade : 1));
            light.Modulate = new Color(1, 1, 1, strength);
            glow.Colour = SuitLight * (L.SuitBand * strength);
        }
        _arcs.Visible = glows && _arcs.Texture != null;
        _arcs.Modulate = new Color(1, 1, 1, (float)SuitGlow.Arcs(_timeMs));
        _arcsGlow.Colour = SuitLight * (L.SuitArcs * _arcs.Modulate.A);
        _pip.Visible = on && !charged && _pip.Texture != null && SuitGlow.Pip(_timeMs);
    }

    /// <summary>A catcher's receiver, on near limbs only, and its goggles, lit as <see cref="GearLights"/> says; its arcs and pip never.</summary>
    private void ShowGear((double Receiver, double Eyes, int EyesColour) gear)
    {
        var colour = new Color((uint)((_colour ?? 0xffffff) << 8 | 0xff));
        var (r, g, b, a) = TrackerBlink.Lit((colour.R, colour.G, colour.B), gear.Receiver);
        foreach (var (light, _, _, far, glow) in _bands)
        {
            light.Visible = !far && light.Texture != null && gear.Receiver > 0;
            light.Modulate = new Color((float)r, (float)g, (float)b, (float)a);
            glow.Colour = light.Visible ? colour * (L.Tracker * (float)gear.Receiver) : default;
        }
        _arcs.Visible = _pip.Visible = false;
        _arcsGlow.Colour = default;
        var eyes = new Color((uint)(gear.EyesColour << 8 | 0xff));
        _eyes.Visible = _eyes.Texture != null && gear.Eyes > 0;
        _eyes.Modulate = eyes with { A = (float)Math.Min(1, gear.Eyes) };
        _eyesGlow.Colour = _eyes.Visible ? eyes * (L.Tracker * (float)gear.Eyes) : default;
    }

    /// <summary>
    /// A limb hanging from its joint in two pictures, the upper turned at the joint and the lower at the
    /// elbow or knee; the upper over the lower, so the joint between them is the upper's rounded end.
    /// </summary>
    private Limb MakeLimb(Segments size, string upper, string lower, bool far, out Node2D view)
    {
        view = new Node2D();
        var bend = new Node2D { Position = new Vector2(0, (float)size.Upper) };
        Node2D? ankle = null;
        if (lower == "shin")
        {
            // The boot under the shin, turning at the ankle on its own (2026-11-04-hero-gait).
            ankle = new Node2D { Position = new Vector2(0, (float)Rig.ToAnkle.Lower) };
            var boot = Part(lower, far, banded: false);
            _soles.Add(boot);
            ankle.AddChild(boot);
            bend.AddChild(ankle);
        }
        bend.AddChild(Part(lower, far));
        if (lower == "shin" && !far)
        {
            Lit.Set(_tracker);
            _tracker.Texture = _trackerStrapPicture ??= HiRes.Texture(HiRes.Load("hero_tracker")!);
            _trackerLight.Texture = _trackerLightPicture ??= HiRes.Texture(HiRes.Load("hero_tracker_light")!);
            bend.AddChild(_tracker);
            bend.AddChild(_trackerLight);
            Gives(_trackerLight, _trackerGlow).Position = TrackerAt + HiRes.MiddleOf("hero_tracker_light");
        }
        view.AddChild(bend);
        view.AddChild(Part(upper, far));
        return new Limb(view, bend, ankle);
    }

    /// <summary>
    /// The gait eased toward what <paramref name="hero"/>'s speed says (2026-11-04-hero-gait), the legs' cycle on by
    /// <paramref name="moved"/> cells at its stride. Standing, it keeps the gait it stopped in as the stride settles; setting off
    /// from still, the stride starts afresh. Off the floor it is what the speed says, so a landing runs on at once.
    /// </summary>
    private void EaseGait(Hero hero, Pose pose, double moved, double elapsedMs)
    {
        if (pose is not (Pose.Stand or Pose.Walk or Pose.Run))
        {
            int speed = Math.Abs(hero.Vx);
            (_cycle, _gait, _moving) = (0, speed > Stillwake.Content.Hero.Walk ? (Poses.Backward(hero) ? -1 : 1) : 0, speed > 0 ? 1 : 0);
            return;
        }
        double gait = pose switch { Pose.Run => Poses.Backward(hero) ? -1 : 1, Pose.Walk => 0, _ => _gait };
        double moving = pose == Pose.Stand ? 0 : 1;
        if (_moving <= 0 && moving > 0) (_cycle, _step) = (0, null);
        _cycle += moved / Poses.StrideOf(_gait);
        _gait = Toward(_gait, gait, elapsedMs / HeroMotion.Gait.RunMs);
        _moving = Toward(_moving, moving, elapsedMs / (moving > _moving ? HeroMotion.Gait.StartMs : HeroMotion.Gait.StopMs));
    }

    /// <summary><paramref name="from"/> moved toward <paramref name="to"/> by at most <paramref name="by"/>.</summary>
    private static double Toward(double from, double to, double by) => from + Math.Clamp(to - from, -by, by);

    /// <summary><paramref name="from"/> moved toward <paramref name="to"/> over <paramref name="elapsedMs"/>, settling in about <paramref name="within"/> ms.</summary>
    private static double Settle(double from, double to, double elapsedMs, double within = SettleMs) =>
        from + (to - from) * Math.Min(1, elapsedMs / within);

    /// <summary>A limb settled the short way round, so arms whirling in space never unwind.</summary>
    private static Bend SettleBend(Bend from, Bend to, double elapsedMs, double within) =>
        new(SettleAngle(from.Upper, to.Upper, elapsedMs, within), SettleAngle(from.Lower, to.Lower, elapsedMs, within));

    /// <summary>A limb <paramref name="share"/> of the way from <paramref name="from"/> to <paramref name="to"/>, the short way round.</summary>
    private static Bend Mix(Bend from, Bend to, double share) =>
        new(from.Upper + Poses.Apart(from.Upper, to.Upper) * share, from.Lower + Poses.Apart(from.Lower, to.Lower) * share);

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
        if (_scanner.Visible)
        {
            int lit = (int)(_timeMs / ScannerDotMs) % (_scannerDots.Length + 1);
            _scannerDot.Visible = lit < _scannerDots.Length;
            if (_scannerDot.Visible) _scannerDot.Texture = _scannerDots[lit];
        }
        ShowTracker();
        ShowSuit();
        // Walking backwards, away from where it faces, the stride runs backwards too. On the ground only the
        // way along counts, so a stair keeps the stride of the flat; on a ladder only the way up or down.
        double moved = _last is var (lx, ly) ? (pose == Pose.Climb ? Math.Abs(hero.Y - ly) : (double)(hero.X - lx) * hero.Facing) / Hero.Unit : 0;
        _travelled += moved;
        _last = (hero.X, hero.Y);
        EaseGait(hero, pose, moved, elapsedMs);
        // A new pose starts its stride afresh, its first foot down a quarter stride in; from walking to running or back
        // the legs go on, or a speed flickering about the line between them would freeze them.
        bool striding = pose is Pose.Walk or Pose.Run, strode = _wasPose is Pose.Walk or Pose.Run;
        if (pose != _wasPose && !(striding && strode)) (_strideFrom, _step) = (_travelled, null);
        if (pose is Pose.Lie or Pose.Tumble && pose != _wasPose) (_outMs, _slump, _outTilt) = (0, Random.Shared.Next(), _shown?.Tilt ?? 0);
        _outMs += elapsedMs;
        // Where the feet are, in pixels of the world.
        double feetX = (double)hero.X / Hero.Unit * Rig.Cell, feetY = (double)hero.Y / Hero.Unit * Rig.Cell;
        bool walking = pose is Pose.Stand or Pose.Walk or Pose.Run, walked = _wasPose is Pose.Stand or Pose.Walk or Pose.Run;
        double lift = _stepLift = _steps.Offset(feetX, feetY, walking && walked, elapsedMs);
        var cues = _motion.Step(hero, pose, elapsedMs);
        if (cues.Jumped && !InWater) Boots(GroundSounds.JumpOf(Footing));
        var water = _splashes.Step(InWater, hero.Vx != 0 || hero.Vy < 0, elapsedMs / 1000);
        if (water.In) Boots(S.SplashIn);
        if (water.Out) Boots(S.SplashOut);
        if (water.Stroke) Boots(S.SwimStroke);
        if (cues.Landed is int fall && !InWater)
        {
            var (landing, louder, tune) = GroundSounds.LandOf(Footing, fall);
            Boots(landing, tune, louder);
            int puff = Math.Min(DustLandMost, Math.Max(DustLandLeast, (int)Math.Round(fall * DustLandPerSpeed, MidpointRounding.AwayFromZero)));
            Land(feetX, feetY, puff, fall);
        }
        // A foot comes down at the end of each half stride, a quarter stride in: heard walking and running,
        // kicking up dust only running.
        int? stepping = pose is Pose.Walk or Pose.Run ? (int)Math.Floor((_cycle - 0.25) / 0.5) : null;
        if (stepping != null && _step != null && stepping != _step && !InWater)
        {
            Boots(GroundSounds.StepOf(Footing));
            if (pose == Pose.Run) Kick(feetX + hero.Facing * Rig.W * 0.3, feetY, hero.Facing);
        }
        _step = stepping;
        _wasPose = pose;
        // Working a plan the screwdriver takes the front hand; the planner waits till it is done.
        bool working = _workAt != null && Poses.HoldsIn(pose);
        _workMs = working ? _workMs + elapsedMs : 0;
        bool holding = holdsPlan && Poses.HoldsIn(pose) && !working;
        var item = HeldLook.Of(Wields);
        bool inHand = item != null && Poses.HoldsIn(pose) && !working && !holding;
        // Struck with by hand: the axe, the sledgehammer swung, the spear thrust (Strokes).
        var stroke = Strokes.Of(Wields);
        bool chopping = _chop != null && stroke != null && item != null && inHand;
        bool aiming = Aim != null && item?.Aimed == true && inHand, drill = HeldLook.IsDrill(Wields);
        // The hands at work, whatever the pose (0030): afloat the paddling stops, the hero drifts calmly
        // upright, and the front arm reaches where the work is, in the body's own frame.
        bool acting = working || chopping || holding && _beamMs > 0;
        bool afloat = pose == Pose.Float;
        _swim = afloat ? Settle(_swim, Poses.Swimming(hero) && !acting ? 1 : 0, elapsedMs, SwimBlendMs) : 0;
        _fast = afloat ? Settle(_fast, Poses.SwimmingFast(hero) && !acting ? 1 : 0, elapsedMs, SwimBlendMs) : 0;
        var to = Poses.LimbsOf(pose, hero, new Moment(_travelled - _strideFrom, _timeMs, _motion.Landing, holding ? _head : null,
            _outMs, _slump, _outTilt, _swim, _fast, item != null, _motion.Now, pose is Pose.Climb or Pose.Air ? Ledge : null,
            walking ? _cycle : null, walking ? _gait : null, walking ? _moving : null));
        _flinchMs += elapsedMs;
        to = Poses.Flinched(to, Poses.FlinchOf(_flinchMs), _flinchFrom * hero.Facing, pose);
        var from = _shown ?? to;
        double within = pose switch { Pose.Float => FloatSettleMs, Pose.Tumble => SlackMs, _ => SettleMs };
        var shown = new Limbs(
            SettleBend(from.FrontArm, to.FrontArm, elapsedMs, within),
            SettleBend(from.BackArm, to.BackArm, elapsedMs, within),
            SettleBend(from.FrontLeg, to.FrontLeg, elapsedMs, within),
            SettleBend(from.BackLeg, to.BackLeg, elapsedMs, within),
            SettleAngle(from.Tilt, to.Tilt, elapsedMs, afloat ? LeanMs : pose is Pose.Lie or Pose.Tumble ? SettleMs : HeroMotion.Lean.SettleMs),
            Settle(from.Lift, to.Lift, elapsedMs),
            to.Lying,
            SettleAngle(from.FrontFoot, to.FrontFoot, elapsedMs, within),
            SettleAngle(from.BackFoot, to.BackFoot, elapsedMs, within));
        // On the floor no boot goes through it as the limbs settle (2026-11-04-hero-gait).
        if (walking) shown = Poses.Kept(shown);
        _shown = shown;
        // The body placed first: what the hands reach for is found in its own frame, turned and lifted.
        _body.Rotation = (float)shown.Tilt;
        double c = Math.Abs(Math.Cos(shown.Tilt)), s = Math.Abs(Math.Sin(shown.Tilt));
        // Lying down, the middle sinks as the body turns, so it comes to rest on the floor.
        _body.Position = new Vector2(0, (float)((shown.Lying ? -(Rig.H / 2) * c - Back * s : -Rig.H / 2) - shown.Lift));
        Position = (OnPixels ?? new Vector2((float)feetX, (float)feetY)) + new Vector2(0, (float)lift);
        Scale = new Vector2(hero.Facing, 1);
        // A combo's step puts the body into the strike (axe-combos): the hip, the legs and the lean, eased in and out.
        BodyLayer? wanted = null;
        if (chopping && _chop is { Phase: var bodyPhase, Play: { Body: not null } bodyPlay } && ComboBody.At(bodyPlay, bodyPhase * bodyPlay.Ms) is { } state)
            wanted = ComboBody.Over(shown, state, pose, walking ? _moving : 1, RoomAhead);
        var body = _bodyBlend.Next(wanted, hero.Facing, pose is Pose.Lie or Pose.Tumble, elapsedMs);
        _waist.Transform = _waistBehind.Transform = WaistOf(body);
        if (_capeOutside.Visible)
        {
            _cape.Step(
                elapsedMs,
                p => { var g = _waist.ToGlobal(new Vector2((float)p.X, (float)p.Y)); return (g.X, g.Y); },
                pose is Pose.Float or Pose.Tumble);
            _capeInside.Refresh();
            _capeOutside.Refresh();
        }
        // Placing plans, the planner thrusts at them: out fast, back slower.
        double t = 1 - _beamMs / BeamMs;
        double thrust = holding && _beamMs > 0 ? (t < 0.3 ? t / 0.3 : (1 - t) / 0.7) : 0;
        var frontArm = shown.FrontArm;
        double aim = 0;
        if (thrust > 0 && _beamTo.Count > 0)
        {
            // Toward the first cell, from the shoulder.
            var (dx, dy) = FromShoulder(_beamTo[0]);
            aim = Math.Atan2(dy, dx);
            double reach = 0.95 * (Rig.Arm.Upper + Rig.Arm.Lower);
            var held = Rig.EndOf(shown.FrontArm, Rig.Arm);
            var point = new Vec(Math.Cos(aim) * reach, Math.Sin(aim) * reach);
            frontArm = Rig.Reach(new Vec(held.X + (point.X - held.X) * thrust, held.Y + (point.Y - held.Y) * thrust), Rig.Arm, 1);
        }
        double work = 0, twist = 0;
        // A builder at work, the screwdriver if the hand holds none (a friend's, a frame late): its motor turns it, else the wrist.
        var builder = item?.Twist != null ? item : HeldLook.Screwdriver;
        _spin = Spinning ? Math.Min(1, _spin + elapsedMs / HeldLook.SpinUpMs) : Math.Max(0, _spin - elapsedMs / HeldLook.SpinDownMs);
        bool driven = working && Spinning && builder.Driven != null;
        if (working && _workAt is { } plan)
        {
            // Toward the plan, from the shoulder.
            var (dx, dy) = FromShoulder(plan);
            work = Math.Atan2(dy, dx);
            double cycle = _workMs % TwistMs / TwistMs;
            twist = Math.Min(1, cycle / TurnShare);
            // Rising as the wrist turns over, sinking as it turns back to grip again; held still while the motor turns it.
            double rise = driven ? 0 : cycle < TurnShare ? Math.Sin(twist * Math.PI / 2) : Math.Cos((cycle - TurnShare) / (1 - TurnShare) * Math.PI / 2);
            double reach = WorkReach * (Rig.Arm.Upper + Rig.Arm.Lower) + TwistPush * rise;
            // Up, square to the tool: the side the knuckles come over.
            double ux = Math.Sin(work), uy = -Math.Cos(work);
            frontArm = Rig.Reach(new Vec(Math.Cos(work) * reach + ux * TwistLift * rise, Math.Sin(work) * reach + uy * TwistLift * rise), Rig.Arm, 1);
        }
        (double Angle, double Phase)? swung = null;
        double elbow = item?.Elbow ?? 0, wrist = 0;
        if (chopping && _chop is { Aim: var aimed, Phase: var phase, Play: var play })
        {
            // Toward its aim from where the hero is now, from the shoulder: the motion chosen at the press, played along it till
            // the blade, not the hand, crosses the line to it (0040, swing-motions).
            if (phase < _lastPhase) _bit = false;
            _lastPhase = phase;
            double drawn = _bit ? AxeSwing.Shown(phase, play) : phase;
            (elbow, wrist) = (play.ElbowAt(drawn), play.WristAt(drawn));
            // The spear: aimed as a gun is, the shaft's line through the point, the elbow cocked and straightened (the jab).
            if (play.Motion.Kind == MotionKind.Thrust)
            {
                var (dx, dy) = FromShoulder(aimed, hero.Facing);
                swung = (HarpoonShot.ArmFor(item! with { Elbow = elbow }, dx, dy), drawn);
            }
            else swung = (play.AngleAt(AimAngleOf(aimed, hero.Facing), drawn), drawn);
        }
        else if (aiming && Aim is { } target && item != null)
        {
            // The gun: the arm straight, turned so the barrel's line, not the arm's, runs through the point; eased in as a swing is.
            var (dx, dy) = FromShoulder(target);
            swung = (HarpoonShot.ArmFor(item, dx, dy), 0);
        }
        // An aimed item comes up and goes down quicker than a swing winds up (aim-on-use).
        frontArm = _swingArm.Next(frontArm, swung, elapsedMs, elbow, aiming || _wasAiming ? ArmUp.RaiseMs : null);
        Aimed = aiming && _swingArm.Settled;
        _wasAiming = aiming;
        // Running with an item in one hand it trails behind (HeroMotion.Carry), and the arm swings on a spring: it lags a
        // start and swings on past a stop. Aimed, swung or at work, it goes where it is put at once.
        bool carried = inHand && swung == null && !working && !aiming;
        _carry = Math.Clamp(_carry + (carried && pose == Pose.Run && !Poses.Backward(hero) ? 1 : -1) * elapsedMs / HeroMotion.Carry.BlendMs, 0, 1);
        double carry = _carry * _carry * (3 - 2 * _carry);
        // An aimed item hanging lies across the fist as a stick does (HeldLook.Rest); along the forearm again as the arm comes up.
        bool resting = inHand && item is { Rest: > 0 } && swung == null && !aiming;
        _rest = resting ? Math.Min(1, _rest + elapsedMs / RestMs) : Math.Max(0, _rest - elapsedMs / ArmUp.RaiseMs);
        _sinceCarryMs = _carry > 0 ? 0 : _sinceCarryMs + elapsedMs;
        if (carried && item != null)
        {
            double bob = HeroMotion.Carry.Bob * Math.Sin(_cycle * 4 * Math.PI);
            frontArm = Mix(frontArm, Rig.Carried(bob), carry);
        }
        // The spring only round a run and a while after it: a landing or a pose's own swing moves the arm as it is.
        if (carried && _sinceCarryMs < HeroMotion.Carry.SpringAfterMs) frontArm = frontArm with { Upper = _armSpring.Follow(frontArm.Upper, elapsedMs) };
        else _armSpring.Hold(frontArm.Upper);
        _swingArm.Drew(frontArm);
        // The far arm keeps its turn in the body's frame as the upper body leans; gripping, it closes on the haft.
        var backArm = new Bend(shown.BackArm.Upper + body.FarArm.Upper + body.Lean, shown.BackArm.Lower + body.FarArm.Lower);
        if (body.Grip > 0 && inHand && item != null) backArm = Mix(backArm, HaftGrip.ArmFor(frontArm, item, wrist).Arm, Math.Min(1, body.Grip));
        _frontArm.Bend(frontArm);
        _backArm.Bend(backArm);
        _frontHip.Position = new Vector2((float)(Rig.HipApart + body.HipX), (float)(-Rig.Hip - Rig.HipRise(Rig.HipApart, shown.Tilt) + body.HipY));
        _backHip.Position = new Vector2((float)(-Rig.HipApart + body.HipX), (float)(-Rig.Hip - Rig.HipRise(-Rig.HipApart, shown.Tilt) + body.HipY));
        _frontLeg.Bend(new Bend(shown.FrontLeg.Upper + body.FrontLeg.Upper, shown.FrontLeg.Lower + body.FrontLeg.Lower), shown.FrontFoot + body.FrontFoot);
        _backLeg.Bend(new Bend(shown.BackLeg.Upper + body.BackLeg.Upper, shown.BackLeg.Lower + body.BackLeg.Lower), shown.BackFoot + body.BackFoot);
        if (cues.Sparks > 0)
        {
            Vector2 Glove(Bend arm) => _waist.ToGlobal(Pixel(Rig.FistOf(arm)));
            Sparks(Glove(frontArm), Glove(backArm), cues.Sparks);
        }
        bool awake = pose is not (Pose.Lie or Pose.Tumble);
        _head = Settle(_head, awake ? look : 0, elapsedMs, HeadSettleMs);
        // On the floor the head takes back some of the body's lean, so a run looks ahead, not at the floor.
        // In a combo's strike it keeps the eyes up as the body leans and follows the blade a little.
        double follow = swung is var (swungAngle, _) && _chop is { Play: var headPlay } ? Math.Clamp(swungAngle - headPlay.Lead, Combos.HeadFollowUp, Combos.HeadFollowDown) : 0;
        _helmet.Rotation = (float)(_head - (pose is Pose.Stand or Pose.Walk or Pose.Run ? shown.Tilt * HeroMotion.Run.HeadSteady : 0)
            - Combos.HeadSteady * body.Lean + Combos.HeadFollow * body.Follow * follow);
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
            float glow = GlowStrength * (float)(1 + 1.5 * thrust);
            _glow.Colour = new Color(GlowColour.R * glow, GlowColour.G * glow, GlowColour.B * glow);
        }
        _tool.Visible = working;
        if (working)
        {
            var hand = Rig.EndOf(frontArm, Rig.Arm);
            _tool.Position = new Vector2((float)hand.X, (float)(-Rig.Shoulder + hand.Y));
            _tool.Rotation = (float)work;
            (_tool.Texture, _tool.Offset) = (HeldArt.Of(driven ? builder.Driven! : builder.Twist!, []), -new Vector2((float)builder.Grip.X, (float)builder.Grip.Y));
            // Turned by its motor, its frames in turn as a drill's spin; by the wrist, three steps of the ribs a turn, held on the way back:
            // they roll on one way only.
            _spinFrame = driven ? (_spinFrame + _spin * HeldLook.SpinFps * elapsedMs / 1000) % 3 : 0;
            _tool.Frame = driven ? (int)_spinFrame : (int)Math.Min(2, Math.Floor(twist * 3));
        }
        // The item in the front fist, held by its grip at its angle to the forearm (0040): swung, aimed or
        // hanging, the hand closed round its haft or stock.
        _held.Visible = inHand;
        Muzzle = EdgeStart = null;
        if (inHand && item != null)
        {
            var fist = Rig.FistOf(frontArm);
            // Through the picture and its spin frames in turn; still, it stops on the picture.
            int frames = 1 + (item.Spin?.Count ?? 0);
            _spinFrame = _spin > 0 ? (_spinFrame + _spin * HeldLook.SpinFps * elapsedMs / 1000) % frames : 0;
            int frame = item.Spin is [_] ? (_spin > 0 ? 1 : 0) : (int)_spinFrame;
            var parts = PartsOf(item);
            _held.Texture = item.Loaded != null && Loaded ? HeldArt.Of(item.Loaded, []) : HeldArt.Of(frame > 0 ? item.Spin![frame - 1] : item.Picture, parts.Layers);
            bool lit = Spinning && item.Lamp != null;
            _lamp.Visible = _lampGlow.Visible = lit;
            // Held by its grip in the fist.
            var origin = item.Grip;
            // The frames draw the lamp dim; lit, its pixel is drawn over them.
            if (item.Lamp is { } at)
            {
                var lamp = new Vector2((float)(at.X - origin.X), (float)(at.Y - origin.Y));
                (_lamp.Position, _lampGlow.Position) = (lamp - Vector2.One / 2, lamp);
            }
            _held.Offset = -new Vector2((float)origin.X, (float)origin.Y);
            _charged.Visible = item.Charged != null && Charge > 0;
            if (_charged.Visible)
            {
                (_charged.Texture, _charged.Offset) = (HeldArt.Of(item.Charged!, []), _held.Offset);
                _charged.Modulate = new Color(1, 1, 1, (float)Charge);
            }
            _held.Position = new Vector2((float)fist.X, (float)(-Rig.Shoulder + fist.Y));
            // Carried trailing on a run, the hand turns the item along itself (Rig.Carried).
            _held.Rotation = (float)(Rig.HeldTurn(frontArm, item, wrist) + item.Hold * carry - item.Rest * _rest * (1 - carry));
            if (item.Aimed)
            {
                Vector2 At((double X, double Y) p) => _held.ToGlobal(new Vector2((float)p.X, (float)p.Y) + _held.Offset);
                Muzzle = At(item.EdgeTo);
                EdgeStart = At(item.EdgeFrom);
                Barrel = (Muzzle.Value - EdgeStart.Value).Normalized();
                if (_charged.Visible)
                {
                    // Lit from the gap between the rails, as it is held this frame.
                    var gap = At(((item.EdgeFrom.X + item.EdgeTo.X) / 2, (item.EdgeFrom.Y + item.EdgeTo.Y) / 2));
                    Held.Flares.Hold(_charged, gap.X, gap.Y, ChargeLight, 0.15 + 0.35 * Charge, 3, 0.85f, 0.2);
                }
            }
        }
        // The air the tool works shows its reach while it is used (tool-reach): a stroke's cut, a running drill's or grinder's zone.
        if (chopping && _chop is { Aim: var cutAim, Phase: var cutPhase, Play: var cutPlay } && cutPlay.HitShare != null
            && Stillwake.Content.Weapons.Of(Wields) is { } weapon)
        {
            bool jab = cutPlay.Motion.Kind == MotionKind.Thrust;
            double half = Strokes.HalfOf(weapon);
            double start = jab ? stroke!.HitShare : cutPlay.ShareAt(-half);
            if (_cutPhase < start && cutPhase >= start)
            {
                var edge = Rig.HeldAt(frontArm, item!, item!.EdgeTo, wrist);
                double reach = Math.Sqrt(edge.X * edge.X + edge.Y * edge.Y);
                double sweep = jab ? WorkLooks.ThrustSweepMs : (cutPlay.ShareAt(half) - start) * cutPlay.Ms;
                _air.Strike(jab, AimAngleOf(cutAim, hero.Facing), half, weapon.Range * Rig.Cell / Hero.Unit, jab ? reach : reach - 4, sweep,
                    WorkLooks.CutFadeOf(Wields));
            }
            _cutPhase = cutPhase;
        }
        else _cutPhase = 0;
        // A combo's blade leaves a pale trail on the fast part of each cut, behind the figure (axe-combos).
        _trail.Show(inHand && item != null && chopping && _chop is { Aim: var trailAim, Phase: var trailPhase, Play: { Step: not null } trailPlay }
            ? TrailOf(trailPlay, trailPhase * trailPlay.Ms, AimAngleOf(trailAim, hero.Facing), item, shown, pose, walking ? _moving : 1) : null);
        _air.Follow(Spinning && inHand && item != null ? ZoneOf(frontArm, item) : null, elapsedMs);
        _beamMs = Math.Max(0, _beamMs - elapsedMs);
        ShowBeams(holding);
        // A drill crackles as it spins; a gun held to charge, as it fills.
        bool charging = item?.Charged != null && Charge > 0;
        var crackling = inHand && item != null && (drill || charging) ? PartsOf(item) : null;
        // Its stage made only once a drill runs or a gun charges in this hand: a body, a seated hero or the title's figures never need one.
        if (crackling != null && _drillLight == null && IsInsideTree()) _drillLight = new DrillLight(Held.Glims, Held.Flares, Held.Bolts);
        _drillLight?.Step(crackling, drill ? _spin : Charge, _heldAt ??= HeldAt, elapsedMs);
    }

    /// <summary>
    /// The zone a running drill or grinder in <paramref name="arm"/> works, in the arms' frame (tool-reach): the drill's bit point
    /// grown into a cone out to its range from the shoulder along the aim, the grinder's forward half of a disc round its hub; none for another tool.
    /// </summary>
    private AirCuts.Zone? ZoneOf(Bend arm, HeldLook item)
    {
        bool grinder = Wields == Stillwake.Content.Weapons.Grinder.Item;
        if (!grinder && Wields != Stillwake.Content.Weapons.Drill.Item) return null;
        var at = Rig.HeldAt(arm, item, grinder ? item.EdgeFrom : item.EdgeTo);
        var (ax, ay) = Aim is { } target ? FromShoulder(target) : (at.X, at.Y);
        double l = Math.Sqrt(ax * ax + ay * ay);
        var (dx, dy) = l > 0 ? (ax / l, ay / l) : (1.0, 0.0);
        if (grinder) return new(true, (at.X, at.Y), (dx, dy), WorkLooks.DiscRadius);
        double range = Stillwake.Content.Weapons.Drill.Range * Rig.Cell / Hero.Unit, cx = dx * range - at.X, cy = dy * range - at.Y;
        double length = Math.Sqrt(cx * cx + cy * cy);
        return length < 1 ? null : new(false, (at.X, at.Y), (cx / length, cy / length), length);
    }

    /// <summary>
    /// The trail of <paramref name="play"/>'s blade <paramref name="nowMs"/> into it, aimed <paramref name="aim"/>, in the body's frame:
    /// each past edge where the arm and the body then put it, the body unblended.
    /// </summary>
    private BladeTrail TrailOf(MotionPlay play, double nowMs, double aim, HeldLook item, Limbs shown, Pose pose, double moving)
    {
        double DrawnAt(double ms) => _bit ? AxeSwing.Shown(ms / play.Ms, play) : ms / play.Ms;
        (Vec, Vec) EdgeAt(double ms)
        {
            double drawn = DrawnAt(ms), wrist = play.WristAt(drawn);
            var arm = AxeSwing.ArmAt(play.AngleAt(aim, drawn), play.ElbowAt(drawn));
            var waist = ComboBody.At(play, ms) is { } state && ComboBody.Over(shown, state, pose, moving, RoomAhead) is { } layer ? WaistOf(layer) : Transform2D.Identity;
            Vec At((double X, double Y) p)
            {
                var v = waist * Pixel(Rig.HeldAt(arm, item, p, wrist));
                return new(v.X, v.Y);
            }
            return (At(item.EdgeFrom), At(item.EdgeTo));
        }
        bool Cutting(double ms) => DrawnAt(ms) is var d && d >= play.Shares.Back && d <= play.Shares.Through;
        return BladeTrail.Of(EdgeAt, Cutting, nowMs);
    }

    /// <summary>The upper body's place in the body's frame under <paramref name="layer"/> (<see cref="ComboBody.WaistOf"/>).</summary>
    private static Transform2D WaistOf(BodyLayer layer)
    {
        var (at, turn) = ComboBody.WaistOf(layer);
        return new Transform2D((float)turn, new Vector2((float)at.X, (float)at.Y));
    }

    /// <summary>A point from the front shoulder, in the arms' own frame, in the upper body's (`_waist`).</summary>
    private static Vector2 Pixel(Vec fromShoulder) => new((float)fromShoulder.X, (float)(fromShoulder.Y - Rig.Shoulder));

    /// <summary>A point of the held item's picture in the world, as it is drawn now.</summary>
    private Vector2 HeldAt((double X, double Y) p) => _held.ToGlobal(new Vector2((float)p.X, (float)p.Y) + _held.Offset);
}

/// <summary>
/// The hero's pictures in any skin and hair (<c>art/hd/hero_*.png</c>), tinted from those they are drawn in
/// (<see cref="HeroLooks.SkinDrawn"/>, <see cref="HeroLooks.HairDrawn"/>), the far limbs darkened. Made once a part, colour, skin and hair.
/// </summary>
public static class SuitArt
{
    /// <summary>The far arm and leg, in the suit's colour and skin darkened by this much, so the body reads as round.</summary>
    public const double FarShade = 0.7;

    private static readonly Dictionary<(string Part, int Colour, int Skin, int Hair, bool Far), Texture2D> Made = new();
    // Each made picture's bytes, kept on the CPU: what a body crumbles into, never read back from the GPU.
    private static readonly Dictionary<Texture2D, (int W, int H, byte[] Data)> Bytes = new();

    /// <summary>A made picture's RGBA bytes, its width and height; none for a picture not made here.</summary>
    public static (int W, int H, byte[] Data)? PixelsOf(Texture2D texture) => Bytes.TryGetValue(texture, out var p) ? p : null;

    public static Texture2D Of(string part, int colour, int skin = HeroLooks.SkinDrawn, int hair = HeroLooks.HairDrawn, bool far = false)
    {
        var key = (part, colour, skin, hair, far);
        if (Made.TryGetValue(key, out var kept)) return kept;
        var fine = HiRes.Load($"hero_{part}") ?? throw new System.InvalidOperationException($"No picture hero_{part}");
        return Inked(key, fine, part, skin, hair, far);
    }

    /// <summary>
    /// A part has no ramps to swap: scaled to the look's skin and hair, its far limbs darkened, any suit in the colours drawn. Clothes
    /// lie over a skin picture of their own, tinted alone, for no colour test tells rags from skin. What a body crumbles into is the picture shrunk.
    /// </summary>
    private static Texture2D Inked((string Part, int Colour, int Skin, int Hair, bool Far) key, Image fine, string part, int skin, int hair, bool far)
    {
        if (HiRes.Load($"hero_{part}_skin") is { } under)
        {
            Tint(under, LookTint.Of($"{part}_skin", skin, hair), 1);
            under.BlendRect(fine, new Rect2I(Vector2I.Zero, fine.GetSize()), Vector2I.Zero);
            fine = under;
        }
        Tint(fine, LookTint.Of(part, skin, hair), far ? (float)FarShade : 1);
        var small = (Image)fine.Duplicate();
        small.Resize(fine.GetWidth() / HiRes.Scale, fine.GetHeight() / HiRes.Scale, Image.Interpolation.Nearest);
        var made = HiRes.Texture(fine);
        Made[key] = made;
        Bytes[made] = (small.GetWidth(), small.GetHeight(), small.GetData());
        return made;
    }

    private static void Tint(Image image, LookTint? tint, float shade)
    {
        if (tint == null && shade == 1) return;
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
            {
                var c = image.GetPixel(x, y);
                var (r, g, b) = tint?.Apply(c.R, c.G, c.B) ?? (c.R, c.G, c.B);
                image.SetPixel(x, y, new Color(r * shade, g * shade, b * shade, c.A));
            }
    }
}
