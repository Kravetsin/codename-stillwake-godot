using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Other players' heroes, as their peers send them through the host, drawn a little behind so they move
/// smoothly between sends (<see cref="RemoteHeroes"/>), their hands wielding and working as sent (0037); and this machine's hero (features/crew/index.ts):
/// moved by the keyboard against the ship's cells, doors as this peer shows them (0014), outside the
/// sim, at the controller's 60 steps a second; drawn between its last two steps, so it moves smoothly at
/// any refresh rate, with its tether out in space and the plan in its hands while the hand is not free;
/// followed by the camera; sent to the others about 20 times a second. Every hero in its suit's colour.
/// E under the pointer, within reach, toggles a door or valve, loads a tank, catches a piece or opens a
/// crate when pressed, and the left button works a plan while held, grabbing debris too: one
/// <c>Work</c> for every tick released; with the fire axe wielded, clicked or held it swings toward the pointer, hitting the wrecks its blade meets (0039);
/// with the harpoon the arm aims it at the pointer, a click fires its rod on a line and the next reels in the piece it
/// stuck in, or pulls the hero to the wall, floor or door it stuck in, to hang there (<see cref="HarpoonShot"/>, 0044); every other hero's rod and line are played out as it sends them (<see cref="RemoteRod"/>, 0065). With an empty hand, what E would work is outlined along its own
/// pictures (<see cref="AimOutline"/>), and a prompt says what E does. With a jetpack worn (0047) the hero ties no
/// tether, and Shift in space thrusts on the suit's air, the sim told by <c>Thrust</c> and every thrusting hero
/// blowing a plume; a right click with a canister in hand, or on one in the inventory, breathes it.
/// </summary>
public partial class HeroView : Node2D
{
    private static readonly Color AimReady = Outline.AimReady, AimRefused = Outline.AimRefused;
    /// <summary>A safety line's yellow, pale enough not to shout across the dark.</summary>
    private static readonly Color Line = new(new Color("d9c27a"), 0.9f);
    /// <summary>The harpoon's line, the rope of its picture's spool: warmer than the tether's.</summary>
    private static readonly Color HarpoonRope = new(new Color("ecc67e"), 0.95f);

    private readonly Ui _ui;
    private readonly WorldButtons _mouse;
    private readonly Figure _figure = new(Suits.All[0], own: true) { ZIndex = Layers.Hero };
    // Over the machines, as the TypeScript drew the crew's layer, so a crate's whole outline shows.
    private readonly Outline _outline = new(Layers.Aim);
    private readonly FaceRim _faceRim = new();
    /// <summary>Every tether, behind the heroes and unlit: a line to find the way back by.</summary>
    private readonly Sketch _tethers = new(Layers.Tethers);
    private readonly UsePrompt _prompt = new();
    // What the hands touch is heard through the suit, in vacuum as a glove's thump; doors and valves sound where they stand (GridView).
    private readonly ActSound _act = new();
    private readonly Clock _clock = new(Hero.StepMs);
    /// <summary>Where the camera looks: ahead of the hero, its height eased after it; put straight on it wherever the hero is put anew.</summary>
    private readonly CameraFollow _follow = new();
    private bool _snap;
    /// <summary>The grid's width last frame, to find where a planned cell is.</summary>
    private int _width = 1;
    private readonly HeroAutopilot? _autopilot;
    private readonly RemoteHeroes _remote = new();
    private readonly HeroSender _sender;
    private readonly Node2D _others = new() { ZIndex = Layers.OtherHeroes };
    /// <summary>What heroes left where they died, lying on their stacks with a beacon blinking (0061).</summary>
    private readonly BodyView _bodies = new();
    /// <summary>
    /// Another hero as drawn: its figure, its tether while it has one, what its hands were busy with last
    /// frame, the swings of its axe as replayed here, each at the point sent with it, and its harpoon's rod as
    /// played here, with its line, its picture and its drum, heard where its gun is (0065).
    /// </summary>
    private sealed class Other(Figure figure, Sprite2D rod, Heard2D drum)
    {
        public readonly Figure Figure = figure;
        public Tether? Tether;
        public Busy? Busy;
        public AxeSwing Swing;
        public readonly RemoteRod Rod = new();
        public HarpoonLine? Line;
        public readonly Sprite2D RodPicture = rod;
        public readonly RodLight Light = new();
        public readonly Heard2D Drum = drum;
        /// <summary>Its suit's hiss while it burns in lava, one a breath (0093).</summary>
        public StrikeTrain Burns;
        /// <summary>Its drill's hits on the ground as played here, at the drill's pace (0093).</summary>
        public DrillPace Drill;
    }

