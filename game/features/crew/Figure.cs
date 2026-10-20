using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A hero's figure (apps/game/src/features/crew/figure.ts): helmet and visor, body, air pack, two arms
/// and two legs that bend at shoulders and elbows, hips and knees, each part a picture from
/// <c>art/hero_*.png</c> recoloured into its suit (<see cref="Wear"/>), lit where it stands as everything
/// in the ship is, the rig moving and turning the parts smoothly. <see cref="Show"/> places it at the
/// hero's feet and poses it, its boots heard stepping, jumping and landing and kicking up dust: this
/// player's own hero heard the same anywhere, any other from where it is in the world; neither while floating.
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
    /// Afloat (0036): how slowly the upright body leans into a swim and rights itself, and how slowly
    /// drifting still, swimming and swimming fast blend into each other. Left for right it turns at once:
    /// a flat figure squeezed through a turn looks worse than a jump.
    /// </summary>
    private const double LeanMs = 450, SwimBlendMs = 320;
    /// <summary>How far the air pack sticks out behind the middle: what a body lying on its back rests on.</summary>
    private const double Back = Rig.W * 0.62;
    /// <summary>Motes of dust a boot kicks up running, and the grains a landing throws at least and at most to either side, by its fall (0075).</summary>
    private const int DustRun = 5, DustLandLeast = 3, DustLandMost = 9;
    private const double DustLandPerSpeed = 1.0 / 50;

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
        ["sheet"] = new(0, 5),
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

    /// <summary>
    /// A leg's boot, drawn apart from its shin only on a slope (<see cref="Footing"/>): the bottom of <c>hero_shin.png</c>
    /// from the top of the boot down, at the ankle, kept level rather than turned with the shin; on the flat the shin is drawn
    /// whole.
    /// </summary>
    private sealed record Boot(Sprite2D Shin, Sprite2D Sole, Vector2 Ankle)
    {
        /// <summary>Sits the boot as <paramref name="shoe"/> says on a shin turned <paramref name="shin"/> from straight down, the way the hero faces.</summary>
        public void Sit(Shoe shoe, double shin)
        {
            Shin.RegionEnabled = Sole.Visible = shoe != default;
            if (!Sole.Visible) return;
            // Level in the figure, seen from the shin's frame: turned back by the shin; blended with the shin's own.
            double c = Math.Cos(shin), s = Math.Sin(shin), w = shoe.Level;
            var x = new Vector2((float)(1 - w + w * c), (float)(-w * s));
            var y = new Vector2((float)(w * s), (float)(1 - w + w * c));
            Sole.Transform = new Transform2D(x, y, Ankle);
        }
    }

    private readonly Limb _frontArm, _backArm, _frontLeg, _backLeg;
    private readonly Boot _frontBoot, _backBoot;
    private readonly Footing _footing = new();
    private readonly Node2D _body, _helmet;
    private readonly Sprite2D _headLamp;

    /// <summary>Where the helmet lamp sits on the helmet, from the neck it turns at, facing +x: on its brow, ahead of the visor.</summary>
    private static readonly Vector2 HeadLampOn = new(4, -9);

    /// <summary>The visor's middle on the helmet, from the neck, facing +x: where air breathed in is drawn to.</summary>
    private static readonly Vector2 VisorOn = new(4, -5);

    /// <summary>The lamp's bulb, a few warm pixels, a placeholder till its art is picked: each figure's own, freed with it.</summary>
    private readonly Texture2D _bulb = MakeBulb();

    private static Texture2D MakeBulb()
    {
        var image = Image.CreateEmpty(3, 2, false, Image.Format.Rgba8);
        image.Fill(new Color(1f, 0.9f, 0.62f));
        image.SetPixel(0, 1, new Color(0.55f, 0.48f, 0.4f));
        image.SetPixel(2, 1, new Color(0.55f, 0.48f, 0.4f));
        return ImageTexture.CreateFromImage(image);
    }
    /// <summary>The jetpack worn on the back over the air pack (0047), a placeholder picture till its art is picked.</summary>
    private readonly Sprite2D _jetpack;
    /// <summary>Where the jetpack's nozzle is in its picture: what the plume leaves from.</summary>
    private static readonly Vector2 NozzleAt = new(3, 14);
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
    /// <summary>The screwdriver's grip in its picture, a frame of <c>hero_screwdriver.png</c>.</summary>
    private static readonly Vector2 Grip = new(3, 2);
    private readonly Sprite2D _tool;
    /// <summary>How far past the line to a swing's point the arm swings for the axe's blade to meet it (0040).</summary>
    private static readonly double AxeLead = AxeSwing.Lead(HeldLook.FireAxe);
    /// <summary>What the front fist holds, the fire axe, the harpoon or the drill, in the picture of its look (<see cref="HeldLook"/>).</summary>
    private readonly Sprite2D _held;
    /// <summary>Chopping wreckage (0034): the wreck swung at, and how far through its swing the axe is (<see cref="AxeSwing"/>).</summary>
    private (Vector2 At, double Phase)? _chop;
    /// <summary>The front arm straight through a swing and eased into and out of it (0040).</summary>
    private SwingArm _swingArm;
    /// <summary>
    /// Held in both hands (0078): how far the hands have gone from the pose to the grips, 0 to 1 over <see cref="GraspMs"/>,
    /// and the item's turn as drawn, settling toward the aim.
    /// </summary>
    private double _grasp, _holdAngle;
    private const double GraspMs = 120;
    /// <summary>The swipe of light the axe leaves on its downstroke (0075).</summary>
    private readonly SwingSmear _smear = new();
    /// <summary>Whether the swing playing bit something, and is drawn held at the strike (<see cref="AxeSwing.Shown"/>); the phase last drawn.</summary>
    private bool _bit;
    private double _lastPhase;
    /// <summary>The node the body's parts hang from: where a shoulder is, the arms' own frame.</summary>
    private readonly Node2D _frame;
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
    /// <summary>How fast the hero last fell, thousandths of a cell a step: how hard it lands.</summary>
    private int _fall;
    private readonly Action _stepped, _landed, _jumped;

    /// <param name="own">Whether it is this player's hero, heard on the suit rather than in the world.</param>
    public Figure(int colour, bool own)
    {
        _stepped = Voice(S.Step, own);
        _landed = Voice(S.Land, own);
        _jumped = Voice(S.Jump, own);
        _backLeg = MakeLimb(Rig.Leg, "thigh", "shin", far: true, out var backLeg);
        _backBoot = MakeBoot(_backLeg, far: true);
        _backArm = MakeLimb(Rig.Arm, "upperArm", "forearm", far: true, out var backArm);
        _frontLeg = MakeLimb(Rig.Leg, "thigh", "shin", far: false, out var frontLeg);
        _frontBoot = MakeBoot(_frontLeg, far: false);
        _frontArm = MakeLimb(Rig.Arm, "upperArm", "forearm", far: false, out var frontArm);
        backLeg.Position = new Vector2((float)-Rig.HipApart, (float)-Rig.Hip);
        frontLeg.Position = new Vector2((float)Rig.HipApart, (float)-Rig.Hip);
        backArm.Position = frontArm.Position = new Vector2(0, (float)-Rig.Shoulder);
        var torso = new Node2D();
        var pack = Part("pack");
        pack.Position = new Vector2((float)-Back, (float)(-Rig.Shoulder - Rig.H * 0.02));
        var chest = Part("torso");
        chest.Position = new Vector2(0, (float)-Rig.Neck);
        _jetpack = new Sprite2D { Texture = GD.Load<Texture2D>("res://art/hero_jetpack.png"), Centered = false, Visible = false };
        _jetpack.Position = pack.Position + new Vector2(-2, -1);
        Lit.Set(_jetpack);
        torso.AddChild(pack);
        torso.AddChild(_jetpack);
        torso.AddChild(chest);
        // The helmet turns at the neck.
        _helmet = new Node2D { Position = new Vector2(0, (float)-Rig.Neck) };
        _helmet.AddChild(Part("helmet"));
        // The helmet lamp's bulb on its brow (0077), drawn unlit: it is the light.
        _headLamp = new Sprite2D { Texture = _bulb, Position = HeadLampOn, Visible = false, Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded } };
        _helmet.AddChild(_headLamp);
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
        _plan.AddChild(_glow);
        _tool = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://art/hero_screwdriver.png"),
            Hframes = 3,
            Centered = false,
            Offset = -Grip,
            Visible = false,
        };
        Lit.Set(_tool);
        _held = new Sprite2D { Centered = false, Visible = false };
        Lit.Set(_held);
        _held.AddChild(_lamp);
        _held.AddChild(_lampGlow);
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
        // Both legs come out from under the body, which hides their joints; the near arm hangs in front,
        // its hand over the sheet it holds and closed round the axe's haft or the harpoon's stock.
        foreach (var n in new Node2D[] { backArm, backLeg, frontLeg, torso, _helmet, _plan, _tool, _held, frontArm, _smear }) parts.AddChild(n);
        _smear.Position = new Vector2(0, (float)-Rig.Shoulder);
        AddChild(_body);
        Wear(colour);
    }

    private static readonly Dictionary<string, Texture2D> Pictures = new();

    /// <summary>A held item's picture, loaded once.</summary>
    private static Texture2D Picture(string path)
    {
        if (!Pictures.TryGetValue(path, out var picture)) Pictures[path] = picture = GD.Load<Texture2D>(path);
        return picture;
    }

    /// <summary>A player of <paramref name="cue"/> at the feet, on the World bus, silent while floating: heard the same anywhere for this player's hero, else placed in the world.</summary>
    private Action Voice(S.Cue cue, bool own)
    {
        if (own)
        {
            var mine = Cues.Player(cue, S.World);
            AddChild(mine);
            return () => mine.Play();
        }
        var world = Cues.Player2D(cue, S.World);
        AddChild(world);
        return () => world.Play();
    }

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
        var local = _frame.ToLocal(at);
        return (local.X, local.Y + Rig.Shoulder);
    }

    /// <summary>Has the hero work the plan at <paramref name="at"/>, in pixels of the world, with its screwdriver; null when it stops.</summary>
    public void Work(Vector2? at) => _workAt = at;

    /// <summary>
    /// Has the hero swing the axe it wields at the wreck at <paramref name="swing"/>'s point, in pixels of
    /// the world, as far through the swing as its phase says; null between swings.
    /// </summary>
    public void Chop((Vector2 At, double Phase)? swing) => _chop = swing;

    /// <summary>The swing playing bit something: its arm is drawn stopped on contact for a moment (0075), the sim untouched.</summary>
    public void Bit() => _bit = true;

    /// <summary>The item the front hand holds, by id, the fire axe or the harpoon: its hotbar slot chosen, the hand free; none for an empty hand.</summary>
    public string? Wields { get; set; }

    /// <summary>Where the harpoon or the drill held aims, in pixels of the world: the arm straight, the barrel's or the bit's line through it; none to let it hang.</summary>
    public Vector2? Aim { get; set; }

    /// <summary>Whether the hero wears a jetpack on its back (0047).</summary>
    public bool Jetpack
    {
        get => _jetpack.Visible;
        set => _jetpack.Visible = value;
    }

    /// <summary>Whether the hero wears the helmet lamp (0077): its bulb shows on the helmet's brow.</summary>
    public bool HeadLamp
    {
        get => _headLamp.Visible;
        set => _headLamp.Visible = value;
    }

    /// <summary>The helmet lamp's bulb as drawn, in pixels of the world: where its light shines from, turning with the head.</summary>
    public Vector2 HeadLampAt => _headLamp.GlobalPosition;

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

    /// <summary>Whether the drill held runs (0054, 0055): its bit spins up and its lamp lights; stopped, the lamp goes out and the bit winds down.</summary>
    public bool Spinning { get; set; }

    /// <summary>Whether the drill held bites a wreck now: its bit throws sparks there (<see cref="DrillLight"/>).</summary>
    public bool Bites { get; set; }

    private readonly DrillLight _drillLight = new();
    private Func<(double X, double Y), Vector2>? _heldAt;

    /// <summary>How fast the bit turns, from still (0) to full (1), and how far through its frames the picture is, in frames.</summary>
    private double _spin, _spinFrame;
    /// <summary>The held item's lamp, lit while it runs: its pixel drawn unlit, so it shows in the dark, and the little light it gives.</summary>
    private readonly ColorRect _lamp = new() { Size = Vector2.One, Color = LampColour, Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore, Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded } };
    private readonly Glow _lampGlow = new() { Visible = false };
    private static readonly Color LampColour = new(HeldLook.LampColour);
    private const float LampLight = 0.25f;

    /// <summary>The held harpoon's muzzle, or the drill's point, as drawn this frame, in pixels of the world; none while neither is shown.</summary>
    public Vector2? Muzzle { get; private set; }

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
    private static void Kick(double x, double y, int faces)
    {
        var glims = Glims.Shared;
        for (int i = 0; i < Math.Round(DustRun * Fx.Count); i++)
            glims.Add(Luminous.BootDust, x + glims.Between(-1.5, 1.5), y - 0.4, faces * glims.Between(-24, 6), -glims.Between(4, 15), y,
                glims.Between(0.5, 0.85), glims.Between(0.45, 0.8));
        glims.Add(Luminous.DustVeil, x - faces, y - 1.5, -faces * glims.Between(6, 14), -glims.Between(1, 4));
    }

    /// <summary>
    /// A landing's dust at the feet, (<paramref name="x"/>, <paramref name="y"/>) the floor (0075): to either side
    /// <paramref name="grains"/> grains skittering out along the floor, slowing into motes, as many motes, two veils rolling
    /// out and curling; and a faint flat ring racing out along the floor, the only light in it.
    /// </summary>
    private void Land(double x, double y, int grains)
    {
        var glims = Glims.Shared;
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
        }
        double ring = Math.Min(3 + 26 * Fx.Length, Math.Min(FloorRoom.Left, FloorRoom.Right));
        if (ring >= 6) Flares.Shared.Shock(x, y - 0.4, 3, ring, 10 * Glim.SampleMs, (236, 240, 248), 0.32 * Fx.Glow, 0.28, 0.16);
    }

    /// <summary>How far, in pixels, a landing's dust needs the floor open to spread its whole way.</summary>
    private const double FloorSpread = 40;

    /// <summary>
    /// How far the floor runs open from the feet to either side, in pixels of the world, till a wall: how far a landing's
    /// dust spreads along it. Set by the crew each frame; unbounded till then.
    /// </summary>
    public (double Left, double Right) FloorRoom { get; set; } = (double.MaxValue, double.MaxValue);

    /// <summary>
    /// The cells the hero stands among, as the client has them: standing, walking or running on them, its feet are planted on
    /// the ground under each (<see cref="Footing"/>). None for a figure with no ground to read, its feet where the pose puts them.
    /// </summary>
    public Ship? Ground { get; set; }

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

    /// <summary>
    /// The boot of a leg made by <see cref="MakeLimb"/>, turning at the ankle, <see cref="Footing.Ankle"/> above the sole:
    /// the shin's picture from that row down, the shin itself drawn only above it while the boot turns.
    /// </summary>
    private Boot MakeBoot(Limb leg, bool far)
    {
        var shin = (Sprite2D)leg.Lower.GetChild(0);
        var joint = Joints["shin"];
        var size = GD.Load<Texture2D>("res://art/hero_shin.png").GetSize();
        float ankle = (float)(Rig.Leg.Lower - Footing.Ankle), row = joint.Y + ankle;
        shin.RegionRect = new Rect2(0, 0, size.X, row);
        var sole = Part("shin", far);
        (sole.RegionEnabled, sole.RegionRect, sole.Visible) = (true, new Rect2(0, row, size.X, size.Y - row), false);
        (sole.Position, sole.Offset) = (new Vector2(0, ankle), new Vector2(-joint.X, 0));
        leg.Lower.AddChild(sole);
        return new Boot(shin, sole, sole.Position);
    }

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
        // Walking backwards, away from where it faces, the stride runs backwards too. On the ground only the
        // way along counts, so a slope keeps the stride of the flat; on a ladder only the way up or down. On a slope the
        // stride runs as the page's walk's does, quicker, so a foot that is down keeps pace with the ground (Footing).
        if (_last is var (lx, ly))
            _travelled += (pose == Pose.Climb ? Math.Abs(hero.Y - ly) : (double)(hero.X - lx) * hero.Facing * (1 + _footing.Steep * (Footing.Quicken(pose) - 1))) / Hero.Unit;
        _last = (hero.X, hero.Y);
        // A new pose starts its stride afresh, its first foot down a quarter stride in; from walking to running or back
        // the legs go on, or a speed flickering about the line between them would freeze them.
        bool striding = pose is Pose.Walk or Pose.Run, strode = _wasPose is Pose.Walk or Pose.Run;
        if (pose != _wasPose && !(striding && strode)) (_strideFrom, _step) = (_travelled, null);
        if (pose is Pose.Lie or Pose.Tumble && pose != _wasPose) (_outMs, _slump, _outTilt) = (0, Random.Shared.Next(), _shown?.Tilt ?? 0);
        _outMs += elapsedMs;
        // Where the feet are, in pixels of the world.
        double feetX = (double)hero.X / Hero.Unit * Rig.Cell, feetY = (double)hero.Y / Hero.Unit * Rig.Cell;
        if (Poses.Jumped(_wasPose, pose, hero)) _jumped();
        if (pose == Pose.Air) _fall = Math.Max(0, hero.Vy);
        if (Poses.Landed(_wasPose, pose, _fall))
        {
            _landingMs = Poses.Land.Ms;
            _landed();
            int puff = Math.Min(DustLandMost, Math.Max(DustLandLeast, (int)Math.Round(_fall * DustLandPerSpeed, MidpointRounding.AwayFromZero)));
            Land(feetX, feetY, puff);
        }
        // A foot comes down at the end of each half stride, a quarter stride in: heard walking and running,
        // kicking up dust only running.
        double? stride = pose switch { Pose.Run => Poses.Stride.Run, Pose.Walk => Poses.Stride.Walk, _ => null };
        int? stepping = stride is double st ? (int)Math.Floor((_travelled - _strideFrom - st / 4) / (st / 2)) : null;
        if (stepping != null && _step != null && stepping != _step)
        {
            _stepped();
            if (pose == Pose.Run) Kick(feetX + hero.Facing * Rig.W * 0.3, feetY, hero.Facing);
        }
        _step = stepping;
        _landingMs = Math.Max(0, _landingMs - elapsedMs);
        _wasPose = pose;
        // Working a plan the screwdriver takes the front hand; the planner waits till it is done.
        bool working = _workAt != null && Poses.HoldsIn(pose);
        _workMs = working ? _workMs + elapsedMs : 0;
        bool holding = holdsPlan && Poses.HoldsIn(pose) && !working;
        var item = HeldLook.Of(Wields);
        bool inHand = item != null && Poses.HoldsIn(pose) && !working && !holding;
        bool chopping = _chop != null && item == HeldLook.FireAxe && inHand;
        bool aiming = Aim != null && item?.Aimed == true && inHand, drill = HeldLook.IsDrill(Wields);
        // The hands at work, whatever the pose (0030): afloat the paddling stops, the hero drifts calmly
        // upright, and the front arm reaches where the work is, in the body's own frame.
        bool acting = working || chopping || holding && _beamMs > 0;
        bool afloat = pose == Pose.Float;
        _swim = afloat ? Settle(_swim, Poses.Swimming(hero) && !acting ? 1 : 0, elapsedMs, SwimBlendMs) : 0;
        _fast = afloat ? Settle(_fast, Poses.SwimmingFast(hero) && !acting ? 1 : 0, elapsedMs, SwimBlendMs) : 0;
        var to = Poses.LimbsOf(pose, hero, new Moment(_travelled - _strideFrom, _timeMs, _landingMs / Poses.Land.Ms, holding ? _head : null,
            _outMs, _slump, _outTilt, _swim, _fast, item != null));
        var from = _shown ?? to;
        double within = pose switch { Pose.Float => FloatSettleMs, Pose.Tumble => SlackMs, _ => SettleMs };
        var shown = new Limbs(
            SettleBend(from.FrontArm, to.FrontArm, elapsedMs, within),
            SettleBend(from.BackArm, to.BackArm, elapsedMs, within),
            SettleBend(from.FrontLeg, to.FrontLeg, elapsedMs, within),
            SettleBend(from.BackLeg, to.BackLeg, elapsedMs, within),
            SettleAngle(from.Tilt, to.Tilt, elapsedMs, afloat ? LeanMs : SettleMs),
            Settle(from.Lift, to.Lift, elapsedMs),
            to.Lying);
        _shown = shown;
        // On the ground, each foot on the ground under it and the hips dropped for the lower one to reach.
        var stance = _footing.Place(shown, pose, _travelled - _strideFrom, Ground, hero, elapsedMs);
        // The body placed first: what the hands reach for is found in its own frame, turned and lifted.
        _body.Rotation = (float)shown.Tilt;
        double c = Math.Abs(Math.Cos(shown.Tilt)), s = Math.Abs(Math.Sin(shown.Tilt));
        // Lying down, the middle sinks as the body turns, so it comes to rest on the floor.
        _body.Position = new Vector2(0, (float)((shown.Lying ? -(Rig.H / 2) * c - Back * s : -Rig.H / 2) - shown.Lift + stance.Drop));
        Position = new Vector2((float)feetX, (float)feetY);
        Scale = new Vector2(hero.Facing, 1);
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
        if (working && _workAt is { } plan)
        {
            // Toward the plan, from the shoulder.
            var (dx, dy) = FromShoulder(plan);
            work = Math.Atan2(dy, dx);
            double cycle = _workMs % TwistMs / TwistMs;
            twist = Math.Min(1, cycle / TurnShare);
            // Rising as the wrist turns over, sinking as it turns back to grip again.
            double rise = cycle < TurnShare ? Math.Sin(twist * Math.PI / 2) : Math.Cos((cycle - TurnShare) / (1 - TurnShare) * Math.PI / 2);
            double reach = WorkReach * (Rig.Arm.Upper + Rig.Arm.Lower) + TwistPush * rise;
            // Up, square to the tool: the side the knuckles come over.
            double ux = Math.Sin(work), uy = -Math.Cos(work);
            frontArm = Rig.Reach(new Vec(Math.Cos(work) * reach + ux * TwistLift * rise, Math.Sin(work) * reach + uy * TwistLift * rise), Rig.Arm, 1);
        }
        (double Angle, double Phase)? swung = null;
        if (chopping && _chop is { At: var wreck, Phase: var phase })
        {
            // Toward the point, from the shoulder: raised back over it, then brought down till the blade,
            // not the hand, crosses the line to it (0040).
            var (dx, dy) = FromShoulder(wreck);
            if (phase < _lastPhase) _bit = false;
            _lastPhase = phase;
            double drawn = _bit ? AxeSwing.Shown(phase) : phase;
            swung = (Math.Atan2(dy, dx) + AxeLead + AxeSwing.Lift(drawn), drawn);
        }
        else if (aiming && Aim is { } target && item is { Both: null })
        {
            // The gun: the arm straight, turned so the barrel's line, not the arm's, runs through the point; eased in as a swing is.
            var (dx, dy) = FromShoulder(target);
            swung = (HarpoonShot.ArmFor(item, dx, dy), 0);
        }
        frontArm = _swingArm.Next(frontArm, swung, elapsedMs);
        var backArm = shown.BackArm;
        // Held in both hands (0078): turned toward the point at the chest, at rest aimed at nothing, the hands on its grips,
        // taking them from the pose over GraspMs.
        bool both = inHand && item?.Both != null;
        if (both)
        {
            var (dx, dy) = aiming && Aim is { } bit ? FromShoulder(bit) : default;
            double wanted = aiming ? TwoHand.AngleFor(item!, dx, dy) : TwoHand.Rest;
            _holdAngle = _grasp == 0 ? wanted : Settle(_holdAngle, wanted, elapsedMs);
            _grasp = Math.Min(1, _grasp + elapsedMs / GraspMs);
            var (near, far) = TwoHand.Arms(item!, _holdAngle);
            double g = _grasp * _grasp * (3 - 2 * _grasp);
            (frontArm, backArm) = (Mix(frontArm, near, g), Mix(backArm, far, g));
        }
        else _grasp = 0;
        _frontArm.Bend(frontArm);
        _backArm.Bend(backArm);
        _frontLeg.Bend(stance.FrontLeg);
        _backLeg.Bend(stance.BackLeg);
        _frontBoot.Sit(stance.FrontShoe, Footing.Lies(shown, stance.FrontLeg));
        _backBoot.Sit(stance.BackShoe, Footing.Lies(shown, stance.BackLeg));
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
            float glow = GlowStrength * (float)(1 + 1.5 * thrust);
            _glow.Colour = new Color(GlowColour.R * glow, GlowColour.G * glow, GlowColour.B * glow);
        }
        _tool.Visible = working;
        if (working)
        {
            var hand = Rig.EndOf(frontArm, Rig.Arm);
            _tool.Position = new Vector2((float)hand.X, (float)(-Rig.Shoulder + hand.Y));
            _tool.Rotation = (float)work;
            // Three steps of the ribs a turn, held on the way back: they roll on one way only.
            _tool.Frame = (int)Math.Min(2, Math.Floor(twist * 3));
        }
        // The item in the front fist, held by its grip at its angle to the forearm (0040): swung, aimed or
        // hanging, the hand closed round its haft or stock.
        _held.Visible = inHand;
        Muzzle = null;
        if (inHand && item != null)
        {
            var fist = Rig.FistOf(frontArm);
            _spin = Spinning ? Math.Min(1, _spin + elapsedMs / HeldLook.SpinUpMs) : Math.Max(0, _spin - elapsedMs / HeldLook.SpinDownMs);
            // Through the picture and its spin frames in turn; still, it stops on the picture.
            int frames = 1 + (item.Spin?.Count ?? 0);
            _spinFrame = _spin > 0 ? (_spinFrame + _spin * HeldLook.SpinFps * elapsedMs / 1000) % frames : 0;
            int frame = (int)_spinFrame;
            _held.Texture = Picture(item.Loaded != null && Loaded ? item.Loaded
                : frame > 0 ? item.Spin![frame - 1]
                : item.Picture);
            bool lit = Spinning && item.Lamp != null;
            _lamp.Visible = _lampGlow.Visible = lit;
            // Held by its grip in the fist, or in both hands by its mount on the pivot.
            var origin = item.Both?.Mount ?? item.Grip;
            // The frames draw the lamp dim; lit, its pixel is drawn over them.
            if (item.Lamp is { } at)
            {
                var lamp = new Vector2((float)(at.X - origin.X), (float)(at.Y - origin.Y));
                (_lamp.Position, _lampGlow.Position) = (lamp - Vector2.One / 2, lamp);
            }
            _held.Offset = -new Vector2((float)origin.X, (float)origin.Y);
            var mount = both ? TwoHand.PivotAt(_holdAngle) : fist;
            _held.Position = new Vector2((float)mount.X, (float)(-Rig.Shoulder + mount.Y));
            _held.Rotation = (float)(both ? _holdAngle : Rig.HeldTurn(frontArm, item));
            if (item.Aimed)
            {
                Vector2 At((double X, double Y) p) => _held.ToGlobal(new Vector2((float)p.X, (float)p.Y) + _held.Offset);
                Muzzle = At(item.EdgeTo);
                Barrel = (Muzzle.Value - At(item.EdgeFrom)).Normalized();
            }
        }
        // The axe's downstroke leaves its swipe of light, from the blade as drawn.
        bool striking = chopping && swung is { Phase: var p } && p > AxeSwing.WindShare && p <= AxeSwing.WindShare + AxeSwing.StrikeShare;
        if (striking && item != null)
        {
            var (back, edge) = (Rig.HeldAt(frontArm, item, item.EdgeFrom), Rig.HeldAt(frontArm, item, item.EdgeTo));
            _smear.Follow(new Vector2((float)back.X, (float)back.Y), new Vector2((float)edge.X, (float)edge.Y), true, elapsedMs);
        }
        else _smear.Follow(null, null, false, elapsedMs);
        _beamMs = Math.Max(0, _beamMs - elapsedMs);
        ShowBeams(holding);
        _drillLight.Step(inHand && drill ? item : null, _spin, Bites && inHand, Muzzle, Barrel, _heldAt ??= HeldAt, elapsedMs);
    }

    /// <summary>A point of the held item's picture in the world, as it is drawn now.</summary>
    private Vector2 HeldAt((double X, double Y) p) => _held.ToGlobal(new Vector2((float)p.X, (float)p.Y) + _held.Offset);
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
