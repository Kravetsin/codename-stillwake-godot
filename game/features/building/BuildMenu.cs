using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Placement = Stillwake.Sim.Grid.Placement;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// The plan in hand (features/building/BuildMenu.tsx, 0022, 0030), taken out with B and shown the whole while it is
/// out: a catalogue docked at the bottom middle, its size fixed whatever the tab or how many cards it holds (0073).
/// Its tabs of what can be built, each with its count — each tab the mode, its overlay showing while it is open — and
/// a search across every tab (/); under them the cards, the inventory's slots, scrolled a row at a time by the wheel
/// anywhere over the panel; each card a picture of what it builds, its name, cost and use in the one tooltip by the
/// pointer, so a card is swapped for another at a click and the menu stays open. A plan costs nothing until it is
/// worked, an item a part (0063), so a card the player carries too little for is as bright as any. It opens on the
/// tab it was closed on. Under the cards, the card in hand and what the buttons do on the tab. B or Esc puts the plan
/// away; with it away, the key that takes it out. Q picks the card under the pointer. Only what the world knows is
/// shown (0024): a tab with nothing known, not at all, nor its mode. It is the screen of a rugged tablet (0073), which
/// slides up from the bottom edge as the plan comes out and back down as it goes away.
/// </summary>
public partial class BuildMenu : CanvasLayer
{
    private static readonly Color Sky500 = new("0ea5e9"), Sky950 = new("082f49");
    private static readonly Font Mono = Render.Style.Mono;
    private const int FontSize = Tablet.FontSize;

    /// <summary>How far below its dock the tablet is drawn now, in screen pixels.</summary>
    private int _drop;

    private readonly PlanHand _hand;
    private readonly Ui _ui;
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly PanelContainer _closed;
    /// <summary>The tablet, its screen the catalogue.</summary>
    private readonly Tablet _open = new();
    private readonly Control _tabs = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly LineEdit _search = new() { ContextMenuEnabled = false, CaretBlink = true, MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly Control _lens = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly PanelContainer _slash;
    private readonly Control _grid = new() { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Pass };
    private readonly Label _none = Tablet.Text(En.T("build.none"), Render.Style.Slate500);
    private readonly (Panel Track, Panel Thumb) _bar = Tablet.ScrollBar();
    private readonly HBoxContainer _foot = new() { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly TextureRect _heldPicture = Picture(20);
    private readonly Label _heldName = Tablet.Text("", Render.Style.Slate100), _hint = Tablet.Text("", Render.Style.Slate500, Tablet.HintSize);
    private readonly List<(Panel Box, TextureRect Picture, Label Count)> _cards = [];
    /// <summary>Beside the tablet while the plan is out: the three layers stacked, the focused one lit (0110).</summary>
    private readonly Control _focus = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private const int FocusWidth = FocusIcon + 8, FocusHeight = 3 * (FocusIcon + 4) + 4;
    private const int FocusIcon = 22;
    private readonly List<(PanelContainer Box, string Id)> _tabBoxes = [];

    private Catalogue _at;
    /// <summary>The cards shown: the open tab's, or every tab's that the search names.</summary>
    private List<Tool> _list = [];
    /// <summary>The first row of cards shown.</summary>
    private int _top;
    private Tool? _hovered, _pinned;
    private string? _hoveredTab;
    private string _tabsShown = "", _shown = "";
    /// <summary>Another card came into the hand: its row is scrolled into view at the next frame.</summary>
    private bool _reveal;
    /// <summary>The card under the pointer says what it is, costs and does in the one tooltip.</summary>
    private readonly TipSpots _tips;

    public BuildMenu(PlanHand hand, Ui ui)
    {
        _hand = hand;
        _ui = ui;
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.Bars;
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.Theme = new Theme { DefaultFont = Mono, DefaultFontSize = FontSize };
        AddChild(_root);

        _closed = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        _closed.AddThemeStyleboxOverride("panel", Render.Style.Box(Render.Style.Slate900 with { A = 0.9f }, Render.Style.Slate700, 4));
        _closed.AddChild(Tablet.Text(En.T("build.open"), Render.Style.Slate400));
        _closed.AnchorLeft = _closed.AnchorRight = 0.5f;
        _closed.AnchorTop = _closed.AnchorBottom = 1;
        _closed.GrowHorizontal = Control.GrowDirection.Both;
        _closed.GrowVertical = Control.GrowDirection.Begin;
        _closed.OffsetBottom = -Slots.Margin;
        _root.AddChild(_closed);

        _open.AnchorLeft = _open.AnchorRight = 0.5f;
        _open.AnchorTop = _open.AnchorBottom = 1;
        // The wheel anywhere over the tablet scrolls the cards; it never reaches the hotbar or the camera.
        _open.GuiInput += e => Scroll(e, _open);
        _open.Bezel.GuiInput += e => Scroll(e, _open.Bezel);
        _grid.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } press) return;
            if (_at.CardAt(press.Position.X, press.Position.Y, _top, _list.Count) is int i)
            {
                if (_list[i] is HandCard card) _hand.PickToolCard(card);
                else Pick(_list[i]);
            }
            _grid.AcceptEvent();
        };
        _root.AddChild(_open);
        _focus.AnchorLeft = _focus.AnchorRight = 0.5f;
        _focus.AnchorTop = _focus.AnchorBottom = 1;
        _focus.Draw += DrawFocus;
        _root.AddChild(_focus);
        hand.Focus.Subscribe(_focus.QueueRedraw);

