using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Crafting;

/// <summary>
/// The crafts, docked beside the open inventory (Tab): each known craft with what a unit costs, what is
/// short in red; a click queues one, a shift-click five. Under them this player's queue, the unit at
/// work with its bar; a click on an entry cancels it. Read on a timer, and at once after a click.
/// </summary>
public partial class CraftsPanel : CanvasLayer
{
    /// <summary>What the panel shows is read this often.</summary>
    private const double RefreshMs = 100;
    private const int Gap = 8;

    private static readonly StyleBoxFlat Card = Style.Box(Style.Slate800 with { A = 0.9f }, Style.Slate600, 4);
    private static readonly StyleBoxFlat CardHover = Style.Box(Style.Slate700, Style.Amber300, 4);
    private static readonly StyleBoxFlat Entry = Style.Box(Style.Slate800 with { A = 0.9f }, Style.Slate600);
    private static readonly StyleBoxFlat EntryHover = Style.Box(Style.Slate700, Style.Red300);
    private static readonly LabelSettings Have = Style.Text(Style.Mono, 10, Style.Slate300);
    private static readonly LabelSettings Lacking = Style.Text(Style.Mono, 10, Style.Red300);
    private static readonly LabelSettings Count = Style.Text(Style.Mono, 10, Colors.White, shadow: true);

    private readonly Ui _ui;
    private readonly VBoxContainer _column = Style.Gap(new VBoxContainer(), 6);
    private readonly GridContainer _cards = Style.Gap(new GridContainer { Columns = 2 }, 4);
    private readonly HBoxContainer _queue = Style.Gap(new HBoxContainer(), 4);
    private PanelContainer _panel = null!;
    private ProgressBar? _bar;

    private Client? _client;
    private CraftingModule? _crafting;
    private IResearchQueries? _research;
    private IItemsQueries? _items;
    private double _sinceRead = RefreshMs;
    /// <summary>What the cards and the queue were built from: rebuilt only when it changes.</summary>
    private string? _shownKey;

    /// <param name="ui">Where the open inventory ends, which the panel docks beside.</param>
    public CraftsPanel(Ui ui) => _ui = ui;

    /// <summary>The crafts shown, by the item each makes: for scripted runs.</summary>
    public IReadOnlyList<string> Showing { get; private set; } = [];

    public override void _Ready()
    {
        Layer = ScreenLayers.Hud;
        _column.AddChild(_cards);
        _column.AddChild(_queue);
        _panel = Style.Panel(Style.Slate700, _column);
        _panel.Visible = false;
        AddChild(_panel);
        _ui.InventoryEdge.Subscribe(Dock);
    }

    /// <summary>Reads the client's world on a timer, through the current registry: a load or a resync brings new modules.</summary>
    public void Frame(Client client, CraftingModule crafting, IResearchQueries research, IItemsQueries items, double elapsedMs)
    {
        bool fresh = _crafting != crafting || _client != client;
        (_client, _crafting, _research, _items) = (client, crafting, research, items);
        _sinceRead += elapsedMs;
        if (fresh || _sinceRead >= RefreshMs) Read();
    }

    private void Dock()
    {
        var edge = _ui.InventoryEdge.Get();
        _panel.Visible = edge != null;
        if (edge is var (right, top)) _panel.Position = new Vector2(right + Gap, top);
        if (edge != null) Read();
    }

    private void Read()
    {
        _sinceRead = 0;
        if (!_panel.Visible || _client == null || _crafting == null || _research == null || _items == null) return;
        int peer = _client.Id;
        var crafts = Crafts.Shown(_research);
        var carried = _items.Totals(peer);
        bool creative = _items.Creative;
        var queue = _crafting.Queue(peer);
        string key = string.Join(";", crafts.Select(c => string.Join(",", Crafts.Lines(c, carried).Select(l => Crafts.Short(l, creative) ? 1 : 0))))
            + "|" + string.Join(";", queue.Select(e => $"{e.Id}:{e.Recipe}:{e.Count}"));
        if (key != _shownKey)
        {
            _shownKey = key;
            Build(crafts, carried, creative, queue);
        }
        if (_bar != null && queue.Count > 0) _bar.Value = Crafts.Share(queue[0].Recipe, _crafting.Progress(peer));
        Showing = crafts.Select(c => c.Id).ToList();
    }

