using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Furnace;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;
using FurnaceSpec = Stillwake.Content.Furnace;
using MachineSpecs = Stillwake.Content.Machines;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The hotbar, always in the top-left corner; and with Tab, or E on a crate, the rest of the inventory
/// and the crate beside it (Inventory.tsx): a crate's slots, a furnace's input over its fuel and its
/// outputs, an analyzer's own panel (<see cref="AnalyzerPanel"/>); the lifepod's is the crafting feature's. Stacks move by clicks (<see cref="Clicks.On"/>),
/// or dragged from one slot to another; Tab or Esc closes both. 1–9 and the wheel over the world choose a
/// hotbar slot, Q throws its stack. A stack's name, and whether it waits for an analyzer, show in the one
/// tooltip; an item not analysed has a violet "?" in its slot's corner, and in the hand.
/// </summary>
public partial class InventoryPanel : CanvasLayer
{
    /// <summary>The slots are read this often, and at once after this peer's own move.</summary>
    private const double RefreshMs = 100;

    private static readonly StyleBoxFlat FireOut = Style.Box(Style.Slate700, Style.Slate700, 0, 5);
    private static readonly StyleBoxFlat FireOn = Style.Box(Style.Orange400, Style.Orange400, 0, 5);
    private static readonly LabelSettings Trouble = Style.Text(Style.Bold, 10, Style.Red300);

    private readonly List<SlotBox> _slots = [];
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    /// <summary>The stack carried, over every panel.</summary>
    private readonly CanvasLayer _over = new() { Layer = ScreenLayers.Tips };
    private Control _hotbar = null!, _shown = null!, _inventory = null!, _held = null!, _fire = null!;
    private Control? _cratePanel;
    private TextureRect _heldIcon = null!;
    private Label _heldCount = null!, _creative = null!, _heldMark = null!;
    private ProgressBar? _progress;
    private Label? _problem;
    private AnalyzerPanel? _analyzer;
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
    private Place? _dragFrom, _hovered;
    private Vector2 _mouse;

    private readonly Ui _ui;

    /// <param name="ui">The hand, which choosing a hotbar slot or opening the inventory frees, and the crate open.</param>
    public InventoryPanel(Ui ui)
    {
        _ui = ui;
        _tipSpots = new TipSpots(ui.Tip);
        // The crew's E opens a crate, and reaching out of it closes it: shown at once.
        ui.OpenCrate.Subscribe(Read);
        // The chosen slot is outlined only while the hand holds what is in it.
        ui.Hand.Subscribe(Refresh);
    }

    /// <summary>The crate open beside the inventory: E on one within reach opens it, out of reach it closes.</summary>
    private int? Crate => _ui.OpenCrate.Get();

    private bool HandFree => _ui.Hand.Get() == Holding.Free;

    /// <summary>What a furnace is doing, by machine id: the furnace module's <c>At</c>.</summary>
    public Func<int, Smelting>? Furnace { get; set; }

    /// <summary>The hotbar slot chosen, with 1–9 or the wheel.</summary>
    public int Chosen => _chosen;

    /// <summary>Whether the inventory or a crate is open: what Esc closes before it opens the menu.</summary>
    public bool Shown => _open || Crate != null;

    /// <summary>A picked stack whose slot still holds it, while the panels are open: one emptied meanwhile, by another player at a crate, is dropped.</summary>
    private Picked? Held => Shown && _picked is { } p && _reading?.At(p.Place) != null ? p : null;

