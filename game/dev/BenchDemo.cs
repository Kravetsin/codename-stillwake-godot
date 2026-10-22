using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Crafting;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --testbed --bench</c>: the hero by the testbed's arc bench, a plain drill in its vise, an arc module and a part-charged
/// accumulator in its bag, the charging station filling another accumulator, for recording the bench (0099). The script
/// opens the bench, points at the accumulator, its card showing, and at the module; shifts the module into the drill on the
/// bench, the shard's arcs striking into its coil; closes the panel, so the fitted drill shows clamped; opens it again and
/// queues two accumulators, then two arc modules, watching the queue run. The drone bay is left empty, so no drone flies past.
/// With <c>--open=charger</c> it only seeds, and the charging station is opened instead.
/// </summary>
public partial class BenchDemo : Control
{
    /// <summary>Where the arc module and the accumulator lie in the hero's own slots.</summary>
    public const int ArcSlot = 12, AccumulatorSlot = 13;

    /// <summary>How full the bag's accumulator is, and the charging station's, in thousandths.</summary>
    public const int Carried = 600, Charging = 250;

    /// <summary>The testbed's world with an arc module and an accumulator in peer 0's bag and an accumulator in the charging station.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        static JsonObject Accumulator(int thousandths) =>
            new() { ["item"] = "accumulator", ["count"] = 1, ["charge"] = Spec.Charge.Accumulator / 1000 * thousandths };
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == ArcSlot ? new JsonObject { ["item"] = Spec.Modules.Arc.Item, ["count"] = 1 } : i == AccumulatorSlot ? Accumulator(Carried) : null);
        var items = root["slices"]!["items"]!;
        items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        int IdOf(string kind) => (int)root["slices"]!["grid"]!["machines"]!.AsArray().First(m => (string?)m!["kind"] == kind)!["id"]!;
        var crates = items["crates"]!.AsArray();
        crates.First(c => (int)c!["id"]! == IdOf("charger"))!["slots"] = new JsonArray(Accumulator(Charging));
        // The bay's drones stay out of it: they fly past the bench all the time otherwise.
        crates.First(c => (int)c!["id"]! == IdOf(Spec.Logistics.Bay))!["slots"] = new JsonArray(null, null, null, null);
        return World.Load(root.ToJsonString(), modules());
    }

    private readonly ItemsView _view;
    private readonly Ui _ui;
    private readonly Func<Registry> _m;
    private readonly Func<FabricatorPanel?> _fabricator;
    private readonly List<(double At, Action Do)> _steps = [];
    private Vector2 _pos = new(640, 360), _target = new(640, 360);
    private double _t;
    private int _next;

    public BenchDemo(ItemsView view, Ui ui, Func<Registry> m, Func<FabricatorPanel?> fabricator)
    {
        (_view, _ui, _m, _fabricator) = (view, ui, m, fabricator);
        MouseFilter = MouseFilterEnum.Ignore;
        At(0.5, () => _ui.OpenCrate.Set(Bench()));
        At(1.2, () => Go(new Place(AccumulatorSlot)));
        At(2.6, () => Go(new Place(ArcSlot)));
        At(3.8, () => Click(shift: true));
        At(4.4, () => _target = _view.Panel.SocketAt(Bench()) ?? _target);
        At(6.0, () => _view.Panel.Close());
        At(6.1, () => _target = new Vector2(1180, 120));
        At(8.5, () => _ui.OpenCrate.Set(Bench()));
        At(9.3, () => _target = _fabricator()?.CenterOf("accumulator") ?? _target);
        At(10.0, () => Click());
        At(10.4, () => Click());
        At(10.9, () => _target = _fabricator()?.CenterOf(Spec.Modules.Arc.Item) ?? _target);
        At(11.4, () => Click());
        At(11.8, () => Click());
        At(12.4, () => _target = new Vector2(1180, 120));
    }

    private int Bench() => _m().Grid.MachinesOf(BenchLook.Kind)[0].Id;

    private void At(double t, Action step) => _steps.Add((t, step));

    private void Go(Place place) => _target = _view.Panel.CenterOf(place);

    public override void _Process(double delta)
    {
        _t += delta;
        while (_next < _steps.Count && _steps[_next].At <= _t) _steps[_next++].Do();
        if (_pos != _target)
        {
            _pos = _pos.MoveToward(_target, (float)(3000 * delta));
            Input.ParseInputEvent(new InputEventMouseMotion { Position = _pos, GlobalPosition = _pos });
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2[] arrow = [_pos, _pos + new Vector2(0, 14), _pos + new Vector2(4, 11), _pos + new Vector2(9, 11)];
        DrawColoredPolygon(arrow, Colors.White);
        DrawPolyline([.. arrow, _pos], Colors.Black, 1);
    }

    private void Click(bool shift = false)
    {
        foreach (bool pressed in new[] { true, false })
            Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = pressed, Position = _pos, GlobalPosition = _pos, ShiftPressed = shift });
    }
}
