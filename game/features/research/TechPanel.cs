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
/// The tech tree on the tablet (2026-10-30-tablet-tree), nearly the whole screen: T takes it out anywhere and puts it away; Esc, Tab, E
/// and B put it away too. Along the top the ship's credits, how far the tree is walked and what comes next, which brings it into view
/// when clicked. Below, the map (2026-10-30-tech-graph): a band a category, lines running left to right from what a node needs, dragged
/// and zoomed (<see cref="TechCamera"/>). A block is green once bought, edged in amber while it can be bought, dim while out of reach;
/// a gate takes what the bag holds of what it asks for. A press on a node counts only if the pointer has not moved: a drag starting
/// on one buys nothing. The world runs on behind it.
/// </summary>
public partial class TechPanel : CanvasLayer
{
    private static readonly Color Line = new("1a1e25"), Socket = new("474c57"), Dim = new("2a2e36"), Lit = new("6fe08a"), LitBack = new("0b1f16"),
        Faint = new("5b6170"), Warm = new("f0c050"), Far = new("39404c"), BandA = new("161c24"), BandB = new("12171e"), GateBack = new("2c2437"), GateEdge = new("b08fe0");

    /// <summary>Seconds between reads of the world: a panel reads on a timer, never every frame.</summary>
    private const double ReadEvery = 0.25;
    /// <summary>The gap kept between the tablet's art and the window's edge.</summary>
    private const int Margin = 12;
    /// <summary>Pixels the pointer may move in a press still meant as a click.</summary>
    private const float ClickSlack = 4;
    /// <summary>Under this zoom a node's words would be a smudge: its pictures and colours say enough.</summary>
    private const double WordsFrom = 0.55;
    private const int Pad = 10, Icon = 34, CellWide = 148, CellTall = 72, NodeWide = 124, NodeTall = 62, BandLeft = 10, BandHead = 22;

    /// <summary>What a node on the map is made of, kept so a read only restyles it: a rebuild under the pointer would eat a click.</summary>
    private sealed record NodeView(TechNode Node, Panel Box, Label Words, List<TextureRect> Pictures);

