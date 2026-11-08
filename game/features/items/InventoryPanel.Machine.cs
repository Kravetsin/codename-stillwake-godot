// InventoryPanel's open machine: its body in the machine frame by kind (crate, bay, furnace, fed station, generator, charger) and its readouts.
using System;
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
    /// <summary>The open crate's panel, made anew for each crate: a crate's grid, a furnace's own, in the machine frame.</summary>
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
            (_frame, _progress, _problem, _tooCold, _crateGrid, _crateKey, _send, _draw, _drawCount, _switch, _liquid) = (null, null, null, null, null, null, null, null, null, null, null);
            _chargerLines.Clear();
        }
        if (want is not var (crate, kind)) return;
        // A queued station (the lifepod's Fabricator, the Former) opens in the same place, the crafting feature's own panel.
        if (Content.Stations.TryGet(kind, out var station) && !station.Fed) return;
        var spec = MachineSpecs.Get(kind);
        // Its bolt the power it makes, or else what it draws.
        _frame = new MachineFrame(T($"tool.{kind}"), spec.Makes ?? spec.Draws)
        {
            // As wide as the wrist's column, as the bag over it (0081).
            CustomMinimumSize = new Vector2(Slots.Column(_ui.Slot), 0),
            Tone = HoloShape.ToneOf(spec),
            // Its contents come in as its hologram grows (machine-holograms).
            Modulate = Colors.White with { A = 0 },
        };
        _holoBox = MachineHolo.BoxOf(_grid, crate);
        // A crate sorts, its button in the header beside the drone badge (0008).
        if (spec.Is(Content.MachineTrait.Container)) _frame.Head.AddChild(_crateKey = Sorter(crate));
        // Whether drones serve it, at the right of its header (0062).
        if (Served?.Invoke(crate) is { } served) _frame.Head.AddChild(served);
        // On or off, for a machine that switches: a press turns it (industrial-oxygen).
        if (spec.Is(Content.MachineTrait.Switches))
            _frame.Head.AddChild(_switch = Style.Button("", () => { if (Switching != null) Send(new Stillwake.Sim.Power.SetSwitch(crate, Switching.IsOff(crate))); }));
        // Its body by what it is (0113): a fire smelting its inputs or making power, a fed station, a bay, a charging station.
        _frame.Add(kind switch
        {
            _ when spec.Is(Content.MachineTrait.Sells) => TradeBody(crate, spec),
            _ when spec.Is(Content.MachineTrait.Gas) => DrawBody(crate, spec, n => new Stillwake.Sim.Atmosphere.DrawCanister(crate, n)),
            _ when spec.Is(Content.MachineTrait.HoldsWater) => DrawBody(crate, spec, n => new Stillwake.Sim.Water.DrawWater(crate, n)),
            _ when spec.Is(Content.MachineTrait.AirSource) && spec.SlotTakes != null => VaporizerBody(crate, spec),
            _ when spec.Is(Content.MachineTrait.Fire) && spec.Inputs > 0 => FurnaceBody(crate, spec),
            _ when spec.Is(Content.MachineTrait.Fire) => GeneratorBody(crate, spec),
            _ when station is { Fed: true } => FedBody(crate, kind),
            _ when spec.Is(Content.MachineTrait.Zone) => BayBody(crate, spec),
            _ when spec.Charges => ChargerBody(crate, spec.Slots ?? 1),
            _ => _crateGrid = Grid(Enumerable.Range(0, spec.Slots ?? 0).Select(i => new Place(i, crate))),
        });
        // Its module sockets under its body, for a machine that takes any (0086).
        if (Sockets(crate, kind) is { } sockets) _frame.Add(sockets);
        // A drop pod's key under its slot: Descend, or Return off the ship.
        if (Launch?.Invoke(crate) is { } launch) _frame.Add(launch);
        _shown.AddChild(_frame);
    }

    /// <summary>
    /// A drone bay's four slots, each showing a faint drone's mark while empty: drones of either kind dock there and charge,
    /// each slot's bar its drone's charge (0062); a gap, then its fuel slot of canisters, a faint canister while empty (salvage-drones).
    /// </summary>
    private Control BayBody(int crate, Content.MachineSpec spec)
    {
        var row = Style.Gap(new HBoxContainer(), Slots.Gap);
        int fuel = spec.SlotTakes?.Slot ?? -1;
        for (int i = 0; i < spec.Slots; i++)
        {
            if (i == fuel) row.AddChild(new Control { CustomMinimumSize = new Vector2(Slots.Gap * 3, 0), MouseFilter = Control.MouseFilterEnum.Ignore });
            row.AddChild(Box(new Place(i, crate)));
        }
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
    /// <summary>A charging station's body: a line a slot, its battery's box beside its numbers, its bar and what it does.</summary>
    private Control ChargerBody(int crate, int slots)
    {
        var lines = Style.Gap(new VBoxContainer(), 8);
        for (int slot = 0; slot < slots; slot++)
        {
            var bay = Style.Gap(new HBoxContainer { CustomMinimumSize = new Vector2(196, 0) }, 10);
            bay.AddChild(Box(new Place(slot, crate)));
            var right = Style.Gap(new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center }, 4);
            var numbers = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            var (stored, storedText) = ItemCardView.PoweredLine("", Style.Text(Style.Mono, 10, Style.Slate300));
            stored.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            numbers.AddChild(stored);
            var rate = new Label { LabelSettings = Style.Text(Style.Mono, 10, new Color(Charges.Green)), MouseFilter = Control.MouseFilterEnum.Ignore };
            numbers.AddChild(rate);
            right.AddChild(numbers);
            var track = new Panel { CustomMinimumSize = new Vector2(0, 10), MouseFilter = Control.MouseFilterEnum.Ignore };
            track.AddThemeStyleboxOverride("panel", Style.Box(new Color("0b1220"), Style.Slate700, radius: 2));
            var charged = new ColorRect { Color = ItemCardView.Green, MouseFilter = Control.MouseFilterEnum.Ignore };
            track.AddChild(charged);
            charged.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            (charged.OffsetLeft, charged.OffsetTop, charged.OffsetRight, charged.OffsetBottom) = (1, 1, 0, -1);
            right.AddChild(track);
            var station = _frame!.Dim(new Label { LabelSettings = Style.Text(Style.Mono, 11, Style.Slate400) });
            right.AddChild(station);
            bay.AddChild(right);
            lines.AddChild(bay);
            _chargerLines.Add(new ChargerLine(charged, station, stored, storedText, rate));
        }
        return lines;
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
    /// The open machine's hologram, a frame on: its window's contents faded in as it grows. A machine another feature shows (a
    /// queued station's) makes it go at once; shut, it shrinks and pulls back where the window was.
    /// </summary>
    private void Hologram(double delta)
    {
        if (_holo == null) return;
        if (_frame != null && Crate is int open)
            _frame.Modulate = Colors.White with { A = _holo.Step(delta, new MachineHolo.Target(open, _frame.GetGlobalRect(), _holoBox, _frame.Tone)) };
        else if (Crate != null) _holo.Snap();
        else _holo.Step(delta, null);
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

    /// <summary>
    /// The open charging station's lines: each battery's bar, what it does in words and what goes into it a tick, the
    /// station's rate shared as the power module shares it; its bolt lit with what it draws while it charges.
    /// </summary>
    private void ShowCharger(Reading reading, int station, bool charging)
    {
        var held = _chargerLines.Select((_, slot) => reading.At(new Place(slot, station))).ToList();
        int shortOf = held.Count(h => h != null && Charges.Share(h) < 1);
        int rate = charging && shortOf > 0 ? (ChargeRate?.Invoke(station) ?? 0) / shortOf : 0;
        for (int slot = 0; slot < _chargerLines.Count; slot++)
        {
            var (charged, said, stored, storedText, rateText) = _chargerLines[slot];
            var stack = held[slot];
            float share = stack == null ? 0 : Charges.Share(stack);
            charged.AnchorRight = share;
            charged.OffsetRight = share >= 1 ? -1 : 0;
            charged.Visible = share > 0;
            said.Text = T(Charges.StationState(stack, charging));
            var meter = stack == null ? null : Charges.MeterOf(stack);
            stored.Visible = meter != null;
            storedText.Text = meter is { } m ? T("card.stored", ("n", m.Stored), ("all", m.Capacity)) : "";
            rateText.Text = rate > 0 && share < 1 && stack != null ? T("charger.rate", ("n", ItemCards.Number((double)rate / Content.Charge.Scale))) : "";
        }
        // What it draws with its modules: an arc fitted makes it five times (0100).
        if (Crate is int open && Draw?.Invoke(open) is int draw) _frame?.Describe(T("tool.charger"), draw);
        _frame?.Light(charging);
    }

    /// <summary>A trade station's body: its slots and its capsule's key; what the ship has earned stands under the bag.</summary>
    private VBoxContainer TradeBody(int crate, Content.MachineSpec spec)
    {
        var body = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
        body.AddChild(_crateGrid = Grid(Enumerable.Range(0, spec.Slots ?? 0).Select(i => new Place(i, crate))));
        body.AddChild(_send = Style.Button("", () => Send(new Stillwake.Sim.Trade.SendCapsule(crate))));
        _send.Icon = Signs.Credit();
        return body;
    }

    /// <summary>
    /// A liquid store's body (industrial-oxygen): its hopper, which hands and drones fill, and a slider of how many to draw
    /// out with the key beside it, <paramref name="draw"/> the command for a count.
    /// </summary>
    private VBoxContainer DrawBody(int crate, Content.MachineSpec spec, Func<int, Command> draw)
    {
        var body = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
        body.AddChild(_crateGrid = Grid(Enumerable.Range(0, spec.Slots ?? 0).Select(i => new Place(i, crate))));
        var row = Style.Gap(new HBoxContainer(), 8);
        row.AddChild(_drawCount = new HSlider
        {
            MinValue = 1,
            MaxValue = 1,
            Step = 1,
            Value = 1,
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(72, 22),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        });
        row.AddChild(_draw = Style.Button("", () => Send(draw((int)_drawCount.Value))));
        body.AddChild(row);
        return body;
    }

    /// <summary>
    /// The draw key: the slider's full scale the <paramref name="whole"/> units the store holds, the key saying
    /// <paramref name="text"/> of the count picked, dark with why by the pointer while it cannot be drawn.
    /// </summary>
    private void ShowDraw(int whole, Func<int, string?> problemOf, Func<int, string> text)
    {
        if (_draw == null || _drawCount == null) return;
        _drawCount.MaxValue = Math.Max(1, whole);
        _drawCount.Visible = whole > 1;
        int count = (int)Math.Min(_drawCount.Value, Math.Max(1, whole));
        string? problem = problemOf(count);
        _draw.Disabled = problem != null;
        _draw.TooltipText = problem != null ? T(problem) : "";
        string said = text(count);
        if (_draw.Text != said) _draw.Text = said;
    }

    /// <summary>A vaporizer's body: its slot of canisters, which hands and drones fill, and the liquid it has to turn (industrial-oxygen).</summary>
    private VBoxContainer VaporizerBody(int crate, Content.MachineSpec spec)
    {
        var body = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
        body.AddChild(_crateGrid = Grid(Enumerable.Range(0, spec.Slots ?? 0).Select(i => new Place(i, crate))));
        body.AddChild(_liquid = new Label { LabelSettings = Style.Text(Style.Mono, 11, Style.Slate400), MouseFilter = Control.MouseFilterEnum.Ignore });
        return body;
    }

    /// <summary>The cistern's draw key: bottles out of what it holds, of its capacity (industrial-oxygen).</summary>
    private void ShowCistern(int cistern)
    {
        if (Water == null || _client == null) return;
        int held = Water.WaterIn(cistern), peer = _client.Id;
        ShowDraw(held / Content.Water.Bottle, n => Water.DrawProblem(cistern, peer, n),
            n => T("cistern.draw", ("n", n), ("held", Litres.Of(held)), ("all", Litres.Of(Content.Water.CisternCapacity))));
    }

    /// <summary>The tank's draw key: canisters out of what it holds, of its capacity.</summary>
    private void ShowTank(int tank)
    {
        if (Air == null || _client == null) return;
        int held = Air.OxygenIn(tank), peer = _client.Id;
        ShowDraw(held / Content.Air.Canister, n => Air.DrawProblem(tank, peer, n),
            n => T("tank.draw", ("n", n), ("held", Litres.Of(held)), ("all", Litres.Of(Content.Air.TankCapacity))));
    }

    /// <summary>The capsule's key: what it would carry while home, what it carries and when it comes back while away, dark with nothing to send.</summary>
    private void ShowCapsule(int station)
    {
        if (_send == null || Trade == null) return;
        var flight = Trade.FlightOf(station);
        _send.Disabled = Trade.SendProblem(station) != null;
        string text = flight != null
            ? T("trade.back", ("s", (flight.Left + 19) / 20), ("n", flight.Credits))
            : T("trade.send", ("n", Trade.Worth(station)));
        if (_send.Text != text) _send.Text = text;
    }

    /// <summary>
    /// Whether the open furnace or generator burns and why it stands still; how
    /// far the furnace's work is; the frame's bolt lit while the machine works.
    /// </summary>
    private void ShowMachine(Reading reading)
    {
        var spec = reading.Kind is string kind && MachineSpecs.TryGet(kind, out var s) ? s : null;
        if (spec?.Is(Content.MachineTrait.Sells) == true && Crate is int sells) ShowCapsule(sells);
        if (spec?.Is(Content.MachineTrait.Gas) == true && Crate is int tank) ShowTank(tank);
        if (_switch != null && Switching != null && Crate is int switched)
        {
            string said = T(Switching.IsOff(switched) ? "switch.off" : "switch.on");
            if (_switch.Text != said) _switch.Text = said;
        }
        if (spec?.Is(Content.MachineTrait.HoldsWater) == true && Crate is int cistern) ShowCistern(cistern);
        if (_liquid != null && Air != null && Crate is int vaporizer)
        {
            string held = T("vaporizer.liquid", ("n", Litres.Of(Air.LiquidFor(vaporizer))));
            if (_liquid.Text != held) _liquid.Text = held;
        }
        bool zone = spec?.Is(Content.MachineTrait.Zone) == true;
        if (spec is { Charges: true } && !zone && Crate is int station) ShowCharger(reading, station, Charger?.Invoke(station) == true);
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
        // A fire blown by liquid oxygen says so where a problem would (industrial-oxygen).
        _problem.Text = at?.Problem is { } problem ? T(problem, ("n", Takes(at.Item))) : at is { Burning: true, Blown: true } ? T("furnace.blown") : "";
        _problem.Visible = (at?.Problem != null || at is { Burning: true, Blown: true }) && _tooCold == null;
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
