using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Furnace;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;
using MachineSpecs = Stillwake.Content.Machines;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The hotbar, always in the top-left corner, each slot's key in its corner; and with Tab, or E on a crate, the
/// rest of the inventory, the open machine under it in the one frame (0058, <see cref="MachineFrame"/>): a crate's
/// slots, a furnace's input over its fuel and its outputs, an electric smelter's or a grow capsule's input and outputs, a burner
/// generator's fuel, an analyzer's own body (<see cref="AnalyzerPanel"/>), a charging station's slot and bar
/// (0053), each named, a powered one with its bolt; a queued station's is the crafting feature's, docked in the
/// same place. Stacks move by clicks (<see cref="Clicks.On"/>), or dragged from one slot to another; Tab or Esc
/// closes both. 1–9 and the wheel over the world choose a hotbar slot, Q throws its stack. A stack's name, and
/// whether it waits for an analyzer, show in the one tooltip; an item the open fed station or fire takes has its
/// flame or sprout in its slot's corner (<see cref="Reading.MarkOf"/>), else one not analysed a violet "?" in its
/// slot's corner, and in the hand. At the screen's right edge, the equipment by the body (0047, 0058) and the
/// hands' tool: a stack carried lights the slots that would wear it and dims the rest; a right click on a
/// canister of the hero's own breathes it (<see cref="Ui.Breathe"/>). Every slot is <see cref="Ui.Slot"/> wide,
/// and all of it is built again when that changes.
/// </summary>
public partial class InventoryPanel : CanvasLayer
{
    /// <summary>The slots are read this often, and at once after this peer's own move.</summary>
    private const double RefreshMs = 100;

    private static readonly StyleBoxFlat FireOut = Style.Box(Style.Slate700, Style.Slate700, 0, 5);
    private static readonly StyleBoxFlat FireOn = Style.Box(Style.Orange400, Style.Orange400, 0, 5);
    /// <summary>A grow capsule's mark while it grows: its lamp's pink (0060).</summary>
    private static readonly StyleBoxFlat LampOn = Style.Box(Style.Pink400, Style.Pink400, 0, 5);
    /// <summary>A fed station too cold for what its input holds: its mark cold blue (0057).</summary>
    private static readonly StyleBoxFlat Cold = Style.Box(Style.Sky400, Style.Sky400, 0, 5);
    private static readonly LabelSettings Trouble = Style.Text(Style.Bold, 10, Style.Red300);

    private readonly List<SlotBox> _slots = [];
    /// <summary>The slots lent to another feature's panel (<see cref="Lend"/>): theirs to take down, not this panel's.</summary>
    private readonly List<SlotBox> _lent = [];
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    /// <summary>The stack carried, over every panel.</summary>
    private readonly CanvasLayer _over = new() { Layer = ScreenLayers.Tips };
    private Control _hotbar = null!, _shown = null!, _inventory = null!, _body = null!, _held = null!, _fire = null!;
    /// <summary>The open machine's frame, under the inventory.</summary>
    private MachineFrame? _frame;
    private TextureRect _heldIcon = null!;
    private Label _heldCount = null!, _creative = null!, _heldMark = null!;
    private ProgressBar? _progress;
    private Label? _problem;
    /// <summary>A charging station's: the bar of what its battery holds, and what it does now in words.</summary>
    private ColorRect? _charged;
    private Label? _station;
    private AnalyzerPanel? _analyzer;
    /// <summary>What the mark between a fed station's slots says by the pointer: what it is too cold for; null while it is not.</summary>
    private string? _tooCold;
    /// <summary>The slots and the analyzer's pictures, each saying what it holds in the one tooltip.</summary>
    private readonly TipSpots _tipSpots;

    private Client? _client;
    private IItemsQueries? _items;
    private IGridQueries? _grid;
    private IResearchQueries? _research;
    private HeroSpot? _hero;
    private Reading? _reading;
    private double _sinceRead;

    private bool _open;
    /// <summary>The crate and its kind the crate panel was made for.</summary>
    private (int Id, string Kind)? _built;
    private int _chosen;
    private Picked? _picked;
    /// <summary>The item the picked slot held as it was picked: one that holds another now, sorted or swapped by anyone, lets it go.</summary>
    private string? _pickedItem;
    private Place? _dragFrom, _hovered;
    private Vector2 _mouse;

    private readonly Ui _ui;