    private void Send(Sim.Command command)
    {
        _client?.Send(command);
        Read();
    }

    private void Build(IReadOnlyList<Recipe> crafts, IReadOnlyList<(string Item, int Count)> carried, bool creative, IReadOnlyList<Entry> queue)
    {
        foreach (var old in _cards.GetChildren()) old.QueueFree();
        foreach (var old in _queue.GetChildren()) old.QueueFree();
        _bar = null;
        foreach (var craft in crafts) _cards.AddChild(CardOf(craft, Crafts.Lines(craft, carried), creative));
        for (int i = 0; i < queue.Count; i++) _queue.AddChild(EntryOf(queue[i], i == 0));
        _queue.Visible = queue.Count > 0;
        _panel.ResetSize();
    }

    /// <summary>A craft: the item it makes, and beside it each item a unit takes, what is carried of it against what is needed.</summary>
    private PanelContainer CardOf(Recipe craft, IReadOnlyList<CostLine> lines, bool creative)
    {
        var card = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop, TooltipText = T($"item.{craft.Id}") };
        card.AddThemeStyleboxOverride("panel", Card);
        var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
        row.AddChild(Icon(craft.Id, 32));
        var cost = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center }, 2);
        foreach (var line in lines)
        {
            var part = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 3);
            part.AddChild(Icon(line.Item, 14));
            part.AddChild(new Label
            {
                Text = creative ? $"{line.Need}" : $"{line.Have}/{line.Need}",
                LabelSettings = Crafts.Short(line, creative) ? Lacking : Have,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            });
            cost.AddChild(part);
        }
        row.AddChild(cost);
        card.AddChild(row);
        card.MouseEntered += () => card.AddThemeStyleboxOverride("panel", CardHover);
        card.MouseExited += () => card.AddThemeStyleboxOverride("panel", Card);
        card.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } press) return;
            card.AcceptEvent();
            Send(new Craft(craft.Id, Crafts.CountFor(press.ShiftPressed)));
        };
        return card;
    }

    /// <summary>A queued craft: the item and how many are left; the one at work with its bar under it. A click cancels it.</summary>
    private Control EntryOf(Entry entry, bool atWork)
    {
        var column = Style.Gap(new VBoxContainer(), 2);
        var box = new Panel { CustomMinimumSize = new Vector2(36, 36), MouseFilter = Control.MouseFilterEnum.Stop, TooltipText = T($"item.{entry.Recipe}") };
        box.AddThemeStyleboxOverride("panel", Entry);
        var icon = Icon(entry.Recipe, 28);
        box.AddChild(icon);
        icon.Position = new Vector2(4, 4);
        var count = new Label
        {
            Text = entry.Count > 1 ? entry.Count.ToString() : "",
            LabelSettings = Count,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        box.AddChild(count);
        count.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        (count.OffsetRight, count.OffsetBottom) = (-2, 2);
        box.MouseEntered += () => box.AddThemeStyleboxOverride("panel", EntryHover);
        box.MouseExited += () => box.AddThemeStyleboxOverride("panel", Entry);
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            box.AcceptEvent();
            Send(new Cancel(entry.Id));
        };
        column.AddChild(box);
        if (atWork) column.AddChild(_bar = Style.Bar(Style.Amber400, 36));
        return column;
    }

    private static TextureRect Icon(string item, int size) => new()
    {
        Texture = ItemIcons.Icon(item),
        CustomMinimumSize = new Vector2(size, size),
        Size = new Vector2(size, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
}