    private readonly Ui _ui;
    private readonly Func<bool> _inWorld;
    private readonly Func<Registry?> _world;
    private readonly Func<int> _peer;
    private readonly Action<Command> _send;
    private readonly Tablet _tablet = new();
    private readonly HBoxContainer _head = Style.Gap(new HBoxContainer { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore }, 16);
    private readonly Label _credits = Tablet.Text("", Warm), _progress = Tablet.Text("", Style.Slate100), _next = Tablet.Text("", Style.Sky200);
    private readonly HBoxContainer _foot = Style.Gap(new HBoxContainer { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
    private readonly Control _view = new() { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly Control _map = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Control _lines = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly TipSpots _tips;
    private readonly TechCamera _camera = new();
    private readonly List<NodeView> _nodes = new();
    private readonly List<Label> _titles = new();
    private readonly TechMap _layout = new(Content.Research.Categories);
    private IResearchQueries? _research;
    private TechNode? _nextNode;
    private string? _shown;
    private double _sinceRead = ReadEvery;
    private bool _focus = true, _wordsOn = true, _pressed;
    private float _moved;

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
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Theme = new Theme { DefaultFont = Style.Mono, DefaultFontSize = Tablet.FontSize } };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        root.AddChild(_tablet);
        _next.MouseFilter = Control.MouseFilterEnum.Stop;
        _next.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } || _nextNode == null) return;
            Bring(_nextNode);
            _next.AcceptEvent();
        };
        _head.AddChild(_credits);
        _head.AddChild(_progress);
        _head.AddChild(_next);
        _foot.AddChild(Tablet.Text(T("research.title"), Style.Slate100));
        var hint = Tablet.Text(T("research.hint"), Style.Slate500, Tablet.HintSize);
        hint.ClipText = true;
        hint.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        hint.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _foot.AddChild(hint);
        _view.AddChild(_map);
        _map.AddChild(_lines);
        _lines.Draw += DrawLines;
        _view.GuiInput += Handle;
        // The wheel over the bezel zooms as over the map; it never reaches the hotbar.
        _tablet.GuiInput += e => Zoom(e, _tablet);
        _tablet.Bezel.GuiInput += e => Zoom(e, _tablet.Bezel);
        foreach (var part in (Control[])[_head, _view, _foot]) _tablet.OnScreen(part);
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

    /// <summary>Where a node's middle is on the screen: what a recording points at.</summary>
    public Vector2 ScreenOf(string id)
    {
        var box = _nodes.First(v => v.Node.Id == id).Box;
        var at = box.Position + box.Size / 2;
        return _view.GlobalPosition + _map.Position + at * _map.Scale;
    }

    /// <summary>Puts it away: T again, Esc through the menu's <c>Escape</c>, Tab, E or B.</summary>
    public void Close()
    {
        _ui.TreeOpen.Set(false);
        _pressed = false;
    }

    public override void _Process(double delta)
    {
        bool open = Shown && _world() != null;
        _tablet.Step(open, delta);
        if (!_tablet.Shown) return;
        LayOut();
        Place();
        var mouse = _view.GetViewport().GetMousePosition();
        _tips.Frame(open && !_ui.MenuOpen.Get() && _view.GetGlobalRect().HasPoint(mouse) ? mouse : null);
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
        const int s = TabletLayout.ArtScale, around = TabletLayout.ArtAround * s;
        float width = Mathf.Floor(screen.X - around - 2 * Margin), height = Mathf.Floor(screen.Y - around - 2 * Margin);
        _tablet.Size = new Vector2(width, height);
        var rest = ((screen - new Vector2(width + around, height + around)) / 2 + new Vector2(TabletLayout.ScreenLeft * s, TabletLayout.ScreenTop * s)).Round();
        _tablet.Position = rest + new Vector2(0, _tablet.Drop(rest.Y));
        const int pad = TabletLayout.Pad, bodyTop = pad + TabletLayout.TabHeight + pad;
        _head.Position = new Vector2(pad, pad);
        _head.Size = new Vector2(width - 2 * pad, TabletLayout.TabHeight);
        _foot.Position = new Vector2(pad, height - pad - TabletLayout.FootHeight);
        _foot.Size = new Vector2(width - 2 * pad, TabletLayout.FootHeight);
        _view.Position = new Vector2(pad, bodyTop);
        _view.Size = new Vector2(width - 2 * pad, _foot.Position.Y - pad - bodyTop);
    }

    /// <summary>The map where the camera has it; band names keep their size and stay in view however it lies.</summary>
    private void Place()
    {
        _camera.Fit(_view.Size.X, _view.Size.Y, _map.CustomMinimumSize.X, _map.CustomMinimumSize.Y);
        float zoom = (float)_camera.Zoom;
        _map.Scale = new Vector2(zoom, zoom);
        _map.Position = new Vector2((float)_camera.X, (float)_camera.Y).Round();
        foreach (var title in _titles)
        {
            title.Scale = new Vector2(1 / zoom, 1 / zoom);
            title.Position = new Vector2(Mathf.Max(0, -_map.Position.X / zoom) + 6 / zoom, title.Position.Y);
        }
        bool words = _camera.Zoom >= WordsFrom;
        if (words == _wordsOn) return;
        _wordsOn = words;
        foreach (var view in _nodes) view.Words.Visible = words;
    }

    /// <summary>Restyles what has changed: a block's state, a gate's deposit, what the bag holds. Credits only retype their line.</summary>
    private void Read(Registry m)
    {
        var research = m.Research;
        _research = research;
        if (_nodes.Count == 0) Build(research);
        _credits.Text = T("research.credits", ("n", research.Credits));
        var all = Content.Research.Nodes;
        _progress.Text = T("research.progress", ("done", all.Count(research.Done)), ("all", all.Count));
        var open = all.Where(n => !research.Done(n) && research.Reached(n)).ToList();
        _nextNode = open.FirstOrDefault(n => !n.Gate && research.BuyProblem(n) == null) ?? open.FirstOrDefault();
        _next.Text = _nextNode is { } next ? T("research.next", ("what", NameOf(next, research))) : "";
        var carried = m.Items.Totals(_peer());
        int Carried(string item) => carried.Where(t => t.Item == item).Sum(t => t.Count);
        string now = string.Join('|', _nodes.Select(v => v.Node.Gate
            ? (research.Done(v.Node) ? "1" : research.Reached(v.Node) ? "0" : "x") + string.Join(',', v.Node.Deposit.Select(d => $"{research.Deposited(v.Node, d.Item)}/{Carried(d.Item)}"))
            : research.BuyProblem(v.Node) ?? ""));
        if (now != _shown)
        {
            _shown = now;
            foreach (var view in _nodes) Restyle(view, research);
            _lines.QueueRedraw();
        }
        if (!_focus) return;
        // The first time out it opens on the first thing to do; after that it stays where it was left.
        _focus = false;
        if (_nextNode != null) Bring(_nextNode);
    }

    /// <summary>A node's name: a gate's own, a block's the first thing it opens.</summary>
    private static string NameOf(TechNode node, IResearchQueries research)
    {
        if (node.Gate) return T($"research.gate.{node.Id}");
        string first = node.Opens[0];
        return ItemCards.NameOf(first, research.Recipes().First(r => r.Id == first).Placement);
    }

    /// <summary>Brings a node to the middle of the view.</summary>
    private void Bring(TechNode node)
    {
        var box = _nodes.First(v => v.Node == node).Box;
        _camera.Fit(_view.Size.X, _view.Size.Y, _map.CustomMinimumSize.X, _map.CustomMinimumSize.Y);
        _camera.Centre(box.Position.X + box.Size.X / 2, box.Position.Y + box.Size.Y / 2);
    }

    private void Build(IResearchQueries research)
    {
        _map.CustomMinimumSize = new Vector2(BandLeft + _layout.Columns * CellWide + Pad, _layout.Rows * CellTall + _layout.Bands.Count * BandHead + Pad);
        _map.Size = _map.CustomMinimumSize;
        _lines.Size = _map.CustomMinimumSize;
        int band = 0;
        foreach (var each in _layout.Bands)
        {
            var back = new ColorRect
            {
                Color = band++ % 2 == 0 ? BandA : BandB,
                Position = new Vector2(0, RowY(each.FirstRow) - BandHead),
                Size = new Vector2(_map.CustomMinimumSize.X, each.Rows * CellTall + BandHead),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            _map.AddChild(back);
            _map.MoveChild(back, 0);
            var title = Tablet.Text(T($"research.category.{each.Category.Id}"), Style.Slate500);
            title.Position = new Vector2(6, RowY(each.FirstRow) - BandHead);
            _titles.Add(title);
            _map.AddChild(title);
        }
        foreach (var node in Content.Research.Nodes) _nodes.Add(NodeOf(node, research));
    }

    private NodeView NodeOf(TechNode node, IResearchQueries research)
    {
        var spot = _layout.Spots[node.Id];
        var box = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
        Restyle(box, Dim, Line);
        box.Position = new Vector2(BandLeft + spot.Column * CellWide, RowY(spot.Row) + (CellTall - NodeTall) / 2);
        box.Size = new Vector2(NodeWide, NodeTall);
        var icons = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Position = new Vector2(4, 3) };
        icons.AddThemeConstantOverride("separation", 2);
        var pictures = new List<TextureRect>();
        var recipes = research.Recipes();
        // At most three pictures fit: a block of more shows its first, and its tooltip names them all.
        foreach (string id in node.Gate ? node.Deposit.Select(d => d.Item) : node.Opens)
        {
            if (pictures.Count == 3) break;
            var picture = Picture(id, node.Gate ? null : recipes.First(r => r.Id == id).Placement);
            pictures.Add(picture);
            icons.AddChild(picture);
        }
        box.AddChild(icons);
        var words = Tablet.Text("", Faint, 14);
        words.Position = new Vector2(6, Icon + 5);
        box.AddChild(words);
        _map.AddChild(box);
        var view = new NodeView(node, box, words, pictures);
        _tips.Add(box, () => TipOf(view));
        return view;
    }

    /// <summary>The map's pointer: any button drags it, the left one clicks a node if it was not dragged.</summary>
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
                _camera.Drag(drag.Relative.X, drag.Relative.Y);
                break;
        }
    }

    private void Zoom(InputEvent e, Control over)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel) return;
        var at = _view.GetLocalMousePosition();
        _camera.Wheel(wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1, at.X, at.Y);
        over.AcceptEvent();
    }

    /// <summary>A click at a point of the view: the node there is bought or deposited in, if it can be.</summary>
    private void Click(Vector2 at)
    {
        var (x, y) = _camera.ToMap(at.X, at.Y);
        var point = new Vector2((float)x, (float)y);
        var hit = _nodes.FirstOrDefault(v => new Rect2(v.Box.Position, v.Box.Size).HasPoint(point));
        if (hit == null || _research is not { } research) return;
        var node = hit.Node;
        if (node.Gate)
        {
            if (!research.Done(node) && research.Reached(node)) _send(new Deposit(node.Id));
        }
        else if (research.BuyProblem(node) == null) _send(new BuyBlock(node.Id));
    }

    private void Restyle(NodeView view, IResearchQueries research)
    {
        var node = view.Node;
        bool done = research.Done(node), reached = research.Reached(node), can = !node.Gate && research.BuyProblem(node) == null;
        Color back = done ? LitBack : node.Gate && reached ? GateBack : reached ? Socket : Dim;
        Restyle(view.Box, back, done ? Lit : node.Gate && reached ? GateEdge : can ? Warm : Line);
        foreach (var picture in view.Pictures) picture.Modulate = done || reached ? Colors.White : new Color(0.45f, 0.45f, 0.5f, 0.7f);
        if (node.Gate)
        {
            int had = node.Deposit.Sum(d => research.Deposited(node, d.Item)), all = node.Deposit.Sum(d => d.Count);
            view.Words.Text = done ? "✓" : $"{had}/{all}";
            view.Words.LabelSettings.FontColor = done ? Lit : reached ? GateEdge : Faint;
            return;
        }
        view.Words.Text = done ? "✓" : Prices.Of(node).ToString();
        view.Words.LabelSettings.FontColor = done ? Lit : can ? Warm : Faint;
    }

    private Tip TipOf(NodeView view)
    {
        var node = view.Node;
        if (_research is not { } research || _world() is not { } world) return new Tip("");
        if (node.Gate)
        {
            var carried = world.Items.Totals(_peer());
            var asked = node.Deposit.Select(d =>
            {
                int bag = carried.Where(t => t.Item == d.Item).Sum(t => t.Count);
                return new TipLine($"{T($"item.{d.Item}")}: {research.Deposited(node, d.Item)}/{d.Count} ({T("research.inTheBag", ("n", bag))})", bag > 0 ? Tone.Good : Tone.Unknown);
            }).ToList();
            if (!research.Reached(node)) asked.Add(new TipLine(T("research.locked"), Tone.Unknown));
            else if (!research.Done(node)) asked.Add(new TipLine(T("research.depositHint"), Tone.Good));
            return new Tip(T($"research.gate.{node.Id}"), asked);
        }
        var recipes = research.Recipes();
        var names = node.Opens.Select(id => ItemCards.NameOf(id, recipes.First(r => r.Id == id).Placement)).ToList();
        var lines = names.Skip(1).Select(n => new TipLine(n, Tone.Good)).ToList();
        if (research.BuyProblem(node) is { } why && why != "research.bought") lines.Add(new TipLine(T(why), Tone.Unknown));
        return new Tip(names[0], lines);
    }

    /// <summary>Lines from what each node needs: bright once it is done, an elbow halfway between the columns.</summary>
    private void DrawLines()
    {
        if (_research is not { } research) return;
        Vector2 Out(TechSpot s) => new(BandLeft + s.Column * CellWide + NodeWide, RowY(s.Row) + CellTall / 2);
        Vector2 In(TechSpot s) => new(BandLeft + s.Column * CellWide, RowY(s.Row) + CellTall / 2);
        foreach (var node in Content.Research.Nodes)
            foreach (string need in node.Needs)
            {
                Vector2 from = Out(_layout.Spots[need]), to = In(_layout.Spots[node.Id]);
                float elbow = to.X - (CellWide - NodeWide) / 2f;
                var colour = research.Done(Content.Research.Node(need)!) ? Lit : Far;
                _lines.DrawPolyline([from, new Vector2(elbow, from.Y), new Vector2(elbow, to.Y), to], colour, 2);
            }
    }

    /// <summary>Where row <paramref name="row"/> starts: each band so far has a head over its rows.</summary>
    private float RowY(int row) => row * CellTall + _layout.Bands.Count(b => b.FirstRow <= row) * BandHead;

    /// <summary>A blueprint as the build menu pictures it, a craft or a material as its item.</summary>
    private static TextureRect Picture(string id, Placement? placement) => new()
    {
        Texture = placement is { } p ? BlueprintArt.Thumbnail(p) : ItemIcons.Icon(id, Icon),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        CustomMinimumSize = new Vector2(Icon, Icon),
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

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

    private static void Restyle(Panel box, Color fill, Color edge)
    {
        var look = new StyleBoxFlat { BgColor = fill, BorderColor = edge };
        look.SetBorderWidthAll(2);
        look.SetCornerRadiusAll(2);
        box.AddThemeStyleboxOverride("panel", look);
    }
}
