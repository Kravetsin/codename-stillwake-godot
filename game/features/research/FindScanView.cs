using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The drone scanning a find (<see cref="FindScan"/>, find-scan), seen by this player only: the item's icon as the bag draws it over the hero's head, the
/// scan's line swept down it on two threads from the drone's eye, the icon folding into one ember, and the ember flying on its thread to the research
/// key, which flares as it lands. The icon and the sweep are on the HUD's layer, under the windows; the flight and the flare over the edge's keys.
/// </summary>
public partial class FindScanView : Node
{
    /// <summary>
    /// How far over the neck the icons sit, world px; their gap over that, how far one rises as it comes in, the ember's side, how far the key's ring
    /// spreads, all screen px.
    /// </summary>
    private const float Above = 14, Gap = 6, RiseBy = 6, Pixel = 3, Spread = 14;

    private static readonly Color Ember = new(DroneInk.Ember.R / 255f, DroneInk.Ember.G / 255f, DroneInk.Ember.B / 255f), Hot = Ember.Lerp(Colors.White, 0.45f);

    private readonly FindScan _scan;
    private readonly Func<bool> _inWorld;
    private readonly Func<Rect2> _art;
    private readonly Func<Rect2?> _key;
    private readonly Func<Vector2?> _head, _eye;
    private readonly Func<int> _slot;
    private readonly Action<Vector2?> _stare;
    private readonly CanvasLayer _hud = new() { Layer = ScreenLayers.Hud }, _edge = new() { Layer = ScreenLayers.TabletEdge };
    /// <summary>The icons, as the bag draws them; the sweep and the folding embers, added; the flying embers and the key's flare, added.</summary>
    private readonly Node2D _icons = new() { TextureFilter = CanvasItem.TextureFilterEnum.Nearest }, _swept = new(), _flown = new();
    /// <summary>Whether anything was drawn last frame: what puts it out may be the research key, between two of its frames.</summary>
    private bool _drawn;
    /// <summary>This frame's point over the head the icons sit on and the drone's eye, screen px; null without them.</summary>
    private Vector2? _overAt, _eyeAt;

    /// <param name="scan">When it plays, read by the research key.</param>
    /// <param name="inWorld">Whether a world is played.</param>
    /// <param name="art">Where the tablet the keys ride lies on the screen: an ember flies to its top's middle while the research key does not show.</param>
    /// <param name="key">Where the research key is on the screen; null while it does not show.</param>
    /// <param name="head">Where this player's hero's neck is, world px; null while it is not drawn.</param>
    /// <param name="eye">Where its drone's eye is, world px; null while no drone floats by it.</param>
    /// <param name="slot">The bag's slot's side, screen px: an icon is as large as in the bag.</param>
    /// <param name="stare">Where the drone keeps its eye, world px: on the icon it scans; null for none.</param>
    public FindScanView(FindScan scan, Func<bool> inWorld, Func<Rect2> art, Func<Rect2?> key, Func<Vector2?> head, Func<Vector2?> eye, Func<int> slot, Action<Vector2?> stare)
    {
        (_scan, _inWorld, _art, _key, _head, _eye, _slot, _stare) = (scan, inWorld, art, key, head, eye, slot, stare);
        foreach (var added in (ReadOnlySpan<Node2D>)[_swept, _flown]) added.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _icons.Draw += DrawIcons;
        _swept.Draw += DrawSwept;
        _flown.Draw += DrawFlown;
        _hud.AddChild(_icons);
        _hud.AddChild(_swept);
        _edge.AddChild(_flown);
        AddChild(_hud);
        AddChild(_edge);
    }

    public override void _Process(double delta)
    {
        bool world = _inWorld();
        var head = world ? _head() : null;
        var eye = world ? _eye() : null;
        _scan.Step(delta, eye != null);
        _hud.Visible = _edge.Visible = world;
        var canvas = GetViewport().GetCanvasTransform();
        _overAt = head is { } neck ? canvas * (neck - new Vector2(0, Above)) : null;
        _eyeAt = eye is { } e ? canvas * e : null;
        _stare(_scan.Over is { } over && _overAt is { } at ? canvas.AffineInverse() * Centre(at, over.Item, over.In) : null);
        bool on = _scan.Busy;
        if (on || _drawn)
        {
            _icons.QueueRedraw();
            _swept.QueueRedraw();
            _flown.QueueRedraw();
        }
        _drawn = on;
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
        _icons.DrawTextureRect(ItemIcons.Icon(item, side), new Rect2(centre - Vector2.One * size / 2, Vector2.One * size), false, new Color(1, 1, 1, alpha));
    }

    /// <summary>The scan's line swept down the icon, a thread to each of its ends from the drone's eye; each record's ember kindling as it folds.</summary>
    private void DrawSwept()
    {
        if (_overAt is not { } at) return;
        if (_scan.Over is { Sweep: { } sweep and > 0 and < 1 } over)
        {
            var centre = Centre(at, over.Item, over.In);
            float half = Side(over.Item) / 2f, y = MathF.Round(centre.Y - half + (float)sweep * 2 * half) + 0.5f, reach = half * 17 / 16;
            Vector2 a = new(centre.X - reach, y), e = new(centre.X + reach, y);
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
        for (int i = 0; i < _scan.Records; i++)
        {
            var record = _scan.RecordAt(i);
            if (record.Fly != null || From(record) is not { } from) continue;
            float fold = (float)record.Fold;
            Halo(_swept, from, 5 + 6 * fold, 0.3f + 0.4f * fold);
            Square(_swept, from, Pixel * (0.6f + 0.4f * fold), Hot);
        }
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
