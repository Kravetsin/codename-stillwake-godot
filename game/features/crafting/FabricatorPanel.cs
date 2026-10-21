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
/// A queued station's panel — the lifepod's Fabricator, the Former — docked under the inventory while E has it
/// open, in the one machine frame (0058; the mockup, "Fabricator panel"): its name, a powered one's bolt and what it draws, and its small
/// screen, running while it works, READY idle and dark with no power; its recipes in fixed places on the left, a locked
/// one a silhouette, a short one dimmed; on the right the queue every hero shares,
/// the unit at work filling its slot from the bottom, each entry marked in its orderer's suit colour;
/// the outputs, a click taking a stack into the inventory; a tool's recipe tinted amber, a worn item's green. A click on a
/// recipe queues one, a shift-click five, a click on a short one pulses what it lacks; a click on an
/// entry cancels it. Read on a timer, and at once after a click; everything else said in the one
/// tooltip, a recipe's as its item's card with the craft block.
/// </summary>
public partial class FabricatorPanel : CanvasLayer
{
    /// <summary>What the panel shows is read this often.</summary>
    private const double RefreshMs = 100;
    private const int Gap = 8;
    /// <summary>One beat of the pulse on a recipe gone to, in seconds, and how many beats.</summary>
    private const double Beat = 0.9;
    private const int Beats = 2;