    /// <param name="ui">The hand, which choosing a hotbar slot or opening the inventory frees, the crate open, and the slots' size.</param>
    public InventoryPanel(Ui ui)
    {
        _ui = ui;
        _tipSpots = new TipSpots(ui.Tip);
        // The crew's E opens a crate, and reaching out of it closes it: shown at once.
        ui.OpenCrate.Subscribe(Read);
        // The chosen slot is outlined only while the hand holds what is in it.
        ui.Hand.Subscribe(Refresh);
        // A slot size chosen in the settings: everything built again at once, what is open kept open.
        ui.SlotScale.Subscribe(() =>
        {
            if (!IsInsideTree()) return;
            Clear();
            Build();
            Refresh();
        });
    }

    /// <summary>The crate open under the inventory: E on one within reach opens it, out of reach it closes.</summary>
    private int? Crate => _ui.OpenCrate.Get();

    private bool HandFree => _ui.Hand.Get() == Holding.Free;

    /// <summary>What a furnace or a burner generator is doing, by machine id: the furnace module's <c>At</c>.</summary>
    public Func<int, Smelting>? Furnace { get; set; }

    /// <summary>The stations, for an electric smelter's or a grow capsule's panel: how far its work is, whether it works, whether it is too cold.</summary>
    public CraftingModule? Crafting { get; set; }

    /// <summary>Whether a charging station fills what it holds now, by machine id: the power module's <c>Charges</c>.</summary>
    public Func<int, bool>? Charger { get; set; }

    /// <summary>A machine's drone badge (0062), the logistics feature's, put at the right of its frame's header; null where drones have no work. Set by <c>Main</c>.</summary>
    public Func<int, Control?>? Served { get; set; }

    /// <summary>The item drones keep a machine's slot full of, while drones serve it (0062); null for none. Set by <c>Main</c>.</summary>
    public Func<Place, string?>? Kept { get; set; }

    /// <summary>Whether something in a bay's reach holds that item for that machine: a kept slot's tooltip (0062). Set by <c>Main</c>.</summary>
    public Func<int, string, bool>? Supplied { get; set; }

    /// <summary>The hotbar slot chosen, with 1–9 or the wheel.</summary>
    public int Chosen => _chosen;

    /// <summary>Whether the inventory or a crate is open: what Esc closes before it opens the menu.</summary>
    public bool Shown => _open || Crate != null;

    /// <summary>
    /// A picked stack whose slot still holds its item, while the panels are open: one emptied meanwhile by another player at
    /// a crate, or holding another item after a sort, is let go.
    /// </summary>
    private Picked? Held => Shown && _picked is { } p ? Clicks.StillHeld(p, _pickedItem, _reading?.At(p.Place)) : null;

    /// <summary>What the pointer carries, drawn there: the stack held, or, with the panels closed, the one a drag along the hotbar moves.</summary>
    private Picked? Carried => Held ?? (Shown || _dragFrom is not { } from ? null : Clicks.Dragged(from, _hovered, _reading?.At(from)));

    public override void _Ready()
    {
        Layer = ScreenLayers.Hud;
        AddChild(_root);
        AddChild(_over);
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        Build();
    }

