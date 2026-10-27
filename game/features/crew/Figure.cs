using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Features.Sound;
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
    private const double DustLandPerSpeed = 1.0 / 100;

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

    private readonly Limb _frontArm, _backArm, _frontLeg, _backLeg;
    private readonly Node2D _body, _helmet;
    private readonly Sprite2D _headLamp, _lens;

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
    private int? _colour;

    private Limbs? _shown;
    private (int X, int Y)? _last;
    private double _travelled;
    // Where the stride began: a hero setting off starts its step from standing, not mid-stride.
    private double _strideFrom;
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
    /// <summary>The screwdriver's grip in its picture, a frame of <c>hero_screwdriver.png</c>.</summary>
    private static readonly Vector2 Grip = new(3, 2);
    private readonly Sprite2D _tool;
    /// <summary>How far past the line to a swing's point the arm swings for the axe's blade to meet it (0040).</summary>
    private static readonly double AxeLead = AxeSwing.Lead(HeldLook.FireAxe);
    /// <summary>What the front fist holds, the fire axe, the harpoon, the drill or the screwdriver, in the picture of its look (<see cref="HeldLook"/>).</summary>
    private readonly Sprite2D _held;
    /// <summary>A gun's charged picture, its lit pixels alone, over the held one as bright as its charge (<see cref="Charge"/>), lit as it is.</summary>
    private readonly Sprite2D _charged = new() { Centered = false, Visible = false, UseParentMaterial = true };

    /// <summary>The charge's own light on the held stage, held while it charges, the brighter the fuller (2026-10-27-the-railgun).</summary>
    private static readonly (int R, int G, int B) ChargeLight = (95, 216, 255);
    /// <summary>Chopping wreckage (0034): the wreck swung at, and how far through its swing the axe is (<see cref="AxeSwing"/>).</summary>
    private (Vector2 At, double Phase)? _chop;
    /// <summary>The front arm straight through a swing and eased into and out of it (0040).</summary>
    private SwingArm _swingArm;
    /// <summary>How far into carrying its item trailing on a run the figure is, 0 to 1; and the spring its arm swings on (<see cref="HeroMotion.Carry"/>).</summary>
    private double _carry, _sinceCarryMs = double.PositiveInfinity;
    private readonly Spring _armSpring = new(HeroMotion.Carry.SpringHz, HeroMotion.Carry.SpringDamping);
    /// <summary>The swipe of light the axe leaves on its downstroke (0075).</summary>
    private readonly SwingSmear _smear = new();
    /// <summary>Whether the swing playing bit something, and is drawn held at the strike (<see cref="AxeSwing.Shown"/>); the phase last drawn.</summary>
    private bool _bit;
    private double _lastPhase;
    /// <summary>The node the body's parts hang from: where a shoulder is, the arms' own frame.</summary>
    private readonly Node2D _frame;
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

    /// <param name="own">Whether it is this player's hero, heard the same anywhere rather than from where it is.</param>
    public Figure(int colour, bool own)
    {
        _own = own;
        AddChild(_boots = new Voices(S.World, voices: 1, placed: !own));
        _backLeg = MakeLimb(Rig.Leg, "thigh", "shin", far: true, out var backLeg);
        _backArm = MakeLimb(Rig.Arm, "upperArm", "forearm", far: true, out var backArm);
        _frontLeg = MakeLimb(Rig.Leg, "thigh", "shin", far: false, out var frontLeg);
        _frontArm = MakeLimb(Rig.Arm, "upperArm", "forearm", far: false, out var frontArm);
        _frontArmNode = frontArm;
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
        // The helmet lamp on its brow (0077), not the player's colour; its lens drawn again unlit: it is the light.
        _headLamp = new Sprite2D { Texture = HeadLampArt.Picture, Centered = false, Offset = HeadLampArt.On - Joints["helmet"], Visible = false };
        Lit.Set(_headLamp);
        _lens = new Sprite2D { Texture = _lensPicture, Position = _headLamp.Offset + HeadLampArt.Lens, Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded } };
        _headLamp.AddChild(_lens);
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
        // Both legs come out from under the body, which hides their joints; the near arm hangs in front,
        // its hand over the sheet it holds and closed round the axe's haft or the harpoon's stock.
        foreach (var n in new Node2D[] { backArm, backLeg, frontLeg, torso, _helmet, _plan, _tool, _held, frontArm, _smear }) parts.AddChild(n);
        (_capeInside, _capeOutside) = (new CapeView(_cape, inside: true) { Visible = false }, new CapeView(_cape, inside: false) { Visible = false });
        Lit.Set(_capeInside);
        Lit.Set(_capeOutside);
        parts.AddChild(_capeInside);
        parts.MoveChild(_capeInside, 0);
        // The inside lies in the cape's shadow: darker, so it reads as depth behind the hero.
        _capeInside.SelfModulate = new Color(0.72f, 0.72f, 0.78f);
        parts.AddChild(_capeOutside);
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

    /// <summary>Whether the hero wears the helmet lamp (0077): it shows on the helmet's brow.</summary>
    public bool HeadLamp
    {
        get => _headLamp.Visible;
        set => _headLamp.Visible = value;
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

    /// <summary>Whether the drill held runs (0054, 0055): its bit spins up and its lamp lights; stopped, the lamp goes out and the bit winds down.</summary>
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
            _frame.AddChild(_heldView);
            _frame.MoveChild(_heldView, _frontArmNode.GetIndex());
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
        // way along counts, so a stair keeps the stride of the flat; on a ladder only the way up or down.
        if (_last is var (lx, ly))
            _travelled += (pose == Pose.Climb ? Math.Abs(hero.Y - ly) : (double)(hero.X - lx) * hero.Facing) / Hero.Unit;
        _last = (hero.X, hero.Y);
        // A new pose starts its stride afresh, its first foot down a quarter stride in; from walking to running or back
        // the legs go on, or a speed flickering about the line between them would freeze them.
        bool striding = pose is Pose.Walk or Pose.Run, strode = _wasPose is Pose.Walk or Pose.Run;
        if (pose != _wasPose && !(striding && strode)) (_strideFrom, _step) = (_travelled, null);
        if (pose is Pose.Lie or Pose.Tumble && pose != _wasPose) (_outMs, _slump, _outTilt) = (0, Random.Shared.Next(), _shown?.Tilt ?? 0);
        _outMs += elapsedMs;
        // Where the feet are, in pixels of the world.
        double feetX = (double)hero.X / Hero.Unit * Rig.Cell, feetY = (double)hero.Y / Hero.Unit * Rig.Cell;
        bool walking = pose is Pose.Stand or Pose.Walk or Pose.Run, walked = _wasPose is Pose.Stand or Pose.Walk or Pose.Run;
        double lift = _steps.Offset(feetX, feetY, walking && walked, elapsedMs);
        var cues = _motion.Step(hero, pose, elapsedMs);
        if (cues.Jumped) Boots(GroundSounds.JumpOf(Footing));
        if (cues.Landed is int fall)
        {
            var (landing, louder, tune) = GroundSounds.LandOf(Footing, fall);
            Boots(landing, tune, louder);
            int puff = Math.Min(DustLandMost, Math.Max(DustLandLeast, (int)Math.Round(fall * DustLandPerSpeed, MidpointRounding.AwayFromZero)));
            Land(feetX, feetY, puff, fall);
        }
        // A foot comes down at the end of each half stride, a quarter stride in: heard walking and running,
        // kicking up dust only running.
        double? stride = pose switch { Pose.Run => Poses.Stride.Run, Pose.Walk => Poses.Stride.Walk, _ => null };
        int? stepping = stride is double st ? (int)Math.Floor((_travelled - _strideFrom - st / 4) / (st / 2)) : null;
        if (stepping != null && _step != null && stepping != _step)
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
        bool chopping = _chop != null && item == HeldLook.FireAxe && inHand;
        bool aiming = Aim != null && item?.Aimed == true && inHand, drill = HeldLook.IsDrill(Wields);
        // The hands at work, whatever the pose (0030): afloat the paddling stops, the hero drifts calmly
        // upright, and the front arm reaches where the work is, in the body's own frame.
        bool acting = working || chopping || holding && _beamMs > 0;
        bool afloat = pose == Pose.Float;
        _swim = afloat ? Settle(_swim, Poses.Swimming(hero) && !acting ? 1 : 0, elapsedMs, SwimBlendMs) : 0;
        _fast = afloat ? Settle(_fast, Poses.SwimmingFast(hero) && !acting ? 1 : 0, elapsedMs, SwimBlendMs) : 0;
        var to = Poses.LimbsOf(pose, hero, new Moment(_travelled - _strideFrom, _timeMs, _motion.Landing, holding ? _head : null,
            _outMs, _slump, _outTilt, _swim, _fast, item != null, _motion.Now, pose is Pose.Climb or Pose.Air ? Ledge : null));
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
            to.Lying);
        _shown = shown;
        // The body placed first: what the hands reach for is found in its own frame, turned and lifted.
        _body.Rotation = (float)shown.Tilt;
        double c = Math.Abs(Math.Cos(shown.Tilt)), s = Math.Abs(Math.Sin(shown.Tilt));
        // Lying down, the middle sinks as the body turns, so it comes to rest on the floor.
        _body.Position = new Vector2(0, (float)((shown.Lying ? -(Rig.H / 2) * c - Back * s : -Rig.H / 2) - shown.Lift));
        Position = (OnPixels ?? new Vector2((float)feetX, (float)feetY)) + new Vector2(0, (float)lift);
        Scale = new Vector2(hero.Facing, 1);
        if (_capeOutside.Visible)
        {
            _cape.Step(
                elapsedMs,
                p => { var g = _frame.ToGlobal(new Vector2((float)p.X, (float)p.Y)); return (g.X, g.Y); },
                p => { var l = _frame.ToLocal(new Vector2((float)p.X, (float)p.Y)); return (l.X, l.Y); });
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
        else if (aiming && Aim is { } target && item != null)
        {
            // The gun: the arm straight, turned so the barrel's line, not the arm's, runs through the point; eased in as a swing is.
            var (dx, dy) = FromShoulder(target);
            swung = (HarpoonShot.ArmFor(item, dx, dy), 0);
        }
        frontArm = _swingArm.Next(frontArm, swung, elapsedMs, item?.Elbow ?? 0);
        // Running with an item in one hand it trails behind (HeroMotion.Carry), and the arm swings on a spring: it lags a
        // start and swings on past a stop. Aimed, swung or at work, it goes where it is put at once.
        bool carried = inHand && swung == null && !working && !aiming;
        _carry = Math.Clamp(_carry + (carried && pose == Pose.Run && !Poses.Backward(hero) ? 1 : -1) * elapsedMs / HeroMotion.Carry.BlendMs, 0, 1);
        double carry = _carry * _carry * (3 - 2 * _carry);
        _sinceCarryMs = _carry > 0 ? 0 : _sinceCarryMs + elapsedMs;
        if (carried && item != null)
        {
            double bob = HeroMotion.Carry.Bob * Math.Sin((_travelled - _strideFrom) / Poses.Stride.Run * 4 * Math.PI);
            frontArm = Mix(frontArm, Rig.Carried(bob), carry);
        }
        // The spring only round a run and a while after it: a landing or a pose's own swing moves the arm as it is.
        if (carried && _sinceCarryMs < HeroMotion.Carry.SpringAfterMs) frontArm = frontArm with { Upper = _armSpring.Follow(frontArm.Upper, elapsedMs) };
        else _armSpring.Hold(frontArm.Upper);
        var backArm = shown.BackArm;
        _frontArm.Bend(frontArm);
        _backArm.Bend(backArm);
        _frontLeg.Bend(shown.FrontLeg);
        _backLeg.Bend(shown.BackLeg);
        if (cues.Sparks > 0)
        {
            Vector2 Glove(Bend arm) => _frame.ToGlobal(Pixel(Rig.FistOf(arm)));
            Sparks(Glove(frontArm), Glove(backArm), cues.Sparks);
        }
        bool awake = pose is not (Pose.Lie or Pose.Tumble);
        _head = Settle(_head, awake ? look : 0, elapsedMs, HeadSettleMs);
        // On the floor the head takes back some of the body's lean, so a run looks ahead, not at the floor.
        _helmet.Rotation = (float)(_head - (pose is Pose.Stand or Pose.Walk or Pose.Run ? shown.Tilt * HeroMotion.Run.HeadSteady : 0));
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
            var parts = PartsOf(item);
            _held.Texture = item.Loaded != null && Loaded ? Picture(item.Loaded) : HeldArt.Of(frame > 0 ? item.Spin![frame - 1] : item.Picture, parts.Layers);
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
                (_charged.Texture, _charged.Offset) = (Picture(item.Charged!), _held.Offset);
                _charged.Modulate = new Color(1, 1, 1, (float)Charge);
            }
            _held.Position = new Vector2((float)fist.X, (float)(-Rig.Shoulder + fist.Y));
            // Carried trailing on a run, the hand turns the item along itself (Rig.Carried).
            _held.Rotation = (float)(Rig.HeldTurn(frontArm, item) + item.Hold * carry);
            if (item.Aimed)
            {
                Vector2 At((double X, double Y) p) => _held.ToGlobal(new Vector2((float)p.X, (float)p.Y) + _held.Offset);
                Muzzle = At(item.EdgeTo);
                Barrel = (Muzzle.Value - At(item.EdgeFrom)).Normalized();
                if (_charged.Visible)
                {
                    // Lit from the gap between the rails, as it is held this frame.
                    var gap = At(((item.EdgeFrom.X + item.EdgeTo.X) / 2, (item.EdgeFrom.Y + item.EdgeTo.Y) / 2));
                    Held.Flares.Hold(_charged, gap.X, gap.Y, ChargeLight, 0.15 + 0.35 * Charge, 3, 0.85f, 0.2);
                }
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
        // A drill crackles as it spins; a gun held to charge, as it fills.
        bool charging = item?.Charged != null && Charge > 0;
        var crackling = inHand && item != null && (drill || charging) ? PartsOf(item) : null;
        // Its stage made only once a drill runs or a gun charges in this hand: a body, a seated hero or the title's figures never need one.
        if (crackling != null && _drillLight == null && IsInsideTree()) _drillLight = new DrillLight(Held.Glims, Held.Flares, Held.Bolts);
        _drillLight?.Step(crackling, drill ? _spin : Charge, _heldAt ??= HeldAt, elapsedMs);
    }

    /// <summary>A point from the front shoulder, in the arms' own frame, in the body's parts' frame.</summary>
    private static Vector2 Pixel(Vec fromShoulder) => new((float)fromShoulder.X, (float)(fromShoulder.Y - Rig.Shoulder));

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
