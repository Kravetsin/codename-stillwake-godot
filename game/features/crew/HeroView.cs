using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// This machine's hero (apps/game/src/features/crew/index.ts): moved by the keyboard against the ship's
/// cells, outside the sim, at the controller's 60 steps a second; drawn between its last two steps, so it
/// moves smoothly at any refresh rate, with its tether out in space and the plan in its hands while the
/// hand is not free; followed by the camera. E under the pointer, within reach, toggles a door or valve,
/// loads a tank, catches a piece or opens a crate when pressed, and the left button works a plan while
/// held, grabbing debris too: one <c>Work</c> for every tick released. With an empty hand, what E would
/// work is outlined, and a prompt says what E does.
/// </summary>
public partial class HeroView : Node2D
{
    private static readonly Color AimReady = new("7fe08a"), AimRefused = new("e05555");
    /// <summary>A safety line's yellow, pale enough not to shout across the dark.</summary>
    private static readonly Color Line = new(new Color("d9c27a"), 0.9f);

    private readonly Ui _ui;
    private readonly WorldButtons _mouse;
    private readonly Figure _figure = new();
    // Over the machines, as the TypeScript drew the crew's layer, so a crate's whole outline shows.
    private readonly Sketch _outline = new(4);
    private readonly UsePrompt _prompt = new();
    private readonly Clock _clock = new(Hero.StepMs);
    private readonly HeroAutopilot? _autopilot;
    private Hero _previous, _current;
    private bool _placed, _wasOut, _jump, _use;
    private Tether? _tether;
    /// <summary>What E would work, outlined: its cells, and whether the sim would refuse it.</summary>
    private (IReadOnlyList<int> Cells, bool Refused)? _aimed;
    private int _width;
    /// <summary>The blueprints delivered to in this hold of the left button: a sweep delivers to each it passes once.</summary>
    private readonly HashSet<int> _served = [];
    private int _workedOnTick = -1;

    /// <param name="ui">What the hand holds, the crate open and the piece aimed at, shared with the other features.</param>
    /// <param name="mouse">The pointer and the left button as the world reads them.</param>
    /// <param name="autopilot">A script to play the hero by instead of the keyboard, for recordings.</param>
    public HeroView(Ui ui, WorldButtons mouse, HeroAutopilot? autopilot = null)
    {
        _ui = ui;
        _mouse = mouse;
        _autopilot = autopilot;
        AddChild(_figure);
        AddChild(_outline);
        AddChild(_prompt);
        _outline.Paint = PaintAim;
    }

    /// <summary>The hero after its latest step: what the sim learns of where it is.</summary>
    public Hero Current => _current;

    /// <summary>
    /// Moves the hero on by <paramref name="elapsedMs"/> against the client's world <paramref name="m"/>
    /// and draws it, the camera centred on it, and tells the sim where it is when that matters to its
    /// breathing (<see cref="Report"/>). The hero wakes at the crew's spawn, and again there on coming
    /// round, and takes no orders while passed out.
    /// </summary>
    public void Frame(Client client, Registry m, Camera2D camera, double elapsedMs)
    {
        var grid = m.Grid;
        var ship = Ship.Of(grid, m.Rooms);
        _width = grid.Width;
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
            var input = passedOut ? new HeroInput(0, 0, false) : taken with { Face = face };
            (_previous, _current) = (_current, _current.Step(input, ship));
        }
        if (Report.Of(m.Crew, m.Rooms, m.Atmosphere, grid.Width, client.Id, _current) is { } report) client.Send(report);

        double between = _clock.Progress;
        double x = _previous.X + (_current.X - _previous.X) * between;
        double y = _previous.Y + (_current.Y - _previous.Y) * between;
        camera.Position = new Vector2((float)(x / Hero.Unit), (float)((y - Spec.Height / 2.0) / Hero.Unit)) * (float)Rig.Cell;

