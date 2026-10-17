using System;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Crafting;

namespace Stillwake.Game.Features.Crafting;

/// <summary>
/// For recordings (<c>-- --pod</c>): the wreck, gear and wire analysed, the hero carrying scrap and iron,
/// which opens the lifepod as E would a second in, queues five wires and one more, and leaves the panel
/// open while they are made; the pointer rests on the fire axe's recipe, then on the rotor's, short of
/// what it takes.
/// </summary>
public partial class PodDemo(Ui ui, Func<(Client Client, Registry M)?> world, Func<FabricatorPanel?> panel) : Node
{
    private const int OpenAt = 20, QueueAt = 30, AxeAt = 50, RotorAt = 80;
    private int _frame;

    /// <summary><paramref name="start"/> with scrap and iron in this peer's inventory and gear and wire analysed, opening the rotor: put in through its save.</summary>
    public static World Seed(World start, Func<System.Collections.Generic.IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(start.Save())!;
        var slots = new JsonArray(new JsonObject { ["item"] = "scrap", ["count"] = 40 }, new JsonObject { ["item"] = "iron", ["count"] = 9 });
        while (slots.Count < Content.Inventory.Total) slots.Add(null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        root["slices"]!["research"]!["analysed"] = new JsonArray("gear", "wire");
        return World.Load(root.ToJsonString(), modules());
    }

    public override void _Process(double delta)
    {
        _frame++;
        if (world() is not var (client, m)) return;
        var pod = m.Grid.Machines.FirstOrDefault(x => x.Kind == "lifepod");
        if (pod == null) return;
        if (_frame == OpenAt) ui.OpenCrate.Set(pod.Id);
        if (_frame == AxeAt) Point("fireAxe");
        if (_frame == RotorAt) Point("rotor");
        if (_frame != QueueAt) return;
        client.Send(new Craft(pod.Id, "wire", 5));
        client.Send(new Craft(pod.Id, "wire", 1));
    }

    private void Point(string craft)
    {
        if (panel()?.CenterOf(craft) is not { } at) return;
        Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at });
    }
}