    /// <summary>Each other hero drawn, by peer id.</summary>
    private readonly Dictionary<int, Other> _shownOthers = new();
    /// <summary>Each other hero as last heard: whether it wields the axe, and the last chop it sent, its swings begun.</summary>
    private readonly Dictionary<int, (bool Axe, Busy? Last)> _heardChops = new();
    /// <summary>What this hero's hands wield and are busy with, as drawn and sent to the others.</summary>
    private string? _wields;
    private Busy? _busy;
    /// <summary>The first cell the planner last beamed in, and how long the beam has left to fade: sent while it does.</summary>
    private Point _beamAt;
    private double _beamLeftMs;
    private Client? _sendTo;
    private Hero _previous, _current;
    private bool _placed, _wasOut, _jump, _use;
    /// <summary>The surface the hero was on last frame (0076): moved to another, it stands where the sim put it there.</summary>
    private int _surface;
    private Tether? _tether;
    private readonly List<(double X, double Y)> _tetherPoints = [];
    /// <summary>
    /// What the outline was traced for: the aim's cell, its plan, whether the sim would refuse it, and the
    /// cell's structure, valve and machine, and the brush's square; traced again only when one of them changes.
    /// </summary>
    private (int Cell, int? Plan, bool Refused, int Structure, int Valve, int? Machine, bool Back)? _traced;
    private int _steps;
    /// <summary>The world tick the last <c>Enter</c> went out on: the report waits <c>Crew.ReportEvery</c> after it (0050).</summary>
    private int _reportedAt = -Content.Crew.ReportEvery;
    private double _clockMs;
    private int _workedOnTick = -1;
    /// <summary>
    /// This hero's axe swinging, and the side it faces while it does, toward the swing's point as it began
    /// (0039); whether the left button's hold began with a grab, which swings nothing.
    /// </summary>
    private AxeSwing _swing;
    private int _swingFace = 1;
    private bool _grabHeld;
    /// <summary>Whether the swing under way was begun with the right button: it digs the back walls (0107).</summary>
    /// <summary>
    /// The harpoon (0044): this hero's rod and the cooldown every harpoon it holds waits for, the line to the
    /// rod, the rod as drawn, and whether the arm aims the gun, so the hero keeps facing the pointer.
    /// </summary>
    private HarpoonShot _harpoon = new();
    private readonly HarpoonWorld _harpoonWorld;
    private HarpoonLine? _line;
    private readonly RodLight _rodLight = new();
    private bool _aimsGun;
    /// <summary>
    /// The drill (0054): whether the arm aims it at the pointer, so the hero keeps facing it, and the world tick
    /// its last hits went on while it runs; none while it does not.
    /// </summary>
    private bool _aimsDrill;
    private DrillPace _drillPace;
    /// <summary>The drill on a wreck, heard at its bit through the suit as the wreck sounds hit, the chop, at the drill's pace (0093).</summary>
    private readonly Heard2D _drillChop = Cues.Player2D(S.Chop, S.Suit, voices: 4);
    private readonly Random _strikeSpread = new();
    /// <summary>The drill's motor, heard at its bit through the suit while it spins: grains, fewer and lower as it spins up or down (0059).</summary>
    private readonly Heard2D _whir = Cues.Player2D(S.DrillWhir, S.Suit, voices: 3);
    private StrikeTrain _grains;
    private MotorSound _drillMotor;
    private TouchHold _onPiece;
    /// <summary>The jetpack's thrust, heard through the suit while this hero thrusts, muffled out in space as all there is, its envelope eased in and out (0047, 0059).</summary>
    private readonly AudioStreamPlayer _thrustLoop = Cues.Player(S.Thrust, S.Suit);
    private LoopEnvelope _thrustSound;
    private readonly Sprite2D _rod;
    /// <summary>Every harpoon's line, over the heroes, unlit as the tethers are.</summary>
    private readonly Sketch _lines = new(Layers.Hero);
    private readonly List<(double X, double Y)> _linePoints = [];
    // The gun is this hero's own, heard through the suit (0042): the shot, the drum while it reels, ready again.
    private readonly AudioStreamPlayer _shot = Cues.Player(S.Shot, S.Suit), _drum = Cues.Player(S.Drum, S.Suit),
        _ready = Cues.Player(S.Ready, S.Suit);
    /// <summary>This hero's axe swung, every swing, through the suit as its chop is (0068); others' swings are silent, as their chops.</summary>
    private readonly AudioStreamPlayer _swingSound = Cues.Player(S.Swing, S.Suit, voices: 2);
    /// <summary>The rod biting into metal, where it struck; a crate or a pod thumps as a glove does.</summary>
    private readonly Heard2D _bite = Cues.Player2D(S.Chop, S.Suit, voices: 2);
    // Others' guns are heard on World, where they are, as their steps are: through the air, gone while this hero floats (0065).
    private readonly Heard2D _othersShot = Cues.Player2D(S.Shot, S.World, voices: 3), _othersBite = Cues.Player2D(S.Chop, S.World, voices: 3),
        _othersThump = Cues.Player2D(S.ActInSpace, S.World, voices: 2);
    /// <summary>
    /// An axe's hit on a planet's ground or what grows there, as its blade strikes (0093): this hero's through the suit, as its
    /// swing is; others' on World where they hit, their swings played as sent.
    /// </summary>
    private readonly Voices _groundHits = new(S.Suit, voices: 5), _othersGroundHits = new(S.World, voices: 6);
    /// <summary>The suit's hiss while this hero burns in lava, one a breath (0093); a friend's on World where it is.</summary>
    private readonly AudioStreamPlayer _burn = Cues.Player(S.LavaBurn, S.Suit);
    private readonly Heard2D _othersBurn = Cues.Player2D(S.LavaBurn, S.World, voices: 2);
    private StrikeTrain _burns;
    /// <summary>What the harpoon reads of the world, set at the start of every frame.</summary>
    private Registry? _m;
    private Client? _client;
    private double _progress;
    /// <summary>The ship as this frame's steps met it, doors as this peer shows them: what the rod grips.</summary>
    private Ship? _ship;
    /// <summary>Whether the player jumped or held a movement key in this frame's steps: a hanging hero lets go.</summary>
    private bool _letsGo;
    /// <summary>Which way the jetpack thrust in this frame's last step, a unit vector in thousandths; none while it does not (0047).</summary>
    private Point? _thrust;
    /// <summary>Each hero's jetpack plume, by peer, made the first time it shows (0075).</summary>
    private readonly Dictionary<int, JetPlume> _plumes = new();
    /// <summary>Whether the player asked to breathe a canister, from the inventory, since the last frame.</summary>
    private bool _breathe;
    // A canister breathed is heard in the helmet, never muffled; a friend's on World, where it is.
    private readonly AudioStreamPlayer _breathes = Cues.Player(S.AirFill, S.Helmet);
    private readonly Heard2D _othersBreathe = Cues.Player2D(S.AirFill, S.World, voices: 2);
    /// <summary>Air coming back round the heroes: a canister's vortex, a breath of room air at the visor (0081).</summary>
    private readonly AirSwirl _air = new();
    /// <summary>Which friend breathed a canister, read off its suit (0081).</summary>
    private readonly CanistersSeen _canisters = new();
    /// <summary>
    /// Rams (0103): this client's guard after its own hit, the controller steps it still reels, the creatures charging last frame
    /// (one leaving its charge by a friend flinches that friend), and the thud, this hero's own through the suit.
    /// </summary>
    private readonly Rams.Guard _ramGuard = new();
    private int _reel;
    private HashSet<int> _charging = [], _chargingNow = [];
    // A ram heard by what rams (S.RamHits): this player's own through the suit, a friend's where it is.
    private readonly Voices _thud = new(S.Suit, voices: 2, placed: false), _othersThud = new(S.World, voices: 2);

