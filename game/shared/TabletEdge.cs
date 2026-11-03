using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Shared;

/// <summary>
/// The tablet's top edge along the window's foot and the keys on it, in place of words ([tablet-edge](../../../docs/decisions/2026-11-02-tablet-edge.md)): the same art as every
/// tablet's, across the window, its screen dark. A tablet taken out rises from it (<see cref="TabletPeek.Pose"/>), so it shows while none is up; the keys, each a feature's own,
/// ride on the top of whichever is, so they stay in reach. A key says its name and hotkey over it, under the pointer.
/// </summary>
public partial class TabletEdge : CanvasLayer
{
    private readonly Ui _ui;
    private readonly Func<bool> _inWorld;
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Tablet _rim = new();
    /// <summary>Over the rim's metal: a click on it is no click on the world behind.</summary>
    private readonly Control _metal = new() { MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly Saddle _saddle = new();
    private readonly List<Tablet> _tablets = [];
    private readonly TipSpots _tips;
    private Vector2 _window;
    private int _keys;

    /// <param name="ui">The one tooltip, and the menu, under which no key speaks.</param>
    /// <param name="inWorld">Whether a world is played: with none it is hidden.</param>
    public TabletEdge(Ui ui, Func<bool> inWorld)
    {
        (_ui, _inWorld) = (ui, inWorld);
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.TabletEdge;
        // After the tablets have moved this frame, so the keys never trail the top they ride on.
        ProcessPriority = 1;
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);
        _root.AddChild(_rim);
        _root.AddChild(_metal);
        _root.AddChild(_saddle);
    }

    /// <summary>
    /// Sets <paramref name="key"/> on the edge, next along, saying the i18n key <paramref name="says"/> under the pointer, and under
    /// it, an edge key that can do nothing now, why.
    /// </summary>
    public void Put(Control key, string says)
    {
        key.Position = new Vector2(TabletPeek.KeyIn + _keys++ * TabletPeek.KeyStride, TabletPeek.KeyDown);
        _saddle.AddChild(key);
        _tips.Add(key, () => (key as EdgeKey)?.Why() is { } why
            ? new Tip(T(says), new TipLine(T(why), Tone.Dim)) { Over = true }
            : new Tip(T(says)) { Over = true });
    }

    /// <summary>A tablet that rises from the edge: while it is up the keys ride on it.</summary>
    public void Follow(Tablet tablet) => _tablets.Add(tablet);

    /// <summary>Where it lies on the screen, the rim while it shows and the keys' metal: what the crew's markers keep out of.</summary>
    public Rect2 Covers => _rim.Visible ? _rim.Covers.Merge(_saddle.GetGlobalRect()) : _saddle.GetGlobalRect();

    public override void _Process(double delta)
    {
        bool shown = _inWorld();
        _root.Visible = shown;
        if (!shown)
        {
            _tips.Show(null);
            return;
        }
        var window = _root.GetViewportRect().Size;
        if (window != _window)
        {
            _window = window;
            _rim.LayAsRim(window);
            float metalTop = TabletPeek.ScreenTop(window.Y) - TabletLayout.BezelAbove;
            _metal.Position = new Vector2(_rim.Covers.Position.X, metalTop);
            _metal.Size = new Vector2(_rim.Covers.Size.X, window.Y - metalTop);
        }
        Tablet? up = null;
        foreach (var tablet in _tablets)
            if (tablet.Shown && (up == null || tablet.Up > up.Up)) up = tablet;
        _rim.Visible = _metal.Visible = up == null;
        var art = (up ?? _rim).Covers;
        var (x, y) = TabletPeek.SaddleAt(art.Position.X, art.Position.Y, art.Size.X);
        _saddle.Position = new Vector2(x, y);
        _tips.Frame(_ui.MenuOpen.Get() ? null : _root.GetViewport().GetMousePosition());
    }

    public override void _ExitTree() => _tips.Show(null);

    /// <summary>The metal the keys sit on, drawn as the build tablet's wings are, its corners cut and lit from above.</summary>
    private sealed partial class Saddle : Control
    {
        private const int U = TabletLayout.ArtScale;
        private static readonly Color Out = new("15181e"), Light = new("6a7385"), Side = new("5d6678"), Metal = new("4a5263"), Dark = new("3a404d"), Scratch = new("262b35");

        public Saddle()
        {
            MouseFilter = MouseFilterEnum.Stop;
            Size = CustomMinimumSize = new Vector2(TabletPeek.SaddleWide, TabletPeek.SaddleTall);
        }

        public override void _Draw()
        {
            float w = Size.X, h = Size.Y;
            DrawRect(new Rect2(U, 0, w - 2 * U, h), Out);
            DrawRect(new Rect2(0, U, w, h - 2 * U), Out);
            DrawRect(new Rect2(U, U, w - 2 * U, h - 2 * U), Metal);
            DrawRect(new Rect2(U, U, w - 2 * U, U), Light);
            DrawRect(new Rect2(U, h - 2 * U, w - 2 * U, U), Dark);
            DrawRect(new Rect2(w - 2 * U, 2 * U, U, h - 4 * U), Dark);
            DrawRect(new Rect2(U, 2 * U, U, h - 4 * U), Side);
            DrawRect(new Rect2(2 * U, 12, U, 8), Scratch);
        }
    }
}
