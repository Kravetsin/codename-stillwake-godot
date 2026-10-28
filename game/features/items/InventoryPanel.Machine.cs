// InventoryPanel's open machine: its body in the machine frame by kind (crate, bay, furnace, fed station, generator, charger) and its readouts.
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Furnace;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using static Stillwake.Game.I18n.En;
using MachineSpecs = Stillwake.Content.Machines;

namespace Stillwake.Game.Features.Items;

public partial class InventoryPanel
{
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
        // A drop pod's key under its slot: Descend, or Return off the ship.
        if (Launch?.Invoke(crate) is { } launch) _frame.Add(launch);
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
        _problem.Text = at?.Problem is { } problem ? T(problem, ("n", Takes(at.Item))) : "";
        _problem.Visible = at?.Problem != null && _tooCold == null;
    }

    /// <summary>How many of <paramref name="item"/> one smelt takes, what "crafting.tooFew" names; 1 for nothing.</summary>
    private static int Takes(string? item) => item != null ? Content.Items.Get(item).SmeltTakes : 1;

    /// <summary>
    /// The open smelter's or grow capsule's: its mark lit while it works (the coils' orange, the lamp's pink) and cold
    /// blue while it is too cold, its bar, the frame's bolt lit while it works, and under it when it waits for more of its
    /// input or has no room for what it makes. What it does not work (rock in a capsule, a seed in a smelter) fills no bar and says nothing.
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
        bool few = crafting.TooFew(id);
        bool full = work != null && _tooCold == null && !few && !crafting.CanSmelt(id);
        _problem.Text = few ? T("crafting.tooFew", ("n", Takes(input))) : full ? T("furnace.full") : "";
        _problem.Visible = few || full;
    }
}