    /// <param name="ui">What the hand holds, the crate open and the piece aimed at, shared with the other features.</param>
    /// <param name="mouse">The pointer and the left button as the world reads them.</param>
    /// <param name="autopilot">A script to play the hero by instead of the keyboard, for recordings.</param>
    public HeroView(Ui ui, WorldButtons mouse, HeroAutopilot? autopilot = null)
    {
        _ui = ui;
        // Plans placed: the planner in the hero's hand beams them in, cell by cell; the others see it beam at the first.
        _ui.Planned += cells =>
        {
            _figure.Beam(cells.Select(c => Pixels(Middle(c))).ToList());
            (_beamAt, _beamLeftMs) = (Middle(cells[0]), Figure.BeamMs);
        };
        _ui.Breathed += () => _breathe = true;
        _mouse = mouse;
        _autopilot = autopilot;
        _sender = new HeroSender(hero => _sendTo?.SendHero(hero.Encode()));
        // Others behind this machine's hero, every tether behind them both (Layers).
        AddChild(_tethers);
        AddChild(_bodies);
        AddChild(_others);
        AddChild(_figure);
        AddChild(_outline);
        AddChild(_faceRim);
        AddChild(_prompt);
        AddChild(_act);
        // The lines and the rod over this machine's hero: they leave its gun's muzzle.
        AddChild(_lines);
        _rod = NewRod();
        foreach (var player in new Node[] { _shot, _drum, _ready, _swingSound, _bite, _breathes, _drillChop, _whir, _thrustLoop, _othersShot, _othersBite,
                     _othersThump, _othersBreathe, _air, _groundHits, _othersGroundHits, _burn, _othersBurn, _thud, _othersThud, _ownShots, _nailShot, _dryClick, _nailReady, _ownShotEnds })
            AddChild(player);
        _tethers.Paint = PaintTethers;
        _lines.Paint = PaintLines;
        _harpoonWorld = new HarpoonWorld(
            Touches: (from, to) => Aiming.RodMeets(_m!, _ship!, _client!.Id, from, to, _progress),
            PieceAt: PieceDrawnAt,
            Reelable: id => _m!.Debris.ReelProblem(_client!.Id, id) == null,
            Reeled: id => ReelInFlight(id) || PieceOf(id)?.Reel?.By == _client!.Id,
            Grips: at => Aiming.Grips(_ship!, at));
    }

