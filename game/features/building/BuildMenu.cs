using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// The plan in hand (features/building/BuildMenu.tsx, 0022), taken out with B and shown the whole while it
/// is out: the build menu's tabs of what can be built, each card a picture of what it builds, its name,
/// cost and use in a tooltip over it, so a card is swapped for another at a click. A plan costs nothing
/// until it is delivered to, so a card the player carries too little for is as bright as any. Taken out
/// while an overlay is on, it opens on that overlay's tab. Under the cards, the card in hand and its
/// buttons. B or Esc puts the plan away; with it away, the key that takes it out. Only what the world
/// knows is shown (0024): a tab with nothing known, not at all.
/// </summary>
public partial class BuildMenu : CanvasLayer
{
    /// <summary>The inventory's counts are read this often while the menu is open: a panel reads on a timer.</summary>
    private const double RefreshMs = 250;

    private static readonly Color Slate950 = new("020617"), Slate900 = new("0f172a"), Slate800 = new("1e293b"),
        Slate700 = new("334155"), Slate600 = new("475569"), Slate500 = new("64748b"), Slate400 = new("94a3b8"),
        Slate300 = new("cbd5e1"), Slate100 = new("f1f5f9"), Sky500 = new("0ea5e9"), Sky700 = new("0369a1"),
        Sky950 = new("082f49"), Red400 = new("f87171");

    private static readonly Font Mono = new SystemFont { FontNames = ["Consolas", "Menlo", "monospace"] };

    private readonly PlanHand _hand;
    private readonly Ui _ui;
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly PanelContainer _closed, _open, _tooltip;
    private readonly HBoxContainer _tabs = new(), _held = new();
    private readonly HFlowContainer _cards = new();
    private readonly VBoxContainer _tip = new();
    private string _tab = Tools.Tabs[0].Id;
    private Tool? _hovered;
    private IReadOnlyList<(string Item, int Count)> _carried = [];
    private double _sinceRead = RefreshMs;
    private string _shown = "", _tipShown = "";
    private bool _wasOpen;
    private readonly Dictionary<Tool, Button> _buttons = new();

    public BuildMenu(PlanHand hand, Ui ui)
    {
        _hand = hand;
        _ui = ui;
        Layer = 10;
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.Theme = new Theme { DefaultFont = Mono, DefaultFontSize = 12 };
        AddChild(_root);

        _closed = Panel(Slate900 with { A = 0.9f }, Slate700, 4, Text(En.T("build.open"), Slate400));
        _closed.MouseFilter = Control.MouseFilterEnum.Ignore;
        BottomCentre(_closed);

        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 8);
        _tabs.AddThemeConstantOverride("separation", 4);
        _cards.AddThemeConstantOverride("h_separation", 4);
        _cards.AddThemeConstantOverride("v_separation", 4);
        _held.AddThemeConstantOverride("separation", 8);
        _held.CustomMinimumSize = new Vector2(0, 20);
        body.AddChild(_tabs);
        body.AddChild(_cards);
        body.AddChild(_held);
        _open = Panel(Slate900 with { A = 0.95f }, Slate700, 8, body);
        _open.CustomMinimumSize = new Vector2(448, 0);
        BottomCentre(_open);

