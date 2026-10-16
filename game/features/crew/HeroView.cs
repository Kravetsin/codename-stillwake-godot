using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Crew;
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
/// <c>Work</c> for every tick released; with the fire axe wielded, clicked or held it swings toward the pointer, hitting the wrecks its blade meets (0039). With an empty hand, what E would work is outlined along its own
/// pictures (<see cref="AimOutline"/>), and a prompt says what E does.
/// </summary>
public partial class HeroView : Node2D
{
    private static readonly Color AimReady = new("7fe08a"), AimRefused = new("e05555");
    /// <summary>A safety line's yellow, pale enough not to shout across the dark.</summary>
    private static readonly Color Line = new(new Color("d9c27a"), 0.9f);

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
    /// <summary>
    /// Another hero as drawn: its figure, its tether while it has one, what its hands were busy with last
    /// frame, and the swings of its axe as replayed here, each at the point sent with it.
    /// </summary>
    private sealed class Other(Figure figure)
    {
        public readonly Figure Figure = figure;
        public Tether? Tether;
        public Busy? Busy;
        public AxeSwing Swing;
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
    private double _clockMs;
    /// <summary>The blueprints delivered to in this hold of the left button: a sweep delivers to each it passes once.</summary>
    private readonly HashSet<int> _served = [];
    private int _workedOnTick = -1;
    /// <summary>
    /// This hero's axe swinging, and the side it faces while it does, toward the swing's point as it began
    /// (0039); whether the left button's hold began with a grab, which swings nothing.
    /// </summary>
    private AxeSwing _swing;
    private int _swingFace = 1;
    private bool _grabHeld;

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
        _mouse = mouse;
        _autopilot = autopilot;
        _sender = new HeroSender(hero => _sendTo?.SendHero(hero.Encode()));
        // Others behind this machine's hero, every tether behind them both (Layers).
        AddChild(_tethers);
        AddChild(_others);
        AddChild(_figure);
        AddChild(_outline);
        AddChild(_prompt);
        AddChild(_act);
        _tethers.Paint = PaintTethers;
    }

    /// <summary>The hero after its latest step: what the sim learns of where it is.</summary>
    public Hero Current => _current;

    /// <summary>Whether the hero stands in the world yet: until its first frame in a session, <see cref="Current"/> is nowhere.</summary>
    public bool Placed => _placed;

    /// <summary>Whether the hero floats out in space, weightless, as drawn this frame.</summary>
    public bool Floats { get; private set; }

    /// <summary>Whether this machine's hero holds the plan in its hands, drawn and sent to the others: the hand not free (0022).</summary>
    public bool HoldsPlan => _ui.Hand.Get() != Holding.Free;

    /// <summary>Where <paramref name="hero"/>'s chest is, in the world's pixels.</summary>
    private static (float X, float Y) Chest(Hero hero) =>
        ((float)(hero.X * Rig.Cell / Hero.Unit), (float)((hero.Y - Spec.Height / 2.0) * Rig.Cell / Hero.Unit));

    /// <summary>Other heroes where they are drawn this frame: what sways the cables besides this one.</summary>
    public IReadOnlyList<Hero> Others { get; private set; } = [];

