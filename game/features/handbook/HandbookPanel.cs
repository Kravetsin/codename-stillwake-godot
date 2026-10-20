using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Handbook;

/// <summary>
/// A section of the Handbook: its name on its tab, its body, a control the feature it speaks of builds and keeps
/// current while it is shown (the debris feature's <c>HandbookPage</c>), and what it says at the right of the tab
/// strip while it is chosen (the debris found), if anything.
/// </summary>
public sealed record HandbookSection(string Name, Control Body, Control? Head = null);

/// <summary>
/// The Handbook (0066), on the build menu's rugged tablet (0081): J lifts it from below the screen's bottom edge to the
/// middle and puts it away, Esc, Tab or E put it away; its screen comes on as the plan's. On the screen, the sections
/// as tabs, the chosen one lit, and what it says at the strip's right; under them the chosen section's body, scrolled
/// by the wheel anywhere over the tablet, a scrollbar beside it; along the foot the book, its name and the keys. Its
/// size is fixed by the slot size alone (<see cref="HandbookScreen"/>). Read only: it changes nothing in the world,
/// which runs on behind it. Opening it puts away what is open or in hand, as <c>Main</c> says (<see cref="Opening"/>),
/// so no feature reaches into another's.
/// </summary>
public partial class HandbookPanel : CanvasLayer
{
    private readonly Ui _ui;
    private readonly Func<bool> _inWorld;
    private readonly IReadOnlyList<HandbookSection> _sections;
    private readonly Tablet _tablet = new();
    private readonly Control _tabs = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly List<PanelContainer> _tabBoxes = [];
    /// <summary>The chosen section's body, seen through the view and scrolled up through it.</summary>
    private readonly Control _view = new() { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly (Panel Track, Panel Thumb) _bar = Tablet.ScrollBar();
    private readonly HBoxContainer _foot = Style.Gap(new HBoxContainer { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
    private HandbookScreen _at;
    private bool _open;
    private int _chosen, _scroll, _hoveredTab = -1;

    /// <param name="ui">The hand, freed as it opens, the menu, under which it takes no key, and the slot size it is laid out for.</param>
    /// <param name="inWorld">Whether a world is played: on the title J does nothing.</param>
    /// <param name="sections">Its sections, in the tabs' order: the first shown first.</param>
    public HandbookPanel(Ui ui, Func<bool> inWorld, IReadOnlyList<HandbookSection> sections)
    {
        (_ui, _inWorld, _sections) = (ui, inWorld, sections);
        Layer = ScreenLayers.Handbook;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Theme = new Theme { DefaultFont = Style.Mono, DefaultFontSize = Tablet.FontSize } };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        root.AddChild(_tablet);
        // The wheel anywhere over the tablet scrolls the body; it never reaches the hotbar or the camera.
        _tablet.GuiInput += e => Scroll(e, _tablet);
        _tablet.Bezel.GuiInput += e => Scroll(e, _tablet.Bezel);
        float x = 0;
        for (int i = 0; i < sections.Count; i++)
        {
            var tab = Tablet.Tab(sections[i].Name, null, x);
            int index = i;
            tab.GuiInput += e =>
            {
                if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
                Choose(index);
                tab.AcceptEvent();
            };
            tab.MouseEntered += () => Hover(index);
            tab.MouseExited += () => Hover(-1);
            _tabs.AddChild(tab);
            _tabBoxes.Add(tab);
            x += tab.CustomMinimumSize.X + 4;
            sections[i].Body.Visible = false;
            _view.AddChild(sections[i].Body);
            if (sections[i].Head is { } head)
            {
                // At the strip's right, as the build menu's search: as wide as it says, growing leftwards, as tall as the tabs.
                head.Visible = false;
                _tabs.AddChild(head);
                head.AnchorLeft = head.AnchorRight = 1;
                head.GrowHorizontal = Control.GrowDirection.Begin;
                (head.OffsetLeft, head.OffsetRight, head.OffsetTop, head.OffsetBottom) = (0, 0, 0, TabletLayout.TabHeight);
            }
        }
        var hint = Tablet.Text(T("handbook.hint"), Style.Slate500, Tablet.HintSize);
        hint.ClipText = true;
        hint.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        hint.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _foot.AddChild(Picture(AsciiArt.Book(), 20));
        _foot.AddChild(Tablet.Text(T("handbook.title"), Style.Slate100));
        _foot.AddChild(hint);
        foreach (var part in (Control[])[_tabs, _view, _bar.Track, _foot]) _tablet.OnScreen(part);
        LayOut();
        ui.SlotScale.Subscribe(LayOut);
        Choose(0);
    }

    /// <summary>What opening it puts away first: the inventory and the machine open, the plan. Set by <c>Main</c>.</summary>
    public Action? Opening { get; set; }

    /// <summary>Whether it is out: what Esc puts away before it opens the menu. Put away, it may still be sliding down.</summary>
    public bool Shown => _open;

    /// <summary>Takes it out on the section chosen last, putting away what is open or in hand.</summary>
    public void Open()
    {
        if (_open) return;
        Opening?.Invoke();
        _ui.Hand.Set(Holding.Free);
        _open = true;
    }

    /// <summary>Puts it away: J again, Esc through the menu's <c>Escape</c>, Tab or E.</summary>
    public void Close() => _open = false;

    /// <summary>Every part placed for the slot size now: the screen's size changes with it and nothing else.</summary>
    private void LayOut()
    {
        _at = new HandbookScreen(_ui.Slot);
        _tablet.Size = new Vector2(_at.Width, _at.Height);
        _tabs.Position = new Vector2(TabletLayout.Pad, TabletLayout.Pad);
        _tabs.Size = new Vector2(_at.Width - 2 * TabletLayout.Pad, TabletLayout.TabHeight);
        _view.Position = new Vector2(TabletLayout.Pad, HandbookScreen.BodyTop);
        _view.Size = new Vector2(_at.BodyWidth, _at.BodyHeight);
        _bar.Track.Position = new Vector2(_at.BarLeft, HandbookScreen.BodyTop);
        _bar.Track.Size = new Vector2(TabletLayout.BarWidth, _at.BodyHeight);
        _foot.Position = new Vector2(TabletLayout.Pad, _at.FootTop);
        _foot.Size = new Vector2(_at.Width - 2 * TabletLayout.Pad, TabletLayout.FootHeight);
    }

    private void Choose(int index)
    {
        if (_sections.Count == 0) return;
        (_chosen, _scroll) = (index, 0);
        for (int i = 0; i < _sections.Count; i++)
        {
            _sections[i].Body.Visible = i == index;
            if (_sections[i].Head is { } head) head.Visible = i == index;
        }
        PaintTabs();
    }

    private void Hover(int index)
    {
        _hoveredTab = index;
        PaintTabs();
    }

    private void PaintTabs()
    {
        for (int i = 0; i < _tabBoxes.Count; i++) Tablet.PaintTab(_tabBoxes[i], i == _chosen, i == _hoveredTab);
    }

    /// <summary>The wheel over the screen or its bezel scrolls the body a row of kinds a notch.</summary>
    private void Scroll(InputEvent e, Control over)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel) return;
        _scroll = _at.Scrolled(_scroll, wheel.ButtonIndex == MouseButton.WheelDown ? 1 : -1, BodyHeight());
        over.AcceptEvent();
    }

