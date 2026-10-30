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
/// A tech station's window, opened by the crew's E at it through <see cref="Ui.OpenTech"/>: the ship's credits over the tree's map
/// (2026-10-30-tech-graph), a band a category, lines running left to right from what a node needs. A block is green once bought,
/// edged in amber while it can be bought, dim while out of reach; a gate takes what the bag holds of what it asks for. Dragged or
/// scrolled. E and Esc put it away; the world runs on behind it.
/// </summary>
public partial class TechPanel : CanvasLayer
{
    private static readonly Color Steel = new("7f848f"), Line = new("1a1e25"), Glass = new("10161d"), Socket = new("474c57"),
        Dim = new("2a2e36"), Lit = new("6fe08a"), LitBack = new("0b1f16"), Faint = new("5b6170"), Warm = new("f0c050"), Far = new("39404c"),
        BandA = new("161c24"), BandB = new("12171e"), GateBack = new("2c2437"), GateEdge = new("b08fe0");

    /// <summary>Seconds between reads of the world: a panel reads on a timer, never every frame.</summary>
    private const double ReadEvery = 0.25;
    private const int Pad = 10, Icon = 34, CellWide = 148, CellTall = 72, NodeWide = 124, NodeTall = 62, BandLeft = 10, BandHead = 22;

    /// <summary>What a node on the map is made of, kept so a read only restyles it: a rebuild under the pointer would eat a click.</summary>
    private sealed record NodeView(TechNode Node, Panel Box, Label Words, List<TextureRect> Pictures);

    private readonly Ui _ui;
    private readonly Func<Registry?> _world;
    private readonly Func<int> _peer;
    private readonly Action<Command> _send;
    private readonly Panel _frame = Boxed(Steel, 3, 4);
    private readonly Panel _screen = Boxed(Glass, 2, 2);
    private readonly Label _credits = Words2("", Warm);
    private readonly ScrollContainer _scroll = new();
    private readonly Control _map = new() { MouseFilter = Control.MouseFilterEnum.Pass };
    private readonly Control _lines = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly TipSpots _tips;
    private readonly List<NodeView> _nodes = new();
    private readonly List<Label> _titles = new();
    private readonly TechMap _layout = new(Content.Research.Categories);
    private IResearchQueries? _research;
    private string? _shown;
    private double _sinceRead;
    private bool _focus;

