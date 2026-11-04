using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Shared;

/// <summary>
/// The tablet's top edge along the window's foot and the keys on it, in place of words ([tablet-edge](../../../docs/decisions/2026-11-02-tablet-edge.md)): the same art as every
/// tablet's, across the window, its screen dark. A tablet taken out rises from it (<see cref="TabletPeek.Pose"/>), so it shows while none is up; the keys, each a feature's own,
/// ride on the top of whichever is, so they stay in reach. A key says its name and hotkey over it, under the pointer. The crew's credits ride with them,
/// on the bezel's status display in place of its charge (market-key), so they are always in sight.
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
    private readonly CreditDisplay _display;
    private readonly List<Tablet> _tablets = [];
    private readonly TipSpots _tips;
    private Vector2 _window;
    private int _keys;

    /// <param name="ui">The one tooltip, and the menu, under which no key speaks.</param>
    /// <param name="inWorld">Whether a world is played: with none it is hidden.</param>
    /// <param name="credits">The crew's credits; null with no world played.</param>
    public TabletEdge(Ui ui, Func<bool> inWorld, Func<int?> credits)
    {
        (_ui, _inWorld) = (ui, inWorld);
        _display = new CreditDisplay(credits);
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.TabletEdge;
        // After the tablets have moved this frame, so the keys never trail the top they ride on.
        ProcessPriority = 1;
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);
        _root.AddChild(_rim);
        _root.AddChild(_metal);
        _root.AddChild(_saddle);
        _root.AddChild(_display);
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
        var (dx, dy) = TabletPeek.DisplayAt(art.End.X, art.Position.Y);
        _display.Position = new Vector2(dx, dy);
        _tips.Frame(_ui.MenuOpen.Get() ? null : _root.GetViewport().GetMousePosition());
    }

    public override void _ExitTree() => _tips.Show(null);

    /// <summary>The credits on a dark display over the art's charge: the coin and the number, read on a timer, counted to and flared (<see cref="CountFlare"/>).</summary>
    private sealed partial class CreditDisplay : Control
    {
        private const double ReadEvery = 0.25;
        private static readonly Color Glass = new("0b0f12"), Rim = new("1c232b"), Gold = new("f0c050");
        private static readonly Texture2D Coin = Signs.Credit();
        private readonly Func<int?> _credits;
        private int? _shown;
        private CountFlare _flare = new();
        private double _since = ReadEvery;

        public CreditDisplay(Func<int?> credits)
        {
            _credits = credits;
            MouseFilter = MouseFilterEnum.Ignore;
            Size = new Vector2(TabletPeek.DisplayWide, TabletPeek.DisplayTall);
        }

        public override void _Process(double delta)
        {
            _flare.Step(delta);
            if (_flare.Heat > 0 || _flare.Shown != _shown) QueueRedraw();
            _since += delta;
            if (_since < ReadEvery) return;
            _since = 0;
            int? now = _credits();
            // With no world the next one starts afresh, shown at once.
            if (now is { } credits) _flare.Read(credits);
            else _flare = new();
            if (now == _shown) return;
            _shown = now;
            QueueRedraw();
        }

        public override void _Draw()
        {
            DrawRect(new Rect2(Vector2.Zero, Size), Rim);
            DrawRect(new Rect2(Vector2.One, Size - 2 * Vector2.One), Glass);
            if (_shown == null) return;
            const int size = 11, coin = 12;
            float right = Size.X - 3;
            float wide = FlareText.DrawRight(this, Style.Clear, size, _flare.Shown.ToString(), right, Size.Y / 2 + size * 0.36f, Gold,
                _flare.Up ? FlareText.Rise : FlareText.Fall, _flare.HeatOf, _flare.Up);
            DrawTextureRect(Coin, new Rect2(right - wide - 2 - coin, (Size.Y - coin) / 2, coin, coin), false);
        }
    }

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
