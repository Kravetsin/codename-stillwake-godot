using System;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Landing;

namespace Stillwake.Game.Dev;

/// <summary>
/// Launch platforms laid in the hull's floor over space, a lamp lighting them, the room full of air: a run of two pieces cabled to
/// a panel out in space, its lamp green, E opening it and venting the room; a run of three with no cable, red, staying shut; a
/// piece alone. With <c>--platform-vent</c> the cabled run opens by itself two seconds in, its air seen going; <c>--platform-air</c>
/// shows it under the air overlay.
/// </summary>
[Dev("--platform", "launch platforms in the floor of an aired room: a run cabled to a panel, lit green, opened by E; a run uncabled, red; a piece alone")]
[Dev("--platform-vent", "the launch platforms, the cabled run opening by itself two seconds in: its room's air seen going out through it")]
[Dev("--platform-air", "--platform-vent with the air overlay on: the air going red as it goes")]
public sealed class PlatformScenario : DevScenario
{
    /// <summary>The tick the cabled run opens on with <c>--platform-vent</c>.</summary>
    private const int VentAt = 40;
    private bool _vented;

    public override Layout? Map => Layout.Parse(
        [
            "                    ",
            "  PPP               ",
            "                    ",
            "####################",
            "#....L.............#",
            "#..................#",
            "#.S................#",
            "#..................#",
            "##__#___###_########",
            "                    ",
        ],
        [],
        [((2, 1), (2, 8)), ((2, 1), (5, 4))]).Doubled();

    /// <summary>Every room's cells holding air, through the save, as a filled room would.</summary>
    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        var roomOf = seed.M.Rooms.RoomOfCells.ToArray();
        root["slices"]!["atmosphere"]!["aired"] = new JsonArray([.. Enumerable.Range(0, roomOf.Length).Where(c => roomOf[c] >= 0).Select(c => (JsonNode)c)]);
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    public override void Attach(DevContext c)
    {
        if (Args.Has("--platform-air")) c.Ui.AirOn.Set(true);
    }

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (_vented || !Args.Has("--platform-vent") && !Args.Has("--platform-air") || client.World.Tick < VentAt) return;
        _vented = true;
        int width = m.Grid.Width, cell = 8 * 2 * width + 2 * 2;
        client.Send(new SetPlatform(cell, true));
        log($"opened the run at {cell}: {m.Landing.Problem(cell, true) ?? "powered"}");
    }
}
