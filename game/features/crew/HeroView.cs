using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Crew;
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
    private static readonly Color AimReady = new("7fe08a"), AimRefused = new("e05555");
    /// <summary>A safety line's yellow, pale enough not to shout across the dark.</summary>
    private static readonly Color Line = new(new Color("d9c27a"), 0.9f);
    /// <summary>The harpoon's line, the rope of its picture's spool: warmer than the tether's.</summary>
    private static readonly Color HarpoonRope = new(new Color("ecc67e"), 0.95f);

    private readonly Ui _ui;
    private readonly WorldButtons _mouse;
    private readonly Figure _figure = new(Suits.All[0], own: true) { ZIndex = Layers.Hero };
    // Over the machines, as the TypeScript drew the crew's layer, so a crate's whole outline shows.
    private readonly Outline _outline = new(Layers.Aim);
    /// <summary>Every tether, behind the heroes and unlit: a line to find the way back by.</summary>
    private readonly Sketch _tethers = new(Layers.Tethers);
    private readonly UsePrompt _prompt = new();
    // What the hands touch is heard through the suit, in vacuum as a glove's thump; doors and valves sound where they stand (GridView).
    private readonly ActSound _act = new();
    private readonly Clock _clock = new(Hero.StepMs);
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
        public readonly Heard2D Drum = drum;
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
    private Tether? _tether;
    private readonly List<(double X, double Y)> _tetherPoints = [];
    /// <summary>
    /// What the outline was traced for: the aim's cell, its plan, whether the sim would refuse it, and the
    /// cell's structure, conduit and machine; traced again only when one of them changes.
    /// </summary>
    private (int Cell, int? Plan, bool Refused, int Structure, int Conduit, int? Machine)? _traced;
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
    /// <summary>
    /// The harpoon (0044): this hero's rod and the cooldown every harpoon it holds waits for, the line to the
    /// rod, the rod as drawn, and whether the arm aims the gun, so the hero keeps facing the pointer.
    /// </summary>
    private readonly HarpoonShot _harpoon = new();
    private readonly HarpoonWorld _harpoonWorld;
    private HarpoonLine? _line;
    private bool _aimsGun;
    /// <summary>
    /// The drill (0054): whether the arm aims it at the pointer, so the hero keeps facing it, and the world tick
    /// its last hits went on while it runs; none while it does not.
    /// </summary>
    private bool _aimsDrill;
    private DrillPace _drillPace;
    /// <summary>The drill's strikes, heard at its bit through the suit while it works on a piece, apart from its hits (0054).</summary>
    private readonly Heard2D _strike = Cues.Player2D(S.DrillStrike, S.Suit, voices: 2);
    private StrikeTrain _strikes;
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
    /// <summary>Each thrusting hero's time toward its next puff of plume, by peer.</summary>
    private readonly Dictionary<int, double> _plumes = new();
    /// <summary>A puff of plume this often while a jetpack thrusts, a few at most in one frame.</summary>
    private const double PlumeEveryMs = 15;
    private const int PlumeMost = 6;
    /// <summary>Whether the player asked to breathe a canister, from the inventory, since the last frame.</summary>
    private bool _breathe;
    // A canister breathed is heard through the suit.
    private readonly AudioStreamPlayer _breathes = Cues.Player(S.Breathe, S.Suit);

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
        AddChild(_prompt);
        AddChild(_act);
        // The lines and the rod over this machine's hero: they leave its gun's muzzle.
        AddChild(_lines);
        _rod = NewRod();
        foreach (var player in new Node[] { _shot, _drum, _ready, _swingSound, _bite, _breathes, _strike, _whir, _thrustLoop, _othersShot, _othersBite, _othersThump })
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
    /// Moves the hero on by <paramref name="elapsedMs"/> against the client's world <paramref name="m"/>
    /// and draws it, the camera centred on it, and tells the sim where it is when that matters to its
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
        var shown = Predicted.Grid(client, grid);
        var ship = _ship = Ship.Of(grid, m.Rooms, cell => shown.StructureAt(grid, cell));
        var known = m.Crew.HeroOf(client.Id);
        bool passedOut = (known?.Out ?? 0) > 0;
        // A jetpack worn (0047): no tether, and Shift in space thrusts while the suit has air to vent.
        bool jetpack = m.Items.Equipped(client.Id, Content.Jetpack.Item);
        bool canThrust = !passedOut && jetpack && known is { Suit: > 0 };
        // Waking from passing out, the sim has put the hero at the spawn: so does the client.
        if (!_placed || (_wasOut && !passedOut))
        {
            _previous = _current = Hero.At(ship, !_placed && Start != null ? Start(m) : m.Crew.Spawn);
            _placed = true;
        }
        // Passing out it dies where the sim has it (0061), where its body falls: it lies there, not a report behind.
        else if (!_wasOut && passedOut && known!.Cell >= 0 && known.Cell < grid.Width * grid.Height)
            _previous = _current = Hero.At(ship, known.Cell);
        _wasOut = passedOut;
        // The hero faces the pointer's side and looks at it, read where the camera was last frame.
        var pointed = Pointer();
        int? face = Poses.FaceOf(pointed, _current);
        int steps = _clock.Advance(elapsedMs);
        if (steps > 0) _thrust = null;
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
            var input = passedOut
                ? new HeroInput(0, 0, false, Jetpack: jetpack)
                : taken with { Face = faces, Swings = swinging || _aimsGun || _aimsDrill, Pull = pull, Jetpack = jetpack, Thrust = thrust };
            (_previous, _current) = (_current, _current.Step(input, ship));
            _steps++;
        }
        double ownLook = passedOut ? 0 : Poses.LookOf(_current, pointed);
        // Passed out it reports no cell: one landing after it wakes at the pod would put it back out there.
        int tick = client.World.Tick;
        if (passedOut) _reportedAt = tick;
        if (Report.Of(m.Crew, m.Rooms, m.Atmosphere, grid.Width, client.Id, _current, tick, _reportedAt) is { } report)
        {
            client.Send(report);
            _reportedAt = tick;
        }
        bool exerting = !passedOut && Report.Exerting(_current, ship.Space(_current.BodyCell(grid.Width)));
        if (!passedOut && Report.ExertOf(m.Crew, client.Id, exerting) is { } exert) client.Send(exert);
        // Off as Shift is let go, aboard, the jetpack taken off or the suit empty: the sim stops venting it.
        if (Report.ThrustOf(m.Crew, client.Id, _thrust != null) is { } thrusts) client.Send(thrusts);
        HearThrust(elapsedMs);
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
        camera.Position = new Vector2((float)(x / Hero.Unit), (float)((y - Spec.Height / 2.0) / Hero.Unit)) * (float)Rig.Cell;

        // Aimed after following, so the pointer is read where the camera now is.
        var pointer = Pointer();
        var drawn = _current with { X = (int)Math.Round(x), Y = (int)Math.Round(y) };
        _ui.Heroes = [Chest(drawn), .. Others.Select(o => Chest(o.Hero))];
        bool inSpace = Floats = ship.Space(drawn.BodyCell(ship.Width));
        var pose = Poses.PoseOf(drawn, inSpace, passedOut);
        // The hands first, so the frame a hit goes draws the blade meeting the wreck; a swing begun turns the hero.
        UseHands(client, m, shown, pointer, passedOut, pose, elapsedMs, handFree: !HoldsPlan);
        drawn = drawn with { Facing = _current.Facing };
        double look = passedOut ? 0 : Poses.LookOf(_current, pointer);
        _figure.Wear(Suits.Of(m.Crew, client.Id));
        _figure.Jetpack = jetpack;
        _figure.Show(drawn, pose, look, holdsPlan, elapsedMs);
        // Its own thrust shows at once (0014); others' as the sim says.
        Plume(client.Id, _figure, _thrust is { } own ? (own.X, own.Y) : null, inSpace, elapsedMs);
        ShowHarpoon(elapsedMs);
        _tether = Tie(_tether, 0, _current.Anchor, Tether.ClipOf(x, y), elapsedMs);
        // Stamped with its step's time, so others place it by when it was there, not when it came.
        _sender.Step(elapsedMs, new Seen(_current, ownLook, holdsPlan, _wields, _busy, _harpoon.Seen), _steps * Hero.StepMs);
        _tethers.QueueRedraw();
        _lines.QueueRedraw();
    }

    /// <summary>
    /// What the hands do at the pointer: the aim said and outlined while the hero is awake, whatever the
    /// hand holds, doors and valves as <paramref name="shown"/>; E on what it works whatever the hand holds,
    /// so a door opens with a card or a cable out; the right button held works plans whatever the hand
    /// holds (0030); the left button grabs only with a free hand, as it is the card's otherwise, and held
    /// or clicked swings the fire axe toward the pointer while the hand wields it (<see cref="AxeSwing"/>),
    /// unless the click grabbed a piece. Passed out, the hands do nothing: a press is spent, a hold works
    /// no plan and swings no axe.
    /// </summary>
    private void UseHands(Client client, Registry m, ShownGrid shown, (double X, double Y)? pointer, bool passedOut, Pose pose,
        double elapsedMs, bool handFree)
    {
        bool axe = handFree && _ui.Wielded.Get() == Content.Weapons.Axe.Item;
        bool gun = handFree && _ui.Wielded.Get() == Content.Harpoon.Item;
        bool drill = handFree && _ui.Wielded.Get() == Content.Weapons.Drill.Item;
        var aim = pointer is { } p ? Aiming.AimAt(m, client.Id, _current, p, Predicted.Grabbing(client.InFlight), axe, gun, drill) : null;
        var prompt = passedOut ? null : Prompts.Of(m, aim, shown);
        TraceAim(m, shown, prompt != null && aim is { Piece: null } ? aim : null, prompt?.Problem != null);
        _prompt.Show(prompt);
        _prompt.Place(_ui.MenuTop);
        _ui.AimedPiece.Set(prompt != null && aim?.Piece is int piece ? (piece, prompt.Problem != null) : null);

        // The press is taken every frame, so one made out of reach is spent rather than kept. The left
        // button grabs debris too: out in space, with the keys held and the mouse aiming, E is a stretch.
        bool pressed = (_autopilot?.TakePress() ?? false) | TakeUse();
        bool clicked = _mouse.Buttons.TakeClick();
        // With the harpoon's rod out of the gun, a click is the gun's: hooked, it reels, even on a piece in reach.
        bool grab = clicked && aim?.Piece != null && !(gun && _harpoon.Rod != Rod.Seated);
        bool acts = !passedOut && handFree;
        var uses = Aiming.UsesOf(aim, !passedOut && pressed || acts && grab, !passedOut && _mouse.Buttons.HoldsRight, client.World.Tick, _workedOnTick);
        _workedOnTick = uses.WorkedOnTick;
        foreach (var command in uses.Commands) client.Send(command);
        if (uses.Open != null || uses.Commands.Any(c => c is Sim.Debris.Grab or Sim.Atmosphere.LoadCanister)) _act.Play(Floats);
        // Held on a plan it can work, the hero turns its screwdriver at it.
        bool working = !passedOut && _mouse.Buttons.HoldsRight && aim is { InReach: true, Work.Problem: null };
        // A right click with a canister in hand breathes it (0047), unless it works a plan.
        bool rightClicked = _mouse.Buttons.TakeRightClick();
        if (rightClicked && acts && _ui.Wielded.Get() == Content.Crew.Canister && aim is not { InReach: true, Work.Problem: null })
            Breathe(client, m);
        // The axe: a swing begun toward the pointer plays through and, as it strikes, hits every wreck its
        // blade meets, let go or not; put away, it stops unstruck. A wreck's chop is heard where it takes the hit (DebrisView).
        bool grabbed = grab && uses.Commands.Any(c => c is Sim.Debris.Grab);
        _grabHeld = (_grabHeld || grabbed) && _mouse.Buttons.HoldsLeft;
        bool swings = acts && axe && !working && Poses.HoldsIn(pose);
        if (!swings) _swing.Stop();
        bool wanted = swings && pointer != null && !grabbed && !_grabHeld && (_mouse.Buttons.HoldsLeft || clicked);
        var swung = _swing.Advance(elapsedMs, wanted, pointer is { } aimed ? Aiming.PointOf(aimed) : default);
        if (swung.Begun)
        {
            _swingFace = Poses.FaceOf(pointer, _current) ?? _current.Facing;
            _current = _current with { Facing = _swingFace };
        }
        if (swung.Whooshes) _swingSound.Play();
        if (swung.Hits)
            foreach (var hit in Aiming.HitsOf(m, client.Id, _current, _swing.At)) client.Send(hit);
        // The drill: aimed at the pointer; held on the left button it runs, aboard or at nothing too, the sim drawing
        // its power every tick it runs (0055), and hits every wreck its bit touches each Pace ticks. Flat, it winds down.
        bool wieldsDrill = acts && drill && !working && Poses.HoldsIn(pose);
        _aimsDrill = wieldsDrill && pointer != null;
        var running = Motor.Wanted(m.Items, client.Id, _aimsDrill ? Content.Weapons.Drill.Item : null, _mouse.Buttons.HoldsLeft, _grabHeld);
        // Sent before this frame's hits, so the host applies the run first: a hit of the drill needs it.
        if (Motor.RunOf(m.Items, client.Id, running) is { } run) client.Send(run);
        bool runs = running != null;
        Drill(client, m, runs ? pointer : null);
        _figure.Spinning = runs;
        // Heard while its bit is on a piece the sim would let it hit, whatever the hits' pace.
        bool works = runs && Aiming.HitsOf(m, client.Id, Aiming.Touched(m, _current, Aiming.PointOf(pointer!.Value)), Content.Weapons.Drill.Item).Count > 0;
        HearDrill(runs, works, elapsedMs);
        // The harpoon: aimed at the pointer out in space; a fresh click fires, reels or does nothing, never a hold.
        bool wieldsGun = acts && gun && Poses.HoldsIn(pose);
        // Aimed aboard too, though it fires only out in space.
        _aimsGun = wieldsGun && pointer != null;
        var middle = _current.Middle;
        var hold = new GunHold(wieldsGun, _figure.Muzzle is { } muzzle ? Thousandths(muzzle) : null, (_figure.Barrel.X, _figure.Barrel.Y),
            (middle.X, middle.Y), _letsGo);
        _letsGo = false;
        var shot = _harpoon.Step(elapsedMs, clicked && !grabbed, hold, _harpoonWorld);
        if (shot.Fired)
        {
            _shot.Play();
            Puff(_figure, Floats);
        }
        if (shot.Hooked is int hooked) Bite(hooked);
        // The rod biting into the ship's structure rings as metal does.
        if (shot.Gripped) _bite.PlayAt(Pixels(_harpoon.Tip));
        if (shot.Reel is { } reel) client.Send(reel);
        if (shot.Ready) _ready.Play();
        // The drum turns from the press that reels till the piece is in, or while it pulls the hero, till the line comes free.
        if (HarpoonShot.Drums(_harpoon.Rod) != _drum.Playing)
        {
            if (_drum.Playing) _drum.Stop();
            else _drum.Play();
        }
        // Pulled, the gun points along the line at the hook; hanging, at the pointer again, as a wall it hangs flush on would swallow it.
        _figure.Aim = _harpoon.Rod == Rod.Pulling && wieldsGun ? Pixels(_harpoon.Tip)
            : (_aimsGun || _aimsDrill) && pointer is { } at ? new Vector2((float)(at.X * Rig.Cell), (float)(at.Y * Rig.Cell)) : null;
        _figure.Loaded = _harpoon.Ready;
        _wields = passedOut ? null : _ui.Wielded.Get();
        _busy = working ? new Busy(Handwork.Work, Middle(aim!.Cell))
            : _swing.Phase != null ? new Busy(Handwork.Chop, _swing.At, _swing.Count)
            : runs ? new Busy(Handwork.Drill, Aiming.PointOf(pointer!.Value))
            : _beamLeftMs > 0 && !handFree && !passedOut ? new Busy(Handwork.Beam, _beamAt)
            : null;
        ShowHands(_figure, _wields, _busy, _swing.Phase is double phase ? (_swing.At, phase) : null);
        if (uses.Open is int open) _ui.OpenCrate.Set(open);
        // A crate stays open while the hero can reach it, awake.
        if (_ui.OpenCrate.Get() is int opened && (passedOut || !Aiming.CrateInReach(m.Grid, _current, opened))) _ui.OpenCrate.Set(null);
    }

    /// <summary>
    /// The drill running at <paramref name="pointer"/>, in cells, or not at all with none: when its pace says (<see cref="DrillPace"/>),
    /// a hit for each wreck its bit touches that the sim would take.
    /// </summary>
    private void Drill(Client client, Registry m, (double X, double Y)? pointer)
    {
        if (!_drillPace.Due(client.World.Tick, pointer != null, Content.Weapons.Drill.Pace)) return;
        var touched = Aiming.Touched(m, _current, Aiming.PointOf(pointer!.Value));
        foreach (var hit in Aiming.HitsOf(m, client.Id, touched, Content.Weapons.Drill.Item)) client.Send(hit);
    }

    /// <summary>
    /// The drill heard at its bit as last drawn: its motor whenever it <paramref name="runs"/>, spun up and down with its picture
    /// and loaded while on a piece (0059); and its strikes while it <paramref name="works"/> on a piece (0054),
    /// <c>Sound.DrillStrikesPerSecond</c>, each its own pitch and volume, held on <c>Sound.DrillTouchHoldS</c> past the bit leaving
    /// it, so grazing an edge keeps striking; silent the moment the drill stops.
    /// </summary>
    private void HearDrill(bool runs, bool works, double elapsedMs)
    {
        double elapsedS = elapsedMs / 1000;
        bool onPiece = runs && _onPiece.Step(works, elapsedS, S.DrillTouchHoldS);
        var at = _figure.Muzzle ?? _figure.GlobalPosition;
        _drillMotor.Step(runs, onPiece, elapsedS, S.DrillMotor);
        double speed = _drillMotor.Speed(S.DrillMotor);
        int grains = _grains.Due(elapsedMs, _drillMotor.Spin > 0, S.DrillWhirGrainsPerSecond * speed);
        for (int i = 0; i < grains; i++)
        {
            var (spread, _) = StrikeTrain.Vary(_strikeSpread, S.DrillWhirPitch, 0);
            _whir.PlayAt(at, (float)speed * spread, _drillMotor.GainDb(S.DrillMotor));
        }
        int due = _strikes.Due(elapsedMs, onPiece, S.DrillStrikesPerSecond);
        if (!onPiece && _strike.Playing) _strike.Stop();
        for (int i = 0; i < due; i++)
        {
            var (pitch, offsetDb) = StrikeTrain.Vary(_strikeSpread, S.DrillStrikePitch, S.DrillStrikeVolumeDb);
            _strike.PlayAt(at, pitch, offsetDb);
        }
    }

    /// <summary>The jetpack's hiss while this hero thrusts, risen and fallen by its envelope; stopped once fallen.</summary>
    private void HearThrust(double elapsedMs)
    {
        _thrustSound.Step(_thrust != null, elapsedMs / 1000, S.ThrustAttackS, S.ThrustReleaseS);
        if (_thrustSound.Playing && !_thrustLoop.Playing) _thrustLoop.Play();
        else if (!_thrustSound.Playing && _thrustLoop.Playing) _thrustLoop.Stop();
        _thrustLoop.VolumeDb = S.Thrust.VolumeDb + _thrustSound.GainDb;
    }

    /// <summary>
    /// A figure's hands as <paramref name="wields"/> and <paramref name="busy"/> say, the axe swung toward
    /// <paramref name="swing"/>'s point as far as it is through: this machine's hero's and the others' alike.
    /// </summary>
    private static void ShowHands(Figure figure, string? wields, Busy? busy, (Point At, double Phase)? swing)
    {
        figure.Wields = wields;
        figure.Chop(swing is { At: var point, Phase: var phase } ? (Pixels(point), phase) : null);
        figure.Work(busy is { Work: Handwork.Work, At: var plan } ? Pixels(plan) : null);
    }

    /// <summary>The middle of <paramref name="cell"/>, in thousandths of a cell.</summary>
    private Point Middle(int cell) => new(cell % _width * Hero.Unit + Hero.Unit / 2, cell / _width * Hero.Unit + Hero.Unit / 2);

    /// <summary>A point in thousandths of a cell, in the world's pixels.</summary>
    private static Vector2 Pixels(Point at) => new Vector2(at.X, at.Y) * (float)(Rig.Cell / Hero.Unit);

    /// <summary>Outlines <paramref name="aim"/>, green, or red when the sim would <paramref name="refuse"/>; nothing with none.</summary>
    private void TraceAim(Registry m, ShownGrid shown, Aim? aim, bool refuse)
    {
        var grid = m.Grid;
        var traced = aim is { } a
            ? (a.Cell, a.Work?.Plan, refuse, shown.StructureAt(grid, a.Cell), shown.ConduitAt(grid, a.Cell), grid.MachineAt(a.Cell)?.Id)
            : ((int, int?, bool, int, int, int?)?)null;
        if (traced == _traced) return;
        _traced = traced;
        _outline.Begin();
        if (aim != null) AimOutline.Trace(_outline, m, aim, shown, refuse ? AimRefused : AimReady);
        _outline.End();
    }

    /// <summary>
    /// Every other hero heard from lately, in its suit, posed by how it moves and whether it is out, its
    /// hands wielding and working as sent, the axe swung once for each swing sent (<see cref="AxeSwing.Follow"/>);
    /// those gone, hidden.
    /// </summary>
    private void ShowOthers(Ship ship, Registry m, double elapsedMs)
    {
        var crew = m.Crew;
        var others = _remote.At(_clockMs);
        foreach (var (id, shown) in _shownOthers)
            if (!others.Any(o => o.Id == id))
            {
                shown.Figure.Visible = false;
                (shown.Tether, shown.Busy) = (null, null);
                shown.Swing.Stop();
                ShowRod(id, shown, null, false, elapsedMs);
            }
        foreach (var (id, seen) in others)
        {
            if (!_shownOthers.TryGetValue(id, out var shown))
            {
                var drum = Cues.Player2D(S.Drum, S.World);
                AddChild(drum);
                shown = _shownOthers[id] = new Other(new Figure(Suits.Of(crew, id), own: false), NewRod(), drum);
                _others.AddChild(shown.Figure);
            }
            var hero = seen.Hero;
            bool out_ = (crew.HeroOf(id)?.Out ?? 0) > 0;
            shown.Figure.Visible = true;
            shown.Figure.Wear(Suits.Of(crew, id));
            var (axe, chop) = _heardChops.GetValueOrDefault(id);
            if (!axe) shown.Swing.Stop();
            shown.Swing.Follow(elapsedMs, axe ? chop : null);
            ShowHands(shown.Figure, seen.Wields, seen.Busy, shown.Swing.Phase is double phase ? (shown.Swing.At, phase) : null);
            // A friend's drill running points where it drills, its harpoon where its rod flies or sticks, else
            // either hangs; the drill spins while the sim says it runs.
            var rodAim = shown.Rod.AimAt(seen.Rod, PieceDrawnAt);
            shown.Figure.Aim = seen.Busy is { Work: Handwork.Drill, At: var drilled } ? Pixels(drilled) : rodAim is { } aimed ? Pixels(aimed) : null;
            shown.Figure.Spinning = m.Items.Running(id) == Content.Weapons.Drill.Item;
            if (Busy.BeamsAnew(shown.Busy, seen.Busy)) shown.Figure.Beam([Pixels(seen.Busy!.Value.At)]);
            shown.Figure.Loaded = shown.Rod.Loaded;
            shown.Figure.Jetpack = m.Items.Equipped(id, Content.Jetpack.Item);
            bool afloat = ship.Space(hero.BodyCell(ship.Width));
            shown.Figure.Show(hero, Poses.PoseOf(hero, afloat, out_), out_ ? 0 : seen.Look ?? 0,
                seen.HoldsPlan, elapsedMs);
            // Its thrust is not sent: the plume blows against the way it moves, down while it is still.
            Plume(id, shown.Figure, crew.HeroOf(id)?.Thrusting == true ? (hero.Vx, hero.Vy) : null, afloat, elapsedMs);
            ShowRod(id, shown, seen.Rod, afloat, elapsedMs);
            (shown.Tether, shown.Busy) = (Tie(shown.Tether, id, hero.Anchor, Tether.ClipOf(hero.X, hero.Y), elapsedMs), seen.Busy);
        }
        Others = others.Select(o => (o.Id, o.Seen.Hero)).ToList();
    }

    /// <summary>
    /// A jetpack's plume (0047): while a hero thrusts toward <paramref name="toward"/>, puffs of grey air from
    /// its figure's nozzle, the harpoon's puff (<see cref="Effect.Puff"/>), blown the other way, down with no way.
    /// </summary>
    private void Plume(int peer, Figure figure, (double X, double Y)? toward, bool inSpace, double elapsedMs)
    {
        if (toward is not var (tx, ty) || figure.Nozzle is not { } at)
        {
            _plumes.Remove(peer);
            return;
        }
        double carry = _plumes.GetValueOrDefault(peer) + elapsedMs;
        int puffs = (int)(carry / PlumeEveryMs);
        _plumes[peer] = carry - puffs * PlumeEveryMs;
        if (puffs == 0) return;
        double heading = tx == 0 && ty == 0 ? Math.PI / 2 : Math.Atan2(-ty, -tx);
        Particles.Shared.Emit(inSpace ? Effect.PuffInSpace : Effect.Puff, at.X, at.Y, Math.Min(puffs, PlumeMost), heading, inSpace);
    }

    /// <summary>
    /// Breathes a canister into the suit (0047): sent, the hiss heard, when the sim would take it now and
    /// none is in flight; a full suit or no canister does nothing.
    /// </summary>
    private void Breathe(Client client, Registry m)
    {
        foreach (var c in client.InFlight)
            if (c is Refill) return;
        if (m.Crew.RefillProblem(client.Id) != null) return;
        client.Send(new Refill());
        _breathes.Play();
    }

    /// <summary>
    /// Another hero's harpoon as it sends it (<see cref="RemoteRod"/>, 0065), from its gun's muzzle as just drawn:
    /// its rod and line drawn as this hero's; heard where it is, on World, as its steps are: the shot and its puff
    /// as the rod leaves, the bite where it sticks (the chop, or the glove's thump on a crate or pod), the drum
    /// at the gun while it reels or is pulled.
    /// </summary>
    private void ShowRod(int peer, Other shown, RodSeen? sent, bool afloat, double elapsedMs)
    {
        var figure = shown.Figure;
        var rod = shown.Rod;
        var cues = rod.Step(elapsedMs, sent, figure.Muzzle is { } muzzle ? Thousandths(muzzle) : null, PieceDrawnAt);
        if (cues.Fired && figure.Muzzle is { } gun)
        {
            _othersShot.PlayAt(gun);
            Puff(figure, afloat);
        }
        if (cues.Bit)
        {
            if (rod.Piece is int id && PieceOf(id) is { } piece && Thumps(piece)) _othersThump.PlayAt(Pixels(rod.Tip));
            else _othersBite.PlayAt(Pixels(rod.Tip));
        }
        bool drums = rod.Drums && figure.Muzzle != null;
        if (drums) shown.Drum.GlobalPosition = figure.Muzzle!.Value;
        if (drums != shown.Drum.Playing)
        {
            if (drums) shown.Drum.Play();
            else shown.Drum.Stop();
        }
        DrawRod(rod.Rod, rod.Tip, rod.Heading, figure.Muzzle, ref shown.Line, shown.RodPicture, peer, elapsedMs);
    }

    /// <summary>A point in thousandths of a cell, in the world's pixels.</summary>
    private static Vector2 Pixels((double X, double Y) at) => new((float)(at.X / Hero.Unit * Rig.Cell), (float)(at.Y / Hero.Unit * Rig.Cell));

    /// <summary>A point in the world's pixels, in thousandths of a cell.</summary>
    private static (double X, double Y) Thousandths(Vector2 at) => (at.X / Rig.Cell * Hero.Unit, at.Y / Rig.Cell * Hero.Unit);

    /// <summary>The rod's eye this far behind its point, in thousandths of a cell, as its picture is drawn.</summary>
    private static readonly double RodLength = (RodLook.Tip.X - RodLook.Eye.X) / Rig.Cell * Hero.Unit;

    /// <summary>This hero's rod and line as the harpoon has them this frame, from the muzzle the figure was just drawn with.</summary>
    private void ShowHarpoon(double elapsedMs) =>
        DrawRod(_harpoon.Rod, _harpoon.Tip, _harpoon.Heading, _figure.Muzzle, ref _line, _rod, 0, elapsedMs);

    /// <summary>
    /// A rod and its line where <paramref name="rod"/> says, its point at <paramref name="tip"/>, from a gun's
    /// <paramref name="muzzle"/> as just drawn: the rod pointing the way it flew, <paramref name="heading"/>, or, reeled
    /// or winding back, away from the gun along the line; the line slack in flight and hooked, taut reeling, pulling,
    /// hanging and winding back. Nothing while the rod is in the gun. This hero's and the others' alike.
    /// </summary>
    private static void DrawRod(Rod rod, (double X, double Y) tip, (double X, double Y) heading, Vector2? muzzle,
        ref HarpoonLine? line, Sprite2D picture, int seed, double elapsedMs)
    {
        if (rod == Rod.Seated || muzzle is not { } at)
        {
            (line, picture.Visible) = (null, false);
            return;
        }
        var gun = Thousandths(at);
        var pointing = heading;
        double far = Math.Sqrt((tip.X - gun.X) * (tip.X - gun.X) + (tip.Y - gun.Y) * (tip.Y - gun.Y));
        // Stuck in the structure it keeps the way it flew in; riding a piece or winding back, it points along the line.
        if (rod is Rod.Reeling or Rod.Returning && far > 1) pointing = ((tip.X - gun.X) / far, (tip.Y - gun.Y) / far);
        var eye = (tip.X - pointing.X * RodLength, tip.Y - pointing.Y * RodLength);
        line ??= new HarpoonLine(gun, eye, seed);
        line.Update(gun, eye, HarpoonShot.Taut(rod), elapsedMs);
        picture.Visible = true;
        picture.Position = Pixels(eye);
        picture.Rotation = (float)Math.Atan2(pointing.Y, pointing.X);
    }

    /// <summary>
    /// The rod biting into piece <paramref name="id"/> as it sticks (0042): metal, a wreck or salvage, heard as an
    /// axe's chop where it struck; a crate or a pod as the glove's thump.
    /// </summary>
    private void Bite(int id)
    {
        if (PieceOf(id) is not { } piece) return;
        if (Thumps(piece)) _act.Play(Floats);
        else _bite.PlayAt(Pixels(_harpoon.Tip));
    }

    /// <summary>Whether a rod biting into <paramref name="piece"/> thumps as a glove does, a crate or a pod, rather than ringing as metal.</summary>
    private static bool Thumps(Piece piece) => Content.Loot.Of(piece.Item) != null && Content.Wreckage.HpOf(piece.Item) == null;

    /// <summary>The spring's breath leaving <paramref name="figure"/>'s muzzle with the rod, along its barrel.</summary>
    private static void Puff(Figure figure, bool inSpace)
    {
        if (figure.Muzzle is not { } at) return;
        foreach (var (effect, count, heading) in HarpoonShot.Puff((figure.Barrel.X, figure.Barrel.Y), inSpace))
            Particles.Shared.Emit(effect, at.X, at.Y, count, heading, inSpace);
    }

    private void PaintLines(Sketch g)
    {
        foreach (var shown in _shownOthers.Values)
            if (shown.Line != null) PaintLine(g, shown.Line);
        if (_line != null) PaintLine(g, _line);
    }

    private void PaintLine(Sketch g, HarpoonLine line)
    {
        _linePoints.Clear();
        line.DrawnInto(_linePoints);
        for (int i = 0; i < _linePoints.Count; i++) _linePoints[i] = (_linePoints[i].X / Hero.Unit, _linePoints[i].Y / Hero.Unit);
        g.Rope(_linePoints, HarpoonRope, 1);
    }

    /// <summary>The world point under the pointer, in cells, or the autopilot's while it points; none while the mouse is off the window.</summary>
    private (double X, double Y)? Pointer() => _autopilot?.PointerFor(_current) ?? (_ui.MenuOpen.Get() ? null : _mouse.Pointer());

    /// <summary>A hero's tether tied at <paramref name="anchor"/>, anew wherever it is tied; none aboard.</summary>
    private static Tether? Tie(Tether? line, int seed, Point? anchor, (double X, double Y) clip, double elapsedMs)
    {
        if (anchor is not Point a) return null;
        if (line?.Anchor != a) line = new Tether(a, clip, seed);
        line.Update((a.X, a.Y), clip, elapsedMs);
        return line;
    }

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
        // Keys held as the game menu opened are not walked on behind it.
        if (_ui.MenuOpen.Get())
        {
            _jump = false;
            return new HeroInput(0, 0, false);
        }
        var input = new HeroInput(
            Axis(Held(Key.A, Key.Left), Held(Key.D, Key.Right)),
            Axis(Held(Key.W, Key.Up), Held(Key.S, Key.Down)),
            _jump,
            Sprint: Input.IsPhysicalKeyPressed(Key.Shift));
        _jump = false;
        return input;
    }

    private bool TakeUse()
    {
        bool pressed = _use;
        _use = false;
        return pressed;
    }

    private void PaintTethers(Sketch g)
    {
        foreach (var shown in _shownOthers.Values) PaintTether(g, shown.Tether);
        PaintTether(g, _tether);
    }

    private void PaintTether(Sketch g, Tether? line)
    {
        if (line == null) return;
        _tetherPoints.Clear();
        line.DrawnInto(_tetherPoints);
        // Thousandths of a cell to cells, which the sketch traces ropes in.
        for (int i = 0; i < _tetherPoints.Count; i++) _tetherPoints[i] = (_tetherPoints[i].X / Hero.Unit, _tetherPoints[i].Y / Hero.Unit);
        g.Rope(_tetherPoints, Line, 1);
        g.DrawCircle(new Vector2(line.Anchor.X, line.Anchor.Y) * (float)(Rig.Cell / Hero.Unit), 1.5f, Line);
    }
}
