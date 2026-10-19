using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;

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
/// away; with it away, the key that takes it out. Q picks the card under the pointer; R turns the shape a hull wall's
/// card plans walls in, Shift+R back, every wall card's picture showing it (0069). Only what the world knows is
/// shown (0024): a tab with nothing known, not at all, nor its mode. It is the screen of a rugged tablet (0073), which
/// slides up from the bottom edge as the plan comes out and back down as it goes away.
/// </summary>
public partial class BuildMenu : CanvasLayer
{
    private static readonly Color Sky500 = new("0ea5e9"), Sky950 = new("082f49");
    private static readonly Font Mono = Render.Style.Mono;
    private const int FontSize = 12;

    /// <summary>
    /// The tablet's art (tablet_frame.png, a 9-slice, the plate and the stylus over it), a pixel of it two of the screen:
    /// the screen's top-left in it, and how much wider and taller it is than the screen, in its pixels.
    /// </summary>
    private const int ArtScale = 2, ScreenLeft = 9, ScreenTop = 16, ArtAround = 26;

    /// <summary>Above the screen, the bezel's top edge, in screen pixels: what the prompt for E keeps above.</summary>
    private const int BezelAbove = 8 * ArtScale;

    /// <summary>The colour the screen lights what is round it with, and how strongly at the middle (the concept's).</summary>
    private static readonly Color ScreenLight = new(70 / 255f, 150 / 255f, 230 / 255f, 0.2f), LedLight = new(74 / 255f, 222 / 255f, 128 / 255f, 0.55f);