    private static readonly StyleBoxFlat Filled = Style.Box(Style.Slate800 with { A = 0.85f }, Style.Slate700, radius: 3);
    private static readonly StyleBoxFlat FilledHover = Style.Box(Style.Slate800 with { A = 0.85f }, Style.Slate400, radius: 3);
    private static readonly StyleBoxFlat Dark = Style.Box(Style.Slate900 with { A = 0.9f }, Style.Slate800, radius: 3);
    private static readonly StyleBoxFlat DarkHover = Style.Box(Style.Slate900 with { A = 0.9f }, Style.Slate400, radius: 3);
    private static readonly StyleBoxFlat Empty = Style.Box(Style.Slate800 with { A = 0.35f }, Style.Slate700 with { A = 0.45f }, radius: 3);
    private static readonly StyleBoxFlat Pulse = Style.Box(Style.Slate800 with { A = 0.85f }, Style.Amber400, radius: 3);
    private static readonly LabelSettings HeadingText = Style.Text(Style.Bold, 11, Style.Slate400);
    private static readonly LabelSettings AsideText = Style.Text(Style.Sans, 11, Style.Slate500);
    private static readonly LabelSettings Count = Style.Text(Style.Mono, 10, Colors.White, shadow: true);
    /// <summary>An entry fed from the inputs, and its endless sign: the inputs' blue (the mockup's requester colour).</summary>
    private static readonly Color Inlet = Style.Sky400;
    private static readonly LabelSettings EndlessText = Style.Text(Style.Bold, 11, Style.Sky400, shadow: true);
    /// <summary>How much of itself a first entry waiting for its inputs shows.</summary>
    private const float Waiting = 0.45f;

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
    private readonly GridContainer _recipes = Grid(), _inputs = Grid(), _queue = Grid(), _outputs = Grid();
    /// <summary>Where the station's drone badge goes, at the right of the header (0062), and the station it is for.</summary>
    private readonly HBoxContainer _served = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private int? _servedFor;
    /// <summary>The inputs' heading, shown only for a station that has inputs.</summary>
    private Control _inputsHeading = null!;
    /// <summary>The station and the slot size the input slots were lent for: built again only when either changes.</summary>
    private (int Pod, int Slot)? _inputsFor;
    /// <summary>Where the station's module sockets go, under its inputs, and their heading (0086).</summary>
    private readonly HBoxContainer _sockets = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.End };
    private readonly Label _socketsHeading = new() { Text = T("module.socket"), LabelSettings = AsideText };
    private readonly Label _opened = new() { LabelSettings = AsideText };
    private readonly FabricatorScreen _screen = new() { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
    /// <summary>The machine frame every machine opens in: the station's name, a powered one's bolt, and the screen in its header.</summary>
    private MachineFrame _panel = null!;
    /// <summary>A slot's side, <see cref="Ui.Slot"/> as the slots were last built.</summary>
    private int _slot;
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

    /// <param name="ui">Where the open inventory ends, which the panel docks under, the machine E opened, and the slots' size.</param>
    /// <param name="suitOf">The suit colour of a peer's hero, marking whose order an entry is.</param>
    public FabricatorPanel(Ui ui, Func<int, Color> suitOf)
    {
        _ui = ui;
        _suitOf = suitOf;
        _tips = new TipSpots(ui.Tip);
    }

    /// <summary>A slot that answers the pointer, and the box it is drawn with when the pointer is elsewhere.</summary>
    private sealed record Cell(Panel Box, StyleBoxFlat Rest, StyleBoxFlat Hover, string? Craft);

    /// <summary>
    /// A slot of the open machine drawn and worked by the items feature as any of its own (its click rules, the hand,
    /// drags, its tip): where the inputs go. Set by <c>Main</c>, which wires the two features.
    /// </summary>
    public Func<Place, Control>? Lend { get; set; }

    /// <summary>The station's drone badge (0062), the logistics feature's; null where drones have no work. Set by <c>Main</c>.</summary>
    public Func<int, Control?>? Served { get; set; }

    /// <summary>
    /// A station's module sockets (0086), drawn and worked by the items feature, which owns what is fitted; null for one that
    /// takes no module (the pod). Set by <c>Main</c>.
    /// </summary>
    public Func<int, string, Control?>? LendSockets { get; set; }

    /// <summary>What a station draws while it works, its modules folded in (0086): the power module's <c>Draw</c>. Set by <c>Main</c>.</summary>
    public Func<int, int>? Draw { get; set; }

    /// <summary>The queued station open under the inventory: the machine E opened, while it is one and the inventory is shown.</summary>
    private (int Id, Content.Station Station)? Open =>
        _ui.InventoryFoot.Get() != null && _ui.OpenCrate.Get() is int id && _crafting?.StationOf(id) is { Fed: false } station ? (id, station) : null;

    /// <summary>Whether the panel shows, and what its screen shows: what a recording checks.</summary>
    public (bool Shown, Doing Doing) Shown => (_panel.Visible, _screen.Doing);

    /// <summary>Where the panel lies on the screen while it shows: what the crew's markers keep out of.</summary>
    public Rect2? Covers => _panel.Visible ? _panel.GetGlobalRect() : null;

    /// <summary>The middle of recipe <paramref name="craft"/>'s slot on the screen while the panel shows: where a recording points.</summary>
    public Vector2? CenterOf(string craft) =>
        _cells.FirstOrDefault(c => c.Craft == craft && c.Box.IsVisibleInTree())?.Box.GetGlobalRect().GetCenter();

    public override void _Ready()
    {
        Layer = ScreenLayers.Hud;
        _panel = new MachineFrame("", null) { Visible = false };
        _panel.Head.AddChild(_screen);
        _panel.Head.AddChild(_served);
        // The recipes on the left, the queue and the outputs on the right: the whole Tab screen fits 720 px at the large slot size.
        var body = Style.Gap(new HBoxContainer(), 12);
        var left = Style.Gap(new VBoxContainer(), 6);
        left.AddChild(Heading(T("fabricator.recipes"), _opened));
        left.AddChild(_recipes);
        var right = Style.Gap(new VBoxContainer(), 6);
        // Inputs, queue, outputs: what goes in, what is ordered, what came out (the logistics mockup).
        // The module socket at the inputs' right end, under its own word (0086): a row of its own would push the panel past 720 px.
        right.AddChild(_inputsHeading = Heading(T("fabricator.inputs"), _socketsHeading));
        var inputs = Style.Gap(new HBoxContainer(), 12);
        inputs.AddChild(_inputs);
        inputs.AddChild(_sockets);
        right.AddChild(inputs);
        right.AddChild(Heading(T("crafts.queue"), new Label { Text = T("fabricator.shared"), LabelSettings = AsideText }));
        right.AddChild(_queue);
        right.AddChild(Heading(T("fabricator.output"), new Label()));
        right.AddChild(_outputs);
        body.AddChild(left);
        body.AddChild(right);
        _panel.Add(body);
        AddChild(_panel);
        _ui.InventoryFoot.Subscribe(Dock);
        _ui.OpenCrate.Subscribe(Dock);
        // Another slot size: every slot built again at it.
        _ui.SlotScale.Subscribe(() =>
        {
            _shownKey = null;
            Read();
        });
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
        var foot = _ui.InventoryFoot.Get();
        bool shown = Open != null;
        _panel.Visible = shown;
        // Where the items feature opens every other machine: under the grid, level with its left edge (0058).
        if (shown && foot is var (left, bottom)) _panel.Position = new Vector2(left, bottom + Gap);
        if (!shown) _tips.Show(null);
        Read();
    }

    private void Read()
    {
        _sinceRead = 0;
        if (Open is not var (pod, station) || _client == null || _crafting == null || _research == null || _items == null) return;
        int peer = _client.Id;
        _carried = _items.Totals(peer);
        _creative = _items.Creative;
        var queue = _crafting.Queue(pod);
        int progress = _crafting.Progress(pod);
        // The outputs as this peer's own takes in flight leave them, so a stack goes on the click (0014).
        var moves = _client.InFlight.Where(c => c is Move or Drop).ToList();
        var outputs = _items.AfterMoves(peer, moves).Crates.Where(c => c.Crate == pod).Select(c => c.Slots).FirstOrDefault() ?? _items.Crate(pod);
        var lacks = _crafting.Lacks(pod);
        string key = $"{pod}|" + string.Join(",", Content.Crafts.All.Select(c => (int)LookOf(c.Item)))
            + "|" + string.Join(";", queue.Select(e => $"{e.Id}:{e.Recipe}:{e.Count}:{e.By}:{e.FromInputs}:{e.Endless}"))
            + "|" + string.Join(";", lacks.Select(l => $"{l.Item}:{l.Count}"))
            + "|" + string.Join(";", outputs.Select(s => s == null ? "" : $"{s.Item}:{s.Count}"));
        if (key != _shownKey)
        {
            _shownKey = key;
            Build(pod, station, queue, outputs, lacks);
        }
        if (_fill != null && queue.Count > 0) Fill(_fill, Crafts.Share(queue[0].Recipe, progress));
        bool works = _crafting.Works(pod);
        _screen.Doing = Crafts.DoingOf(queue, progress, works, lacks.Count > 0);
        // What it draws with its modules: an arc fitted doubles it (0086).
        if (Content.Machines.Get(station.Machine).Draws != null && Draw?.Invoke(pod) is int draw) _panel.Describe(T($"station.{station.Machine}"), draw);
        // A station that draws shows its bolt and what it draws, lit while it works and dark at 0 while it rests or has no power.
        _panel.Light(works);
    }

    private Look LookOf(string craft) => Crafts.LookOf(_research!.Knows(craft), Crafts.Lines(craft, _carried), _creative);

    private void Send(Sim.Command command)
    {
        _client?.Send(command);
        Read();
    }

    private void Build(int pod, Content.Station station, IReadOnlyList<Entry> queue, IReadOnlyList<Stack?> outputs, IReadOnlyList<(string Item, int Count)> lacks)
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

        _slot = _ui.Slot;
        _panel.Describe(T($"station.{station.Machine}"), Content.Machines.Get(station.Machine).Draws);
        var places = Crafts.Places(station);
        foreach (var place in places) _recipes.AddChild(place is { } craft ? RecipeSlot(pod, station, craft) : EmptySlot());
        var crafts = places.OfType<string>().ToList();
        _opened.Text = T("crafts.opened", ("n", crafts.Count(_research!.Knows)), ("all", crafts.Count));
        LendInputs(pod, station);
        for (int i = 0; i < station.QueueMost; i++)
            _queue.AddChild(i < queue.Count ? EntrySlot(pod, queue[i], i == 0, i == 0 ? lacks : []) : EmptySlot());
        for (int i = Inputs(station); i < outputs.Count; i++)
            _outputs.AddChild(outputs[i] is { } stack ? OutputSlot(pod, i, stack) : EmptySlot());
        _panel.ResetSize();
    }

    /// <summary>How many of <paramref name="station"/>'s first slots are its inputs: the Former's two, the assembler's three, none of the pod's.</summary>
    private static int Inputs(Content.Station station) => Content.Machines.Get(station.Machine).Inputs ?? 0;

    /// <summary>
    /// The station's input slots, lent by the items feature (<see cref="Lend"/>), so a stack goes in and out by its
    /// clicks as into any machine; laid out again only for another station or slot size, not on every read.
    /// </summary>
    private void LendInputs(int pod, Content.Station station)
    {
        int inputs = Inputs(station);
        _inputsHeading.Visible = _inputs.Visible = inputs > 0;
        if (_servedFor != pod)
        {
            _servedFor = pod;
            foreach (var old in _served.GetChildren())
            {
                _served.RemoveChild(old);
                old.QueueFree();
            }
            if (Served?.Invoke(pod) is { } badge) _served.AddChild(badge);
        }
        if (_inputsFor == (pod, _slot)) return;
        _inputsFor = (pod, _slot);
        foreach (var old in _inputs.GetChildren().Concat(_sockets.GetChildren()))
        {
            old.GetParent().RemoveChild(old);
            old.QueueFree();
        }
        for (int i = 0; i < inputs; i++) _inputs.AddChild(Lend?.Invoke(new Place(i, pod)) ?? EmptySlot());
        // Its module sockets, the items feature's (0086): the pod takes none.
        var sockets = LendSockets?.Invoke(pod, station.Machine);
        if (sockets != null) _sockets.AddChild(sockets);
        _socketsHeading.Visible = _sockets.Visible = sockets != null;
    }

    /// <summary>A recipe in its place: as it looks now, its cost in the tip; a click queues it or goes to what it is short of.</summary>
    private Panel RecipeSlot(int pod, Content.Station station, string craft)
    {
        var look = LookOf(craft);
        var box = Box();
        var icon = Icon(craft);
        icon.Material = look switch { Look.Short => Greyed, Look.Locked => Silhouette, _ => null };
        box.AddChild(icon);
        Add(look == Look.Locked ? new Cell(box, Dark, DarkHover, craft)
            // A tool's recipe amber, a worn item's green, as their slots are everywhere (0058).
            : Slots.TintOf(craft) is not SlotTint.Plain and var tint ? new Cell(box, SlotLooks.Of(tint, false, false), SlotLooks.Of(tint, true, false), craft)
            : new Cell(box, Filled, FilledHover, craft));
        _tips.Add(box, () => TipOf(craft, station));
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right } press) return;
            // Accepted first: what the press does rebuilds the slots, this one gone from the tree.
            box.AcceptEvent();
            if (press.ButtonIndex == MouseButton.Right)
            {
                // From the inputs, whatever is carried: each unit takes its cost there as it begins.
                if (Crafts.HasInputs(station) && LookOf(craft) != Look.Locked)
                    Send(new Craft(pod, craft, Crafts.CountFor(press.ShiftPressed), FromInputs: true));
                return;
            }
            switch (LookOf(craft))
            {
                case Look.Ready:
                    Send(new Craft(pod, craft, Crafts.CountFor(press.ShiftPressed)));
                    break;
                case Look.Short when Crafts.MissingCraft(Crafts.Lines(craft, _carried), _creative, station) is { } missing:
                    _pulse = (missing, 0);
                    break;
            }
        };
        return box;
    }

    /// <summary>A recipe's tip: its item's card with the craft block (<see cref="Crafts.Block"/>); a closed one, what opens it.</summary>
    private Tip TipOf(string craft, Content.Station station)
    {
        if (_research?.Knows(craft) != true) return new Tip("???", new TipLine(T("crafts.notOpened"), Tone.Unknown));
        return CardTip(craft, Crafts.Block(craft, station, _carried, _creative));
    }

    /// <summary>The card of <paramref name="item"/> with what this station adds, its band as a slot's "?".</summary>
    private Tip CardTip(string item, StationBlock station)
    {
        bool unknown = ItemCards.Unknown(item, _creative, _research?.Analysed ?? []);
        var card = ItemCards.Of(item, unknown) with { Station = station };
        return new Tip(card.Name) { Card = card };
    }

    /// <summary>The slot at work filled from the bottom by <paramref name="share"/> of its unit, 0 to 1.</summary>
    private void Fill(ColorRect fill, double share)
    {
        float inside = _slot - 2, high = (float)(inside * Math.Clamp(share, 0, 1));
        fill.Position = new Vector2(1, 1 + inside - high);
        fill.Size = new Vector2(inside, high);
    }

    /// <summary>
    /// A queued craft: its item and how many are left, its orderer's suit colour in the corner, the one at
    /// work filling its slot from the bottom up behind them. One fed from the inputs has a small blue inlet in the
    /// other top corner, and repeating, a blue ∞ in place of its count; first and waiting for its inputs
    /// (<paramref name="lacks"/>), it is dimmed. A click cancels it, what was paid given to whoever cancels; a right
    /// click on one fed from the inputs makes it repeat, or stop.
    /// </summary>
    private Panel EntrySlot(int pod, Entry entry, bool atWork, IReadOnlyList<(string Item, int Count)> lacks)
    {
        var box = Box();
        box.AddChild(Icon(entry.Recipe));
        box.AddChild(CountOf(entry.Endless ? 1 : entry.Count));
        var who = new Panel { Position = new Vector2(2, 2), Size = new Vector2(5, 5), MouseFilter = Control.MouseFilterEnum.Ignore };
        var colour = _suitOf(entry.By);
        who.AddThemeStyleboxOverride("panel", Style.Box(colour, colour, radius: 1));
        box.AddChild(who);
        if (entry.FromInputs)
        {
            var inlet = new Panel { Position = new Vector2(_slot - 9, 2), Size = new Vector2(7, 7), MouseFilter = Control.MouseFilterEnum.Ignore };
            inlet.AddThemeStyleboxOverride("panel", Style.Box(Inlet, new Color("0b1220"), radius: 2));
            box.AddChild(inlet);
        }
        if (entry.Endless)
            box.AddChild(new Label { Text = "∞", LabelSettings = EndlessText, Position = new Vector2(2, _slot - 18), MouseFilter = Control.MouseFilterEnum.Ignore });
        if (lacks.Count > 0) box.Modulate = new Color(1, 1, 1, Waiting);
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
        var lines = new List<TipLine>();
        if (entry.FromInputs) lines.Add(new TipLine(entry.Endless ? $"{T("fabricator.fromInputs")} · {T("fabricator.repeats")}" : T("fabricator.fromInputs")));
        if (lacks.Count > 0)
            lines.Add(new TipLine(T("fabricator.waiting", ("items", string.Join(", ", lacks.Select(l => $"{T($"item.{l.Item}")} ×{l.Count}")))), Tone.Bad));
        lines.Add(new TipLine($"{whose} · {T("fabricator.cancel")}", Tone.Dim));
        if (entry.FromInputs) lines.Add(new TipLine(T(entry.Endless ? "fabricator.stopRepeating" : "fabricator.repeat"), Tone.Dim));
        var tip = new Tip($"{T($"item.{entry.Recipe}")} {(entry.Endless ? "∞" : $"×{entry.Count}")}", lines);
        _tips.Add(box, () => tip);
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right } press) return;
            box.AcceptEvent();
            // The left button cancels any entry, as before; the right makes one fed from the inputs repeat, or stop.
            if (press.ButtonIndex == MouseButton.Left) Send(new Cancel(pod, entry.Id));
            else if (entry.FromInputs) Send(new SetEndless(pod, entry.Id, !entry.Endless));
        };
        return box;
    }

    /// <summary>What the station made, in an output slot: a click takes the stack into the inventory.</summary>
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
        Style.Gap(new GridContainer { Columns = Crafts.Columns, MouseFilter = Control.MouseFilterEnum.Ignore }, Slots.Gap);

    private Panel Box() => new()
    {
        CustomMinimumSize = new Vector2(_slot, _slot),
        MouseFilter = Control.MouseFilterEnum.Stop,
    };

    private Panel EmptySlot()
    {
        var box = new Panel { CustomMinimumSize = new Vector2(_slot, _slot), MouseFilter = Control.MouseFilterEnum.Ignore };
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

    private TextureRect Icon(string item) => new()
    {
        Texture = ItemIcons.Icon(item),
        Position = new Vector2(Slots.Inset, Slots.Inset),
        Size = new Vector2(_slot - 2 * Slots.Inset, _slot - 2 * Slots.Inset),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
}
