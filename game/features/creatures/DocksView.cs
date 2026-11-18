using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using L = Stillwake.Game.Light;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// A vessel's guards' docks (derelict-enemies), from <see cref="CreaturesModule.Docks"/>, listed once as its world opens: a spider's
/// capsule G1 on its room's back wall, a robot's stand S1 on its room's floor, its yoke and clamp in front. A capsule is drawn with the
/// back-wall objects; a stand before the machines, for it stands over what furniture its floor holds; both behind bodies and under the
/// unseen rooms' fog. A capsule shows its spider's state: asleep, shut, its hazard lamp's amber standby pip blinking with a little light;
/// waking, its lamp flashing red and amber and lighting the room as its iris opens (<see cref="DockWake"/>); open after, its lamp red
/// while its spider lives, dark once it is killed. The spider is drawn by <see cref="TickBodies"/> once the iris is open.
/// </summary>
public partial class DocksView : Node2D, ILightSource
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>A capsule's state as drawn.</summary>
    private enum Shown { Asleep, Waking, Open, Empty }

    private readonly CreaturesModule _creatures;
    private readonly Func<double> _progress;

    /// <summary>Each dock's picture's top-left in the world's pixels, and whether it is a capsule; a capsule's picture holds the row over its box.</summary>
    private readonly (Vector2 Corner, bool Capsule)[] _docks;

    /// <summary>Each capsule's state this frame and how far through its waking it is.</summary>
    private readonly (Shown State, double P)[] _now;

    /// <summary>The creature at each dock this frame, by its place among the creatures; -1 for none.</summary>
    private readonly int[] _guards;

    private readonly Sketch _back = new(Layers.Pipes, lit: true), _stands = new(Layers.Fittings, lit: true), _pips = new(Layers.Pipes);
    private readonly List<LightSource> _lights = [];
    private double _timeMs;

    /// <summary>Where in the capsule's picture its hazard lamp is, its iris's middle, and the shelf its spider sits folded on (guards.js).</summary>
    private static readonly Vector2 Pip = new(31.03f, 12.97f), Iris = new(20, 24), OnShelf = new(20, 34);

    /// <summary>The iris's radius and its blades' width, as guards.js draws them shut.</summary>
    private const float IrisR = 12.85f, Blade = 3.6f;

    private static readonly Color Ink = new(0.043f, 0.055f, 0.075f), Smoke = new(0.15f, 0.2f, 0.23f, 0.6f);

    public DocksView(CreaturesModule creatures, Func<double> progress)
    {
        (_creatures, _progress) = (creatures, progress);
        var docks = creatures.Docks;
        _docks = new (Vector2, bool)[docks.Count];
        _now = new (Shown, double)[docks.Count];
        _guards = new int[docks.Count];
        for (int i = 0; i < docks.Count; i++) _docks[i] = (Corner(docks[i]), docks[i].Rule == Content.Vessels.Aboard.Capsule);
        Lit.Set(_back);
        Lit.Set(_stands);
        AddChild(_back);
        AddChild(_stands);
        AddChild(_pips);
        _back.Paint = g => Paint(g, capsules: true);
        _stands.Paint = g => Paint(g, capsules: false);
        _pips.Paint = PaintPips;
    }

    /// <summary>The docks of a vessel world's guards; null for a surface with none.</summary>
    public static DocksView? Of(CreaturesModule creatures, Func<double> progress) => creatures.Docks.Count > 0 ? new DocksView(creatures, progress) : null;

    /// <summary>Dock <paramref name="d"/>'s picture's top-left in the world's pixels: a capsule's holds the row over its box.</summary>
    private static Vector2 Corner(Shipyard.Dock d)
    {
        int m = Content.Vessels.Margin;
        return new Vector2(m + d.X, m + d.Y - (d.Rule == Content.Vessels.Aboard.Capsule ? 1 : 0)) * Cell;
    }

    /// <summary>Where the folded spider of capsule <paramref name="d"/> stands on its shelf, in the world's pixels: where it drops from.</summary>
    public static Vector2 Shelf(Shipyard.Dock d) => Corner(d) + OnShelf;

    /// <summary>The ticks <paramref name="c"/>'s species takes to wake; 0 for one that never does.</summary>
    public static int WakeOf(Creature c)
    {
        foreach (var d in Spec.Get(c.Species).Brain)
            if (d is Spec.Dock dock) return dock.Wake;
        return 0;
    }

    public IReadOnlyList<LightSource> Lights => _lights;

    public override void _Process(double delta)
    {
        _timeMs += delta * 1000;
        bool changed = Read();
        _lights.Clear();
        var view = GetCanvasTransform().AffineInverse() * GetViewportRect();
        for (int i = 0; i < _docks.Length; i++)
        {
            if (!_docks[i].Capsule || Lamp(i) is not { } lamp) continue;
            var at = _docks[i].Corner + Pip;
            if (view.HasPoint(at)) _lights.Add(new LightSource(at.X, at.Y, lamp.Colour, lamp.Strength, lamp.Reach, lamp.Keep, 1));
        }
        if (changed) _back.QueueRedraw();
        _pips.QueueRedraw();
    }

    /// <summary>Each capsule's state from its spider: whether any changed, or one is waking, its iris moving.</summary>
    private bool Read()
    {
        Array.Fill(_guards, -1);
        var all = _creatures.Creatures;
        for (int k = 0; k < all.Count; k++)
            if (CreaturesModule.DockOf(all[k]) is int dock && dock < _guards.Length) _guards[dock] = k;
        bool changed = false;
        for (int i = 0; i < _docks.Length; i++)
        {
            if (!_docks[i].Capsule) continue;
            var now = (State: Shown.Empty, P: 1.0);
            if (_guards[i] >= 0)
            {
                var c = all[_guards[i]];
                now = c.Posture switch
                {
                    Posture.Docked => (Shown.Asleep, 0),
                    Posture.Waking => (Shown.Waking, DockWake.Progress(c.Left, WakeOf(c), _progress())),
                    _ => (Shown.Open, 1),
                };
            }
            changed |= now.State != _now[i].State || now.State == Shown.Waking;
            _now[i] = now;
        }
        return changed;
    }

    /// <summary>Capsule <paramref name="i"/>'s lamp as lit now, on this client's clock: none while dark.</summary>
    private L.LampLight? Lamp(int i) => _now[i] switch
    {
        (Shown.Asleep, _) => (_timeMs + i * L.GuardPipStaggerMs) % L.GuardPipEveryMs < L.GuardPipOnMs ? L.GuardPip : null,
        (Shown.Waking, var p) => DockWake.Red(p * WakeOf(_creatures.Creatures[_guards[i]]) / World.TicksPerSecond) ? L.GuardAlarm : L.GuardAlarmAmber,
        (Shown.Open, _) => L.GuardAlarmOn,
        _ => null,
    };

    private void Paint(Sketch g, bool capsules)
    {
        for (int i = 0; i < _docks.Length; i++)
        {
            var (corner, capsule) = _docks[i];
            if (capsule != capsules) continue;
            if (!capsule)
            {
                g.DrawTexture(PixelArt.Picture("guard_stand"), corner);
                g.DrawTexture(PixelArt.Picture("guard_stand_front"), corner);
                continue;
            }
            var (state, p) = _now[i];
            if (state == Shown.Asleep)
            {
                g.DrawTexture(PixelArt.Picture("guard_capsule"), corner);
                continue;
            }
            g.DrawTexture(PixelArt.Picture("guard_capsule_open"), corner);
            if (state == Shown.Waking && DockWake.Iris(p) < 1) Blades(g, corner + Iris, DockWake.Iris(p));
        }
    }

    /// <summary>The iris's smoked glass and blades, <paramref name="open"/> of the way open: guards.js's blades, their pinhole grown to the rim.</summary>
    private static void Blades(Sketch g, Vector2 middle, double open)
    {
        g.DrawCircle(middle, IrisR, Smoke with { A = Smoke.A * (float)(1 - open) });
        double h = 0.6 + (IrisR - 0.6) * open;
        float d = (float)Math.Sqrt(Math.Max(0, IrisR * IrisR - h * h)) + 1;
        for (int k = 0; k < 8; k++)
        {
            double q = k / 8.0 * Math.Tau;
            float ca = (float)Math.Cos(q), sa = (float)Math.Sin(q);
            var from = middle + new Vector2(ca, sa) * (float)h;
            g.DrawLine(from, from + new Vector2(-sa, ca) * d, Ink, Blade);
        }
    }

    private void PaintPips(Sketch g)
    {
        for (int i = 0; i < _docks.Length; i++)
        {
            if (!_docks[i].Capsule || Lamp(i) is not { } lamp) continue;
            float size = _now[i].State == Shown.Asleep ? L.GuardPipSize : L.GuardAlarmSize;
            g.DrawCircle(_docks[i].Corner + Pip, size / 2, new Color(lamp.Halo));
        }
    }
}
