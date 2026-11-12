using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Shared;

/// <summary>
/// The tablet's top edge along the window's foot and the keys over it, in place of words ([tablet-edge](../../../docs/decisions/2026-11-02-tablet-edge.md)): the same art as every
/// tablet's, across the window, its screen dark. A tablet taken out rises from it (<see cref="TabletPeek.Pose"/>), so it shows while none is up. The keys are holograms
/// floating over the top of whichever is up (hologram-keys): only those that can be used now, the row closing up round them (<see cref="EdgeRow"/>), dim unless the
/// pointer nears the screen's foot, as large as the interface's size has them; the crew's credits on the right (<see cref="CreditHolo"/>). A key says its name and hotkey over it, under the pointer.
/// </summary>
public partial class TabletEdge : CanvasLayer
{
    /// <summary>How long the rest of the row takes to slide as a key comes or goes, and how long a key flickers in or out.</summary>
    private const double SlideS = 0.25, FlickerS = 0.35;
    /// <summary>How far in from the rim's right edge the credits end, a tablet up or not: short of the bezel's charge lamps, off the keys in the middle.</summary>
    private const float ReadoutIn = 160;

    private readonly Ui _ui;
    private readonly Func<bool> _inWorld;
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Tablet _rim = new();
    /// <summary>Over the rim's metal: a click on it is no click on the world behind.</summary>
    private readonly Control _metal = new() { MouseFilter = Control.MouseFilterEnum.Stop };
    /// <summary>The span of the row: a click between its keys is no click on the world either.</summary>
    private readonly Control _row = new() { MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly List<Slot> _slots = [];
    private readonly List<Tablet> _tablets = [];
    private readonly List<Tablet> _keysAway = [];
    private readonly TipSpots _tips;
    private readonly KeyGlow _near = new(comesOn: 0.12, goesOut: 0.5);
    private CreditHolo? _readout;
    private Func<bool> _readoutShown = () => false;
    private float _readoutAppear;
    private Vector2 _window;
    private bool _laid;

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
        _root.AddChild(_row);
    }

    /// <summary>
    /// Sets <paramref name="key"/> on the edge, next along, saying the i18n key <paramref name="says"/> under the pointer, shown while
    /// <paramref name="shown"/> says it can be used, or always with none.
    /// </summary>
    public void Put(EdgeKey key, string says, Func<bool>? shown = null)
    {
        _slots.Add(new Slot(key, shown ?? (() => true)));
        _root.AddChild(key);
        _tips.Add(key, () => new Tip(T(says)) { Over = true });
    }

    /// <summary>Sets the crew's credits on the right of the edge, saying <paramref name="says"/> under the pointer, shown while <paramref name="shown"/>.</summary>
    public void Readout(CreditHolo holo, string says, Func<bool> shown)
    {
        (_readout, _readoutShown) = (holo, shown);
        _root.AddChild(holo);
        _tips.Add(holo, () => new Tip(T(says)) { Over = true });
    }

    /// <summary>
    /// A tablet that rises from the edge: while it is up the keys ride on it, or, <paramref name="keysAway"/>, none shows at all (the game
    /// menu's, settings-menu: its keys could not be used under it, and the row sliding over its top read as a jumble).
    /// </summary>
    public void Follow(Tablet tablet, bool keysAway = false)
    {
        _tablets.Add(tablet);
        if (keysAway) _keysAway.Add(tablet);
    }

    /// <summary>Where it lies on the screen, the rim while it shows and the keys' row: what the crew's markers keep out of.</summary>
    public Rect2 Covers
    {
        get
        {
            var keys = _readout is { Visible: true } r ? _row.GetGlobalRect().Merge(r.GetGlobalRect()) : _row.GetGlobalRect();
            return _rim.Visible ? _rim.Covers.Merge(keys) : keys;
        }
    }

    /// <summary>Where the tablet the keys ride lies on the screen, the one up or the rim, its top the metal's: what a find's flash runs along.</summary>
    public Rect2 Art { get; private set; }

