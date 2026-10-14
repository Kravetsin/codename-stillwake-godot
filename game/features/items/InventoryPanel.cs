using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Net;
using Stillwake.Sim;
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
/// outputs, an analyzer's slot and how far it is. Stacks move by clicks (<see cref="Clicks.On"/>), or
/// dragged from one slot to another; Tab or Esc closes both. 1–9 and the wheel over the world choose a
/// hotbar slot, Q throws its stack.
/// </summary>
public partial class InventoryPanel : CanvasLayer
{
    /// <summary>The slots are read this often, and at once after this peer's own move.</summary>
    private const double RefreshMs = 100;

    private static readonly StyleBoxFlat FireOut = Style.Box(Style.Slate700, Style.Slate700, 0, 5);

    private readonly List<SlotBox> _slots = [];
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private Control _hotbar = null!, _shown = null!, _held = null!, _fire = null!;
    private Control? _cratePanel;
    private TextureRect _heldIcon = null!;
    private Label _heldCount = null!, _creative = null!, _tipText = null!;
    private PanelContainer _tip = null!;
    private ProgressBar? _progress;
    private Label? _problem;

    private Client? _client;
    private IItemsQueries? _items;
    private IGridQueries? _grid;
    private IResearchQueries? _research;
    private HeroSpot? _hero;
    private Reading? _reading;
    private double _sinceRead;

    private bool _open;
    private int? _crate;
    /// <summary>The crate and its kind the crate panel was made for.</summary>
    private (int Id, string Kind)? _built;
    private int _chosen;
    private Picked? _picked;
    private Place? _dragFrom, _hovered;
    private Vector2 _mouse;

    /// <summary>The crate open beside the inventory: E on one within reach opens it, out of reach it closes.</summary>
    public int? Crate => _crate;

    /// <summary>The hotbar slot chosen, with 1–9 or the wheel.</summary>
    public int Chosen => _chosen;

    private bool Shown => _open || _crate != null;

    /// <summary>A picked stack whose slot still holds it, while the panels are open: one emptied meanwhile, by another player at a crate, is dropped.</summary>
    private Picked? Held => Shown && _picked is { } p && _reading?.At(p.Place) != null ? p : null;

