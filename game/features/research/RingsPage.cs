using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Research;
using static Stillwake.Game.Features.Research.StripParts;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// A tab of the tablet as a map of rings round a hub (2026-10-30-tech-rings, <see cref="TechRings"/>), dragged and zoomed (<see cref="TechCamera"/>):
/// the hub asks for what opens the next ring, the rings filled are lit, a branch a way out. A click picks a node and buys nothing: what it
/// takes, where it is made and what it goes into light up (<see cref="TechLinks"/>), known or not, and the strip below says so and buys.
/// A press counts as a click only if the pointer has not moved. Words are set at the size they are seen, never scaled, so they stay sharp;
/// further out a node keeps its picture and loses its words, and from far it is a plain dot under its branch's name.
/// </summary>
public sealed class RingsPage : ITabletPage
{
    private static readonly Color Socket = new("474c57"), Dim = new("2a2e36"), BoughtBack = new("0b1f16"),
        Far = new("39404c"), LitFill = new("16261f"), GateEdge = new("b08fe0"), GateBack = new("2c2437");

    /// <summary>Under the first zoom words would be too small to read, and only pictures show; under the second pictures would be a smudge too, and nodes turn plain dots.</summary>
    private const double WordsFrom = 0.5, PicturesFrom = 0.28;
    /// <summary>How fast a ring lights as it opens, of what is left a second.</summary>
    private const double LitSpeed = 3;
    /// <summary>A node's sizes on the map at zoom 1; how small a dot gets; words' sizes at zoom 1; how far from its way's edge a branch's name stands.</summary>
    private const int NodeSide = 68, Icon = 40, RootWide = 112, RootTall = 106, LeastDot = 12, NameSize = 12, HubSize = 13, TitleOut = 48;
    private const int Roundest = 4096;

    /// <summary>What a node on the map is made of, kept so a read only restyles it: a rebuild under the pointer would eat a click.</summary>
    private sealed record NodeView(TechNode Node, Control Root, Panel Box, TextureRect Picture, Label Name, CreditLine Price, Label Done);