    /// <summary>A harpoon rod's picture, over the heroes, lit, hidden till a rod is out.</summary>
    private Sprite2D NewRod()
    {
        var rod = new Sprite2D
        {
            Centered = false,
            Visible = false,
            ZIndex = Layers.Hero,
            Texture = GD.Load<Texture2D>(RodLook.Picture),
            Offset = -new Vector2((float)RodLook.Eye.X, (float)RodLook.Eye.Y),
        };
        Lit.Set(rod);
        AddChild(rod);
        return rod;
    }

    /// <summary>Where piece <paramref name="id"/> is drawn this frame, between ticks; none once it is gone.</summary>
    private (double X, double Y)? PieceDrawnAt(int id) => PieceOf(id) is { } piece ? PieceDrawn.At(piece, _progress) : null;

    /// <summary>Piece <paramref name="id"/> as the sim has it this frame; none once it is gone.</summary>
    private Piece? PieceOf(int id)
    {
        var pieces = _m!.Debris.Pieces;
        for (int i = 0; i < pieces.Count; i++)
            if (pieces[i].Id == id) return pieces[i];
        return null;
    }

    /// <summary>Whether a <see cref="Reel"/> of piece <paramref name="id"/> this peer sent has yet to run.</summary>
    private bool ReelInFlight(int id)
    {
        var flying = _client!.InFlight;
        for (int i = 0; i < flying.Count; i++)
            if (flying[i] is Reel reel && reel.Piece == id) return true;
        return false;
    }

    /// <summary>The hero after its latest step: what the sim learns of where it is.</summary>
    public Hero Current => _current;

    /// <summary>Where a dev run places the hero first (<c>--edge</c>), not the crew's spawn; it wakes at the spawn as ever.</summary>
    public Func<Registry, int>? Start { get; set; }

    /// <summary>Where the prompt for E lies on the screen while it shows: what the markers keep out of.</summary>
    public Rect2? PromptCovers => _prompt.Covers;

    /// <summary>Whether this machine's hero is passed out, as the sim said this frame: it picks nothing up and its inventory closes.</summary>
    public bool PassedOut => _wasOut;

    /// <summary>Whether the hero stands in the world yet: until its first frame in a session, <see cref="Current"/> is nowhere.</summary>
    public bool Placed => _placed;

    /// <summary>Whether the hero floats out in space, weightless, as drawn this frame.</summary>
    public bool Floats { get; private set; }

