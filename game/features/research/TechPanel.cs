using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The tech tree on the tablet (2026-10-30-tech-rings), nearly the whole screen: T takes it out anywhere and puts it away; Esc, Tab, E
/// and B put it away too. A tab a map of rings round a hub (<see cref="TechRings"/>), dragged and zoomed (<see cref="TechCamera"/>): the
/// hub asks for what opens the next ring, the rings filled are lit, a branch a way out. A click picks a node and buys nothing: what it
/// takes, where it is made and what it goes into light up (<see cref="TechLinks"/>), known or not, and the strip below says so and buys.
/// A press counts as a click only if the pointer has not moved. Words are set at the size they are seen, never scaled, so they stay sharp;
/// from far nodes are plain dots under their branches' names. The world runs on behind it.
/// </summary>
public partial class TechPanel : CanvasLayer
{
    private static readonly Color Line = new("1a1e25"), Socket = new("474c57"), Dim = new("2a2e36"), Bought = new("6fe08a"), BoughtBack = new("0b1f16"),
        Faint = new("7a8090"), Warm = new("f0c050"), Far = new("39404c"), LitFill = new("16261f"), GateEdge = new("b08fe0"), GateBack = new("2c2437"),
        Picked = new("ffffff"), From = new("5eead4"), MadeAt = new("f28fd0"), UsedIn = new("7dd3fc");

    /// <summary>Seconds between reads of the world: a panel reads on a timer, never every frame.</summary>
    private const double ReadEvery = 0.25;
    /// <summary>The gap kept between the tablet's art and the window's edge, and the height of the strip about what is picked.</summary>
    private const int Margin = 12, DetailTall = 116;
    /// <summary>Pixels the pointer may move in a press still meant as a click.</summary>
    private const float ClickSlack = 4;
    /// <summary>Under this zoom words would be too small to read and pictures a smudge: nodes turn plain dots.</summary>
    private const double WordsFrom = 0.65;
    /// <summary>How fast a ring lights as it opens, of what is left a second.</summary>
    private const double LitSpeed = 3;
    /// <summary>A node's sizes on the map at zoom 1; how small a dot gets; words' sizes at zoom 1; how far from its way's edge a branch's name stands.</summary>
    private const int NodeSide = 68, Icon = 40, RootWide = 112, RootTall = 106, LeastDot = 12, NameSize = 12, HubSize = 13, TitleOut = 48;
    private const int Big = 64, Entry = 24, UsedLarge = 32, UsedSmall = 24, UsedManyFrom = 20, ButtonWide = 200;
    private const int Roundest = 4096;

    /// <summary>What a node on the map is made of, kept so a read only restyles it: a rebuild under the pointer would eat a click.</summary>
    private sealed record NodeView(TechNode Node, Control Root, Panel Box, TextureRect Picture, Label Name, CreditLine Price, Label Done);