    private readonly TechTab _tab;
    private readonly Control _view;
    private readonly StripParts _strip;
    private readonly Func<int> _peer;
    private readonly Action<Command> _send;
    private readonly Action _readSoon;
    private readonly TechRings _rings;
    private readonly TechNode[] _own;
    private readonly TechCamera _camera = new();
    // What scales is under _map, what is set at its seen size under _front.
    private readonly Control _map = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private readonly Control _front = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private readonly Control _back = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Panel _hub = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly CenterContainer _hubWords = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Label _hubTitle = Text("", Style.Slate100, HubSize);
    private readonly VBoxContainer _hubRows = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 2);
    private readonly List<NodeView> _nodes = [];
    private readonly List<(Label Title, Vector2 Way)> _titles = [];
    private IResearchQueries? _research;
    private Func<string, int> _carried = _ => 0;
    private TechNode? _picked, _gate;
    private string? _shown;
    private bool _centred, _out, _pressed;
    private double _lit = TechRings.HubRadius, _setAt;
    private float _moved;

    private bool Words => _camera.Zoom >= WordsFrom;
    private bool Pictures => _camera.Zoom >= PicturesFrom;

    /// <param name="tab">The tab of <c>Content.Research</c> it draws.</param>
    /// <param name="view">The tablet's view, which it draws into.</param>
    /// <param name="strip">The strip below and its words.</param>
    /// <param name="peer">This player's peer: whose bag a deposit is taken from.</param>
    /// <param name="send">Sends a command to the session.</param>
    /// <param name="readSoon">Has the tablet read on its next frame.</param>
    public RingsPage(TechTab tab, Control view, StripParts strip, Func<int> peer, Action<Command> send, Action readSoon)
    {
        (_tab, _view, _strip, _peer, _send, _readSoon) = (tab, view, strip, peer, send, readSoon);
        _rings = new TechRings(tab);
        _own = Content.Research.Nodes.Where(n => n.Tab == tab.Id).ToArray();
        view.AddChild(_map);
        view.AddChild(_front);
        strip.Action.Pressed += Act;
    }

    /// <summary>Where a node's middle is on the screen: what a recording points at.</summary>
    public Vector2 ScreenOf(string id) => _view.GlobalPosition + _nodes.First(v => v.Node.Id == id).Root.Position;

    /// <summary>Where the hub's middle is on the screen: what a recording points at.</summary>
    public Vector2 HubAt => _hub.GetGlobalRect().GetCenter();

    public void Show(bool shown)
    {
        _out = _map.Visible = _front.Visible = shown;
        if (!shown) _picked = null;
        _shown = null;
    }

    /// <summary>The rings where the camera has them, growing lit to what is open; the nodes and words set over them at the size they are seen.</summary>
    public void Step(double delta)
    {
        if (_nodes.Count == 0) return;
        _camera.Fit(_view.Size.X, _view.Size.Y, _rings.Side, _rings.Side);
        if (!_centred)
        {
            _centred = true;
            _camera.Centre(_rings.Middle, _rings.Middle);
        }
        float zoom = (float)_camera.Zoom;
        var origin = new Vector2((float)_camera.X, (float)_camera.Y).Round();
        _map.Scale = new Vector2(zoom, zoom);
        _map.Position = origin;
        if (_setAt != _camera.Zoom) SetSizes(zoom);
        foreach (var view in _nodes)
        {
            var spot = _rings.Spots[view.Node.Id];
            view.Root.Position = (origin + new Vector2((float)spot.X, (float)spot.Y) * zoom).Round();
        }
        var middle = origin + new Vector2((float)_rings.Middle, (float)_rings.Middle) * zoom;
        float hub = (float)TechRings.HubRadius * zoom;
        _hub.Position = middle - new Vector2(hub, hub);
        float reach = Words ? hub + TitleOut : (float)_rings.Edges[^1] * zoom - TitleOut / 2f;
        foreach (var (title, way) in _titles) title.Position = (middle + way * reach - title.GetMinimumSize() / 2).Round();
        double lit = OpenEdge();
        if (Math.Abs(lit - _lit) <= 0.5) return;
        _lit += (lit - _lit) * Math.Min(1, LitSpeed * delta);
        _back.QueueRedraw();
    }

    /// <summary>Sets every node and word at the size <paramref name="zoom"/> shows them, and has the next read restyle them for it.</summary>
    private void SetSizes(float zoom)
    {
        _setAt = _camera.Zoom;
        _shown = null;
        _readSoon();
        bool words = Words;
        float side = Pictures ? NodeSide * zoom : Math.Max(LeastDot, NodeSide * zoom);
        int size = Math.Max(8, Mathf.RoundToInt(NameSize * zoom));
        foreach (var view in _nodes)
        {
            view.Box.Position = new Vector2(-side / 2, -side / 2);
            view.Box.Size = new Vector2(side, side);
            float icon = words ? Icon * zoom : side * 0.75f;
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
        _hub.Size = new Vector2(2 * hub, 2 * hub);
        _hubWords.Size = _hub.Size;
        _hubWords.Visible = words;
        _hubTitle.LabelSettings.FontSize = Math.Max(8, Mathf.RoundToInt(HubSize * zoom));
        foreach (var (title, _) in _titles)
        {
            title.LabelSettings.FontSize = words ? 12 : 14;
            title.LabelSettings.FontColor = words ? Style.Slate500 : Style.Slate100;
        }
    }

    /// <summary>How far out the tab's open rings reach; the hub's edge with none open.</summary>
    private double OpenEdge()
    {
        if (_research is not { } research) return TechRings.HubRadius;
        int open = -1;
        for (int ring = 0; ring < _tab.Rings.Length; ring++)
        {
            if (_tab.Rings[ring].Gate is { } gate && !research.Done(Content.Research.Node(gate)!)) break;
            open = ring;
        }
        return open < 0 ? TechRings.HubRadius : _rings.Edges[open];
    }

    /// <summary>Restyles what has changed: a block's state, a gate's deposit, what the bag holds.</summary>
    public void Read(Registry m)
    {
        var research = m.Research;
        _research = research;
        if (_nodes.Count == 0) Build();
        var totals = m.Items.Totals(_peer());
        _carried = item => totals.Where(t => t.Item == item).Sum(t => t.Count);
        string now = (_picked?.Id ?? "") + '|' + string.Join('|', _own.Select(n => n.Gate
            ? (research.Done(n) ? "1" : research.Reached(n) ? "0" : "x") + string.Join(',', n.Deposit.Select(d => $"{research.Deposited(n, d.Item)}/{_carried(d.Item)}"))
            : research.BuyProblem(n) ?? ""));
        if (now == _shown) return;
        _shown = now;
        foreach (var view in _nodes) Restyle(view, research);
        RestyleHub(research);
        _back.QueueRedraw();
        if (!_out) return;
        if (_picked is { Gate: true }) Fill();
        ShowAction(research);
    }

    private void Build()
    {
        _strip.Learn(_research!);
        var side = new Vector2((float)_rings.Side, (float)_rings.Side);
        _map.Size = side;
        _back.Size = side;
        _back.Draw += DrawBack;
        _map.AddChild(_back);
        foreach (var node in _own.Where(n => !n.Gate)) _nodes.Add(NodeOf(node));
        double wedge = Mathf.Tau / _tab.Branches.Length;
        for (int b = 0; b < _tab.Branches.Length; b++)
        {
            double angle = -Mathf.Pi / 2 + b * wedge;
            var title = Text(T($"research.branch.{_tab.Branches[b]}"), Style.Slate500, 12);
            _titles.Add((title, new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle))));
            _front.AddChild(title);
        }
        var words = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center }, 4);
        _hubTitle.HorizontalAlignment = HorizontalAlignment.Center;
        words.AddChild(_hubTitle);
        words.AddChild(_hubRows);
        _hubWords.AddChild(words);
        _hub.AddChild(_hubWords);
        _front.AddChild(_hub);
        if (_out) Fill();
    }

    private NodeView NodeOf(TechNode node)
    {
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        var box = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(box);
        var picture = _strip.Picture(node.Id, Icon);
        picture.CustomMinimumSize = Vector2.Zero;
        root.AddChild(picture);
        var name = Text(_strip.NameOf(node.Id), Style.Slate100, NameSize);
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
        _front.AddChild(root);
        var view = new NodeView(node, root, box, picture, name, price, done);
        _strip.Tips.Add(box, () => TipOf(node.Id));
        return view;
    }

    /// <summary>The map's pointer: any button drags it, the left one picks what it clicks if it was not dragged.</summary>
    public void Pointer(InputEvent e)
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

    public void LetGo() => _pressed = false;

    public void Zoom(InputEvent e, Control over)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel) return;
        var at = _view.GetLocalMousePosition();
        _camera.Wheel(wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1, at.X, at.Y);
        over.AcceptEvent();
    }

    /// <summary>A click at a point of the view: picks the node there or the hub's gate; off them, picks nothing.</summary>
    private void Click(Vector2 at)
    {
        var (x, y) = _camera.ToMap(at.X, at.Y);
        var point = new Vector2((float)x, (float)y);
        var hit = _nodes.FirstOrDefault(v => new Rect2((float)_rings.Spots[v.Node.Id].X - RootWide / 2f, (float)_rings.Spots[v.Node.Id].Y - NodeSide / 2f, RootWide, RootTall).HasPoint(point));
        if (hit != null) Pick(hit.Node);
        else if (point.DistanceTo(new Vector2((float)_rings.Middle, (float)_rings.Middle)) <= TechRings.HubRadius && _gate != null) Pick(_gate);
        else Pick(null);
    }

    /// <summary>Picks a node, the strip below saying what it is.</summary>
    private void Pick(TechNode? node)
    {
        _picked = node;
        _readSoon();
        _shown = null;
        Fill();
    }

    /// <summary>The strip below, built again when what is picked changes: the whole of its width shared out among what it has to say.</summary>
    public void Fill()
    {
        _strip.Clear();
        var facts = _strip.Facts;
        if (_picked is not { } node)
        {
            var hint = Text(T("research.pickHint"), Style.Slate500);
            hint.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            facts.AddChild(hint);
            return;
        }
        if (node.Gate)
        {
            facts.AddChild(_strip.Title(T($"research.gate.{node.Id}"), T("research.gateAbout"), null, TipOf));
            var asks = Flow(14);
            foreach (var (item, count) in node.Deposit)
            {
                int left = _research?.Done(node) == true ? 0 : count - (_research?.Deposited(node, item) ?? 0), had = _carried(item);
                asks.AddChild(_strip.Named(item, left <= 0 ? $"{_strip.NameOf(item)} ✓" : T("research.asked", ("what", _strip.NameOf(item)), ("had", had), ("left", left)), AskColour(had, left)));
            }
            facts.AddChild(Fact(T("research.asks"), GateEdge, asks, 2));
            facts.AddChild(Fact(T("research.opens"), GateEdge, _strip.Pictures(Content.Research.Nodes.Where(n => n.Needs.Contains(node.Id) && !n.Gate).Select(n => n.Id).ToList(), TipOf), 4));
            return;
        }
        facts.AddChild(_strip.Title(_strip.NameOf(node.Id), Table.ContainsKey($"item.{node.Id}.kind") ? T($"item.{node.Id}.kind") : "", node.Id, TipOf));
        var link = TechLinks.Of(node.Id);
        if (link.Takes.Count > 0)
        {
            var takes = Flow(10);
            foreach (var (item, count) in link.Takes) takes.AddChild(_strip.Named(item, T("research.taken", ("n", count), ("what", _strip.NameOf(item))), Style.Slate100));
            facts.AddChild(Fact(T("research.takes"), From, takes, 3));
        }
        var at = Flow(10);
        foreach (string id in link.MadeAt) at.AddChild(_strip.Named(id, _strip.NameOf(id), Style.Slate100));
        if (link.MadeAt.Count == 0) at.AddChild(Text(T(_strip.PlacementOf(node.Id) != null ? "research.builtInPlace" : "research.byHand"), Style.Slate500));
        facts.AddChild(Fact(T("research.madeAt"), MadeAt, at, 2));
        if (link.UsedIn.Count == 0) return;
        facts.AddChild(Fact(T("research.usedIn"), UsedIn, _strip.Pictures(link.UsedIn, TipOf), 5));
    }

    /// <summary>What is carried set against what is still asked: green with enough, amber with some.</summary>
    private static Color AskColour(int had, int left) => left <= 0 || had >= left ? Bought : had > 0 ? Warm : Style.Slate100;

    /// <summary>What the button says now, and whether it can be pressed.</summary>
    private void ShowAction(IResearchQueries research)
    {
        var action = _strip.Action;
        if (_picked is not { } node)
        {
            action.Visible = false;
            return;
        }
        action.Visible = true;
        if (node.Gate)
        {
            bool takes = research.Reached(node) && !research.Done(node) && node.Deposit.Any(d => d.Count > research.Deposited(node, d.Item) && _carried(d.Item) > 0);
            _strip.ActionLine.Visible = false;
            action.Disabled = !takes;
            action.Text = research.Done(node) ? T("research.gateOpen") : !research.Reached(node) ? T("research.locked") : takes ? T("research.depositHint") : T("research.nothingToDeposit");
            return;
        }
        string? why = research.BuyProblem(node);
        action.Disabled = why != null;
        _strip.ActionLine.Visible = why == null;
        _strip.ActionPrice.Set(Prices.Of(node));
        action.Text = why == null ? "" : T(why);
    }

    /// <summary>The strip's button: buys what is picked, or puts into its gate what the bag holds. A page off the screen has nothing picked.</summary>
    private void Act()
    {
        if (_research is not { } research || _picked is not { } node) return;
        if (node.Gate) _send(new Deposit(node.Id));
        else if (research.BuyProblem(node) == null) _send(new BuyBlock(node.Id));
        _readSoon();
    }

    private void Restyle(NodeView view, IResearchQueries research)
    {
        var node = view.Node;
        bool done = research.Done(node), reached = research.Reached(node), can = research.BuyProblem(node) == null, words = Words, pictures = Pictures;
        Color? tie = Tie(node);
        Color state = done ? Bought : can ? Warm : reached ? Faint : Socket;
        if (pictures) Paint(view.Box, done ? BoughtBack : reached ? Socket : Dim, tie ?? (done ? Bought : can ? Warm : reached ? Socket : Line), tie != null ? 3 : 2);
        else Paint(view.Box, tie ?? state, tie ?? state, 0);
        view.Picture.Visible = pictures;
        view.Picture.Modulate = done || reached || tie != null ? Colors.White : new Color(0.55f, 0.55f, 0.6f, 0.8f);
        view.Root.Modulate = _picked == null || tie != null ? Colors.White : new Color(1, 1, 1, 0.3f);
        view.Name.Visible = words;
        view.Done.Visible = words && done;
        view.Price.Visible = words && !done;
        view.Price.Tint(can ? Warm : Faint);
    }

    /// <summary>The colour that ties <paramref name="node"/> to what is picked, a gate to the ring it opens; none if nothing does.</summary>
    private Color? Tie(TechNode node)
    {
        if (_picked is not { } picked) return null;
        if (picked == node) return Picked;
        if (picked.Gate) return node.Needs.Contains(picked.Id) ? GateEdge : null;
        var link = TechLinks.Of(picked.Id);
        return link.MadeAt.Contains(node.Id) ? MadeAt : link.From.Contains(node.Id) ? From : link.UsedIn.Contains(node.Id) ? UsedIn : null;
    }

    /// <summary>The hub: its gate's name, and by each thing it still asks for, what the bag holds of it over what is still needed.</summary>
    private void RestyleHub(IResearchQueries research)
    {
        _gate = _own.FirstOrDefault(n => n.Gate && !research.Done(n));
        foreach (var child in _hubRows.GetChildren())
        {
            _hubRows.RemoveChild(child);
            child.QueueFree();
        }
        if (_gate is not { } gate)
        {
            _hubTitle.Text = T("research.allOpen");
            Paint(_hub, BoughtBack, Bought, 2);
            return;
        }
        bool reached = research.Reached(gate);
        _hubTitle.Text = T($"research.gate.{gate.Id}");
        Paint(_hub, GateBack, _picked == gate ? Picked : reached ? GateEdge : Far, _picked == gate ? 3 : 2);
        float zoom = (float)_camera.Zoom;
        int size = Math.Max(8, Mathf.RoundToInt(Tablet.FontSize * zoom));
        foreach (var (item, count) in gate.Deposit)
        {
            int left = count - research.Deposited(gate, item), had = _carried(item);
            var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center }, 4);
            row.AddChild(_strip.Picture(item, Mathf.RoundToInt(18 * zoom)));
            row.AddChild(Text(left <= 0 ? "✓" : $"{had}/{left}", AskColour(had, left), size));
            _hubRows.AddChild(row);
        }
    }

    /// <summary>What the pointer says of <paramref name="id"/>: a thing made its card, a blueprint its picture, name and words; and why it cannot be bought yet.</summary>
    private Tip TipOf(string id)
    {
        if (Content.Upgrades.Of(id) is { } upgrade) return new Tip(_strip.NameOf(id), [new TipLine(T($"upgrade.{upgrade.Kind}.about"), Tone.Dim)]);
        if (_strip.PlacementOf(id) is not { } placement) return new Tip(_strip.NameOf(id)) { Card = ItemCards.Of(id) };
        var lines = new List<TipLine>();
        if (Table.ContainsKey($"tool.{id}.about")) lines.Add(new TipLine(T($"tool.{id}.about"), Tone.Dim));
        if (Content.Research.Node(id) is { } node && _research?.BuyProblem(node) is { } why && why != "research.bought") lines.Add(new TipLine(T(why), Tone.Unknown));
        return new Tip(_strip.NameOf(id), lines) { Picture = placement };
    }

    /// <summary>What lies under the nodes: the lit rings, each ring's edge, the branches' borders, and the lines from what is picked to what it ties to.</summary>
    private void DrawBack()
    {
        var middle = new Vector2((float)_rings.Middle, (float)_rings.Middle);
        _back.DrawCircle(middle, (float)_lit, LitFill);
        foreach (double edge in _rings.Edges) _back.DrawArc(middle, (float)edge, 0, Mathf.Tau, 180, edge <= _lit + 1 ? Bought with { A = 0.5f } : Far, 2);
        int branches = _tab.Branches.Length;
        for (int b = 0; b < branches; b++)
        {
            float angle = -Mathf.Pi / 2 + (b - 0.5f) * Mathf.Tau / branches;
            var way = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            _back.DrawLine(middle + way * (float)TechRings.HubRadius, middle + way * (float)_rings.Edges[^1], Far, 1);
        }
        if (_picked is not { Gate: false } picked) return;
        var link = TechLinks.Of(picked.Id);
        Vector2 At(string id) => new((float)_rings.Spots[id].X, (float)_rings.Spots[id].Y);
        void Ties(IEnumerable<string> ids, Color colour)
        {
            foreach (string id in ids)
                if (_rings.Spots.ContainsKey(id)) _back.DrawLine(At(picked.Id), At(id), colour with { A = 0.7f }, 2, true);
        }
        Ties(link.UsedIn, UsedIn);
        Ties(link.From, From);
        Ties(link.MadeAt, MadeAt);
    }

    private static void Paint(Panel box, Color fill, Color edge, int border)
    {
        var look = new StyleBoxFlat { BgColor = fill, BorderColor = edge, AntiAliasing = true };
        look.SetBorderWidthAll(border);
        look.SetCornerRadiusAll(Roundest);
        box.AddThemeStyleboxOverride("panel", look);
    }
}
