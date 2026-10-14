using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Other players' heroes, as their peers send them through the host, drawn a little behind so they move
/// smoothly between sends (<see cref="RemoteHeroes"/>); and this machine's hero (features/crew/index.ts):
/// moved by the keyboard against the ship's cells, doors as this peer shows them (0014), outside the
/// sim, at the controller's 60 steps a second; drawn between its last two steps, so it moves smoothly at
/// any refresh rate, with its tether out in space; followed by the camera; sent to the others about 20
/// times a second. Every hero in its suit's colour. E under the pointer, within reach, toggles a door
/// or valve, outlined while it would.
/// </summary>
public partial class HeroView : Node2D
{
    private static readonly Color AimReady = new("7fe08a"), AimRefused = new("e05555");
    /// <summary>A safety line's yellow, pale enough not to shout across the dark.</summary>
    private static readonly Color Line = new(new Color("d9c27a"), 0.9f);

    private readonly Figure _figure = new(Suits.All[0]);
    private readonly Clock _clock = new(Hero.StepMs);
    private readonly HeroAutopilot? _autopilot;
    private readonly RemoteHeroes _remote = new();
    private readonly HeroSender _sender;
    private readonly Node2D _others = new();
    /// <summary>Each other hero's figure, and its tether while it has one, by peer id.</summary>
    private readonly Dictionary<int, (Figure Figure, Tether? Tether)> _shownOthers = new();
    private Client? _sendTo;
    private Hero _previous, _current;
    private bool _placed, _wasOut, _jump, _use;
    private Tether? _tether;
    private Aim? _aim;
    private int _width, _steps;
    private double _clockMs;

    /// <param name="autopilot">A script to play the hero by instead of the keyboard, for recordings.</param>
    public HeroView(HeroAutopilot? autopilot = null)
    {
        _autopilot = autopilot;
        _sender = new HeroSender(hero => _sendTo?.SendHero(hero.Encode()));
        // Others behind this machine's hero.
        AddChild(_others);
        AddChild(_figure);
    }

    /// <summary>The hero after its latest step: what the sim learns of where it is.</summary>
    public Hero Current => _current;

    /// <summary>Whether this machine's hero holds the plan in its hands, drawn and sent to the others: the hand not free.</summary>
    public bool HoldsPlan { get; set; }

    /// <summary>Other heroes where they are drawn this frame: what sways the cables besides this one.</summary>
    public IReadOnlyList<Hero> Others { get; private set; } = [];

    /// <summary>
    /// Moves the hero on by <paramref name="elapsedMs"/> against the client's world and draws it, the
    /// camera centred on it, and tells the sim where it is when that matters to its breathing
    /// (<see cref="Report"/>). The hero wakes at the crew's spawn, and again there on coming round, and
    /// takes no orders while passed out. <paramref name="heard"/> are the other heroes the client took
    /// since the last frame, each drawn where it stood a little while ago.
    /// </summary>
    public void Frame(
        Client client, IGridQueries grid, IRoomsQueries rooms, IAtmosphereQueries air, ICrewQueries crew, Camera2D camera,
        IReadOnlyList<PeerHeroMessage> heard, double elapsedMs)
    {
        // Doors as this peer shows them: its own toggles still in flight already open or shut.
        var shown = ShownGrid.Of(client, grid);
        var ship = Ship.Of(grid, rooms, cell => shown.StructureAt(grid, cell));
        _width = grid.Width;
        bool passedOut = (crew.HeroOf(client.Id)?.Out ?? 0) > 0;
        // Waking from passing out, the sim has put the hero at the spawn: so does the client.
        if (!_placed || (_wasOut && !passedOut))
        {
            _previous = _current = Hero.At(ship, crew.Spawn);
            _placed = true;
        }
        _wasOut = passedOut;
        // The hero faces the pointer's side and looks at it, read where the camera was last frame.
        var pointed = Pointer(camera);
        int? face = Poses.FaceOf(pointed, _current);
        for (int due = _clock.Advance(elapsedMs); due > 0; due--)
        {
            // Passed out, the hero takes no orders and only falls; keys pressed meanwhile are spent.
            var taken = _autopilot?.Next(_current) ?? Take();
            var input = passedOut ? new HeroInput(0, 0, false) : taken with { Face = face };
            (_previous, _current) = (_current, _current.Step(input, ship));
            _steps++;
        }
        double ownLook = passedOut ? 0 : Poses.LookOf(_current, pointed);
        if (Report.Of(crew, rooms, air, grid.Width, client.Id, _current) is { } report) client.Send(report);

        // Others' heroes in, this one out: through the host, never through the simulation. Stamped with
        // its step's time, so others place it by when it was there, not when it came.
        _clockMs += elapsedMs;
        _sendTo = client;
        _sender.Step(elapsedMs, new Seen(_current, ownLook, HoldsPlan && !passedOut), _steps * Hero.StepMs);
        foreach (var message in heard)
            if (SentHero.Decode(message.State) is { } sent) _remote.Add(message.From, sent, _clockMs);
        ShowOthers(ship, crew, elapsedMs);

        double between = _clock.Progress;
        double x = _previous.X + (_current.X - _previous.X) * between;
        double y = _previous.Y + (_current.Y - _previous.Y) * between;
        camera.Position = new Vector2((float)(x / Hero.Unit), (float)((y - Spec.Height / 2.0) / Hero.Unit)) * (float)Rig.Cell;

        // Aimed after following, so the pointer is read where the camera now is.
        var pointer = Pointer(camera);
        var drawn = _current with { X = (int)Math.Round(x), Y = (int)Math.Round(y) };
        bool inSpace = ship.Space(drawn.BodyCell(ship.Width));
        double look = passedOut ? 0 : Poses.LookOf(_current, pointer);
        _figure.Wear(Suits.Of(crew, client.Id));
        _figure.Show(drawn, Poses.PoseOf(drawn, inSpace, passedOut), look, HoldsPlan && !passedOut, elapsedMs);
        _tether = Tie(_tether, 0, _current.Anchor, Tether.ClipOf(x, y), elapsedMs);

        // The press is taken every frame, so one made out of reach is spent rather than kept.
        bool pressed = (_autopilot?.TakePress() ?? false) | TakeUse();
        _aim = pointer is { } p && !passedOut ? Aiming.AimAt(grid, _current, p) : null;
        if (Aiming.UseOf(_aim, pressed && !passedOut) is { } use) client.Send(use);
        QueueRedraw();
    }

