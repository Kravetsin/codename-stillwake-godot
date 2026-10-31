using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using static Stillwake.Game.Features.Research.StripParts;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The research tablet (2026-10-30-tech-rings), nearly the whole screen: T takes it out anywhere and puts it away; Esc, Tab, E and B put
/// it away too. A tab a page (<see cref="ITabletPage"/>) over a strip about what is picked, the credits above. The world runs on behind it.
/// </summary>
public partial class ResearchTablet : CanvasLayer
{
    /// <summary>Seconds between reads of the world: a panel reads on a timer, never every frame.</summary>
    private const double ReadEvery = 0.25;
    /// <summary>The gap kept between the tablet's art and the window's edge, and the height of the strip about what is picked.</summary>
    private const int Margin = 12, DetailTall = 116, ButtonWide = 200;

    private readonly Ui _ui;
    private readonly Func<bool> _inWorld;
    private readonly Func<Registry?> _world;
    private readonly Tablet _tablet = new();
    private readonly Control _strip = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly HBoxContainer _head = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, GrowHorizontal = Control.GrowDirection.Begin, Alignment = BoxContainer.AlignmentMode.End }, 16);
    private readonly CreditLine _credits = new(Tablet.FontSize + 1);
    private readonly Label _progress = Text("", Style.Slate100);
    private readonly Control _view = new() { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly Panel _detail = new() { MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly TipSpots _tips;
    private readonly StripParts _parts;
    private readonly List<ITabletPage> _pages = [];
    private readonly List<PanelContainer> _tabBoxes = [];
    private ITabletPage _page;
    private double _sinceRead = ReadEvery;
    private int _hoveredTab = -1;

    /// <param name="ui">Whether it is out, the hand freed as it comes, the menu, under which it takes no key.</param>
    /// <param name="inWorld">Whether a world is played: on the title T does nothing.</param>
    /// <param name="world">The world the hero stands in; null with none played.</param>
    /// <param name="peer">This player's peer: whose bag a deposit is taken from.</param>
    /// <param name="send">Sends a command to the session.</param>
    public ResearchTablet(Ui ui, Func<bool> inWorld, Func<Registry?> world, Func<int> peer, Action<Command> send)
    {
        (_ui, _inWorld, _world) = (ui, inWorld, world);
        _tips = new TipSpots(ui.Tip);
        _parts = new StripParts(_tips);
        Layer = ScreenLayers.Handbook;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, TextureFilter = CanvasItem.TextureFilterEnum.Linear, Theme = new Theme { DefaultFont = Style.Clear, DefaultFontSize = Tablet.FontSize + 1 } };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        root.AddChild(_tablet);
        float x = 0;
        void AddTab(string title, ITabletPage page)
        {
            int index = _pages.Count;
            _pages.Add(page);
            var box = Tablet.Tab(title, null, x);
            foreach (var label in box.FindChildren("*", nameof(Label), true, false).OfType<Label>()) label.LabelSettings.Font = Style.Clear;
            box.GuiInput += e =>
            {
                if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
                Choose(_pages[index]);
                box.AcceptEvent();
            };
            box.MouseEntered += () => HoverTab(index);
            box.MouseExited += () => HoverTab(-1);
            _strip.AddChild(box);
            _tabBoxes.Add(box);
            x += box.CustomMinimumSize.X + 4;
        }
        AddTab(T("research.tab.board"), new BoardPage(_view, _parts, () => _sinceRead = ReadEvery));
        foreach (var tab in Content.Research.Tabs) AddTab(T($"research.tab.{tab.Id}"), new RingsPage(tab, _view, _parts, peer, send, () => _sinceRead = ReadEvery));
        _page = _pages[0];
        _head.AddChild(_progress);
        _head.AddChild(_credits);
        _strip.AddChild(_head);
        _view.GuiInput += e => _page.Pointer(e);
        var look = new StyleBoxFlat { BgColor = new Color("0d1218"), BorderColor = Line };
        look.SetBorderWidthAll(1);
        look.SetCornerRadiusAll(2);
        _detail.AddThemeStyleboxOverride("panel", look);
        _detail.AddChild(_parts.Facts);
        var action = _parts.Action;
        action.ClipText = true;
        action.AddThemeFontOverride("font", Style.Clear);
        action.AddThemeFontSizeOverride("font_size", Tablet.FontSize + 1);
        PaintAction();
        _detail.AddChild(action);
        _parts.ActionLine.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _parts.ActionLine.AddChild(Text(T("research.buy"), Style.Slate100, Tablet.FontSize + 3));
        _parts.ActionLine.AddChild(_parts.ActionPrice);
        action.AddChild(_parts.ActionLine);
        // The wheel over the bezel zooms as over the map; it never reaches the hotbar.
        _tablet.GuiInput += e => _page.Zoom(e, _tablet);
        _tablet.Bezel.GuiInput += e => _page.Zoom(e, _tablet.Bezel);
        foreach (var part in (Control[])[_strip, _view, _detail]) _tablet.OnScreen(part);
        PaintTabs();
        _page.Show(true);
    }

    /// <summary>Whether it is out: what Esc puts away before it opens the menu.</summary>
    public bool Shown => _ui.TreeOpen.Get();

    /// <summary>Takes it out, the hand freed. What it puts away is <c>Main</c>'s to say, as it listens to <see cref="Ui.TreeOpen"/>.</summary>
    public void Open()
    {
        if (Shown) return;
        _ui.Hand.Set(Holding.Free);
        _page.Show(true);
        _sinceRead = ReadEvery;
        _ui.TreeOpen.Set(true);
    }

    /// <summary>Puts it away: T again, Esc through the menu's <c>Escape</c>, Tab, E or B.</summary>
    public void Close()
    {
        _ui.TreeOpen.Set(false);
        _page.LetGo();
    }

    /// <summary>Where a thing's middle is on the page shown: what a recording points at.</summary>
    public Vector2 ScreenOf(string id) => _page.ScreenOf(id);

    /// <summary>Where the button buying what is picked is on the screen: what a recording points at.</summary>
    public Vector2 ActionAt => _parts.Action.GetGlobalRect().GetCenter();

    /// <summary>Where the hub's middle is on the screen: what a recording points at.</summary>
    public Vector2 HubAt => ((RingsPage)_page).HubAt;

    /// <summary>Where tab <paramref name="index"/> is on the screen: what a recording points at.</summary>
    public Vector2 TabAt(int index) => _tabBoxes[index].GetGlobalRect().GetCenter();

    public override void _Process(double delta)
    {
        bool open = Shown && _world() != null;
        _tablet.Step(open, delta);
        if (!_tablet.Shown) return;
        LayOut();
        _page.Step(delta);
        var mouse = _view.GetViewport().GetMousePosition();
        bool over = _view.GetGlobalRect().HasPoint(mouse) || _detail.GetGlobalRect().HasPoint(mouse);
        _tips.Frame(open && !_ui.MenuOpen.Get() && over ? mouse : null);
        if (!open) return;
        _sinceRead += delta;
        if (_sinceRead < ReadEvery) return;
        _sinceRead = 0;
        Read(_world()!);
    }

    /// <summary>The tablet at rest in the middle of the window, as large as its art lets it be, and the strips on its screen.</summary>
    private void LayOut()
    {
        var screen = _tablet.GetViewportRect().Size;
        const int s = TabletLayout.ArtScale, around = TabletLayout.ArtAround * s, pad = TabletLayout.Pad;
        float width = Mathf.Floor(screen.X - around - 2 * Margin), height = Mathf.Floor(screen.Y - around - 2 * Margin);
        _tablet.Size = new Vector2(width, height);
        var rest = ((screen - new Vector2(width + around, height + around)) / 2 + new Vector2(TabletLayout.ScreenLeft * s, TabletLayout.ScreenTop * s)).Round();
        _tablet.Position = rest + new Vector2(0, _tablet.Drop(rest.Y));
        const int bodyTop = pad + TabletLayout.TabHeight + pad;
        _strip.Position = new Vector2(pad, pad);
        _strip.Size = new Vector2(width - 2 * pad, TabletLayout.TabHeight);
        _head.Size = new Vector2(_head.GetCombinedMinimumSize().X, TabletLayout.TabHeight);
        _head.Position = new Vector2(_strip.Size.X - _head.Size.X, 0);
        _detail.Position = new Vector2(pad, height - pad - DetailTall);
        _detail.Size = new Vector2(width - 2 * pad, DetailTall);
        _parts.Facts.Position = new Vector2(pad, pad);
        _parts.Facts.Size = new Vector2(_detail.Size.X - ButtonWide - 3 * pad, DetailTall - 2 * pad);
        _parts.Action.Position = new Vector2(_detail.Size.X - ButtonWide - pad, pad);
        _parts.Action.Size = new Vector2(ButtonWide, DetailTall - 2 * pad);
        _view.Position = new Vector2(pad, bodyTop);
        _view.Size = new Vector2(width - 2 * pad, _detail.Position.Y - pad - bodyTop);
    }

    /// <summary>The head's credits and progress, then every page: each restyles only what has changed.</summary>
    private void Read(Registry m)
    {
        var research = m.Research;
        _credits.Set(research.Credits);
        var all = Content.Research.Nodes;
        _progress.Text = T("research.progress", ("done", all.Count(research.Done)), ("all", all.Count));
        foreach (var page in _pages) page.Read(m);
    }

    private void Choose(ITabletPage page)
    {
        if (page == _page) return;
        _page.Show(false);
        _page = page;
        _page.Show(true);
        _page.Fill();
        _sinceRead = ReadEvery;
        PaintTabs();
    }

    private void HoverTab(int index)
    {
        _hoveredTab = index;
        PaintTabs();
    }

    private void PaintTabs()
    {
        for (int i = 0; i < _tabBoxes.Count; i++) Tablet.PaintTab(_tabBoxes[i], _pages[i] == _page, i == _hoveredTab);
    }

    /// <summary>Amber to be pressed, dark with its reason when it cannot.</summary>
    private void PaintAction()
    {
        StyleBoxFlat Look(Color fill, Color edge)
        {
            var box = new StyleBoxFlat { BgColor = fill, BorderColor = edge };
            box.SetBorderWidthAll(2);
            box.SetCornerRadiusAll(3);
            return box;
        }
        var action = _parts.Action;
        action.AddThemeStyleboxOverride("normal", Look(new Color("6b4e14"), Warm));
        action.AddThemeStyleboxOverride("hover", Look(new Color("8a661c"), Warm));
        action.AddThemeStyleboxOverride("pressed", Look(new Color("4f3a0f"), Warm));
        action.AddThemeStyleboxOverride("disabled", Look(new Color("14181f"), Line));
        action.AddThemeColorOverride("font_color", Style.Slate100);
        action.AddThemeColorOverride("font_hover_color", Colors.White);
        action.AddThemeColorOverride("font_pressed_color", Colors.White);
        action.AddThemeColorOverride("font_disabled_color", Faint);
    }

    /// <summary>Tab, E and B put it away and go on to what they do.</summary>
    public override void _Input(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false } key || !Shown) return;
        if (key.PhysicalKeycode is Key.Tab or Key.E or Key.B) Close();
    }

    /// <summary>T takes it out and puts it away, in a world with the menu shut.</summary>
    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.T }) return;
        if (!_inWorld() || _ui.MenuOpen.Get()) return;
        if (Shown) Close();
        else Open();
        GetViewport().SetInputAsHandled();
    }
}