    private readonly TabletMotion _motion = new();
    private readonly NinePatchRect _frame = new()
    {
        Texture = GD.Load<Texture2D>("res://art/tablet_frame.png"),
        PatchMarginLeft = 74,
        PatchMarginRight = 77,
        PatchMarginTop = 21,
        PatchMarginBottom = 14,
        AxisStretchHorizontal = NinePatchRect.AxisStretchMode.Tile,
        AxisStretchVertical = NinePatchRect.AxisStretchMode.Tile,
        DrawCenter = false,
        Scale = new Vector2(ArtScale, ArtScale),
        ShowBehindParent = true,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = Control.MouseFilterEnum.Stop,
    };
    private readonly TextureRect _plate = Art("res://art/tablet_plate.png"), _stylus = Art("res://art/tablet_stylus.png");
    /// <summary>The status lamp dark, over the art's lit one, while the screen is off; its glow while it is on.</summary>
    private readonly ColorRect _ledOff = new() { Color = new Color("1f3b2a"), Size = new Vector2(2, 2), MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly TextureRect _ledGlow = Glow(LedLight, 0), _wash = Glow(ScreenLight, 40f / 560);
    /// <summary>The glass over the screen: the recess's shadow and a faint glare.</summary>
    private readonly Control _glass = new() { MouseFilter = Control.MouseFilterEnum.Ignore, ClipContents = true };
    /// <summary>How far below its dock the tablet is drawn now, in screen pixels.</summary>
    private int _drop;
    /// <summary>What the screen shows, faded in as it comes on.</summary>
    private readonly Control[] _screen;

    private readonly PlanHand _hand;
    private readonly Ui _ui;
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly PanelContainer _closed;
    private readonly Panel _open = new() { MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly Control _tabs = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly LineEdit _search = new() { ContextMenuEnabled = false, CaretBlink = true, MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly Control _lens = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly PanelContainer _slash;
    private readonly Control _grid = new() { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Pass };
    private readonly Label _none = Text(En.T("build.none"), Render.Style.Slate500);
    private readonly Panel _track = new() { MouseFilter = Control.MouseFilterEnum.Ignore }, _thumb = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly HBoxContainer _foot = new() { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly TextureRect _heldPicture = Picture(20);
    private readonly Label _heldName = Text("", Render.Style.Slate100), _hint = Text("", Render.Style.Slate500, 11);
    private readonly List<(Panel Box, TextureRect Picture)> _cards = [];
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
        _closed.AddChild(Text(En.T("build.open"), Render.Style.Slate400));
        _closed.AnchorLeft = _closed.AnchorRight = 0.5f;
        _closed.AnchorTop = _closed.AnchorBottom = 1;
        _closed.GrowHorizontal = Control.GrowDirection.Both;
        _closed.GrowVertical = Control.GrowDirection.Begin;
        _closed.OffsetBottom = -Slots.Margin;
        _root.AddChild(_closed);

        // The tablet's screen: square, edged by the bezel's recess rather than a border of its own.
        _open.AddThemeStyleboxOverride("panel", Render.Style.Box(Render.Style.Slate900, Render.Style.Slate900, radius: 0));
        _open.AnchorLeft = _open.AnchorRight = 0.5f;
        _open.AnchorTop = _open.AnchorBottom = 1;
        // The wheel anywhere over the tablet scrolls the cards; it never reaches the hotbar or the camera.
        _open.GuiInput += e => Scroll(e, _open);
        _frame.GuiInput += e => Scroll(e, _frame);
        // Behind the screen: its light on what is round it, then the tablet; over the bezel, the lamp's glow.
        _wash.ShowBehindParent = true;
        _open.Visible = false;
        _open.AddChild(_wash);
        _open.AddChild(_frame);
        foreach (var part in (Control[])[_plate, _stylus, _ledOff]) _frame.AddChild(part);
        _open.AddChild(_ledGlow);
        _glass.Draw += DrawGlass;
        _grid.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } press) return;
            if (_at.CardAt(press.Position.X, press.Position.Y, _top, _list.Count) is int i) Pick(_list[i]);
            _grid.AcceptEvent();
        };
        _root.AddChild(_open);

        SearchField();
        _slash = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var keyBox = Render.Style.Box(Colors.Transparent, Render.Style.Slate700, radius: 3);
        (keyBox.ContentMarginLeft, keyBox.ContentMarginRight) = (4, 4);
        _slash.AddThemeStyleboxOverride("panel", keyBox);
        _slash.AddChild(Text("/", Render.Style.Slate500, 11));
        _lens.Draw += () =>
        {
            _lens.DrawArc(new Vector2(12, 11), 4, 0, Mathf.Tau, 16, Render.Style.Slate500, 1.5f, true);
            _lens.DrawLine(new Vector2(15, 14), new Vector2(18, 17), Render.Style.Slate500, 1.5f, true);
        };
        _search.AddChild(_lens);
        _search.AddChild(_slash);

        _none.Position = new Vector2(4, 10);
        _grid.AddChild(_none);
        _track.AddThemeStyleboxOverride("panel", Render.Style.Box(Render.Style.Slate800, Render.Style.Slate800, radius: 3));
        _thumb.AddThemeStyleboxOverride("panel", Render.Style.Box(Render.Style.Slate600, Render.Style.Slate600, radius: 3));
        _track.AddChild(_thumb);
        _foot.AddThemeConstantOverride("separation", 6);
        _hint.ClipText = true;
        _hint.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _hint.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _foot.AddChild(_heldPicture);
        _foot.AddChild(_heldName);
        _foot.AddChild(_hint);
        _screen = [_tabs, _search, _grid, _track, _foot];
        foreach (var part in _screen) _open.AddChild(part);
        _open.AddChild(_glass);

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
        // The tablet round the screen, in its art's pixels; its stretches tile, so the large slot's frame is as crisp.
        var art = new Vector2(_at.Width / ArtScale + ArtAround, _at.Height / ArtScale + ArtAround);
        _frame.Position = new Vector2(-ScreenLeft * ArtScale, -ScreenTop * ArtScale);
        _frame.Size = art;
        _plate.Position = new Vector2(2 + ((int)art.X - 12 - 33) / 2, 9);
        _stylus.Position = new Vector2(art.X - 13, 0);
        _ledOff.Position = new Vector2(art.X - 43, 11);
        _ledGlow.Size = new Vector2(18, 18);
        _ledGlow.Position = new Vector2(_at.Width - 59, -17);
        _wash.Size = new Vector2(1120, 470);
        _wash.Position = new Vector2(_at.Width / 2, _at.Height / 2) - _wash.Size / 2;
        _glass.Size = new Vector2(_at.Width, _at.Height);
        _glass.QueueRedraw();
        _tabs.Position = new Vector2(Catalogue.Pad, Catalogue.Pad);
        _grid.Position = new Vector2(Catalogue.Pad, _at.GridTop);
        _grid.Size = new Vector2(_at.GridWidth, _at.GridHeight);
        _track.Position = new Vector2(_at.BarLeft, _at.GridTop);
        _track.Size = new Vector2(Catalogue.BarWidth, _at.GridHeight);
        _foot.Position = new Vector2(Catalogue.Pad, _at.FootTop);
        _foot.Size = new Vector2(_at.Width - 2 * Catalogue.Pad, Catalogue.FootHeight);
        foreach (var (box, _) in _cards) box.QueueFree();
        _cards.Clear();
        int icon = Slots.IconOf(_ui.SlotScale.Get());
        for (int i = 0; i < Catalogue.Columns * Catalogue.Rows; i++)
        {
            var box = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore, Size = new Vector2(_at.Slot, _at.Slot) };
            var picture = Picture(icon);
            picture.Position = new Vector2((_at.Slot - icon) / 2, (_at.Slot - icon) / 2);
            box.AddChild(picture);
            _grid.AddChild(box);
            _cards.Add((box, picture));
        }
        _tabsShown = _shown = "";
    }

    private static Label Text(string text, Color colour, int size = FontSize) => new()
    {
        Text = text,
        LabelSettings = new LabelSettings { Font = Mono, FontSize = size, FontColor = colour },
        MouseFilter = Control.MouseFilterEnum.Ignore,
        VerticalAlignment = VerticalAlignment.Center,
    };

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

    private static TextureRect Art(string path) => new()
    {
        Texture = GD.Load<Texture2D>(path),
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    /// <summary>A soft round light of <paramref name="colour"/>, its alpha whole out to <paramref name="core"/> of its radius, added to what is under it.</summary>
    private static TextureRect Glow(Color colour, float core)
    {
        var gradient = new Gradient
        {
            Offsets = [0, core, 0.45f, 1],
            Colors = [colour, colour, colour with { A = colour.A * 0.45f }, colour with { A = 0 }],
        };
        return new TextureRect
        {
            Texture = new GradientTexture2D
            {
                Gradient = gradient,
                Fill = GradientTexture2D.FillEnum.Radial,
                FillFrom = new Vector2(0.5f, 0.5f),
                FillTo = new Vector2(1, 0.5f),
                Width = 128,
                Height = 128,
            },
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }

    /// <summary>The recess's shadow on the screen's top and left edges, and the glass's glare: faint, over whatever it shows.</summary>
    private void DrawGlass()
    {
        float w = _glass.Size.X, h = _glass.Size.Y;
        for (int y = 0; y < 6; y++) _glass.DrawRect(new Rect2(0, y, w, 1), Colors.Black with { A = 0.55f * (1 - (y + 0.5f) / 6) });
        for (int x = 0; x < 4; x++) _glass.DrawRect(new Rect2(x, 6, 1, h - 6), Colors.Black with { A = 0.4f * (1 - (x + 0.5f) / 4) });
        var glare = new Color(200 / 255f, 225 / 255f, 1);
        _glass.DrawColoredPolygon([new(70, 0), new(190, 0), new(60, h), new(-60, h)], glare with { A = 0.035f });
        _glass.DrawColoredPolygon([new(206, 0), new(222, 0), new(92, h), new(76, h)], glare with { A = 0.03f });
    }

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
    }

    /// <summary>
    /// The tablet slid and its screen lit for this frame (<see cref="TabletMotion"/>). Put away, it takes no more input
    /// at once, though it shows while it slides down; its chip shows once it is gone.
    /// </summary>
    public override void _Process(double delta)
    {
        bool open = _hand.PlanOut.Get();
        if (!open && _search.HasFocus()) _search.ReleaseFocus();
        _motion.Step(open, delta);
        _open.Visible = _motion.Shown;
        _closed.Visible = !open && !_motion.Shown;
        var mouse = open ? Control.MouseBehaviorRecursiveEnum.Inherited : Control.MouseBehaviorRecursiveEnum.Disabled;
        if (_open.MouseBehaviorRecursive != mouse) _open.MouseBehaviorRecursive = mouse;
        // Below the edge it hides the stylus's tip too, and a few pixels more.
        int drop = (int)Mathf.Round((1 - _motion.Up) * (Slots.Margin + _at.Height + ScreenTop * ArtScale + 4));
        if (drop != _drop)
        {
            _drop = drop;
            Dock();
        }
        float power = (float)_motion.Power;
        foreach (var part in _screen) part.Modulate = Colors.White with { A = power };
        _ledOff.Modulate = Colors.White with { A = 1 - power };
        _ledGlow.Modulate = Colors.White with { A = power };
        _wash.Modulate = Colors.White with { A = power * (float)_motion.Up };
    }

    /// <summary>A card's picture: what it builds, as the ship draws it; the cable its plug.</summary>
    public static Texture2D ThumbOf(Tool tool) => tool is PlaceTool p ? BlueprintArt.Thumbnail(p.Placement) : AsciiArt.Plug();

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
        // Esc puts the plan away through Shared.Escape, before it opens the game menu.
        if (e is not InputEventKey { Pressed: true } key || key.Echo) return;
        switch (key.PhysicalKeycode)
        {
            case Key.B:
                _hand.Toggle();
                break;
            // With a hand free Q drops the hotbar's item, taken before it gets here; with the menu open it picks.
            // With a hull wall's card in hand, R turns the shape walls are planned in, Shift+R back (0069).
            case Key.R when _hand.PlanOut.Get() && _hand.Turn(key.ShiftPressed ? -1 : 1):
                break;
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
    public Rect2 Covers => _open.Visible ? _frame.GetGlobalRect() : _closed.GetGlobalRect();

    /// <summary>One frame: what the world knows; prices weigh what this player carries (<see cref="Ui.Carried"/>).</summary>
    public void Frame(Registry m)
    {
        bool open = _hand.PlanOut.Get();
        // The bezel's top at its dock, wherever the tablet is in its slide.
        _ui.MenuTop = open ? _open.GetGlobalRect().Position.Y - _drop - BezelAbove : null;
        if (!open)
        {
            _tips.Show(null);
            _hovered = null;
            // Put away, the menu forgets its search: it opens on its tab.
            if (_search.Text != "") _search.Text = "";
            return;
        }
        var tabs = Tools.Tabs
            .Select(t => t with { Tools = t.Tools.Where(each => Tools.Known(each, m.Research)).ToList() })
            .Where(t => t.Tools.Count > 0)
            .ToList();
        var shownTab = tabs.FirstOrDefault(t => t.Id == _hand.Tab.Get()) ?? tabs.FirstOrDefault();
        // A tab the world shows nothing on is never the mode: the one shown instead is.
        if (shownTab != null && shownTab.Id != _hand.Tab.Get()) _hand.Tab.Set(shownTab.Id);
        var tool = _hand.Selected.Get();
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
        string key = $"{tabsKey}|{shownTab?.Id}|{query}|{tool?.Id}|{_hand.Shape.Get()}|{_top}|{string.Join(",", _list.Select(t => t.Id))}|{_hovered?.Id}|{_hoveredTab}|{_search.HasFocus()}";
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
            var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 6);
            row.AddChild(Text(name, Render.Style.Slate300));
            row.AddChild(Text(count, Render.Style.Slate500));
            var box = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Pass, Position = new Vector2(x, 0) };
            box.AddChild(row);
            float width = 6 + Mono.GetStringSize(name, fontSize: FontSize).X + 6 + Mono.GetStringSize(count, fontSize: FontSize).X + 6;
            box.CustomMinimumSize = new Vector2(Mathf.Ceil(width), Catalogue.TabHeight);
            string id = tab.Id;
            box.GuiInput += e =>
            {
                if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
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
            var style = Render.Style.Box(on ? Render.Style.Sky700 : id == _hoveredTab ? Render.Style.Slate700 : Render.Style.Slate800,
                on ? Render.Style.Sky700 : Render.Style.Slate800);
            (style.ContentMarginLeft, style.ContentMarginRight) = (6, 6);
            box.AddThemeStyleboxOverride("panel", style);
            var labels = box.GetChild(0).GetChildren().OfType<Label>().ToList();
            labels[0].LabelSettings.FontColor = on ? Colors.White : Render.Style.Slate300;
            labels[1].LabelSettings.FontColor = on ? Render.Style.Sky200 : Render.Style.Slate500;
        }
        _slash.Visible = _search.Text == "" && !_search.HasFocus();
        int shape = _hand.Shape.Get();
        for (int slot = 0; slot < _cards.Count; slot++)
        {
            var (box, picture) = _cards[slot];
            int index = _top * Catalogue.Columns + slot;
            box.Visible = index < _list.Count;
            if (!box.Visible) continue;
            var card = _list[index];
            var (x, y) = _at.SpotOf(_top, index);
            box.Position = new Vector2(x, y);
            box.AddThemeStyleboxOverride("panel", card == tool ? Held : SlotLooks.Of(SlotTint.Plain, card == _hovered, chosen: false));
            // A wall's card shows the shape walls are planned in.
            picture.Texture = ThumbOf(Tools.Shaped(card, shape)!);
        }
        _none.Visible = query != "" && _list.Count == 0;
        var thumb = _at.Thumb(_top, _list.Count);
        _thumb.Visible = thumb != null;
        if (thumb is { } bar)
        {
            _thumb.Position = new Vector2(0, Mathf.Round(bar.Top));
            _thumb.Size = new Vector2(Catalogue.BarWidth, Mathf.Round(bar.Length));
        }
        _heldPicture.Visible = _heldName.Visible = tool != null;
        if (tool != null)
        {
            _heldPicture.Texture = ThumbOf(Tools.Shaped(tool, shape)!);
            _heldName.Text = tool.Label;
        }
        _hint.Text = shownTab != null ? En.T(Tools.HintOf(shownTab.Id)) : "";
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