        _tip.AddThemeConstantOverride("separation", 4);
        _tooltip = Panel(Slate950, Slate600, 6, _tip);
        _tooltip.MouseFilter = Control.MouseFilterEnum.Ignore;
        _tooltip.CustomMinimumSize = new Vector2(224, 0);
        _tooltip.Visible = false;
        _root.AddChild(_tooltip);
    }

    private void BottomCentre(Control panel)
    {
        panel.AnchorLeft = panel.AnchorRight = 0.5f;
        panel.AnchorTop = panel.AnchorBottom = 1;
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Begin;
        panel.OffsetBottom = -12;
        _root.AddChild(panel);
    }

    private static PanelContainer Panel(Color bg, Color border, int padding, Control child)
    {
        var box = new StyleBoxFlat { BgColor = bg, BorderColor = border };
        box.SetBorderWidthAll(1);
        box.SetCornerRadiusAll(4);
        box.SetContentMarginAll(padding);
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        panel.AddThemeStyleboxOverride("panel", box);
        panel.AddChild(child);
        return panel;
    }

    private static Label Text(string text, Color colour, int size = 12) => new()
    {
        Text = text,
        LabelSettings = new LabelSettings { Font = Mono, FontSize = size, FontColor = colour },
        MouseFilter = Control.MouseFilterEnum.Ignore,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static TextureRect Picture(Texture2D texture, int size) => new()
    {
        Texture = texture,
        CustomMinimumSize = new Vector2(size, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    /// <summary>A card's picture: what it builds, as the ship draws it; the cable its plug.</summary>
    public static Texture2D ThumbOf(Tool tool) => tool is PlaceTool p ? BlueprintArt.Thumbnail(p.Placement) : AsciiArt.Plug();

    /// <summary>A card clicked: picked, or let go when it is the one in hand. The menu stays open.</summary>
    public void Pick(Tool tool) => _hand.Selected.Set(tool == _hand.Selected.Get() ? null : tool);

    /// <summary>The pointer over a card, or off every card: its tooltip shown.</summary>
    public void Hover(Tool? tool) => _hovered = tool;

    /// <summary>The tab shown.</summary>
    public void ShowTab(string id) => _tab = id;

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true } key) return;
        if (key.PhysicalKeycode == Key.B && !key.Echo) _hand.Toggle();
        else if (key.PhysicalKeycode == Key.Escape) _hand.PutPlanAway();
        else return;
        GetViewport().SetInputAsHandled();
    }

    /// <summary>One frame: what the world knows and what peer <paramref name="peer"/> carries, read on a timer while open.</summary>
    public void Frame(Registry m, int peer, double elapsedMs)
    {
        bool open = _hand.PlanOut.Get();
        _closed.Visible = !open;
        _open.Visible = open;
        if (!open)
        {
            _tooltip.Visible = false;
            _wasOpen = false;
            _hovered = null;
            return;
        }
        if (!_wasOpen)
        {
            // Taken out while an overlay is on, it opens on that overlay's tab.
            if (Tools.TabFor(_ui.OverlayOn.Get()) is { } opensOn) _tab = opensOn;
            _sinceRead = RefreshMs;
            _wasOpen = true;
        }
        if ((_sinceRead += elapsedMs) >= RefreshMs)
        {
            _sinceRead = 0;
            _carried = m.Items.Totals(peer);
        }
        var tabs = Tools.Tabs
            .Select(t => new Tab(t.Id, t.Tools.Where(each => Tools.Known(each, m.Research)).ToList()))
            .Where(t => t.Tools.Count > 0)
            .ToList();
        var shownTab = tabs.FirstOrDefault(t => t.Id == _tab) ?? tabs.FirstOrDefault();
        var tool = _hand.Selected.Get();
        bool wiring = _ui.OverlayOn.Get() == Tools.Power;
        string key = $"{string.Join(",", tabs.Select(t => $"{t.Id}:{t.Tools.Count}"))}|{shownTab?.Id}|{tool?.Id}|{wiring}";
        if (key != _shown)
        {
            _shown = key;
            Rebuild(tabs, shownTab, tool, wiring);
        }
        ShowTooltip(m);
    }

    private void Rebuild(List<Tab> tabs, Tab? shownTab, Tool? tool, bool wiring)
    {
        foreach (var child in _tabs.GetChildren().Concat(_cards.GetChildren()).Concat(_held.GetChildren()))
        {
            child.GetParent().RemoveChild(child);
            child.QueueFree();
        }
        _buttons.Clear();
        foreach (var tab in tabs)
        {
            bool on = tab.Id == shownTab?.Id;
            var button = new Button { Text = En.T($"build.tab.{tab.Id}"), FocusMode = Control.FocusModeEnum.None };
            Style(button, on ? Sky700 : Slate800, on ? Sky700 : Slate800, on ? Colors.White : Slate300, padding: 4);
            string id = tab.Id;
            button.Pressed += () => _tab = id;
            _tabs.AddChild(button);
        }
        foreach (var each in shownTab?.Tools ?? [])
        {
            bool held = each == tool;
            var card = new Button { CustomMinimumSize = new Vector2(48, 48), FocusMode = Control.FocusModeEnum.None, TooltipText = "" };
            Style(card, held ? Sky950 : Slate800 with { A = 0.8f }, held ? Sky500 : Slate700, Slate300, padding: 4);
            var thumb = Picture(ThumbOf(each), 36);
            thumb.Size = new Vector2(36, 36);
            thumb.Position = new Vector2(6, 6);
            card.AddChild(thumb);
            var picked = each;
            card.Pressed += () => Pick(picked);
            card.MouseEntered += () => Hover(picked);
            card.MouseExited += () => { if (_hovered == picked) Hover(null); };
            _cards.AddChild(card);
            _buttons[each] = card;
        }
        if (tool != null)
        {
            _held.AddChild(Picture(ThumbOf(tool), 20));
            _held.AddChild(Text(tool.Label, Slate100));
            _held.AddChild(Text(En.T(tool is CableTool ? "hand.cable" : "hand.card"), Slate500));
        }
        else _held.AddChild(Text(En.T(wiring ? "hand.wires" : "build.close"), Slate500));
        _tipShown = "";
    }

    private static void Style(Button button, Color bg, Color border, Color text, int padding)
    {
        foreach (var state in (string[])["normal", "hover", "pressed", "focus"])
        {
            var box = new StyleBoxFlat { BgColor = state == "hover" && bg != Sky700 && bg != Sky950 ? Slate700 : bg, BorderColor = border };
            box.SetBorderWidthAll(1);
            box.SetCornerRadiusAll(4);
            box.SetContentMarginAll(padding);
            button.AddThemeStyleboxOverride(state, box);
        }
        foreach (var colour in (string[])["font_color", "font_hover_color", "font_pressed_color"]) button.AddThemeColorOverride(colour, text);
    }

    /// <summary>The tooltip over the card the pointer is on: its name, cost against what is carried, and what it does.</summary>
    private void ShowTooltip(Registry m)
    {
        if (_hovered is not { } tool || !_buttons.TryGetValue(tool, out var card))
        {
            _tooltip.Visible = false;
            return;
        }
        var lines = CostLine.Of(Tools.CostOf(tool, m.Building.CostFor), _carried);
        string key = $"{tool.Id}|{string.Join(",", lines)}";
        if (key != _tipShown)
        {
            _tipShown = key;
            foreach (var child in _tip.GetChildren())
            {
                _tip.RemoveChild(child);
                child.QueueFree();
            }
            _tip.AddChild(Text(tool.Label, Slate100));
            var cost = new HFlowContainer();
            cost.AddThemeConstantOverride("h_separation", 8);
            foreach (var line in lines)
            {
                var item = new HBoxContainer();
                item.AddThemeConstantOverride("separation", 2);
                item.AddChild(Picture(PixelArt.Picture($"items/{line.Item}"), 16));
                item.AddChild(Text($"{line.Have}/{line.Need}", line.Have < line.Need ? Red400 : Slate300, 10));
                cost.AddChild(item);
            }
            _tip.AddChild(cost);
            var about = Text(En.T($"tool.{tool.Id}.about"), Slate400);
            about.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            about.CustomMinimumSize = new Vector2(212, 0);
            _tip.AddChild(about);
            _tooltip.Size = _tooltip.GetCombinedMinimumSize();
        }
        _tooltip.Visible = true;
        // Over the card, centred on it.
        var rect = card.GetGlobalRect();
        var size = _tooltip.GetCombinedMinimumSize();
        _tooltip.Position = new Vector2(rect.Position.X + rect.Size.X / 2 - size.X / 2, rect.Position.Y - size.Y - 4);
    }
}
