using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Fauna;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --planet --fauna</c> (0102): once the hero stands on the planet, creatures let out on the ground either side of it, near
/// enough to see at once, where spawning never puts one. With <c>--manual</c> that is all; else <see cref="FaunaHands"/> hunts
/// them, the axe first and then the drill, for recordings.
/// </summary>
public sealed class FaunaDemo(bool drill = false, bool course = false, string? kind = null, bool herd = false, string? ram = null)
{
    /// <summary>
    /// <c>--strip --fauna=tapir --fauna-ram=hit</c> (<c>dodge</c>, <c>strike</c>), 0103: the hero beamed onto the strip's open
    /// ground at column 26, four columns short of the cliff's foot, whose shelter, three rows tall, no tapir leaps out of; a tapir
    /// let out beside it, struck into the shelter (a dev strike, past reach) and again each time it runs out of it unrammed, till
    /// it winds up. Then the hero stands and is hit, jumps as the charge begins, or strikes it as it crouches.
    /// </summary>
    public static readonly int RamCell = 29 * Spec.Surfaces.TestStrip.Rows[0].Length + 26;

    private bool Beams => course || ram != null;
    private int BeamCell => ram != null ? RamCell : CourseCell;
    /// <summary>
    /// The species let out: <c>--fauna=fluff</c>'s (a species' id or its look's), else each of them in turn round the hero, so one
    /// run shows them all.
    /// </summary>
    private string SpeciesAt(int i) =>
        kind == null ? Spec.Fauna.All[i % Spec.Fauna.All.Count].Id
        : Spec.Fauna.All.FirstOrDefault(s => s.Id == kind || s.Look == kind)?.Id ?? throw new ArgumentException($"--fauna={kind}: no such species");

    /// <summary>Where the creatures are let out, in columns from the hero's: some near, some farther, both sides.</summary>
    private static readonly int[] Around = [6, 11, 17, -7, -12, -18];

    /// <summary>With <c>--fauna-herd</c>, five a side of the hero, three columns apart: a herd, to tell its individuals apart.</summary>
    private static readonly int[] Herd = [-16, -13, -10, -7, -4, 3, 6, 9, 12, 15];

    /// <summary>With <c>--fauna-drill</c>, one loper right before the hero, under its drill's bit.</summary>
    private int[] Offsets => drill ? [1] : course ? [-8] : ram != null ? [1] : herd ? Herd : Around;

    /// <summary>
    /// <c>--strip --fauna-course</c>: the cell the hero beams down to, on the strip's open ground at column 64 (its row 29), a loper six
    /// columns left: struck from the right it flees left up a wall of three and one of two to the cliff's top, turns at its foot's drop of five and comes back across the shaft.
    /// </summary>
    public static readonly int CourseCell = 29 * Spec.Surfaces.TestStrip.Rows[0].Length + 64;

    private bool _released, _beamed, _down;

    /// <summary>The planet's registry once the creatures are out; null before.</summary>
    public Registry? Planet { get; private set; }

    /// <summary>A frame: once this peer's hero is on a planet, its creatures let out over the ground there.</summary>
    public void Frame(Client client, Action<string> log)
    {
        var world = client.World;
        int surface = world.SurfaceOf(client.Id);
        if (Planet is { } on)
        {
            Trace(on, world.Tick, log);
            if (Kill) Killing(client, on, world.Tick);
            if (ram != null) Ramming(client, on, world.Tick, log);
        }
        if (_released) return;
        // The course: beamed back aboard and down again at its cell, once the hero stands on the strip (a hero not yet made
        // when the first beam went down arrives at the spawn).
        if (Beams && world.Surfaces.Count > 1)
        {
            if (surface == 0 && _beamed && !_down)
            {
                client.Send(new Transfer(1, BeamCell));
                _down = true;
            }
            else if (surface == 1 && !_beamed && Registry.On(world, 1).Crew.HeroOf(client.Id) is { } first && first.Cell != BeamCell)
            {
                client.Send(new Transfer(0));
                _beamed = true;
                return;
            }
        }
        if (surface == 0) return;
        var m = Registry.On(world, surface);
        if (m.Crew.HeroOf(client.Id) is not { } hero || Beams && !_beamed) return;
        int w = m.Grid.Width, x0 = hero.Cell % w, y0 = hero.Cell / w;
        var offsets = Offsets;
        for (int i = 0; i < offsets.Length; i++)
        {
            int x = x0 + offsets[i];
            if (x < 1 || x + 2 >= w) continue;
            // From the hero's row in both its columns: up out of a hill, else down to the floor, so it stands on the ground under
            // the sky and on a cave's floor alike; over the higher of the two.
            bool Solid(int column, int y) => m.Grid.StructureAt(y * w + column) == Structure.Wall;
            int Ground(int column)
            {
                int y = y0;
                while (y > y0 - 24 && y > 2 && Solid(column, y)) y--;
                while (y < y0 + 24 && !Solid(column, y + 1)) y++;
                return y;
            }
            client.Send(new Release(SpeciesAt(i), Math.Min(Ground(x), Ground(x + 1)) * w + x));
        }
        _released = true;
        Planet = m;
        log($"fauna demo: {offsets.Length} of {kind ?? "every kind"} let out round ({x0}, {y0})");
    }

