using System;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Landing;

namespace Stillwake.Game.Dev;

/// <summary>
/// Launch platforms laid in the hull's floor over space, a lamp lighting them, the rooms full of air: a run of two pieces cabled
/// to a panel out in space, its lamp green, E opening it and venting its room; a run of three with no cable, red, staying shut; a
/// piece alone in the room beside, which has a door to space. A drop pod stands on the cabled run, boarded on E, biofuel in the
/// hero's bag for its slot. With <c>--platform-vent</c> the cabled run and that door open by
/// themselves two seconds in, both rooms' air seen going alike; <c>--platform-air</c> shows it under the air overlay.
/// </summary>
[Dev("--platform", "launch platforms in the floor of an aired room: a run cabled to a panel, lit green, opened by E, a drop pod on it; a run uncabled, red; a piece alone")]
[Dev("--platform-vent", "the launch platforms, the cabled run opening by itself two seconds in: its room's air seen going out through it")]
[Dev("--platform-air", "--platform-vent with the air overlay on: the air going red as it goes")]
[Dev("--platform-seat", "the launch platforms, the hero boarding the drop pod by itself a second in, seated behind its porthole, and leaving it two seconds later")]
public sealed class PlatformScenario : DevScenario
{
    /// <summary>The tick the cabled run opens on with <c>--platform-vent</c>.</summary>
    private const int VentAt = 40;
    private bool _vented;
    /// <summary>The tick the hero boards the pod on with <c>--platform-seat</c>.</summary>
    private const int SeatAt = 20;
    /// <summary>Ticks it sits before it leaves again with <c>--platform-seat</c>: the porthole seen emptying as it steps out.</summary>
    private const int LeaveAfter = 40;

    public override Layout? Map => Layout.Parse(
        [
            "                     ",
            "  PPP                ",
            "                     ",
            "#################### ",
            "#....L....#........# ",
            "#.ZZ......#........# ",
            "#.ZZ..S...#........D ",
            "#.ZZ......#........D ",
            "##__#___###_######## ",
            "                     ",
        ],
        [],
        [((2, 1), (2, 8)), ((2, 1), (5, 4))]).Doubled();

    /// <summary>Biofuel in the hero's bag, for the drop pod's slot.</summary>
    private static readonly (string Item, int Count) Fuel = ("biofuel", 10);

    /// <summary>Every room's cells holding air, through the save, as a filled room would; the biofuel in the first free slot of the bag.</summary>
    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        var roomOf = seed.M.Rooms.RoomOfCells.ToArray();
        root["slices"]!["atmosphere"]!["aired"] = new JsonArray([.. Enumerable.Range(0, roomOf.Length).Where(c => roomOf[c] >= 0).Select(c => (JsonNode)c)]);
        var items = root["slices"]!["items"]!;
        var inventories = items["inventories"] as JsonArray ?? [];
        // A bag made here is the hero's from the start, so it holds what a hero comes aboard with too.
        if (inventories.FirstOrDefault(i => (int)i!["id"]! == 0) is not { } own)
            inventories.Add(own = new JsonObject
            {
                ["id"] = 0,
                ["slots"] = new JsonArray([.. Enumerable.Range(0, Content.Inventory.Total).Select(i => i < Content.Inventory.Kit.Count
                    ? new JsonObject { ["item"] = Content.Inventory.Kit[i].Item, ["count"] = Content.Inventory.Kit[i].Count } : (JsonNode?)null)]),
            });
        var slots = own["slots"]!.AsArray();
        int free = Enumerable.Range(Content.Inventory.Hotbar, Content.Inventory.Slots - Content.Inventory.Hotbar).First(i => slots[i] == null);
        slots[free] = new JsonObject { ["item"] = Fuel.Item, ["count"] = Fuel.Count };
        items["inventories"] = inventories;
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    public override void Attach(DevContext c)
    {
        if (Args.Has("--platform-air")) c.Ui.AirOn.Set(true);
    }

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (Args.Has("--platform-seat") && client.World.Tick == SeatAt && m.Grid.MachinesWith(Content.MachineTrait.Seats) is [var pod, ..])
            client.Send(new Board(pod.Id));
        if (Args.Has("--platform-seat") && client.World.Tick == SeatAt + LeaveAfter) client.Send(new Alight());
        if (_vented || !Args.Has("--platform-vent") && !Args.Has("--platform-air") || client.World.Tick < VentAt) return;
        _vented = true;
        int width = m.Grid.Width, cell = 8 * 2 * width + 2 * 2;
        client.Send(new SetPlatform(cell, true));
        // The room beside it opened to space through a door on the same tick, to be seen venting alike.
        client.Send(new Stillwake.Sim.Grid.SetDoor(6 * 2 * width + 19 * 2, true));
        log($"opened the run at {cell}: {m.Landing.Problem(cell, true) ?? "powered"}");
    }
}