    /// <summary>How tall the chosen section's body is now; none without sections.</summary>
    private int BodyHeight() => _sections.Count == 0 ? 0 : (int)Mathf.Ceil(_sections[_chosen].Body.GetCombinedMinimumSize().Y);

    /// <summary>
    /// The tablet slid and its screen lit for this frame (<see cref="Tablet.Step"/>), resting in the middle of the
    /// screen, the body scrolled where the wheel left it, kept within it as it grows or shrinks.
    /// </summary>
    public override void _Process(double delta)
    {
        _tablet.Step(_open, delta);
        if (!_tablet.Shown) return;
        var screen = _tablet.GetViewportRect().Size;
        // Centred with its bezel, which reaches further above and to the right of the screen than below and to the left.
        const int s = TabletLayout.ArtScale;
        var art = new Vector2(_at.Width + TabletLayout.ArtAround * s, _at.Height + TabletLayout.ArtAround * s);
        var rest = ((screen - art) / 2 + new Vector2(TabletLayout.ScreenLeft * s, TabletLayout.ScreenTop * s)).Round();
        _tablet.Position = rest + new Vector2(0, _tablet.Drop(rest.Y));
        int height = BodyHeight();
        _scroll = _at.Scrolled(_scroll, 0, height);
        if (_sections.Count > 0)
        {
            var body = _sections[_chosen].Body;
            body.Position = new Vector2(0, -_scroll);
            body.Size = new Vector2(_at.BodyWidth, Mathf.Max(height, _at.BodyHeight));
        }
        Tablet.PlaceThumb(_bar.Thumb, TabletLayout.Thumb(_at.BodyHeight, height, _scroll));
    }

    /// <summary>A picture <paramref name="size"/> wide and high at most, its pixels square, centred.</summary>
    private static TextureRect Picture(Texture2D texture, int size) => new()
    {
        Texture = texture,
        CustomMinimumSize = new Vector2(size, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    /// <summary>Tab and E put it away and go on to what they do.</summary>
    public override void _Input(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false } key || !_open) return;
        // Not taken: Tab opens the inventory, E works what is in reach.
        if (key.PhysicalKeycode is Key.Tab or Key.E) Close();
    }

    /// <summary>J takes it out and puts it away, in a world with the menu shut.</summary>
    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.J }) return;
        if (!_inWorld() || _ui.MenuOpen.Get()) return;
        if (_open) Close();
        else Open();
        GetViewport().SetInputAsHandled();
    }
}