    /// <summary>Whether this machine's hero holds the plan in its hands, drawn and sent to the others: the hand not free (0022).</summary>
    public bool HoldsPlan => _ui.Hand.Get() != Holding.Free;

    /// <summary>Where <paramref name="hero"/>'s chest is, in the world's pixels.</summary>
    private static (float X, float Y) Chest(Hero hero) =>
        ((float)(hero.X * Rig.Cell / Hero.Unit), (float)((hero.Y - Spec.Height / 2.0) * Rig.Cell / Hero.Unit));

    /// <summary>Other heroes where they are drawn this frame, by peer: what sways the cables besides this one, what the markers point at.</summary>
    public IReadOnlyList<(int Id, Hero Hero)> Others { get; private set; } = [];

    /// <summary>
    /// Where every helmet lamp worn on this surface shines from as drawn this frame, in the world's pixels, and the way it
    /// points, where its head looks, and the helmet's neck, inside the hero (0077): this hero's and each friend's shown, so a friend's beam lights a cave for
    /// everyone in it. What the light draws.
    /// </summary>
    public IReadOnlyList<(Vector2 At, Vector2 Toward, Vector2 Head)> HeadLamps()
    {
        _headLamps.Clear();
        if (_figure.HeadLamp && _figure.Visible) _headLamps.Add((_figure.HeadLampAt, _figure.HeadLampToward, _figure.HeadAt));
        foreach (var (_, shown) in _shownOthers)
            if (shown.Figure.Visible && shown.Figure.HeadLamp) _headLamps.Add((shown.Figure.HeadLampAt, shown.Figure.HeadLampToward, shown.Figure.HeadAt));
        return _headLamps;
    }

    private readonly List<(Vector2 At, Vector2 Toward, Vector2 Head)> _headLamps = [];

