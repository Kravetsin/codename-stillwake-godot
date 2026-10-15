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
/// The crafts, docked beside the open inventory (Tab): a row of pinned crafts, the tabs, and a tab's grid
/// where every craft has a place that never moves, one not known yet a dark silhouette in it, one short
/// of items dimmed. A click queues one, a shift-click five, a click on a short one goes to what is
/// missing; a right-click pins. Under the grid this player's queue; a click on an entry cancels it.
/// What a craft costs shows in the one tooltip. Read on a timer, and at once after a click.
/// </summary>
public partial class CraftsPanel : CanvasLayer
{
    /// <summary>What the panel shows is read this often.</summary>
    private const double RefreshMs = 100;
    private const int Gap = 8, Slot = 36, Small = 32;
    /// <summary>One beat of the pulse on a craft gone to, in seconds, and how many beats.</summary>
    private const double Beat = 0.9;
    private const int Beats = 2;
    /// <summary>Where the pins are kept between runs.</summary>
    private const string PinsFile = "user://crafts.cfg";

    private static readonly StyleBoxFlat Filled = Style.Box(Style.Slate800 with { A = 0.85f }, Style.Slate700, radius: 3);
    private static readonly StyleBoxFlat FilledHover = Style.Box(Style.Slate800 with { A = 0.85f }, Style.Slate400, radius: 3);
    private static readonly StyleBoxFlat Dark = Style.Box(Style.Slate900 with { A = 0.9f }, Style.Slate800, radius: 3);
    private static readonly StyleBoxFlat DarkHover = Style.Box(Style.Slate900 with { A = 0.9f }, Style.Slate400, radius: 3);
    private static readonly StyleBoxFlat Empty = Style.Box(Style.Slate800 with { A = 0.35f }, Style.Slate700 with { A = 0.45f }, radius: 3);
    /// <summary>The craft gone to: an amber edge and a ring spreading from it, redrawn every frame of the pulse.</summary>
    private static readonly StyleBoxFlat Pulse = Style.Box(Style.Slate800 with { A = 0.85f }, Style.Amber400, radius: 3);
    private static readonly StyleBoxFlat TabOff = TabBox(Style.Slate800 with { A = 0.55f }, Style.Slate800);
    private static readonly StyleBoxFlat TabOn = TabBox(Style.Slate700, Style.Slate600);
    private static readonly StyleBoxFlat PinDot = Style.Box(Style.Sky400, Style.Sky400, radius: 2);
    private static readonly LabelSettings HeadingText = Style.Text(Style.Bold, 11, Style.Slate400);
    private static readonly LabelSettings AsideText = Style.Text(Style.Sans, 11, Style.Slate500);
    private static readonly LabelSettings Count = Style.Text(Style.Mono, 10, Colors.White, shadow: true);

    /// <summary>A craft short of items: its icon grey and faint.</summary>
    private static readonly ShaderMaterial Greyed = Shaded("""
        shader_type canvas_item;
        void fragment() {
            vec4 c = texture(TEXTURE, UV) * COLOR;
            COLOR = vec4(mix(c.rgb, vec3(dot(c.rgb, vec3(0.299, 0.587, 0.114))), 0.6), c.a * 0.38);
        }
        """);

    /// <summary>A craft not known yet: its icon's shape alone, dark grey.</summary>
    private static readonly ShaderMaterial Silhouette = Shaded("""
        shader_type canvas_item;
        void fragment() {
            COLOR = vec4(vec3(0.16), texture(TEXTURE, UV).a * COLOR.a * 0.9);
        }
        """);