    public override void _Ready()
    {
        Layer = ScreenLayers.Hud;
        AddChild(_root);
        AddChild(_over);
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var top = Style.Gap(new HBoxContainer(), 8);
        top.AddChild(ToolSlot());
        top.AddChild(Grid(Enumerable.Range(0, Hotbar.Row).Select(i => new Place(i))));
        top.AddChild(_creative = new Label
        {
            LabelSettings = Style.Text(Style.Mono, 12, Style.Amber400),
            Text = T("items.creative"),
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false,
        });
        _hotbar = Style.Panel(Style.Slate700, top);
        _hotbar.Position = new Vector2(12, 12);
        _root.AddChild(_hotbar);

        var shown = Style.Gap(new VBoxContainer { Position = new Vector2(12, 112) }, 8);
        shown.AddChild(_inventory = Style.Panel(Style.Slate700,
            Grid(Enumerable.Range(Hotbar.Row, Content.Inventory.Slots - Hotbar.Row).Select(i => new Place(i)))));
        _root.AddChild(_shown = shown);
        _shown.Visible = false;

        _held = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Size = new Vector2(36, 36), Visible = false };
        _held.AddChild(_heldIcon = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Position = new Vector2(4, 4),
            Size = new Vector2(28, 28),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        _held.AddChild(_heldMark = SlotBox.Mark());
        _held.AddChild(_heldCount = new Label
        {
            LabelSettings = Style.Text(Style.Mono, 10, Colors.White, shadow: true),
            Size = new Vector2(34, 38),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        _over.AddChild(_held);
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

    private GridContainer Grid(IEnumerable<Place> places)
    {
        var grid = Style.Gap(new GridContainer { Columns = Hotbar.Row }, 4);
        foreach (var place in places) grid.AddChild(Box(place));
        return grid;
    }

    /// <summary>
    /// The tool the right button works with, left of the hotbar: empty, the screwdriver everyone has, pale.
    /// Only shown for now (0030): nothing is put in it until a tool better than the screwdriver exists.
    /// </summary>
    private Control ToolSlot()
    {
        var slot = new Panel { CustomMinimumSize = new Vector2(36, 36), MouseFilter = Control.MouseFilterEnum.Stop };
        slot.AddThemeStyleboxOverride("panel", Style.Box(Style.Slate800 with { A = 0.9f }, Style.Slate600));
        var icon = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://art/tool_screwdriver.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(1, 1, 1, 0.5f),
        };
        slot.AddChild(icon);
        icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize, 4);
        var key = new Label { Text = "RMB", LabelSettings = Style.Text(Style.Mono, 8, Style.Slate400, shadow: true), MouseFilter = Control.MouseFilterEnum.Ignore };
        slot.AddChild(key);
        // The icon runs from the bottom left to the top right: the bottom right corner is free.
        key.Position = new Vector2(19, 24);
        _tipSpots.Add(slot, () => new Tip(T("gear.screwdriver"), [new TipLine(T("gear.screwdriver.about"), Tone.Dim)]));
        return slot;
    }

    private SlotBox Box(Place place)
    {
        var box = new SlotBox(place);
        box.Pressed += ClickAt;
        _tipSpots.Add(box, () => _reading?.At(box.Place) is { } stack ? _reading.TipOf(stack.Item) : null);
        _slots.Add(box);
        return box;
    }

    /// <summary>The open crate's panel, made anew for each crate: a crate's grid, a furnace's or an analyzer's own.</summary>
    private void BuildCrate()
    {
        (int, string)? want = Crate is int id && _reading?.Kind is string k ? (id, k) : null;
        if (want == _built) return;
        _built = want;
        if (_cratePanel != null)
        {
            _slots.RemoveAll(s => s.Place.Crate != null);
            _shown.RemoveChild(_cratePanel);
            _cratePanel.QueueFree();
            (_cratePanel, _progress, _problem, _analyzer) = (null, null, null, null);
        }
        if (want is not var (crate, kind)) return;
        // The lifepod's Fabricator docks beside the inventory, the crafting feature's own panel.
        if (kind == "lifepod") return;
        _cratePanel = kind switch
        {
            "furnace" => FurnacePanel(crate),
            "analyzer" => _analyzer = new AnalyzerPanel(Box(new Place(0, crate)), _tipSpots),
            _ => Style.Panel(Style.Amber700 with { A = 0.7f },
                Grid(Enumerable.Range(0, MachineSpecs.Get(kind).Slots ?? 0).Select(i => new Place(i, crate)))),
        };
        _cratePanel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        _shown.AddChild(_cratePanel);
    }

    /// <summary>Its input over its fuel, as Minecraft lays them, the fire between them; then how far its work is and its outputs.</summary>
    private Control FurnacePanel(int crate)
    {
        var column = Style.Gap(new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, 4);
        column.AddChild(Box(new Place(0, crate)));
        _fire = new Panel { CustomMinimumSize = new Vector2(10, 10), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
        column.AddChild(_fire);
        column.AddChild(Box(new Place(FurnaceSpec.FuelSlot, crate)));
        var outputs = Style.Gap(new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter }, 4);
        for (int i = FurnaceSpec.FuelSlot + 1; i < MachineSpecs.Get("furnace").Slots; i++) outputs.AddChild(Box(new Place(i, crate)));
        var line = Style.Gap(new HBoxContainer(), 8);
        line.AddChild(column);
        line.AddChild(_progress = Style.Bar(Style.Orange400, 56));
        line.AddChild(outputs);
        return WithProblem(Style.Orange700, line);
    }

    /// <summary>A panel of <paramref name="edge"/> with <paramref name="line"/>, under it why the machine stands still.</summary>
    private Control WithProblem(Color edge, Control line)
    {
        var stack = Style.Gap(new VBoxContainer(), 4);
        stack.AddChild(line);
        stack.AddChild(_problem = new Label { LabelSettings = Trouble, Visible = false });
        return Style.Panel(edge with { A = 0.7f }, stack);
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
                    _chosen = (int)(key.PhysicalKeycode - Key.Key1);
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
        if (SlotUnder(_mouse) is { } place)
        {
            ReleaseAt(place);
            return;
        }
        _dragFrom = null;
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
        || (_shown.Visible && _shown.GetChildren().OfType<Control>().Any(c => c.IsVisibleInTree() && c.GetGlobalRect().HasPoint(at)));

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
        var lifted = Clicks.On(null, new SlotClick(from, _reading.At(from), false, false, Crate));
        if (lifted.Picked == null) return;
        var put = Clicks.On(lifted.Picked, new SlotClick(place, _reading.At(place), false, false, Crate));
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
        int? fuelSlot = _reading.Kind == "furnace" ? FurnaceSpec.FuelSlot : null;
        var result = Clicks.On(Held, new SlotClick(place, _reading.At(place), right, shift, Crate, fuelSlot));
        _picked = result.Picked;
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
        _held.Position = _mouse + new Vector2(6, 6);
        var grid = _inventory.GetGlobalRect();
        _ui.InventoryEdge.Set(_shown.Visible ? (grid.End.X, grid.Position.Y) : null);
        // A hidden slot says nothing; under the open menu none is pointed at.
        _tipSpots.Frame(_ui.MenuOpen.Get() ? null : _mouse);
    }

    private void Refresh()
    {
        if (_reading is not { } reading) return;
        BuildCrate();
        _shown.Visible = Shown;
        _creative.Visible = reading.Creative;
        var held = Held;
        foreach (var box in _slots)
        {
            var p = box.Place;
            var stack = reading.At(p);
            box.Show(stack, HandFree && p.Crate == null && p.Slot == _chosen, held?.Place == p, _hovered == p,
                stack != null && reading.Unknown(stack.Item));
        }
        _held.Visible = held != null;
        if (held is { } h)
        {
            var item = reading.At(h.Place)!.Item;
            _heldIcon.Texture = ItemIcons.Icon(item);
            _heldMark.Visible = reading.Unknown(item);
            _heldCount.Text = h.Count > 1 ? h.Count.ToString() : "";
        }
        ShowMachine(reading);
    }

    /// <summary>The open analyzer's panel; how far the open furnace's work is, whether it burns, and why it stands still.</summary>
    private void ShowMachine(Reading reading)
    {
        if (reading.Lessons != null) _analyzer?.Show(reading.Lessons);
        if (_progress == null || _problem == null || reading.Kind != "furnace") return;
        var at = Crate is int id ? Furnace?.Invoke(id) : null;
        _fire.AddThemeStyleboxOverride("panel", at?.Burning == true ? FireOn : FireOut);
        _progress.Value = at is { Item: not null, Work: > 0 } ? (double)at.Progress / at.Work : 0;
        _problem.Text = at?.Problem is { } problem ? T(problem) : "";
        _problem.Visible = at?.Problem != null;
    }
}