    /// <summary>
    /// Moves the hero on by <paramref name="elapsedMs"/> against the client's world <paramref name="m"/>
    /// and draws it, the camera following it (<see cref="CameraFollow"/>), and tells the sim where it is when that matters to its
    /// breathing (<see cref="Report"/>). The hero wakes at the crew's spawn, and again there on coming
    /// round, and takes no orders while passed out. <paramref name="heard"/> are the other heroes the
    /// client took since the last frame, each drawn where it stood a little while ago; <paramref name="progress"/> is
    /// how far the world is between its last tick and the next, where the debris a harpoon's rod sticks in is drawn.
    /// </summary>
    public void Frame(Client client, Registry m, Camera2D camera, IReadOnlyList<PeerHeroMessage> heard, double progress, double elapsedMs)
    {
        (_m, _client, _progress) = (m, client, progress);
        var grid = m.Grid;
        _width = grid.Width;
        // Doors as this peer shows them: its own toggles still in flight already open or shut.
        var shown = Predicted.Grid(client, grid, m.Pipes);
        var ship = _ship = Ship.Of(grid, m.Rooms, cell => shown.StructureAt(grid, cell), m.Rules);
        var known = m.Crew.HeroOf(client.Id);
        bool passedOut = (known?.Out ?? 0) > 0;
        // Sitting in a drop pod, the hero keeps to its seat, takes no orders but E's, and is drawn behind its porthole (SeatedHeroes).
        int? seat = Seating.SeatOf(m, client.Id);
        // A jetpack worn (0047): no tether, and Shift in space thrusts while the suit has air to vent.
        bool jetpack = m.Items.Equipped(client.Id, Content.Jetpack.Item);
        bool canThrust = !passedOut && jetpack && known is { Suit: > 0 };
        int surface = client.World.SurfaceOf(client.Id);
        bool moved = _placed && surface != _surface;
        _surface = surface;
        // Moved to another surface, the harpoon's rod is back in the gun: what it stuck in is another grid's (0076).
        if (moved)
        {
            _harpoon = new HarpoonShot();
            _guns.Drop();
        }
        // Waking from passing out, the sim has put the hero at the spawn: so does the client. Moved to another surface, it
        // stands where it arrived.
        if (moved && known != null)
        {
            _previous = _current = Hero.At(ship, known.Cell);
            _snap = true;
        }
        else if (!_placed || (_wasOut && !passedOut) || moved)
        {
            _previous = _current = Hero.At(ship, !_placed && Start != null ? Start(m) : m.Crew.Spawn);
            _placed = _snap = true;
        }
        // Passing out it dies where the sim has it (0061), where its body falls: it lies there, not a report behind.
        else if (!_wasOut && passedOut && known!.Cell >= 0 && known.Cell < grid.Width * grid.Height)
        {
            _previous = _current = Hero.At(ship, known.Cell);
            _snap = true;
        }
        _wasOut = passedOut;
        // The hero faces the pointer's side and looks at it, read where the camera was last frame.
        var pointed = Pointer();
        int? face = Poses.FaceOf(pointed, _current);
        int steps = _clock.Advance(elapsedMs);
        if (steps > 0) _thrust = null;
        if (passedOut || moved) _reel = 0;
        else Rammed(client, m);
        for (int due = steps; due > 0; due--)
        {
            // Passed out, the hero takes no orders and only falls; keys pressed meanwhile are spent.
            var taken = _autopilot?.Next(_current) ?? Take();
            // Swinging, it faces where the swing began toward, swimming too: the blade comes down there. Aiming
            // the harpoon, it faces the pointer, swimming the other way too.
            bool swinging = _swing.Phase != null;
            _letsGo |= taken.Jump || taken.X != 0 || taken.Y != 0;
            // Pulled by the grapple or hanging from it, the line moves the hero, not the keys; pulled, it faces the hook.
            var pull = passedOut ? null : _harpoon.PullTo;
            int? faces = _harpoon.Rod == Rod.Pulling ? (_harpoon.Tip.X < _current.X ? -1 : 1) : swinging ? _swingFace : face;
            // Shift in space with a jetpack thrusts where the keys point, or with none toward the pointer.
            bool afloat = ship.Space(_current.BodyCell(ship.Width));
            var thrust = canThrust && afloat && taken.Sprint && pull == null
                ? Hero.ThrustToward(taken.X, taken.Y, _current.Middle, pointed)
                : null;
            if (thrust != null) _thrust = thrust;
            var input = passedOut || seat != null
                ? new HeroInput(0, 0, false, Jetpack: jetpack)
                : taken with { Face = faces, Swings = swinging || _aimsGun || _aimsDrill, Pull = pull, Jetpack = jetpack, Thrust = thrust };
            // Rammed, the hero is thrown, not walked: no keys, no ladder, a held Up or Space taken by nothing (0103).
            if (_reel > 0)
            {
                input = input with { X = 0, Y = 0, Jump = false, JumpHeld = false, Sprint = false, Thrust = null, Reeling = true };
                _reel--;
            }
            (_previous, _current) = (_current, _current.Step(input, ship));
            _steps++;
            Fogged(ship, seated: seat != null);
        }
        if (seat is int seatCell) _previous = _current = Hero.At(ship, seatCell) with { Facing = _current.Facing };
        double ownLook = passedOut ? 0 : Poses.LookOf(_current, pointed);
        // Passed out it reports no cell: one landing after it wakes at the pod would put it back out there.
        int tick = client.World.Tick;
        if (passedOut) _reportedAt = tick;
        if (Report.Of(m.Crew, m.Rooms, m.Atmosphere, grid.Width, client.Id, _current, tick, _reportedAt, Report.Every(m.Creatures.Threatens(client.Id))) is { } report)
        {
            client.Send(report);
            _reportedAt = tick;
        }
        bool exerting = !passedOut && Report.Exerting(_current, ship.Space(_current.BodyCell(grid.Width)));
        if (!passedOut && Report.ExertOf(m.Crew, client.Id, exerting) is { } exert) client.Send(exert);
        // Off as Shift is let go, aboard, the jetpack taken off or the suit empty: the sim stops venting it.
        if (Report.ThrustOf(m.Crew, client.Id, _thrust != null) is { } thrusts) client.Send(thrusts);
        HearThrust(elapsedMs);
        for (int burnt = _burns.Due(elapsedMs, Burns(m, known), 1 / S.LavaBurnEveryS); burnt > 0; burnt--) _burn.Play();
        if (_breathe && !passedOut) Breathe(client, m);
        _breathe = false;

        // Others' heroes in, this one out once its hands are known: through the host, never through the simulation.
        bool holdsPlan = HoldsPlan && !passedOut;
        _clockMs += elapsedMs;
        _beamLeftMs = Math.Max(0, _beamLeftMs - elapsedMs);
        _sendTo = client;
        foreach (var message in heard)
            if (SentHero.Decode(message.State) is { } sent)
            {
                _remote.Add(message.From, sent, _clockMs);
                // Swings are swung as heard, not behind with the body, so the blade meets the wreck as the hit lands (0038).
                var last = _heardChops.GetValueOrDefault(message.From).Last;
                _heardChops[message.From] = (sent.Seen.Wields == Content.Weapons.Axe.Item, Busy.Chopping(last, sent.Seen.Busy));
            }
        ShowOthers(ship, m, elapsedMs);
        _bodies.Frame(m, progress, elapsedMs);

        double between = _clock.Progress;
        double x = _previous.X + (_current.X - _previous.X) * between;
        double y = _previous.Y + (_current.Y - _previous.Y) * between;
        Follow(camera, ship, x, y, elapsedMs);

        // Aimed after following, so the pointer is read where the camera now is.
        var pointer = Pointer();
        var drawn = _current with { X = (int)Math.Round(x), Y = (int)Math.Round(y) };
        _ui.Heroes = [Chest(drawn), .. Others.Select(o => Chest(o.Hero))];
        _ui.Bits = Bits(m, client.Id);
        bool inSpace = Floats = ship.Space(drawn.BodyCell(ship.Width));
        // Gliding in a planet's fog it floats as in space (fog-edges), though nothing there is space.
        var pose = Poses.PoseOf(drawn, inSpace || drawn.Gliding, passedOut);
        // The hands first, so the frame a hit goes draws the blade meeting the wreck; a swing begun turns the hero.
        UseHands(client, m, shown, pointer, passedOut, pose, elapsedMs, handFree: !HoldsPlan);
        drawn = drawn with { Facing = _current.Facing };
        double look = passedOut ? 0 : Poses.LookOf(_current, pointer);
        _figure.Wear(Suits.Of(m.Players, client.Id));
        _figure.Jetpack = jetpack;
        _figure.HeadLamp = m.Items.Equipped(client.Id, Content.HelmetLamp.Item);
        _figure.FloorRoom = FloorRoom(m.Grid, drawn);
        _figure.Footing = FootingOf(m, drawn, _figure.Footing);
        _figure.Ledge = LedgeOf(ship, drawn);
        _figure.Show(drawn, pose, look, holdsPlan, elapsedMs);
        _figure.Visible = seat == null;
        // Its own thrust shows at once (0014); others' as the sim says.
        Plume(client.Id, _figure, _thrust is { } own ? (own.X, own.Y) : null, elapsedMs);
        ShowHarpoon(elapsedMs);
        _tether = Tie(_tether, 0, _current.Anchor, Tether.ClipOf(x, y), elapsedMs);
        // Stamped with its step's time, so others place it by when it was there, not when it came.
        _sender.Step(elapsedMs, new Seen(_current, ownLook, holdsPlan, _wields, _busy, _harpoon.Seen), _steps * Hero.StepMs);
        _tethers.QueueRedraw();
        _lines.QueueRedraw();
    }