    /// <summary>
    /// Moves the hero on by <paramref name="elapsedMs"/> against the client's world <paramref name="m"/>
    /// and draws it, the camera centred on it, and tells the sim where it is when that matters to its
    /// breathing (<see cref="Report"/>). The hero wakes at the crew's spawn, and again there on coming
    /// round, and takes no orders while passed out. <paramref name="heard"/> are the other heroes the
    /// client took since the last frame, each drawn where it stood a little while ago.
    /// </summary>
    public void Frame(Client client, Registry m, Camera2D camera, IReadOnlyList<PeerHeroMessage> heard, double elapsedMs)
    {
        var grid = m.Grid;
        _width = grid.Width;
        // Doors as this peer shows them: its own toggles still in flight already open or shut.
        var shown = Predicted.Grid(client, grid);
        var ship = Ship.Of(grid, m.Rooms, cell => shown.StructureAt(grid, cell));
        bool passedOut = (m.Crew.HeroOf(client.Id)?.Out ?? 0) > 0;
        // Waking from passing out, the sim has put the hero at the spawn: so does the client.
        if (!_placed || (_wasOut && !passedOut))
        {
            _previous = _current = Hero.At(ship, m.Crew.Spawn);
            _placed = true;
        }
        _wasOut = passedOut;
        // The hero faces the pointer's side and looks at it, read where the camera was last frame.
        var pointed = Pointer();
        int? face = Poses.FaceOf(pointed, _current);
        for (int due = _clock.Advance(elapsedMs); due > 0; due--)
        {
            // Passed out, the hero takes no orders and only falls; keys pressed meanwhile are spent.
            var taken = _autopilot?.Next(_current) ?? Take();
            // Swinging, it faces where the swing began toward, swimming too: the blade comes down there.
            bool swinging = _swing.Phase != null;
            var input = passedOut ? new HeroInput(0, 0, false) : taken with { Face = swinging ? _swingFace : face, Swings = swinging };
            (_previous, _current) = (_current, _current.Step(input, ship));
            _steps++;
        }
        double ownLook = passedOut ? 0 : Poses.LookOf(_current, pointed);
        if (Report.Of(m.Crew, m.Rooms, m.Atmosphere, grid.Width, client.Id, _current) is { } report) client.Send(report);
        bool exerting = !passedOut && Report.Exerting(_current, ship.Space(_current.BodyCell(grid.Width)));
        if (!passedOut && Report.ExertOf(m.Crew, client.Id, exerting) is { } exert) client.Send(exert);

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
                _heardChops[message.From] = (sent.Seen.Wields == Content.Axe.Item, Busy.Chopping(last, sent.Seen.Busy));
            }
        ShowOthers(ship, m.Crew, elapsedMs);

        double between = _clock.Progress;
        double x = _previous.X + (_current.X - _previous.X) * between;
        double y = _previous.Y + (_current.Y - _previous.Y) * between;
        camera.Position = new Vector2((float)(x / Hero.Unit), (float)((y - Spec.Height / 2.0) / Hero.Unit)) * (float)Rig.Cell;

        // Aimed after following, so the pointer is read where the camera now is.
        var pointer = Pointer();
        var drawn = _current with { X = (int)Math.Round(x), Y = (int)Math.Round(y) };
        _ui.Heroes = [Chest(drawn), .. Others.Select(Chest)];
        bool inSpace = Floats = ship.Space(drawn.BodyCell(ship.Width));
        var pose = Poses.PoseOf(drawn, inSpace, passedOut);
        // The hands first, so the frame a hit goes draws the blade meeting the wreck; a swing begun turns the hero.
        UseHands(client, m, shown, pointer, passedOut, pose, elapsedMs, handFree: !HoldsPlan);
        drawn = drawn with { Facing = _current.Facing };
        double look = passedOut ? 0 : Poses.LookOf(_current, pointer);
        _figure.Wear(Suits.Of(m.Crew, client.Id));
        _figure.Show(drawn, pose, look, holdsPlan, elapsedMs);
        _tether = Tie(_tether, 0, _current.Anchor, Tether.ClipOf(x, y), elapsedMs);
        // Stamped with its step's time, so others place it by when it was there, not when it came.
        _sender.Step(elapsedMs, new Seen(_current, ownLook, holdsPlan, _wields, _busy), _steps * Hero.StepMs);
        _tethers.QueueRedraw();
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
        if (!_mouse.Buttons.HoldsRight) _served.Clear();
        bool axe = handFree && _ui.Wielded.Get() == Content.Axe.Item;
        var aim = pointer is { } p ? Aiming.AimAt(m, client.Id, _current, p, Predicted.Grabbing(client.InFlight), _served, axe) : null;
        var prompt = passedOut ? null : Prompts.Of(m, aim, shown);
        TraceAim(m, shown, prompt != null && aim is { Piece: null } ? aim : null, prompt?.Problem != null);
        _prompt.Show(prompt);
        _prompt.Place(_ui.MenuTop);
        _ui.AimedPiece.Set(prompt != null && aim?.Piece is int piece ? (piece, prompt.Problem != null) : null);