    public override void _Ready()
    {
        AddChild(_root);
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var top = Style.Gap(new HBoxContainer(), 8);
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
        shown.AddChild(Style.Panel(Style.Slate700,
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
        _held.AddChild(_heldCount = new Label
        {
            LabelSettings = Style.Text(Style.Mono, 10, Colors.White, shadow: true),
            Size = new Vector2(34, 38),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        _root.AddChild(_held);

        _tip = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        var tipBox = Style.Box(Style.Slate950 with { A = 0.9f }, Style.Slate600 with { A = 0.8f });
        (tipBox.ContentMarginLeft, tipBox.ContentMarginRight, tipBox.ContentMarginTop, tipBox.ContentMarginBottom) = (8, 8, 3, 4);
        (tipBox.ShadowColor, tipBox.ShadowSize) = (new Color(0, 0, 0, 0.3f), 3);
        _tip.AddThemeStyleboxOverride("panel", tipBox);
        _tip.AddChild(_tipText = new Label { LabelSettings = Style.Text(Style.Bold, 12, Style.Slate100) });
        _root.AddChild(_tip);
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

    /// <summary>Opens crate <paramref name="id"/> beside the inventory.</summary>
    public void Open(int id)
    {
        if (_crate == id) return;
        _crate = id;
        Read();
    }

    /// <summary>Closes the crate beside the inventory: out of reach, or gone.</summary>
    public void CloseCrate()
    {
        _crate = null;
        Read();
    }

    /// <summary>Closes both the inventory and the crate, putting a picked stack back.</summary>
    public void Close()
    {
        (_open, _picked, _crate) = (false, null, null);
        Read();
    }

    private void Read()
    {
        _sinceRead = 0;
        if (_client == null || _items == null || _grid == null || _research == null) return;
        _reading = Reading.Of(_client.Id, _client.InFlight, _items, _grid, _research, _crate);
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

    private SlotBox Box(Place place)
    {
        var box = new SlotBox(place);
        box.Pressed += ClickAt;
        _slots.Add(box);
        return box;
    }

    /// <summary>The open crate's panel, made anew for each crate: a crate's grid, a furnace's or an analyzer's own.</summary>
    private void BuildCrate()
    {
        (int, string)? want = _crate is int id && _reading?.Kind is string k ? (id, k) : null;
        if (want == _built) return;
        _built = want;
        if (_cratePanel != null)
        {
            _slots.RemoveAll(s => s.Place.Crate != null);
            _shown.RemoveChild(_cratePanel);
            _cratePanel.QueueFree();
            (_cratePanel, _progress, _problem) = (null, null, null);
        }
        if (want is not var (crate, kind)) return;
        _cratePanel = kind switch
        {
            "furnace" => FurnacePanel(crate),
            "analyzer" => AnalyzerPanel(crate),
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

    /// <summary>What is in its slot, and how far its analysis is.</summary>
    private Control AnalyzerPanel(int crate)
    {
        var line = Style.Gap(new HBoxContainer(), 8);
        line.AddChild(Box(new Place(0, crate)));
        line.AddChild(_progress = Style.Bar(Style.Sky400, 80));
        return WithProblem(Style.Sky700, line);
    }

    /// <summary>A panel of <paramref name="edge"/> with <paramref name="line"/>, and under it why the machine stands still.</summary>
    private Control WithProblem(Color edge, Control line)
    {
        var stack = Style.Gap(new VBoxContainer(), 4);
        stack.AddChild(line);
        stack.AddChild(_problem = new Label { LabelSettings = Style.Text(Style.Bold, 10, Style.Red300), Visible = false });
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
                    else (_open, _picked) = (true, null);
                    break;
                case Key.Escape:
                    Close();
                    break;
                case >= Key.Key1 and <= Key.Key9:
                    _chosen = (int)(key.PhysicalKeycode - Key.Key1);
                    break;
                case Key.Q:
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

    private Place? SlotUnder(Vector2 at) =>
        _slots.FirstOrDefault(s => s.IsVisibleInTree() && s.GetGlobalRect().HasPoint(at))?.Place;

    private bool OverPanel(Vector2 at) =>
        _hotbar.GetGlobalRect().HasPoint(at)
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
        var lifted = Clicks.On(null, new SlotClick(from, _reading.At(from), false, false, _crate));
        if (lifted.Picked == null) return;
        var put = Clicks.On(lifted.Picked, new SlotClick(place, _reading.At(place), false, false, _crate));
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
        var result = Clicks.On(Held, new SlotClick(place, _reading.At(place), right, shift, _crate, fuelSlot));
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
        _tip.Position = _mouse + new Vector2(16, 16);
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
            box.Show(reading.At(p), p.Crate == null && p.Slot == _chosen, held?.Place == p, _hovered == p);
        }
        _held.Visible = held != null;
        if (held is { } h)
        {
            _heldIcon.Texture = ItemIcons.Icon(reading.At(h.Place)!.Item);
            _heldCount.Text = h.Count > 1 ? h.Count.ToString() : "";
        }
        ShowMachine(reading);
        // The name of the stack under the pointer, beside it at once; none once its slot is not drawn.
        var stack = _hovered is { } at && (Shown || (at.Crate == null && at.Slot < Hotbar.Row)) ? reading.At(at) : null;
        _tip.Visible = stack != null;
        if (stack != null) _tipText.Text = T($"item.{stack.Item}");
        _tip.ResetSize();
    }

    /// <summary>
    /// How far the open machine's work is and why it stands still. The furnace module is not ported yet:
    /// its fire shows out and its bar empty.
    /// </summary>
    private void ShowMachine(Reading reading)
    {
        if (_progress == null || _problem == null) return;
        string? problem = null;
        if (reading.Kind == "analyzer")
        {
            var at = reading.Analyzer;
            _progress.Value = at == null ? 0 : (double)at.Progress / at.Work;
            problem = at?.Problem;
        }
        else if (reading.Kind == "furnace")
        {
            _fire.AddThemeStyleboxOverride("panel", FireOut);
            _progress.Value = 0;
        }
        _problem.Text = problem == null ? "" : T(problem);
        _problem.Visible = problem != null;
    }
}
