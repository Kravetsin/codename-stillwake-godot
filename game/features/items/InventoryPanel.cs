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
/// flame, ingot or sprout in its slot's corner (<see cref="Reading.MarkOf"/>), else one not analysed a violet "?" in its
/// slot's corner, and in the hand; a station's empty slot the faint ghost of its mark, lit while what is carried would go in (0097).
/// At the screen's right edge, the equipment by the body (0047, 0058) and the
/// hands' tool: a stack carried lights the slots that would wear it and dims the rest; a right click on a
/// canister of the hero's own breathes it (<see cref="Ui.Breathe"/>). Every slot is <see cref="Ui.Slot"/> wide,
/// and all of it is built again when that changes. It is the wrist computer (0074): the hotbar on the cuff, the bag on
/// the forearm display with the machine's module under it, the equipment on its unit (<see cref="WristScreen"/>).
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
    /// <summary>The open machine's module sockets (0086), this panel's and those lent (<see cref="LendSockets"/>); each forgotten as it leaves the tree.</summary>
    private readonly List<ModuleSocket> _sockets = [];
    private readonly HashSet<ModuleSocket> _lentSockets = [];
    /// <summary>A fitted socket pressed, its module being taken out: let go over a bag slot it goes there, over the socket anywhere it fits.</summary>
    private ModuleSocket? _unfitting;
    private ModuleSocket? _socketHovered;
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    /// <summary>The stack carried, over every panel.</summary>
    private readonly CanvasLayer _over = new() { Layer = ScreenLayers.Tips };
    /// <summary>Stacks sent across with shift flying to where they went, over every panel and under the stack carried.</summary>
    private readonly SlotFlights _flights = new();
    private bool _flown;
    private Control _shown = null!, _held = null!, _fire = null!;
    /// <summary>The wrist computer's screens (0074): the hotbar on the cuff, the bag on the forearm display, the equipment's unit.</summary>
    private WristScreen _hotbar = null!, _inventory = null!, _body = null!;
    /// <summary>How far the open screens have come on: their content fades in as the build tablet's does (0073).</summary>
    private readonly TabletMotion _motion = new();
    /// <summary>The open machine's frame, under the inventory.</summary>
    private MachineFrame? _frame;
    /// <summary>The bag's grid and the open crate's, which a sort's light crosses (<see cref="SortSweep"/>, 0081).</summary>
    private GridContainer _bag = null!;
    private GridContainer? _crateGrid;
    private SortButton _bagKey = null!;
    private SortButton? _crateKey;
    private SortSweep _sweep = null!;
    private TextureRect _heldIcon = null!;
    private Label _heldCount = null!, _creative = null!;
    /// <summary>The stack in hand's "?" while it is not analysed, in its corner as a slot's.</summary>
    private GlowIcon _heldMark = null!;
    private ProgressBar? _progress;
    private Label? _problem;
    /// <summary>A charging station's: the bar of what its battery holds, and what it does now in words.</summary>
    private ColorRect? _charged;
    private Label? _station;
    /// <summary>A charging station's numbers over its bar: what its battery holds of full, and what goes in a tick while it charges.</summary>
    private HBoxContainer? _stored;
    private Label? _storedText, _rate;
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
    /// <summary>The slot a canister was last asked to be breathed from by a right click, and the frame: the crew breathes it the next or never.</summary>
    private (Place Place, ulong Frame)? _breathedFrom;
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
        // A canister breathed pulses its slot as a shifted stack's landing does: the one clicked, else the hand's (0081).
        ui.Refilled += () =>
        {
            var from = _breathedFrom is var (place, frame) && Engine.GetProcessFrames() - frame <= 2 ? place : new Place(_chosen);
            if (BoxAt(from) is { } box) _flights.Pulse(box.GetGlobalRect());
            _breathedFrom = null;
        };
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

    /// <summary>What a charging station puts in a tick, stored scaled, by machine id: the power module's <c>ChargeRate</c>.</summary>
    public Func<int, int>? ChargeRate { get; set; }

    /// <summary>What a machine draws while it works, its modules folded in (0086), by machine id: the power module's <c>Draw</c>.</summary>
    public Func<int, int>? Draw { get; set; }

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

    public override void _Ready()
    {
        Layer = ScreenLayers.Hud;
        AddChild(_root);
        var flying = new CanvasLayer { Layer = ScreenLayers.Tips };
        flying.AddChild(_flights);
        AddChild(flying);
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
        _hotbar = new WristScreen(WristPieces.Cuff, WristArt.Screen, 4);
        _hotbar.AddChild(top);
        _hotbar.Position = new Vector2(Slots.Margin, Slots.Margin);
        _root.AddChild(_hotbar);

        // The grid, and under it the machine open; the equipment at the screen's other edge (0058).
        var shown = Style.Gap(new VBoxContainer { Position = new Vector2(Slots.Margin, Slots.InventoryTop(size)) }, 8);
        // The bag, as wide as the hotbar; its sort key on the display's right edge, outside the screen (0008, 0081).
        var grid = new WristScreen(WristPieces.Display, WristArt.Screen, 4, lit: true) { SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin };
        grid.AddChild(_bag = Grid(Enumerable.Range(Hotbar.Row, Content.Inventory.Slots - Hotbar.Row).Select(i => new Place(i))));
        grid.Mount(_bagKey = Sorter(null), screen => new Vector2(screen.X + WristLook.SideKeyOut, WristLook.SideKeyDown));
        shown.AddChild(_inventory = grid);
        _root.AddChild(_shown = shown);
        _shown.Visible = false;
        _root.AddChild(_body = Body(size));
        _body.Visible = false;
        // Last: a sort's light crosses over the slots.
        _root.AddChild(_sweep = new SortSweep());

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
        _held.AddChild(_heldMark = SlotBox.Corner(MarkLooks.Unknown, size));
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
        foreach (var socket in _lentSockets) _tipSpots.Add(socket, () => SocketTip(socket));
        _tipSpots.Show(null);
        _built = null;
        (_frame, _progress, _problem, _analyzer, _charged, _station, _tooCold, _hovered, _crateGrid, _crateKey) = (null, null, null, null, null, null, null, null, null, null);
        (_stored, _storedText, _rate) = (null, null, null);
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
        (_open, _picked, _unfitting) = (false, null, null);
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
    /// with the helmet mount's beside it, over the back's, the belt's three and the body's, each kind under its name;
    /// no silhouettes in empty slots. The screwdriver is a hotbar item (0107), so the hands have no slot.
    /// </summary>
    private WristScreen Body(int size)
    {
        var column = Style.Gap(new VBoxContainer(), 2);
        foreach (var kind in Content.Inventory.Equipment.Distinct().Where(k => k != Content.Helmet.Mount))
        {
            var row = Style.Gap(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, Slots.Gap);
            row.AddChild(Kind(kind));
            // What is fixed onto the helmet sits beside it.
            if (kind == Content.Helmet.Slot) row.AddChild(Kind(Content.Helmet.Mount));
            column.AddChild(row);
        }
        var body = new WristScreen(WristPieces.Body, WristArt.Screen, 4, lit: true);
        body.AddChild(column);
        return body;
    }

    /// <summary>One kind's slots in a row under its name.</summary>
    private VBoxContainer Kind(string kind)
    {
        var column = Style.Gap(new VBoxContainer(), 2);
        column.AddChild(Caption(kind));
        var row = Style.Gap(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, Slots.Gap);
        foreach (var place in Equipment.Of(kind)) row.AddChild(Box(place));
        column.AddChild(row);
        return column;
    }

    private static Label Caption(string kind) => new()
    {
        Text = T($"equip.{kind}"),
        LabelSettings = Style.Text(Style.Mono, 9, Style.Slate500),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    private SlotBox Box(Place place, string? key = null)
    {
        var box = new SlotBox(place, _ui.Slot, key);
        // A station's slot shows the ghost of what it takes (0097).
        if (place.Crate != null && _reading?.Kind is { } kind) box.GhostMark = Reading.GhostOf(kind, place.Slot);
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

    /// <summary>The bag's tall sort key, or with <paramref name="crate"/> that crate's square one in its header; its tip says what it does.</summary>
    private SortButton Sorter(int? crate)
    {
        var size = crate == null ? new Vector2(WristLook.SideKeyWidth, WristLook.SideKeyHeight) : new Vector2(WristLook.KeycapSide, WristLook.KeycapSide);
        var button = new SortButton(size, () => SortOf(crate));
        _tipSpots.Add(button, () => new Tip(T("items.sort")));
        return button;
    }

    /// <summary>
    /// Sorts the bag or the crate, light crossing its grid and, for the bag, the display's lamp flaring; a stack picked is
    /// put back first, as its slot may hold another after.
    /// </summary>
    private void SortOf(int? crate)
    {
        _ui.Click();
        (_picked, _dragFrom) = (null, null);
        Send(new Sort(crate));
        if (crate == null)
        {
            _sweep.Run(_bag);
            _inventory.Blink();
        }
        else if (_crateGrid != null) _sweep.Run(_crateGrid);
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
        return box.GhostMark == SlotMark.Docks ? new Tip(T("item.drone"), new TipLine(T("logistics.drone"), Tone.Dim)) : null;
    }

    public override void _Process(double delta)
    {
        // Hover by hit test: while a button is held Godot sends no mouse_entered to other controls.
        var hovered = SlotUnder(_mouse);
        var socketHovered = SocketUnder(_mouse);
        if (socketHovered != _socketHovered)
        {
            _socketHovered = socketHovered;
            Refresh();
        }
        if (hovered != _hovered)
        {
            _hovered = hovered;
            // Closed, a drag along the hotbar chooses each slot it passes over.
            if (!Shown && _dragFrom != null && hovered is { Crate: null } h && h.Slot < Hotbar.Row) _chosen = h.Slot;
            Refresh();
        }
        // A slot a shifted stack flies into, empty before, shows it only as it lands.
        if (_flights.Busy || _flown)
        {
            foreach (var box in _slots) box.Awaiting(_flights.Awaits(new SlotSpot(box.Place)));
            foreach (var socket in _sockets) socket.Awaiting(_flights.Awaits(new SocketSpot(socket.Machine, socket.Module)));
            _flown = _flights.Busy;
        }
        // A stack carried is the pointer: its icon centred on it, the arrow hidden while it is.
        _held.Position = _mouse - _held.Size / 2;
        bool carrying = _held.Visible && !_ui.MenuOpen.Get();
        var mode = carrying ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
        if (Input.MouseMode != mode) Input.MouseMode = mode;
        _ui.OnPointer.Set(carrying ? _held.Size.X : 0);
        Power(delta);
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
        var held = Held;
        // Carried, a stack lights the equipment slots that would wear it and dims the rest (0047).
        var carried = held is { } inHand ? reading.At(inHand.Place)?.Item : null;
        foreach (var box in _slots)
        {
            var p = box.Place;
            var stack = reading.At(p);
            box.Kept = p.Crate != null ? Kept?.Invoke(p) : null;
            box.Show(stack, HandFree && p.Crate == null && p.Slot == _chosen, held?.Place == p, _hovered == p,
                stack == null ? SlotMark.None : p.Crate == null ? reading.MarkOf(stack.Item) : reading.Unknown(stack.Item) ? SlotMark.Unknown : SlotMark.None,
                Equipment.Is(p) ? Equipment.GlowOf(p, carried) : null,
                stack == null && carried != null && reading.Lights(box.GhostMark, carried));
        }
        // A socket glows while a module carried would go in; one being taken out is carried as a stack is (0086).
        foreach (var socket in _sockets)
            socket.Show(FittedTo(socket.Machine).Contains(socket.Module), carried == socket.Module && GoesIn(carried, socket.Machine), _socketHovered == socket);
        if (_unfitting is { } unfitting && !GodotObject.IsInstanceValid(unfitting)) _unfitting = null;
        _held.Visible = held != null || _unfitting != null;
        if (held is { } h)
        {
            var stack = reading.At(h.Place)!;
            var item = stack.Item;
            _heldIcon.Texture = ItemIcons.Icon(Charges.Picture(item, stack.Charge));
            _heldMark.Visible = reading.Unknown(item);
            _heldCount.Text = h.Count > 1 ? h.Count.ToString() : "";
        }
        else if (_unfitting is { } coming)
        {
            _heldIcon.Texture = ItemIcons.Icon(coming.Module);
            _heldMark.Visible = false;
            _heldCount.Text = "";
        }
        ShowMachine(reading);
        // A machine open under the bag squares the display's bottom edge, where the machine's module meets it (0074).
        _inventory.Piece = _built != null ? WristPieces.DisplayDocked : WristPieces.Display;
        Power(0);
    }
}