    /// <summary>The hotbar, the inventory, the equipment and the stack in hand, at the slot size chosen now.</summary>
    private void Build()
    {
        int size = _ui.Slot, icon = size - 2 * Slots.Inset;
        var top = Style.Gap(new HBoxContainer(), 8);
        top.AddChild(Grid(Enumerable.Range(0, Hotbar.Row).Select(i => new Place(i)), keys: true));
        top.AddChild(_creative = new Label
        {
            LabelSettings = Style.Text(Style.Mono, 12, Style.Amber400),
            Text = T("items.creative"),
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false,
        });
        _hotbar = Style.Panel(Style.Slate700, top);
        _hotbar.Position = new Vector2(Slots.Margin, Slots.Margin);
        _root.AddChild(_hotbar);

        // The grid, and under it the machine open; the equipment at the screen's other edge (0058).
        var shown = Style.Gap(new VBoxContainer { Position = new Vector2(Slots.Margin, Slots.InventoryTop(size)) }, 8);
        // The bag, its sort button in the top-right corner (0008).
        var bag = Style.Gap(new HBoxContainer(), 4);
        bag.AddChild(Grid(Enumerable.Range(Hotbar.Row, Content.Inventory.Slots - Hotbar.Row).Select(i => new Place(i))));
        var corner = new VBoxContainer();
        corner.AddChild(Sorter(null));
        bag.AddChild(corner);
        var grid = Style.Panel(Style.Slate700, bag);
        grid.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        shown.AddChild(_inventory = grid);
        _root.AddChild(_shown = shown);
        _shown.Visible = false;
        _root.AddChild(_body = Body(size));
        _body.Visible = false;

        _held = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Size = new Vector2(size, size), Visible = false };
        _held.AddChild(_heldIcon = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Position = new Vector2(Slots.Inset, Slots.Inset),
            Size = new Vector2(icon, icon),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        _held.AddChild(_heldMark = SlotBox.Mark(size));
        _held.AddChild(_heldCount = new Label
        {
            LabelSettings = Style.Text(Style.Mono, 10, Colors.White, shadow: true),
            Size = new Vector2(size - 2, size + 2),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        _over.AddChild(_held);
    }

    /// <summary>Takes down everything <see cref="Build"/> and the machine's panel made, before they are built at another size.</summary>
    private void Clear()
    {
        foreach (var node in _root.GetChildren().Concat(_over.GetChildren()))
        {
            node.GetParent().RemoveChild(node);
            node.QueueFree();
        }
        // Lent slots are their panel's to build again: kept, with their tips, until they leave the tree.
        _slots.RemoveAll(s => !_lent.Contains(s));
        _tipSpots.Clear();
        foreach (var box in _lent) _tipSpots.Add(box, () => SlotTip(box));
        _tipSpots.Show(null);
        _built = null;
        (_frame, _progress, _problem, _analyzer, _charged, _station, _tooCold, _hovered) = (null, null, null, null, null, null, null, null);
    }

    /// <summary>
    /// Reads the client's world on a timer, through the current registry's queries: a resync brings new
    /// ones. <paramref name="hero"/> is where a stack is thrown from; none until the hero is placed.
    /// </summary>
    public void Frame(Client client, IItemsQueries items, IGridQueries grid, IResearchQueries research, HeroSpot? hero, double elapsedMs)
    {
        bool fresh = _items != items || _client != client;
        (_client, _items, _grid, _research, _hero) = (client, items, grid, research, hero);
        _sinceRead += elapsedMs;
        if (fresh || _sinceRead >= RefreshMs) Read();
    }

    /// <summary>Closes both the inventory and the crate, putting a picked stack back: Tab, or Esc through <see cref="Escape"/>.</summary>
    public void Close()
    {
        (_open, _picked) = (false, null);
        _ui.OpenCrate.Set(null);
        Read();
    }

    private void Read()
    {
        _sinceRead = 0;
        if (_client == null || _items == null || _grid == null || _research == null) return;
        _reading = Reading.Of(_client.Id, _client.InFlight, _items, _grid, _research, Crate);
        Refresh();
    }

    private void Send(Command command)
    {
        _client?.Send(command);
        // Read again now, not on the next refresh: the move shows at once, and a quick next click acts on it.
        Read();
    }

    /// <param name="keys">Whether each slot shows the key that chooses it: the hotbar's.</param>
    private GridContainer Grid(IEnumerable<Place> places, bool keys = false)
    {
        var grid = Style.Gap(new GridContainer { Columns = Hotbar.Row }, Slots.Gap);
        foreach (var place in places) grid.AddChild(Box(place, keys ? Hotbar.KeyOf(place.Slot) : null));
        return grid;
    }

    /// <summary>
    /// The equipment at the screen's right edge, its top level with the inventory's (0047, 0058): the head's slot
    /// over the back's over the belt's three, then the hands' tool, each row under its name; no silhouettes in
    /// empty slots.
    /// </summary>
    private Control Body(int size)
    {
        var column = Style.Gap(new VBoxContainer(), 2);
        foreach (var kind in Content.Inventory.Equipment.Distinct())
        {
            column.AddChild(Caption(kind));
            var row = Style.Gap(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, Slots.Gap);
            foreach (var place in Equipment.Of(kind)) row.AddChild(Box(place));
            column.AddChild(row);
        }
        column.AddChild(Caption("hands"));
        var hands = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        hands.AddChild(ToolSlot(size));
        column.AddChild(hands);
        var body = Style.Panel(Style.Slate700, column);
        return body;
    }

    private static Label Caption(string kind) => new()
    {
        Text = T($"equip.{kind}"),
        LabelSettings = Style.Text(Style.Mono, 9, Style.Slate500),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    /// <summary>
    /// The tool the right button works with, the equipment's last row: the screwdriver everyone has. Only shown
    /// for now (0030): nothing is put in it until a tool better than the screwdriver exists.
    /// </summary>
    private Control ToolSlot(int size)
    {
        var slot = new Panel { CustomMinimumSize = new Vector2(size, size), MouseFilter = Control.MouseFilterEnum.Stop };
        slot.AddThemeStyleboxOverride("panel", SlotLooks.Of(SlotTint.Plain, false, false));
        var icon = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://art/tool_screwdriver.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        slot.AddChild(icon);
        icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize, Slots.Inset);
        _tipSpots.Add(slot, () => new Tip(T("gear.screwdriver"), [new TipLine(T("gear.screwdriver.about"), Tone.Dim)]));
        return slot;
    }

    private SlotBox Box(Place place, string? key = null)
    {
        var box = new SlotBox(place, _ui.Slot, key);
        box.Pressed += ClickAt;
        _tipSpots.Add(box, () => SlotTip(box));
        _slots.Add(box);
        return box;
    }

    /// <summary>
    /// A slot of the open machine for another feature's panel to lay out, drawn and worked here as any of this panel's:
    /// clicks, the hand, drags, its tip. The crafting feature's queued station puts its inputs so (wired by <c>Main</c>).
    /// Forgotten as it leaves the tree.
    /// </summary>
    public Control Lend(Place place)
    {
        var box = Box(place);
        _lent.Add(box);
        box.TreeExiting += () =>
        {
            _slots.Remove(box);
            _lent.Remove(box);
        };
        Refresh();
        return box;
    }

    /// <summary>A sort button for the bag, or with <paramref name="crate"/> for that crate; its tip says what it does.</summary>
    private SortButton Sorter(int? crate)
    {
        var button = new SortButton(() => SortOf(crate));
        _tipSpots.Add(button, () => new Tip(T("items.sort")));
        return button;
    }

    /// <summary>Sorts the bag or the crate; a stack picked is put back first, as its slot may hold another after.</summary>
    private void SortOf(int? crate)
    {
        (_picked, _dragFrom) = (null, null);
        Send(new Sort(crate));
    }

    /// <summary>A stack's tip; an empty bay slot says a drone docks there, an empty kept one what drones keep it full of (0062).</summary>
    private Tip? SlotTip(SlotBox box)
    {
        if (_reading?.At(box.Place) is { } stack) return box.Place.Crate == null ? _reading.HereTipOf(stack) : _reading.TipOf(stack);
        if (box.Kept is { } kept && box.Place.Crate is int id)
        {
            var name = T("logistics.kept", ("item", T($"item.{kept}")));
            return Supplied?.Invoke(id, kept) == false ? new Tip(name, new TipLine(T("logistics.noneInReach"), Tone.Bad)) : new Tip(name);
        }
        return box.Ghost == Content.Logistics.Drone ? new Tip(T("item.drone"), new TipLine(T("logistics.drone"), Tone.Dim)) : null;
    }

    /// <summary>The open crate's panel, made anew for each crate: a crate's grid, a furnace's or an analyzer's own, in the machine frame.</summary>
    private void BuildCrate()
    {
        (int, string)? want = Crate is int id && _reading?.Kind is string k ? (id, k) : null;
        if (want == _built) return;
        _built = want;
        if (_frame != null)
        {
            _slots.RemoveAll(s => s.Place.Crate != null && !_lent.Contains(s));
            _shown.RemoveChild(_frame);
            _frame.QueueFree();
            (_frame, _progress, _problem, _analyzer, _charged, _station, _tooCold) = (null, null, null, null, null, null, null);
        }
        if (want is not var (crate, kind)) return;
        // A queued station (the lifepod's Fabricator, the Former) docks in the same place, the crafting feature's own panel.
        if (Content.Stations.TryGet(kind, out var station) && !station.Fed) return;
        var spec = MachineSpecs.Get(kind);
        _frame = new MachineFrame(T($"tool.{kind}"), kind == "burnerGenerator" ? spec.Makes : spec.Draws);
        // A crate sorts, its button in the header beside the drone badge (0008).
        if (kind == "crate") _frame.Head.AddChild(Sorter(crate));
        // Whether drones serve it, at the right of its header (0062).
        if (Served?.Invoke(crate) is { } served) _frame.Head.AddChild(served);
        _frame.Add(kind switch
        {
            "furnace" => FurnaceBody(crate),
            "burnerGenerator" => GeneratorBody(crate),
            "smelter" or "growCapsule" => FedBody(crate, kind),
            "analyzer" => _analyzer = new AnalyzerPanel(Box(new Place(0, crate)), _tipSpots, _ui.Slot),
            "charger" => ChargerBody(crate),
            _ when kind == Content.Logistics.Bay => BayBody(crate, spec),
            _ => Grid(Enumerable.Range(0, spec.Slots ?? 0).Select(i => new Place(i, crate))),
        });
        _shown.AddChild(_frame);
    }

    /// <summary>A drone bay's four slots, each showing a faint drone while empty: drones dock there and charge, each slot's bar its drone's charge (0062).</summary>
    private Control BayBody(int crate, Content.MachineSpec spec)
    {
        var row = Style.Gap(new HBoxContainer(), Slots.Gap);
        for (int i = 0; i < spec.Slots; i++)
        {
            var box = Box(new Place(i, crate));
            box.Ghost = Content.Logistics.Drone;
            row.AddChild(box);
        }
        return row;
    }

    /// <summary>Its input over its fuel, as Minecraft lays them, the fire between them; then how far its work is and its outputs.</summary>
    private Control FurnaceBody(int crate)
    {
        var column = Style.Gap(new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, 4);
        int fuel = MachineSpecs.Get("furnace").FuelSlot!.Value;
        column.AddChild(Box(new Place(0, crate)));
        column.AddChild(_fire = HeatMark());
        column.AddChild(Box(new Place(fuel, crate)));
        var outputs = Style.Gap(new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter }, Slots.Gap);
        for (int i = fuel + 1; i < MachineSpecs.Get("furnace").Slots; i++) outputs.AddChild(Box(new Place(i, crate)));
        var line = Style.Gap(new HBoxContainer(), 8);
        line.AddChild(column);
        line.AddChild(_progress = Style.Bar(Style.Orange400, 56));
        line.AddChild(outputs);
        return WithProblem(line);
    }

    /// <summary>
    /// A fed station on power, the furnace's without the fuel: its input over its mark, lit while it works, its bar and its
    /// outputs. The electric smelter's mark and bar are a fire's orange; a grow capsule's mark is its lamp's pink, its bar
    /// green, and its empty input shows the seed it takes (0060): the one item of its process.
    /// </summary>
    private Control FedBody(int crate, string kind)
    {
        var column = Style.Gap(new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, 4);
        var spec = MachineSpecs.Get(kind);
        var process = Content.Stations.Get(kind).Process;
        var input = Box(new Place(0, crate));
        var takes = Content.Items.All.Where(i => i.Yields != null && i.Process == process).ToList();
        if (takes.Count == 1) input.Ghost = takes[0].Id;
        column.AddChild(input);
        column.AddChild(_fire = HeatMark());
        var outputs = Style.Gap(new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter }, Slots.Gap);
        for (int i = spec.Inputs!.Value; i < spec.Slots; i++) outputs.AddChild(Box(new Place(i, crate)));
        var line = Style.Gap(new HBoxContainer(), 8);
        line.AddChild(column);
        line.AddChild(_progress = Style.Bar(process == "grow" ? Style.Green400 : Style.Orange400, 56));
        line.AddChild(outputs);
        return WithProblem(line);
    }