    /// <summary>
    /// <c>--fauna-kill</c>, for recording deaths: <see cref="KillAfter"/> ticks after they are out, every creature within 20 columns of
    /// the hero struck with the axe, as this player, all but its last hit's worth at once; a fifth of a second later the last.
    /// Strikes straight to the sim, past reach (the client's word, 0034): a dev shortcut, never a player's.
    /// </summary>
    public bool Kill { get; init; }

    public const int KillAfter = 60, LastAfter = 4;

    private int _outAt = -1;

    private void Killing(Client client, Registry m, int tick)
    {
        if (_outAt < 0) _outAt = tick;
        if (tick != _outAt + KillAfter && tick != _outAt + KillAfter + LastAfter || m.Crew.HeroOf(client.Id) is not { } hero) return;
        string axe = Spec.Weapons.Axe.Item;
        int w = m.Grid.Width, x0 = hero.Cell % w, hit = m.Items.Damage(client.Id, axe);
        foreach (var c in m.Fauna.Creatures)
        {
            if (Math.Abs(c.X / FaunaModule.Unit - x0) > 20) continue;
            int left = (Spec.Fauna.Get(c.Species).Hp - c.Damage + hit - 1) / hit;
            for (int i = 0; i < (tick == _outAt + KillAfter ? left - 1 : left); i++) client.Send(new Strike(c.Id, axe));
        }
    }

    private int _rammedOut = -1, _struckAt = -1;
    private Mode? _ramWas;
    private bool _jumping, _struckWindUp, _wound;

    /// <summary>
    /// <c>--fauna-ram</c>'s script, a tick at a time: the tapir struck toward the brink a second after it is out and each time it
    /// runs back from it, a cell or more short of the hero, till it winds up; then the hero's part. Every mode it takes is logged.
    /// </summary>
    private void Ramming(Client client, Registry m, int tick, Action<string> log)
    {
        if (_rammedOut < 0) _rammedOut = tick;
        if (m.Fauna.Creatures.Count == 0 || m.Crew.HeroOf(client.Id) is not { } hero) return;
        var c = m.Fauna.Creatures[0];
        int heroX = hero.Cell % m.Grid.Width * FaunaModule.Unit + FaunaModule.Unit / 2;
        if (c.Mode != _ramWas) log($"fauna ram: tick {tick}, {c.Species} {c.Id} {c.Mode} at {c.X}, hero at {heroX}, suit {hero.Suit}");
        _ramWas = c.Mode;
        _wound |= c.Mode == Mode.WindUp;
        string axe = Spec.Weapons.Axe.Item;
        bool once = tick - _struckAt > 6;
        if (!_wound && tick >= _rammedOut + 20 && once && c.X >= heroX + 1500
            && (c.Mode is Mode.Idle or Mode.Wander || c.Mode == Mode.Flee && c.VX < 0))
        {
            client.Send(new Strike(c.Id, axe));
            _struckAt = tick;
        }
        if (ram == "strike" && c.Mode == Mode.WindUp && !_struckWindUp && c.ModeLeft <= Spec.Fauna.WindUp - 3)
        {
            client.Send(new Strike(c.Id, axe));
            _struckWindUp = true;
            log($"fauna ram: tick {tick}, struck it winding up");
        }
        bool jump = ram == "dodge" && c.Mode == Mode.Charge;
        if (jump != _jumping)
        {
            Godot.Input.ParseInputEvent(new Godot.InputEventKey { Keycode = Godot.Key.Space, PhysicalKeycode = Godot.Key.Space, Pressed = jump });
            _jumping = jump;
            if (jump) log($"fauna ram: tick {tick}, jumped as it charged");
        }
    }

    /// <summary>Each creature's damage as last logged, by id: what the log says changed.</summary>
    private readonly Dictionary<int, int> _damage = [];
    private readonly Dictionary<int, Mode> _modes = [];

    /// <summary>Logs every strike that landed and every creature gone, for reading a recording against.</summary>
    private void Trace(Registry m, int tick, Action<string> log)
    {
        var all = m.Fauna.Creatures;
        foreach (var c in all)
        {
            // A ram's wind-up and charge, and how either ended (0103).
            var mode = _modes.GetValueOrDefault(c.Id, c.Mode);
            if (mode != c.Mode && (c.Mode is Mode.WindUp or Mode.Charge || mode is Mode.WindUp or Mode.Charge))
                log($"fauna demo: tick {tick}, {c.Species} {c.Id} {mode} to {c.Mode} at ({c.X}, {c.Y})");
            _modes[c.Id] = c.Mode;
            if (_damage.TryGetValue(c.Id, out int was) && was != c.Damage)
                log($"fauna demo: tick {tick}, {all.Count} out, {c.Species} {c.Id} struck to {c.Damage}/{Spec.Fauna.Get(c.Species).Hp}, {c.Mode}");
            _damage[c.Id] = c.Damage;
        }
        foreach (int id in _damage.Keys.Where(id => all.All(c => c.Id != id)).ToList())
        {
            log($"fauna demo: tick {tick}, creature {id} gone at {_damage[id]} damage");
            _damage.Remove(id);
        }
    }
}
