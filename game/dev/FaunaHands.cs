using System;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Stillwake.Sim.Fauna;
using Spec = Stillwake.Content;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --planet --fauna</c>'s hands (0102), as a player's keys and mouse would: the lopers watched wandering a while, then the
/// nearest hunted — run at with Shift held while it is far, walked up to, the pointer on its middle and the left button held —
/// with the axe till one dies, then with the drill. Never the sim: only the input a player gives.
/// </summary>
public partial class FaunaHands(FaunaDemo demo, Func<Hero?> hero, bool still = false, bool quick = false) : Node
{
    /// <summary>Seconds of watching before the hunt, and of the axe before the drill if nothing has died by then.</summary>
    private const double WatchS = 6, AxeS = 16;

    /// <summary>Within this many cells across it strikes; past <see cref="RunCells"/> it runs.</summary>
    private const double StrikeCells = 2.2, RunCells = 6;

    /// <summary>A creature more rows than this above or below the hero, or more columns than <see cref="Range"/> from where it began, is let be.</summary>
    private const int Reach = 6, Range = 45;

    private int? _home;
    private double _since;
    private int _tool = -1, _dead, _seen = -1, _way;
    private bool _holding, _running;

    public override void _Process(double delta)
    {
        if (demo.Planet is not { } m || hero() is not { } me) return;
        _since += delta;
        var all = m.Fauna.Creatures;
        if (_seen >= 0 && all.Count < _seen) _dead += _seen - all.Count;
        _seen = all.Count;
        if (_since < (still || quick ? 1 : WatchS) || all.Count == 0)
        {
            Walk(0, false);
            Hold(false);
            return;
        }
        Wield(still || _dead > 0 || _since > WatchS + AxeS ? 1 : 0);
        // Only those on the hero's own ground, near where it began: it never chases one off a cliff or down a hole.
        _home ??= me.X;
        Creature? nearest = null;
        long best = long.MaxValue;
        foreach (var c in all)
        {
            if (Math.Abs(c.Y - me.Y) > Reach * Hero.Unit || Math.Abs(c.X - _home.Value) > Range * Hero.Unit || !Seen(m.Grid, me, c)) continue;
            long d = Math.Abs((long)c.X - me.X) + Math.Abs((long)c.Y - me.Y);
            if (d < best) (best, nearest) = (d, c);
        }
        if (nearest is not { } target)
        {
            Walk(0, false);
            Hold(false);
            return;
        }
        var spec = Spec.Fauna.Get(target.Species);
        double dx = (target.X - me.X) / (double)Hero.Unit;
        // The drill's bit is shorter than the axe's swing: it is held right up against the body.
        double close = _tool == 1 ? DrillCells : StrikeCells;
        // Standing still (--fauna-drill): the drill held where the loper was, never chasing it.
        Walk(!still && Math.Abs(dx) > close ? Math.Sign(dx) : 0, !still && Math.Abs(dx) > RunCells);
        var at = GetViewport().GetCanvasTransform() * (new Vector2(target.X, target.Y - spec.Height * Hero.Unit / 2) / Hero.Unit * ShipCamera.Cell);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at, ButtonMask = _holding ? MouseButtonMask.Left : 0 });
        Hold(Math.Abs(dx) <= close + 1.2);
    }

    private const double DrillCells = 1.2;

    /// <summary>Whether nothing built stands between the hero's chest and the middle of <paramref name="c"/>: one in a cave under the feet is let be.</summary>
    private static bool Seen(Sim.Grid.IGridQueries grid, Hero me, Creature c)
    {
        double ax = me.X, ay = me.Y - 1500, bx = c.X, by = c.Y - 1000;
        int steps = (int)(Math.Max(Math.Abs(bx - ax), Math.Abs(by - ay)) / 400) + 1;
        for (int i = 1; i < steps; i++)
        {
            int x = (int)((ax + (bx - ax) * i / steps) / Hero.Unit), y = (int)((ay + (by - ay) * i / steps) / Hero.Unit);
            if (x >= 0 && y >= 0 && x < grid.Width && y < grid.Height && grid.StructureAt(y * grid.Width + x) == Structure.Wall) return false;
        }
        return true;
    }

    /// <summary>The hotbar's slot <paramref name="slot"/> taken in hand: the axe's, then the drill's.</summary>
    private void Wield(int slot)
    {
        if (_tool == slot) return;
        _tool = slot;
        Hold(false);
        var key = slot == 0 ? Key.Key1 : Key.Key2;
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
    }

    /// <summary>D or A held for <paramref name="way"/>, none for none, Shift with it to run.</summary>
    private void Walk(int way, bool run)
    {
        if (way != _way)
        {
            if (_way != 0) Press(_way > 0 ? Key.D : Key.A, false);
            if (way != 0) Press(way > 0 ? Key.D : Key.A, true);
            _way = way;
        }
        if (run != _running)
        {
            Press(Key.Shift, run);
            _running = run;
        }
    }

    private static void Press(Key key, bool down) =>
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = down });

    private void Hold(bool held)
    {
        if (_holding == held) return;
        _holding = held;
        var at = GetViewport().GetMousePosition();
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = held,
            ButtonMask = held ? MouseButtonMask.Left : 0,
            Position = at,
            GlobalPosition = at,
        });
    }
}
