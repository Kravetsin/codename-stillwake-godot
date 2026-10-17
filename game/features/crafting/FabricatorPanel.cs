using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Crafting;

/// <summary>
/// The lifepod's Fabricator, docked beside the inventory while E has the pod open (the mockup,
/// "Fabricator panel"): its name and its small screen, running while it works and READY idle; its
/// recipes in fixed places, a locked one a silhouette, a short one dimmed; the queue every hero shares,
/// the unit at work filling its slot from the bottom, each entry marked in its orderer's suit colour;
/// the outputs, a click taking a stack into the inventory; a tool's recipe tinted amber. A click on a
/// recipe queues one, a shift-click five, a click on a short one pulses what it lacks; a click on an
/// entry cancels it. Read on a timer, and at once after a click; everything else said in the one
/// tooltip, a recipe's as its item's card with the craft block.
/// </summary>
public partial class FabricatorPanel : CanvasLayer
{
    /// <summary>What the panel shows is read this often.</summary>
    private const double RefreshMs = 100;
    private const int Gap = 8, Slot = 36;
    /// <summary>One beat of the pulse on a recipe gone to, in seconds, and how many beats.</summary>
    private const double Beat = 0.9;
    private const int Beats = 2;

    private static readonly StyleBoxFlat Filled = Style.Box(Style.Slate800 with { A = 0.85f }, Style.Slate700, radius: 3);
    private static readonly StyleBoxFlat FilledHover = Style.Box(Style.Slate800 with { A = 0.85f }, Style.Slate400, radius: 3);
    private static readonly StyleBoxFlat Dark = Style.Box(Style.Slate900 with { A = 0.9f }, Style.Slate800, radius: 3);
    private static readonly StyleBoxFlat DarkHover = Style.Box(Style.Slate900 with { A = 0.9f }, Style.Slate400, radius: 3);
    // A tool's recipe: the ground mixed with amber, its edge amber-ish, amber under the pointer: the mockup's option B.
    private static readonly StyleBoxFlat ToolFilled = Style.Box((Style.Slate800 with { A = 0.85f }).Lerp(ItemCardView.Amber, 0.16f), Style.Slate700.Lerp(ItemCardView.Amber, 0.6f), radius: 3);
    private static readonly StyleBoxFlat ToolHover = Style.Box((Style.Slate800 with { A = 0.85f }).Lerp(ItemCardView.Amber, 0.16f), ItemCardView.Amber, radius: 3);
    private static readonly StyleBoxFlat Empty = Style.Box(Style.Slate800 with { A = 0.35f }, Style.Slate700 with { A = 0.45f }, radius: 3);
    private static readonly StyleBoxFlat Pulse = Style.Box(Style.Slate800 with { A = 0.85f }, Style.Amber400, radius: 3);
    private static readonly LabelSettings NameText = Style.Text(Style.Bold, 13, Style.Slate100);
    private static readonly LabelSettings HeadingText = Style.Text(Style.Bold, 11, Style.Slate400);
    private static readonly LabelSettings AsideText = Style.Text(Style.Sans, 11, Style.Slate500);
    private static readonly LabelSettings Count = Style.Text(Style.Mono, 10, Colors.White, shadow: true);

    /// <summary>A recipe short of items: its icon grey and faint.</summary>
    private static readonly ShaderMaterial Greyed = Shaded("""
        shader_type canvas_item;
        void fragment() {
            vec4 c = texture(TEXTURE, UV) * COLOR;
            COLOR = vec4(mix(c.rgb, vec3(dot(c.rgb, vec3(0.299, 0.587, 0.114))), 0.6), c.a * 0.38);
        }
        """);

    /// <summary>A recipe not known yet: its icon's shape alone, dark grey.</summary>
    private static readonly ShaderMaterial Silhouette = Shaded("""
        shader_type canvas_item;
        void fragment() {
            COLOR = vec4(vec3(0.16), texture(TEXTURE, UV).a * COLOR.a * 0.9);
        }
        """);

