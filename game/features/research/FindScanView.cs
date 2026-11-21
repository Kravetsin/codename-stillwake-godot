using System;
using Godot;
using Stillwake.Game.Features.Scanner;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The drone scanning a find (<see cref="FindScan"/>, find-scan), seen by this player only: the item's icon as the bag draws it over the hero's head, the
/// scan's line swept down it on two threads from the drone's eye, the icon folding into one ember, and the ember flying on its thread to the research
/// key, which flares as it lands. A form learned (drone-lessons) is a lesson first: the drone flies to the thing it learned from, fixes its eye on it
/// and fills the scanner's row of ember dots over it, then shows the new form by the head. The icon, the sweep and the dots are on the HUD's layer,
/// under the windows; the flight and the flare over the edge's keys.
/// </summary>
public partial class FindScanView : Node
{
    /// <summary>
    /// How far over the neck the icons sit, world px; their gap over that, how far one rises as it comes in, the ember's side, how far the key's ring
    /// spreads, all screen px.
    /// </summary>
    private const float Above = 14, Gap = 6, RiseBy = 6, Pixel = 3, Spread = 14;

    /// <summary>How far over the top of the thing a lesson is learned from the drone hangs, world px.</summary>
    private const float Hover = 10;

    /// <summary>The fill of a lesson's dots is read as this many parts, so the last dot lit kindles smoothly.</summary>
    private const int FillParts = 1000;

    private static readonly Color Ember = new(DroneInk.Ember.R / 255f, DroneInk.Ember.G / 255f, DroneInk.Ember.B / 255f), Hot = Ember.Lerp(Colors.White, 0.45f);

    private readonly FindScan _scan;
    private readonly Func<bool> _inWorld;
    private readonly Func<Rect2> _art;
    private readonly Func<Rect2?> _key;
    private readonly Func<Vector2?> _head;
    private readonly Func<(Vector2 Mouth, Vector2 Eye, float Tint)?> _drone;
    private readonly Func<int> _slot;
    private readonly Action<Vector2?> _stare, _visit;
    private readonly Action<(string Form, double Seconds)?> _becoming;
    private readonly Func<string, Rect2?> _teacher;
    private readonly CanvasLayer _hud = new() { Layer = ScreenLayers.Hud }, _edge = new() { Layer = ScreenLayers.TabletEdge };
    /// <summary>The icons, as the bag draws them; the sweep and the folding embers, added; the flying embers and the key's flare, added.</summary>
    private readonly Node2D _icons = new() { TextureFilter = CanvasItem.TextureFilterEnum.Nearest }, _swept = new(), _flown = new();
    /// <summary>A lesson's dots filling over the thing: the scanner's own row (<see cref="ScanDots"/>).</summary>
    private readonly ScannerView _dots = new();
    /// <summary>Whether anything was drawn last frame: what puts it out may be the research key, between two of its frames.</summary>
    private bool _drawn;
    /// <summary>This frame's point over the head the icons sit on and the drone's eye, screen px; null without them.</summary>
    private Vector2? _overAt, _eyeAt;
    /// <summary>The form of the lesson playing and the thing it is learned from, world px, found as it begins; null for none.</summary>
    private string? _lesson;
    private Rect2? _thing;

    /// <param name="scan">When it plays, read by the research key.</param>
    /// <param name="inWorld">Whether a world is played.</param>
    /// <param name="art">Where the tablet the keys ride lies on the screen: an ember flies to its top's middle while the research key does not show.</param>
    /// <param name="key">Where the research key is on the screen; null while it does not show.</param>
    /// <param name="head">Where this player's hero's neck is, world px; null while it is not drawn.</param>
    /// <param name="drone">Its drone's spyglass mouth and eye, world px, and how red it burns; null while no drone floats by it.</param>
    /// <param name="slot">The bag's slot's side, screen px: an icon is as large as in the bag.</param>
    /// <param name="stare">Where the drone keeps its eye, world px: on the icon it scans, the thing it learns from; null for none.</param>
    /// <param name="teacher">The box, world px, of the thing a form is learned from: the machine near the hero, the wreck last broken; null for none.</param>
    /// <param name="visit">Where the drone flies to, world px, while it learns from a thing; null for none.</param>
    /// <param name="becoming">The form the drone shows off by the head, and how many seconds into the show; null for none.</param>
    public FindScanView(FindScan scan, Func<bool> inWorld, Func<Rect2> art, Func<Rect2?> key, Func<Vector2?> head, Func<(Vector2 Mouth, Vector2 Eye, float Tint)?> drone,
        Func<int> slot, Action<Vector2?> stare, Func<string, Rect2?> teacher, Action<Vector2?> visit, Action<(string Form, double Seconds)?> becoming)
    {
        (_scan, _inWorld, _art, _key, _head, _drone, _slot, _stare, _teacher, _visit, _becoming) = (scan, inWorld, art, key, head, drone, slot, stare, teacher, visit, becoming);
        foreach (var added in (ReadOnlySpan<Node2D>)[_swept, _flown]) added.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _icons.Draw += DrawIcons;
        _swept.Draw += DrawSwept;
        _flown.Draw += DrawFlown;
        _hud.AddChild(_icons);
        _hud.AddChild(_swept);
        _edge.AddChild(_flown);
        AddChild(_hud);
        AddChild(_edge);
        AddChild(_dots);
    }