    private readonly Ui _ui;
    private readonly VBoxContainer _column = Style.Gap(new VBoxContainer(), 6);
    private readonly HBoxContainer _pins = Style.Gap(new HBoxContainer(), 4);
    private readonly HBoxContainer _tabs = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Stop }, 4);
    private readonly Label _tabName = new() { LabelSettings = HeadingText }, _opened = new() { LabelSettings = AsideText };
    private readonly ScrollContainer _scroll = new()
    {
        HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        CustomMinimumSize = new Vector2(0, Crafts.RowsShown * Slot + (Crafts.RowsShown - 1) * 4),
    };
    private readonly GridContainer _grid = Style.Gap(new GridContainer { Columns = Crafts.Columns }, 4);
    private readonly HBoxContainer _queue = Style.Gap(new HBoxContainer(), 4);
    private PanelContainer _panel = null!;
    /// <summary>The slots, the queue's entries and the tabs, each saying what it is in the one tooltip.</summary>
    private readonly TipSpots _tips;
    /// <summary>Every slot that answers the pointer: its edge lights under it.</summary>
    private readonly List<Cell> _cells = [];
    private Cell? _hovered;
    private Vector2 _mouse;
    private ProgressBar? _bar;

    private Client? _client;
    private CraftingModule? _crafting;
    private IResearchQueries? _research;
    private IItemsQueries? _items;
    private IReadOnlyList<(string Item, int Count)> _carried = [];
    private bool _creative;
    private double _sinceRead = RefreshMs;
    /// <summary>What the slots and the queue were built from: rebuilt only when it changes.</summary>
    private string? _shownKey;

    private int _tab;
    private IReadOnlyList<string> _pinned = [];
    /// <summary>The craft a click went to, and how long it has pulsed.</summary>
    private (string Craft, double Time)? _pulse;

    /// <param name="ui">Where the open inventory ends, which the panel docks beside.</param>
    public CraftsPanel(Ui ui)
    {
        _ui = ui;
        _tips = new TipSpots(ui.Tip);
    }

    /// <summary>A slot that answers the pointer, and the box it is drawn with when the pointer is elsewhere.</summary>
    private sealed record Cell(Panel Box, StyleBoxFlat Rest, StyleBoxFlat Hover, string? Craft);

    public override void _Ready()
    {
        Layer = ScreenLayers.Hud;
        var kept = new ConfigFile();
        if (kept.Load(PinsFile) == Error.Ok) _pinned = Crafts.Loaded(kept.GetValue("crafts", "pinned", new string[0]).AsStringArray());

        _column.AddChild(Section(Heading(T("crafts.pinned"), T("crafts.pinHow")), _pins));
        var tabs = new VBoxContainer();
        tabs.AddThemeConstantOverride("separation", 0);
        tabs.AddChild(_tabs);
        tabs.AddChild(new ColorRect { Color = Style.Slate600, CustomMinimumSize = new Vector2(0, 1), MouseFilter = Control.MouseFilterEnum.Ignore });
        _column.AddChild(tabs);
        _tabs.GuiInput += WheelOverTabs;
        _scroll.AddChild(_grid);
        _column.AddChild(Section(Heading(_tabName, _opened), _scroll));
        _column.AddChild(Section(Heading(T("crafts.queue"), ""), _queue));
        _panel = Style.Panel(Style.Slate700, _column);
        _panel.Visible = false;
        AddChild(_panel);
        _ui.InventoryEdge.Subscribe(Dock);
    }

    /// <summary>The pointer, from its events, so a recording's own events move it too.</summary>
    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse) _mouse = mouse.Position;
    }

    public override void _Process(double delta)
    {
        // A slot rebuilt under a still pointer gets no mouse_entered: hover and the tip are found by a hit test.
        var pointer = _ui.MenuOpen.Get() ? (Vector2?)null : _mouse;
        _tips.Frame(pointer);
        var hovered = pointer is { } at ? _cells.FindLast(c => c.Box.IsVisibleInTree() && c.Box.GetGlobalRect().HasPoint(at)) : null;
        if (hovered != _hovered)
        {
            var was = _hovered;
            _hovered = hovered;
            if (was != null && GodotObject.IsInstanceValid(was.Box)) Paint(was);
            if (hovered != null) Paint(hovered);
        }
        if (_pulse is var (craft, time))
        {
            _pulse = time + delta < Beat * Beats ? (craft, time + delta) : null;
            foreach (var cell in _cells.Where(c => c.Craft == craft && c.Box.GetParent() == _grid)) Paint(cell);
        }
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
        _carried = _items.Totals(peer);
        _creative = _items.Creative;
        var queue = _crafting.Queue(peer);
        string key = $"{_tab}|{string.Join(",", _pinned)}|"
            + string.Join(",", Content.Crafts.All.Select(c => (int)LookOf(c.Item)))
            + "|" + string.Join(";", queue.Select(e => $"{e.Id}:{e.Recipe}:{e.Count}"));
        if (key != _shownKey)
        {
            _shownKey = key;
            Build(queue);
        }
        if (_bar != null && queue.Count > 0) _bar.Value = Crafts.Share(queue[0].Recipe, _crafting.Progress(peer));
    }

    private Look LookOf(string craft) => Crafts.LookOf(_research!.Knows(craft), Crafts.Lines(craft, _carried), _creative);

    private void Send(Sim.Command command)
    {
        _client?.Send(command);
        Read();
    }

    private void Build(IReadOnlyList<Entry> queue)
    {
        foreach (var box in (Container[])[_pins, _tabs, _grid, _queue])
            foreach (var old in box.GetChildren())
            {
                box.RemoveChild(old);
                old.QueueFree();
            }
        (_bar, _hovered) = (null, null);
        _cells.Clear();
        _tips.Clear();

        for (int i = 0; i < Crafts.PinMost; i++)
            _pins.AddChild(i < _pinned.Count ? CraftSlot(_pinned[i], Small) : EmptySlot(Small));

        for (int i = 0; i < Crafts.Tabs.Count; i++) _tabs.AddChild(TabButton(i));

        var tab = Crafts.Tabs[_tab];
        for (int row = 0; row < Crafts.RowsOf(tab); row++)
        {
            var crafts = row < tab.Rows.Count ? tab.Rows[row] : [];
            for (int column = 0; column < Crafts.Columns; column++)
                _grid.AddChild(column < crafts.Count ? CraftSlot(crafts[column], Slot) : EmptySlot(Slot));
        }
        var placed = tab.Rows.SelectMany(r => r).ToList();
        _tabName.Text = T($"crafts.tab.{tab.Id}");
        _opened.Text = T("crafts.opened", ("n", placed.Count(_research!.Knows)), ("all", placed.Count));

        for (int i = 0; i < Content.Crafting.QueueMost; i++)
            _queue.AddChild(i < queue.Count ? EntrySlot(queue[i], i == 0) : EmptySlot(Small));
        _panel.ResetSize();
    }

    /// <summary>A craft in its place: as it looks now, its cost in the tip; a click queues it or goes to what it is short of, a right-click pins it.</summary>
    private Panel CraftSlot(string craft, int size)
    {
        var look = LookOf(craft);
        var box = Box(size);
        var icon = Icon(craft, size - 8);
        icon.Material = look switch { Look.Short => Greyed, Look.Locked => Silhouette, _ => null };
        box.AddChild(icon);
        if (_pinned.Contains(craft))
        {
            var dot = new Panel { Size = new Vector2(4, 4), Position = new Vector2(size - 6, 2), MouseFilter = Control.MouseFilterEnum.Ignore };
            dot.AddThemeStyleboxOverride("panel", PinDot);
            box.AddChild(dot);
        }
        Add(new Cell(box, look == Look.Locked ? Dark : Filled, look == Look.Locked ? DarkHover : FilledHover, craft));
        _tips.Add(box, () => TipOf(craft));
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right } press) return;
            // Accepted first: what the press does rebuilds the slots, this one gone from the tree.
            box.AcceptEvent();
            if (press.ButtonIndex == MouseButton.Left) Pressed(craft, press.ShiftPressed);
            else Pin(craft);
        };
        return box;
    }

    private void Pressed(string craft, bool shift)
    {
        switch (LookOf(craft))
        {
            case Look.Ready:
                Send(new Craft(craft, Crafts.CountFor(shift)));
                break;
            case Look.Short when Crafts.MissingCraft(Crafts.Lines(craft, _carried), _creative) is { } missing:
                GoTo(missing);
                break;
        }
    }

    /// <summary>Shows <paramref name="craft"/>'s tab and pulses its slot amber.</summary>
    private void GoTo(string craft)
    {
        if (Crafts.SpotOf(craft) is not { } spot) return;
        _tab = spot.Tab;
        _pulse = (craft, 0);
        Read();
        if (_cells.FirstOrDefault(c => c.Craft == craft && c.Box.GetParent() == _grid) is { } cell)
            Callable.From(() => _scroll.EnsureControlVisible(cell.Box)).CallDeferred();
    }

    private void Pin(string craft)
    {
        var pinned = Crafts.Toggle(_pinned, craft, _research?.Knows(craft) == true);
        if (pinned == _pinned) return;
        _pinned = pinned;
        var kept = new ConfigFile();
        kept.SetValue("crafts", "pinned", _pinned.ToArray());
        kept.Save(PinsFile);
        Read();
    }

    /// <summary>A craft's tip: its cost against what is carried, where a short raw item comes from, its time; a closed one, what opens it.</summary>
    private Tip TipOf(string craft)
    {
        if (_research?.Knows(craft) != true) return new Tip("???", new TipLine(T("crafts.notOpened"), Tone.Unknown));
        var lines = new List<TipLine>();
        var cost = Crafts.Lines(craft, _carried);
        foreach (var line in cost)
            lines.Add(new TipLine($"{line.Need} {T($"item.{line.Item}")} ({line.Have})", Crafts.Short(line, _creative) ? Tone.Bad : Tone.Good, line.Item));
        foreach (var line in cost.Where(l => Crafts.Short(l, _creative) && Crafts.Raw(l.Item)))
            lines.Add(new TipLine($"{T($"item.{line.Item}")}: {T($"crafts.from.{line.Item}")}", Tone.Dim));
        lines.Add(new TipLine(T("time.seconds", ("n", Crafts.Seconds(craft))), Tone.Dim));
        if (Crafts.MissingCraft(cost, _creative) != null) lines.Add(new TipLine(T("crafts.goToMissing"), Tone.Dim));
        return new Tip(T($"item.{craft}"), lines);
    }

    /// <summary>A queued craft: the item and how many are left, the one at work with its bar along the bottom. A click cancels it.</summary>
    private Panel EntrySlot(Entry entry, bool atWork)
    {
        var box = Box(Small);
        box.AddChild(Icon(entry.Recipe, Small - 8));
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
        if (atWork)
        {
            _bar = new ProgressBar
            {
                ShowPercentage = false,
                MaxValue = 1,
                Position = new Vector2(2, Small - 5),
                Size = new Vector2(Small - 4, 3),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            _bar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = Style.Slate700 });
            _bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = Style.Amber400 });
            box.AddChild(_bar);
        }
        Add(new Cell(box, Filled, FilledHover, null));
        var tip = new Tip(T($"item.{entry.Recipe}"));
        _tips.Add(box, () => tip);
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            box.AcceptEvent();
            Send(new Cancel(entry.Id));
        };
        return box;
    }

    /// <summary>A tab's button, its item's icon, lit while it is the one shown; a click shows it.</summary>
    private Panel TabButton(int tab)
    {
        bool on = tab == _tab;
        var button = new Panel { CustomMinimumSize = new Vector2(40, 32), MouseFilter = Control.MouseFilterEnum.Pass };
        button.AddThemeStyleboxOverride("panel", on ? TabOn : TabOff);
        var icon = Icon(Crafts.Tabs[tab].Icon, 24);
        icon.Position = new Vector2(8, 4);
        icon.Modulate = new Color(1, 1, 1, on ? 1 : 0.65f);
        button.AddChild(icon);
        var tip = new Tip(T($"crafts.tab.{Crafts.Tabs[tab].Id}"), new TipLine(T("crafts.tabWheel"), Tone.Dim));
        _tips.Add(button, () => tip);
        button.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            button.AcceptEvent();
            Show(tab);
        };
        return button;
    }

    private void WheelOverTabs(InputEvent e)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown or MouseButton.WheelUp } wheel) return;
        _tabs.AcceptEvent();
        Show(Crafts.Step(_tab, wheel.ButtonIndex == MouseButton.WheelDown ? 1 : -1));
    }

    private void Show(int tab)
    {
        if (tab == _tab) return;
        _tab = tab;
        _scroll.ScrollVertical = 0;
        Read();
    }

    private void Add(Cell cell)
    {
        _cells.Add(cell);
        Paint(cell);
    }

    /// <summary>A slot's edge: amber and ringed while it pulses, light under the pointer.</summary>
    private void Paint(Cell cell)
    {
        if (_pulse is var (craft, time) && cell.Craft == craft && cell.Box.GetParent() == _grid)
        {
            // box-shadow 0 0 0 0 → 0 0 0 9px, amber fading out, ease-out: the mockup's .slot.pulse.
            float t = (float)(time % Beat / Beat), eased = 1 - (1 - t) * (1 - t);
            (Pulse.ShadowSize, Pulse.ShadowColor) = ((int)(9 * eased), Style.Amber400 with { A = 0.9f * (1 - eased) });
            cell.Box.AddThemeStyleboxOverride("panel", Pulse);
            return;
        }
        cell.Box.AddThemeStyleboxOverride("panel", cell == _hovered ? cell.Hover : cell.Rest);
    }

    private static Panel Box(int size) => new()
    {
        CustomMinimumSize = new Vector2(size, size),
        // Pass, not Stop: the wheel over a slot still reaches the grid's scroll.
        MouseFilter = Control.MouseFilterEnum.Pass,
    };

    private static Panel EmptySlot(int size)
    {
        var box = new Panel { CustomMinimumSize = new Vector2(size, size), MouseFilter = Control.MouseFilterEnum.Ignore };
        box.AddThemeStyleboxOverride("panel", Empty);
        return box;
    }

    /// <summary>A heading over a section: its name, and on the right a word in grey.</summary>
    private static HBoxContainer Heading(string name, string aside) =>
        Heading(new Label { Text = name, LabelSettings = HeadingText }, new Label { Text = aside, LabelSettings = AsideText });

    private static HBoxContainer Heading(Label name, Label aside)
    {
        var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 8);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(name);
        row.AddChild(aside);
        return row;
    }

    private static VBoxContainer Section(Control heading, Control body)
    {
        var section = Style.Gap(new VBoxContainer(), 4);
        section.AddChild(heading);
        section.AddChild(body);
        return section;
    }

    /// <summary>A tab's box: rounded at the top, open at the bottom, where the line under the tabs runs.</summary>
    private static StyleBoxFlat TabBox(Color bg, Color border)
    {
        var box = Style.Box(bg, border, radius: 3);
        box.BorderWidthBottom = 0;
        (box.CornerRadiusBottomLeft, box.CornerRadiusBottomRight) = (0, 0);
        return box;
    }

    private static ShaderMaterial Shaded(string code) => new() { Shader = new Shader { Code = code } };

    private static TextureRect Icon(string item, int size) => new()
    {
        Texture = ItemIcons.Icon(item),
        Position = new Vector2(4, 4),
        Size = new Vector2(size, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
}