    public override void _Process(double delta)
    {
        bool shown = _inWorld();
        _root.Visible = shown;
        if (!shown)
        {
            _tips.Show(null);
            _ui.EdgeTop = null;
            _laid = false;
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
        var art = Art = (up ?? _rim).Covers;
        var (middle, foot) = TabletPeek.RowAt(art.Position.X, art.Position.Y, art.Size.X);
        float grown = EdgeRow.Grown(_ui.Slot), top = EdgeRow.Top(foot, EdgeKey.Side, grown);
        _ui.EdgeTop = top;
        // The credits stay where the rim has them, a tablet up or not: over a narrower one they ran into the keys.
        Lay(middle, top, _rim.Covers.End.X - ReadoutIn, grown, delta);
        if (_keysAway.Exists(t => t.Shown)) HideKeys();
        var pointer = _root.GetViewport().GetMousePosition();
        _near.Step(delta, EdgeRow.Bright(pointer.Y, top, up != null && art.HasPoint(pointer), new Rect2(Vector2.Zero, window).HasPoint(pointer)));
        foreach (var slot in _slots) slot.Key.Near = (float)_near.Lit;
        if (_readout != null) _readout.Near = (float)_near.Lit;
        _tips.Frame(_ui.MenuOpen.Get() ? null : pointer);
    }

    /// <summary>Puts each key where the row has it, the rest sliding as one comes or goes, and the row's span under them; the credits ending at <paramref name="readoutRight"/>.</summary>
    private void Lay(float middle, float top, float readoutRight, float grown, double delta)
    {
        var shown = new bool[_slots.Count];
        for (int i = 0; i < _slots.Count; i++) shown[i] = _slots[i].Shown();
        var lefts = EdgeRow.Lefts(shown, middle, EdgeKey.Side * grown, TabletPeek.KeyStride * grown);
        float left = float.MaxValue, right = float.MinValue;
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            // Opening a world shows the row as it stands, with no key flickering in.
            if (!_laid) slot.Appear = shown[i] ? 1 : 0;
            slot.Appear = Mathf.MoveToward(slot.Appear, shown[i] ? 1 : 0, (float)(delta / FlickerS));
            if (lefts[i] is { } to)
            {
                if (!_laid || slot.To == null) (slot.From, slot.Along) = (to, 1);
                else if (to != slot.To) (slot.From, slot.Along) = (slot.X, 0);
                slot.To = to;
            }
            slot.Along = Math.Min(1, slot.Along + delta / SlideS);
            float t = (float)slot.Along;
            slot.X = Mathf.Lerp(slot.From, slot.To ?? slot.From, t * t * (3 - 2 * t));
            var key = slot.Key;
            key.Visible = slot.Appear > 0;
            key.MouseFilter = shown[i] ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
            key.Shows = Flicker(slot.Appear);
            key.Scale = Vector2.One * grown;
            key.Position = new Vector2(Mathf.Round(slot.X), top);
            if (!shown[i]) continue;
            left = Math.Min(left, slot.X);
            right = Math.Max(right, slot.X + EdgeKey.Side * grown);
        }
        if (left == float.MaxValue) (left, right) = (middle, middle);
        if (_readout is { } readout)
        {
            bool on = _readoutShown();
            _readoutAppear = !_laid ? (on ? 1 : 0) : Mathf.MoveToward(_readoutAppear, on ? 1 : 0, (float)(delta / FlickerS));
            readout.Visible = _readoutAppear > 0;
            readout.MouseFilter = on ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
            readout.Shows = Flicker(_readoutAppear);
            readout.Lay(readoutRight, top, grown);
        }
        _row.Position = new Vector2(left, top);
        _row.Size = new Vector2(right - left, (EdgeKey.Side + EdgeKey.NumberDown + 2) * grown);
        _laid = true;
    }

    /// <summary>Every key and the credits gone this frame, and the row's span: they come back as laid the next.</summary>
    private void HideKeys()
    {
        foreach (var slot in _slots) slot.Key.Visible = false;
        if (_readout != null) _readout.Visible = false;
        _row.Size = Vector2.Zero;
    }

    // Flickering in or out, it drops out every third frame of thirty a second, as a tube catching.
    private static float Flicker(float appear) => appear >= 1 ? 1 : Mathf.FloorToInt(Time.GetTicksMsec() / 33.3f) % 3 == 0 ? appear * 0.2f : appear;

    public override void _ExitTree() => _tips.Show(null);

    /// <summary>A key on the row and where it is on its way.</summary>
    private sealed class Slot(EdgeKey key, Func<bool> shown)
    {
        public EdgeKey Key { get; } = key;
        public Func<bool> Shown { get; } = shown;
        public float Appear { get; set; }
        public float X { get; set; }
        public float From { get; set; }
        public float? To { get; set; }
        public double Along { get; set; } = 1;
    }
}