    /// <summary>
    /// The step just made, in a planet's fog (fog-edges): going in, the harpoon let go, for a glide neither rises nor sinks;
    /// crossing a seam outward, the hero moved by the period to the other band at the drop height, the camera snapped and its
    /// cell reported this frame, so the sim never keeps it on the side it left.
    /// </summary>
    private void Fogged(Ship ship, bool seated)
    {
        if (ship.Glides(_current.X) && !ship.Glides(_previous.X)) _harpoon = new HarpoonShot();
        if (FogMove.Of(ship, _previous, _current, seated) is not { } moved) return;
        _previous = _current = moved;
        _harpoon = new HarpoonShot();
        _snap = true;
        _reportedAt = int.MinValue / 2;
    }

    /// <summary>
    /// A creature's charge touching this hero (0103), as this client sees them, while its guard is open: <c>HitBy</c> sent, and at
    /// once, a round trip before the sim's word, the hero thrown away from it and reeling, flinching, its suit's air knocked out,
    /// a thud through the suit. The sim takes the air; the gauge shows it as it drops.
    /// </summary>
    private void Rammed(Client client, Registry m)
    {
        int tick = client.World.Tick;
        if (!_ramGuard.Open(tick) || Rams.Contact(m.Creatures.Creatures, _current) is not { } c || m.Creatures.HitByProblem(c.Id) != null) return;
        client.Send(new Stillwake.Sim.Creatures.HitBy(c.Id));
        _ramGuard.Sent(tick);
        int away = Rams.Away(c, _current);
        _current = Rams.Knocked(_current, away, Content.Creatures.Get(c.Species).Moves[c.Act!.Move].Damage);
        _reel = Spec.Reel;
        _figure.Flinch(-away);
        var (x, y) = Chest(_current);
        _air.Leak(new Vector2(x, y), -away);
        _thud.Play(S.RamHits.GetValueOrDefault(c.Species, S.RamThud), default);
    }