        // The press is taken every frame, so one made out of reach is spent rather than kept. The left
        // button grabs debris too: out in space, with the keys held and the mouse aiming, E is a stretch.
        bool pressed = (_autopilot?.TakePress() ?? false) | TakeUse();
        bool clicked = _mouse.Buttons.TakeClick();
        bool grab = clicked && aim?.Piece != null;
        bool acts = !passedOut && handFree;
        var uses = Aiming.UsesOf(aim, !passedOut && pressed || acts && grab, !passedOut && _mouse.Buttons.HoldsRight, client.World.Tick, _workedOnTick);
        _workedOnTick = uses.WorkedOnTick;
        if (uses.Served is int served) _served.Add(served);
        foreach (var command in uses.Commands) client.Send(command);
        if (uses.Open != null || uses.Commands.Any(c => c is Sim.Debris.Grab or Sim.Atmosphere.LoadCanister)) _act.Play(Floats);
        // Held on a plan it can work, the hero turns its screwdriver at it.
        bool working = !passedOut && _mouse.Buttons.HoldsRight && aim is { InReach: true, Work.Problem: null };
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
        if (swung.Hits)
            foreach (var hit in Aiming.HitsOf(m, client.Id, _current, _swing.At)) client.Send(hit);
        _wields = passedOut ? null : _ui.Wielded.Get();
        _busy = working ? new Busy(Handwork.Work, Middle(aim!.Cell))
            : _swing.Phase != null ? new Busy(Handwork.Chop, _swing.At, _swing.Count)
            : _beamLeftMs > 0 && !handFree && !passedOut ? new Busy(Handwork.Beam, _beamAt)
            : null;
        ShowHands(_figure, _wields, _busy, _swing.Phase is double phase ? (_swing.At, phase) : null);
        if (uses.Open is int open) _ui.OpenCrate.Set(open);
        // A crate stays open while the hero can reach it, awake.
        if (_ui.OpenCrate.Get() is int opened && (passedOut || !Aiming.CrateInReach(m.Grid, _current, opened))) _ui.OpenCrate.Set(null);
    }

    /// <summary>
    /// A figure's hands as <paramref name="wields"/> and <paramref name="busy"/> say, the axe swung toward
    /// <paramref name="swing"/>'s point as far as it is through: this machine's hero's and the others' alike.
    /// </summary>
    private static void ShowHands(Figure figure, string? wields, Busy? busy, (Point At, double Phase)? swing)
    {
        figure.Wields = wields == Content.Axe.Item;
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
    private void ShowOthers(Ship ship, ICrewQueries crew, double elapsedMs)
    {
        var others = _remote.At(_clockMs);
        foreach (var (id, shown) in _shownOthers)
            if (!others.Any(o => o.Id == id))
            {
                shown.Figure.Visible = false;
                (shown.Tether, shown.Busy) = (null, null);
                shown.Swing.Stop();
            }
        foreach (var (id, seen) in others)
        {
            if (!_shownOthers.TryGetValue(id, out var shown))
            {
                shown = _shownOthers[id] = new Other(new Figure(Suits.Of(crew, id), own: false));
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
            if (Busy.BeamsAnew(shown.Busy, seen.Busy)) shown.Figure.Beam([Pixels(seen.Busy!.Value.At)]);
            shown.Figure.Show(hero, Poses.PoseOf(hero, ship.Space(hero.BodyCell(ship.Width)), out_), out_ ? 0 : seen.Look ?? 0,
                seen.HoldsPlan, elapsedMs);
            (shown.Tether, shown.Busy) = (Tie(shown.Tether, id, hero.Anchor, Tether.ClipOf(hero.X, hero.Y), elapsedMs), seen.Busy);
        }
        Others = others.Select(o => o.Seen.Hero).ToList();
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