    /// <summary>Its fire over its fuel, as the furnace's; the power it makes is the frame's bolt, lit while it burns.</summary>
    private Control GeneratorBody(int crate)
    {
        var column = Style.Gap(new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, 4);
        column.AddChild(_fire = HeatMark());
        column.AddChild(Box(new Place(MachineSpecs.Get("burnerGenerator").FuelSlot!.Value, crate)));
        // In a row, as the furnace's: a column alone in the frame would stretch its slot across it.
        var line = new HBoxContainer();
        line.AddChild(column);
        return WithProblem(line);
    }

    /// <summary>
    /// A charging station's, as the station mockup: its slot beside a bar of what the battery holds, and under the
    /// bar what it does now; what it draws is the frame's bolt, lit while it charges.
    /// </summary>
    private Control ChargerBody(int crate)
    {
        var bay = Style.Gap(new HBoxContainer { CustomMinimumSize = new Vector2(196, 0) }, 10);
        bay.AddChild(Box(new Place(0, crate)));
        var right = Style.Gap(new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center }, 4);
        var track = new Panel { CustomMinimumSize = new Vector2(0, 10), MouseFilter = Control.MouseFilterEnum.Ignore };
        track.AddThemeStyleboxOverride("panel", Style.Box(new Color("0b1220"), Style.Slate700, radius: 2));
        track.AddChild(_charged = new ColorRect { Color = ItemCardView.Green, MouseFilter = Control.MouseFilterEnum.Ignore });
        _charged.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        (_charged.OffsetLeft, _charged.OffsetTop, _charged.OffsetRight, _charged.OffsetBottom) = (1, 1, 0, -1);
        right.AddChild(track);
        right.AddChild(_station = new Label { LabelSettings = Style.Text(Style.Mono, 11, Style.Slate400) });
        bay.AddChild(right);
        return bay;
    }

    /// <summary>
    /// The square between a station's slots: orange while it burns or works, cold blue while it is too cold for
    /// what its input holds, saying so by the pointer.
    /// </summary>
    private Panel HeatMark()
    {
        var mark = new Panel
        {
            CustomMinimumSize = new Vector2(10, 10),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _tipSpots.Add(mark, () => _tooCold is { } item ? Reading.TooColdTip(item) : null);
        return mark;
    }

    /// <summary><paramref name="line"/>, and under it why the machine stands still.</summary>
    private Control WithProblem(Control line)
    {
        var stack = Style.Gap(new VBoxContainer(), 4);
        stack.AddChild(line);
        stack.AddChild(_problem = new Label { LabelSettings = Trouble, Visible = false });
        return stack;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false } key)
        {
            switch (key.PhysicalKeycode)
            {
                case Key.Tab:
                    if (Shown) Close();
                    else
                    {
                        // One hand (0022): the inventory, a stack carried from it, takes it.
                        _ui.Hand.Set(Holding.Free);
                        (_open, _picked) = (true, null);
                    }
                    break;
                case >= Key.Key1 and <= Key.Key9:
                    _ui.Hand.Set(Holding.Free);
                    _chosen = Hotbar.SlotOfDigit((int)(key.PhysicalKeycode - Key.Key0))!.Value;
                    break;
                case Key.Q:
                    // A card in hand, not the chosen slot: Q drops nothing it does not show.
                    if (!HandFree) return;
                    if (_reading?.Inventory[_chosen] is { } stack) DropFrom(new Place(_chosen), stack.Count);
                    break;
                default:
                    return;
            }
            GetViewport().SetInputAsHandled();
            Refresh();
        }
        // Over the world only: a panel under the pointer takes the wheel before it gets here.
        else if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown or MouseButton.WheelUp } wheel)
        {
            _ui.Hand.Set(Holding.Free);
            _chosen = Hotbar.WheelSlot(_chosen, wheel.ButtonIndex == MouseButton.WheelDown ? 1 : -1);
            Refresh();
        }
    }

    /// <summary>
    /// The pointer and the left button let go, wherever they are: Godot sends a release to the control
    /// that took the press, so the slot under the pointer is found here. A stack in hand let go over the
    /// world is thrown.
    /// </summary>
    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouseMotion motion) _mouse = motion.Position;
        if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } up) return;
        _mouse = up.Position;
        // A drag along the closed hotbar carried its stack on the pointer: let go anywhere, it is gone from there.
        bool dragged = Carried != null && Held == null;
        if (SlotUnder(_mouse) is { } place)
        {
            ReleaseAt(place);
            if (dragged) Refresh();
            return;
        }
        _dragFrom = null;
        if (dragged) Refresh();
        if (Held is { } inHand && !OverPanel(_mouse))
        {
            DropFrom(inHand.Place, inHand.Count);
            _picked = null;
            Refresh();
        }
    }

    /// <summary>The middle of the slot at <paramref name="place"/> on screen, for scripted runs.</summary>
    public Vector2 CenterOf(Place place) => _slots.First(s => s.Place == place).GetGlobalRect().GetCenter();

    // Under the open menu no slot is pointed at: its name would show over the menu.
    private Place? SlotUnder(Vector2 at) => _ui.MenuOpen.Get() ? null :
        _slots.FirstOrDefault(s => s.IsVisibleInTree() && s.GetGlobalRect().HasPoint(at))?.Place;

    // Any panel under the pointer counts, another feature's too: a click on the crafts throws nothing.
    private bool OverPanel(Vector2 at) =>
        GetViewport().GuiGetHoveredControl() != null
        || _hotbar.GetGlobalRect().HasPoint(at)
        || (_body.Visible && _body.GetGlobalRect().HasPoint(at))
        || (_shown.Visible && _shown.GetChildren().OfType<Control>().Any(c => c.IsVisibleInTree() && c.GetGlobalRect().HasPoint(at)));

    /// <summary>Where the hotbar, the open inventory with the machine under it and the equipment lie on the screen: what the crew's markers keep out of.</summary>
    public IEnumerable<Rect2> Covers()
    {
        yield return _hotbar.GetGlobalRect();
        if (_body.Visible) yield return _body.GetGlobalRect();
        if (!_shown.Visible) yield break;
        foreach (var panel in _shown.GetChildren().OfType<Control>())
            if (panel.IsVisibleInTree()) yield return panel.GetGlobalRect();
    }

    /// <summary>Throws a stack from the hero the way it faces.</summary>
    private void DropFrom(Place src, int count)
    {
        if (_hero is not { } spot) return;
        Send(new Drop(src, count, spot.Cell, spot.Facing));
    }

    /// <summary>
    /// A stack picked up by a press, carried while the button is held: let go over another slot, it is
    /// put down there, as a second click would; let go where it was picked, it stays in hand.
    /// </summary>
    private void ReleaseAt(Place place)
    {
        var from = _dragFrom;
        _dragFrom = null;
        if (from == null || from == place || _reading == null) return;
        if (Held != null)
        {
            ClickAt(place, false, false);
            return;
        }
        if (Shown) return;
        // Along the hotbar with the inventory closed, where a press only chooses a slot: the stack is
        // lifted and put down in one go, as two clicks would with it open.
        var lifted = Clicks.On(null, new SlotClick(from, _reading.At(from), false, false, Crate, _reading.Inventory));
        if (lifted.Picked == null) return;
        var put = Clicks.On(lifted.Picked, new SlotClick(place, _reading.At(place), false, false, Crate, _reading.Inventory));
        if (put.Command == null) return;
        _chosen = place.Slot;
        Send(put.Command);
    }

    private void ClickAt(Place place, bool right, bool shift)
    {
        if (!right && !shift) _dragFrom = Held != null ? null : place;
        if (!Shown)
        {
            if (place.Crate == null && place.Slot < Hotbar.Row) _chosen = place.Slot;
            Refresh();
            return;
        }
        if (_reading == null) return;
        var spec = _reading.Kind is string kind && MachineSpecs.TryGet(kind, out var s) ? s : null;
        // A queued station's inputs share what is shift-clicked in, onto its kind first (the Former's, the assembler's).
        int inputs = _reading.Kind is string queued && Content.Stations.TryGet(queued, out var station) && !station.Fed ? spec?.Inputs ?? 0 : 0;
        var result = Clicks.On(Held, new SlotClick(
            place, _reading.At(place), right, shift, Crate, _reading.Inventory, spec?.FuelSlot, spec?.Charges == true, inputs, _reading.Crate));
        (_picked, _pickedItem) = (result.Picked, result.Picked is { } p ? _reading.At(p.Place)?.Item : null);
        if (result.Breathes) _ui.Breathe();
        if (result.Command != null) Send(result.Command);
        else Refresh();
    }

    public override void _Process(double delta)
    {
        // Hover by hit test: while a button is held Godot sends no mouse_entered to other controls.
        var hovered = SlotUnder(_mouse);
        if (hovered != _hovered)
        {
            _hovered = hovered;
            Refresh();
        }
        // A stack carried is the pointer: its icon centred on it, the arrow hidden while it is.
        _held.Position = _mouse - _held.Size / 2;
        bool carrying = _held.Visible && !_ui.MenuOpen.Get();
        var mode = carrying ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
        if (Input.MouseMode != mode) Input.MouseMode = mode;
        _ui.OnPointer.Set(carrying ? _held.Size.X : 0);
        // The equipment keeps its margin from the right edge, whatever the window's width and its own.
        _body.Position = new Vector2(_root.GetViewportRect().Size.X - Slots.Margin - _body.Size.X, Slots.InventoryTop(_ui.Slot));
        var grid = _inventory.GetGlobalRect();
        _ui.InventoryFoot.Set(_shown.Visible ? (grid.Position.X, grid.End.Y) : null);
        // A hidden slot says nothing; under the open menu none is pointed at.
        _tipSpots.Frame(_ui.MenuOpen.Get() ? null : _mouse);
    }

    private void Refresh()
    {
        if (_reading is not { } reading) return;
        BuildCrate();
        _shown.Visible = _body.Visible = Shown;
        _creative.Visible = reading.Creative;
        _ui.Wielded.Set(HandFree ? reading.Inventory[_chosen]?.Item : null);
        var held = Carried;
        // Carried, a stack lights the equipment slots that would wear it and dims the rest (0047).
        var carried = held is { } inHand ? reading.At(inHand.Place)?.Item : null;
        foreach (var box in _slots)
        {
            var p = box.Place;
            var stack = reading.At(p);
            box.Kept = p.Crate != null ? Kept?.Invoke(p) : null;
            box.Show(stack, HandFree && p.Crate == null && p.Slot == _chosen, held?.Place == p, _hovered == p,
                stack == null ? SlotMark.None : p.Crate == null ? reading.MarkOf(stack.Item) : reading.Unknown(stack.Item) ? SlotMark.Unknown : SlotMark.None,
                Equipment.Is(p) ? Equipment.GlowOf(p, carried) : null);
        }
        _held.Visible = held != null;
        if (held is { } h)
        {
            var stack = reading.At(h.Place)!;
            var item = stack.Item;
            _heldIcon.Texture = ItemIcons.Icon(Charges.Picture(item, stack.Charge));
            _heldMark.Visible = reading.Unknown(item);
            _heldCount.Text = h.Count > 1 ? h.Count.ToString() : "";
        }
        ShowMachine(reading);
    }

    /// <summary>The open charging station's bar, what it does in words, and its bolt lit with what it draws while it charges.</summary>
    private void ShowCharger(Stack? held, bool charging)
    {
        if (_charged == null || _station == null) return;
        float share = held == null ? 0 : Charges.Share(held);
        _charged.AnchorRight = share;
        _charged.OffsetRight = share >= 1 ? -1 : 0;
        _charged.Visible = share > 0;
        _station.Text = T(Charges.StationState(held, charging));
        _frame?.Light(charging);
    }

    /// <summary>
    /// The open analyzer's panel; whether the open furnace or generator burns and why it stands still; how
    /// far the furnace's work is; the frame's bolt lit while the machine works.
    /// </summary>
    private void ShowMachine(Reading reading)
    {
        if (reading.Lessons != null && _analyzer != null)
        {
            _analyzer.Show(reading.Lessons);
            _frame?.Light(_analyzer.Works);
        }
        if (reading.Kind == "charger" && Crate is int station) ShowCharger(reading.At(new Place(0, station)), Charger?.Invoke(station) == true);
        // A bay's bolt lit while it charges a drone docked in it.
        if (reading.Kind == Content.Logistics.Bay && Crate is int bay) _frame?.Light(Charger?.Invoke(bay) == true);
        if (reading.Kind is "smelter" or "growCapsule" && Crate is int fed) ShowFed(reading, fed);
        if (_problem == null || reading.Kind is not ("furnace" or "burnerGenerator")) return;
        var at = Crate is int id ? Furnace?.Invoke(id) : null;
        bool burning = at?.Burning == true;
        // Too cold for its input: the mark cold blue, and why by the pointer rather than under the panel.
        _tooCold = at is { Problem: "crafting.tooCold", Item: { } cold } ? cold : null;
        _fire.AddThemeStyleboxOverride("panel", _tooCold != null ? Cold : burning ? FireOn : FireOut);
        if (_progress != null) _progress.Value = at is { Item: not null, Work: > 0 } ? (double)at.Progress / at.Work : 0;
        _frame?.Light(burning);
        _problem.Text = at?.Problem is { } problem ? T(problem) : "";
        _problem.Visible = at?.Problem != null && _tooCold == null;
    }

    /// <summary>
    /// The open smelter's or grow capsule's: its mark lit while it works (the coils' orange, the lamp's pink) and cold
    /// blue while it is too cold, its bar, the frame's bolt lit while it works, and under it when it has no room for
    /// what it makes. What it does not work (rock in a capsule, a seed in a smelter) fills no bar and says nothing.
    /// </summary>
    private void ShowFed(Reading reading, int id)
    {
        if (Crafting is not { } crafting || _problem == null || _progress == null) return;
        var input = reading.At(new Place(0, id))?.Item;
        bool works = crafting.Works(id);
        _tooCold = input != null && crafting.TooCold(id) ? input : null;
        var on = Content.Stations.Get(reading.Kind!).Process == "grow" ? LampOn : FireOn;
        _fire.AddThemeStyleboxOverride("panel", _tooCold != null ? Cold : works ? on : FireOut);
        int? work = crafting.WorkOf(id);
        _progress.Value = work is int w ? (double)crafting.Progress(id) / w : 0;
        _frame?.Light(works);
        bool full = work != null && _tooCold == null && !crafting.CanSmelt(id);
        _problem.Text = full ? T("furnace.full") : "";
        _problem.Visible = full;
    }
}