    private readonly Ui _ui;
    private readonly Func<int, Color> _suitOf;
    private readonly GridContainer _recipes = Grid(), _queue = Grid(), _outputs = Grid();
    private readonly Label _opened = new() { LabelSettings = AsideText };
    private readonly FabricatorScreen _screen = new() { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
    private PanelContainer _panel = null!;
    /// <summary>The slots, each saying what it is in the one tooltip.</summary>
    private readonly TipSpots _tips;
    /// <summary>Every slot that answers the pointer: its edge lights under it.</summary>
    private readonly List<Cell> _cells = [];
    private Cell? _hovered;
    private Vector2 _mouse;
    private ColorRect? _fill;

    private Client? _client;
    private CraftingModule? _crafting;
    private IResearchQueries? _research;
    private IItemsQueries? _items;
    private IGridQueries? _grid;
    private IReadOnlyList<(string Item, int Count)> _carried = [];
    private bool _creative;
    private double _sinceRead = RefreshMs;
    /// <summary>What the slots were built from: rebuilt only when it changes.</summary>
    private string? _shownKey;
    /// <summary>The recipe a click went to, and how long it has pulsed.</summary>
    private (string Craft, double Time)? _pulse;

    /// <param name="ui">Where the open inventory ends, which the panel docks beside, and the machine E opened.</param>
    /// <param name="suitOf">The suit colour of a peer's hero, marking whose order an entry is.</param>
    public FabricatorPanel(Ui ui, Func<int, Color> suitOf)
    {
        _ui = ui;
        _suitOf = suitOf;
        _tips = new TipSpots(ui.Tip);
    }

    /// <summary>A slot that answers the pointer, and the box it is drawn with when the pointer is elsewhere.</summary>
    private sealed record Cell(Panel Box, StyleBoxFlat Rest, StyleBoxFlat Hover, string? Craft);

    /// <summary>The pod open beside the inventory: the machine E opened, while it crafts and the inventory is shown.</summary>
    private int? Pod => _ui.InventoryEdge.Get() != null && _ui.OpenCrate.Get() is int id && _crafting?.Crafts(id) == true ? id : null;

    /// <summary>Whether the panel shows, and what its screen shows: what a recording checks.</summary>
    public (bool Shown, Doing Doing) Shown => (_panel.Visible, _screen.Doing);

    /// <summary>The middle of recipe <paramref name="craft"/>'s slot on the screen while the panel shows: where a recording points.</summary>
    public Vector2? CenterOf(string craft) =>
        _cells.FirstOrDefault(c => c.Craft == craft && c.Box.IsVisibleInTree())?.Box.GetGlobalRect().GetCenter();

    public override void _Ready()
    {
        Layer = ScreenLayers.Hud;
        var column = Style.Gap(new VBoxContainer(), 6);
        var head = Style.Gap(new HBoxContainer(), 8);
        var name = new Label { Text = T("fabricator.name"), LabelSettings = NameText, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
        head.AddChild(name);
        head.AddChild(_screen);
        column.AddChild(head);
        column.AddChild(new ColorRect { Color = Style.Slate800, CustomMinimumSize = new Vector2(0, 1), MouseFilter = Control.MouseFilterEnum.Ignore });
        column.AddChild(Heading(T("fabricator.recipes"), _opened));
        column.AddChild(_recipes);
        column.AddChild(Heading(T("crafts.queue"), new Label { Text = T("fabricator.shared"), LabelSettings = AsideText }));
        column.AddChild(_queue);
        column.AddChild(Heading(T("fabricator.output"), new Label()));
        column.AddChild(_outputs);
        _panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
        _panel.AddThemeStyleboxOverride("panel", Style.Box(Style.Slate900 with { A = 0.92f }, Style.Slate600 with { A = 0.55f }, 6));
        _panel.AddChild(column);
        AddChild(_panel);
        _ui.InventoryEdge.Subscribe(Dock);
        _ui.OpenCrate.Subscribe(Dock);
    }

    /// <summary>The pointer, from its events, so a recording's own events move it too.</summary>
    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse) _mouse = mouse.Position;
    }