    public override void _Process(double delta)
    {
        bool world = _inWorld();
        var head = world ? _head() : null;
        var drone = world ? _drone() : null;
        _scan.Step(delta, drone != null);
        _hud.Visible = _edge.Visible = world;
        var canvas = GetViewport().GetCanvasTransform();
        _overAt = head is { } neck ? canvas * (neck - new Vector2(0, Above)) : null;
        _eyeAt = drone is { } d ? canvas * d.Eye : null;
        Learn(world ? _scan.Learning : null, head, drone, delta);
        bool on = _scan.Busy;
        if (on || _drawn)
        {
            _icons.QueueRedraw();
            _swept.QueueRedraw();
            _flown.QueueRedraw();
        }
        _drawn = on;
    }

    /// <summary>
    /// A lesson's stages: the drone sent to the thing and its eye fixed on it while it goes and scans, the dots filling over it as it scans, the form
    /// shown off by the head after; with no thing found, the dots fill over the head. Out of a lesson, the drone looks at the icon over the head.
    /// </summary>
    private void Learn(FindLesson? lesson, Vector2? head, (Vector2 Mouth, Vector2 Eye, float Tint)? drone, double delta)
    {
        if (lesson?.Form != _lesson) (_lesson, _thing) = (lesson?.Form, null);
        // Looked for until found while the drone sets off: the wreck broken may be seen gone a frame after its lesson.
        if (lesson is { Stage: LessonStage.Go } going && _thing == null) _thing = _teacher(going.Form);
        bool away = lesson is { Stage: LessonStage.Go or LessonStage.Study };
        var thing = away ? _thing : null;
        _visit(thing is { } to ? new Vector2(to.GetCenter().X, to.Position.Y - Hover) : null);
        var canvas = GetViewport().GetCanvasTransform();
        _stare(thing is { } seen ? seen.GetCenter()
            : !away && _scan.Over is { } over && _overAt is { } at ? canvas.AffineInverse() * Centre(at, over.Item, over.In) : null);
        _becoming(lesson is { Stage: LessonStage.Become } becoming ? (becoming.Form, becoming.Progress * FindScan.Become) : null);
        // The dots read the thing's top, or the head's point, in cells.
        Vector2? read = thing is { } box ? new Vector2(box.GetCenter().X, box.Position.Y) : head is { } neck ? neck - new Vector2(0, Above) : null;
        var readout = lesson is { Stage: LessonStage.Study } study && read is { } r
            ? new Readout(-1, r.X / Camera.Cell, r.Y / Camera.Cell, (int)(study.Progress * FillParts), FillParts)
            : (Readout?)null;
        _dots.Frame(readout, drone, delta * 1000);
    }

    /// <summary>
    /// The box, world px, of the thing on <paramref name="m"/>'s surface <paramref name="peer"/>'s hero learns <paramref name="form"/> from
    /// (drone-lessons): the nearest machine within reach for a lesson by a machine, <paramref name="lastBroken"/> for one by a deed; null for none.
    /// </summary>
    public static Rect2? TeacherOf(Sim.Registry m, int peer, string form, Rect2? lastBroken)
    {
        if (m.Crew.HeroOf(peer) is not { } hero) return null;
        foreach (var lesson in Stillwake.Content.Lessons.All)
        {
            if (lesson.Form != form) continue;
            if (lesson.Source is not Stillwake.Content.Near near) return lastBroken;
            if (m.Lessons.Teacher(hero.Cell, near.Machine) is not { } machine) return null;
            var spec = Stillwake.Content.Machines.Get(machine.Kind);
            int w = m.Grid.Width;
            return new Rect2(new Vector2(machine.Cell % w, machine.Cell / w) * Camera.Cell, new Vector2(spec.Width, spec.Height) * Camera.Cell);
        }
        return null;
    }

    /// <summary>An item's icon's side, as the bag draws it.</summary>
    private int Side(string item) => Slots.IconBox(item, _slot()).Side;

    /// <summary>The middle of <paramref name="item"/>'s icon over the head at <paramref name="at"/>, <paramref name="come"/> of the way in, on whole pixels.</summary>
    private Vector2 Centre(Vector2 at, string item, double come)
    {
        float half = Side(item) / 2f;
        return new Vector2(MathF.Round(at.X - half) + half, MathF.Round(at.Y - Gap - 2 * half + RiseBy * (float)(1 - come)) + half);
    }

    /// <summary>Where a record leaves from: its icon's middle, or the drone's eye.</summary>
    private Vector2? From(FindRecord record) =>
        record.Item is { } item ? _overAt is { } at ? Centre(at, item, 1) : null : _eyeAt ?? (_overAt is { } over ? over - new Vector2(0, Gap) : null);

