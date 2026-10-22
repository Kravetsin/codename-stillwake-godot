using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Fauna;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --planet --fauna</c> (0102): once the hero stands on the planet, lopers let out on the ground either side of it, near
/// enough to see at once, where spawning never puts one. With <c>--manual</c> that is all; else <see cref="FaunaHands"/> hunts
/// them, the axe first and then the drill, for recordings.
/// </summary>
public sealed class FaunaDemo(bool drill = false, bool course = false)
{
    /// <summary>Where the lopers are let out, in columns from the hero's: some near, some farther, both sides.</summary>
    private static readonly int[] Around = [6, 11, 17, -7, -12, -18];

    /// <summary>With <c>--fauna-drill</c>, one loper right before the hero, under its drill's bit.</summary>
    private int[] Offsets => drill ? [1] : course ? [-8] : Around;

    /// <summary>
    /// <c>--strip --fauna-course</c>: the cell the hero beams down to, on the strip's open ground at column 64 (its row 29), a loper six
    /// columns left: struck from the right it flees left up a wall of three and one of two to the cliff's top, turns at its foot's drop of five and comes back across the shaft.
    /// </summary>
    public static readonly int CourseCell = 29 * Spec.Surfaces.TestStrip.Rows[0].Length + 64;

    private bool _released, _beamed, _down;

    /// <summary>The planet's registry once the lopers are out; null before.</summary>
    public Registry? Planet { get; private set; }

    /// <summary>A frame: once this peer's hero is on a planet, its lopers let out over the ground there.</summary>
    public void Frame(Client client, Action<string> log)
    {
        var world = client.World;
        int surface = world.SurfaceOf(client.Id);
        if (Planet is { } on) Trace(on, world.Tick, log);
        if (_released) return;
        // The course: beamed back aboard and down again at its cell, once the hero stands on the strip (a hero not yet made
        // when the first beam went down arrives at the spawn).
        if (course && world.Surfaces.Count > 1)
        {
            if (surface == 0 && _beamed && !_down)
            {
                client.Send(new Transfer(1, CourseCell));
                _down = true;
            }
            else if (surface == 1 && !_beamed && Registry.On(world, 1).Crew.HeroOf(client.Id) is { } first && first.Cell != CourseCell)
            {
                client.Send(new Transfer(0));
                _beamed = true;
                return;
            }
        }
        if (surface == 0) return;
        var m = Registry.On(world, surface);
        if (m.Crew.HeroOf(client.Id) is not { } hero || course && !_beamed) return;
        int w = m.Grid.Width, x0 = hero.Cell % w, y0 = hero.Cell / w;
        foreach (int dx in Offsets)
        {
            int x = x0 + dx;
            if (x < 1 || x + 2 >= w) continue;
            // Down both its columns from well over the hero to the first wall: the loper stands over the higher.
            int Ground(int column)
            {
                int y = Math.Max(2, y0 - 24);
                while (y < y0 + 24 && m.Grid.StructureAt((y + 1) * w + column) != Structure.Wall) y++;
                return y;
            }
            client.Send(new Release(Spec.Fauna.Loper.Id, Math.Min(Ground(x), Ground(x + 1)) * w + x));
        }
        _released = true;
        Planet = m;
        log($"fauna demo: {Offsets.Length} lopers let out round ({x0}, {y0})");
    }

    /// <summary>Each creature's damage as last logged, by id: what the log says changed.</summary>
    private readonly Dictionary<int, int> _damage = [];

    /// <summary>Logs every strike that landed and every creature gone, for reading a recording against.</summary>
    private void Trace(Registry m, int tick, Action<string> log)
    {
        var all = m.Fauna.Creatures;
        foreach (var c in all)
        {
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