    public override void _Process(double delta)
    {
        // A slot rebuilt under a still pointer gets no mouse_entered: hover and the tip are found by a hit test.
        var pointer = _ui.MenuOpen.Get() || !_panel.Visible ? (Vector2?)null : _mouse;
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
            foreach (var cell in _cells.Where(c => c.Craft == craft)) Paint(cell);
        }
    }

    /// <summary>Reads the client's world on a timer, through the current registry: a load or a resync brings new modules.</summary>
    public void Frame(Client client, CraftingModule crafting, IResearchQueries research, IItemsQueries items, IGridQueries grid, double elapsedMs)
    {
        bool fresh = _crafting != crafting || _client != client;
        (_client, _crafting, _research, _items, _grid) = (client, crafting, research, items, grid);
        _sinceRead += elapsedMs;
        if (fresh) Dock();
        else if (_sinceRead >= RefreshMs) Read();
    }

    private void Dock()
    {
        var edge = _ui.InventoryEdge.Get();
        bool shown = Pod != null;
        _panel.Visible = shown;
        if (shown && edge is var (right, top)) _panel.Position = new Vector2(right + Gap, top);
        if (!shown) _tips.Show(null);
        Read();
    }

    private void Read()
    {
        _sinceRead = 0;
        if (Pod is not int pod || _client == null || _crafting == null || _research == null || _items == null) return;
        int peer = _client.Id;
        _carried = _items.Totals(peer);
        _creative = _items.Creative;
        var queue = _crafting.Queue(pod);
        int progress = _crafting.Progress(pod);
        // The outputs as this peer's own takes in flight leave them, so a stack goes on the click (0014).
        var moves = _client.InFlight.Where(c => c is Move or Drop).ToList();
        var outputs = _items.AfterMoves(peer, moves).Crates.Where(c => c.Crate == pod).Select(c => c.Slots).FirstOrDefault() ?? _items.Crate(pod);
        string key = $"{pod}|" + string.Join(",", Content.Crafts.All.Select(c => (int)LookOf(c.Item)))
            + "|" + string.Join(";", queue.Select(e => $"{e.Id}:{e.Recipe}:{e.Count}:{e.By}"))
            + "|" + string.Join(";", outputs.Select(s => s == null ? "" : $"{s.Item}:{s.Count}"));
        if (key != _shownKey)
        {
            _shownKey = key;
            Build(pod, queue, outputs);
        }
        if (_fill != null && queue.Count > 0) Fill(_fill, Crafts.Share(queue[0].Recipe, progress));
        _screen.Doing = Crafts.DoingOf(queue, progress);
    }

    private Look LookOf(string craft) => Crafts.LookOf(_research!.Knows(craft), Crafts.Lines(craft, _carried), _creative);

    private void Send(Sim.Command command)
    {
        _client?.Send(command);
        Read();
    }

    private void Build(int pod, IReadOnlyList<Entry> queue, IReadOnlyList<Stack?> outputs)
    {
        foreach (var box in (Container[])[_recipes, _queue, _outputs])
            foreach (var old in box.GetChildren())
            {
                box.RemoveChild(old);
                old.QueueFree();
            }
        (_fill, _hovered) = (null, null);
        _cells.Clear();
        _tips.Clear();

        foreach (var place in Crafts.Places) _recipes.AddChild(place is { } craft ? RecipeSlot(pod, craft) : EmptySlot());
        int opened = Content.Crafts.All.Count(c => _research!.Knows(c.Item));
        _opened.Text = T("crafts.opened", ("n", opened), ("all", Content.Crafts.All.Count));
        for (int i = 0; i < Content.Crafting.QueueMost; i++)
            _queue.AddChild(i < queue.Count ? EntrySlot(pod, queue[i], i == 0) : EmptySlot());
        for (int i = 0; i < outputs.Count; i++)
            _outputs.AddChild(outputs[i] is { } stack ? OutputSlot(pod, i, stack) : EmptySlot());
        _panel.ResetSize();
    }

    /// <summary>A recipe in its place: as it looks now, its cost in the tip; a click queues it or goes to what it is short of.</summary>
    private Panel RecipeSlot(int pod, string craft)
    {
        var look = LookOf(craft);
        var box = Box();
        var icon = Icon(craft);
        icon.Material = look switch { Look.Short => Greyed, Look.Locked => Silhouette, _ => null };
        box.AddChild(icon);
        Add(look == Look.Locked ? new Cell(box, Dark, DarkHover, craft)
            : ItemCards.IsTool(craft) ? new Cell(box, ToolFilled, ToolHover, craft)
            : new Cell(box, Filled, FilledHover, craft));
        _tips.Add(box, () => TipOf(craft));
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } press) return;
            // Accepted first: what the press does rebuilds the slots, this one gone from the tree.
            box.AcceptEvent();
            switch (LookOf(craft))
            {
                case Look.Ready:
                    Send(new Craft(pod, craft, Crafts.CountFor(press.ShiftPressed)));
                    break;
                case Look.Short when Crafts.MissingCraft(Crafts.Lines(craft, _carried), _creative) is { } missing:
                    _pulse = (missing, 0);
                    break;
            }
        };
        return box;
    }

    /// <summary>A recipe's tip: its item's card with the craft block (<see cref="Crafts.Block"/>); a closed one, what opens it.</summary>
    private Tip TipOf(string craft)
    {
        if (_research?.Knows(craft) != true) return new Tip("???", new TipLine(T("crafts.notOpened"), Tone.Unknown));
        return CardTip(craft, Crafts.Block(craft, _carried, _creative));
    }

    /// <summary>The card of <paramref name="item"/> with what this station adds, its band as a slot's "?".</summary>
    private Tip CardTip(string item, StationBlock station)
    {
        bool unknown = !_creative && _research?.Analysed.Contains(item) != true;
        var card = ItemCards.Of(item, unknown) with { Station = station };
        return new Tip(card.Name) { Card = card };
    }

    /// <summary>The slot at work filled from the bottom by <paramref name="share"/> of its unit, 0 to 1.</summary>
    private static void Fill(ColorRect fill, double share)
    {
        float inside = Slot - 2, high = (float)(inside * Math.Clamp(share, 0, 1));
        fill.Position = new Vector2(1, 1 + inside - high);
        fill.Size = new Vector2(inside, high);
    }

    /// <summary>
    /// A queued craft: its item and how many are left, its orderer's suit colour in the corner, the one at
    /// work filling its slot from the bottom up behind them. A click cancels it, its cost given to whoever cancels.
    /// </summary>
    private Panel EntrySlot(int pod, Entry entry, bool atWork)
    {
        var box = Box();
        box.AddChild(Icon(entry.Recipe));
        box.AddChild(CountOf(entry.Count));
        var who = new Panel { Position = new Vector2(2, 2), Size = new Vector2(5, 5), MouseFilter = Control.MouseFilterEnum.Ignore };
        var colour = _suitOf(entry.By);
        who.AddThemeStyleboxOverride("panel", Style.Box(colour, colour, radius: 1));
        box.AddChild(who);
        if (atWork)
        {
            _fill = new ColorRect { Color = Style.Amber400 with { A = 0.35f }, MouseFilter = Control.MouseFilterEnum.Ignore };
            box.AddChild(_fill);
            // Behind the item, its count and its mark.
            box.MoveChild(_fill, 0);
            Fill(_fill, 0);
        }
        Add(new Cell(box, Filled, FilledHover, null));
        string whose = T(entry.By == _client?.Id ? "fabricator.yours" : "fabricator.friends");
        var tip = new Tip($"{T($"item.{entry.Recipe}")} ×{entry.Count}", new TipLine($"{whose} · {T("fabricator.cancel")}", Tone.Dim));
        _tips.Add(box, () => tip);
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            box.AcceptEvent();
            Send(new Cancel(pod, entry.Id));
        };
        return box;
    }

    /// <summary>What the pod made, in an output slot: a click takes the stack into the inventory.</summary>
    private Panel OutputSlot(int pod, int slot, Stack stack)
    {
        var box = Box();
        box.AddChild(Icon(stack.Item));
        box.AddChild(CountOf(stack.Count));
        Add(new Cell(box, Filled, FilledHover, null));
        var tip = CardTip(stack.Item, new StationBlock(null, [], [new CardKey(T("key.lmb"), T("card.take"))]));
        _tips.Add(box, () => tip);
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            box.AcceptEvent();
            Send(new Move(new Place(slot, pod), new Target(), stack.Count));
        };
        return box;
    }

    private void Add(Cell cell)
    {
        _cells.Add(cell);
        Paint(cell);
    }

    /// <summary>A slot's edge: amber and ringed while it pulses, light under the pointer.</summary>
    private void Paint(Cell cell)
    {
        if (_pulse is var (craft, time) && cell.Craft == craft)
        {
            // box-shadow 0 0 0 0 → 0 0 0 9px, amber fading out, ease-out: the mockup's .slot.pulse.
            float t = (float)(time % Beat / Beat), eased = 1 - (1 - t) * (1 - t);
            (Pulse.ShadowSize, Pulse.ShadowColor) = ((int)(9 * eased), Style.Amber400 with { A = 0.9f * (1 - eased) });
            cell.Box.AddThemeStyleboxOverride("panel", Pulse);
            return;
        }
        cell.Box.AddThemeStyleboxOverride("panel", cell == _hovered ? cell.Hover : cell.Rest);
    }

    private static GridContainer Grid() =>
        Style.Gap(new GridContainer { Columns = Crafts.Columns, MouseFilter = Control.MouseFilterEnum.Ignore }, 4);

    private static Panel Box() => new()
    {
        CustomMinimumSize = new Vector2(Slot, Slot),
        MouseFilter = Control.MouseFilterEnum.Stop,
    };

    private static Panel EmptySlot()
    {
        var box = new Panel { CustomMinimumSize = new Vector2(Slot, Slot), MouseFilter = Control.MouseFilterEnum.Ignore };
        box.AddThemeStyleboxOverride("panel", Empty);
        return box;
    }

    private static Label CountOf(int count)
    {
        var label = new Label
        {
            Text = count > 1 ? count.ToString() : "",
            LabelSettings = Count,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        (label.OffsetRight, label.OffsetBottom) = (-2, 2);
        return label;
    }

    /// <summary>A heading over a section: its name, and on the right a word in grey.</summary>
    private static HBoxContainer Heading(string name, Label aside)
    {
        var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 8);
        row.AddChild(new Label { Text = name, LabelSettings = HeadingText, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        row.AddChild(aside);
        return row;
    }

    private static ShaderMaterial Shaded(string code) => new() { Shader = new Shader { Code = code } };

    private static TextureRect Icon(string item) => new()
    {
        Texture = ItemIcons.Icon(item),
        Position = new Vector2(4, 4),
        Size = new Vector2(Slot - 8, Slot - 8),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
}