    /// <summary>A tab's map and all that is drawn on it: what scales is under <see cref="Map"/>, what is set at its seen size under <see cref="Front"/>.</summary>
    private sealed class TabView(TechTab tab)
    {
        public readonly TechTab Tab = tab;
        public readonly TechRings Rings = new(tab);
        public readonly TechCamera Camera = new();
        public readonly Control Map = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        public readonly Control Front = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        public readonly Control Back = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        public readonly Panel Hub = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        public readonly CenterContainer HubWords = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        public readonly Label HubTitle = Text("", Style.Slate100, HubSize);
        public readonly VBoxContainer HubRows = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 2);
        public readonly List<NodeView> Nodes = [];
        public readonly List<(Label Title, Vector2 Way)> Titles = [];
        public bool Centred;
        public double Lit = TechRings.HubRadius, SetAt;
        public TechNode? Gate;
        public bool Near => Camera.Zoom >= WordsFrom;
    }

    private readonly Ui _ui;
    private readonly Func<bool> _inWorld;
    private readonly Func<Registry?> _world;
    private readonly Func<int> _peer;
    private readonly Action<Command> _send;
    private readonly Tablet _tablet = new();
    private readonly Control _strip = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly HBoxContainer _head = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, GrowHorizontal = Control.GrowDirection.Begin, Alignment = BoxContainer.AlignmentMode.End }, 16);
    private readonly CreditLine _credits = new(Tablet.FontSize + 1);
    private readonly Label _progress = Text("", Style.Slate100);
    private readonly HBoxContainer _foot = Style.Gap(new HBoxContainer { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore }, 8);
    private readonly Control _view = new() { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly Panel _detail = new() { MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly HBoxContainer _facts = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, ClipContents = true }, 24);
    private readonly Button _action = new() { FocusMode = Control.FocusModeEnum.None };
    private readonly HBoxContainer _actionLine = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center }, 8);
    private readonly CreditLine _actionPrice = new(Tablet.FontSize + 3);
    private readonly TipSpots _tips;
    private readonly List<TabView> _tabs;
    private readonly List<PanelContainer> _tabBoxes = [];
    private readonly Dictionary<string, Placement?> _placements = new(StringComparer.Ordinal);
    private IResearchQueries? _research;
    private Func<string, int> _carried = _ => 0;
    private TabView _tab;
    private TechNode? _picked;
    private string? _shown;
    private double _sinceRead = ReadEvery;
    private bool _pressed;
    private float _moved;
    private int _hoveredTab = -1;

    /// <param name="ui">Whether it is out, the hand freed as it comes, the menu, under which it takes no key.</param>
    /// <param name="inWorld">Whether a world is played: on the title T does nothing.</param>
    /// <param name="world">The world the hero stands in; null with none played.</param>
    /// <param name="peer">This player's peer: whose bag a deposit is taken from.</param>
    /// <param name="send">Sends a command to the session.</param>
    public TechPanel(Ui ui, Func<bool> inWorld, Func<Registry?> world, Func<int> peer, Action<Command> send)
    {
        (_ui, _inWorld, _world, _peer, _send) = (ui, inWorld, world, peer, send);
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.Handbook;
        _tabs = Content.Research.Tabs.Select(t => new TabView(t)).ToList();
        _tab = _tabs[0];
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, TextureFilter = CanvasItem.TextureFilterEnum.Linear, Theme = new Theme { DefaultFont = Style.Clear, DefaultFontSize = Tablet.FontSize + 1 } };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        root.AddChild(_tablet);
        float x = 0;
        for (int i = 0; i < _tabs.Count; i++)
        {
            int index = i;
            var box = Tablet.Tab(T($"research.tab.{_tabs[i].Tab.Id}"), null, x);
            foreach (var label in box.FindChildren("*", nameof(Label), true, false).OfType<Label>()) label.LabelSettings.Font = Style.Clear;
            box.GuiInput += e =>
            {
                if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
                Choose(_tabs[index]);
                box.AcceptEvent();
            };
            box.MouseEntered += () => HoverTab(index);
            box.MouseExited += () => HoverTab(-1);
            _strip.AddChild(box);
            _tabBoxes.Add(box);
            x += box.CustomMinimumSize.X + 4;
        }
        _head.AddChild(_progress);
        _head.AddChild(_credits);
        _strip.AddChild(_head);
        _foot.AddChild(Text(T("research.title"), Style.Slate100));
        var hint = Text(T("research.hint"), Style.Slate500, Tablet.HintSize + 1);
        hint.ClipText = true;
        hint.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        hint.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _foot.AddChild(hint);
        foreach (var tab in _tabs)
        {
            _view.AddChild(tab.Map);
            _view.AddChild(tab.Front);
        }
        _view.GuiInput += Handle;
        var look = new StyleBoxFlat { BgColor = new Color("0d1218"), BorderColor = Line };
        look.SetBorderWidthAll(1);
        look.SetCornerRadiusAll(2);
        _detail.AddThemeStyleboxOverride("panel", look);
        _detail.AddChild(_facts);
        _action.Pressed += Act;
        _action.ClipText = true;
        _action.AddThemeFontOverride("font", Style.Clear);
        _action.AddThemeFontSizeOverride("font_size", Tablet.FontSize + 1);
        PaintAction();
        _detail.AddChild(_action);
        _actionLine.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _actionLine.AddChild(Text(T("research.buy"), Style.Slate100, Tablet.FontSize + 3));
        _actionLine.AddChild(_actionPrice);
        _action.AddChild(_actionLine);
        // The wheel over the bezel zooms as over the map; it never reaches the hotbar.
        _tablet.GuiInput += e => Zoom(e, _tablet);
        _tablet.Bezel.GuiInput += e => Zoom(e, _tablet.Bezel);
        foreach (var part in (Control[])[_strip, _view, _detail, _foot]) _tablet.OnScreen(part);
        PaintTabs();
        Show(_tab, true);
    }

    /// <summary>Whether it is out: what Esc puts away before it opens the menu.</summary>
    public bool Shown => _ui.TreeOpen.Get();

    /// <summary>Takes it out, the hand freed. What it puts away is <c>Main</c>'s to say, as it listens to <see cref="Ui.TreeOpen"/>.</summary>
    public void Open()
    {
        if (Shown) return;
        _ui.Hand.Set(Holding.Free);
        _shown = null;
        _sinceRead = ReadEvery;
        _ui.TreeOpen.Set(true);
    }

    /// <summary>Puts it away: T again, Esc through the menu's <c>Escape</c>, Tab, E or B.</summary>
    public void Close()
    {
        _ui.TreeOpen.Set(false);
        _pressed = false;
    }

    /// <summary>Where a node's middle is on the screen: what a recording points at.</summary>
    public Vector2 ScreenOf(string id) => _view.GlobalPosition + _tab.Nodes.First(v => v.Node.Id == id).Root.Position;

    /// <summary>Where the button buying what is picked is on the screen: what a recording points at.</summary>
    public Vector2 ActionAt => _action.GetGlobalRect().GetCenter();

    /// <summary>Where the hub's middle is on the screen: what a recording points at.</summary>
    public Vector2 HubAt => _tab.Hub.GetGlobalRect().GetCenter();

    /// <summary>Where tab <paramref name="index"/> is on the screen: what a recording points at.</summary>
    public Vector2 TabAt(int index) => _tabBoxes[index].GetGlobalRect().GetCenter();

    public override void _Process(double delta)
    {
        bool open = Shown && _world() != null;
        _tablet.Step(open, delta);
        if (!_tablet.Shown) return;
        LayOut();
        Place(delta);
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
        _foot.Position = new Vector2(pad, height - pad - TabletLayout.FootHeight);
        _foot.Size = new Vector2(width - 2 * pad, TabletLayout.FootHeight);
        _detail.Position = new Vector2(pad, _foot.Position.Y - pad - DetailTall);
        _detail.Size = new Vector2(width - 2 * pad, DetailTall);
        _facts.Position = new Vector2(pad, pad);
        _facts.Size = new Vector2(_detail.Size.X - ButtonWide - 3 * pad, DetailTall - 2 * pad);
        _action.Position = new Vector2(_detail.Size.X - ButtonWide - pad, pad);
        _action.Size = new Vector2(ButtonWide, DetailTall - 2 * pad);
        _view.Position = new Vector2(pad, bodyTop);
        _view.Size = new Vector2(width - 2 * pad, _detail.Position.Y - pad - bodyTop);
    }

    /// <summary>The rings where the camera has them, growing lit to what is open; the nodes and words set over them at the size they are seen.</summary>
    private void Place(double delta)
    {
        if (_tab.Nodes.Count == 0) return;
        var camera = _tab.Camera;
        camera.Fit(_view.Size.X, _view.Size.Y, _tab.Rings.Side, _tab.Rings.Side);
        if (!_tab.Centred)
        {
            _tab.Centred = true;
            camera.Centre(_tab.Rings.Middle, _tab.Rings.Middle);
        }
        float zoom = (float)camera.Zoom;
        var origin = new Vector2((float)camera.X, (float)camera.Y).Round();
        _tab.Map.Scale = new Vector2(zoom, zoom);
        _tab.Map.Position = origin;
        if (_tab.SetAt != camera.Zoom) SetSizes(_tab, zoom);
        foreach (var view in _tab.Nodes)
        {
            var spot = _tab.Rings.Spots[view.Node.Id];
            view.Root.Position = (origin + new Vector2((float)spot.X, (float)spot.Y) * zoom).Round();
        }
        var middle = origin + new Vector2((float)_tab.Rings.Middle, (float)_tab.Rings.Middle) * zoom;
        float hub = (float)TechRings.HubRadius * zoom;
        _tab.Hub.Position = middle - new Vector2(hub, hub);
        float reach = _tab.Near ? hub + TitleOut : (float)_tab.Rings.Edges[^1] * zoom - TitleOut / 2f;
        foreach (var (title, way) in _tab.Titles) title.Position = (middle + way * reach - title.GetMinimumSize() / 2).Round();
        double lit = OpenEdge(_tab);
        if (Math.Abs(lit - _tab.Lit) <= 0.5) return;
        _tab.Lit += (lit - _tab.Lit) * Math.Min(1, LitSpeed * delta);
        _tab.Back.QueueRedraw();
    }

    /// <summary>Sets every node and word at the size <paramref name="zoom"/> shows them, and has the next read restyle them for it.</summary>
    private void SetSizes(TabView tab, float zoom)
    {
        tab.SetAt = tab.Camera.Zoom;
        _shown = null;
        _sinceRead = ReadEvery;
        bool near = tab.Near;
        float side = near ? NodeSide * zoom : Math.Max(LeastDot, NodeSide * zoom);
        int size = Math.Max(8, Mathf.RoundToInt(NameSize * zoom));
        foreach (var view in tab.Nodes)
        {
            view.Box.Position = new Vector2(-side / 2, -side / 2);
            view.Box.Size = new Vector2(side, side);
            float icon = Icon * zoom;
            view.Picture.Position = new Vector2(-icon / 2, -icon / 2);
            view.Picture.Size = new Vector2(icon, icon);
            view.Picture.TextureFilter = zoom < 1 ? CanvasItem.TextureFilterEnum.Linear : CanvasItem.TextureFilterEnum.Nearest;
            float wide = RootWide * zoom;
            view.Name.LabelSettings.FontSize = size;
            view.Name.Position = new Vector2(-wide / 2, side / 2 + 2 * zoom);
            view.Name.Size = new Vector2(wide, size + 4);
            view.Price.Resize(size);
            view.Price.CustomMinimumSize = new Vector2(wide, 0);
            // A container never shrinks by itself: left at its old size it would centre the price in that, off to the right and down.
            view.Price.Size = view.Price.GetCombinedMinimumSize();
            view.Price.Position = new Vector2(-wide / 2, side / 2 + 2 * zoom + size + 3);
            view.Done.LabelSettings.FontSize = size + 2;
            view.Done.Position = view.Price.Position;
            view.Done.Size = new Vector2(wide, size + 4);
        }
        float hub = (float)TechRings.HubRadius * zoom;
        tab.Hub.Size = new Vector2(2 * hub, 2 * hub);
        tab.HubWords.Size = tab.Hub.Size;
        tab.HubWords.Visible = near;
        tab.HubTitle.LabelSettings.FontSize = Math.Max(8, Mathf.RoundToInt(HubSize * zoom));
        foreach (var (title, _) in tab.Titles)
        {
            title.LabelSettings.FontSize = near ? 12 : 14;
            title.LabelSettings.FontColor = near ? Style.Slate500 : Style.Slate100;
        }
    }

    /// <summary>How far out the tab's open rings reach; the hub's edge with none open.</summary>
    private double OpenEdge(TabView tab)
    {
        if (_research is not { } research) return TechRings.HubRadius;
        int open = -1;
        for (int ring = 0; ring < tab.Tab.Rings.Length; ring++)
        {
            if (tab.Tab.Rings[ring].Gate is { } gate && !research.Done(Content.Research.Node(gate)!)) break;
            open = ring;
        }
        return open < 0 ? TechRings.HubRadius : tab.Rings.Edges[open];
    }

    /// <summary>Restyles what has changed: a block's state, a gate's deposit, what the bag holds. Credits only retype their line.</summary>
    private void Read(Registry m)
    {
        var research = m.Research;
        _research = research;
        if (_tab.Nodes.Count == 0) Build();
        _credits.Set(research.Credits);
        var all = Content.Research.Nodes;
        _progress.Text = T("research.progress", ("done", all.Count(research.Done)), ("all", all.Count));
        var totals = m.Items.Totals(_peer());
        _carried = item => totals.Where(t => t.Item == item).Sum(t => t.Count);
        string now = (_picked?.Id ?? "") + '|' + string.Join('|', all.Select(n => n.Gate
            ? (research.Done(n) ? "1" : research.Reached(n) ? "0" : "x") + string.Join(',', n.Deposit.Select(d => $"{research.Deposited(n, d.Item)}/{_carried(d.Item)}"))
            : research.BuyProblem(n) ?? ""));
        if (now == _shown) return;
        _shown = now;
        foreach (var tab in _tabs)
        {
            foreach (var view in tab.Nodes) Restyle(tab, view, research);
            RestyleHub(tab, research);
            tab.Back.QueueRedraw();
        }
        if (_picked is { Gate: true }) Fill();
        ShowAction(research);
    }

    private void Build()
    {
        foreach (var recipe in _research!.Recipes()) _placements[recipe.Id] = recipe.Placement;
        foreach (var tab in _tabs) BuildTab(tab);
        Fill();
    }

    private void BuildTab(TabView tab)
    {
        var side = new Vector2((float)tab.Rings.Side, (float)tab.Rings.Side);
        tab.Map.Size = side;
        tab.Back.Size = side;
        tab.Back.Draw += () => DrawBack(tab);
        tab.Map.AddChild(tab.Back);
        foreach (var node in Content.Research.Nodes.Where(n => n.Tab == tab.Tab.Id && !n.Gate)) tab.Nodes.Add(NodeOf(tab, node));
        double wedge = Mathf.Tau / tab.Tab.Branches.Length;
        for (int b = 0; b < tab.Tab.Branches.Length; b++)
        {
            double angle = -Mathf.Pi / 2 + b * wedge;
            var title = Text(T($"research.branch.{tab.Tab.Branches[b]}"), Style.Slate500, 12);
            tab.Titles.Add((title, new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle))));
            tab.Front.AddChild(title);
        }
        var words = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center }, 4);
        tab.HubTitle.HorizontalAlignment = HorizontalAlignment.Center;
        words.AddChild(tab.HubTitle);
        words.AddChild(tab.HubRows);
        tab.HubWords.AddChild(words);
        tab.Hub.AddChild(tab.HubWords);
        tab.Front.AddChild(tab.Hub);
    }

    private NodeView NodeOf(TabView tab, TechNode node)
    {
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        var box = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(box);
        var picture = Picture(node.Id, Icon);
        picture.CustomMinimumSize = Vector2.Zero;
        root.AddChild(picture);
        var name = Text(NameOf(node.Id), Style.Slate100, NameSize);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.ClipText = true;
        name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        root.AddChild(name);
        var price = new CreditLine(NameSize) { Alignment = BoxContainer.AlignmentMode.Center };
        price.Set(Prices.Of(node));
        root.AddChild(price);
        var done = Text("✓", Bought, NameSize + 2);
        done.HorizontalAlignment = HorizontalAlignment.Center;
        root.AddChild(done);
        tab.Front.AddChild(root);
        var view = new NodeView(node, root, box, picture, name, price, done);
        _tips.Add(box, () => TipOf(view));
        return view;
    }

    /// <summary>A label in the tree's font: its words keep sharp at any size.</summary>
    private static Label Text(string text, Color colour, int size = Tablet.FontSize + 1)
    {
        var label = Tablet.Text(text, colour, size);
        label.LabelSettings.Font = Style.Clear;
        return label;
    }

    /// <summary>What to show of <paramref name="id"/>: a blueprint as the build menu pictures it, a craft or a material as its item.</summary>
    private TextureRect Picture(string id, int size) => new()
    {
        Texture = _placements.GetValueOrDefault(id) is { } p ? BlueprintArt.Thumbnail(p) : ItemIcons.Icon(id, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        CustomMinimumSize = new Vector2(size, size),
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    private string NameOf(string id) =>
        _placements.ContainsKey(id) ? ItemCards.NameOf(id, _placements[id]) : T($"item.{id}");

    /// <summary>The map's pointer: any button drags it, the left one picks what it clicks if it was not dragged.</summary>
    private void Handle(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                if (button.Pressed) (_pressed, _moved) = (true, 0);
                else if (_pressed && _moved < ClickSlack) Click(button.Position);
                if (!button.Pressed) _pressed = false;
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown }:
                Zoom(e, _view);
                break;
            case InputEventMouseMotion { ButtonMask: not 0 } drag:
                _moved += drag.Relative.Length();
                _tab.Camera.Drag(drag.Relative.X, drag.Relative.Y);
                break;
        }
    }

    private void Zoom(InputEvent e, Control over)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel) return;
        var at = _view.GetLocalMousePosition();
        _tab.Camera.Wheel(wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1, at.X, at.Y);
        over.AcceptEvent();
    }

    /// <summary>A click at a point of the view: picks the node there or the hub's gate; off them, picks nothing.</summary>
    private void Click(Vector2 at)
    {
        var (x, y) = _tab.Camera.ToMap(at.X, at.Y);
        var point = new Vector2((float)x, (float)y);
        var hit = _tab.Nodes.FirstOrDefault(v => new Rect2((float)_tab.Rings.Spots[v.Node.Id].X - RootWide / 2f, (float)_tab.Rings.Spots[v.Node.Id].Y - NodeSide / 2f, RootWide, RootTall).HasPoint(point));
        if (hit != null) Pick(hit.Node);
        else if (point.DistanceTo(new Vector2((float)_tab.Rings.Middle, (float)_tab.Rings.Middle)) <= TechRings.HubRadius && _tab.Gate != null) Pick(_tab.Gate);
        else Pick(null);
    }

    /// <summary>Picks a node, the strip below saying what it is.</summary>
    private void Pick(TechNode? node)
    {
        _picked = node;
        _sinceRead = ReadEvery;
        _shown = null;
        Fill();
    }

    private void Choose(TabView tab)
    {
        if (tab == _tab) return;
        Show(_tab, false);
        _tab = tab;
        Show(_tab, true);
        Pick(null);
        PaintTabs();
    }

    private static void Show(TabView tab, bool shown) => tab.Map.Visible = tab.Front.Visible = shown;

    private void HoverTab(int index)
    {
        _hoveredTab = index;
        PaintTabs();
    }

    private void PaintTabs()
    {
        for (int i = 0; i < _tabBoxes.Count; i++) Tablet.PaintTab(_tabBoxes[i], _tabs[i] == _tab, i == _hoveredTab);
    }

    /// <summary>The strip below, built again when what is picked changes: the whole of its width shared out among what it has to say.</summary>
    private void Fill()
    {
        foreach (var child in _facts.GetChildren())
        {
            _facts.RemoveChild(child);
            child.QueueFree();
        }
        if (_picked is not { } node)
        {
            var hint = Text(T("research.pickHint"), Style.Slate500);
            hint.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            _facts.AddChild(hint);
            return;
        }
        if (node.Gate)
        {
            _facts.AddChild(Title(T($"research.gate.{node.Id}"), T("research.gateAbout"), null));
            var asks = Flow(14);
            foreach (var (item, count) in node.Deposit)
            {
                int left = count - (_research?.Deposited(node, item) ?? 0), had = _carried(item);
                asks.AddChild(Named(item, left <= 0 ? $"{NameOf(item)} ✓" : T("research.asked", ("what", NameOf(item)), ("had", had), ("left", left)), AskColour(had, left)));
            }
            _facts.AddChild(Fact(T("research.asks"), GateEdge, asks, 2));
            _facts.AddChild(Fact(T("research.opens"), GateEdge, Pictures(Content.Research.Nodes.Where(n => n.Needs.Contains(node.Id) && !n.Gate).Select(n => n.Id).ToList()), 4));
            return;
        }
        _facts.AddChild(Title(NameOf(node.Id), AboutOf(node.Id), node.Id));
        var link = TechLinks.Of(node);
        if (link.Takes.Count > 0)
        {
            var takes = Flow(10);
            foreach (var (item, count) in link.Takes) takes.AddChild(Named(item, T("research.taken", ("n", count), ("what", NameOf(item))), Style.Slate100));
            _facts.AddChild(Fact(T("research.takes"), From, takes, 3));
        }
        var at = Flow(10);
        foreach (string id in link.MadeAt) at.AddChild(Named(id, NameOf(id), Style.Slate100));
        if (link.MadeAt.Count == 0) at.AddChild(Text(T(_placements.GetValueOrDefault(node.Id) != null ? "research.builtInPlace" : "research.byHand"), Style.Slate500));
        _facts.AddChild(Fact(T("research.madeAt"), MadeAt, at, 2));
        if (link.UsedIn.Count == 0) return;
        _facts.AddChild(Fact(T("research.usedIn"), UsedIn, Pictures(link.UsedIn), 5));
    }

    /// <summary>The pictures of <paramref name="ids"/>, smaller the more of them there are, each named by the pointer.</summary>
    private Control Pictures(IReadOnlyList<string> ids)
    {
        var flow = Flow(4);
        int size = ids.Count > UsedManyFrom ? UsedSmall : UsedLarge;
        foreach (string id in ids)
        {
            var one = Picture(id, size);
            _tips.Add(one, () => new Tip(NameOf(id)));
            flow.AddChild(one);
        }
        return flow;
    }

    /// <summary>What is carried set against what is still asked: green with enough, amber with some.</summary>
    private static Color AskColour(int had, int left) => left <= 0 || had >= left ? Bought : had > 0 ? Warm : Style.Slate100;

    /// <summary>What a blueprint or a craft is, in a line: a machine's own words, or an item's kind.</summary>
    private static string AboutOf(string id) =>
        Table.ContainsKey($"tool.{id}.about") ? T($"tool.{id}.about") : Table.ContainsKey($"item.{id}.kind") ? T($"item.{id}.kind") : "";

    /// <summary>The strip's first column: the large picture, the name and what it is.</summary>
    private Control Title(string name, string about, string? id)
    {
        var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(250, 0) }, 10);
        if (id != null)
        {
            var picture = Picture(id, Big);
            picture.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            row.AddChild(picture);
        }
        var words = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }, 2);
        words.AddChild(Text(name, Style.Slate100, 15));
        var line = Text(about, Style.Slate500, Tablet.HintSize + 1);
        line.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        words.AddChild(line);
        row.AddChild(words);
        return row;
    }

    private static HFlowContainer Flow(int gap)
    {
        var flow = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        flow.AddThemeConstantOverride("h_separation", gap);
        flow.AddThemeConstantOverride("v_separation", 4);
        return flow;
    }

    /// <summary>A picture with words by it.</summary>
    private Control Named(string id, string words, Color colour)
    {
        var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 5);
        row.AddChild(Picture(id, Entry));
        row.AddChild(Text(words, colour));
        return row;
    }

    /// <summary>A column of the strip, taking <paramref name="share"/> of what width is left: its heading in the colour its nodes are lit in, and what it says.</summary>
    private static Control Fact(string heading, Color colour, Control what, float share)
    {
        var column = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsStretchRatio = share }, 4);
        column.AddChild(Text(heading, colour, Tablet.HintSize + 1));
        column.AddChild(what);
        return column;
    }

    /// <summary>What the button says now, and whether it can be pressed.</summary>
    private void ShowAction(IResearchQueries research)
    {
        if (_picked is not { } node)
        {
            _action.Visible = false;
            return;
        }
        _action.Visible = true;
        if (node.Gate)
        {
            bool takes = research.Reached(node) && !research.Done(node) && node.Deposit.Any(d => d.Count > research.Deposited(node, d.Item) && _carried(d.Item) > 0);
            _actionLine.Visible = false;
            _action.Disabled = !takes;
            _action.Text = research.Done(node) ? T("research.gateOpen") : !research.Reached(node) ? T("research.locked") : takes ? T("research.depositHint") : T("research.nothingToDeposit");
            return;
        }
        string? why = research.BuyProblem(node);
        _action.Disabled = why != null;
        _actionLine.Visible = why == null;
        _actionPrice.Set(Prices.Of(node));
        _action.Text = why == null ? "" : T(why);
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
        _action.AddThemeStyleboxOverride("normal", Look(new Color("6b4e14"), Warm));
        _action.AddThemeStyleboxOverride("hover", Look(new Color("8a661c"), Warm));
        _action.AddThemeStyleboxOverride("pressed", Look(new Color("4f3a0f"), Warm));
        _action.AddThemeStyleboxOverride("disabled", Look(new Color("14181f"), Line));
        _action.AddThemeColorOverride("font_color", Style.Slate100);
        _action.AddThemeColorOverride("font_hover_color", Colors.White);
        _action.AddThemeColorOverride("font_pressed_color", Colors.White);
        _action.AddThemeColorOverride("font_disabled_color", Faint);
    }

    /// <summary>The strip's button: buys what is picked, or puts into its gate what the bag holds.</summary>
    private void Act()
    {
        if (_research is not { } research || _picked is not { } node) return;
        if (node.Gate) _send(new Deposit(node.Id));
        else if (research.BuyProblem(node) == null) _send(new BuyBlock(node.Id));
        _sinceRead = ReadEvery;
    }

    private void Restyle(TabView tab, NodeView view, IResearchQueries research)
    {
        var node = view.Node;
        bool done = research.Done(node), reached = research.Reached(node), can = research.BuyProblem(node) == null, near = tab.Near;
        Color? tie = Tie(node);
        Color state = done ? Bought : can ? Warm : reached ? Faint : Socket;
        if (near) Paint(view.Box, done ? BoughtBack : reached ? Socket : Dim, tie ?? (done ? Bought : can ? Warm : reached ? Socket : Line), tie != null ? 3 : 2);
        else Paint(view.Box, tie ?? state, tie ?? state, 0);
        view.Picture.Visible = near;
        view.Picture.Modulate = done || reached || tie != null ? Colors.White : new Color(0.55f, 0.55f, 0.6f, 0.8f);
        view.Root.Modulate = _picked == null || tie != null ? Colors.White : new Color(1, 1, 1, 0.3f);
        view.Name.Visible = near;
        view.Done.Visible = near && done;
        view.Price.Visible = near && !done;
        view.Price.Tint(can ? Warm : Faint);
    }

    /// <summary>The colour that ties <paramref name="node"/> to what is picked, a gate to the ring it opens; none if nothing does.</summary>
    private Color? Tie(TechNode node)
    {
        if (_picked is not { } picked) return null;
        if (picked == node) return Picked;
        if (picked.Gate) return node.Needs.Contains(picked.Id) ? GateEdge : null;
        var link = TechLinks.Of(picked);
        return link.MadeAt.Contains(node.Id) ? MadeAt : link.From.Contains(node.Id) ? From : link.UsedIn.Contains(node.Id) ? UsedIn : null;
    }

    /// <summary>The hub: its gate's name, and by each thing it still asks for, what the bag holds of it over what is still needed.</summary>
    private void RestyleHub(TabView tab, IResearchQueries research)
    {
        tab.Gate = Content.Research.Nodes.FirstOrDefault(n => n.Tab == tab.Tab.Id && n.Gate && !research.Done(n));
        foreach (var child in tab.HubRows.GetChildren())
        {
            tab.HubRows.RemoveChild(child);
            child.QueueFree();
        }
        if (tab.Gate is not { } gate)
        {
            tab.HubTitle.Text = T("research.allOpen");
            Paint(tab.Hub, BoughtBack, Bought, 2);
            return;
        }
        bool reached = research.Reached(gate);
        tab.HubTitle.Text = T($"research.gate.{gate.Id}");
        Paint(tab.Hub, GateBack, _picked == gate ? Picked : reached ? GateEdge : Far, _picked == gate ? 3 : 2);
        float zoom = (float)tab.Camera.Zoom;
        int size = Math.Max(8, Mathf.RoundToInt(Tablet.FontSize * zoom));
        foreach (var (item, count) in gate.Deposit)
        {
            int left = count - research.Deposited(gate, item), had = _carried(item);
            var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center }, 4);
            row.AddChild(Picture(item, Mathf.RoundToInt(18 * zoom)));
            row.AddChild(Text(left <= 0 ? "✓" : $"{had}/{left}", AskColour(had, left), size));
            tab.HubRows.AddChild(row);
        }
    }

    private Tip TipOf(NodeView view)
    {
        var node = view.Node;
        if (_research is not { } research) return new Tip("");
        var lines = new List<TipLine>();
        if (research.BuyProblem(node) is { } why && why != "research.bought") lines.Add(new TipLine(T(why), Tone.Unknown));
        return new Tip(NameOf(node.Id), lines);
    }

    /// <summary>What lies under the nodes: the lit rings, each ring's edge, the branches' borders, and the lines from what is picked to what it ties to.</summary>
    private void DrawBack(TabView tab)
    {
        var rings = tab.Rings;
        var middle = new Vector2((float)rings.Middle, (float)rings.Middle);
        var back = tab.Back;
        back.DrawCircle(middle, (float)tab.Lit, LitFill);
        foreach (double edge in rings.Edges) back.DrawArc(middle, (float)edge, 0, Mathf.Tau, 180, edge <= tab.Lit + 1 ? Bought with { A = 0.5f } : Far, 2);
        int branches = tab.Tab.Branches.Length;
        for (int b = 0; b < branches; b++)
        {
            float angle = -Mathf.Pi / 2 + (b - 0.5f) * Mathf.Tau / branches;
            var way = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            back.DrawLine(middle + way * (float)TechRings.HubRadius, middle + way * (float)rings.Edges[^1], Far, 1);
        }
        if (_picked is not { Gate: false } picked || picked.Tab != tab.Tab.Id) return;
        var link = TechLinks.Of(picked);
        Vector2 At(string id) => new((float)rings.Spots[id].X, (float)rings.Spots[id].Y);
        void Ties(IEnumerable<string> ids, Color colour)
        {
            foreach (string id in ids)
                if (rings.Spots.ContainsKey(id)) back.DrawLine(At(picked.Id), At(id), colour with { A = 0.7f }, 2, true);
        }
        Ties(link.UsedIn, UsedIn);
        Ties(link.From, From);
        Ties(link.MadeAt, MadeAt);
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

    private static void Paint(Panel box, Color fill, Color edge, int border)
    {
        var look = new StyleBoxFlat { BgColor = fill, BorderColor = edge, AntiAliasing = true };
        look.SetBorderWidthAll(border);
        look.SetCornerRadiusAll(Roundest);
        box.AddThemeStyleboxOverride("panel", look);
    }
}
