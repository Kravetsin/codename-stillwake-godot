using System;
using System.Linq;
using Godot;
using Stillwake.Net;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// This machine's hero (apps/game/src/features/crew/index.ts): moved by the keyboard against the ship's
/// cells, outside the sim, at the controller's 60 steps a second; drawn between its last two steps, so it
/// moves smoothly at any refresh rate, with its tether out in space; followed by the camera. E under the
/// pointer, within reach, toggles a door or valve, outlined while it would.
/// </summary>
public partial class HeroView : Node2D
{
    private static readonly Color AimReady = new("7fe08a"), AimRefused = new("e05555");
    /// <summary>A safety line's yellow, pale enough not to shout across the dark.</summary>
    private static readonly Color Line = new(new Color("d9c27a"), 0.9f);

    private readonly Figure _figure = new();
    private readonly Clock _clock = new(Hero.StepMs);
    private readonly HeroAutopilot? _autopilot;
    private Hero _previous, _current;
    private bool _placed, _wasOut, _jump, _use;
    private Tether? _tether;
    private Aim? _aim;
    private int _width;

    /// <param name="autopilot">A script to play the hero by instead of the keyboard, for recordings.</param>
    public HeroView(HeroAutopilot? autopilot = null)
    {
        _autopilot = autopilot;
        AddChild(_figure);
    }

    /// <summary>The hero after its latest step: what the sim learns of where it is, once crew is ported.</summary>
    public Hero Current => _current;

    /// <summary>
    /// Moves the hero on by <paramref name="elapsedMs"/> against the client's world and draws it, the
    /// camera centred on it. The crew module's seams: the hero wakes at <paramref name="spawn"/>, and
    /// again there on coming round, and takes no orders while <paramref name="passedOut"/>.
    /// </summary>
    public void Frame(Client client, IGridQueries grid, IRoomsQueries rooms, Camera2D camera, int spawn, bool passedOut, double elapsedMs)
    {
        var ship = Ship.Of(grid, rooms);
        _width = grid.Width;
        // Waking from passing out, the sim has put the hero at the spawn: so does the client.
        if (!_placed || (_wasOut && !passedOut))
        {
            _previous = _current = Hero.At(ship, spawn);
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
        }

        double between = _clock.Progress;
        double x = _previous.X + (_current.X - _previous.X) * between;
        double y = _previous.Y + (_current.Y - _previous.Y) * between;
        camera.Position = new Vector2((float)(x / Hero.Unit), (float)((y - Spec.Height / 2.0) / Hero.Unit)) * (float)Rig.Cell;

        // Aimed after following, so the pointer is read where the camera now is.
        var pointer = Pointer(camera);
        var drawn = _current with { X = (int)Math.Round(x), Y = (int)Math.Round(y) };
        bool inSpace = ship.Space(drawn.BodyCell(ship.Width));
        double look = passedOut ? 0 : Poses.LookOf(_current, pointer);
        _figure.Show(drawn, Poses.PoseOf(drawn, inSpace, passedOut), look, holdsPlan: false, elapsedMs);
        Tie(_current.Anchor, Tether.ClipOf(x, y), elapsedMs);

        // The press is taken every frame, so one made out of reach is spent rather than kept.
        bool pressed = (_autopilot?.TakePress() ?? false) | TakeUse();
        _aim = pointer is { } p && !passedOut ? Aiming.AimAt(grid, _current, p) : null;
        if (Aiming.UseOf(_aim, pressed && !passedOut) is { } use) client.Send(use);
        QueueRedraw();
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

    /// <summary>The tether tied at <paramref name="anchor"/>, anew wherever it is tied; none aboard.</summary>
    private void Tie(Point? anchor, (double X, double Y) clip, double elapsedMs)
    {
        if (anchor is not Point a)
        {
            _tether = null;
            return;
        }
        if (_tether?.Anchor != a) _tether = new Tether(a, clip);
        _tether.Update((a.X, a.Y), clip, elapsedMs);
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
        // Behind the hero: what E would work, then the tether, unlit, a line to find the way back by.
        if (_aim is { InReach: true, Press: { } press })
        {
            float cell = (float)Rig.Cell;
            var at = new Vector2(_aim.Cell % _width, _aim.Cell / _width) * cell;
            DrawRect(new Rect2(at + Vector2.One, new Vector2(cell - 2, cell - 2)), press.Problem == null ? AimReady : AimRefused, false, 2);
        }
        if (_tether is not { } line) return;
        float scale = (float)(Rig.Cell / Hero.Unit);
        var points = line.Trace(scale).Select(p => new Vector2((float)p.X, (float)p.Y)).ToArray();
        DrawPolyline(points, Line, 1, true);
        DrawCircle(new Vector2(line.Anchor.X, line.Anchor.Y) * scale, 1.5f, Line);
    }
}
