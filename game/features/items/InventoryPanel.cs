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
    /// over the back's over the belt's three, each row under its name; no silhouettes in empty slots. The screwdriver is
    /// a hotbar item (0107), so the hands have no slot.
    /// </summary>
    private WristScreen Body(int size)
    {
        var column = Style.Gap(new VBoxContainer(), 2);
        foreach (var kind in Content.Inventory.Equipment.Distinct())
        {
            column.AddChild(Caption(kind));
            var row = Style.Gap(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, Slots.Gap);
            foreach (var place in Equipment.Of(kind)) row.AddChild(Box(place));
            column.AddChild(row);
        }
        var body = new WristScreen(WristPieces.Body, WristArt.Screen, 4, lit: true);
        body.AddChild(column);
        return body;
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

    /// <summary>
    /// Machine <paramref name="machine"/>'s module sockets for another feature's panel to lay out (0086), worked here as this
    /// panel's own: the crafting feature's queued stations (wired by <c>Main</c>). Null for a machine that takes no module.
    /// </summary>
    public Control? LendSockets(int machine, string kind)
    {
        if (Sockets(machine, kind, caption: false) is not { } row) return null;
        foreach (var socket in row.GetChildren().OfType<ModuleSocket>()) _lentSockets.Add(socket);
        Refresh();
        return row;
    }

    /// <summary>
    /// A row of machine <paramref name="machine"/>'s sockets, one for each module a machine of <paramref name="kind"/> takes
    /// (<c>Content.Modules</c>), after its caption; null for a machine that takes none (the furnace, the pod).
    /// </summary>
    private Control? Sockets(int machine, string kind, bool caption = true)
    {
        var takes = HostOf(machine, kind) is { } host ? Content.Modules.For(host).ToList() : [];
        if (takes.Count == 0) return null;
        var row = Style.Gap(new HBoxContainer(), Slots.Gap);
        foreach (var module in takes)
        {
            var socket = new ModuleSocket(machine, module, _ui.Slot);
            socket.Pressed += SocketPressed;
            _tipSpots.Add(socket, () => SocketTip(socket));
            _sockets.Add(socket);
            socket.TreeExiting += () =>
            {
                _sockets.Remove(socket);
                _lentSockets.Remove(socket);
            };
            row.AddChild(socket);
        }
        if (!caption) return row;
        var words = new Label
        {
            Text = T("module.socket"),
            LabelSettings = Style.Text(Style.Mono, 10, Style.Slate400),
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        row.AddChild(words);
        row.MoveChild(words, 0);
        return row;
    }

    /// <summary>
    /// The modules fitted into <paramref name="machine"/>: the open machine's as shown, this peer's own fits and takes in
    /// flight made (<see cref="Reading.Fitted"/>); another's as the world has them.
    /// </summary>
    private IReadOnlyList<string> FittedTo(int machine) =>
        machine == Crate && _reading != null ? _reading.Fitted : _items?.FittedTo(machine) ?? [];

    /// <summary>Whether <paramref name="item"/> would go into a socket of open machine <paramref name="machine"/>: a module it takes, not fitted yet.</summary>
    private bool GoesIn(string? item, int machine) =>
        item != null && _grid?.MachineById(machine) is { } m && HostOf(machine, m.Kind) is { } host && Content.Modules.Fits(item, host) && !FittedTo(machine).Contains(item);

    /// <summary>
    /// What machine <paramref name="machine"/>, of <paramref name="kind"/>, takes modules as: its kind, or at an arc bench the tool
    /// on it, as shown for the open one (<see cref="Reading.Host"/>, 0099); none for a bench holding none.
    /// </summary>
    private string? HostOf(int machine, string kind)
    {
        if (!MachineSpecs.TryGet(kind, out var spec) || !spec.Tunes) return kind;
        return machine == Crate && _reading != null ? _reading.Host : _items?.Crate(machine) is { Count: > 0 } slots ? slots[0]?.Item : null;
    }

    /// <summary>
    /// A socket pressed (<see cref="Clicks.OnSocket"/>): a module carried that goes in is fitted; a fitted one begins to
    /// come out, with shift at once into the bag, flying there.
    /// </summary>
    private void SocketPressed(ModuleSocket socket, bool shift)
    {
        if (!Shown || _reading is not { } reading) return;
        var held = Held;
        var result = Clicks.OnSocket(held, new SocketClick(
            socket.Machine, socket.Module, FittedTo(socket.Machine).Contains(socket.Module), shift, held is { } h ? reading.At(h.Place) : null, reading.Inventory));
        if (result.Command != null || result.TakesOut) _ui.Click();
        if (result.Command != null) (_picked, _dragFrom) = (null, null);
        if (result.TakesOut) _unfitting = socket;
        Act(reading, result);
        Refresh();
    }

    /// <summary>Fits what <paramref name="from"/> holds into <paramref name="socket"/>'s machine, the hand let go.</summary>
    private void FitFrom(Place from, ModuleSocket socket)
    {
        _ui.Click();
        (_picked, _dragFrom) = (null, null);
        Send(new Fit(from, socket.Machine));
    }

    /// <summary>
    /// The left button let go over a socket: a module carried or dragged there is fitted; one being taken out, let go
    /// where it was pressed, flies into the bag's first empty slot (<see cref="Clicks.TakeOut"/>).
    /// </summary>
    private void ReleaseOnSocket(ModuleSocket socket)
    {
        var unfitting = _unfitting;
        _unfitting = null;
        if (unfitting == socket && _reading is { } reading) Act(reading, Clicks.TakeOut(socket.Machine, socket.Module, reading.Inventory));
        else if (Held is { } held && _reading?.At(held.Place)?.Item == socket.Module && GoesIn(socket.Module, socket.Machine)) FitFrom(held.Place, socket);
        _dragFrom = null;
        Refresh();
    }

    private ModuleSocket? SocketUnder(Vector2 at) => _ui.MenuOpen.Get() ? null :
        _sockets.FirstOrDefault(s => GodotObject.IsInstanceValid(s) && s.IsVisibleInTree() && s.GetGlobalRect().HasPoint(at));

    /// <summary>
    /// A fitted socket's tip: its module's card, a click taking it out; an empty one's, only its name: the tech icon in it
    /// says what it takes (0086).
    /// </summary>
    private Tip? SocketTip(ModuleSocket socket)
    {
        if (_reading is not { } reading) return null;
        if (FittedTo(socket.Machine).Contains(socket.Module))
        {
            var tip = reading.TipOf(socket.Module);
            return tip with { Card = tip.Card! with { Use = new CardKey("LMB", T("card.takeOut")) } };
        }
        return new Tip(T("module.empty"));
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
            (_frame, _progress, _problem, _analyzer, _charged, _station, _tooCold, _crateGrid, _crateKey) = (null, null, null, null, null, null, null, null, null);
            (_stored, _storedText, _rate) = (null, null, null);
        }
        if (want is not var (crate, kind)) return;
        // A queued station (the lifepod's Fabricator, the Former) docks in the same place, the crafting feature's own panel.
        if (Content.Stations.TryGet(kind, out var station) && !station.Fed) return;
        var spec = MachineSpecs.Get(kind);
        // Its bolt the power it makes, or else what it draws.
        _frame = new MachineFrame(T($"tool.{kind}"), spec.Makes ?? spec.Draws)
        {
            // As wide as the wrist's column, as the bag over it (0081).
            CustomMinimumSize = new Vector2(Slots.Column(_ui.Slot), 0),
        };
        // A crate sorts, its button in the header beside the drone badge (0008).
        if (spec.Is(Content.MachineTrait.Container)) _frame.Head.AddChild(_crateKey = Sorter(crate));
        // Whether drones serve it, at the right of its header (0062).
        if (Served?.Invoke(crate) is { } served) _frame.Head.AddChild(served);
        // Its body by what it is (0113): a fire smelting its inputs or making power, a fed station, a bay, a charging station.
        _frame.Add(kind switch
        {
            "analyzer" => _analyzer = new AnalyzerPanel(Box(new Place(0, crate)), _tipSpots, _ui.Slot),
            _ when spec.Is(Content.MachineTrait.Fire) && spec.Inputs > 0 => FurnaceBody(crate, spec),
            _ when spec.Is(Content.MachineTrait.Fire) => GeneratorBody(crate, spec),
            _ when station is { Fed: true } => FedBody(crate, kind),
            _ when spec.Is(Content.MachineTrait.Zone) => BayBody(crate, spec),
            _ when spec.Charges => ChargerBody(crate),
            _ => _crateGrid = Grid(Enumerable.Range(0, spec.Slots ?? 0).Select(i => new Place(i, crate))),
        });
        // Its module sockets under its body, for a machine that takes any (0086).
        if (Sockets(crate, kind) is { } sockets) _frame.Add(sockets);
        _shown.AddChild(_frame);
    }

    /// <summary>A drone bay's four slots, each showing a faint drone's mark while empty: drones dock there and charge, each slot's bar its drone's charge (0062).</summary>
    private Control BayBody(int crate, Content.MachineSpec spec)
    {
        var row = Style.Gap(new HBoxContainer(), Slots.Gap);
        for (int i = 0; i < spec.Slots; i++) row.AddChild(Box(new Place(i, crate)));
        return row;
    }

    /// <summary>Its input over its fuel, as Minecraft lays them, the fire between them; then how far its work is and its outputs.</summary>
    private Control FurnaceBody(int crate, Content.MachineSpec spec)
    {
        var column = Style.Gap(new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, 4);
        int fuel = spec.FuelSlot!.Value;
        column.AddChild(Box(new Place(0, crate)));
        column.AddChild(_fire = HeatMark());
        column.AddChild(Box(new Place(fuel, crate)));
        var outputs = Style.Gap(new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter }, Slots.Gap);
        for (int i = fuel + 1; i < spec.Slots; i++) outputs.AddChild(Box(new Place(i, crate)));
        var line = Style.Gap(new HBoxContainer(), 8);
        line.AddChild(column);
        line.AddChild(_progress = Style.Bar(Style.Orange400, 56));
        line.AddChild(outputs);
        return WithProblem(line);
    }

    /// <summary>
    /// A fed station on power, the furnace's without the fuel: its input over its mark, lit while it works, its bar and its
    /// outputs. The electric smelter's mark and bar are a fire's orange; a grow capsule's mark is its lamp's pink, its bar
    /// green; its empty input shows the ghost of a sprout, the smelter's a crucible (0097).
    /// </summary>
    private Control FedBody(int crate, string kind)
    {
        var column = Style.Gap(new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, 4);
        var spec = MachineSpecs.Get(kind);
        var process = Content.Stations.Get(kind).Process;
        column.AddChild(Box(new Place(0, crate)));
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
    private Control GeneratorBody(int crate, Content.MachineSpec spec)
    {
        var column = Style.Gap(new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, 4);
        column.AddChild(_fire = HeatMark());
        column.AddChild(Box(new Place(spec.FuelSlot!.Value, crate)));
        // In a row, as the furnace's: a column alone in the frame would stretch its slot across it.
        var line = new HBoxContainer();
        line.AddChild(column);
        return WithProblem(line);
    }

    /// <summary>
    /// A charging station's, as the station mockup: its slot beside a bar of what the battery holds, over the bar what it holds
    /// and holds full in power units, as its card says it, and while it charges what goes in a tick; under the bar what it does
    /// now; what it draws is the frame's bolt, lit while it charges.
    /// </summary>
    private Control ChargerBody(int crate)
    {
        var bay = Style.Gap(new HBoxContainer { CustomMinimumSize = new Vector2(196, 0) }, 10);
        bay.AddChild(Box(new Place(0, crate)));
        var right = Style.Gap(new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center }, 4);
        var numbers = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        (_stored, _storedText) = ItemCardView.PoweredLine("", Style.Text(Style.Mono, 10, Style.Slate300));
        _stored.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        numbers.AddChild(_stored);
        numbers.AddChild(_rate = new Label { LabelSettings = Style.Text(Style.Mono, 10, new Color(Charges.Green)), MouseFilter = Control.MouseFilterEnum.Ignore });
        right.AddChild(numbers);
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
                    int digit = Hotbar.SlotOfDigit((int)(key.PhysicalKeycode - Key.Key0))!.Value;
                    if (digit != _chosen) _ui.Click();
                    _chosen = digit;
                    break;
                case Key.Q:
                    // A card in hand, not the chosen slot: Q drops nothing it does not show.
                    if (!HandFree) return;
                    if (_reading?.Inventory[_chosen] is { } stack)
                    {
                        _ui.Click();
                        DropFrom(new Place(_chosen), stack.Count);
                    }
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
        // Sockets first (0086): a module let go there is fitted; one taken out goes to the bag slot it is let go over.
        if (SocketUnder(_mouse) is { } socket)
        {
            ReleaseOnSocket(socket);
            return;
        }
        if (_unfitting is { } from)
        {
            _unfitting = null;
            if (SlotUnder(_mouse) is { Crate: null } bag && bag.Slot < Content.Inventory.Slots) Send(new Unfit(from.Machine, from.Module, bag.Slot));
            Refresh();
            return;
        }
        if (SlotUnder(_mouse) is { } place)
        {
            ReleaseAt(place);
            return;
        }
        _dragFrom = null;
        if (Held is { } inHand && !OverPanel(_mouse))
        {
            _ui.Click();
            DropFrom(inHand.Place, inHand.Count);
            _picked = null;
            Refresh();
        }
    }

    /// <summary>The middle of the bag's sort key, or of the open crate's; null with none shown: for scripted runs.</summary>
    public Vector2? SortKeyAt(bool crate) => (crate ? _crateKey : _bagKey) is { } key && key.IsVisibleInTree() ? key.GetGlobalRect().GetCenter() : null;

    /// <summary>The middle of machine <paramref name="machine"/>'s first module socket on screen while it shows, for scripted runs (0086).</summary>
    public Vector2? SocketAt(int machine) =>
        _sockets.FirstOrDefault(s => GodotObject.IsInstanceValid(s) && s.Machine == machine && s.IsVisibleInTree())?.GetGlobalRect().GetCenter();

    /// <summary>The middle of the slot at <paramref name="place"/> on screen, for scripted runs.</summary>
    public Vector2 CenterOf(Place place) => _slots.First(s => s.Place == place).GetGlobalRect().GetCenter();

    // Under the open menu no slot is pointed at: its name would show over the menu.
    private Place? SlotUnder(Vector2 at) => _ui.MenuOpen.Get() ? null :
        _slots.FirstOrDefault(s => s.IsVisibleInTree() && s.GetGlobalRect().HasPoint(at))?.Place;

    // Any panel under the pointer counts, another feature's too: a click on the crafts throws nothing.
    private bool OverPanel(Vector2 at) => GetViewport().GuiGetHoveredControl() != null || Covers().Any(r => r.HasPoint(at));

    /// <summary>
    /// Where the hotbar, the open inventory with the machine under it and the equipment lie on the screen, their metal
    /// round them: what the crew's markers keep out of.
    /// </summary>
    public IEnumerable<Rect2> Covers()
    {
        yield return _hotbar.Outer;
        if (_body.Visible) yield return _body.Outer;
        if (!_shown.Visible) yield break;
        foreach (var panel in _shown.GetChildren().OfType<Control>())
            if (panel.IsVisibleInTree()) yield return panel is WristScreen screen ? screen.Outer : panel.GetGlobalRect();
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
        // With the inventory closed a press only chooses a slot, and a drag along the hotbar chooses the slot
        // it is let go over, as Terraria does: nothing is moved where the player cannot see it carried.
        if (!Shown && place.Crate == null && place.Slot < Hotbar.Row)
        {
            // A drag along the hotbar clicks as it is let go, on the slot it chose.
            _ui.Click();
            _chosen = place.Slot;
            Refresh();
        }
    }

    private void ClickAt(Place place, bool right, bool shift)
    {
        if (!right && !shift) _dragFrom = Held != null ? null : place;
        if (!Shown)
        {
            if (place.Crate == null && place.Slot < Hotbar.Row)
            {
                _ui.Click();
                _chosen = place.Slot;
            }
            Refresh();
            return;
        }
        if (_reading == null) return;
        var before = _reading;
        var held = Held;
        // A queued station's inputs share what is shift-clicked in, onto its kind first; a module goes into its free socket.
        var result = Clicks.On(held, SlotClick.Of(_reading, place, right, shift, Crate));
        (_picked, _pickedItem) = (result.Picked, result.Picked is { } p ? _reading.At(p.Place)?.Item : null);
        if (result.Breathes)
        {
            _breathedFrom = (place, Engine.GetProcessFrames());
            _ui.Breathe();
        }
        // Every action clicks as it is made, a shift's on launch; a canister's when the crew breathes it.
        if (!result.Breathes && (result.Command != null || result.Picked != held)) _ui.Click();
        if (result.Command != null) Act(before, result);
        else
        {
            if (shift && !right && before.At(place) != null) BoxAt(place)?.Shake();
            Refresh();
        }
    }

    /// <summary>
    /// A stack shift-clicked out of a slot that is not one of this panel's, at <paramref name="from"/> on screen: a queued
    /// station's output, the crafting feature's (wired by <c>Main</c>). Taken as a shift-click on its slot is, flying.
    /// </summary>
    public void ShiftOut(Place place, Rect2 from)
    {
        if (_reading is not { } reading) return;
        Act(reading, Clicks.On(null, SlotClick.Of(reading, place, false, true, Crate)), from);
    }

    /// <summary>
    /// The one way a click's command goes out (0075, 0086): sent, the slots read again at once, and a stack sent with shift
    /// flying from where it was (<paramref name="from"/> for a slot not this panel's) to where it went (<see cref="Fly"/>).
    /// </summary>
    private void Act(Reading before, Clicked result, Rect2? from = null)
    {
        if (result.Command is not { } command) return;
        Send(command);
        if (result.Flight is { } flight) Fly(before, flight, from);
    }

    private SlotBox? BoxAt(Place place) => _slots.FirstOrDefault(s => s.Place == place && s.IsVisibleInTree());

    private ModuleSocket? SocketAt(SocketSpot spot) =>
        _sockets.FirstOrDefault(s => GodotObject.IsInstanceValid(s) && s.Machine == spot.Machine && s.Module == spot.Module && s.IsVisibleInTree());

    /// <summary>Where <paramref name="spot"/> is on screen while it shows: a slot's box or a socket.</summary>
    private Rect2? RectOf(Spot spot) => spot switch
    {
        SlotSpot slot => BoxAt(slot.Place)?.GetGlobalRect(),
        SocketSpot socket => SocketAt(socket)?.GetGlobalRect(),
        _ => null,
    };

    /// <summary>
    /// A stack sent with shift flying from where it was to where it went, as the slots show it now, the command made at
    /// once (0014): into a socket, into the slot named, or each slot it landed in; a move the sim would refuse lands
    /// nowhere, and its slot shakes.
    /// </summary>
    private void Fly(Reading before, Flight flight, Rect2? from)
    {
        if (_reading is not { } after || (from ?? RectOf(flight.From)) is not { } src) return;
        var stack = flight.From is SlotSpot { Place: var place } ? before.At(place) : flight.From is SocketSpot s ? new Stack(s.Module, 1) : null;
        if (stack == null) return;
        var icon = ItemIcons.Icon(Charges.Picture(stack.Item, stack.Charge));
        var start = src.GetCenter();
        switch (flight.To)
        {
            case SocketSpot socket when SocketAt(socket) is { } to:
                _flights.Fly(icon, start, to.GetGlobalRect(), socket, true);
                return;
            case SlotSpot slot when BoxAt(slot.Place) is { } to:
                _flights.Fly(icon, start, to.GetGlobalRect(), slot, before.At(slot.Place) == null);
                return;
            case null when flight.From is SlotSpot { Place: var at }:
                var shown = _slots.Where(b => b.IsVisibleInTree()).Select(b => b.Place).ToList();
                var landed = Clicks.Landed(at, stack.Item, before.At, after.At, shown).ToList();
                if (landed.Count == 0) BoxAt(at)?.Shake();
                for (int i = 0; i < landed.Count; i++)
                    if (BoxAt(landed[i]) is { } box) _flights.Fly(icon, start, box.GetGlobalRect(), new SlotSpot(landed[i]), before.At(landed[i]) == null, i);
                return;
        }
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

    /// <summary>
    /// The open screens' content faded in as the build tablet's screen comes on (<see cref="TabletMotion"/>, 0073), the
    /// metal at once; opened again soon after it was closed, lit at once. Stepped as it opens too, so no frame shows it lit before.
    /// </summary>
    private void Power(double seconds)
    {
        _motion.Step(Shown, seconds);
        float power = (float)_motion.Power;
        _inventory.Lit(power);
        _body.Lit(power);
        _frame?.Lit(power);
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
        var meter = held == null ? null : Charges.MeterOf(held);
        if (_stored != null && _storedText != null)
        {
            _stored.Visible = meter != null;
            _storedText.Text = meter is { } m ? T("card.stored", ("n", m.Stored), ("all", m.Capacity)) : "";
        }
        if (_rate != null)
        {
            int rate = charging && Crate is int station ? ChargeRate?.Invoke(station) ?? 0 : 0;
            _rate.Text = rate > 0 ? T("charger.rate", ("n", ItemCards.Number((double)rate / Content.Charge.Scale))) : "";
        }
        // What it draws with its modules: an arc fitted makes it five times (0100).
        if (Crate is int open && Draw?.Invoke(open) is int draw) _frame?.Describe(T("tool.charger"), draw);
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
        var spec = reading.Kind is string kind && MachineSpecs.TryGet(kind, out var s) ? s : null;
        bool zone = spec?.Is(Content.MachineTrait.Zone) == true;
        if (spec is { Charges: true } && !zone && Crate is int station) ShowCharger(reading.At(new Place(0, station)), Charger?.Invoke(station) == true);
        // A bay's bolt lit while it charges a drone docked in it.
        if (zone && Crate is int bay) _frame?.Light(Charger?.Invoke(bay) == true);
        if (reading.Kind is string fedKind && Stillwake.Content.Stations.TryGet(fedKind, out var fedSpec) && fedSpec.Fed && spec?.FuelSlot == null && Crate is int fed) ShowFed(reading, fed);
        if (_problem == null || spec?.FuelSlot == null) return;
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
        // What it draws with its modules: an arc fitted doubles it (0086).
        if (Draw?.Invoke(id) is int draw) _frame?.Describe(T($"tool.{reading.Kind}"), draw);
        _frame?.Light(works);
        bool full = work != null && _tooCold == null && !crafting.CanSmelt(id);
        _problem.Text = full ? T("furnace.full") : "";
        _problem.Visible = full;
    }
}