    /// <summary>Every other hero heard from lately, in its suit, posed by how it moves and whether it is out; those gone, hidden.</summary>
    private void ShowOthers(Ship ship, ICrewQueries crew, double elapsedMs)
    {
        var others = _remote.At(_clockMs);
        foreach (var (id, shown) in _shownOthers.ToList())
            if (!others.Any(o => o.Id == id))
            {
                shown.Figure.Visible = false;
                _shownOthers[id] = (shown.Figure, null);
            }
        foreach (var (id, seen) in others)
        {
            if (!_shownOthers.TryGetValue(id, out var shown))
            {
                shown = (new Figure(Suits.Of(crew, id)), null);
                _others.AddChild(shown.Figure);
            }
            var hero = seen.Hero;
            bool out_ = (crew.HeroOf(id)?.Out ?? 0) > 0;
            shown.Figure.Visible = true;
            shown.Figure.Wear(Suits.Of(crew, id));
            shown.Figure.Show(hero, Poses.PoseOf(hero, ship.Space(hero.BodyCell(ship.Width)), out_), out_ ? 0 : seen.Look ?? 0,
                seen.HoldsPlan, elapsedMs);
            _shownOthers[id] = (shown.Figure, Tie(shown.Tether, id, hero.Anchor, Tether.ClipOf(hero.X, hero.Y), elapsedMs));
        }
        Others = others.Select(o => o.Seen.Hero).ToList();
    }

    /// <summary>The world point under the mouse, in cells, or the autopilot's; none while the mouse is off the window.</summary>
    private (double X, double Y)? Pointer(Camera2D camera)
    {
        if (_autopilot != null) return _autopilot.PointerFor(_current);
        var viewport = GetViewport();
        var mouse = viewport.GetMousePosition();
        if (!viewport.GetVisibleRect().HasPoint(mouse)) return null;
        var at = camera.GetGlobalMousePosition() / (float)Rig.Cell;
        return (at.X, at.Y);
    }

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
        var input = new HeroInput(
            Axis(Held(Key.A, Key.Left), Held(Key.D, Key.Right)),
            Axis(Held(Key.W, Key.Up), Held(Key.S, Key.Down)),
            _jump);
        _jump = false;
        return input;
    }

    private bool TakeUse()
    {
        bool pressed = _use;
        _use = false;
        return pressed;
    }

    public override void _Draw()
    {
        // Behind the heroes: what E would work, then every tether, unlit, a line to find the way back by.
        if (_aim is { InReach: true, Press: { } press })
        {
            float cell = (float)Rig.Cell;
            var at = new Vector2(_aim.Cell % _width, _aim.Cell / _width) * cell;
            DrawRect(new Rect2(at + Vector2.One, new Vector2(cell - 2, cell - 2)), press.Problem == null ? AimReady : AimRefused, false, 2);
        }
        foreach (var line in _shownOthers.Values.Select(s => s.Tether).Append(_tether))
        {
            if (line == null) continue;
            float scale = (float)(Rig.Cell / Hero.Unit);
            var points = line.Trace(scale).Select(p => new Vector2((float)p.X, (float)p.Y)).ToArray();
            DrawPolyline(points, Line, 1, true);
            DrawCircle(new Vector2(line.Anchor.X, line.Anchor.Y) * scale, 1.5f, Line);
        }
    }
}