        SearchField();
        _slash = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var keyBox = Render.Style.Box(Colors.Transparent, Render.Style.Slate700, radius: 3);
        (keyBox.ContentMarginLeft, keyBox.ContentMarginRight) = (4, 4);
        _slash.AddThemeStyleboxOverride("panel", keyBox);
        _slash.AddChild(Tablet.Text("/", Render.Style.Slate500, Tablet.HintSize));
        _lens.Draw += () =>
        {
            _lens.DrawArc(new Vector2(12, 11), 4, 0, Mathf.Tau, 16, Render.Style.Slate500, 1.5f, true);
            _lens.DrawLine(new Vector2(15, 14), new Vector2(18, 17), Render.Style.Slate500, 1.5f, true);
        };
        _search.AddChild(_lens);
        _search.AddChild(_slash);

        _none.Position = new Vector2(4, 10);
        _grid.AddChild(_none);
        _foot.AddThemeConstantOverride("separation", 6);
        _hint.ClipText = true;
        _hint.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _hint.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _foot.AddChild(_heldPicture);
        _foot.AddChild(_heldName);
        _foot.AddChild(_hint);
        foreach (var part in (Control[])[_tabs, _search, _grid, _bar.Track, _foot]) _open.OnScreen(part);

        LayOut();
        ui.SlotScale.Subscribe(LayOut);
        hand.Selected.Subscribe(() => _reveal = true);
        hand.Tab.Subscribe(() => _top = 0);
    }

    private void SearchField()
    {
        _search.PlaceholderText = En.T("build.search");
        _search.AddThemeFontOverride("font", Mono);
        _search.AddThemeFontSizeOverride("font_size", FontSize);
        _search.AddThemeColorOverride("font_color", Render.Style.Slate100);
        _search.AddThemeColorOverride("font_placeholder_color", Render.Style.Slate500);
        _search.AddThemeColorOverride("caret_color", Render.Style.Slate100);
        StyleBoxFlat Field(Color border)
        {
            var box = Render.Style.Box(Render.Style.Slate950, border);
            (box.ContentMarginLeft, box.ContentMarginRight, box.ContentMarginTop, box.ContentMarginBottom) = (24, 8, 0, 0);
            return box;
        }
        _search.AddThemeStyleboxOverride("normal", Field(Render.Style.Slate700));
        var focus = Field(Sky500);
        focus.DrawCenter = false;
        _search.AddThemeStyleboxOverride("focus", focus);
        _search.TextChanged += _ => _top = 0;
        _search.TextSubmitted += _ => _search.ReleaseFocus();
    }

    /// <summary>Every part placed for the slot size now: the panel's size changes with it and nothing else.</summary>
    private void LayOut()
    {
        _at = new Catalogue(_ui.Slot);
        _open.OffsetLeft = -(_at.Width / 2);
        _open.OffsetRight = _at.Width - _at.Width / 2;
        Dock();
        _tabs.Position = new Vector2(Catalogue.Pad, Catalogue.Pad);
        _grid.Position = new Vector2(Catalogue.Pad, _at.GridTop);
        _grid.Size = new Vector2(_at.GridWidth, _at.GridHeight);
        _bar.Track.Position = new Vector2(_at.BarLeft, _at.GridTop);
        _bar.Track.Size = new Vector2(Catalogue.BarWidth, _at.GridHeight);
        _foot.Position = new Vector2(Catalogue.Pad, _at.FootTop);
        _foot.Size = new Vector2(_at.Width - 2 * Catalogue.Pad, Catalogue.FootHeight);
        foreach (var (box, _, _) in _cards) box.QueueFree();
        _cards.Clear();
        int icon = _at.Icon;
        for (int i = 0; i < Catalogue.Columns * Catalogue.Rows; i++)
        {
            var box = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore, Size = new Vector2(_at.Card, _at.Card) };
            var picture = Picture(icon);
            picture.Position = new Vector2((_at.Card - icon) / 2, (_at.Card - icon) / 2);
            box.AddChild(picture);
            // How many of its item the player carries, a hand tool's card's only (0110).
            var count = Tablet.Text("", Render.Style.Slate100, Tablet.HintSize);
            count.Position = new Vector2(3, _at.Card - 16);
            box.AddChild(count);
            _grid.AddChild(box);
            _cards.Add((box, picture, count));
        }
        _tabsShown = _shown = "";
    }

    private static TextureRect Picture(int size) => new()
    {
        CustomMinimumSize = new Vector2(size, size),
        Size = new Vector2(size, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = Control.MouseFilterEnum.Ignore,
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
    };

    /// <summary>The wheel over the screen or its bezel scrolls the cards a row a notch.</summary>
    private void Scroll(InputEvent e, Control over)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel) return;
        _top = Catalogue.Scrolled(_top, wheel.ButtonIndex == MouseButton.WheelDown ? 1 : -1, _list.Count);
        over.AcceptEvent();
    }

    /// <summary>The screen at its dock, 12 px above the bottom edge, less how far the tablet has slid down.</summary>
    private void Dock()
    {
        _open.OffsetTop = -Slots.Margin - _at.Height + _drop;
        _open.OffsetBottom = -Slots.Margin + _drop;
        // The layers' marker beside the tablet's right bezel, its foot at the dock's.
        _focus.OffsetLeft = _at.Width - _at.Width / 2 + TabletLayout.ScreenLeft * TabletLayout.ArtScale + 8;
        _focus.OffsetRight = _focus.OffsetLeft + FocusWidth;
        _focus.OffsetBottom = -Slots.Margin + _drop;
        _focus.OffsetTop = _focus.OffsetBottom - FocusHeight;
    }


    /// <summary>The layers' pictures, top to bottom as they stand: objects, walls, back walls (0110).</summary>
    private static readonly Placement[] FocusLooks =
        [new("machine", "crate"), Placement.Wall("scrap"), Placement.BackWall("scrap")];

    /// <summary>The three layers stacked, no words: the focused one edged sky and whole, the others dim.</summary>
    private void DrawFocus()
    {
        var focus = (int)_hand.Focus.Get();
        for (int i = 0; i < FocusLooks.Length; i++)
        {
            bool lit = i == focus;
            var box = new Rect2(4, 4 + i * (FocusIcon + 4), FocusIcon, FocusIcon);
            _focus.DrawRect(box, (lit ? Sky950 : Render.Style.Slate950) with { A = 0.9f });
            _focus.DrawRect(box, lit ? Sky500 : Render.Style.Slate700, false, lit ? 2 : 1);
            _focus.DrawTextureRect(BlueprintArt.Thumbnail(FocusLooks[i]), box.Grow(-3), false, new Color(1, 1, 1, lit ? 1 : Tools.Faded));
        }
    }
    /// <summary>The tablet slid and its screen lit for this frame (<see cref="Tablet.Step"/>); its chip shows once it is gone.</summary>
    public override void _Process(double delta)
    {
        bool open = _hand.PlanOut.Get() || ToolMenu;
        if (!open && _search.HasFocus()) _search.ReleaseFocus();
        _open.Step(open, delta);
        _closed.Visible = !open && !_open.Shown;
        int drop = _open.Drop(GetViewport().GetVisibleRect().Size.Y - Slots.Margin - _at.Height);
        if (drop != _drop)
        {
            _drop = drop;
            Dock();
        }
    }

    /// <summary>A card's picture: what it builds, as the ship draws it.</summary>
    public static Texture2D ThumbOf(Tool tool) => tool switch
    {
        HandCard { Id: "valve" } => ShipTiles.PictureOf("valve_open"),
        HandCard card => ItemIcons.Icon(card.Item),
        _ => BlueprintArt.Thumbnail(((PlaceTool)tool).Placement),
    };

    /// <summary>Whether the menu shows a hand tool's cards (0110): the stripper or the wrench held, the plan away.</summary>
    private bool ToolMenu => !_hand.PlanOut.Get() && _hand.ToolCards.Count > 0;

    /// <summary>How many of <paramref name="item"/> this player carries, as last read.</summary>
    private int Carried(string item) => _ui.Carried.Where(c => c.Item == item).Sum(c => c.Count);

    /// <summary>A card clicked: picked, or let go when it is the one in hand. The menu stays open.</summary>
    public void Pick(Tool tool) => _hand.Pick(tool);

    /// <summary>For a scripted recording: <paramref name="tool"/>'s tooltip shown while the mouse is on no card; null for none.</summary>
    public void Hover(Tool? tool) => _pinned = tool;

    /// <summary>The tab shown, and its mode.</summary>
    public void ShowTab(string id) => _hand.Tab.Set(id);

    /// <summary>
    /// Esc, before it puts the plan away: a search being typed or shown is cleared, the menu staying open. Answers
    /// whether there was one.
    /// </summary>
    public bool DropSearch()
    {
        if (!_hand.PlanOut.Get() || _search.Text == "" && !_search.HasFocus()) return false;
        _search.Text = "";
        _search.ReleaseFocus();
        _top = 0;
        return true;
    }

    public override void _Input(InputEvent e)
    {
        // A press anywhere but the search field ends the typing; the press goes on to whatever it was for.
        if (e is InputEventMouseButton { Pressed: true } press && _search.HasFocus() && !_search.GetGlobalRect().HasPoint(press.Position))
            _search.ReleaseFocus();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        // The wheel over the world turns the open plan's layer (0110); over the tablet it scrolled the cards before it got here.
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel && _hand.PlanOut.Get())
        {
            _hand.TurnFocus(wheel.ButtonIndex == MouseButton.WheelDown ? 1 : -1);
            GetViewport().SetInputAsHandled();
            return;
        }
        // Esc puts the plan away through Shared.Escape, before it opens the game menu.
        if (e is not InputEventKey { Pressed: true } key || key.Echo) return;
        switch (key.PhysicalKeycode)
        {
            case Key.B:
                _hand.Toggle();
                break;
            // With a hand free Q drops the hotbar's item, taken before it gets here; with the menu open it picks.
            case Key.Q when _hand.PlanOut.Get():
                _hand.PickWanted = true;
                break;
            // / types in the search; while it is typed in, the field takes every key before it gets here, B and Q too.
            case Key.Slash when _hand.PlanOut.Get():
                _search.GrabFocus();
                break;
            default:
                return;
        }
        GetViewport().SetInputAsHandled();
    }

    /// <summary>Where the menu lies on the screen, open or as its closed chip: what the crew's markers keep out of.</summary>
    public Rect2 Covers => _open.Visible ? _open.Covers : _closed.GetGlobalRect();

    /// <summary>One frame: what the world knows; prices weigh what this player carries (<see cref="Ui.Carried"/>).</summary>
    public void Frame(Registry m)
    {
        bool open = _hand.PlanOut.Get() || ToolMenu;
        // The bezel's top at its dock, wherever the tablet is in its slide.
        _ui.MenuTop = open ? _open.GetGlobalRect().Position.Y - _drop - TabletLayout.BezelAbove : null;
        if (!open)
        {
            _tips.Show(null);
            _hovered = null;
            // Put away, the menu forgets its search: it opens on its tab.
            if (_search.Text != "") _search.Text = "";
            return;
        }
        bool toolMenu = ToolMenu;
        // A hand tool's menu is its cards alone, on one tab named for it, with no search (0110).
        var tabs = toolMenu
            ? [new Tab(_ui.Wielded.Get()!, [.. _hand.ToolCards])]
            : Tools.Tabs
                .Select(t => t with { Tools = t.Tools.Where(each => Tools.Known(each, m.Research) && Tools.Allowed(each, m.Rules)).ToList() })
                // A tab with nothing known is not shown.
                .Where(t => t.Tools.Count > 0)
                .ToList();
        var shownTab = toolMenu ? tabs[0] : tabs.FirstOrDefault(t => t.Id == _hand.Tab.Get()) ?? tabs.FirstOrDefault();
        // A tab the world shows nothing on is never the mode: the one shown instead is.
        if (!toolMenu && shownTab != null && shownTab.Id != _hand.Tab.Get()) _hand.Tab.Set(shownTab.Id);
        Tool? tool = toolMenu ? _hand.ToolCard.Get() : _hand.Selected.Get();
        _search.Visible = !toolMenu;
        if (toolMenu && _search.Text != "") _search.Text = "";
        _focus.Visible = !toolMenu;
        string query = _search.Text.Trim();
        _list = query == "" ? [.. shownTab?.Tools ?? []] : Tools.Matching(tabs.SelectMany(t => t.Tools), query);
        if (_reveal)
        {
            _reveal = false;
            // A card picked that the search does not name, by Q, ends the search: it shows on its own tab.
            if (tool != null && query != "" && !_list.Contains(tool))
            {
                _search.Text = query = "";
                _list = [.. shownTab?.Tools ?? []];
            }
            if (tool != null && _list.IndexOf(tool) is >= 0 and int at) _top = Catalogue.Revealing(_top, at);
        }
        _top = System.Math.Clamp(_top, 0, Catalogue.LastTop(_list.Count));
        FindHovered();
        string tabsKey = string.Join(",", tabs.Select(t => $"{t.Id}:{t.Tools.Count}"));
        if (tabsKey != _tabsShown)
        {
            _tabsShown = tabsKey;
            MakeTabs(tabs);
        }
        string key = $"{tabsKey}|{shownTab?.Id}|{query}|{tool?.Id}|{_top}|{string.Join(",", _list.Select(t => t.Id))}|{_hovered?.Id}|{_hoveredTab}|{_search.HasFocus()}|{string.Join(",", _list.OfType<HandCard>().Select(h => Carried(h.Item)))}";
        if (key != _shown)
        {
            _shown = key;
            Paint(shownTab, query, tool);
        }
        ShowTooltip(m);
    }

    /// <summary>The card and the tab under the mouse, by where it is; none behind the game menu.</summary>
    private void FindHovered()
    {
        _hovered = null;
        _hoveredTab = null;
        if (_ui.MenuOpen.Get()) return;
        var mouse = _open.GetGlobalMousePosition();
        if (_grid.GetGlobalRect().HasPoint(mouse))
        {
            var at = mouse - _grid.GlobalPosition;
            if (_at.CardAt(at.X, at.Y, _top, _list.Count) is int i) _hovered = _list[i];
        }
        foreach (var (box, id) in _tabBoxes)
            if (box.GetGlobalRect().HasPoint(mouse)) _hoveredTab = id;
        if (_hovered == null && _pinned != null && _list.Contains(_pinned)) _hovered = _pinned;
    }

    /// <summary>The tab strip: a tab a known tab, its name and how many cards it holds; the search field takes the rest of the row.</summary>
    private void MakeTabs(List<Tab> tabs)
    {
        foreach (var (box, _) in _tabBoxes) box.QueueFree();
        _tabBoxes.Clear();
        float x = 0;
        foreach (var tab in tabs)
        {
            string name = En.T($"build.tab.{tab.Id}"), count = tab.Tools.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var box = Tablet.Tab(name, count, x);
            string id = tab.Id;
            box.GuiInput += e =>
            {
                if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
                // A hand tool's one tab is its menu: nothing to switch to.
                if (ToolMenu) return;
                // A tab clicked shows its own cards: the search ends.
                _search.Text = "";
                _hand.Tab.Set(id);
                _shown = "";
                box.AcceptEvent();
            };
            _tabs.AddChild(box);
            _tabBoxes.Add((box, id));
            x += box.CustomMinimumSize.X + 4;
        }
        float left = Catalogue.Pad + x;
        _search.Position = new Vector2(left, Catalogue.Pad);
        _search.Size = new Vector2(_at.Width - Catalogue.Pad - left, Catalogue.TabHeight);
        _lens.Size = new Vector2(24, Catalogue.TabHeight);
        _slash.Position = new Vector2(_search.Size.X - 22, 4);
        _slash.Size = new Vector2(16, Catalogue.TabHeight - 8);
    }

    /// <summary>Everything painted as it now is: the tabs, the cards shown, the scrollbar and the line under them.</summary>
    private void Paint(Tab? shownTab, string query, Tool? tool)
    {
        foreach (var (box, id) in _tabBoxes)
        {
            // While a search shows, no tab is lit: the cards are every tab's.
            bool on = query == "" && id == shownTab?.Id;
            Tablet.PaintTab(box, on, id == _hoveredTab);
        }
        _slash.Visible = _search.Text == "" && !_search.HasFocus();
        for (int slot = 0; slot < _cards.Count; slot++)
        {
            var (box, picture, count) = _cards[slot];
            int index = _top * Catalogue.Columns + slot;
            box.Visible = index < _list.Count;
            if (!box.Visible) continue;
            var card = _list[index];
            var (x, y) = _at.SpotOf(_top, index);
            box.Position = new Vector2(x, y);
            box.AddThemeStyleboxOverride("panel", card == tool ? Held : SlotLooks.Of(SlotTint.Plain, card == _hovered, chosen: false));
            picture.Texture = ThumbOf(card);
            count.Visible = card is HandCard;
            if (card is HandCard h) count.Text = Carried(h.Item).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        _none.Visible = query != "" && _list.Count == 0;
        Tablet.PlaceThumb(_bar.Thumb, _at.Thumb(_top, _list.Count));
        _heldPicture.Visible = _heldName.Visible = tool != null;
        if (tool != null)
        {
            _heldPicture.Texture = ThumbOf(tool);
            _heldName.Text = tool.Label;
        }
        _hint.Text = shownTab != null ? En.T(Tools.HintOf(tool)) : "";
    }

    /// <summary>The card in hand: the selected slot's look, sky edged twice as thick.</summary>
    private static readonly StyleBoxFlat Held = HeldBox();

    private static StyleBoxFlat HeldBox()
    {
        var box = Render.Style.Box(Sky950, Sky500, radius: 3);
        box.SetBorderWidthAll(2);
        return box;
    }

    /// <summary>The one tooltip over the card the pointer is on: its name, each item of its cost carried against needed, and what it does.</summary>
    private void ShowTooltip(Registry m)
    {
        if (_hovered is not { } tool)
        {
            _tips.Show(null);
            return;
        }
        var lines = CostLine.Lacking(Tools.CostOf(tool, m.Building.CostFor), [], _ui.Carried)
            .Select(c => new TipLine($"{c.Count} {En.T($"item.{c.Item}")}", c.Tone, c.Item))
            .Append(new TipLine(En.T($"tool.{tool.Id}.about"), Tone.Dim));
        _tips.Show(new Tip(tool.Label, lines.ToList()));
    }
}
