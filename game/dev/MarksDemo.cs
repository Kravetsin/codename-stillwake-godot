using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --testbed --open=&lt;kind&gt; --marks &lt;item&gt; [--into &lt;slot&gt;]</c>: for recording the slot marks and
/// the station slots' ghosts (0095). Creative mode off and nothing analysed, so the "?" shows; the hero's bag holds a stack of
/// each item a station marks. Once the machine is open the pointer rests on the item's slot, its card shown, picks the
/// stack up and carries it over the machine's slot <c>into</c> (0 by default), its ghost lit, and over the bag again.
/// </summary>
public partial class MarksDemo : Control
{
    /// <summary>What the bag holds, from its first slot: something each station marks, a module, what is never analysed.</summary>
    public static readonly IReadOnlyList<string> Bag =
        ["rock", "scrapChunk", "planks", "silicon", "seed", "battery", Spec.Logistics.Drone, Spec.Modules.Arc.Item, "iron", "fireAxe"];

    /// <summary>
    /// The testbed's world, creative mode off, nothing analysed, <see cref="Bag"/> in peer 0's bag past the hotbar, and every
    /// machine of <paramref name="kind"/> emptied and remembering nothing, so its slots show their ghosts.
    /// </summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules, string? kind)
    {
        var root = JsonNode.Parse(world.Save())!;
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++)
        {
            int at = i - Spec.Inventory.Hotbar;
            slots.Add(at >= 0 && at < Bag.Count ? new JsonObject { ["item"] = Bag[at], ["count"] = Bag[at] is "rock" or "planks" or "seed" ? 20 : 1 } : null);
        }
        var items = root["slices"]!["items"]!;
        items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        items["creative"] = false;
        if (root["slices"]!["research"] is JsonObject research) research["analysed"] = new JsonArray();
        var emptied = new HashSet<int>();
        foreach (var machine in root["slices"]!["grid"]!["machines"]!.AsArray())
            if (machine!["kind"]!.GetValue<string>() == kind?.Split('@')[0]) emptied.Add(machine["id"]!.GetValue<int>());
        Drop(items["crates"]!.AsArray(), n => emptied.Contains(n["id"]!.GetValue<int>()));
        Drop(items["kept"]!.AsArray(), n => emptied.Contains(n["machine"]!.GetValue<int>()));
        return World.Load(root.ToJsonString(), modules());
    }

    private static void Drop(JsonArray array, Func<JsonNode, bool> gone)
    {
        for (int i = array.Count - 1; i >= 0; i--)
            if (gone(array[i]!)) array.RemoveAt(i);
    }

    private readonly ItemsView _view;
    private readonly string _kind;
    private readonly Func<Registry> _m;
    private readonly List<(double At, Action Do)> _steps = [];
    private Vector2 _pos = new(900, 500), _target = new(900, 500);
    private double _t;
    private int _next;

    public MarksDemo(ItemsView view, string kind, string item, int into, Func<Registry> m)
    {
        (_view, _kind, _m) = (view, kind, m);
        MouseFilter = MouseFilterEnum.Ignore;
        var from = new Place(Spec.Inventory.Hotbar + Math.Max(0, IndexOf(item)));
        At(1.6, () => Go(from));
        At(3.0, Click);
        At(3.4, () => _target = _view.Panel.CenterOf(new Place(into, Machine())) + new Vector2(0, 30));
        At(4.4, () => Go(new Place(into, Machine())));
        At(6.0, () => Go(from));
        At(6.8, Click);
        At(7.2, () => _target = new Vector2(1180, 120));
    }

    private static int IndexOf(string item)
    {
        for (int i = 0; i < Bag.Count; i++)
            if (Bag[i] == item) return i;
        return -1;
    }

    private int Machine() => _m().Grid.MachinesOf(_kind.Split('@')[0])[0].Id;

    private void At(double t, Action step) => _steps.Add((t, step));

    private void Go(Place place) => _target = _view.Panel.CenterOf(place);

    public override void _Process(double delta)
    {
        _t += delta;
        while (_next < _steps.Count && _steps[_next].At <= _t) _steps[_next++].Do();
        if (_pos != _target)
        {
            _pos = _pos.MoveToward(_target, (float)(1500 * delta));
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

    private void Click()
    {
        foreach (bool pressed in new[] { true, false })
            Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = pressed, Position = _pos, GlobalPosition = _pos });
    }
}