    /// <param name="ui">Which station is open, set by the crew's E, and cleared here when it is put away.</param>
    /// <param name="world">The world the station stands in; null with none played.</param>
    /// <param name="peer">This player's peer: whose bag a deposit is taken from.</param>
    /// <param name="send">Sends a command to the session.</param>
    public TechPanel(Ui ui, Func<Registry?> world, Func<int> peer, Action<Command> send)
    {
        (_ui, _world, _peer, _send) = (ui, world, peer, send);
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.Handbook;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Theme = new Theme { DefaultFont = Style.Mono, DefaultFontSize = 16 } };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        root.AddChild(_frame);
        _frame.AddChild(_screen);
        _screen.ClipContents = true;
        _screen.AddChild(_credits);
        _screen.AddChild(_scroll);
        _scroll.AddChild(_map);
        _map.AddChild(_lines);
        _lines.Draw += DrawLines;
        _map.GuiInput += Pan;
        _frame.Visible = false;
        ui.OpenTech.Subscribe(() =>
        {
            _shown = null;
            _sinceRead = ReadEvery;
            _focus = true;
        });
    }

    /// <summary>Whether a station is open: what Esc puts away before it opens the menu.</summary>
    public bool Shown => _ui.OpenTech.Get() != null;

    /// <summary>Puts it away: E, or Esc through the menu's <c>Escape</c>.</summary>
    public void Close() => _ui.OpenTech.Set(null);

    public override void _Process(double delta)
    {
        bool open = Shown && _world() != null;
        _frame.Visible = open;
        if (!open)
        {
            _tips.Show(null);
            return;
        }
        LayOut();
        // A band's name stays in view however far along the map is scrolled.
        foreach (var title in _titles) title.Position = new Vector2(_scroll.ScrollHorizontal + 6, title.Position.Y);
        _tips.Frame(_ui.MenuOpen.Get() ? null : _frame.GetViewport().GetMousePosition());
        _sinceRead += delta;
        if (_sinceRead < ReadEvery) return;
        _sinceRead = 0;
        Read(_world()!);
    }

    private void LayOut()
    {
        var view = _frame.GetViewportRect().Size;
        float width = Mathf.Clamp(view.X * 0.86f, 560, 1800), height = Mathf.Clamp(view.Y * 0.84f, 380, 1000);
        _frame.Size = new Vector2(width, height);
        _frame.Position = ((view - _frame.Size) / 2).Round();
        _screen.Position = new Vector2(Pad, Pad);
        _screen.Size = new Vector2(width - 2 * Pad, height - 2 * Pad);
        _credits.Position = new Vector2(Pad, 4);
        _scroll.Position = new Vector2(2, 28);
        _scroll.Size = _screen.Size - new Vector2(4, 30);
    }

    /// <summary>Restyles what has changed: a block's state, a gate's deposit, what the bag holds. Credits only retype their line.</summary>
    private void Read(Registry m)
    {
        var research = m.Research;
        _research = research;
        if (_nodes.Count == 0) Build(research);
        _credits.Text = T("research.credits", ("n", research.Credits));
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
        if (_focus) Focus(research);
    }

    /// <summary>Scrolls to the first thing there is to do, once the map is laid out.</summary>
    private void Focus(IResearchQueries research)
    {
        if (_scroll.GetHScrollBar().MaxValue <= 0) return;
        _focus = false;
        var next = Content.Research.Nodes.FirstOrDefault(n => !research.Done(n) && research.Reached(n));
        if (next == null) return;
        var spot = _layout.Spots[next.Id];
        _scroll.ScrollHorizontal = Math.Max(0, BandLeft + spot.Column * CellWide - CellWide);
        _scroll.ScrollVertical = Math.Max(0, (int)RowY(spot.Row) - CellTall - BandHead);
    }

    private void Build(IResearchQueries research)
    {
        _map.CustomMinimumSize = new Vector2(BandLeft + _layout.Columns * CellWide + Pad, _layout.Rows * CellTall + _layout.Bands.Count * BandHead + Pad);
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
                ShowBehindParent = false,
            };
            _map.AddChild(back);
            _map.MoveChild(back, 0);
            var title = Words2(T($"research.category.{each.Category.Id}"), Faint);
            title.Position = new Vector2(6, RowY(each.FirstRow) - BandHead);
            _titles.Add(title);
            _map.AddChild(title);
        }
        foreach (var node in Content.Research.Nodes) _nodes.Add(NodeOf(node, research));
    }

    private NodeView NodeOf(TechNode node, IResearchQueries research)
    {
        var spot = _layout.Spots[node.Id];
        var box = Boxed(Dim, 2, 2);
        box.MouseFilter = Control.MouseFilterEnum.Pass;
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
        var words = Words2("", Faint);
        words.Position = new Vector2(6, Icon + 3);
        box.AddChild(words);
        _map.AddChild(box);
        var view = new NodeView(node, box, words, pictures);
        _tips.Add(box, () => TipOf(view));
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            Press(node);
        };
        return view;
    }

    private void Press(TechNode node)
    {
        if (_research is not { } research || _ui.OpenTech.Get() is not int station) return;
        if (node.Gate)
        {
            if (!research.Done(node) && research.Reached(node)) _send(new Deposit(station, node.Id));
        }
        else if (research.BuyProblem(node) == null) _send(new BuyBlock(station, node.Id));
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
            view.Words.AddThemeColorOverride("font_color", done ? Lit : reached ? GateEdge : Faint);
            return;
        }
        view.Words.Text = done ? "✓" : Prices.Of(node).ToString();
        view.Words.AddThemeColorOverride("font_color", done ? Lit : can ? Warm : Faint);
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

    /// <summary>Dragging the map scrolls it.</summary>
    private void Pan(InputEvent e)
    {
        if (e is not InputEventMouseMotion { ButtonMask: MouseButtonMask.Left or MouseButtonMask.Right } drag) return;
        _scroll.ScrollHorizontal -= (int)drag.Relative.X;
        _scroll.ScrollVertical -= (int)drag.Relative.Y;
    }

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

    private static Label Words2(string text, Color colour)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeColorOverride("font_color", colour);
        return label;
    }

    public override void _Input(InputEvent e)
    {
        // E puts it away and is spent, so the crew's E does not open it again at once.
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.E } || !Shown) return;
        Close();
        GetViewport().SetInputAsHandled();
    }

    private static Panel Boxed(Color fill, int border, int corner)
    {
        var box = new Panel { MouseFilter = Control.MouseFilterEnum.Stop };
        Restyle(box, fill, Line, border, corner);
        return box;
    }

    private static void Restyle(Panel box, Color fill, Color edge, int border = 2, int corner = 2)
    {
        var look = new StyleBoxFlat { BgColor = fill, BorderColor = edge };
        look.SetBorderWidthAll(border);
        look.SetCornerRadiusAll(corner);
        box.AddThemeStyleboxOverride("panel", look);
    }
}