    /// <summary>
    /// Whether a creature whose charge ended since last frame touched <paramref name="hero"/>, a friend drawn here (0103): the side
    /// it came from, +1 right, the way it recoils, for the friend to flinch from, and what rammed; none for none. Its peer sent the hit; this one
    /// only shows it.
    /// </summary>
    private (int From, string Species)? RamEndedBy(Registry m, Hero hero)
    {
        foreach (var c in m.Creatures.Creatures)
            if (_charging.Contains(c.Id) && c.Posture != Stillwake.Sim.Creatures.Posture.Charge && Rams.Touches(c, hero, Rams.FriendMargin))
                return (c.VX >= 0 ? 1 : -1, c.Species);
        return null;
    }

    /// <summary>
    /// The camera after the hero drawn at (<paramref name="x"/>, <paramref name="y"/>), its feet in thousandths of a
    /// cell: led the way its last step went, the height eased after it (<see cref="CameraFollow"/>), it and the hero on whole pixels.
    /// </summary>
    private void Follow(Camera2D camera, Ship ship, double x, double y, double elapsedMs)
    {
        var hero = new Followed(x / Hero.Unit, (y - Spec.Height / 2.0) / Hero.Unit, _current.X - _previous.X, _current.Y - _previous.Y,
            ship.Space(_current.BodyCell(ship.Width)));
        if (_snap) _follow.Snap(hero);
        else _follow.Step(hero, camera.GetViewportRect().Size.X / camera.Zoom.X / Rig.Cell, elapsedMs);
        _snap = false;
        var (look, feet) = CameraFollow.OnPixels(_follow.X, _follow.Y, x / Hero.Unit, y / Hero.Unit, Rig.Cell * camera.Zoom.X);
        camera.Position = new Vector2((float)look.X, (float)look.Y) * (float)Rig.Cell;
        _figure.OnPixels = new Vector2((float)feet.X, (float)feet.Y) * (float)Rig.Cell;
    }

    /// <summary>The world point under the pointer, in cells, or the autopilot's while it points; none off the window, under the menu or the computer's window.</summary>
    private (double X, double Y)? Pointer() => _autopilot?.PointerFor(_current) ?? (_ui.MenuOpen.Get() || _ui.OpenComputer.Get() != null ? null : _mouse.Pointer());

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.PhysicalKeycode == Key.Space) _jump = true;
        if (key.PhysicalKeycode == Key.E) _use = true;
    }

    /// <summary>The input for the next controller step, by the keys' places on the keyboard; a jump counts for one step only.</summary>
    private HeroInput Take()
    {
        static bool Held(Key a, Key b) => Input.IsPhysicalKeyPressed(a) || Input.IsPhysicalKeyPressed(b);
        static int Axis(bool minus, bool plus) => (plus ? 1 : 0) - (minus ? 1 : 0);
        // Keys held as the game menu opened are not walked on behind it, nor letters typed in a field (the build menu's search).
        if (_ui.MenuOpen.Get() || GetViewport().GuiGetFocusOwner() is LineEdit)
        {
            _jump = false;
            return new HeroInput(0, 0, false);
        }
        var input = new HeroInput(
            Axis(Held(Key.A, Key.Left), Held(Key.D, Key.Right)),
            Axis(Held(Key.W, Key.Up), Held(Key.S, Key.Down)),
            _jump,
            Sprint: Input.IsPhysicalKeyPressed(Key.Shift),
            JumpHeld: Input.IsPhysicalKeyPressed(Key.Space));
        _jump = false;
        return input;
    }

    private bool TakeUse()
    {
        bool pressed = _use;
        _use = false;
        return pressed;
    }
}
