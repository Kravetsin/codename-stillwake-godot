using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using L = Stillwake.Game.Light;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// A vessel's guards' docks (derelict-enemies), from <see cref="CreaturesModule.Docks"/>, listed once as its world opens: a spider's
/// capsule G1 on its room's back wall, a robot's stand S1 on its room's floor, its yoke and clamp in front of its robot. A capsule is drawn with the
/// back-wall objects; a stand before the machines, for it stands over what furniture its floor holds; both behind bodies and under the
/// unseen rooms' fog. A capsule shows its spider's state: asleep, shut, its hazard lamp's amber standby pip blinking with a little light;
/// waking, its lamp flashing red and amber and lighting the room as its iris opens step by step (<see cref="DockWake"/>); open after, its lamp red
/// while its spider lives, dark once it is killed. The spider is drawn by <see cref="TickBodies"/> once the iris is open. A stand lets its
/// escort go as it wakes (<see cref="StandWake"/>): its clamp swings up, its drum pays out the cable that lowers the yoke with the robot,
/// the yoke's hooks open and it rises off its shoulders, its beacon flashing amber; after, it stands open and empty. The escort is drawn
/// by <see cref="EscortBodies"/>, between the stand's back and its front. Each moment of a waking is heard on World where its part is: the
/// capsule's alarm and iris; the stand's clamp, winch and yoke, and its robot powering up as its lamp lights.
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

    private readonly Sketch _back = new(Layers.Pipes, lit: true), _stands = new(Layers.Fittings, lit: true), _fronts = new(Layers.StandFronts, lit: true),
        _pips = new(Layers.Pipes);
    private readonly List<LightSource> _lights = [];
    private double _timeMs;

    /// <summary>How far through its waking each dock was last heard: <see cref="DockWake.Asleep"/> asleep, NaN before its first frame or once awake.</summary>
    private readonly double[] _heard;

    private readonly Voices _voices = new(S.World, voices: 3);

    /// <summary>Where in the capsule's picture its hazard lamp is, and the shelf its spider sits folded on (guards.js).</summary>
    private static readonly Vector2 Pip = new(31.03f, 12.97f), OnShelf = new(20, 34);

    /// <summary>The iris's steps opening, drawn over the open capsule: <see cref="DockWake.IrisStep"/> 1 is the first.</summary>
    private static readonly string[] IrisSteps = [.. Enumerable.Range(1, DockWake.IrisSteps).Select(k => $"guard_capsule_iris_{k}")];

    private static readonly Color Cable = new(0.086f, 0.094f, 0.11f);

    /// <summary>
    /// Where a stand's moving parts sit in its picture (guards.js, the page's cells mapped to it: a cell 8 px, its floor at the bottom):
    /// the drum's middle, the yoke's middle (its bar's bottom, asleep), its hooks either side, the clamp's hinge, the beacon, the cables' tops.
    /// </summary>
    private static readonly Vector2 Drum = new(30.8f, 12.96f), Yoke = new(24, 17.2f), Clamp = new(42.4f, 40.4f), Beacon = new(10, 3.2f);

    private const float HookApart = 10, CableLeft = 22.4f, CableRight = 25.6f, CableTop = 12.4f, CableShort = 4;

    public DocksView(CreaturesModule creatures, Func<double> progress)
    {
        (_creatures, _progress) = (creatures, progress);
        var docks = creatures.Docks;
        _docks = new (Vector2, bool)[docks.Count];
        _now = new (Shown, double)[docks.Count];
        _guards = new int[docks.Count];
        _heard = new double[docks.Count];
        Array.Fill(_heard, double.NaN);
        for (int i = 0; i < docks.Count; i++) _docks[i] = (Corner(docks[i]), docks[i].Rule == Content.Vessels.Aboard.Capsule);
        Lit.Set(_back);
        Lit.Set(_stands);
        Lit.Set(_fronts);
        AddChild(_back);
        AddChild(_stands);
        AddChild(_fronts);
        AddChild(_pips);
        AddChild(_voices);
        _back.Paint = g => Paint(g, capsules: true);
        _stands.Paint = g => Paint(g, capsules: false);
        _fronts.Paint = PaintFronts;
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
            if (Lamp(i) is not { } lamp) continue;
            var at = _docks[i].Corner + (_docks[i].Capsule ? Pip : Beacon);
            if (view.HasPoint(at)) _lights.Add(new LightSource(at.X, at.Y, lamp.Colour, lamp.Strength, lamp.Reach, lamp.Keep, 1));
        }
        if (changed)
        {
            _back.QueueRedraw();
            _stands.QueueRedraw();
            _fronts.QueueRedraw();
        }
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
            Heard(i, now.State, now.P, _guards[i] >= 0 ? all[_guards[i]].Species : null);
        }
        return changed;
    }

    /// <summary>
    /// Dock <paramref name="i"/>'s moments heard since its last frame, <paramref name="p"/> through its waking in <paramref name="state"/>,
    /// its guard of <paramref name="species"/>: only while it wakes, so a dock first seen mid-wake, or emptied asleep, is silent.
    /// </summary>
    private void Heard(int i, Shown state, double p, string? species)
    {
        double was = _heard[i];
        _heard[i] = state switch { Shown.Asleep => DockWake.Asleep, Shown.Waking => double.IsNaN(was) ? p : Math.Max(was, p), _ => double.NaN };
        if (state != Shown.Waking) return;
        double now = _heard[i];
        var corner = _docks[i].Corner;
        if (_docks[i].Capsule)
        {
            if (DockWake.Passed(was, now, 0)) _voices.Play(S.CapsuleAlarm, corner + Pip);
            if (DockWake.Passed(was, now, DockWake.IrisFrom)) _voices.Play(S.CapsuleIris, corner + OnShelf);
            return;
        }
        if (DockWake.Passed(was, now, 0)) _voices.Play(S.StandClamp, corner + Clamp);
        if (DockWake.Passed(was, now, StandWake.LowerFrom)) _voices.Play(S.StandWinch, corner + Drum);
        if (DockWake.Passed(was, now, StandWake.LampFrom) && species != null && S.Escorts.TryGetValue(species, out var cues)) _voices.Play(cues.Wake, YokeAt(corner, p));
        if (DockWake.Passed(was, now, StandWake.HooksFrom)) _voices.Play(S.StandYoke, YokeAt(corner, p));
    }

    /// <summary>Capsule <paramref name="i"/>'s lamp as lit now, on this client's clock: none while dark.</summary>
    private L.LampLight? Lamp(int i) => !_docks[i].Capsule ? _now[i] is (Shown.Waking, var w) && StandWake.Beacon(w * WakeOf(_creatures.Creatures[_guards[i]]) / World.TicksPerSecond) ? L.GuardAlarmAmber : null
        : _now[i] switch
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
                Stand(g, corner, Let(i), Lamp(i) != null);
                continue;
            }
            var (state, p) = _now[i];
            int step = state switch { Shown.Asleep => 0, Shown.Waking => DockWake.IrisStep(p), _ => DockWake.IrisSteps + 1 };
            g.DrawTexture(PixelArt.Picture(step == 0 ? "guard_capsule" : "guard_capsule_open"), corner);
            if (step is > 0 and <= DockWake.IrisSteps) g.DrawTexture(PixelArt.Picture(IrisSteps[step - 1]), corner);
        }
    }

    /// <summary>How far a stand has let its robot go: 0 asleep, its waking's share, 1 open (its robot up, or killed).</summary>
    private double Let(int i) => _now[i].State switch { Shown.Asleep => 0, Shown.Waking => _now[i].P, _ => 1 };

    /// <summary>Where a stand's yoke is at <paramref name="p"/>: over its robot's head as it is lowered, risen off it at the end.</summary>
    private static Vector2 YokeAt(Vector2 corner, double p) =>
        corner + Yoke + new Vector2(0, (float)((StandWake.Hang - StandWake.Hung(p) - StandWake.Risen(p)) * Cell));

    /// <summary>A stand's back at <paramref name="p"/>: its picture, its drum turned as it pays out, its cables down to the yoke, its beacon lit or not.</summary>
    private static void Stand(Sketch g, Vector2 corner, double p, bool beacon)
    {
        g.DrawTexture(PixelArt.Picture("guard_stand"), corner);
        Part(g, "drum", corner + Drum, (float)((StandWake.Hang - StandWake.Hung(p)) * 9), 1);
        float bottom = YokeAt(corner, p).Y - CableShort;
        foreach (float x in new[] { CableLeft, CableRight }) g.DrawLine(corner + new Vector2(x, CableTop), new Vector2(corner.X + x, bottom), Cable, 0.8f);
        if (beacon) Part(g, "beacon_lit", corner + Beacon, 0, 1);
    }

    /// <summary>Each stand's front, over its robot: its yoke and hooks, its waist clamp.</summary>
    private void PaintFronts(Sketch g)
    {
        for (int i = 0; i < _docks.Length; i++)
        {
            var (corner, capsule) = _docks[i];
            if (capsule) continue;
            double p = Let(i), open = StandWake.Hooks(p) * 0.9;
            var yoke = YokeAt(corner, p);
            Part(g, "hook", yoke + new Vector2(-HookApart, 0), (float)open, 1);
            Part(g, "hook", yoke + new Vector2(HookApart, 0), (float)-open, -1);
            Part(g, "yoke", yoke, 0, 1);
            Part(g, "clamp", corner + Clamp, (float)StandWake.Clamp(p), 1);
        }
    }

    /// <summary>Stand part <paramref name="part"/> with its pivot at <paramref name="at"/>, turned <paramref name="turn"/>, mirrored for a <paramref name="face"/> of −1.</summary>
    private static void Part(Sketch g, string part, Vector2 at, float turn, int face)
    {
        var p = CreatureArt.InkStand.Parts[part];
        g.DrawSetTransform(at, turn, new Vector2(face, 1));
        g.DrawTexture(CreatureParts.Ink(CreatureArt.InkStand.Id, part, CreatureParts.Kind.Colour), new Vector2(-p.PivotX, -p.PivotY));
        g.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
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