        // Aimed after following, so the pointer is read where the camera now is.
        var pointer = Pointer();
        var drawn = _current with { X = (int)Math.Round(x), Y = (int)Math.Round(y) };
        bool inSpace = ship.Space(drawn.BodyCell(ship.Width));
        double look = passedOut ? 0 : Poses.LookOf(_current, pointer);
        bool handFree = _ui.Hand.Get() == Holding.Free;
        _figure.Show(drawn, Poses.PoseOf(drawn, inSpace, passedOut), look, holdsPlan: !handFree && !passedOut, elapsedMs);
        Tie(_current.Anchor, Tether.ClipOf(x, y), elapsedMs);
        Use(client, m, pointer, passedOut, handFree);
        _outline.QueueRedraw();
        QueueRedraw();
    }

    /// <summary>
    /// What the hands do at the pointer: the aim outlined and said while the hand is free and the hero
    /// awake; E and the left button on what it works. Passed out, or with the plan in hand, the hands do
    /// nothing: a press is spent, a hold works no plan.
    /// </summary>
    private void Use(Client client, Registry m, (double X, double Y)? pointer, bool passedOut, bool handFree)
    {
        if (!_mouse.Buttons.HoldsLeft) _served.Clear();
        var aim = pointer is { } p ? Aiming.AimAt(m, client.Id, _current, p, Predicted.Grabbing(client.InFlight), _served) : null;
        // With a card out the building shows the pointer's cell and its own reasons.
        var prompt = passedOut || !handFree ? null : Prompts.Of(m, aim);
        _aimed = prompt != null && aim is { Piece: null } ? (OutlineOf(m, aim), prompt.Problem != null) : null;
        _prompt.Show(prompt);
        _ui.AimedPiece.Set(prompt != null && aim?.Piece is int piece ? (piece, prompt.Problem != null) : null);

        // The press is taken every frame, so one made out of reach is spent rather than kept. The left
        // button grabs debris too: out in space, with the keys held and the mouse aiming, E is a stretch.
        bool pressed = (_autopilot?.TakePress() ?? false) | TakeUse();
        bool grab = _mouse.Buttons.TakeClick() && aim?.Piece != null;
        bool acts = !passedOut && handFree;
        var uses = Aiming.UsesOf(aim, acts && (pressed || grab), acts && _mouse.Buttons.HoldsLeft, client.World.Tick, _workedOnTick);
        _workedOnTick = uses.WorkedOnTick;
        if (uses.Served is int served) _served.Add(served);
        foreach (var command in uses.Commands) client.Send(command);
        if (uses.Open is int open) _ui.OpenCrate.Set(open);
        // A crate stays open while the hero can reach it, awake.
        if (_ui.OpenCrate.Get() is int shown && (passedOut || !Aiming.CrateInReach(m.Grid, _current, shown))) _ui.OpenCrate.Set(null);
    }

    /// <summary>The cells outlined for <paramref name="aim"/>: a machine's whole footprint, unless a plan in its cell is worked, else the cell.</summary>
    private static IReadOnlyList<int> OutlineOf(Registry m, Aim aim) =>
        aim.Work == null && m.Grid.MachineAt(aim.Cell) is { } machine ? m.Grid.Footprint(machine.Kind, machine.Cell) ?? [aim.Cell] : [aim.Cell];

    /// <summary>The world point under the pointer, in cells, or the autopilot's while it points; none while the mouse is off the window.</summary>
    private (double X, double Y)? Pointer() => _autopilot?.PointerFor(_current) ?? _mouse.Pointer();

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

    /// <summary>What E would work under the pointer within reach, a machine's whole footprint: green, or red when the sim would refuse.</summary>
    private void PaintAim(Sketch g)
    {
        if (_aimed is not var (cells, refused)) return;
        int left = cells.Min(c => c % _width), top = cells.Min(c => c / _width);
        int right = cells.Max(c => c % _width) + 1, bottom = cells.Max(c => c / _width) + 1;
        float cell = (float)Rig.Cell;
        var box = new Rect2(left * cell + 1, top * cell + 1, (right - left) * cell - 2, (bottom - top) * cell - 2);
        g.DrawRect(box, refused ? AimRefused : AimReady, false, 2);
    }

    public override void _Draw()
    {
        // Behind the hero, unlit: the tether, a line to find the way back by.
        if (_tether is not { } line) return;
        float scale = (float)(Rig.Cell / Hero.Unit);
        var points = line.Trace(scale).Select(p => new Vector2((float)p.X, (float)p.Y)).ToArray();
        DrawPolyline(points, Line, 1, true);
        DrawCircle(new Vector2(line.Anchor.X, line.Anchor.Y) * scale, 1.5f, Line);
    }
}