    /// <summary>The icon over the head, and each one folding into its ember, shrinking to its middle.</summary>
    private void DrawIcons()
    {
        if (_overAt is not { } at) return;
        if (_scan.Over is { } over) Icon(over.Item, Centre(at, over.Item, over.In), 1, (float)over.In);
        for (int i = 0; i < _scan.Records; i++)
        {
            var record = _scan.RecordAt(i);
            if (record is not { Item: { } item, Fly: null }) continue;
            float fold = (float)(record.Fold * record.Fold * (3 - 2 * record.Fold));
            Icon(item, Centre(at, item, 1), 1 - fold, 1 - (float)record.Fold);
        }
    }

    private void Icon(string item, Vector2 centre, float scale, float alpha)
    {
        int side = Side(item);
        float size = side * scale;
        if (size < 0.5f || alpha <= 0.003f) return;
        // A drone's form shows its thread icon (drone-lessons), an item its bag picture.
        var picture = FormPictures.Of(item, side) ?? ItemIcons.Icon(item, side);
        _icons.DrawTextureRect(picture, new Rect2(centre - Vector2.One * size / 2, Vector2.One * size), false, new Color(1, 1, 1, alpha));
    }

    /// <summary>The scan's line swept down the icon, a thread to each of its ends from the drone's eye; each record's ember kindling as it folds.</summary>
    private void DrawSwept()
    {
        if (_overAt is not { } at) return;
        if (_scan.Over is { Sweep: { } sweep and > 0 and < 1 } over)
        {
            var centre = Centre(at, over.Item, over.In);
            float half = Side(over.Item) / 2f, reach = half * 17 / 16;
            SweepAcross(new Rect2(centre.X - reach, centre.Y - half, 2 * reach, 2 * half), (float)sweep);
        }
        for (int i = 0; i < _scan.Records; i++)
        {
            var record = _scan.RecordAt(i);
            if (record.Fly != null || From(record) is not { } from) continue;
            float fold = (float)record.Fold;
            Halo(_swept, from, 5 + 6 * fold, 0.3f + 0.4f * fold);
            Square(_swept, from, Pixel * (0.6f + 0.4f * fold), Hot);
        }
    }

    /// <summary>The scan's line swept down an icon's <paramref name="box"/>, <paramref name="swept"/> of the way, a thread to each end from the drone's eye.</summary>
    private void SweepAcross(Rect2 box, float swept)
    {
        float y = MathF.Round(box.Position.Y + swept * box.Size.Y) + 0.5f;
        Vector2 a = new(box.Position.X, y), e = new(box.End.X, y);
        if (_eyeAt is { } eye)
        {
            _swept.DrawLine(eye, a, Ember with { A = 0.12f }, 1);
            _swept.DrawLine(eye, e, Ember with { A = 0.12f }, 1);
        }
        _swept.DrawLine(a, e, Ember with { A = 0.1f }, 3);
        _swept.DrawLine(a, e, Ember with { A = 0.85f }, 1);
        Square(_swept, a, 2, Hot);
        Square(_swept, e, 2, Hot);
    }

    /// <summary>Each ember flying on its thread to the key, a tail behind it; the key's flare as one lands, a ring spreading off it.</summary>
    private void DrawFlown()
    {
        var art = _art();
        var key = _key();
        var to = key is { } k ? k.GetCenter() : new Vector2(art.GetCenter().X, art.Position.Y);
        for (int i = 0; i < _scan.Records; i++)
        {
            var record = _scan.RecordAt(i);
            if (record.Fly is not { } fly || From(record) is not { } from) continue;
            var at = from.Lerp(to, (float)fly);
            _flown.DrawLine(from, at, Ember with { A = 0.16f * (1 - 0.6f * (float)fly) }, 1);
            Halo(_flown, at, 7, 0.5f);
            var back = (to - from).Normalized() * Pixel * 1.3f;
            for (int m = 2; m >= 0; m--)
                Square(_flown, at - back * m, Pixel, Ember.Lerp(Colors.White, 0.3f * (1 - m / 3f)) with { A = 0.95f - 0.3f * m });
        }
        float glow = (float)_scan.Glow;
        if (glow <= 0 || key is not { } box) return;
        float spread = Spread * (1 - glow);
        _flown.DrawRect(box.Grow(2 + spread), Ember with { A = 0.7f * glow }, false, 2);
        _flown.DrawRect(box.Grow(4 + spread * 1.6f), Ember with { A = 0.22f * glow }, false, 4);
        _flown.DrawRect(box, Hot with { A = 0.35f * glow * glow });
    }

    /// <summary>A square <paramref name="side"/> px round <paramref name="at"/>, on whole pixels.</summary>
    private static void Square(Node2D on, Vector2 at, float side, Color colour)
    {
        side = Math.Max(1, MathF.Round(side));
        on.DrawRect(new Rect2(MathF.Round(at.X - side / 2), MathF.Round(at.Y - side / 2), side, side), colour);
    }

    /// <summary>The ember's soft light, <paramref name="radius"/> px, <paramref name="alpha"/> in its middle.</summary>
    private static void Halo(Node2D on, Vector2 at, float radius, float alpha) =>
        on.DrawTextureRect(PixelArt.Glow, new Rect2(at - Vector2.One * radius, Vector2.One * 2 * radius), false, Ember with { A = alpha });
}
